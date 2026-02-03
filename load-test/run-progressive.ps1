# Scénario progressif 5K -> 20K -> 50K (durée ~45 min)
# Usage : depuis le dossier load-test, .\run-progressive.ps1
# GrandNode doit tourner : docker compose up -d (à la racine du projet)

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
  k6 run -e SCENARIO=progressive -e ('BASE_URL=' + $baseUrl) ($scriptDir + '\k6\storefront.js')
} else {
  Write-Host ('k6 non installé – lancement via Docker. BASE_URL=' + $baseUrlDocker) -ForegroundColor Yellow
  $volume = $scriptDir + ':/scripts'
  $envArg = 'BASE_URL=' + $baseUrlDocker
  & docker run --rm -v $volume grafana/k6 run -e SCENARIO=progressive -e $envArg /scripts/k6/storefront.js
}
