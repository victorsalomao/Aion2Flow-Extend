<#
.SYNOPSIS  Mede o Aion2Flow durante uma sessao: counters CSV + gcdumps periodicos.
.NOTES     Requer build com EventSource: dotnet build src/Aion2Flow -c Release -p:Measure=true
           (sem isso o CSV sai vazio). Build JIT: a RAM fica acima da do AOT; use para tendencia/vazamento.
.EXAMPLE   .\scripts\measure-session.ps1 -GcDumpMinutes 15
#>
param(
  [string]$ProcessName = 'Aion2Flow',
  [int]$GcDumpMinutes = 15,
  [int]$RefreshSeconds = 1
)
$ErrorActionPreference = 'Stop'
$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')
foreach ($t in 'dotnet-counters','dotnet-gcdump') {
  if (-not (Get-Command $t -ErrorAction SilentlyContinue)) { dotnet tool install -g $t | Out-Null
    $env:Path += ";$env:USERPROFILE\.dotnet\tools" }
}
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'docs\perf'
New-Item -ItemType Directory -Force $out | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$csv = Join-Path $out "session-$stamp.csv"

Write-Host "Aguardando processo $ProcessName..."
while (-not ($p = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1)) { Start-Sleep -Seconds 1 }
$procId = $p.Id
Write-Host "Processo $procId encontrado. CSV: $csv"

$counters = Start-Process dotnet-counters -ArgumentList "collect -p $procId --counters System.Runtime --format csv --refresh-interval $RefreshSeconds -o `"$csv`"" -PassThru -WindowStyle Hidden
$n = 0
function Dump($tag) {
  $f = Join-Path $out ("session-$stamp-gc{0:D2}-$tag.gcdump" -f $script:n)
  Write-Host "gcdump -> $f"
  & dotnet-gcdump collect -p $procId -o $f | Out-Null
  $script:n++
}
$next = (Get-Date).AddMinutes($GcDumpMinutes)
while (Get-Process -Id $procId -ErrorAction SilentlyContinue) {
  if ((Get-Date) -ge $next) { try { Dump 'periodic' } catch { Write-Warning $_ }; $next = (Get-Date).AddMinutes($GcDumpMinutes) }
  Start-Sleep -Seconds 2
}
Write-Host 'Processo encerrou.'
# gcdump final: processo ja saiu; o ultimo periodico serve. Tentamos antes de sair apenas se ainda vivo.
if (-not $counters.HasExited) { Wait-Process -Id $counters.Id -Timeout 10 -ErrorAction SilentlyContinue; if (-not $counters.HasExited) { Stop-Process -Id $counters.Id -Force } }
Write-Host "Resumo: .\scripts\summarize-session.ps1 -Csv `"$csv`""
