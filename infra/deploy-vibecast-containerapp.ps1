<#
.SYNOPSIS
    Deploy a verified VibeCast release into the existing Azure Container Apps environment.
.DESCRIPTION
    Run infra/bootstrap-production-database.ps1 first.
    This script does NOT provision PostgreSQL, ACR, Foundry, or the Container Apps environment.
    Uses one versioned ACR image, managed-identity image pull, Bicep what-if, and public
    HTTPS ingress. Registration is disabled unless explicitly enabled for brief account
    onboarding; always disable it again before recording or publishing the demo URL.
.EXAMPLE
    .\infra\deploy-vibecast-containerapp.ps1 -DatabaseBootstrapVerified
#>
[CmdletBinding()]
param(
    [string]$DeploymentName = 'vibecast-production-prerequisites',
    [string]$ContainerAppDeploymentName = 'vibecast-container-app',
    [string]$ResourceGroup = 'rg-vibecast-prod',
    [string]$AppName = 'ca-vibecast-prod',
    [string]$ImageRepository = 'vibecast-web',
    [string]$SearchResourceGroup = 'foundry-rg',
    [string]$SearchServiceName = 'vibecast-search',
    [string]$FoundryProjectEndpoint = $env:VIBECAST_FOUNDRY_PROJECT_ENDPOINT,
    [string]$ChatModelDeployment = $env:VIBECAST_CHAT_MODEL_DEPLOYMENT,
    [string]$ImageModelDeployment = $env:VIBECAST_IMAGE_MODEL_DEPLOYMENT,
    [string]$SpeechEndpoint = $env:VIBECAST_SPEECH_ENDPOINT,
    [string]$ContentUnderstandingEndpoint = $env:VIBECAST_CONTENT_UNDERSTANDING_ENDPOINT,
    [switch]$DatabaseBootstrapVerified,
    [switch]$EnableRegistrationTemporarily,
    [string]$DemoAccountEmail,
    [switch]$SkipTests
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Description
    )
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $output = @()
    $exitCode = 1
    try {
        $output = @(& $Command @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
    $response = (($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine).Trim()
    if ($exitCode -ne 0) { throw "$Description failed ($exitCode). $response" }
    return $response
}

function Get-RequiredOutput {
    param([psobject]$Outputs, [string]$Name)
    $property = $Outputs.PSObject.Properties[$Name]
    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value.value)) {
        throw "Prerequisite deployment output '$Name' was not found."
    }
    return [string]$property.Value.value
}

function Resolve-Setting {
    param([string]$Value, [string]$Prompt, [switch]$RequireHttps)
    $resolved = $Value
    if ([string]::IsNullOrWhiteSpace($resolved)) {
        $resolved = Read-Host $Prompt
    }
    if ([string]::IsNullOrWhiteSpace($resolved)) {
        throw "$Prompt is required. Pass the corresponding parameter."
    }
    $resolved = $resolved.Trim()
    if ($RequireHttps) {
        $parsed = $null
        if (-not [uri]::TryCreate($resolved,[System.UriKind]::Absolute,[ref]$parsed) -or
            $parsed.Scheme -ne 'https') {
            throw "$Prompt must be an absolute HTTPS URI."
        }
    }
    return $resolved
}

function Add-DeploymentParameter {
    param([hashtable]$Target, [string]$Name, $Value)
    $Target[$Name] = @{ value = $Value }
}

