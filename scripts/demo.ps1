param([string]$BaseUrl = 'http://127.0.0.1:5080')
# The smoke scenario is also a complete reproducible demonstration.
& (Join-Path $PSScriptRoot 'smoke.ps1') -BaseUrl $BaseUrl
й   