<#
.SYNOPSIS
    Create the PostgreSQL runtime principal from Azure Cloud Shell

.DESCRIPTION
    Continues AFTER the preceding lessons and scripts have:
      - provisioned the Azure infrastructure through Bicep;
      - configured id-vibecast-prod Azure RBAC roles;
      - added the current developer as PostgreSQL Microsoft Entra admin;
      - configured the dedicated local Docker development service principal.

    - Bootstrap the PostgreSQL runtime Entra principal from PowerShell.
              Uses a short-lived PostgreSQL token, a Docker-hosted psql client,
              and a temporary firewall exception for the workstation's IP.
              Confirms the role maps to the correct Entra OBJECT ID.

.PREREQUISITES
    Run this file FROM THE REPOSITORY ROOT in a PowerShell terminal.
    Required: Azure CLI (logged in to correct subscription), Docker Desktop
    (daemon running), .NET 10 SDK, Git, network access to PostgreSQL port 5432.

    The signed-in Azure user MUST already be an Entra admin for the PostgreSQL
    server, as configured by infra/configure-production-identity.ps1.

    NOTE: The current Bicep topology uses public PostgreSQL networking.
    This script makes a TEMPORARY /32 firewall exception and removes it.
    For a private-only production topology, use a trusted machine inside the
    private network instead of opening a public firewall exception.

.EXAMPLE
    .\infra\vibecast-steps-18-23-identity-readiness.ps1

.EXAMPLE
    # Repeat compilation/review without touching PostgreSQL:
    .\infra\vibecast-steps-18-23-identity-readiness.ps1 -SkipDatabaseBootstrap

.EXAMPLE
    # After bootstrap/build checks, launch AppHost for manual regression tests:
    .\infra\vibecast-steps-18-23-identity-readiness.ps1 -LaunchAppHost

.EXAMPLE
    # If public-IP discovery is blocked by the student's network:
    .\infra\vibecast-steps-18-23-identity-readiness.ps1 -ClientPublicIp '203.0.113.10'
#>

[CmdletBinding()]
param(
    [string]$DeploymentName = 'vibecast-production-prerequisites',
    [string]$ResourceGroupName = 'rg-vibecast-prod',
    [string]$SolutionPath = 'VibeCast.sln',
    [string]$AppHostProject = 'VibeCast.AppHost/VibeCast.AppHost.csproj',
    [string]$PostgresDockerImage = 'postgres:18',
    [string]$ClientPublicIp = '',
    [switch]$SkipDatabaseBootstrap,
    [switch]$SkipBuild,
    [switch]$LaunchAppHost
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# --------------------------- Shared helpers -------------------------------
function Write-Step {
    param([Parameter(Mandatory)][string]$Title)
    Write-Host ''
    Write-Host ('=' * 72) -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ('=' * 72) -ForegroundColor Cyan
}

function Assert-Command {
    param([Parameter(Mandatory)][string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' is unavailable. Install it and rerun."
    }
}

function Invoke-AzJson {
    param([Parameter(Mandatory)][string[]]$Arguments,
          [Parameter(Mandatory)][string]$Description)
    $result = & az @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI failed: $Description"
    }
    $json = ($result -join [Environment]::NewLine).Trim()
    if ([string]::IsNullOrWhiteSpace($json) -or $json -eq 'null') {
        throw "Azure CLI returned no result: $Description"
    }
    return ($json | ConvertFrom-Json)
}

function Invoke-AzText {
    param([Parameter(Mandatory)][string[]]$Arguments,
          [Parameter(Mandatory)][string]$Description)
    $result = & az @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI failed: $Description"
    }
    return ($result -join [Environment]::NewLine).Trim()
}

function Read-DeploymentOutput {
    param([Parameter(Mandatory)][object]$Outputs,
          [Parameter(Mandatory)][string]$Name)
    $property = $Outputs.PSObject.Properties[$Name]
    if ($null -eq $property) {
        throw "Required Bicep deployment output '$Name' is missing."
    }
    $value = [string]$property.Value.value
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Required Bicep deployment output '$Name' is empty."
    }
    return $value
}

