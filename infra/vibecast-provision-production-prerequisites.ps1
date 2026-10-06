<#
.SYNOPSIS
    Provisions and verifies the Azure production prerequisites for VibeCast.

.DESCRIPTION
    This script accompanies the lesson "Provision Production Dependencies with Bicep".

    It:
      1. Signs in to Azure and confirms the active subscription.
      2. Registers the Azure resource providers used by the VibeCast production stack.
      3. Resolves the existing VibeCast Storage Account.
      4. Sets the environment variables consumed by main.prod.bicepparam.
      5. Lints and compiles the Bicep template and parameter file.
      6. Runs an ARM what-if preview.
      7. Pauses for explicit approval before deployment.
      8. Deploys the production prerequisites.
      9. Reads deployment outputs.
     10. Verifies PostgreSQL, Container Apps Environment, Storage containers,
         managed identity, ACR, and the Data Protection Key Vault key.

    This script intentionally DOES NOT:
      - create the PostgreSQL Microsoft Entra administrator;
      - create the VibeCast PostgreSQL runtime principal;
      - assign Azure RBAC roles to id-vibecast-prod;
      - deploy the Container App;
      - run EF Core production migrations;
      - configure Application Insights.

    Those concerns belong to later lessons.

.PREREQUISITES
    - Run from the repository root.
    - Azure CLI installed.
    - Bicep available through Azure CLI.
    - The corrected files exist:
        infra/main.bicep
        infra/main.prod.bicepparam
        infra/modules/*.bicep
    - The active Azure identity has permission to deploy the required resources.

.EXAMPLE
    .\infra\deploy-production-prerequisites.ps1

.EXAMPLE
    .\infra\deploy-production-prerequisites.ps1 `
        -SubscriptionId "<your-subscription-id>"
#>

param(
    # Optional. Use this when the signed-in account has access to more than one subscription.
    [string]$SubscriptionId = "",

    # Existing Storage Account created earlier in the VibeCast course.
    [string]$StorageAccountName = "vibecastkb90423471",

    # Resource group that will contain the new production hosting resources.
    [string]$ProductionResourceGroupName = "rg-vibecast-prod",

    # Stable ARM deployment name used by what-if, deployment, and output queries.
    [string]$DeploymentName = "vibecast-production-prerequisites",

    # Corrected Bicep parameters file.
    [string]$BicepParametersFile = "infra/main.prod.bicepparam",

    # Root Bicep file.
    [string]$BicepFile = "infra/main.bicep"
)

$ErrorActionPreference = "Stop"

function Assert-AzureCliSucceeded {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Step
    )

    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI failed during: $Step"
    }
}

function Get-DeploymentOutputValue {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Outputs,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $property = $Outputs.PSObject.Properties[$Name]

    if ($null -eq $property) {
        return $null
    }

    return $property.Value.value
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " VibeCast - Provision Production Dependencies with Bicep" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# ---------------------------------------------------------------------------
# 1. Verify that the expected infrastructure files exist.
# ---------------------------------------------------------------------------
# Running from the repository root keeps the paths used in the lesson stable.
if (-not (Test-Path $BicepFile)) {
    throw "Bicep file not found: $BicepFile. Run this script from the repository root."
}

if (-not (Test-Path $BicepParametersFile)) {
    throw "Bicep parameters file not found: $BicepParametersFile."
}

# ---------------------------------------------------------------------------
# 2. Sign in to Azure.
# ---------------------------------------------------------------------------
# This authenticates the developer who is deploying the infrastructure.
# No application/runtime identity is configured by this command.
Write-Host "Signing in to Azure..." -ForegroundColor Yellow
az login --output none
Assert-AzureCliSucceeded "Azure sign-in"

# If the student supplied a subscription ID, explicitly select it.
if (-not [string]::IsNullOrWhiteSpace($SubscriptionId)) {
    Write-Host "Selecting subscription $SubscriptionId..." -ForegroundColor Yellow
    az account set --subscription $SubscriptionId
    Assert-AzureCliSucceeded "subscription selection"
}

# Show the active context before creating billable resources.
Write-Host ""
Write-Host "Active Azure context:" -ForegroundColor Green
az account show `
    --query "{Name:name, SubscriptionId:id, TenantId:tenantId}" `
    --output table
Assert-AzureCliSucceeded "reading the active Azure account"

# ---------------------------------------------------------------------------
# 3. Register the Azure resource providers used by this deployment.
# ---------------------------------------------------------------------------
# Provider registration is subscription-scoped. --wait ensures the provider
# is ready before the Bicep deployment tries to create its resource types.
Write-Host ""
Write-Host "Registering required Azure resource providers..." -ForegroundColor Yellow

$providers = @(
    "Microsoft.App",
    "Microsoft.ContainerRegistry",
    "Microsoft.DBforPostgreSQL",
    "Microsoft.KeyVault",
    "Microsoft.ManagedIdentity"
)

foreach ($provider in $providers) {
    Write-Host "  Registering $provider..."
    az provider register `
        --namespace $provider `
        --wait `
        --output none

    Assert-AzureCliSucceeded "registration of $provider"
}

# ---------------------------------------------------------------------------
# 4. Resolve the existing VibeCast Storage Account.
# ---------------------------------------------------------------------------
# The course deliberately REUSES the Storage Account that already hosts the
# curated knowledge container. Bicep adds/maintains the media and Data
# Protection containers rather than creating another Storage Account.
Write-Host ""
Write-Host "Resolving existing Storage Account '$StorageAccountName'..." -ForegroundColor Yellow

$storageJson = az storage account list `
    --query "[?name=='$StorageAccountName'] | [0]" `
    --output json

Assert-AzureCliSucceeded "Storage Account lookup"

$storage = $storageJson | ConvertFrom-Json

if ($null -eq $storage) {
    throw "Storage Account '$StorageAccountName' was not found in the active subscription."
}

# main.prod.bicepparam reads these values with readEnvironmentVariable().
# They are deployment metadata, not application secrets.
$env:VIBECAST_AZURE_LOCATION = $storage.primaryLocation
$env:VIBECAST_STORAGE_RESOURCE_GROUP = $storage.resourceGroup
$env:VIBECAST_STORAGE_SUBSCRIPTION_ID = $storage.id.Split("/")[2]

Write-Host ""
Write-Host "Bicep environment variables:" -ForegroundColor Green

@(
    "VIBECAST_AZURE_LOCATION",
    "VIBECAST_STORAGE_SUBSCRIPTION_ID",
    "VIBECAST_STORAGE_RESOURCE_GROUP"
) | ForEach-Object {
    $value = [Environment]::GetEnvironmentVariable($_, "Process")

    [PSCustomObject]@{
        Variable = $_
        IsSet = -not [string]::IsNullOrWhiteSpace($value)
        Value = $value
    }
} | Format-Table -AutoSize

# ---------------------------------------------------------------------------
# 5. Show the installed Bicep CLI version.
# ---------------------------------------------------------------------------
# If this command reports an old Bicep installation, update it manually with:
#
#     az bicep upgrade
#
# We do not auto-upgrade tooling during a deployment script because toolchain
# upgrades should be a deliberate developer action.
Write-Host ""
Write-Host "Installed Bicep version:" -ForegroundColor Green
az bicep version
Assert-AzureCliSucceeded "Bicep version check"

# ---------------------------------------------------------------------------
# 6. Lint the Bicep template.
# ---------------------------------------------------------------------------
# Linting finds syntax issues and best-practice violations before ARM is
# contacted. The corrected lesson should not report hard-coded Azure DNS
# warnings for core.windows.net or vault.azure.net.
Write-Host ""
Write-Host "Linting Bicep..." -ForegroundColor Yellow

az bicep lint `
    --file $BicepFile

Assert-AzureCliSucceeded "Bicep lint"

# ---------------------------------------------------------------------------
# 7. Compile the root Bicep template.
# ---------------------------------------------------------------------------
# This validates resource declarations, modules, scopes, types, and outputs.
Write-Host ""
Write-Host "Building root Bicep template..." -ForegroundColor Yellow

az bicep build `
    --file $BicepFile

Assert-AzureCliSucceeded "Bicep build"

# ---------------------------------------------------------------------------
# 8. Compile the .bicepparam file independently.
# ---------------------------------------------------------------------------
# This is important: a valid main.bicep does NOT prove that main.prod.bicepparam
# is valid. build-params verifies parameter names, expressions, and the
# environment variables read by readEnvironmentVariable().
Write-Host ""
Write-Host "Validating Bicep parameter file..." -ForegroundColor Yellow

az bicep build-params `
    --file $BicepParametersFile `
    --stdout |
    Out-Null

Assert-AzureCliSucceeded "Bicep parameter build"

# ---------------------------------------------------------------------------
# 9. Preview the ARM changes with what-if.
# ---------------------------------------------------------------------------
# what-if shows what Azure plans to create/change before the deployment occurs.
# On a fresh environment, expect the production resource group, UAMI, ACR,
# PostgreSQL Flexible Server/database/firewall rule, Key Vault/key,
# Container Apps Environment, and Blob container resources.
#
# The existing Microsoft Foundry resource, Azure AI Search service,
# Storage Account, and vibecast-knowledge container must NOT be recreated.
Write-Host ""
Write-Host "Running Azure Resource Manager what-if..." -ForegroundColor Yellow
Write-Host ""

az deployment sub what-if `
    --name $DeploymentName `
    --location $env:VIBECAST_AZURE_LOCATION `
    --parameters $BicepParametersFile

Assert-AzureCliSucceeded "subscription what-if"

# ---------------------------------------------------------------------------
# 10. Require explicit approval before creating or changing Azure resources.
# ---------------------------------------------------------------------------
Write-Host ""
$confirmation = Read-Host "Review the what-if output above. Deploy these changes? Type YES to continue"

if ($confirmation -cne "YES") {
    Write-Host "Deployment cancelled. No deployment command was executed." -ForegroundColor Yellow
    exit 0
}

# ---------------------------------------------------------------------------
# 11. Deploy the production prerequisites.
# ---------------------------------------------------------------------------
# Bicep is declarative. If some resources already exist from an earlier run,
# ARM converges them toward the declared state instead of blindly recreating
# the entire environment.
Write-Host ""
Write-Host "Deploying VibeCast production prerequisites..." -ForegroundColor Yellow

az deployment sub create `
    --name $DeploymentName `
    --location $env:VIBECAST_AZURE_LOCATION `
    --parameters $BicepParametersFile `
    --output none

Assert-AzureCliSucceeded "subscription deployment"

Write-Host "Deployment completed." -ForegroundColor Green

# ---------------------------------------------------------------------------
# 12. Read the deployment outputs once and reuse them for verification.
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Reading deployment outputs..." -ForegroundColor Yellow

$outputsJson = az deployment sub show `
    --name $DeploymentName `
    --query properties.outputs `
    --output json

Assert-AzureCliSucceeded "reading deployment outputs"

$outputs = $outputsJson | ConvertFrom-Json

# Display the complete set of outputs for the learner.
$outputs | ConvertTo-Json -Depth 10

# Read the primary IDs.
$productionResourceGroup = Get-DeploymentOutputValue $outputs "productionResourceGroupName"

$runtimeIdentityId = Get-DeploymentOutputValue $outputs "runtimeIdentityId"
$runtimeIdentityName = Get-DeploymentOutputValue $outputs "runtimeIdentityName"

$registryId = Get-DeploymentOutputValue $outputs "registryId"
$registryName = Get-DeploymentOutputValue $outputs "registryName"

$postgresServerId = Get-DeploymentOutputValue $outputs "postgresServerId"
$postgresServerName = Get-DeploymentOutputValue $outputs "postgresServerName"

$keyVaultId = Get-DeploymentOutputValue $outputs "keyVaultId"
$keyVaultName = Get-DeploymentOutputValue $outputs "keyVaultName"

$containerAppsEnvironmentId = Get-DeploymentOutputValue $outputs "containerAppsEnvironmentId"
$containerAppsEnvironmentName = Get-DeploymentOutputValue $outputs "containerAppsEnvironmentName"

$storageAccountId = Get-DeploymentOutputValue $outputs "storageAccountId"
$deployedStorageAccountName = Get-DeploymentOutputValue $outputs "storageAccountName"

$mediaContainerName = Get-DeploymentOutputValue $outputs "mediaContainerName"
$knowledgeContainerName = Get-DeploymentOutputValue $outputs "knowledgeContainerName"
$dataProtectionContainerName = Get-DeploymentOutputValue $outputs "dataProtectionContainerName"

$dataProtectionKeyIdentifier = Get-DeploymentOutputValue $outputs "dataProtectionKeyIdentifier"

# Backward-compatible fallback: derive resource names from IDs if the branch
# being demonstrated does not yet expose the convenience "name" outputs.
if ([string]::IsNullOrWhiteSpace($runtimeIdentityName) -and $runtimeIdentityId) {
    $runtimeIdentityName = $runtimeIdentityId.Split("/")[-1]
}

if ([string]::IsNullOrWhiteSpace($registryName) -and $registryId) {
    $registryName = $registryId.Split("/")[-1]
}

if ([string]::IsNullOrWhiteSpace($postgresServerName) -and $postgresServerId) {
    $postgresServerName = $postgresServerId.Split("/")[-1]
}

if ([string]::IsNullOrWhiteSpace($keyVaultName) -and $keyVaultId) {
    $keyVaultName = $keyVaultId.Split("/")[-1]
}

if ([string]::IsNullOrWhiteSpace($containerAppsEnvironmentName) -and $containerAppsEnvironmentId) {
    $containerAppsEnvironmentName = $containerAppsEnvironmentId.Split("/")[-1]
}

if ([string]::IsNullOrWhiteSpace($deployedStorageAccountName) -and $storageAccountId) {
    $deployedStorageAccountName = $storageAccountId.Split("/")[-1]
}

# ---------------------------------------------------------------------------
# 13. List the application-owned production resources.
# ---------------------------------------------------------------------------
# This gives students a concise view of the resource types produced by Bicep.
Write-Host ""
Write-Host "Resources in '$productionResourceGroup':" -ForegroundColor Green

az resource list `
    --resource-group $productionResourceGroup `
    --query "[].{Name:name, Type:type, Location:location}" `
    --output table

Assert-AzureCliSucceeded "resource-group verification"

# ---------------------------------------------------------------------------
# 14. Verify PostgreSQL reached its service-ready state.
# ---------------------------------------------------------------------------
# ARM resource completion and service readiness are not always identical.
# We intentionally configure the PostgreSQL Entra administrator in the NEXT
# lesson only after the Flexible Server reports Ready.
Write-Host ""
Write-Host "Verifying PostgreSQL server state..." -ForegroundColor Green

az postgres flexible-server show `
    --resource-group $productionResourceGroup `
    --name $postgresServerName `
    --query "{State:state, Version:version, FQDN:fullyQualifiedDomainName}" `
    --output table

Assert-AzureCliSucceeded "PostgreSQL state verification"

# ---------------------------------------------------------------------------
# 15. Verify PostgreSQL authentication policy.
# ---------------------------------------------------------------------------
# The production server should be Entra-only:
#   activeDirectoryAuth = Enabled
#   passwordAuth        = Disabled
#
# No application database password is created in this lesson.
Write-Host ""
Write-Host "Verifying PostgreSQL authentication policy..." -ForegroundColor Green

az postgres flexible-server show `
    --resource-group $productionResourceGroup `
    --name $postgresServerName `
    --query authConfig `
    --output json

Assert-AzureCliSucceeded "PostgreSQL authentication verification"

# ---------------------------------------------------------------------------
# 16. Verify the Container Apps Environment exists.
# ---------------------------------------------------------------------------
# The Container App itself is intentionally NOT created in this lesson.
Write-Host ""
Write-Host "Verifying Container Apps Environment..." -ForegroundColor Green

az containerapp env show `
    --resource-group $productionResourceGroup `
    --name $containerAppsEnvironmentName `
    --query "{Name:name, Location:location, ProvisioningState:properties.provisioningState}" `
    --output table

Assert-AzureCliSucceeded "Container Apps Environment verification"

# ---------------------------------------------------------------------------
# 17. Verify the three Blob container boundaries through ARM.
# ---------------------------------------------------------------------------
# We use ARM resource lookups instead of Blob data-plane commands so this
# verification does not require the deploying developer to also have a
# Storage Blob Data Reader/Contributor role.
Write-Host ""
Write-Host "Verifying Blob container resources..." -ForegroundColor Green

$containers = @(
    $mediaContainerName,
    $knowledgeContainerName,
    $dataProtectionContainerName
)

foreach ($containerName in $containers) {
    $containerResourceId =
        "$storageAccountId/blobServices/default/containers/$containerName"

    az resource show `
        --ids $containerResourceId `
        --api-version 2025-01-01 `
        --query "{Name:name, PublicAccess:properties.publicAccess}" `
        --output table

    Assert-AzureCliSucceeded "verification of Blob container '$containerName'"
}

# ---------------------------------------------------------------------------
# 18. Verify the user-assigned managed identity.
# ---------------------------------------------------------------------------
# The identity should exist and expose both:
#   - clientId: used by application/runtime identity configuration;
#   - principalId: used by Azure RBAC role assignments.
#
# It intentionally has not received its VibeCast permissions yet.
Write-Host ""
Write-Host "Verifying user-assigned managed identity..." -ForegroundColor Green

az identity show `
    --resource-group $productionResourceGroup `
    --name $runtimeIdentityName `
    --query "{Name:name, ClientId:clientId, PrincipalId:principalId}" `
    --output table

Assert-AzureCliSucceeded "managed identity verification"

# ---------------------------------------------------------------------------
# 19. Verify Azure Container Registry.
# ---------------------------------------------------------------------------
# adminUserEnabled must remain false. The next lesson grants AcrPull to the
# managed identity instead of creating a long-lived registry username/password.
Write-Host ""
Write-Host "Verifying Azure Container Registry..." -ForegroundColor Green

az acr show `
    --resource-group $productionResourceGroup `
    --name $registryName `
    --query "{Name:name, LoginServer:loginServer, AdminUserEnabled:adminUserEnabled, Sku:sku.name}" `
    --output table

Assert-AzureCliSucceeded "ACR verification"

# ---------------------------------------------------------------------------
# 20. Verify the Data Protection key through the ARM control plane.
# ---------------------------------------------------------------------------
# Using az resource show here avoids requiring the deploying developer to have
# Key Vault data-plane permissions merely to verify the Bicep-created key.
Write-Host ""
Write-Host "Verifying Data Protection Key Vault key..." -ForegroundColor Green

$keyResourceId = "$keyVaultId/keys/data-protection"

az resource show `
    --ids $keyResourceId `
    --api-version 2024-11-01 `
    --query "{Name:name, KeyType:properties.kty, KeyOps:properties.keyOps}" `
    --output json

Assert-AzureCliSucceeded "Data Protection key verification"

# ---------------------------------------------------------------------------
# 21. Verify that the Data Protection key identifier is version-independent.
# ---------------------------------------------------------------------------
# ASP.NET Core Data Protection should receive:
#
#   https://<vault>.vault.azure.net/keys/data-protection
#
# not a URI containing a specific key version.
Write-Host ""
Write-Host "Data Protection Key Identifier:" -ForegroundColor Green
Write-Host $dataProtectionKeyIdentifier

if ($dataProtectionKeyIdentifier -match "/keys/data-protection/[^/]+$") {
    Write-Warning "The Data Protection key identifier appears to include a key version. The application should use a versionless key URI."
}
else {
    Write-Host "The key identifier is versionless as expected." -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# 22. Final handoff.
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Production prerequisite deployment verified." -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next lesson:" -ForegroundColor Yellow
Write-Host "  Configure Managed Identity and Production Settings"
Write-Host ""
Write-Host "That lesson will:"
Write-Host "  - verify PostgreSQL is Ready;"
Write-Host "  - configure the PostgreSQL Microsoft Entra administrator;"
Write-Host "  - create the VibeCast runtime database principal;"
Write-Host "  - grant least-privilege SQL permissions;"
Write-Host "  - assign Azure RBAC roles to id-vibecast-prod;"
Write-Host "  - configure deterministic production managed identity authentication;"
Write-Host "  - remove production dependence on API keys and passwords."
Write-Host ""
