# Scenario 1k users - test local (ramp 2m -> 1000 VUs, palier 3m, ramp down 1m)
# Usage: from load-test folder, .\run-1k.ps1
# GrandNode must be running: docker compose up -d (from project root)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if ($env:BASE_URL) {
  $baseUrl = $env:BASE_URL
} else {
  $baseUrl = 'http://127.0.0.1:8080'
}

if ($env:BASE_URL) {
  $baseUrlDocker = $env:BASE_URL
} else {
  $baseUrlDocker = 'http://host.docker.internal:8080'
}

if (Get-Command k6 -ErrorAction SilentlyContinue) {
  k6 run -e SCENARIO=1k -e ('BASE_URL=' + $baseUrl) ($scriptDir + '\k6\storefront.js')
} else {
  Write-Host ('k6 not installed - running via Docker. BASE_URL=' + $baseUrlDocker) -ForegroundColor Yellow
  $volume = $scriptDir + ':/scripts'
  $envArg = 'BASE_URL=' + $baseUrlDocker
  & docker run --rm -v $volume grafana/k6 run -e SCENARIO=1k -e $envArg /scripts/k6/storefront.js
}
