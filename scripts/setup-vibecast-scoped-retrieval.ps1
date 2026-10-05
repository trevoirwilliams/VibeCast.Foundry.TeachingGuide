<#
.SYNOPSIS
    One-time VibeCast setup for scoped Foundry IQ retrieval.

.DESCRIPTION
    Keeps the existing Blob knowledge source and managed ingestion pipeline intact.
    Discovers the Search index created by 'vibecast-blob-ks', validates a
    filterable/retrievable field containing the originating Blob URI, creates or
    updates 'vibecast-index-ks', and updates 'vibecast-kb' to reference it.

    Safe to re-run. Does not recreate or modify the managed index, skillset,
    indexer, or Blob knowledge source.

    Uses Azure AI Search REST API 2026-04-01 (GA).
#>

[CmdletBinding()]
param(
    [string]$SearchServiceName,
    [string]$BlobKnowledgeSourceName = "vibecast-blob-ks",
    [string]$IndexKnowledgeSourceName = "vibecast-index-ks",
    [string]$KnowledgeBaseName = "vibecast-kb",
    [string]$SourcePathField,
    [string]$ApiVersion = "2026-04-01"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([Parameter(Mandatory)][string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Assert-Command {
    param([Parameter(Mandatory)][string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found. Install it and rerun this script."
    }
}

function Get-PropertyValue {
    param(
        [Parameter(Mandatory)][object]$Object,
        [Parameter(Mandatory)][string]$PropertyName
    )

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Invoke-SearchRest {
    param(
        [Parameter(Mandatory)][ValidateSet("GET", "POST", "PUT")][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [object]$Body
    )

    $uri = "$script:SearchEndpoint/$($Path.TrimStart('/'))"
    $headers = @{
        Authorization = "Bearer $script:SearchAccessToken"
        Accept        = "application/json"
    }

    if ($Method -eq "PUT") {
        $headers["Prefer"] = "return=representation"
    }

    $parameters = @{
        Method      = $Method
        Uri         = $uri
        Headers     = $headers
        ContentType = "application/json"
    }

    if ($null -ne $Body) {
        $parameters["Body"] = ($Body | ConvertTo-Json -Depth 30)
    }

    try {
        return Invoke-RestMethod @parameters
    }
    catch {
        $details = $_.ErrorDetails.Message
        if ([string]::IsNullOrWhiteSpace($details)) {
            $details = $_.Exception.Message
        }

        throw "Azure AI Search request failed.`nMethod: $Method`nURI: $uri`nDetails: $details"
    }
}

Assert-Command -Name "az"

Write-Step "Sign in to Azure"

$account = $null
try {
    $accountJson = az account show --output json 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($accountJson)) {
        $account = $accountJson | ConvertFrom-Json
    }
}
catch {
    $account = $null
}

if ($null -eq $account) {
    az login | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Azure CLI sign-in failed." }
    $account = (az account show --output json) | ConvertFrom-Json
}

Write-Host "Signed in as: $($account.user.name)"
Write-Host "Subscription: $($account.name)"
Write-Host "Subscription ID: $($account.id)"

if ([string]::IsNullOrWhiteSpace($SearchServiceName)) {
    Write-Step "Choose the Azure AI Search service"

    $searchServicesJson = az resource list `
        --resource-type "Microsoft.Search/searchServices" `
        --query "[].{Name:name,ResourceGroup:resourceGroup,Location:location}" `
        --output json

    if ($LASTEXITCODE -ne 0) { throw "Could not list Azure AI Search services." }

    $searchServices = @($searchServicesJson | ConvertFrom-Json)
    if ($searchServices.Count -gt 0) {
        $searchServices | Format-Table Name, ResourceGroup, Location -AutoSize | Out-Host
    }

    $SearchServiceName = Read-Host "Enter the Azure AI Search service name"
}

if ([string]::IsNullOrWhiteSpace($SearchServiceName)) {
    throw "The Azure AI Search service name is required."
}

$script:SearchEndpoint = "https://$SearchServiceName.search.windows.net"
Write-Host "Search endpoint: $script:SearchEndpoint"

Write-Step "Acquire an Azure AI Search data-plane token"

$script:SearchAccessToken = az account get-access-token `
    --scope "https://search.azure.com/.default" `
    --query accessToken `
    --output tsv

if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($script:SearchAccessToken)) {
    throw "Azure CLI could not acquire an Azure AI Search access token."
}

Write-Step "Read the existing Blob knowledge source"

$blobKnowledgeSource = Invoke-SearchRest `
    -Method GET `
    -Path "knowledgesources/$BlobKnowledgeSourceName?api-version=$ApiVersion"

$kind = Get-PropertyValue -Object $blobKnowledgeSource -PropertyName "kind"
if ($kind -ne "azureBlob") {
    throw "Knowledge source '$BlobKnowledgeSourceName' is '$kind', not 'azureBlob'."
}

$azureBlobParameters = Get-PropertyValue -Object $blobKnowledgeSource -PropertyName "azureBlobParameters"
$createdResources = Get-PropertyValue -Object $azureBlobParameters -PropertyName "createdResources"
$generatedIndexName = Get-PropertyValue -Object $createdResources -PropertyName "index"

if ([string]::IsNullOrWhiteSpace($generatedIndexName)) {
    throw "The Blob knowledge source did not expose its generated Search index."
}

Write-Host "Blob knowledge source: $BlobKnowledgeSourceName"
Write-Host "Generated index:       $generatedIndexName"

Write-Step "Inspect the generated index schema"

$indexDefinition = Invoke-SearchRest `
    -Method GET `
    -Path "indexes/$generatedIndexName?api-version=$ApiVersion"

$fields = @($indexDefinition.fields)
$fields |
    Select-Object name,type,searchable,filterable,retrievable |
    Format-Table -AutoSize |
    Out-Host

$eligibleFields = @(
    $fields |
        Where-Object {
            $_.type -eq "Edm.String" -and
            $_.filterable -eq $true -and
            $_.retrievable -eq $true
        }
)

if ($eligibleFields.Count -eq 0) {
    throw @"
The generated index has no Edm.String field that is both filterable and retrievable.
Scoped selected-document retrieval cannot be configured safely.

Do not use prompt instructions or post-retrieval filtering as a workaround.
"@
}

Write-Host "Candidate string fields that are filterable + retrievable:" -ForegroundColor Yellow
$eligibleFields | Select-Object name,type,filterable,retrievable | Format-Table -AutoSize | Out-Host

if ([string]::IsNullOrWhiteSpace($SourcePathField)) {
    $SourcePathField = Read-Host "Enter the field that contains the FULL originating Blob URI"
}

$selectedField = $fields | Where-Object { $_.name -eq $SourcePathField } | Select-Object -First 1
if ($null -eq $selectedField) { throw "Field '$SourcePathField' does not exist." }
if ($selectedField.type -ne "Edm.String") { throw "Field '$SourcePathField' must be Edm.String." }
if ($selectedField.filterable -ne $true) { throw "Field '$SourcePathField' is not filterable." }
if ($selectedField.retrievable -ne $true) { throw "Field '$SourcePathField' is not retrievable." }

Write-Step "Verify that '$SourcePathField' contains full Blob URIs"

$sampleSearchBody = @{
    search = "*"
    select = $SourcePathField
    top = 5
}

$sampleResults = Invoke-SearchRest `
    -Method POST `
    -Path "indexes/$generatedIndexName/docs/search?api-version=$ApiVersion" `
    -Body $sampleSearchBody

if ($null -eq $sampleResults.value -or @($sampleResults.value).Count -eq 0) {
    throw "The generated index returned no documents. Confirm ingestion completed successfully."
}

$sampleValues = @(
    $sampleResults.value |
        ForEach-Object { Get-PropertyValue -Object $_ -PropertyName $SourcePathField } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique
)

if ($sampleValues.Count -eq 0) {
    throw "Field '$SourcePathField' is empty in the sampled documents."
}

Write-Host "Sample values:" -ForegroundColor Yellow
$sampleValues | ForEach-Object { Write-Host "  $_" }

foreach ($value in $sampleValues) {
    $uri = $null
    $isUri = [Uri]::TryCreate([string]$value, [UriKind]::Absolute, [ref]$uri)
    if (-not $isUri -or $uri.Scheme -ne "https") {
        throw "Field '$SourcePathField' does not consistently contain absolute HTTPS URIs."
    }
}

$confirmation = Read-Host "Do these values identify the VibeCast source Blobs? Type YES to continue"
if ($confirmation -ne "YES") {
    throw "Setup stopped. Rerun and choose the correct Blob URI field."
}

Write-Step "Resolve the semantic configuration"

$semanticConfigurationName = $null
if ($null -ne $indexDefinition.semantic) {
    $semanticConfigurationName = Get-PropertyValue `
        -Object $indexDefinition.semantic `
        -PropertyName "defaultConfiguration"

    if ([string]::IsNullOrWhiteSpace($semanticConfigurationName)) {
        $configurations = @(Get-PropertyValue -Object $indexDefinition.semantic -PropertyName "configurations")
        if ($configurations.Count -gt 0) {
            $semanticConfigurationName = $configurations[0].name
        }
    }
}

if ([string]::IsNullOrWhiteSpace($semanticConfigurationName)) {
    throw "The generated index does not expose a semantic configuration. API 2026-04-01 requires one for this knowledge source."
}

Write-Host "Semantic configuration: $semanticConfigurationName"

Write-Step "Create or update '$IndexKnowledgeSourceName'"

$indexKnowledgeSourceBody = @{
    name        = $IndexKnowledgeSourceName
    kind        = "searchIndex"
    description = "Scoped retrieval facade over the VibeCast managed knowledge index."
    encryptionKey = $null
    searchIndexParameters = @{
        searchIndexName           = $generatedIndexName
        semanticConfigurationName = $semanticConfigurationName
        sourceDataFields = @(
            @{ name = $SourcePathField }
        )
    }
}

$indexKnowledgeSource = Invoke-SearchRest `
    -Method PUT `
    -Path "knowledgesources/$IndexKnowledgeSourceName?api-version=$ApiVersion" `
    -Body $indexKnowledgeSourceBody

Write-Host "Configured knowledge source: $($indexKnowledgeSource.name)"

Write-Step "Create or update '$KnowledgeBaseName'"

$knowledgeBaseBody = @{
    name        = $KnowledgeBaseName
    description = "Retrieves scoped evidence from the VibeCast managed knowledge index."
    knowledgeSources = @(
        @{ name = $IndexKnowledgeSourceName }
    )
    encryptionKey = $null
}

$knowledgeBase = Invoke-SearchRest `
    -Method PUT `
    -Path "knowledgebases/$KnowledgeBaseName?api-version=$ApiVersion" `
    -Body $knowledgeBaseBody

Write-Host "Configured knowledge base: $($knowledgeBase.name)"

Write-Step "Verify persisted configuration"

$verifiedKnowledgeSource = Invoke-SearchRest `
    -Method GET `
    -Path "knowledgesources/$IndexKnowledgeSourceName?api-version=$ApiVersion"

$verifiedKnowledgeBase = Invoke-SearchRest `
    -Method GET `
    -Path "knowledgebases/$KnowledgeBaseName?api-version=$ApiVersion"

if ($verifiedKnowledgeSource.kind -ne "searchIndex") {
    throw "Verification failed: '$IndexKnowledgeSourceName' is not a Search Index knowledge source."
}

if ($verifiedKnowledgeSource.searchIndexParameters.searchIndexName -ne $generatedIndexName) {
    throw "Verification failed: '$IndexKnowledgeSourceName' points to the wrong index."
}

$verifiedKbSources = @($verifiedKnowledgeBase.knowledgeSources | ForEach-Object { $_.name })
if ($IndexKnowledgeSourceName -notin $verifiedKbSources) {
    throw "Verification failed: '$KnowledgeBaseName' does not reference '$IndexKnowledgeSourceName'."
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "VibeCast scoped retrieval setup completed successfully." -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Search endpoint:         $script:SearchEndpoint"
Write-Host "Managed index:           $generatedIndexName"
Write-Host "Blob ingestion source:   $BlobKnowledgeSourceName"
Write-Host "Scoped retrieval source: $IndexKnowledgeSourceName"
Write-Host "Knowledge base:          $KnowledgeBaseName"
Write-Host "Source-path field:       $SourcePathField"
Write-Host "Semantic configuration:  $semanticConfigurationName"

Write-Host ""
Write-Host "Add these values to VibeCast user secrets:" -ForegroundColor Yellow
Write-Host ""
Write-Host "dotnet user-secrets set `"KnowledgeRetrieval:SearchEndpoint`" `"$script:SearchEndpoint`" --project .\src\VibeCast.Web"
Write-Host "dotnet user-secrets set `"KnowledgeRetrieval:KnowledgeBaseName`" `"$KnowledgeBaseName`" --project .\src\VibeCast.Web"
Write-Host "dotnet user-secrets set `"KnowledgeRetrieval:KnowledgeSourceName`" `"$IndexKnowledgeSourceName`" --project .\src\VibeCast.Web"
Write-Host "dotnet user-secrets set `"KnowledgeRetrieval:SourcePathField`" `"$SourcePathField`" --project .\src\VibeCast.Web"

Write-Host ""
Write-Host "This Azure configuration is persistent." -ForegroundColor Cyan
Write-Host "Do not rerun it for each upload, user, app restart, or retrieval request."
Write-Host "Rerun it only if the Search service/index/knowledge-source configuration changes."
