<#
.SYNOPSIS
    Bootstrap the VibeCast PostgreSQL runtime identity from Windows PowerShell.

.DESCRIPTION
    Continues after infra/configure-production-identity.ps1 has configured
    Azure RBAC and the PostgreSQL Microsoft Entra administrator.

    This script:
      1. Reads resource IDs and names from the existing Bicep deployment.
      2. Checks PostgreSQL management readiness, using bounded retries and
         an Azure Resource Manager fallback for transient CLI failures.
      3. Verifies Azure login and that Docker can run the PostgreSQL client.
      4. Temporarily allows this workstation's public IPv4 address.
      5. Obtains a short-lived Microsoft Entra token for PostgreSQL.
      6. Creates OR verifies the PostgreSQL role mapped to id-vibecast-prod.
      7. Grants only database CONNECT and public-schema USAGE.
      8. Verifies the Entra mapping, role capabilities, and SQL privileges.
      9. Removes the temporary firewall rule and clears the token, even if
         PostgreSQL operations fail.

    Re-running the script verifies the existing role and repeats safe GRANTs.
    It never resets an existing PostgreSQL principal or replaces its mapping.

    Firewall rule naming is stable for this workstation/user/server. This
    makes a rerun reuse/clean up a rule left by an interrupted prior run,
    rather than leave behind an increasing number of temporary rules.

    This script does NOT create the PostgreSQL schema, run EF migrations,
    grant table/sequence privileges, or deploy Azure Container Apps.

.PREREQUISITES
    - Run from the repository root, in Windows PowerShell 5.1 or PowerShell 7.
    - Azure CLI logged into the correct subscription as the PostgreSQL
      Microsoft Entra administrator configured in the prior step.
    - Docker Desktop running, with outbound connectivity to the PostgreSQL
      server on TCP 5432.
    - Azure permissions to create and delete PostgreSQL firewall rules.
    - PostgreSQL Flexible Server public network access enabled (as in the
      teaching Bicep deployment). Do not use this on private-only servers.

.EXAMPLE
    .\infra\configure-pgsql-entra-admin.ps1

.EXAMPLE
    .\infra\configure-pgsql-entra-admin.ps1 -ClientPublicIp (Invoke-RestMethod 'https://api.ipify.org')

    Use -ClientPublicIp with a real public IPv4 address if automatic
    discovery is blocked or your Docker traffic exits through a VPN.
#>

[CmdletBinding()]
param(
    [string]$DeploymentName = 'vibecast-production-prerequisites',
    [string]$ExpectedResourceGroup = 'rg-vibecast-prod',
    [string]$PostgresDockerImage = 'postgres:18',
    [string]$ClientPublicIp = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host ''
    Write-Host ('=' * 72) -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host ('=' * 72) -ForegroundColor Cyan
}

function Assert-Command {
    param([Parameter(Mandatory = $true)][string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found. Install it and rerun."
    }
}

# Execute Azure CLI without treating warnings on stderr as JSON. Never print
# successful stdout: in particular, the access-token response is sensitive.
# Read operations may be retried; create/delete calls have explicit controls.
function Invoke-Az {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Description,
        [ValidateRange(1, 6)][int]$Attempts = 1,
        [switch]$AllowFailure
    )

    $lastDetails = 'No response.'
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        $errorFile = [System.IO.Path]::GetTempFileName()
        $output = @()
        $exitCode = -1
        $invokeException = ''

        try {
            # Windows PowerShell surfaces native stderr as ErrorRecords when
            # ErrorActionPreference is Stop. Handle exit codes explicitly.
            $oldPreference = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            try {
                $output = @(& az @Arguments 2> $errorFile)
                $exitCode = $LASTEXITCODE
            }
            catch {
                $invokeException = $_.Exception.Message
            }
            finally {
                $ErrorActionPreference = $oldPreference
            }

            $details = ''
            if (Test-Path -LiteralPath $errorFile) {
                $details = [System.IO.File]::ReadAllText($errorFile).Trim()
            }
        }
        finally {
            Remove-Item -LiteralPath $errorFile -Force -ErrorAction SilentlyContinue
        }

        $text = ($output -join [Environment]::NewLine).Trim()
        if ($exitCode -eq 0 -and [string]::IsNullOrWhiteSpace($invokeException)) {
            return [pscustomobject]@{
                Succeeded = $true
                Text      = $text
                Details   = ''
            }
        }

        $lastDetails = if (-not [string]::IsNullOrWhiteSpace($details)) {
            $details
        }
        elseif (-not [string]::IsNullOrWhiteSpace($invokeException)) {
            $invokeException
        }
        else {
            "Process exit code: $exitCode"
        }

        if ($attempt -lt $Attempts) {
            Write-Warning "$Description failed ($attempt/$Attempts). Retrying shortly."
            Start-Sleep -Seconds (5 * $attempt)
        }
    }

    if ($AllowFailure) {
        return [pscustomobject]@{
            Succeeded = $false
            Text      = ''
            Details   = $lastDetails
        }
    }

    throw "Azure CLI failed: $Description.`n$lastDetails"
}

