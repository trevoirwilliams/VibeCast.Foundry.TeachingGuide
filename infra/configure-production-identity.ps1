<#
.SYNOPSIS
    Configures Azure authorization for the VibeCast production
    user-assigned managed identity.

.DESCRIPTION
    Reads the resources created by the prerequisite Bicep deployment and
    grants id-vibecast-prod the least-privilege Azure roles required by
    the runtime application.

    It also configures the currently signed-in Azure user as a Microsoft
    Entra administrator for Azure Database for PostgreSQL Flexible Server.

    This script DOES NOT:
      - create the Container App;
      - attach the identity to the Container App;
      - create the PostgreSQL runtime database role;
      - apply EF Core migrations;
      - grant runtime table permissions.

    Those activities occur in subsequent steps.

.PARAMETER FoundryResourceId
    Resource ID of the Microsoft Foundry / Cognitive Services account
    used by VibeCast for model inference.

.PARAMETER SpeechResourceId
    Resource ID of the Cognitive Services account used for Speech.
    This can be the same resource ID as FoundryResourceId.

.PARAMETER ContentUnderstandingResourceId
    Resource ID of the Cognitive Services / Foundry resource used for
    Content Understanding. This can be the same resource ID as
    FoundryResourceId.

.PARAMETER SearchServiceId
    Resource ID of the Azure AI Search service.

.EXAMPLE
    .\infra\configure-production-identity.ps1

.EXAMPLE
    .\infra\configure-production-identity.ps1 `
        -FoundryResourceId "/subscriptions/.../providers/Microsoft.CognitiveServices/accounts/..." `
        -SpeechResourceId "/subscriptions/.../providers/Microsoft.CognitiveServices/accounts/..." `
        -ContentUnderstandingResourceId "/subscriptions/.../providers/Microsoft.CognitiveServices/accounts/..." `
        -SearchServiceId "/subscriptions/.../providers/Microsoft.Search/searchServices/..."
#>