foreach ($exe in @('az','dotnet','git')) {
    if ($null -eq (Get-Command $exe -ErrorAction SilentlyContinue)) {
        throw "Required command '$exe' is unavailable."
    }
}
if (-not $DatabaseBootstrapVerified) {
    throw 'Database has not been confirmed ready. Run infra/bootstrap-production-database.ps1 first, then rerun with -DatabaseBootstrapVerified.'
}
$account = (Invoke-Checked -Command 'az' -Arguments @('account','show','--output','json') -Description 'reading active Azure account') | ConvertFrom-Json
if ($EnableRegistrationTemporarily -and
    ($DemoAccountEmail -notmatch '^[^\s@]+@[^\s@]+\.[^\s@]+$')) {
    throw 'Temporary onboarding requires -DemoAccountEmail with the designated nonproduction email. No passwords are accepted.'
}
if (-not $EnableRegistrationTemporarily) { $DemoAccountEmail = '' }
$outputs = (Invoke-Checked -Command 'az' -Arguments @(
    'deployment','sub','show','--name',$DeploymentName,
    '--query','properties.outputs','--output','json'
) -Description 'reading prerequisite deployment outputs') | ConvertFrom-Json
$actualGroup = Get-RequiredOutput -Outputs $outputs -Name 'productionResourceGroupName'
if ($actualGroup -ne $ResourceGroup) {
    throw "Prerequisites are in '$actualGroup', not requested '$ResourceGroup'."
}
$environmentId = Get-RequiredOutput -Outputs $outputs -Name 'containerAppsEnvironmentId'
$identityId = Get-RequiredOutput -Outputs $outputs -Name 'runtimeIdentityId'
$identityClientId = Get-RequiredOutput -Outputs $outputs -Name 'runtimeIdentityClientId'
$identityPrincipalId = Get-RequiredOutput -Outputs $outputs -Name 'runtimeIdentityPrincipalId'
$registryId = Get-RequiredOutput -Outputs $outputs -Name 'registryId'
$registryLoginServer = Get-RequiredOutput -Outputs $outputs -Name 'registryLoginServer'
$postgresConnectionString = Get-RequiredOutput -Outputs $outputs -Name 'postgresConnectionString'
$storageServiceUri = Get-RequiredOutput -Outputs $outputs -Name 'storageServiceUri'
$dataProtectionBlobUri = Get-RequiredOutput -Outputs $outputs -Name 'dataProtectionBlobUri'
$dataProtectionKeyIdentifier = Get-RequiredOutput -Outputs $outputs -Name 'dataProtectionKeyIdentifier'
$registryName = $registryId.Split('/')[-1]
if ($environmentId.Split('/')[-1] -ne 'cae-vibecast-prod' -or
    $identityId.Split('/')[-1] -ne 'id-vibecast-prod') {
    throw 'This checkpoint requires the existing cae-vibecast-prod environment and id-vibecast-prod identity.'
}
if ($postgresConnectionString -match '(?i)(password|pwd)\s*=') {
    throw 'Production PostgreSQL must use Entra authentication, without a password in deployment parameters.'
}

$environment = (Invoke-Checked -Command 'az' -Arguments @(
    'resource','show','--ids',$environmentId,'--output','json'
) -Description 'verifying the existing Container Apps environment') | ConvertFrom-Json
$null = Invoke-Checked -Command 'az' -Arguments @('resource','show','--ids',$identityId,'--query','id','--output','tsv') -Description 'verifying runtime managed identity'
$null = Invoke-Checked -Command 'az' -Arguments @('resource','show','--ids',$registryId,'--query','id','--output','tsv') -Description 'verifying the Azure Container Registry'
$null = Invoke-Checked -Command 'az' -Arguments @('resource','show','--ids',(Get-RequiredOutput -Outputs $outputs -Name 'postgresServerId'),'--query','id','--output','tsv') -Description 'verifying PostgreSQL resource'

$acrPullAssignments = Invoke-Checked -Command 'az' -Arguments @(
    'role','assignment','list','--assignee-object-id',$identityPrincipalId,
    '--scope',$registryId,'--include-inherited',
    '--query',"[?roleDefinitionName=='AcrPull'] | length(@)",'--output','tsv'
) -Description 'verifying managed-identity AcrPull'
if ($acrPullAssignments.Trim() -ne '1' -and [int]$acrPullAssignments.Trim() -lt 1) {
    throw 'The runtime identity lacks AcrPull on the configured ACR. Run infra/configure-production-identity.ps1.'
}
$search = (Invoke-Checked -Command 'az' -Arguments @(
    'search','service','show','--name',$SearchServiceName,
    '--resource-group',$SearchResourceGroup,'--output','json'
) -Description 'locating the existing Azure AI Search service') | ConvertFrom-Json
if ($search.name -ne $SearchServiceName) { throw 'Search resource name mismatch.' }
$searchEndpoint = "https://$SearchServiceName.search.windows.net"
$FoundryProjectEndpoint = Resolve-Setting -Value $FoundryProjectEndpoint -Prompt 'Foundry project endpoint' -RequireHttps
$ChatModelDeployment = Resolve-Setting -Value $ChatModelDeployment -Prompt 'Foundry chat deployment name'
$ImageModelDeployment = Resolve-Setting -Value $ImageModelDeployment -Prompt 'Foundry image deployment name'
$SpeechEndpoint = Resolve-Setting -Value $SpeechEndpoint -Prompt 'Foundry Speech endpoint' -RequireHttps
$ContentUnderstandingEndpoint = Resolve-Setting -Value $ContentUnderstandingEndpoint -Prompt 'Content Understanding endpoint' -RequireHttps