function Get-AzJson {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Description,
        [ValidateRange(1, 6)][int]$Attempts = 1,
        [switch]$AllowFailure
    )

    $result = Invoke-Az -Arguments $Arguments -Description $Description -Attempts $Attempts -AllowFailure:$AllowFailure
    if (-not $result.Succeeded) { return $null }
    if ([string]::IsNullOrWhiteSpace($result.Text) -or $result.Text -eq 'null') {
        throw "Azure CLI returned no result: $Description."
    }
    return ($result.Text | ConvertFrom-Json)
}

function Get-DeploymentOutput {
    param(
        [Parameter(Mandatory = $true)][object]$Outputs,
        [Parameter(Mandatory = $true)][string]$Name
    )
    $property = $Outputs.PSObject.Properties[$Name]
    if ($null -eq $property) { throw "Bicep output '$Name' is missing." }
    $value = [string]$property.Value.value
    if ([string]::IsNullOrWhiteSpace($value)) { throw "Bicep output '$Name' is empty." }
    return $value
}

function Test-PublicIPv4 {
    param([Parameter(Mandatory = $true)][string]$Value)
    $address = $null
    if (-not [System.Net.IPAddress]::TryParse($Value, [ref]$address)) { return $false }
    if ($address.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork) { return $false }
    $b = $address.GetAddressBytes()

    # Reject private, loopback, link-local, shared-address-space, multicast,
    # benchmarking, and documentation-only IPs. An Azure firewall rule must
    # target the workstation's actual PUBLIC egress address.
    if ($b[0] -eq 0 -or $b[0] -eq 10 -or $b[0] -eq 127 -or
        $b[0] -ge 224 -or
        ($b[0] -eq 100 -and $b[1] -ge 64 -and $b[1] -le 127) -or
        ($b[0] -eq 169 -and $b[1] -eq 254) -or
        ($b[0] -eq 172 -and $b[1] -ge 16 -and $b[1] -le 31) -or
        ($b[0] -eq 192 -and $b[1] -eq 168) -or
        ($b[0] -eq 192 -and $b[1] -eq 0 -and $b[2] -eq 0) -or
        ($b[0] -eq 192 -and $b[1] -eq 0 -and $b[2] -eq 2) -or
        ($b[0] -eq 198 -and $b[1] -ge 18 -and $b[1] -le 19) -or
        ($b[0] -eq 198 -and $b[1] -eq 51 -and $b[2] -eq 100) -or
        ($b[0] -eq 203 -and $b[1] -eq 0 -and $b[2] -eq 113)) {
        return $false
    }
    return $true
}

# Docker may legitimately return a nonzero exit code (for example when a
# local image is absent). On Windows PowerShell 5.1, native stderr can become
# a terminating NativeCommandError when ErrorActionPreference is Stop.
# Capture the result and evaluate the native exit code explicitly instead.
function Invoke-Docker {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $lines = @(& docker @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }

    $resultText = (($lines | ForEach-Object { [string]$_ }) -join [Environment]::NewLine).Trim()
    if ($exitCode -ne 0) {
        throw "Docker failed while $Description (exit code $exitCode).`n$resultText"
    }
    return $resultText
}

