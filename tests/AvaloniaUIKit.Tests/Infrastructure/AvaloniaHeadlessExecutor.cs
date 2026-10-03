using Avalonia.Headless;
using Avalonia.Threading;
using TUnit.Core.Interfaces;

[assembly: TUnit.Core.Executors.TestExecutor<AvaloniaUIKit.Tests.Infrastructure.AvaloniaHeadlessExecutor>]
[assembly: NotInParallel]
[assembly: Timeout(120_000)]

namespace AvaloniaUIKit.Tests.Infrastructure;

/// <summary>
/// Runs every test body on the Avalonia headless UI thread, in a fresh
/// application instance (per-test isolation) whose time only moves when the
/// test advances it (<see cref="VirtualTime"/>).
/// </summary>
public sealed class AvaloniaHeadlessExecutor : ITestExecutor
{
    private static readonly Lazy<HeadlessUnitTestSession> Session = new(() =>
        HeadlessUnitTestSession.StartNew(typeof(TestApp), AvaloniaTestIsolationLevel.PerTest));

    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action) =>
        new(Session.Value.Dispatch(async () =>
        {
            VirtualTime.Install();
            await action();
            Dispatcher.UIThread.RunJobs();
            return 0;
        }, CancellationToken.None));
}
