using System;
using System.Threading.Tasks;

namespace Recorder.Capture;

// The hook must already be disabled before retirement begins. A timeout on the
// calling thread must never authorize freeing a trampoline still in use.
internal sealed class CaptureRetirement
{
    private readonly object _sync = new();
    private Task? _completion;

    public Task Retire(Func<bool> isIdle, Action cleanup)
    {
        lock (_sync)
        {
            return _completion ??= Task.Run(async () =>
            {
                while (!isIdle())
                    await Task.Delay(10).ConfigureAwait(false);
                cleanup();
            });
        }
    }
}
