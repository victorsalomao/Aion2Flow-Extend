<#
.SYNOPSIS  Bench reproduzivel: replay dos logs de fixtures + metricas de memoria/CPU/GC.
.EXAMPLE   pwsh tools/bench.ps1 -Scenario idle
.EXAMPLE   pwsh tools/bench.ps1 -Scenario fights -Fights 10 -Runs 2
.NOTES     Cenarios: idle (cena vazia), fights (N logs em sequencia = N lutas), session (alias de fights, 5 logs).
           Limitacao: roda o pipeline real (parse + SceneRuntime + Journal) sem UI/WinDivert; nao mede Avalonia.
           Saida: tabela + tools/bench-out/bench-<scenario>-<timestamp>.csv
#>
param(
    [ValidateSet('idle', 'fights', 'session', 'archive-ram', 'archive-spill')][string]$Scenario = 'idle',
    [int]$Fights = 10,
    [int]$Runs = 2,
    [switch]$Lite,
    [int]$HoldSeconds = 0   # mantem o processo vivo para dotnet-gcdump/dotnet-counters (pid em %TEMP%\benchharness.pid)
)
$ErrorActionPreference = 'Stop'
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + $env:Path
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $PSScriptRoot 'bench-out'; New-Item -ItemType Directory -Force $out | Out-Null
$bin = Join-Path $env:TEMP 'aion2-benchharness'
dotnet build "$root/tools/BenchHarness" -c Release -o $bin --nologo -v q | Out-Null
if ($LASTEXITCODE) { throw 'build falhou' }
$fix = "$root/tests/Aion2Flow.ReplayTests/Fixtures/logs"
if ($Scenario -eq 'session') { $Fights = 5 }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$env:BENCH_LITE = if ($Lite) { '1' } else { '0' }
$rows = @()
for ($i = 1; $i -le $Runs; $i++) {
    $mode = if ($Scenario -eq 'idle') { 'idle' } elseif ($Scenario -like 'archive*') { $Scenario } else { 'fights' }
    $line = & dotnet "$bin/BenchHarness.dll" $mode $fix $Fights $HoldSeconds
    $kv = [ordered]@{ run = $i }
    foreach ($p in ($line -split ',')) { $k, $v = $p -split '=', 2; $kv[$k] = $v }
    $rows += [pscustomobject]$kv
}
$csv = Join-Path $out "bench-$Scenario$(if ($Lite) { "-lite" })-$stamp.csv"
$rows | Export-Csv $csv -NoTypeInformation
$rows | Format-Table -AutoSize
foreach ($m in 'ws_after_gc_mb', 'ws_peak_mb', 'cpu_pct', 'startup_ms') {
    $v = $rows | ForEach-Object { [double]$_.$m }
    if ($v.Count -ge 2 -and $v[0] -gt 0) { '{0}: variacao entre runs = {1:P1}' -f $m, ([math]::Abs($v[1] - $v[0]) / $v[0]) }
}
"CSV: $csv"
