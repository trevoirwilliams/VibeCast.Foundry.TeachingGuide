param(
    [string]$AppHostProject = "VibeCast.AppHost/VibeCast.AppHost.csproj",
    [string]$StorageAccountName = "vibecastkb90423471",
    [string]$KnowledgeContainerName = "vibecast-knowledge",
    [string]$ServicePrincipalName = "sp-vibecast-local-dev"
)

$ErrorActionPreference = "Stop"

function Assert-AzureCliSucceeded {
    param([string]$Step)

    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI failed during: $Step"
    }
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
        --query "[?roleDefinitionName=='$Role'] | length(@)" `
        --output tsv

    Assert-AzureCliSucceeded "checking role '$Role'"

    if ([int]$count -gt 0) {
        Write-Host "Role '$Role' already exists at $Scope."
        return
    }

    az role assignment create `
        --assignee-object-id $PrincipalId `
        --assignee-principal-type ServicePrincipal `
        --role $Role `
        --scope $Scope `
        --output none

    Assert-AzureCliSucceeded "assigning role '$Role'"
}

az account show --output none
Assert-AzureCliSucceeded "Azure account validation"

$tenantId = az account show `
    --query tenantId `
    --output tsv

$storageJson = az resource list `
    --resource-type Microsoft.Storage/storageAccounts `
    --query "[?name=='$StorageAccountName'] | [0]" `
    --output json

Assert-AzureCliSucceeded "resolving Storage Account"

$storage = $storageJson | ConvertFrom-Json

if ($null -eq $storage) {
    throw "Storage Account '$StorageAccountName' was not found."
}

$secretLines = dotnet user-secrets list `
    --project $AppHostProject

if ($LASTEXITCODE -ne 0) {
    throw "Unable to read AppHost user secrets."
}

$searchEndpointLine =
    $secretLines |
    Where-Object {
        $_ -match `
            "^Parameters:knowledge-search-endpoint\s*="
    } |
    Select-Object -First 1

if ($null -eq $searchEndpointLine) {
    throw `
        "Parameters:knowledge-search-endpoint is missing from AppHost user secrets."
}

$searchEndpoint =
    ($searchEndpointLine -split "\s*=\s*", 2)[1]
        .Trim()

$searchUri = [Uri]$searchEndpoint

$searchServiceName = $searchUri.Host.Split(".")[0]

$searchJson = az resource list `
    --resource-type Microsoft.Search/searchServices `
    --query "[?name=='$searchServiceName'] | [0]" `
    --output json

Assert-AzureCliSucceeded "resolving Azure AI Search"

$search = $searchJson | ConvertFrom-Json

if ($null -eq $search) {
    throw "Azure AI Search service '$searchServiceName' was not found."
}

$existingServicePrincipalJson =
    az ad sp list `
        --display-name $ServicePrincipalName `
        --query "[0]" `
        --output json

Assert-AzureCliSucceeded "checking local-development service principal"

$existingServicePrincipal = $existingServicePrincipalJson | ConvertFrom-Json

if ($null -eq $existingServicePrincipal) {
    $createdJson =
        az ad sp create-for-rbac `
            --name $ServicePrincipalName `
            --skip-assignment `
            --output json

    Assert-AzureCliSucceeded "creating local-development service principal"

    $created = $createdJson | ConvertFrom-Json

    $clientId = $created.appId

    $clientSecret = $created.password

    $principalId =
        az ad sp show `
            --id $clientId `
            --query id `
            --output tsv
}
else {
    $clientId = $existingServicePrincipal.appId

    $principalId = $existingServicePrincipal.id

    $clientSecret =
        az ad app credential reset `
            --id $clientId `
            --append `
            --display-name "vibecast-local-development" `
            --query password `
            --output tsv

    Assert-AzureCliSucceeded "rotating local-development credential"
}

$knowledgeContainerScope = "$($storage.id)/blobServices/default/containers/$KnowledgeContainerName"

Ensure-RoleAssignment `
    -PrincipalId $principalId `
    -Role "Storage Blob Data Contributor" `
    -Scope $knowledgeContainerScope

Ensure-RoleAssignment `
    -PrincipalId $principalId `
    -Role "Search Index Data Reader" `
    -Scope $search.id

dotnet user-secrets set "Parameters:azure-tenant-id" $tenantId `
    --project $AppHostProject

dotnet user-secrets set "Parameters:azure-client-id" $clientId `
    --project $AppHostProject

dotnet user-secrets set "Parameters:azure-client-secret" $clientSecret `
    --project $AppHostProject

if ($LASTEXITCODE -ne 0) {
    throw "Failed to update AppHost user secrets."
}

Write-Host ""
Write-Host "Local development identity configured."

Write-Host "Client secret was stored in AppHost user secrets and was not written to source control."
