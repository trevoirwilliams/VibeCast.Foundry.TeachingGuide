param(
    [string]$DeploymentName =
        "vibecast-production-prerequisites",

    [string]$FoundryResourceId = "",

    [string]$SpeechResourceId = "",

    [string]$ContentUnderstandingResourceId = "",

    [string]$SearchServiceId = ""
)

$ErrorActionPreference = "Stop"

function Assert-AzureCliSucceeded {
    param([string]$Step)

    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI failed during: $Step"
    }
}

function Read-RequiredResourceId {
    param(
        [string]$Value,
        [string]$Prompt
    )

    if (-not [string]::IsNullOrWhiteSpace(
            $Value))
    {
        return $Value
    }

    $resolved =
        Read-Host $Prompt

    if ([string]::IsNullOrWhiteSpace(
            $resolved))
    {
        throw "A resource ID is required."
    }

    return $resolved
}

function Ensure-RoleAssignment {
    param(
        [string]$PrincipalId,
        [string]$Role,
        [string]$Scope
    )

    $count = az role assignment list `
        --assignee $PrincipalId `
        --scope $Scope `
        --query `
            "[?roleDefinitionName=='$Role'] | length(@)" `
        --output tsv

    Assert-AzureCliSucceeded `
        "checking role '$Role'"

    if ([int]$count -gt 0) {
        Write-Host `
            "Role '$Role' already assigned."
        return
    }

    az role assignment create `
        --assignee-object-id $PrincipalId `
        --assignee-principal-type ServicePrincipal `
        --role $Role `
        --scope $Scope `
        --output none

    Assert-AzureCliSucceeded `
        "assigning role '$Role'"
}

az account show --output none
Assert-AzureCliSucceeded `
    "Azure account validation"

$FoundryResourceId =
    Read-RequiredResourceId `
        -Value $FoundryResourceId `
        -Prompt `
            "Enter the Microsoft Foundry resource ID used for model inference"

$SpeechResourceId =
    Read-RequiredResourceId `
        -Value $SpeechResourceId `
        -Prompt `
            "Enter the Speech resource ID"

$ContentUnderstandingResourceId =
    Read-RequiredResourceId `
        -Value $ContentUnderstandingResourceId `
        -Prompt `
            "Enter the Content Understanding Foundry resource ID"

$SearchServiceId =
    Read-RequiredResourceId `
        -Value $SearchServiceId `
        -Prompt `
            "Enter the Azure AI Search service resource ID"

$outputsJson =
    az deployment sub show `
        --name $DeploymentName `
        --query properties.outputs `
        --output json

Assert-AzureCliSucceeded `
    "reading prerequisite deployment"

$outputs =
    $outputsJson |
    ConvertFrom-Json

$resourceGroup =
    $outputs.productionResourceGroupName.value

$runtimePrincipalId =
    $outputs.runtimeIdentityPrincipalId.value

$runtimeIdentityId =
    $outputs.runtimeIdentityId.value

$registryId =
    $outputs.registryId.value

$storageAccountId =
    $outputs.storageAccountId.value

$keyVaultId =
    $outputs.keyVaultId.value

$postgresServerId =
    $outputs.postgresServerId.value

$postgresServerName =
    $postgresServerId.Split("/")[-1]

$mediaContainer =
    $outputs.mediaContainerName.value

$knowledgeContainer =
    $outputs.knowledgeContainerName.value

$dataProtectionContainer =
    $outputs.dataProtectionContainerName.value

$postgresState =
    az postgres flexible-server show `
        --resource-group $resourceGroup `
        --name $postgresServerName `
        --query state `
        --output tsv

Assert-AzureCliSucceeded `
    "checking PostgreSQL state"

if ($postgresState -ne "Ready") {
    throw `
        "PostgreSQL must report Ready before identity configuration. Current state: $postgresState"
}

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
    @{
        Role = "Key Vault Crypto User"
        Scope = "$keyVaultId/keys/data-protection"
    },
    @{
        Role = "Cognitive Services OpenAI User"
        Scope = $FoundryResourceId
    },
    @{
        Role = "Cognitive Services Speech User"
        Scope = $SpeechResourceId
    },
    @{
        Role = "Cognitive Services Content Understanding Reader"
        Scope = $ContentUnderstandingResourceId
    },
    @{
        Role = "Search Index Data Reader"
        Scope = $SearchServiceId
    }
)

foreach ($assignment in $roleAssignments) {
    Ensure-RoleAssignment `
        -PrincipalId $runtimePrincipalId `
        -Role $assignment.Role `
        -Scope $assignment.Scope
}

$currentUserJson =
    az ad signed-in-user show `
        --output json

Assert-AzureCliSucceeded `
    "reading signed-in Entra user"

$currentUser =
    $currentUserJson |
    ConvertFrom-Json

az postgres flexible-server `
    microsoft-entra-admin create `
    --resource-group $resourceGroup `
    --server-name $postgresServerName `
    --display-name `
        $currentUser.userPrincipalName `
    --object-id $currentUser.id `
    --type User `
    --output none

Assert-AzureCliSucceeded `
    "configuring PostgreSQL Entra administrator"

Write-Host ""
Write-Host `
    "Production Azure RBAC assignments are configured."

Write-Host `
    "PostgreSQL Entra administrator: $($currentUser.userPrincipalName)"

Write-Host `
    "Runtime identity resource ID: $runtimeIdentityId"

Write-Host `
    "Runtime identity principal ID: $runtimePrincipalId"

Write-Host `
    "PostgreSQL server: $postgresServerName"
