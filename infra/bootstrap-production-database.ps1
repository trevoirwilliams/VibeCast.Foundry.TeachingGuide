<#
.SYNOPSIS
    Initialize VibeCast's production PostgreSQL schema and runtime DML grants.
.DESCRIPTION
    Run once before the first Container Apps deployment, then safely rerun to verify.
    Uses the signed-in PostgreSQL Microsoft Entra administrator for schema creation.
    Uses the official postgres:18 client in Docker and streams SQL through stdin,
    preserving double-quoted PostgreSQL identifiers on Windows PowerShell 5.1.
    Never supplies database passwords or installs migrations inside the web image.
#>
[CmdletBinding()]
param(
    [string]$DeploymentName = 'vibecast-production-prerequisites',
    [string]$ExpectedResourceGroup = 'rg-vibecast-prod',
    [string]$PostgresDockerImage = 'postgres:18',
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$expectedMigration = '20261001044539_InitialPostgreSql'
$runtimeRole = 'id-vibecast-prod'

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
    if ($exitCode -ne 0) {
        throw "$Description failed ($exitCode). $response"
    }
    return $response
}

function Get-OutputValue {
    param([psobject]$Outputs, [string]$Name)
    $property = $Outputs.PSObject.Properties[$Name]
    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value.value)) {
        throw "Prerequisite deployment output '$Name' is missing."
    }
    return [string]$property.Value.value
}

