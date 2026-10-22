using Recorder.Capture;

namespace Recorder;

// Stand-in for the plugin host; the production reflection scheduler is linked unchanged.
internal static class Plugin
{
    public static Host PluginInterface { get; } = new();
    internal sealed class Host { public object UiBuilder { get; set; } = new(); }
}

internal static class SchedulerChecks
{
    public static void Run()
    {
        var manager = new Manager();
        var builder = new Builder();
        Plugin.PluginInterface.UiBuilder = builder;
        var scheduler = new DalamudPostImGuiScheduler();
        int captures = 0;
        if (scheduler.TryEnqueue(() => captures++, out _)) throw new Exception("Scene not ready must fall back.");
        builder.Scene = manager;
        if (!scheduler.TryEnqueue(() => captures++, out _)) throw new Exception("Ready scene must schedule.");
        if (captures != 0) throw new Exception("Capture ran before post-ImGui drain.");
        manager.Drain();
        if (captures != 1) throw new Exception("Post-ImGui capture was lost.");
        Plugin.PluginInterface.UiBuilder = new object();
        if (new DalamudPostImGuiScheduler().TryEnqueue(() => captures++, out _))
            throw new Exception("Missing internal API must fall back.");
        Plugin.PluginInterface.UiBuilder = new FieldBuilder(manager);
        if (!new DalamudPostImGuiScheduler().TryEnqueue(() => captures++, out _))
            throw new Exception("Legacy field accessor must work.");
        manager.Drain();
        if (captures != 2) throw new Exception("Legacy queue did not capture.");
        Console.WriteLine("PASS: post-ImGui deferral, scene retry, missing API fallback and legacy field lookup.");
    }

    private sealed class Builder
    {
        public Manager? Scene;
        private Manager? InterfaceManagerWithScene => Scene;
    }
    private sealed class FieldBuilder(Manager manager)
    {
        public readonly Manager interfaceManager = manager;
    }
    private sealed class Manager
    {
        private readonly Queue<Action> _queue = new();
        public Task RunAfterImGuiRender(Action action)
        {
            var completion = new TaskCompletionSource();
            _queue.Enqueue(() => { action(); completion.SetResult(); });
            return completion.Task;
        }
        public void Drain() { while (_queue.TryDequeue(out var action)) action(); }
    }
}
