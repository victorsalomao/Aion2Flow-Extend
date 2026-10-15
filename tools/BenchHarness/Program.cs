// Bench harness (nao-producao): reproduz logs de captura via PacketLogReplayService e mede memoria/GC/CPU.
// Uso: BenchHarness <idle|fights|session> <fixturesDir> [count] [holdSeconds] [pidFile]
using System.Diagnostics;using System.Globalization;
using Cloris.Aion2Flow.Capture.Diagnostics;
using Cloris.Aion2Flow.SceneRuntime.Archive;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var scenario = args.Length > 0 ? args[0] : "idle";
var dir = args.Length > 1 ? args[1] : ".";
var count = args.Length > 2 ? int.Parse(args[2]) : 10;
var hold = args.Length > 3 ? int.Parse(args[3]) : 0;
var proc = Process.GetCurrentProcess();
var sw = Stopwatch.StartNew();
var startMs = (DateTime.Now - proc.StartTime).TotalMilliseconds;

static double Mb(long b) => Math.Round(b / 1048576.0, 1);
static (double ws, double heap) Snap(bool collect)
{
    if (collect) { GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(2, GCCollectionMode.Forced, true, true); }
    var p = Process.GetCurrentProcess();
    return (Mb(p.WorkingSet64), Mb(GC.GetTotalMemory(false)));
}

var results = new List<PacketLogReplayResult>();
var wsSamples = new List<double>();
var cpu0 = proc.TotalProcessorTime;
var files = Directory.GetFiles(dir, "*.log").OrderByDescending(f => new FileInfo(f).Length).Take(count).ToArray();
var (wsIdle, heapIdle) = Snap(true);
var lines = 0L;
var archive = scenario.StartsWith("archive") ? new EncounterArchiveService(() => scenario == "archive-spill") : null;
if (scenario != "idle")
{
    foreach (var f in files)
    {
        var r = PacketLogReplayService.Replay(f, Environment.GetEnvironmentVariable("BENCH_LITE") == "1");
        if (scenario.StartsWith("archive")) { archive!.Archive(r.SceneOwner.CreateArchivePayload(), "bench", false); }
        else results.Add(r);
        lines += r.TotalLines;
        wsSamples.Add(Snap(false).ws);
    }
}
var cpuMs = (proc.TotalProcessorTime - cpu0).TotalMilliseconds;
var wallMs = sw.Elapsed.TotalMilliseconds;
var (wsPeak, _) = Snap(false);
proc.Refresh();
var peakWs = Mb(proc.PeakWorkingSet64);
var (wsAfter, heapAfter) = Snap(true);
if (hold > 0)
{
    File.WriteAllText(Path.Combine(Path.GetTempPath(), "benchharness.pid"), Environment.ProcessId.ToString());
    Console.Error.WriteLine($"HOLD pid={Environment.ProcessId}");
    Thread.Sleep(hold * 1000);
}
GC.KeepAlive(results); GC.KeepAlive(archive);
Console.WriteLine($"scenario={scenario},fights={files.Length},lines={lines},startup_ms={startMs:F0},ws_idle_mb={wsIdle},heap_idle_mb={heapIdle},ws_peak_mb={peakWs},ws_after_gc_mb={wsAfter},heap_after_gc_mb={heapAfter},cpu_ms={cpuMs:F0},wall_ms={wallMs:F0},cpu_pct={(wallMs > 0 ? cpuMs / wallMs * 100 : 0):F1},gen0={GC.CollectionCount(0)},gen1={GC.CollectionCount(1)},gen2={GC.CollectionCount(2)},alloc_mb={Mb(GC.GetTotalAllocatedBytes())}");


