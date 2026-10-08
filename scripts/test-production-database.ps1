# Local PostgreSQL regression test. No Azure calls, published ports, or persistent volume.
[CmdletBinding()]
param([string]$PostgresDockerImage = 'postgres:18')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$name = 'vibecast-db-test-' + [guid]::NewGuid().ToString('N').Substring(0,12)
$previousEncoding = $OutputEncoding
$OutputEncoding = New-Object System.Text.UTF8Encoding($false)
function Invoke-Sql([string]$Sql) {
    $Sql | & docker exec -i $name psql -U postgres -d vibecast -X -v ON_ERROR_STOP=1 -q
    if ($LASTEXITCODE -ne 0) { throw 'Local PostgreSQL regression failed.' }
}
Push-Location $repoRoot
try {
    New-Item -ItemType Directory -Force artifacts | Out-Null
    dotnet ef migrations script 0 20261001044539_InitialPostgreSql --idempotent --project src/VibeCast.Infrastructure --startup-project src/VibeCast.Web --configuration Release --no-build --output artifacts/initial-migration.sql
    if ($LASTEXITCODE -ne 0) { throw 'Migration generation failed.' }
    docker run -d --name $name --network none --env POSTGRES_HOST_AUTH_METHOD=trust --env POSTGRES_DB=vibecast $PostgresDockerImage | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Local PostgreSQL container failed to start.' }
    $ready = $false
    for ($attempt=0; $attempt -lt 30; $attempt++) {
        docker exec $name pg_isready -U postgres -d vibecast 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'Local PostgreSQL startup timed out.' }
    Invoke-Sql 'CREATE ROLE "id-vibecast-prod"; CREATE TABLE public.unrelated (id integer);'
    foreach ($iteration in 1..2) {
        foreach ($file in @('artifacts/initial-migration.sql', 'infra/seed-production-reference-data.sql', 'infra/grant-production-runtime.sql')) {
            Invoke-Sql ([System.IO.File]::ReadAllText((Join-Path $repoRoot $file)))
        }
        if ($iteration -eq 1) {
            Invoke-Sql 'UPDATE public."EpisodeFormatPolicies" SET "Rationale" = ''preserve administrator edit'';'
        }
    }
    Invoke-Sql ([System.IO.File]::ReadAllText((Join-Path $repoRoot 'scripts/verify-local-production-database.sql')))
    Write-Host 'PASS: repeated migration, policy preservation, exact grants, sequence access, and prohibited operations.'
}
finally {
    docker rm -f $name 2>$null | Out-Null
    $OutputEncoding = $previousEncoding
    Pop-Location
}
