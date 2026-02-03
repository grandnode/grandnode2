# Génère les PNG des diagrammes Mermaid (mermaid-cli installé en global)
# Usage : depuis docs/mermaid, .\render.ps1   ou   depuis racine, .\docs\mermaid\render.ps1

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$outDir = Join-Path $scriptDir 'out'

if (-not (Get-Command mmdc -ErrorAction SilentlyContinue)) {
  Write-Host 'mermaid-cli non trouvé. Installer avec: npm install -g @mermaid-js/mermaid-cli' -ForegroundColor Red
  exit 1
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$files = Get-ChildItem -Path $scriptDir -Filter '*.mmd'
foreach ($f in $files) {
  $base = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
  $outPath = Join-Path $outDir ($base + '.png')
  Write-Host ('Génération: ' + $f.Name + ' -> out/' + $base + '.png')
  & mmdc -i $f.FullName -o $outPath
}

Write-Host ('Terminé. Images dans: ' + $outDir)
