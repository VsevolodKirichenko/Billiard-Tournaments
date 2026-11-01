param([Parameter(Mandatory)][Guid]$TournamentId, [string]$BaseUrl='http://127.0.0.1:5080', [int]$Requests=30)
$ErrorActionPreference='Stop'
if ($Requests -lt 2 -or $Requests -gt 1000) { throw 'Requests must be between 2 and 1000.' }
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$t = Invoke-RestMethod "$BaseUrl/tournaments/$TournamentId"
# Remove ONLY this tournament/version cache entry. Never flush unrelated Redis data.
Push-Location $root
try { docker compose exec -T redis redis-cli DEL "tournament:$($TournamentId):v$($t.version):standings" | Out-Null }
finally { Pop-Location }
if ($LASTEXITCODE -ne 0) { throw 'Redis invalidation failed.' }
$timer=[Diagnostics.Stopwatch]::StartNew()
$null=Invoke-RestMethod "$BaseUrl/tournaments/$TournamentId/standings"
$timer.Stop(); $cold=$timer.Elapsed.TotalMilliseconds
$samples=for ($i=0; $i -lt $Requests; $i++) {
    $timer.Restart(); $null=Invoke-RestMethod "$BaseUrl/tournaments/$TournamentId/standings"
    $timer.Stop(); $timer.Elapsed.TotalMilliseconds
}
$sorted=@($samples | Sort-Object)
$result=[ordered]@{
    timestampUtc=[DateTime]::UtcNow.ToString('o'); tournamentId=$TournamentId.ToString(); matchCount=$t.matches.Count
    requests=$Requests; coldRequestMs=$cold; warmMedianMs=$sorted[[int][Math]::Floor(($Requests-1)*0.5)]
    warmP95Ms=$sorted[[int][Math]::Ceiling($Requests*0.95)-1]
    scope='Sequential local HTTP requests; no concurrency; cold includes EF/JIT/network; not a controlled SQL-only comparison.'
}
$reports=Join-Path $root 'reports'; $null=New-Item -ItemType Directory -Path $reports -Force
$result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $reports 'benchmark.json')
[pscustomobject]$result
