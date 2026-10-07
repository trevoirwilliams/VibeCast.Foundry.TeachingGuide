<#
.SYNOPSIS
    Configures the low-privilege Microsoft Entra identity used by the
    Dockerized VibeCast application during local development.

.DESCRIPTION
    The Docker container cannot automatically inherit the Azure CLI or
    Visual Studio identity from the Windows host.

    This script creates or reuses a dedicated service principal and grants
    only the permissions needed by the Azure-backed knowledge workflow:

      - Storage Blob Data Contributor on vibecast-knowledge
      - Search Index Data Reader on the existing Azure AI Search service

    The service principal credentials are then stored in the AppHost
    user-secrets store as:

      Parameters:azure-tenant-id
      Parameters:azure-client-id
      Parameters:azure-client-secret

    AppHost injects these into the Docker container as:

      AZURE_TENANT_ID
      AZURE_CLIENT_ID
      AZURE_CLIENT_SECRET

    DefaultAzureCredential can then resolve EnvironmentCredential from
    inside the local Docker container.

    Rerunning the script normally reuses the client secret already stored
    in AppHost user secrets. Use -RotateCredential to deliberately create
    a new client secret.

.NOTES
    ASP.NET Core User Secrets prevent accidental source-control commits,
    but they are not an encrypted enterprise secret store. This identity
    is for Development only.

.EXAMPLE
    .\infra\configure-local-development-identity.ps1

.EXAMPLE
    .\infra\configure-local-development-identity.ps1 -RotateCredential
#>

[CmdletBinding()]
param(
    [string]$AppHostProject =
        "VibeCast.AppHost/VibeCast.AppHost.csproj",

    [string]$StorageAccountName =
        "vibecastkb90423471",

    [string]$KnowledgeContainerName =
        "vibecast-knowledge",

    [string]$ServicePrincipalName =
        "sp-vibecast-local-dev",

    [switch]$RotateCredential
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Command {
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found."
    }
}

function Invoke-AzJson {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$Step
    )

    $output = & az @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI failed during: $Step"
    }

    $text = ($output -join [Environment]::NewLine).Trim()

    if ([string]::IsNullOrWhiteSpace($text) -or $text -eq "null") {
        return $null
    }

    return $text | ConvertFrom-Json
}

function Invoke-AzText {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$Step
    )

    $output = & az @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI failed during: $Step"
    }

    return ($output -join [Environment]::NewLine).Trim()
}

function Invoke-AzNoOutput {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$Step
    )

    & az @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI failed during: $Step"
    }
}

function Invoke-DotNetNoOutput {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$Step
    )

    $output = & dotnet @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw ".NET CLI failed during: $Step"
    }

    if ($null -ne $output) {
        $output | Out-Host
    }
}