[CmdletBinding()]
param(
    [string]$DeploymentName =
        "vibecast-production-prerequisites",

    [string]$FoundryResourceId = "",

    [string]$SpeechResourceId = "",

    [string]$ContentUnderstandingResourceId = "",

    [string]$SearchServiceId = ""
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

    if ([string]::IsNullOrWhiteSpace($text) -or
        $text -eq "null")
    {
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

function Read-RequiredResourceId {
    param(
        [string]$Value,

        [Parameter(Mandatory)]
        [string]$Prompt
    )

    $resolved = $Value

    if ([string]::IsNullOrWhiteSpace($resolved)) {
        $resolved = Read-Host $Prompt
    }

    if ([string]::IsNullOrWhiteSpace($resolved)) {
        throw "A resource ID is required."
    }

    return $resolved.Trim()
}

function Resolve-AzureResource {
    param(
        [Parameter(Mandatory)]
        [string]$ResourceId,

        [Parameter(Mandatory)]
        [string]$ExpectedType,

        [Parameter(Mandatory)]
        [string]$DisplayName
    )

    $resource = Invoke-AzJson `
        -Arguments @(
            "resource", "show",
            "--ids", $ResourceId,
            "--output", "json"
        ) `
        -Step "resolving $DisplayName"

    if ($null -eq $resource) {
        throw "$DisplayName could not be resolved."
    }

    if ([string]$resource.type -ne $ExpectedType) {
        throw "$DisplayName must be a '$ExpectedType' resource. Received '$($resource.type)'."
    }

    return $resource
}

function Get-RequiredDeploymentOutput {
    param(
        [Parameter(Mandatory)]
        [psobject]$Outputs,

        [Parameter(Mandatory)]
        [string]$Name
    )

    $property =
        $Outputs.PSObject.Properties[$Name]

    if ($null -eq $property) {
        throw "Deployment output '$Name' was not found."
    }

    $value =
        $property.Value.value

    if ([string]::IsNullOrWhiteSpace(
            [string]$value))
    {
        throw "Deployment output '$Name' is empty."
    }

    return [string]$value
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

    if (-not [string]::IsNullOrWhiteSpace(
            $countText))
    {
        $count =
            [int]$countText
    }

    if ($count -gt 0) {
        Write-Host "Already assigned: $Role"
        Write-Host "  Scope: $Scope"
        return
    }

    Write-Host "Assigning: $Role"

    Invoke-AzNoOutput `
        -Arguments @(
            "role", "assignment", "create",
            "--assignee-object-id", $PrincipalId,
            "--assignee-principal-type",
            "ServicePrincipal",
            "--role", $Role,
            "--scope", $Scope,
            "--output", "none"
        ) `
        -Step "assigning role '$Role'"
}

function Test-PostgresAdministratorExists {
    param(
        [Parameter(Mandatory)]
        [string]$ResourceGroup,

        [Parameter(Mandatory)]
        [string]$ServerName,

        [Parameter(Mandatory)]
        [string]$ObjectId
    )

    $administrators = @(
        Invoke-AzJson `
            -Arguments @(
                "postgres", "flexible-server",
                "microsoft-entra-admin", "list",
                "--resource-group", $ResourceGroup,
                "--server-name", $ServerName,
                "--output", "json"
            ) `
            -Step "listing PostgreSQL Microsoft Entra administrators"
    )

    foreach ($administrator in $administrators) {
        if ($null -eq $administrator) {
            continue
        }

        $candidateObjectId = $null

        $objectIdProperty =
            $administrator.PSObject.Properties[
                "objectId"]

        if ($null -ne $objectIdProperty) {
            $candidateObjectId =
                [string]$objectIdProperty.Value
        }

        if ([string]::IsNullOrWhiteSpace(
                $candidateObjectId))
        {
            $propertiesProperty =
                $administrator.PSObject.Properties[
                    "properties"]

            if ($null -ne $propertiesProperty -and
                $null -ne $propertiesProperty.Value)
            {
                $nestedObjectId =
                    $propertiesProperty.Value
                        .PSObject
                        .Properties["objectId"]

                if ($null -ne $nestedObjectId) {
                    $candidateObjectId =
                        [string]$nestedObjectId.Value
                }
            }
        }

        if ($candidateObjectId -eq $ObjectId) {
            return $true
        }
    }

    return $false
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " VibeCast - Configure Production Identity" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

Assert-Command -Name "az"

# ---------------------------------------------------------------------------
# 1. Verify Azure authentication and read the prerequisite deployment.
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

Write-Host "Subscription: $($account.name)"
Write-Host "Tenant:       $($account.tenantId)"

$outputs = Invoke-AzJson `
    -Arguments @(
        "deployment", "sub", "show",
        "--name", $DeploymentName,
        "--query", "properties.outputs",
        "--output", "json"
    ) `
    -Step "reading prerequisite deployment"

if ($null -eq $outputs) {
    throw "Deployment '$DeploymentName' has no outputs."
}

# ---------------------------------------------------------------------------
# 2. Resolve the resources provisioned in the preceding Bicep lesson.
# ---------------------------------------------------------------------------

$resourceGroup =
    Get-RequiredDeploymentOutput `
        -Outputs $outputs `
        -Name "productionResourceGroupName"

$runtimePrincipalId =
    Get-RequiredDeploymentOutput `
        -Outputs $outputs `
        -Name "runtimeIdentityPrincipalId"

$runtimeIdentityId =
    Get-RequiredDeploymentOutput `
        -Outputs $outputs `
        -Name "runtimeIdentityId"

$registryId =
    Get-RequiredDeploymentOutput `
        -Outputs $outputs `
        -Name "registryId"

$storageAccountId =
    Get-RequiredDeploymentOutput `
        -Outputs $outputs `
        -Name "storageAccountId"

$keyVaultId =
    Get-RequiredDeploymentOutput `
        -Outputs $outputs `
        -Name "keyVaultId"

$postgresServerId =
    Get-RequiredDeploymentOutput `
        -Outputs $outputs `
        -Name "postgresServerId"

$mediaContainer =
    Get-RequiredDeploymentOutput `
        -Outputs $outputs `
        -Name "mediaContainerName"

$knowledgeContainer =
    Get-RequiredDeploymentOutput `
        -Outputs $outputs `
        -Name "knowledgeContainerName"

$dataProtectionContainer =
    Get-RequiredDeploymentOutput `
        -Outputs $outputs `
        -Name "dataProtectionContainerName"

$postgresServerName =
    $postgresServerId.Split("/")[-1]

# ---------------------------------------------------------------------------
# 3. Validate the four existing shared Azure resources.
# ---------------------------------------------------------------------------

$FoundryResourceId =
    Read-RequiredResourceId `
        -Value $FoundryResourceId `
        -Prompt "Enter the Microsoft Foundry resource ID used for model inference"

$SpeechResourceId =
    Read-RequiredResourceId `
        -Value $SpeechResourceId `
        -Prompt "Enter the Speech resource ID"

$ContentUnderstandingResourceId =
    Read-RequiredResourceId `
        -Value $ContentUnderstandingResourceId `
        -Prompt "Enter the Content Understanding resource ID"

$SearchServiceId =
    Read-RequiredResourceId `
        -Value $SearchServiceId `
        -Prompt "Enter the Azure AI Search service resource ID"

$foundryResource =
    Resolve-AzureResource `
        -ResourceId $FoundryResourceId `
        -ExpectedType "Microsoft.CognitiveServices/accounts" `
        -DisplayName "Microsoft Foundry resource"

$speechResource =
    Resolve-AzureResource `
        -ResourceId $SpeechResourceId `
        -ExpectedType "Microsoft.CognitiveServices/accounts" `
        -DisplayName "Speech resource"

$contentUnderstandingResource =
    Resolve-AzureResource `
        -ResourceId $ContentUnderstandingResourceId `
        -ExpectedType "Microsoft.CognitiveServices/accounts" `
        -DisplayName "Content Understanding resource"

$searchResource =
    Resolve-AzureResource `
        -ResourceId $SearchServiceId `
        -ExpectedType "Microsoft.Search/searchServices" `
        -DisplayName "Azure AI Search service"

Write-Host ""
Write-Host "Resolved shared resources:" -ForegroundColor Green
Write-Host "  Foundry:               $($foundryResource.name)"
Write-Host "  Speech:                $($speechResource.name)"
Write-Host "  Content Understanding: $($contentUnderstandingResource.name)"
Write-Host "  Azure AI Search:       $($searchResource.name)"

# ---------------------------------------------------------------------------
# 4. Verify PostgreSQL is service-ready.
# ---------------------------------------------------------------------------

$postgresState = Invoke-AzText `
    -Arguments @(
        "postgres", "flexible-server", "show",
        "--resource-group", $resourceGroup,
        "--name", $postgresServerName,
        "--query", "state",
        "--output", "tsv"
    ) `
    -Step "checking PostgreSQL state"

if ($postgresState -ne "Ready") {
    throw "PostgreSQL must report Ready before identity configuration. Current state: $postgresState"
}

Write-Host ""
Write-Host "PostgreSQL state: Ready" -ForegroundColor Green

# ---------------------------------------------------------------------------
# 5. Grant Azure runtime permissions.
# ---------------------------------------------------------------------------

$roleAssignments = @(
    @{
        Role = "AcrPull"
        Scope = $registryId
    },
    @{
        Role = "Storage Blob Data Contributor"
        Scope = "$storageAccountId/blobServices/default/containers/$mediaContainer"
    },
    @{
        Role = "Storage Blob Data Contributor"
        Scope = "$storageAccountId/blobServices/default/containers/$knowledgeContainer"
    },
    @{
        Role = "Storage Blob Data Contributor"
        Scope = "$storageAccountId/blobServices/default/containers/$dataProtectionContainer"
    },

    # This Key Vault belongs to the VibeCast Production application.
    # Microsoft recommends vault-level application/environment RBAC rather
    # than individual-key role assignments for this normal case.
    @{
        Role = "Key Vault Crypto User"
        Scope = $keyVaultId
    },
    @{
        Role = "Cognitive Services OpenAI User"
        Scope = $foundryResource.id
    },
    @{
        Role = "Cognitive Services Speech User"
        Scope = $speechResource.id
    },
    @{
        Role = "Cognitive Services Content Understanding Reader"
        Scope = $contentUnderstandingResource.id
    },
    @{
        Role = "Search Index Data Reader"
        Scope = $searchResource.id
    }
)

Write-Host ""
Write-Host "Configuring Production RBAC..." -ForegroundColor Yellow

foreach ($assignment in $roleAssignments) {
    Ensure-RoleAssignment `
        -PrincipalId $runtimePrincipalId `
        -Role $assignment.Role `
        -Scope $assignment.Scope
}

# ---------------------------------------------------------------------------
# 6. Configure the signed-in developer as the PostgreSQL Entra administrator.
# ---------------------------------------------------------------------------
# This administrator is needed to perform the one-time database identity
# bootstrap in the next step.
#
# The VibeCast runtime identity itself is NOT made an administrator.
# ---------------------------------------------------------------------------

$currentUser = Invoke-AzJson `
    -Arguments @(
        "ad", "signed-in-user", "show",
        "--output", "json"
    ) `
    -Step "reading signed-in Entra user"

if ($null -eq $currentUser) {
    throw "The current Azure CLI session does not represent a Microsoft Entra user."
}

$currentUserObjectId =
    [string]$currentUser.id

$currentUserPrincipalName =
    [string]$currentUser.userPrincipalName

if ([string]::IsNullOrWhiteSpace(
        $currentUserObjectId) -or
    [string]::IsNullOrWhiteSpace(
        $currentUserPrincipalName))
{
    throw "The current Entra user's object ID or user principal name could not be resolved."
}

$administratorExists =
    Test-PostgresAdministratorExists `
        -ResourceGroup $resourceGroup `
        -ServerName $postgresServerName `
        -ObjectId $currentUserObjectId

if ($administratorExists) {
    Write-Host ""
    Write-Host "PostgreSQL Entra administrator already exists for:"
    Write-Host "  $currentUserPrincipalName"
}
else {
    Write-Host ""
    Write-Host "Creating PostgreSQL Microsoft Entra administrator..." -ForegroundColor Yellow

    Invoke-AzNoOutput `
        -Arguments @(
            "postgres", "flexible-server",
            "microsoft-entra-admin", "create",
            "--resource-group", $resourceGroup,
            "--server-name", $postgresServerName,
            "--display-name", $currentUserPrincipalName,
            "--object-id", $currentUserObjectId,
            "--type", "User",
            "--output", "none"
        ) `
        -Step "configuring PostgreSQL Entra administrator"

    Invoke-AzNoOutput `
        -Arguments @(
            "postgres", "flexible-server",
            "microsoft-entra-admin", "wait",
            "--resource-group", $resourceGroup,
            "--server-name", $postgresServerName,
            "--object-id", $currentUserObjectId,
            "--created",
            "--interval", "10",
            "--timeout", "600",
            "--output", "none"
        ) `
        -Step "waiting for PostgreSQL Entra administrator"
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host " Production identity configuration completed." -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Runtime identity resource ID:"
Write-Host "  $runtimeIdentityId"
Write-Host ""
Write-Host "Runtime principal ID:"
Write-Host "  $runtimePrincipalId"
Write-Host ""
Write-Host "PostgreSQL server:"
Write-Host "  $postgresServerName"
Write-Host ""
Write-Host "PostgreSQL Entra administrator:"
Write-Host "  $currentUserPrincipalName"
Write-Host ""
Write-Host "Azure RBAC changes can take several minutes to propagate."
Write-Host ""
Write-Host "Next:"
Write-Host "  Create the PostgreSQL runtime principal for id-vibecast-prod."
Write-Host "  Do not grant schema migration privileges to that runtime identity."
