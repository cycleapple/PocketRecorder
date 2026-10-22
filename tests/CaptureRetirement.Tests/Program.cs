using Recorder.Capture;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

// Model a Present call held by the graphics driver beyond the former timeout.
using var presentReturned = new ManualResetEventSlim(false);
var retirement = new CaptureRetirement();
int releases = 0;
var completion = retirement.Retire(presentReturned.IsSetPredicate, () => Interlocked.Increment(ref releases));
await Task.Delay(600);
Check(!completion.IsCompleted && releases == 0, "Released an active Present trampoline after timeout.");
var duplicate = retirement.Retire(() => true, () => throw new Exception("Duplicate release."));
Check(ReferenceEquals(completion, duplicate), "Concurrent Stop/Dispose must share retirement.");
presentReturned.Set();
await completion.WaitAsync(TimeSpan.FromSeconds(5));
Check(releases == 1, "Resources were not released exactly once after Present returned.");

var idle = new CaptureRetirement();
int idleReleases = 0;
await idle.Retire(() => true, () => idleReleases++).WaitAsync(TimeSpan.FromSeconds(5));
Check(idleReleases == 1, "Idle capture was not released.");
Console.WriteLine("PASS: long-running Present retains resources; retirement is idempotent; idle and completed captures are released.");
Recorder.SchedulerChecks.Run();

static class EventExtensions
{
    public static bool IsSetPredicate(this ManualResetEventSlim value) => value.IsSet;
}
