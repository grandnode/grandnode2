# Run k6 90k-style load test (ramp to 3k VUs over ~1h).
# Usage: from load-test folder, .\run-90k.ps1
# Ensure GrandNode is up: docker compose up -d (from project root)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$baseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://127.0.0.1:8080" }
$baseUrlDocker = if ($env:BASE_URL) { $env:BASE_URL } else { "http://host.docker.internal:8080" }

if (Get-Command k6 -ErrorAction SilentlyContinue) {
  k6 run -e SCENARIO=90k -e "BASE_URL=$baseUrl" "$scriptDir\k6\storefront.js"
} else {
  Write-Host "k6 not found – running via Docker (BASE_URL=$baseUrlDocker)." -ForegroundColor Yellow
  docker run --rm -v "${scriptDir}:/scripts" grafana/k6 run -e SCENARIO=90k -e "BASE_URL=$baseUrlDocker" /scripts/k6/storefront.js
}
