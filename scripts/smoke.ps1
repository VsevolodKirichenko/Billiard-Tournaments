param([string]$BaseUrl = 'http://127.0.0.1:5080')
$ErrorActionPreference = 'Stop'
function Assert-That($Condition, $Message) { if (-not $Condition) { throw $Message }; Write-Output "PASS $Message" }
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$values = @{}
Get-Content -LiteralPath (Join-Path $root '.env') | Where-Object { $_ -match '^[A-Z_]+=' } | ForEach-Object {
    $parts = $_ -split '=',2; $values[$parts[0]]=$parts[1]
}
$login = @{email=$values['ADMIN_EMAIL']; password=$values['ADMIN_PASSWORD']} | ConvertTo-Json
$token = Invoke-RestMethod "$BaseUrl/auth/login" -Method Post -ContentType 'application/json' -Body $login
$headers = @{Authorization="Bearer $($token.accessToken)"}
$ready = Invoke-RestMethod "$BaseUrl/health/ready"
Assert-That ($ready.database -eq 'ready') 'PostgreSQL ready'
$players = @('Alpha','Beta','Gamma') | ForEach-Object {
    Invoke-RestMethod "$BaseUrl/players" -Headers $headers -Method Post -ContentType 'application/json' -Body (@{name=$_} | ConvertTo-Json)
}
$t = Invoke-RestMethod "$BaseUrl/tournaments" -Headers $headers -Method Post -ContentType 'application/json' -Body (@{name='Smoke Cup';players=@($players.id)} | ConvertTo-Json)
Assert-That ($t.matches.Count -eq 3 -and $t.version -eq 1) 'round-robin tournament created'
$url = "$BaseUrl/tournaments/$($t.id)/matches/$($t.matches[0].id)/result"
$changed = Invoke-RestMethod $url -Headers $headers -Method Put -ContentType 'application/json' -Body '{"homeScore":8,"awayScore":3,"expectedVersion":1}'
Assert-That ($changed.version -eq 2) 'result changes aggregate version'
$stale = Invoke-WebRequest $url -Headers $headers -Method Put -ContentType 'application/json' -Body '{"homeScore":1,"awayScore":8,"expectedVersion":1}' -SkipHttpErrorCheck
Assert-That ($stale.StatusCode -eq 409) 'stale result returns 409'
$null = Invoke-RestMethod "$BaseUrl/tournaments/$($t.id)/standings"
$null = Invoke-RestMethod $url -Headers $headers -Method Put -ContentType 'application/json' -Body '{"homeScore":1,"awayScore":8,"expectedVersion":2}'
$standing = Invoke-RestMethod "$BaseUrl/tournaments/$($t.id)/standings"
Assert-That ($standing[0].playerId -eq $t.matches[0].awayId -and $standing[0].wins -eq 1) 'correction bypasses old cache version'
$invalid = Invoke-WebRequest $url -Headers $headers -Method Put -ContentType 'application/json' -Body '{"homeScore":3,"awayScore":3,"expectedVersion":3}' -SkipHttpErrorCheck
Assert-That ($invalid.StatusCode -eq 400) 'invalid draw returns 400'
$globalReady = $false
for ($attempt = 0; $attempt -lt 50; $attempt++) {
    $board = Invoke-RestMethod "$BaseUrl/statistics/leaderboard"
    $row = @($board.players | Where-Object playerId -eq $t.matches[0].awayId)
    if ($row.Count -eq 1 -and $row[0].wins -eq 1) { $globalReady=$true; break }
    Start-Sleep -Milliseconds 400
}
Assert-That $globalReady 'outbox -> Kafka -> statistics projection'
$training = @{playerId=$players[0].id;exercise='Straight shots';performedAt=[DateTime]::UtcNow.ToString('o');durationMinutes=30;attempts=20;successfulAttempts=14} | ConvertTo-Json
$recorded = Invoke-RestMethod "$BaseUrl/trainings" -Headers $headers -Method Post -ContentType 'application/json' -Body $training
Assert-That ($recorded.successfulAttempts -eq 14) 'training saved'
$export = Invoke-WebRequest "$BaseUrl/tournaments/$($t.id)/standings.xlsx"
Assert-That ($export.RawContentLength -gt 100 -and $export.Headers['Content-Type'] -like '*spreadsheetml*') 'Excel report exported'
$csv = Invoke-WebRequest "$BaseUrl/tournaments/$($t.id)/standings.csv"
Assert-That ($csv.Headers['Content-Type'] -like 'text/csv*') 'CSV report exported'
$account = @{email=('smoke-'+[Guid]::NewGuid().ToString('N')+'@example.test');password='Smoke-Only!12345'}
$playerToken = Invoke-RestMethod "$BaseUrl/auth/register" -Method Post -ContentType 'application/json' -Body ($account | ConvertTo-Json)
$playerHeaders = @{Authorization="Bearer $($playerToken.accessToken)"}
$forbidden = Invoke-WebRequest "$BaseUrl/players" -Headers $playerHeaders -Method Post -ContentType 'application/json' -Body '{"name":"Forbidden"}' -SkipHttpErrorCheck
Assert-That ($forbidden.StatusCode -eq 403) 'self-registered player cannot manage tournaments'
$missing = Invoke-WebRequest "$BaseUrl/tournaments/00000000-0000-0000-0000-000000000001" -SkipHttpErrorCheck
Assert-That ($missing.StatusCode -eq 404) 'missing tournament returns 404'
Write-Output "Smoke tournament: $($t.id)"
Write-Output 'Full-stack smoke checks passed.'