function Test-IPv4Address {
    param([Parameter(Mandatory)][string]$Value)
    $address = $null
    if (-not [System.Net.IPAddress]::TryParse($Value, [ref]$address)) {
        return $false
    }
    return $address.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork
}

# Executes SQL through the official PostgreSQL 18 client inside a short-lived
# Docker container. The bearer token is passed via an ENVIRONMENT VARIABLE,
# never as a command-line argument, SQL literal, or source-controlled file.
function Invoke-PostgresSql {
    param([Parameter(Mandatory)][string]$Database,
          [Parameter(Mandatory)][string]$Sql,
          [string]$Description = 'executing PostgreSQL SQL')

    $dockerArgs = @(
        'run', '--rm',
        '--env', 'PGPASSWORD',
        '--env', "PGHOST=$script:postgresHost",
        '--env', 'PGPORT=5432',
        '--env', "PGDATABASE=$Database",
        '--env', "PGUSER=$script:adminUpn",
        '--env', 'PGSSLMODE=require',
        $PostgresDockerImage,
        'psql', '--no-psqlrc', '--no-align', '--tuples-only',
        '--set', 'ON_ERROR_STOP=1',
        '--command', $Sql
    )

    # Capture normal output to facilitate explicit checks after statements.
    # We do not print access tokens, and the SQL never contains credentials.
    $output = & docker @dockerArgs
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL operation failed: $Description"
    }
    return ($output -join [Environment]::NewLine).Trim()
}

# --------------------------- Preliminary checks ---------------------------
Write-Step 'PRE-FLIGHT - Azure account, prerequisite deployment, and tooling'

Assert-Command 'az'
Assert-Command 'dotnet'
Assert-Command 'git'
if (-not $SkipDatabaseBootstrap) { Assert-Command 'docker' }
if (-not (Test-Path $SolutionPath)) {
    throw "Solution '$SolutionPath' was not found. Run from the repository root."
}
if (-not (Test-Path $AppHostProject)) {
    throw "AppHost project '$AppHostProject' was not found."
}

$account = Invoke-AzJson -Arguments @('account','show','--output','json') -Description 'reading Azure account'
Write-Host "Subscription : $($account.name)"
Write-Host "Tenant       : $($account.tenantId)"

$outputs = Invoke-AzJson -Arguments @(
    'deployment','sub','show',
    '--name',$DeploymentName,
    '--query','properties.outputs',
    '--output','json'
) -Description 'reading Bicep deployment outputs'

$deployedResourceGroup = Read-DeploymentOutput $outputs 'productionResourceGroupName'
if ($deployedResourceGroup -ne $ResourceGroupName) {
    throw "Deployment belongs to '$deployedResourceGroup', not expected '$ResourceGroupName'. Check the active subscription/deployment."
}

$script:postgresHost = Read-DeploymentOutput $outputs 'postgresHost'
$postgresServerId = Read-DeploymentOutput $outputs 'postgresServerId'
$postgresServerName = $postgresServerId.Split('/')[-1]
$postgresDatabase = Read-DeploymentOutput $outputs 'postgresDatabase'
$runtimePrincipalId = Read-DeploymentOutput $outputs 'runtimeIdentityPrincipalId'
$runtimeIdentityClientId = Read-DeploymentOutput $outputs 'runtimeIdentityClientId'
$runtimeIdentityId = Read-DeploymentOutput $outputs 'runtimeIdentityId'
$storageUri = Read-DeploymentOutput $outputs 'storageServiceUri'
$mediaContainer = Read-DeploymentOutput $outputs 'mediaContainerName'
$knowledgeContainer = Read-DeploymentOutput $outputs 'knowledgeContainerName'
$dataProtectionUri = Read-DeploymentOutput $outputs 'dataProtectionBlobUri'
$keyIdentifier = Read-DeploymentOutput $outputs 'dataProtectionKeyIdentifier'

