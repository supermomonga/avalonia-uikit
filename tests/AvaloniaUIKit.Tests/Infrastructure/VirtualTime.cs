using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls.Platform;
using Avalonia.Threading;

namespace AvaloniaUIKit.Tests.Infrastructure;

/// <summary>
/// A clock the tests move by hand, the way GPUI's test executor does, so a
/// control can be rendered at any moment of a motion.
/// </summary>
/// <remarks>
/// Avalonia has no public way to set the time, so this reaches into its
/// internals (tests only; the theme itself uses no reflection):
/// <list type="bullet">
/// <item>Transitions and animations read the inherited <c>Animatable.Clock</c>.
/// Each test window gets a clock that only moves when <see cref="Advance"/> pulses it.</item>
/// <item><see cref="DispatcherTimer"/>s (a tooltip's show delay, a scrollbar's hide
/// delay, a caret's blink) read the dispatcher's time. Its source is replaced, the
/// dispatcher loop's own stopwatch is stopped so it never fires a timer by itself,
/// and due timers are promoted as the time passes.</item>
/// </list>
/// Each test runs in its own dispatcher (per-test isolation), so
/// <see cref="Install"/> runs at the start of every test.
/// </remarks>
public static class VirtualTime
{
    private const string ClockBaseType = "Avalonia.Animation.ClockBase, Avalonia.Base";

    private static long s_milliseconds;
    private static object? s_clock;

    /// <summary>The current time, in milliseconds since the test started.</summary>
    public static long Now => s_milliseconds;

    /// <summary>Makes the current dispatcher and new test windows run on the virtual clock.</summary>
    public static void Install()
    {
        var dispatcher = Dispatcher.UIThread;
        s_milliseconds = 0;
        TimeProvider(dispatcher) = static () => s_milliseconds;
        if (ImplField.GetValue(dispatcher) is ManagedDispatcherImpl impl)
        {
            LoopClock(impl) = new Stopwatch();
        }
        s_clock = NewClock();
        Pulse(s_clock, TimeSpan.Zero);
    }

    /// <summary>Runs <paramref name="target"/>'s transitions and animations on the virtual clock.</summary>
    public static void Attach(AvaloniaObject target) =>
        target.SetValue(ClockProperty.Value, s_clock ?? throw new InvalidOperationException("VirtualTime is not installed"));

    /// <summary>Lets <paramref name="by"/> pass, a millisecond at a time: timers fire when due and every motion moves.</summary>
    public static void Advance(TimeSpan by)
    {
        var end = s_milliseconds + (long)Math.Round(by.TotalMilliseconds);
        while (s_milliseconds < end)
        {
            s_milliseconds++;
            Tick();
        }
    }

    /// <summary>Fires the timers due now and lets motions that just started see their first moment.</summary>
    public static void Tick()
    {
        var dispatcher = Dispatcher.UIThread;
        PromoteTimers(dispatcher);
        dispatcher.RunJobs();
        if (s_clock is not null)
        {
            Pulse(s_clock, TimeSpan.FromMilliseconds(s_milliseconds));
        }
        dispatcher.RunJobs();
    }

    private static readonly Lazy<AvaloniaProperty> ClockProperty = new(() =>
        AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(Animatable), "Clock")
            ?? throw new InvalidOperationException("Animatable.Clock is gone: Avalonia changed its animation clock"));

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_timeProvider")]
    private static extern ref Func<long> TimeProvider(Dispatcher dispatcher);

    // The field's type (IDispatcherImpl) is not in Avalonia's reference assemblies.
    private static readonly FieldInfo ImplField =
        typeof(Dispatcher).GetField("_impl", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Dispatcher._impl is gone: Avalonia changed its dispatcher");

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "PromoteTimers")]
    private static extern void PromoteTimers(Dispatcher dispatcher);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_clock")]
    private static extern ref Stopwatch LoopClock(ManagedDispatcherImpl impl);

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    [return: UnsafeAccessorType(ClockBaseType)]
    private static extern object NewClock();

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Pulse")]
    private static extern void Pulse([UnsafeAccessorType(ClockBaseType)] object clock, TimeSpan systemTime);
}