# Use the official PostgreSQL client bundled with the Docker postgres:18
# image. No PostgreSQL tools need to be installed on the developer's host.
#
# IMPORTANT: Do not pass SQL through `docker ... psql --command $Sql`.
# In Windows PowerShell 5.1, embedded double quotes can be stripped while
# constructing the native docker.exe argument list. The SQL role name here
# contains hyphens and MUST reach PostgreSQL as "id-vibecast-prod".
#
# Instead, stream SQL through standard input (`docker run -i` and `psql -f -`).
# Standard input carries the literal SQL characters, including identifier
# quoting, with no native command-line quoting ambiguity.
#
# The Entra token is forwarded through PGPASSWORD, never a CLI argument, SQL
# literal, or file. ON_ERROR_STOP forces psql to exit nonzero on SQL errors.
function Invoke-PostgresSql {
    param(
        [Parameter(Mandatory = $true)][string]$Database,
        [Parameter(Mandatory = $true)][string]$Sql,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $dockerArgs = @(
        'run', '--rm', '--interactive',
        '--env', 'PGPASSWORD',
        '--env', "PGHOST=$script:postgresHost",
        '--env', 'PGPORT=5432',
        '--env', "PGDATABASE=$Database",
        '--env', "PGUSER=$script:adminUpn",
        '--env', 'PGSSLMODE=require',
        '--env', 'PGCONNECT_TIMEOUT=10',
        $PostgresDockerImage,
        'psql', '--no-psqlrc', '--no-align', '--tuples-only', '--quiet',
        '--set', 'ON_ERROR_STOP=1',
        '--file', '-'
    )

    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        # PowerShell pipes the literal SQL to Docker's stdin. This is safe for
        # the single/multiline, ASCII SQL used in this bootstrap script and
        # works in Windows PowerShell 5.1 as well as PowerShell 7.
        $output = @($Sql | & docker @dockerArgs 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }

    if ($exitCode -ne 0) {
        $details = ($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine
        throw "PostgreSQL operation failed ($Description).`n$details"
    }
    return (($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine).Trim()
}

function Get-PostgresReadyState {
    param([Parameter(Mandatory = $true)][string]$ServerResourceId,
          [Parameter(Mandatory = $true)][string]$ResourceGroup,
          [Parameter(Mandatory = $true)][string]$ServerName)

    # The service-specific CLI command can fail transiently with Windows
    # ConnectionResetError(10054). Retry, then use the ARM resource API.
    Write-Host 'Checking PostgreSQL server state via Azure CLI...'
    $serviceResult = Invoke-Az -Arguments @(
        'postgres', 'flexible-server', 'show',
        '--resource-group', $ResourceGroup,
        '--name', $ServerName,
        '--output', 'json'
    ) -Description 'reading PostgreSQL server state' -Attempts 3 -AllowFailure

    if ($serviceResult.Succeeded -and -not [string]::IsNullOrWhiteSpace($serviceResult.Text)) {
        $server = $serviceResult.Text | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace([string]$server.state)) {
            return [string]$server.state
        }
    }

    Write-Warning 'Service-specific status lookup failed or lacked state; trying ARM resource lookup.'
    $resourceResult = Invoke-Az -Arguments @(
        'resource', 'show',
        '--ids', $ServerResourceId,
        '--api-version', '2024-08-01',
        '--output', 'json'
    ) -Description 'reading PostgreSQL ARM resource' -Attempts 3 -AllowFailure

    if (-not $resourceResult.Succeeded -or [string]::IsNullOrWhiteSpace($resourceResult.Text)) {
        # The actual data-plane SELECT 1 below is a stronger connectivity
        # check. If BOTH management reads suffer a transient reset, do not
        # block the lab solely on a management API response. Never claim
        # the server is Ready without proof.
        Write-Warning 'Both management status lookups failed. Readiness is UNKNOWN, not Ready.'
        Write-Warning 'Continuing only to attempt firewall access and an authenticated SELECT 1. Database writes remain gated on that successful connection.'
        return 'Unknown'
    }

    $resource = $resourceResult.Text | ConvertFrom-Json
    $state = [string]$resource.properties.state
    if ([string]::IsNullOrWhiteSpace($state)) {
        Write-Warning 'ARM lookup omitted the PostgreSQL state. Relying on the authenticated SELECT 1 gate.'
        return 'Unknown'
    }
    return $state
}

Write-Step 'VibeCast PostgreSQL runtime identity - prerequisite checks'
Assert-Command 'az'
Assert-Command 'docker'

$account = Get-AzJson -Arguments @('account', 'show', '--output', 'json') -Description 'reading Azure subscription'
Write-Host "Subscription : $($account.name)"
Write-Host "Tenant       : $($account.tenantId)"

$outputs = Get-AzJson -Arguments @(
    'deployment', 'sub', 'show',
    '--name', $DeploymentName,
    '--query', 'properties.outputs',
    '--output', 'json'
) -Description 'reading production Bicep outputs' -Attempts 3

$resourceGroup = Get-DeploymentOutput -Outputs $outputs -Name 'productionResourceGroupName'
if ($resourceGroup -ne $ExpectedResourceGroup) {
    throw "Expected resource group '$ExpectedResourceGroup', but deployment outputs say '$resourceGroup'. Check subscription/deployment."
}
$postgresServerId = Get-DeploymentOutput -Outputs $outputs -Name 'postgresServerId'
$script:postgresHost = Get-DeploymentOutput -Outputs $outputs -Name 'postgresHost'
$databaseName = Get-DeploymentOutput -Outputs $outputs -Name 'postgresDatabase'
$runtimePrincipalId = Get-DeploymentOutput -Outputs $outputs -Name 'runtimeIdentityPrincipalId'
$runtimeRole = 'id-vibecast-prod'
$serverName = $postgresServerId.Split('/')[-1]

$parsedGuid = [Guid]::Empty
if (-not [Guid]::TryParse($runtimePrincipalId, [ref]$parsedGuid)) {
    throw 'The deployed runtime identity principal ID is not a valid GUID.'
}
if ($databaseName -cnotmatch '^[a-zA-Z_][a-zA-Z0-9_]*$') {
    throw 'PostgreSQL database name is not a simple identifier; aborting before SQL interpolation.'
}

Write-Host "PostgreSQL   : $script:postgresHost"
Write-Host "Database     : $databaseName"
Write-Host "Runtime UAMI : $runtimeRole"

Write-Step 'Verify PostgreSQL readiness and local Docker tools'
$serverState = Get-PostgresReadyState -ServerResourceId $postgresServerId -ResourceGroup $resourceGroup -ServerName $serverName
if ($serverState -ne 'Ready' -and $serverState -ne 'Unknown') {
    throw "PostgreSQL is '$serverState', not Ready. No database changes were made."
}
if ($serverState -eq 'Ready') {
    Write-Host 'PostgreSQL reports Ready.' -ForegroundColor Green
}
else {
    Write-Warning 'PostgreSQL management status is unknown. The script MUST establish an authenticated SQL connection before any role changes.'
}

$currentUser = Get-AzJson -Arguments @('ad', 'signed-in-user', 'show', '--output', 'json') -Description 'reading signed-in Entra administrator'
$script:adminUpn = [string]$currentUser.userPrincipalName
if ([string]::IsNullOrWhiteSpace($script:adminUpn)) {
    throw 'Azure CLI must be signed in as the Entra user configured as PostgreSQL administrator in the previous step.'
}
Write-Host "Entra user   : $script:adminUpn"
Write-Host 'SQL connectivity and permission checks will confirm database administrator access.'

# Verify Docker is available and pre-pull BEFORE creating firewall rules or
# requesting a short-lived Entra token.
#
# IMPORTANT: Do NOT use `docker image inspect` as an existence test with
# $ErrorActionPreference = 'Stop' in Windows PowerShell 5.1. A missing image is
# normal, but Docker writes its "No such image" message to stderr, which
# PowerShell can promote to a terminating NativeCommandError before the pull.
$dockerServerVersion = Invoke-Docker -Arguments @(
    'info', '--format', '{{.ServerVersion}}'
) -Description 'checking Docker Desktop availability'
Write-Host "Docker engine : $dockerServerVersion"

# Docker image ls succeeds with an empty result when there is no local match.
# Its reference filter avoids both the missing-image error and unnecessary
# pulls on repeat executions of this idempotent bootstrap.
$localImageIds = Invoke-Docker -Arguments @(
    'image', 'ls', '--filter', "reference=$PostgresDockerImage",
    '--format', '{{.ID}}'
) -Description "checking for local PostgreSQL image '$PostgresDockerImage'"

if ([string]::IsNullOrWhiteSpace($localImageIds)) {
    Write-Host "PostgreSQL client image '$PostgresDockerImage' is not cached; pulling it now..." -ForegroundColor Yellow
    $pulledImage = Invoke-Docker -Arguments @(
        'pull', $PostgresDockerImage
    ) -Description "pulling PostgreSQL client image '$PostgresDockerImage'"
    Write-Host "Docker pull completed for '$PostgresDockerImage'." -ForegroundColor Green
}
else {
    Write-Host "PostgreSQL client image '$PostgresDockerImage' is already cached." -ForegroundColor Green
}

if ([string]::IsNullOrWhiteSpace($ClientPublicIp)) {
    try {
        $ClientPublicIp = ([string](Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 20)).Trim()
    }
    catch {
        throw 'Could not detect your public IPv4. Rerun with -ClientPublicIp using the real public IPv4 of your Docker/workstation egress.'
    }
}
if (-not (Test-PublicIPv4 -Value $ClientPublicIp)) {
    throw "The address '$ClientPublicIp' is not a usable public IPv4 address. Supply the actual workstation egress IPv4."
}
Write-Host "Public egress : $ClientPublicIp"

# Derive a predictable name from this workstation/user/subscription/server.
# If the CLI loses the create response, a later rerun addresses the same
# script-owned firewall rule, not a new randomly named rule each time.
$ruleIdentity = ('{0}|{1}|{2}|{3}' -f [Environment]::MachineName, [Environment]::UserName, $account.id, $serverName)
$sha = [System.Security.Cryptography.SHA256]::Create()
try {
    $hash = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($ruleIdentity))
}
finally {
    $sha.Dispose()
}
$ruleSuffix = ([System.BitConverter]::ToString($hash)).Replace('-', '').Substring(0, 12)
$firewallRuleName = 'VibeCastBootstrap-' + $ruleSuffix

Write-Step 'Create or verify the PostgreSQL runtime Entra role'
Write-Host 'The script will temporarily allow this one public IP and then remove its rule.'
Write-Host "Temporary rule name: $firewallRuleName"
$approval = Read-Host 'Type YES to continue'
if ($approval -cne 'YES') {
    Write-Host 'Cancelled before any firewall or PostgreSQL changes.' -ForegroundColor Yellow
    return
}

$firewallAttempted = $false
$cleanupSucceeded = $true
$previousPgPassword = [Environment]::GetEnvironmentVariable('PGPASSWORD', 'Process')
$completed = $false

try {
    # Azure's 0.0.0.0 rule only allows Azure-originating addresses; your
    # workstation requires its own temporary exact-IP firewall rule.
    # Reusing the same script-owned name updates a stale leftover rule.
    $firewallAttempted = $true
    $created = Invoke-Az -Arguments @(
        'postgres', 'flexible-server', 'firewall-rule', 'create',
        '--resource-group', $resourceGroup,
        '--server-name', $serverName,
        '--name', $firewallRuleName,
        '--start-ip-address', $ClientPublicIp,
        '--end-ip-address', $ClientPublicIp,
        '--output', 'none'
    ) -Description 'creating temporary PostgreSQL firewall rule' -Attempts 2 -AllowFailure

    if (-not $created.Succeeded) {
        throw "Firewall creation was not acknowledged. Its result is uncertain; cleanup will be attempted. Details: $($created.Details)"
    }
    Write-Host "Temporary firewall rule provisioned: $firewallRuleName"

    # The Entra token is issued to the signed-in PostgreSQL admin, NOT to
    # the managed identity being configured. It is never printed.
    $tokenResult = Invoke-Az -Arguments @(
        'account', 'get-access-token',
        '--resource-type', 'oss-rdbms',
        '--query', 'accessToken',
        '--output', 'tsv'
    ) -Description 'obtaining a PostgreSQL Entra token' -Attempts 2

    if ([string]::IsNullOrWhiteSpace($tokenResult.Text)) {
        throw 'Azure CLI returned an empty PostgreSQL Entra access token.'
    }
    $env:PGPASSWORD = $tokenResult.Text
    $tokenResult = $null

    # Azure may take some time to apply the new firewall rule. Retry only
    # the harmless connectivity probe; never blindly replay write queries.
    Write-Host 'Testing SQL connectivity with the Entra administrator...'
    $connected = $false
    $lastProbeError = ''
    for ($attempt = 1; $attempt -le 8; $attempt++) {
        try {
            $probe = Invoke-PostgresSql -Database 'postgres' -Sql 'SELECT 1;' -Description 'SQL connectivity probe'
            if ($probe -eq '1') { $connected = $true; break }
            $lastProbeError = "Unexpected probe result: $probe"
        }
        catch {
            $lastProbeError = $_.Exception.Message
        }
        if ($attempt -lt 8) {
            Write-Host "Waiting for PostgreSQL firewall/connection readiness ($attempt/8)..."
            Start-Sleep -Seconds 10
        }
    }
    if (-not $connected) {
        throw "Could not connect to PostgreSQL as '$script:adminUpn'. Check PostgreSQL Entra administrator configuration, Docker's public egress IP, port 5432, and firewall propagation.`n$lastProbeError"
    }

    # Do not create/replace an existing role based merely on its display
    # name. Verify the Entra object-ID mapping before granting access.
    $roleCount = Invoke-PostgresSql -Database 'postgres' -Sql "SELECT count(*) FROM pg_catalog.pg_roles WHERE rolname = '$runtimeRole';" -Description 'checking runtime role existence'
    if ($roleCount -eq '0') {
        $createSql = @"
SELECT pg_catalog.pgaadauth_create_principal_with_oid(
    '$runtimeRole',
    '$runtimePrincipalId',
    'service',
    false,
    false
);
"@
        $creationResult = Invoke-PostgresSql -Database 'postgres' -Sql $createSql -Description 'creating PostgreSQL Entra runtime principal'
        Write-Host $creationResult
    }
    elseif ($roleCount -eq '1') {
        Write-Host 'PostgreSQL role exists; verifying its Entra mapping before applying grants.'
    }
    else {
        throw "Unexpected runtime role count: $roleCount"
    }

    # The PostgreSQL pgaadauth extension maps each PostgreSQL role to an
    # Entra object ID. Verify the mapping rather than trusting the role name.
    #
    # Important: Microsoft Learn documents the first output column as
    # 'rolename', but the actual PostgreSQL server returned 'rolname'.
    # Convert each function result to JSON and accept either field spelling.
    # This also accommodates upper/lowercase variants of other field names.
    # PostgreSQL's to_jsonb(record) is standard and does not require a schema
    # change or any extension installation.
    $mappingSql = @"
WITH mapped_principals AS (
    SELECT to_jsonb(p) AS details
    FROM pg_catalog.pgaadauth_list_principals(false) AS p
)
SELECT
    COALESCE(details->>'objectid', details->>'objectId') || '|' ||
    COALESCE(details->>'principaltype', details->>'principalType') || '|' ||
    COALESCE(details->>'isadmin', details->>'isAdmin')
FROM mapped_principals
WHERE COALESCE(details->>'rolname', details->>'rolename') = '$runtimeRole';
"@
    $mapping = Invoke-PostgresSql -Database 'postgres' -Sql $mappingSql -Description 'verifying managed-identity-to-database-role mapping'
    $fields = ([string]$mapping).Trim().Split('|')
    $isNonAdmin = ($fields.Count -eq 3 -and $fields[2] -in @('0', 'false'))
    if (-not $isNonAdmin -or
        $fields[0] -ine $runtimePrincipalId -or
        $fields[1] -ine 'service') {
        throw "Unsafe or missing Entra mapping for role '$runtimeRole' (details: $mapping). No GRANTs were applied."
    }

    $capabilitySql = "SELECT CASE WHEN rolsuper OR rolcreatedb OR rolcreaterole THEN 'unsafe' ELSE 'safe' END FROM pg_catalog.pg_roles WHERE rolname = '$runtimeRole';"
    $capabilities = Invoke-PostgresSql -Database 'postgres' -Sql $capabilitySql -Description 'checking PostgreSQL role administrative capabilities'
    if ($capabilities -ne 'safe') {
        throw "Runtime role '$runtimeRole' has administrative PostgreSQL capabilities. No GRANTs were applied."
    }
    Write-Host 'Entra object ID, principal type, and non-admin role capabilities verified.' -ForegroundColor Green

    # Intentionally defer table/sequence privileges and migrations until
    # the production schema has been deployed in the NEXT lesson.
    $dbGrant = 'GRANT CONNECT ON DATABASE "' + $databaseName + '" TO "' + $runtimeRole + '";'
    $null = Invoke-PostgresSql -Database 'postgres' -Sql $dbGrant -Description 'granting database CONNECT'
    $schemaGrant = 'GRANT USAGE ON SCHEMA public TO "' + $runtimeRole + '";'
    $null = Invoke-PostgresSql -Database $databaseName -Sql $schemaGrant -Description 'granting public schema USAGE'

    $dbCheck = Invoke-PostgresSql -Database $databaseName -Sql "SELECT has_database_privilege('$runtimeRole', '$databaseName', 'CONNECT');" -Description 'verifying CONNECT privilege'
    $schemaCheck = Invoke-PostgresSql -Database $databaseName -Sql "SELECT has_schema_privilege('$runtimeRole', 'public', 'USAGE');" -Description 'verifying schema USAGE privilege'
    $schemaCreate = Invoke-PostgresSql -Database $databaseName -Sql "SELECT has_schema_privilege('$runtimeRole', 'public', 'CREATE');" -Description 'verifying absence of schema CREATE permission'

    if ($dbCheck -ne 't' -or $schemaCheck -ne 't' -or $schemaCreate -ne 'f') {
        throw "Unexpected database privileges (CONNECT=$dbCheck, USAGE=$schemaCheck, CREATE=$schemaCreate)."
    }

    $completed = $true
    Write-Host 'Verified CONNECT and USAGE; no schema CREATE or role administration.' -ForegroundColor Green
}
finally {
    # Never leave the Entra access token in the student's PowerShell session.
    if ($null -eq $previousPgPassword) {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:PGPASSWORD = $previousPgPassword
    }

    # ALWAYS attempt cleanup when firewall creation was issued, including if
    # the Azure CLI connection reset after Azure accepted the create request.
    if ($firewallAttempted) {
        Write-Host 'Removing the script-owned temporary firewall rule...'
        $removed = $false
        $lastCleanupError = ''
        for ($attempt = 1; $attempt -le 4; $attempt++) {
            $deleteResult = Invoke-Az -Arguments @(
                'postgres', 'flexible-server', 'firewall-rule', 'delete',
                '--resource-group', $resourceGroup,
                '--server-name', $serverName,
                '--name', $firewallRuleName,
                '--yes',
                '--output', 'none'
            ) -Description 'removing temporary PostgreSQL firewall rule' -Attempts 1 -AllowFailure

            if ($deleteResult.Succeeded) {
                $removed = $true
                break
            }
            $lastCleanupError = $deleteResult.Details
            if ($attempt -lt 4) { Start-Sleep -Seconds (5 * $attempt) }
        }

        if (-not $removed) {
            $cleanupSucceeded = $false
            Write-Warning "SECURITY ACTION REQUIRED: Could not confirm removal of temporary firewall rule '$firewallRuleName'."
            Write-Warning "Manually verify and delete: az postgres flexible-server firewall-rule delete --resource-group $resourceGroup --server-name $serverName --name $firewallRuleName --yes"
            Write-Warning "Last CLI error: $lastCleanupError"
        }
        else {
            Write-Host "Temporary firewall rule '$firewallRuleName' removed." -ForegroundColor Green
        }
    }
}

if (-not $cleanupSucceeded) {
    throw 'PostgreSQL operations may have succeeded, but firewall cleanup was not confirmed. Resolve the warning before continuing.'
}
if (-not $completed) {
    throw 'PostgreSQL runtime identity setup did not complete.'
}

Write-Step 'Completed - PostgreSQL runtime identity ready'
Write-Host "Role          : $runtimeRole"
Write-Host "Entra object  : $runtimePrincipalId"
Write-Host "Database      : $databaseName"
Write-Host 'Permissions   : CONNECT on database; USAGE on public schema'
Write-Host 'Not granted   : CREATE, CREATEROLE, CREATEDB, table DML, sequence privileges'
Write-Host ''
Write-Host 'Next lesson: apply the production EF Core schema and table/sequence grants during Container Apps deployment.'