function Invoke-PsqlFile {
    param(
        [Parameter(Mandatory = $true)][string]$File,
        [Parameter(Mandatory = $true)][string]$Description
    )
    # All SQL travels through the pipeline; SQL containing double-quoted identifiers
    # must not be passed as a Windows PowerShell native command argument.
    $sql = [System.IO.File]::ReadAllText($File, [System.Text.Encoding]::UTF8)
    $argsDocker = @(
        'run', '--rm', '-i',
        '--env', 'PGPASSWORD',
        '--env', "PGHOST=$script:pgHost",
        '--env', "PGDATABASE=$script:pgDatabase",
        '--env', "PGUSER=$script:pgAdminUpn",
        '--env', 'PGPORT=5432',
        '--env', 'PGSSLMODE=require',
        '--env', 'PGCONNECT_TIMEOUT=10',
        $PostgresDockerImage,
        'psql', '--no-psqlrc', '--set', 'ON_ERROR_STOP=1',
        '--no-align', '--tuples-only', '--quiet', '--file', '-'
    )
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $output = @()
    $exitCode = 1
    try {
        $output = @($sql | & docker @argsDocker 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previous }
    $response = (($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine).Trim()
    if ($exitCode -ne 0) {
        throw "$Description failed ($exitCode). $response"
    }
    return $response
}

function Invoke-PsqlText {
    param([string]$Sql, [string]$Description)
    $path = Join-Path $script:workingDir ([guid]::NewGuid().ToString('N') + '.sql')
    try {
        [System.IO.File]::WriteAllText($path, $Sql, (New-Object System.Text.UTF8Encoding($false)))
        return Invoke-PsqlFile -File $path -Description $Description
    }
    finally { Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue }
}

foreach ($exe in @('az', 'docker', 'dotnet')) {
    if ($null -eq (Get-Command $exe -ErrorAction SilentlyContinue)) {
        throw "Required tool '$exe' is unavailable."
    }
}
$account = (Invoke-Checked -Command 'az' -Arguments @('account','show','--output','json') -Description 'Azure account lookup') | ConvertFrom-Json
$outputs = (Invoke-Checked -Command 'az' -Arguments @(
    'deployment','sub','show','--name',$DeploymentName,
    '--query','properties.outputs','--output','json'
) -Description 'prerequisite deployment lookup') | ConvertFrom-Json

$resourceGroup = Get-OutputValue -Outputs $outputs -Name 'productionResourceGroupName'
if ($resourceGroup -ne $ExpectedResourceGroup) {
    throw "Unexpected production resource group '$resourceGroup'."
}
$script:pgHost = Get-OutputValue -Outputs $outputs -Name 'postgresHost'
$script:pgDatabase = Get-OutputValue -Outputs $outputs -Name 'postgresDatabase'
$expectedPrincipalId = Get-OutputValue -Outputs $outputs -Name 'runtimeIdentityPrincipalId'
$pgServerId = Get-OutputValue -Outputs $outputs -Name 'postgresServerId'
$pgServerName = $pgServerId.Split('/')[-1]
if ($script:pgDatabase -notmatch '^[a-zA-Z][a-zA-Z0-9_]*$') {
    throw 'Unexpected PostgreSQL database identifier.'
}
try { $null = [guid]::Parse($expectedPrincipalId) }
catch { throw 'Invalid managed identity principal ID from Bicep.' }
$pgAdmin = (Invoke-Checked -Command 'az' -Arguments @('ad','signed-in-user','show','--output','json') -Description 'signed-in PostgreSQL administrator lookup') | ConvertFrom-Json
$script:pgAdminUpn = [string]$pgAdmin.userPrincipalName
if ([string]::IsNullOrWhiteSpace($script:pgAdminUpn)) {
    throw 'Sign into Azure CLI as the Microsoft Entra PostgreSQL administrator.'
}
Write-Host "Subscription : $($account.name)"
Write-Host "PostgreSQL   : $script:pgHost"
Write-Host "Database     : $script:pgDatabase"
Write-Host "Entra admin  : $script:pgAdminUpn"
Write-Host "Runtime role : $runtimeRole"

if (-not $SkipBuild) {
    Push-Location $repoRoot
    try {
        $null = Invoke-Checked -Command 'dotnet' -Arguments @('build','VibeCast.sln','--configuration','Release') -Description 'Release build'
        $null = Invoke-Checked -Command 'dotnet' -Arguments @('test','VibeCast.sln','--configuration','Release','--no-build') -Description 'Release tests'
    }
    finally { Pop-Location }
}
$script:workingDir = Join-Path ([System.IO.Path]::GetTempPath()) ('vibecast-bootstrap-' + [guid]::NewGuid().ToString('N'))
New-Item -Path $script:workingDir -ItemType Directory -Force | Out-Null
$previousPgPassword = [Environment]::GetEnvironmentVariable('PGPASSWORD', 'Process')
$firewallName = 'VibeCastSchema-' + [guid]::NewGuid().ToString('N').Substring(0, 12)
$firewallCreated = $false
$cleanupVerified = $true
try {
    Push-Location $repoRoot
    try {
        $migrationSqlFile = Join-Path $script:workingDir 'migrations.sql'
        $null = Invoke-Checked -Command 'dotnet' -Arguments @(
            'ef','migrations','script','--idempotent',
            '--project','src/VibeCast.Infrastructure/VibeCast.Infrastructure.csproj',
            '--startup-project','src/VibeCast.Web/VibeCast.Web.csproj',
            '--configuration','Release','--no-build','--output',$migrationSqlFile
        ) -Description 'generating the production EF Core migration script'
        if (-not (Test-Path -LiteralPath $migrationSqlFile)) {
            throw 'EF Core did not produce a migration script.'
        }
    }
    finally { Pop-Location }

    $null = Invoke-Checked -Command 'docker' -Arguments @('info','--format','{{.ServerVersion}}') -Description 'Docker runtime check'
    $available = Invoke-Checked -Command 'docker' -Arguments @('image','ls','--filter',"reference=$PostgresDockerImage",'--format','{{.ID}}') -Description 'PostgreSQL image check'
    if ([string]::IsNullOrWhiteSpace($available)) {
        $null = Invoke-Checked -Command 'docker' -Arguments @('pull',$PostgresDockerImage) -Description 'PostgreSQL image pull'
    }

    $publicIp = [string](Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 15)
    $ipAddress = $null
    if (-not [System.Net.IPAddress]::TryParse($publicIp, [ref]$ipAddress) -or
        $ipAddress.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork) {
        throw "Failed to determine a valid public IPv4 address: $publicIp"
    }

    Write-Host "Temporary firewall rule: $firewallName ($publicIp)."
    Write-Host 'Operations: apply pending EF migrations, seed one required policy, grant verified runtime DML privileges.'
    if ((Read-Host 'Type YES to approve PostgreSQL schema and permission changes') -cne 'YES') {
        throw 'Operation cancelled before database changes.'
    }
    $null = Invoke-Checked -Command 'az' -Arguments @(
        'postgres','flexible-server','firewall-rule','create',
        '--resource-group',$resourceGroup,'--name',$pgServerName,
        '--rule-name',$firewallName,'--start-ip-address',$publicIp,
        '--end-ip-address',$publicIp,'--output','none'
    ) -Description 'creating the script-owned temporary PostgreSQL firewall rule'
    $firewallCreated = $true

    $token = Invoke-Checked -Command 'az' -Arguments @(
        'account','get-access-token','--resource-type','oss-rdbms',
        '--query','accessToken','--output','tsv'
    ) -Description 'obtaining a PostgreSQL Entra token'
    if ([string]::IsNullOrWhiteSpace($token)) { throw 'Azure CLI returned an empty PostgreSQL token.' }
    $env:PGPASSWORD = $token
    $token = $null

    $connected = $false
    for ($attempt=1; $attempt -le 8; $attempt++) {
        try {
            $result = Invoke-PsqlText -Sql 'SELECT 1;' -Description 'PostgreSQL administrator connectivity'
            if ($result -match '(?m)^1$') { $connected = $true; break }
        }
        catch {
            if ($attempt -eq 8) { throw }
        }
        Start-Sleep -Seconds 5
    }
    if (-not $connected) { throw 'PostgreSQL administrator could not connect.' }

    # Verify mapping before any privileged operation. Never trust role name alone.
    $mappingSql = @"
SELECT COALESCE(to_jsonb(p)->>'objectid', to_jsonb(p)->>'objectId', '')
FROM pg_catalog.pgaadauth_list_principals(false) p
WHERE COALESCE(to_jsonb(p)->>'rolname', to_jsonb(p)->>'rolename', '') = '$runtimeRole';
"@
    $actualPrincipalId = Invoke-PsqlText -Sql $mappingSql -Description 'runtime Entra principal verification'
    if ($actualPrincipalId.Trim().ToLowerInvariant() -ne $expectedPrincipalId.ToLowerInvariant()) {
        throw "The PostgreSQL role does not map to the expected managed identity. Aborting."
    }
    Write-Host 'Verified PostgreSQL role/Entra principal mapping.'

    Write-Host 'Applying reviewed, idempotent EF Core migrations...'
    $null = Invoke-PsqlFile -File $migrationSqlFile -Description 'applying EF Core migrations'
    $migrationCount = Invoke-PsqlText -Sql (
        'SELECT count(*) FROM public."__EFMigrationsHistory" WHERE "MigrationId" = ' +
        "'$expectedMigration';"
    ) -Description 'migration history verification'
    if ($migrationCount.Trim() -ne '1') { throw "Expected EF migration '$expectedMigration' was not applied." }

    $seedSqlFile = Join-Path $repoRoot 'infra/seed-production-reference-data.sql'
    $null = Invoke-PsqlFile -File $seedSqlFile -Description 'required production reference-data initialization'

    $grantSql = @"
GRANT CONNECT ON DATABASE "$script:pgDatabase" TO "$runtimeRole";
GRANT USAGE ON SCHEMA public TO "$runtimeRole";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO "$runtimeRole";
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO "$runtimeRole";
ALTER DEFAULT PRIVILEGES IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO "$runtimeRole";
ALTER DEFAULT PRIVILEGES IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO "$runtimeRole";
"@
    $null = Invoke-PsqlText -Sql $grantSql -Description 'runtime DML and sequence grants'
    $verificationSql = @"
SELECT CASE WHEN
    to_regclass('public."Episodes"') IS NOT NULL
    AND to_regclass('public."AspNetUsers"') IS NOT NULL
    AND to_regclass('public."EpisodeFormatPolicies"') IS NOT NULL
    AND has_database_privilege('$runtimeRole', '$script:pgDatabase', 'CONNECT')
    AND has_schema_privilege('$runtimeRole', 'public', 'USAGE')
    AND NOT has_schema_privilege('$runtimeRole', 'public', 'CREATE')
    AND (
        SELECT count(*) FROM pg_catalog.pg_tables
        WHERE schemaname = 'public' AND NOT (
            has_table_privilege('$runtimeRole', format('%I.%I', schemaname, tablename), 'SELECT')
            AND has_table_privilege('$runtimeRole', format('%I.%I', schemaname, tablename), 'INSERT')
            AND has_table_privilege('$runtimeRole', format('%I.%I', schemaname, tablename), 'UPDATE')
            AND has_table_privilege('$runtimeRole', format('%I.%I', schemaname, tablename), 'DELETE')
        )
    ) = 0
THEN 'READY' ELSE 'NOT_READY' END;
"@
    $verification = Invoke-PsqlText -Sql $verificationSql -Description 'runtime grant verification'
    if ($verification.Trim() -ne 'READY') { throw "Runtime grants are not complete: $verification" }
    Write-Host 'READY: PostgreSQL schema, application reference data, and runtime grants are verified.' -ForegroundColor Green
}
finally {
    [Environment]::SetEnvironmentVariable('PGPASSWORD', $previousPgPassword, 'Process')
    if ($firewallCreated) {
        $cleanupVerified = $false
        for ($attempt=1; $attempt -le 4; $attempt++) {
            try {
                $null = Invoke-Checked -Command 'az' -Arguments @(
                    'postgres','flexible-server','firewall-rule','delete',
                    '--resource-group',$resourceGroup,'--name',$pgServerName,
                    '--rule-name',$firewallName,'--yes','--output','none'
                ) -Description 'temporary PostgreSQL firewall cleanup'
                $cleanupVerified = $true
                break
            }
            catch {
                Write-Warning "Temporary firewall cleanup attempt $attempt failed: $_"
                Start-Sleep -Seconds 3
            }
        }
        if (-not $cleanupVerified) {
            Write-Warning "MANUAL CLEANUP REQUIRED: az postgres flexible-server firewall-rule delete -g $resourceGroup -n $pgServerName --rule-name $firewallName --yes"
        }
        else { Write-Host "Removed temporary firewall rule '$firewallName'." }
    }
    Remove-Item -LiteralPath $script:workingDir -Recurse -Force -ErrorAction SilentlyContinue
}
if (-not $cleanupVerified) { throw 'Database operations completed but temporary firewall cleanup could not be verified.' }
