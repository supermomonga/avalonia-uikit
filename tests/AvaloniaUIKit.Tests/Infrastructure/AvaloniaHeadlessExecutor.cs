using Avalonia.Headless;
using Avalonia.Threading;
using TUnit.Core.Interfaces;

[assembly: TUnit.Core.Executors.TestExecutor<AvaloniaUIKit.Tests.Infrastructure.AvaloniaHeadlessExecutor>]
[assembly: NotInParallel]

namespace AvaloniaUIKit.Tests.Infrastructure;

/// <summary>
/// Runs every test body on the Avalonia headless UI thread, in a fresh
/// application instance (per-test isolation).
/// </summary>
public sealed class AvaloniaHeadlessExecutor : ITestExecutor
{
    private static readonly Lazy<HeadlessUnitTestSession> Session = new(() =>
        HeadlessUnitTestSession.StartNew(typeof(TestApp), AvaloniaTestIsolationLevel.PerTest));

    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action) =>
        new(Session.Value.Dispatch(async () =>
        {
            await action();
            Dispatcher.UIThread.RunJobs();
            return 0;
        }, CancellationToken.None));
}
