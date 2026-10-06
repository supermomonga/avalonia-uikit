using Avalonia.Headless;
using Avalonia.Threading;
using TUnit.Core.Interfaces;

[assembly: TUnit.Core.Executors.TestExecutor<AvaloniaUIKit.Demo.ControlCatalog.Tests.Infrastructure.AvaloniaHeadlessExecutor>]
[assembly: NotInParallel]
[assembly: Timeout(120_000)]

namespace AvaloniaUIKit.Demo.ControlCatalog.Tests.Infrastructure;

/// <summary>Runs every test body on the Avalonia headless UI thread, in one application.</summary>
public sealed class AvaloniaHeadlessExecutor : ITestExecutor
{
    private static readonly Lazy<HeadlessUnitTestSession> Session = new(() =>
        HeadlessUnitTestSession.StartNew(typeof(TestApp), AvaloniaTestIsolationLevel.PerAssembly));

    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action) =>
        new(Session.Value.Dispatch(async () =>
        {
            await action();
            Dispatcher.UIThread.RunJobs();
            return 0;
        }, CancellationToken.None));
}