Push-Location $repoRoot
try {
    $branch = (Invoke-Checked -Command 'git' -Arguments @('branch','--show-current') -Description 'reading current teaching branch').Trim()
    if ($branch -ne 'section-09-container-apps-complete') {
        throw "Switch to section-09-container-apps-complete before building a tagged release (current: $branch)."
    }
    $dirty = Invoke-Checked -Command 'git' -Arguments @('status','--porcelain') -Description 'checking clean release worktree'
    if (-not [string]::IsNullOrWhiteSpace($dirty)) {
        throw 'Release worktree is dirty; commit or discard changes before publishing a commit-tagged image.'
    }
    $commit = (Invoke-Checked -Command 'git' -Arguments @('rev-parse','HEAD') -Description 'identifying current release commit').Trim()
    if ($commit -notmatch '^[a-fA-F0-9]{40}$') { throw 'Invalid Git release commit SHA.' }
    $tag = $commit.Substring(0,12).ToLowerInvariant()
    $imageName = "$registryLoginServer/$ImageRepository" + ':' + $tag

    Write-Host "Subscription    : $($account.name)"
    Write-Host "Resource group  : $ResourceGroup"
    Write-Host "Managed env     : $($environment.name)"
    Write-Host "Container App   : $AppName"
    Write-Host "Image           : $imageName"
    Write-Host "Min/Max replicas: 0/1"
    Write-Host "Public ingress  : HTTPS"
    Write-Host "Registration    : $([bool]$EnableRegistrationTemporarily)"
    if ($EnableRegistrationTemporarily) {
        Write-Warning 'TEMPORARY REGISTRATION ENABLED: only the designated email is accepted. This is not email ownership verification; disable immediately after onboarding.'
    }

    $null = Invoke-Checked -Command 'az' -Arguments @('bicep','lint','--file','infra/container-app.bicep') -Description 'Bicep lint'
    $null = Invoke-Checked -Command 'az' -Arguments @('bicep','build','--file','infra/container-app.bicep','--outfile',(Join-Path ([System.IO.Path]::GetTempPath()) 'vibecast-containerapp-validated.json')) -Description 'Bicep compilation'
    $null = Invoke-Checked -Command 'dotnet' -Arguments @('build','VibeCast.sln','--configuration','Release') -Description 'Release build'
    if (-not $SkipTests) {
        $null = Invoke-Checked -Command 'dotnet' -Arguments @('test','VibeCast.sln','--configuration','Release','--no-build') -Description 'Release test suite'
    }

    $parameters = @{
        '$schema'='https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
        contentVersion='1.0.0.0'
        parameters=@{}
    }
    $p = $parameters.parameters
    Add-DeploymentParameter $p 'appName' $AppName
    Add-DeploymentParameter $p 'location' ([string]$environment.location)
    Add-DeploymentParameter $p 'environmentId' $environmentId
    Add-DeploymentParameter $p 'runtimeIdentityId' $identityId
    Add-DeploymentParameter $p 'runtimeIdentityClientId' $identityClientId
    Add-DeploymentParameter $p 'registryLoginServer' $registryLoginServer
    Add-DeploymentParameter $p 'image' $imageName
    Add-DeploymentParameter $p 'postgresConnectionString' $postgresConnectionString
    Add-DeploymentParameter $p 'storageServiceUri' $storageServiceUri
    Add-DeploymentParameter $p 'dataProtectionBlobUri' $dataProtectionBlobUri
    Add-DeploymentParameter $p 'dataProtectionKeyIdentifier' $dataProtectionKeyIdentifier
    Add-DeploymentParameter $p 'foundryProjectEndpoint' $FoundryProjectEndpoint
    Add-DeploymentParameter $p 'foundryChatModelDeployment' $ChatModelDeployment
    Add-DeploymentParameter $p 'foundryImageModelDeployment' $ImageModelDeployment
    Add-DeploymentParameter $p 'speechEndpoint' $SpeechEndpoint
    Add-DeploymentParameter $p 'contentUnderstandingEndpoint' $ContentUnderstandingEndpoint
    Add-DeploymentParameter $p 'knowledgeSearchEndpoint' $searchEndpoint
    Add-DeploymentParameter $p 'registrationEnabled' ([bool]$EnableRegistrationTemporarily)
    Add-DeploymentParameter $p 'registrationAllowedEmail' $DemoAccountEmail
    $parameterFile = Join-Path ([System.IO.Path]::GetTempPath()) ('vibecast-aca-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        [System.IO.File]::WriteAllText(
            $parameterFile,
            ($parameters | ConvertTo-Json -Depth 12),
            (New-Object System.Text.UTF8Encoding($false))
        )
        $paramArg = '@' + $parameterFile
        if ((Read-Host 'Build/publish this commit-tagged image to ACR? Type YES') -cne 'YES') {
            throw 'Image publish cancelled.'
        }

        # ACR Tasks builds from this clean checkout without embedding developer secrets.
        $existingRepo = Invoke-Checked -Command 'az' -Arguments @(
            'acr','repository','list','--name',$registryName,
            '--query',"[?@=='$ImageRepository'] | length(@)",'--output','tsv'
        ) -Description 'checking whether the image repository exists'
        $existingTag = '0'
        if ($existingRepo.Trim() -ne '0') {
            $existingTag = Invoke-Checked -Command 'az' -Arguments @(
                'acr','repository','show-tags','--name',$registryName,
                '--repository',$ImageRepository,
                '--query',"[?@=='$tag'] | length(@)",'--output','tsv'
            ) -Description 'checking existing release image tags'
        }
        if ($existingTag.Trim() -eq '0') {
            $null = Invoke-Checked -Command 'az' -Arguments @(
                'acr','build','--registry',$registryName,
                '--image',($ImageRepository + ':' + $tag),
                '--file','Dockerfile','.'
            ) -Description 'building and publishing VibeCast image'
        }
        else {
            Write-Host 'Release tag already exists in ACR; skipping redundant build.'
        }

        # Pin the deployed image to content, even if a registry tag is later changed.
        $digest = Invoke-Checked -Command 'az' -Arguments @(
            'acr','repository','show','--name',$registryName,
            '--image',($ImageRepository + ':' + $tag),'--query','digest','--output','tsv'
        ) -Description 'resolving the versioned image digest'
        if ($digest.Trim() -notmatch '^sha256:[a-f0-9]{64}$') { throw 'ACR returned an invalid image digest.' }
        $p['image'] = @{ value = "$registryLoginServer/$ImageRepository@$($digest.Trim())" }
        [System.IO.File]::WriteAllText($parameterFile, ($parameters | ConvertTo-Json -Depth 12), (New-Object System.Text.UTF8Encoding($false)))
        Write-Host "Deployment image: $($p['image'].value) (release tag $tag)"

        Write-Host 'Review the Container App changes (what-if)...'
        $whatIf = Invoke-Checked -Command 'az' -Arguments @(
            'deployment','group','what-if','--resource-group',$ResourceGroup,
            '--name',$ContainerAppDeploymentName,
            '--template-file','infra/container-app.bicep','--parameters',$paramArg
        ) -Description 'Container App Bicep what-if'
        Write-Host $whatIf
        if ((Read-Host 'Deploy this Container App configuration? Type YES') -cne 'YES') {
            throw 'Container App deployment cancelled.'
        }
        $result = Invoke-Checked -Command 'az' -Arguments @(
            'deployment','group','create','--resource-group',$ResourceGroup,
            '--name',$ContainerAppDeploymentName,
            '--template-file','infra/container-app.bicep','--parameters',$paramArg,
            '--output','json'
        ) -Description 'deploying Azure Container App'
        $deployment = $result | ConvertFrom-Json
        if ([string]$deployment.properties.provisioningState -ne 'Succeeded') {
            throw 'Azure did not report successful Container App provisioning.'
        }
    }
    finally {
        Remove-Item -LiteralPath $parameterFile -Force -ErrorAction SilentlyContinue
    }

    $fqdn = (Invoke-Checked -Command 'az' -Arguments @(
        'containerapp','show','--resource-group',$ResourceGroup,'--name',$AppName,
        '--query','properties.configuration.ingress.fqdn','--output','tsv'
    ) -Description 'reading Container App ingress URL').Trim()
    if ([string]::IsNullOrWhiteSpace($fqdn)) {
        throw 'Azure did not assign an ingress FQDN.'
    }
    $url = "https://$fqdn"
    Write-Host "Public HTTPS URL: $url"

    $healthOk = $false
    for ($attempt=1; $attempt -le 15; $attempt++) {
        try {
            $health = Invoke-WebRequest -Uri "$url/health" -UseBasicParsing -TimeoutSec 30
            if ($health.StatusCode -eq 200) {
                $healthOk = $true
                break
            }
        }
        catch {
            Write-Warning "Awaiting cold-start/readiness ($attempt/15): $($_.Exception.Message)"
        }
        Start-Sleep -Seconds 8
    }
    if (-not $healthOk) {
        Write-Warning "View container logs: az containerapp logs show -g $ResourceGroup -n $AppName --type console"
        throw 'Container App was deployed but the public /health endpoint did not return 200.'
    }
    $login = Invoke-WebRequest -Uri "$url/Account/Login" -UseBasicParsing -TimeoutSec 45
    if ($login.StatusCode -ne 200 -or $login.Content -notmatch 'Sign in') {
        throw 'Public login page did not render successfully.'
    }
    Write-Host 'PASS: versioned image, public health endpoint, and login page are available.' -ForegroundColor Green
    Write-Host "Now sign in at $url and create/reload one episode to verify PostgreSQL persistence."
    Write-Host 'Account sign-in and episode CRUD require an actual authenticated browser session; they are not automatically certified by this script.'
    if ($EnableRegistrationTemporarily) {
        Write-Warning 'Once the intended demo user registers, rerun this script without -EnableRegistrationTemporarily and confirm /Account/Register returns 404.'
    }
}
finally { Pop-Location }