# Both inputs originate from Azure, but validate them before interpolation
# into SQL. The database name must be a conventional PostgreSQL identifier.
$parsedGuid = [Guid]::Empty
if (-not [Guid]::TryParse($runtimePrincipalId, [ref]$parsedGuid)) {
    throw 'The runtime principal ID returned by Bicep is not a GUID.'
}
if ($postgresDatabase -cnotmatch '^[a-zA-Z_][a-zA-Z0-9_]*$') {
    throw 'The database name must be a simple PostgreSQL identifier.'
}

# The PostgreSQL role name is fixed by this course and never read from input.
$runtimePgRole = 'id-vibecast-prod'

Write-Host "PostgreSQL   : $script:postgresHost"
Write-Host "Database     : $postgresDatabase"
Write-Host "Runtime UAMI : $runtimePgRole"

Write-Step 'Map the managed identity to a PostgreSQL runtime role'

if ($SkipDatabaseBootstrap) {
    Write-Host 'Skipping PostgreSQL bootstrap as requested.' -ForegroundColor Yellow
}
else {
    # 18A: Confirm service readiness BEFORE adding a firewall rule.
    $server = Invoke-AzJson -Arguments @(
        'postgres','flexible-server','show',
        '--resource-group',$ResourceGroupName,
        '--name',$postgresServerName,
        '--output','json'
    ) -Description 'reading PostgreSQL server status'

    if ([string]$server.state -ne 'Ready') {
        throw "PostgreSQL is '$($server.state)', not Ready. Stop here."
    }

    # The PostgreSQL Entra administrator was configured in Step 17.
    # We deliberately use THAT user rather than making the UAMI an admin.
    $currentUser = Invoke-AzJson -Arguments @(
        'ad','signed-in-user','show','--output','json'
    ) -Description 'reading the signed-in Microsoft Entra user'
    $script:adminUpn = [string]$currentUser.userPrincipalName
    $adminObjectId = [string]$currentUser.id
    if ([string]::IsNullOrWhiteSpace($script:adminUpn)) {
        throw 'A signed-in Microsoft Entra user is required; run az login.'
    }

    $admins = Invoke-AzJson -Arguments @(
        'postgres','flexible-server','microsoft-entra-admin','list',
        '--resource-group',$ResourceGroupName,
        '--server-name',$postgresServerName,
        '--output','json'
    ) -Description 'listing PostgreSQL Entra administrators'

    $isCurrentUserAdmin = $false
    foreach ($entry in @($admins)) {
        if ($null -eq $entry) { continue }
        # Azure CLI versions can return the object ID in name, objectId,
        # or properties.objectId; avoid assuming one response shape.
        $candidateIds = @()
        foreach ($name in @('name', 'objectId')) {
            $property = $entry.PSObject.Properties[$name]
            if ($null -ne $property) { $candidateIds += [string]$property.Value }
        }
        $properties = $entry.PSObject.Properties['properties']
        if ($null -ne $properties -and $null -ne $properties.Value) {
            $nested = $properties.Value.PSObject.Properties['objectId']
            if ($null -ne $nested) { $candidateIds += [string]$nested.Value }
        }
        if ($adminObjectId -in $candidateIds) {
            $isCurrentUserAdmin = $true
            break
        }
    }
    if (-not $isCurrentUserAdmin) {
        throw "Current user '$script:adminUpn' is not listed as a direct PostgreSQL Entra admin. Complete Step 17 or use the configured Entra administrator."
    }

    # 18B: Check that Docker Desktop is running and pull client image BEFORE
    # creating a firewall exception or issuing the short-lived access token.
    & docker info --format '{{.ServerVersion}}' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Docker Desktop is not running.' }

    & docker image inspect $PostgresDockerImage | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Pulling PostgreSQL client image '$PostgresDockerImage'..."
        & docker pull $PostgresDockerImage | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Unable to obtain the PostgreSQL Docker image.' }
    }

    # 18C: Determine the PUBLIC IPv4 address through which this workstation
    # reaches Azure. VPN/corporate NAT users may need to provide -ClientPublicIp.
    if ([string]::IsNullOrWhiteSpace($ClientPublicIp)) {
        $ClientPublicIp = ([string](Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 20)).Trim()
    }
    if (-not (Test-IPv4Address $ClientPublicIp)) {
        throw "'$ClientPublicIp' is not a valid public IPv4 address."
    }

    Write-Host "Workstation public IPv4: $ClientPublicIp"
    Write-Host 'A temporary /32 firewall rule will be created, then deleted.'
    $approval = Read-Host 'Type YES to perform PostgreSQL identity bootstrap'
    if ($approval -cne 'YES') {
        throw 'PostgreSQL identity bootstrap was cancelled before any network change.'
    }

    # Save any pre-existing token variable and restore it afterwards. The
    # temporary token never goes to the command line or a local file.
    $previousPgPassword = [Environment]::GetEnvironmentVariable('PGPASSWORD','Process')
    $firewallRuleName = 'VibeCastBootstrap' + [Guid]::NewGuid().ToString('N').Substring(0,8)
    $firewallCreated = $false

    try {
        # 18D: Public-network teaching topology only. The 0.0.0.0 Azure
        # services rule from the Bicep lesson does NOT allow a home workstation.
        & az postgres flexible-server firewall-rule create `
            --resource-group $ResourceGroupName `
            --server-name $postgresServerName `
            --name $firewallRuleName `
            --start-ip-address $ClientPublicIp `
            --end-ip-address $ClientPublicIp `
            --output none
        if ($LASTEXITCODE -ne 0) {
            throw 'Failed to create the temporary PostgreSQL firewall rule.'
        }
        $firewallCreated = $true
        Write-Host "Created temporary firewall rule: $firewallRuleName"

        # 18E: The token is issued to the signed-in POSTGRESQL ADMIN user,
        # not to the runtime managed identity. psql treats this token as the
        # connection password. Never echo or persist its value.
        $env:PGPASSWORD = Invoke-AzText -Arguments @(
            'account','get-access-token',
            '--resource-type','oss-rdbms',
            '--query','accessToken',
            '--output','tsv'
        ) -Description 'getting an Entra access token for PostgreSQL'

        if ([string]::IsNullOrWhiteSpace($env:PGPASSWORD)) {
            throw 'Azure CLI returned an empty PostgreSQL access token.'
        }

        # 18F: Firewall changes may take a short time to propagate. Retry an
        # innocuous SELECT instead of immediately retrying privileged writes.
        $connected = $false
        for ($attempt = 1; $attempt -le 8; $attempt++) {
            try {
                $probe = Invoke-PostgresSql -Database 'postgres' `
                    -Sql 'SELECT 1;' -Description 'checking PostgreSQL connectivity'
                if ($probe -eq '1') {
                    $connected = $true
                    break
                }
            }
            catch {
                if ($attempt -eq 8) { throw }
            }
            Write-Host "Waiting for PostgreSQL network access ($attempt/8)..."
            Start-Sleep -Seconds 10
        }
        if (-not $connected) {
            throw 'Could not connect to PostgreSQL using the Entra administrator token.'
        }

        # 18G: Determine whether the named PostgreSQL role already exists.
        # A rerun must NOT create a duplicate or silently accept the wrong
        # Microsoft Entra identity mapped to that PostgreSQL role name.
        $roleCount = Invoke-PostgresSql -Database 'postgres' `
            -Sql "SELECT count(*) FROM pg_catalog.pg_roles WHERE rolname = '$runtimePgRole';" `
            -Description 'checking whether the runtime PostgreSQL role exists'

        if ($roleCount -eq '0') {
            $createPrincipalSql = @"
SELECT pg_catalog.pgaadauth_create_principal_with_oid(
    '$runtimePgRole',
    '$runtimePrincipalId',
    'service',
    false,
    false
);
"@
            $createdRoleMessage = Invoke-PostgresSql -Database 'postgres' `
                -Sql $createPrincipalSql -Description 'creating the Entra runtime principal'
            Write-Host $createdRoleMessage
        }
        elseif ($roleCount -eq '1') {
            Write-Host 'The PostgreSQL role already exists; verifying its Entra mapping.'
        }
        else {
            throw "Unexpected PostgreSQL role count: $roleCount"
        }

        # Microsoft's pgaadauth_list_principals(false) exposes the mapping.
        # Enforce exact object ID, service principal type, and non-admin flag.
        $mappingSql = @"
SELECT objectid::text || '|' || principaltype::text || '|' || isadmin::text
FROM pg_catalog.pgaadauth_list_principals(false)
WHERE rolename = '$runtimePgRole';
"@
        $mapping = Invoke-PostgresSql -Database 'postgres' `
            -Sql $mappingSql -Description 'verifying PostgreSQL-Entra role mapping'
        $fields = ([string]$mapping).Split('|')
        if ($fields.Count -ne 3 -or
            $fields[0] -ine $runtimePrincipalId -or
            $fields[1] -ine 'service' -or
            $fields[2] -ne '0') {
            throw "Runtime role '$runtimePgRole' has an unexpected Entra mapping or admin status. Do not grant permissions."
        }
        Write-Host 'Verified runtime role maps to the expected non-admin UAMI.' -ForegroundColor Green

        # 18H: Establish the minimal DATABASE/SCHEMA privileges. We do NOT
        # create the EF Core schema and do NOT grant table or sequence DML.
        # The deployment lesson handles those after the initial migration.
        $connectGrant = 'GRANT CONNECT ON DATABASE "' + $postgresDatabase + '" TO "' + $runtimePgRole + '";'
        Invoke-PostgresSql -Database 'postgres' -Sql $connectGrant `
            -Description 'granting access to the VibeCast database' | Out-Host

        $schemaGrant = 'GRANT USAGE ON SCHEMA public TO "' + $runtimePgRole + '";'
        Invoke-PostgresSql -Database $postgresDatabase -Sql $schemaGrant `
            -Description 'granting usage on the public schema' | Out-Host

        # 18I: Verify actual privileges. Do not interpret a successful SQL
        # statement alone as proof of the expected least-privilege policy.
        $dbAccess = Invoke-PostgresSql -Database $postgresDatabase `
            -Sql "SELECT has_database_privilege('$runtimePgRole', '$postgresDatabase', 'CONNECT');" `
            -Description 'checking database CONNECT privilege'
        $schemaAccess = Invoke-PostgresSql -Database $postgresDatabase `
            -Sql "SELECT has_schema_privilege('$runtimePgRole', 'public', 'USAGE');" `
            -Description 'checking schema USAGE privilege'
        $roleCapabilities = Invoke-PostgresSql -Database 'postgres' `
            -Sql "SELECT rolcreatedb::text || '|' || rolcreaterole::text FROM pg_catalog.pg_roles WHERE rolname = '$runtimePgRole';" `
            -Description 'checking database administrator capabilities'

        if ($dbAccess -ne 't' -or $schemaAccess -ne 't' -or $roleCapabilities -ne 'false|false') {
            throw "Privilege verification failed (CONNECT=$dbAccess, USAGE=$schemaAccess, capabilities=$roleCapabilities)."
        }
        Write-Host 'PostgreSQL identity and minimal grants verified.' -ForegroundColor Green
    }
    finally {
        # 18J: Always restore the previous PGPASSWORD state. Azure tokens
        # are short-lived, but must not remain in the student's shell.
        if ($null -eq $previousPgPassword) {
            Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
        }
        else {
            $env:PGPASSWORD = $previousPgPassword
        }

        # Delete ONLY the uniquely named rule created by this script.
        if ($firewallCreated) {
            & az postgres flexible-server firewall-rule delete `
                --resource-group $ResourceGroupName `
                --server-name $postgresServerName `
                --name $firewallRuleName `
                --yes --output none
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "SECURITY ACTION REQUIRED: Remove temporary PostgreSQL firewall rule '$firewallRuleName' manually."
                Write-Warning "Command: az postgres flexible-server firewall-rule delete --resource-group $ResourceGroupName --server-name $postgresServerName --name $firewallRuleName --yes"
            }
            else {
                Write-Host "Temporary firewall rule '$firewallRuleName' removed." -ForegroundColor Green
            }
        }
    }
}