function Get-AppHostSecrets {
    param(
        [Parameter(Mandatory)]
        [string]$Project
    )

    $lines = & dotnet user-secrets list `
        --project $Project

    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read AppHost user secrets."
    }

    $secrets = @{}

    foreach ($line in @($lines)) {
        $text = [string]$line

        if ($text -match "^(?<key>[^=]+?)\s*=\s*(?<value>.*)$") {
            $key = $Matches["key"].Trim()
            $value = $Matches["value"]

            $secrets[$key] = $value
        }
    }

    return $secrets
}

function Get-ServicePrincipalWithRetry {
    param(
        [Parameter(Mandatory)]
        [string]$ClientId
    )

    for ($attempt = 1; $attempt -le 12; $attempt++) {
        $output = & az ad sp show `
            --id $ClientId `
            --output json 2>$null

        if ($LASTEXITCODE -eq 0) {
            $text = ($output -join [Environment]::NewLine).Trim()

            if (-not [string]::IsNullOrWhiteSpace($text)) {
                return $text | ConvertFrom-Json
            }
        }

        Start-Sleep -Seconds 5
    }

    throw "The new service principal did not become available in Microsoft Entra ID in time."
}

function Ensure-RoleAssignment {
    param(
        [Parameter(Mandatory)]
        [string]$PrincipalId,

        [Parameter(Mandatory)]
        [string]$Role,

        [Parameter(Mandatory)]
        [string]$Scope
    )

    $countText = Invoke-AzText `
        -Arguments @(
            "role", "assignment", "list",
            "--assignee-object-id", $PrincipalId,
            "--scope", $Scope,
            "--query",
            "[?roleDefinitionName=='$Role'] | length(@)",
            "--output", "tsv"
        ) `
        -Step "checking role '$Role'"

    $count = 0

    if (-not [string]::IsNullOrWhiteSpace($countText)) {
        $count = [int]$countText
    }

    if ($count -gt 0) {
        Write-Host "Role '$Role' is already assigned at:"
        Write-Host "  $Scope"
        return
    }

    Write-Host "Assigning '$Role'..."

    Invoke-AzNoOutput `
        -Arguments @(
            "role", "assignment", "create",
            "--assignee-object-id", $PrincipalId,
            "--assignee-principal-type", "ServicePrincipal",
            "--role", $Role,
            "--scope", $Scope,
            "--output", "none"
        ) `
        -Step "assigning role '$Role'"
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " VibeCast - Configure Local Development Identity" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

Assert-Command -Name "az"
Assert-Command -Name "dotnet"

if (-not (Test-Path $AppHostProject)) {
    throw "AppHost project was not found: $AppHostProject"
}

# ---------------------------------------------------------------------------
# 1. Verify Azure authentication.
# ---------------------------------------------------------------------------

$account = Invoke-AzJson `
    -Arguments @(
        "account", "show",
        "--output", "json"
    ) `
    -Step "Azure account validation"

if ($null -eq $account) {
    throw "No active Azure account was found. Run 'az login' first."
}

$tenantId = [string]$account.tenantId

Write-Host "Subscription: $($account.name)"
Write-Host "Tenant:       $tenantId"

# ---------------------------------------------------------------------------
# 2. Read the existing AppHost user secrets.
# ---------------------------------------------------------------------------
# The knowledge-search endpoint already exists there from the previous
# VibeCast lessons. We use it to discover the Search service without
# hard-coding another Azure resource name.
# ---------------------------------------------------------------------------

$appHostSecrets = Get-AppHostSecrets `
    -Project $AppHostProject

$searchEndpointKey =
    "Parameters:knowledge-search-endpoint"

if (-not $appHostSecrets.ContainsKey($searchEndpointKey)) {
    throw "$searchEndpointKey is missing from AppHost user secrets."
}

$searchEndpoint =
    ([string]$appHostSecrets[$searchEndpointKey]).Trim()

if ([string]::IsNullOrWhiteSpace($searchEndpoint)) {
    throw "$searchEndpointKey is empty."
}

$searchUri = $null

if (-not [Uri]::TryCreate(
        $searchEndpoint,
        [UriKind]::Absolute,
        [ref]$searchUri))
{
    throw "The configured knowledge-search endpoint is not a valid absolute URI."
}

if ($searchUri.Scheme -ne "https") {
    throw "The knowledge-search endpoint must use HTTPS."
}

$hostParts = $searchUri.Host.Split(".")

if ($hostParts.Count -lt 2) {
    throw "The Azure AI Search service name could not be derived from '$searchEndpoint'."
}

$searchServiceName = $hostParts[0]

Write-Host ""
Write-Host "Azure AI Search service: $searchServiceName"

# ---------------------------------------------------------------------------
# 3. Resolve the existing Storage Account.
# ---------------------------------------------------------------------------

$storage = Invoke-AzJson `
    -Arguments @(
        "resource", "list",
        "--resource-type",
        "Microsoft.Storage/storageAccounts",
        "--query",
        "[?name=='$StorageAccountName'] | [0]",
        "--output", "json"
    ) `
    -Step "resolving Storage Account"

if ($null -eq $storage) {
    throw "Storage Account '$StorageAccountName' was not found in the active subscription."
}

$knowledgeContainerScope =
    "$($storage.id)/blobServices/default/containers/$KnowledgeContainerName"

# ---------------------------------------------------------------------------
# 4. Resolve the Azure AI Search resource.
# ---------------------------------------------------------------------------

$search = Invoke-AzJson `
    -Arguments @(
        "resource", "list",
        "--resource-type",
        "Microsoft.Search/searchServices",
        "--query",
        "[?name=='$searchServiceName'] | [0]",
        "--output", "json"
    ) `
    -Step "resolving Azure AI Search"

if ($null -eq $search) {
    throw "Azure AI Search service '$searchServiceName' was not found in the active subscription."
}

# ---------------------------------------------------------------------------
# 5. Find or create the dedicated Development service principal.
# ---------------------------------------------------------------------------

$servicePrincipalResult = Invoke-AzJson `
    -Arguments @(
        "ad", "sp", "list",
        "--display-name", $ServicePrincipalName,
        "--query",
        "[?displayName=='$ServicePrincipalName']",
        "--output", "json"
    ) `
    -Step "checking local-development service principal"

$servicePrincipals = @($servicePrincipalResult)

if ($servicePrincipals.Count -gt 1) {
    throw "More than one service principal named '$ServicePrincipalName' exists. Use a unique service-principal name."
}

$clientSecret = $null

if ($servicePrincipals.Count -eq 0) {
    Write-Host ""
    Write-Host "Creating service principal '$ServicePrincipalName'..." -ForegroundColor Yellow

    # Modern Azure CLI no longer creates a role assignment automatically.
    $created = Invoke-AzJson `
        -Arguments @(
            "ad", "sp", "create-for-rbac",
            "--name", $ServicePrincipalName,
            "--output", "json"
        ) `
        -Step "creating local-development service principal"

    if ($null -eq $created) {
        throw "Service principal creation returned no result."
    }

    $clientId = [string]$created.appId
    $clientSecret = [string]$created.password

    if ([string]::IsNullOrWhiteSpace($clientId) -or
        [string]::IsNullOrWhiteSpace($clientSecret))
    {
        throw "Azure CLI did not return the new service-principal credentials."
    }

    # Entra replication isn't always instantaneous.
    $servicePrincipal =
        Get-ServicePrincipalWithRetry `
            -ClientId $clientId

    $principalId =
        [string]$servicePrincipal.id
}
else {
    $servicePrincipal =
        $servicePrincipals[0]

    $clientId =
        [string]$servicePrincipal.appId

    $principalId =
        [string]$servicePrincipal.id

    $storedClientId = $null
    $storedClientSecret = $null

    if ($appHostSecrets.ContainsKey(
            "Parameters:azure-client-id"))
    {
        $storedClientId =
            ([string]$appHostSecrets[
                "Parameters:azure-client-id"]).Trim()
    }

    if ($appHostSecrets.ContainsKey(
            "Parameters:azure-client-secret"))
    {
        $storedClientSecret =
            [string]$appHostSecrets[
                "Parameters:azure-client-secret"]
    }

    $canReuseStoredCredential =
        -not $RotateCredential.IsPresent -and
        $storedClientId -eq $clientId -and
        -not [string]::IsNullOrWhiteSpace(
            $storedClientSecret)

    if ($canReuseStoredCredential) {
        Write-Host ""
        Write-Host "Reusing the existing Development credential from AppHost user secrets."

        $clientSecret =
            $storedClientSecret
    }
    else {
        Write-Host ""
        Write-Host "Creating a new Development client secret..." -ForegroundColor Yellow

        # Append instead of replacing existing credentials. This avoids
        # breaking another developer who might already be using the same
        # dedicated Development service principal.
        $clientSecret = Invoke-AzText `
            -Arguments @(
                "ad", "sp", "credential", "reset",
                "--id", $principalId,
                "--append",
                "--display-name",
                "vibecast-local-development",
                "--years", "1",
                "--query", "password",
                "--output", "tsv"
            ) `
            -Step "creating local-development service-principal credential"

        if ([string]::IsNullOrWhiteSpace($clientSecret)) {
            throw "Azure CLI did not return a new service-principal secret."
        }
    }
}

if ([string]::IsNullOrWhiteSpace($principalId)) {
    throw "The service-principal object ID could not be resolved."
}

# ---------------------------------------------------------------------------
# 6. Grant only the Development permissions required by the knowledge path.
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "Configuring Development RBAC..." -ForegroundColor Yellow

Ensure-RoleAssignment `
    -PrincipalId $principalId `
    -Role "Storage Blob Data Contributor" `
    -Scope $knowledgeContainerScope

Ensure-RoleAssignment `
    -PrincipalId $principalId `
    -Role "Search Index Data Reader" `
    -Scope $search.id

# ---------------------------------------------------------------------------
# 7. Store the credential in AppHost User Secrets.
# ---------------------------------------------------------------------------
# AppHost later injects these as AZURE_TENANT_ID, AZURE_CLIENT_ID and
# AZURE_CLIENT_SECRET inside the Docker container.
# ---------------------------------------------------------------------------

Invoke-DotNetNoOutput `
    -Arguments @(
        "user-secrets", "set",
        "Parameters:azure-tenant-id",
        $tenantId,
        "--project", $AppHostProject
    ) `
    -Step "saving Azure tenant ID"

Invoke-DotNetNoOutput `
    -Arguments @(
        "user-secrets", "set",
        "Parameters:azure-client-id",
        $clientId,
        "--project", $AppHostProject
    ) `
    -Step "saving Azure client ID"

Invoke-DotNetNoOutput `
    -Arguments @(
        "user-secrets", "set",
        "Parameters:azure-client-secret",
        $clientSecret,
        "--project", $AppHostProject
    ) `
    -Step "saving Azure client secret"

# Avoid retaining the secret variable longer than necessary.
$clientSecret = $null

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host " Local Development identity configured." -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Service principal: $ServicePrincipalName"
Write-Host "Client ID:         $clientId"
Write-Host ""
Write-Host "Granted:"
Write-Host "  Storage Blob Data Contributor -> $KnowledgeContainerName"
Write-Host "  Search Index Data Reader       -> $searchServiceName"
Write-Host ""
Write-Host "The client secret was stored in AppHost user secrets."
Write-Host "It was not written to source control."
Write-Host ""
Write-Host "Azure RBAC can take several minutes to propagate."
