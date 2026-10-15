<# Resume um CSV do dotnet-counters e compara com as metas (PLANO.md secao 5). Sem -Csv usa o mais recente. #>
param([string]$Csv)
$root = Split-Path $PSScriptRoot -Parent
if (-not $Csv) { $Csv = (Get-ChildItem (Join-Path $root 'docs\perf\session-*.csv') | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName }
$rows = Import-Csv $Csv
$cn = ($rows[0].PSObject.Properties.Name | Where-Object { $_ -like 'Counter Name*' }) | Select-Object -First 1
$vn = ($rows[0].PSObject.Properties.Name | Where-Object { $_ -like 'Mean*' -or $_ -eq 'Value' }) | Select-Object -First 1
$inv = [Globalization.CultureInfo]::InvariantCulture
# Nomes novos (.NET 9+/10, bytes) e legados (MB). Soma por timestamp.
function Series($pat, [double]$div = 1) {
  $rows | Where-Object { $_.$cn -match $pat } | Group-Object Timestamp | ForEach-Object {
    $sum = 0.0; foreach ($r in $_.Group) { $v = 0.0; if ([double]::TryParse($r.$vn, 'Float', $inv, [ref]$v)) { $sum += $v } }
    $sum / $div }
}
function Slope($s) { # MB por minuto, regressao linear sobre amostras (1/refresh desconhecido -> usa indice e duracao)
  $n = $s.Count; if ($n -lt 10) { return 0 }
  $sx=0;$sy=0;$sxy=0;$sxx=0; for($i=0;$i -lt $n;$i++){$sx+=$i;$sy+=$s[$i];$sxy+=$i*$s[$i];$sxx+=$i*$i}
  ($n*$sxy-$sx*$sy)/($n*$sxx-$sx*$sx)
}
$t0 = [datetime]::Parse($rows[0].Timestamp,$inv); $t1 = [datetime]::Parse($rows[-1].Timestamp,$inv)
$minutes = [math]::Max(($t1-$t0).TotalMinutes, 0.01)
Write-Host ("Arquivo: $Csv`nDuracao: {0:N1} min, amostras: {1}" -f $minutes, $rows.Count)
$verdict = @{}
foreach ($m in @(@('Working Set','process.memory.working_set|^Working Set'), @('GC Heap','last_collection.heap.size|^GC Heap Size'))) {
  $div = if ($rows[0].$cn -match '^dotnet.') { 1MB } else { 1 }; $s = @(Series $m[1] $div); if (-not $s) { Write-Host "$($m[0]): sem dados"; continue }
  $st = $s | Measure-Object -Minimum -Maximum -Average
  # tendencia: media do ultimo terco vs. segundo terco (ignora aquecimento inicial)
  $k=[int]($s.Count/3); $mid=($s[$k..(2*$k-1)]|Measure-Object -Average).Average; $last=($s[(2*$k)..($s.Count-1)]|Measure-Object -Average).Average
  $perMin = (Slope $s) * ($s.Count/$minutes)
  "{0}: min {1:N1} / max {2:N1} / media {3:N1} MB | final {4:N1} | tendencia {5:+0.00;-0.00} MB/min" -f $m[0],$st.Minimum,$st.Maximum,$st.Average,$s[-1],$perMin | Write-Host
  $verdict[$m[0]] = @{ Max=$st.Maximum; Min=$st.Minimum; Grow=($last -gt $mid*1.10 -and $perMin -gt 0.05) }
}
$ncpu = @(Series 'process\.cpu\.count') | Select-Object -First 1; if (-not $ncpu) { $ncpu = [Environment]::ProcessorCount }
$cpu = @(Series 'process\.cpu\.time' 0.01 | ForEach-Object { $_ / $ncpu }); if (-not $cpu) { $cpu = @(Series '^CPU Usage') }
if ($cpu) { $ca = ($cpu|Measure-Object -Average).Average; "CPU: media {0:N2}% / max {1:N2}%" -f $ca,($cpu|Measure-Object -Maximum).Maximum | Write-Host }
Write-Host "`nMetas (PLANO.md sec. 5):"
function Chk($ok,$txt){ Write-Host ("  [{0}] {1}" -f $(if($ok){'OK'}else{'FALHA'}),$txt) }
if ($verdict['Working Set']) {
  Chk ($verdict['Working Set'].Min -lt 60) ("RAM idle (minimo WS) {0:N1} < 60 MB" -f $verdict['Working Set'].Min)
  Chk ($verdict['Working Set'].Max -lt 100) ("RAM pico {0:N1} < 100 MB" -f $verdict['Working Set'].Max)
  Chk (-not $verdict['Working Set'].Grow) 'WS sem crescimento continuo'
}
if ($verdict['GC Heap']) { Chk (-not $verdict['GC Heap'].Grow) 'GC heap sem crescimento continuo' }
if ($cpu) { Chk ($ca -lt 2) ("CPU media {0:N2}% < 2% (inclui tempo ocioso; avalie so o trecho de luta separadamente)" -f $ca) }
Write-Host '  (Startup < 1 s: medir a parte.)'

