param([ValidateSet('up','down','infra','rider')][string]$Action = 'up')
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$envFile = Join-Path $root '.env'
if (-not (Test-Path -LiteralPath $envFile)) {
    $key = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    $password = 'Admin!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(12))
    [IO.File]::WriteAllLines($envFile, @("JWT_KEY=$key", 'ADMIN_EMAIL=admin@billiard.local', "ADMIN_PASSWORD=$password"))
    Write-Output 'Created local .env with random credentials; do not commit it.'
}
Push-Location $root
try {
    switch ($Action) {
        'up' { docker compose up -d --build }
        'infra' { docker compose up -d postgres redis kafka }
        'down' { docker compose down }
        'rider' {
            $values = @{}
            Get-Content -LiteralPath $envFile | Where-Object { $_ -match '^[A-Z_]+=' } | ForEach-Object {
                $parts = $_ -split '=',2; $values[$parts[0]]=$parts[1]
            }
            $env:Jwt__Key=$values['JWT_KEY']
            $env:Bootstrap__AdminEmail=$values['ADMIN_EMAIL']
            $env:Bootstrap__AdminPassword=$values['ADMIN_PASSWORD']
            $env:ApplyMigrations='true'; $env:InitializeIdentity='true'
            dotnet run --project src/Tournaments.Api -- --urls http://127.0.0.1:5080
        }
    }
    if ($LASTEXITCODE -ne 0) { throw "Command failed with exit code $LASTEXITCODE" }
} finally { Pop-Location }
