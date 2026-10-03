namespace AvaloniaUIKit;

/// <summary>
/// A physical spring, as GPUI Kit's <c>gpui_base::Spring</c> defines one: a
/// damped harmonic oscillator that keeps its velocity when its target changes,
/// so a value reversed mid-flight turns around instead of restarting.
/// </summary>
/// <remarks>
/// The step is the exact solution GPUI integrates (<c>SpringConfig::step</c>),
/// so a value advanced over any number of frames lands where GPUI's does.
/// </remarks>
public sealed class Spring
{
    private const double CriticalDampingTolerance = 1e-4;

    /// <summary>The period one full oscillation would take without damping.</summary>
    public TimeSpan Response { get; set; } = TimeSpan.FromMilliseconds(180);

    /// <summary>The damping ratio: 1 reaches the target without overshooting it.</summary>
    public double Damping { get; set; } = 1;

    /// <summary>How close, in the value's own units, counts as settled.</summary>
    public double Epsilon { get; set; } = 0.001;

    private double Frequency => 2 * Math.PI / Response.TotalSeconds;

    /// <summary>Advances <paramref name="state"/> toward a fixed <paramref name="target"/> over <paramref name="seconds"/>.</summary>
    internal SpringState Step(SpringState state, double target, double seconds)
    {
        var w = Frequency;
        var z = Damping;
        double p00, p01, p10, p11;
        if (z < 1 - CriticalDampingTolerance)
        {
            var decay = z * w;
            var damped = w * Math.Sqrt(1 - z * z);
            var e = Math.Exp(-decay * seconds);
            var (sin, cos) = Math.SinCos(damped * seconds);
            var sinOverFrequency = sin / damped;
            p00 = e * (cos + decay * sinOverFrequency);
            p01 = e * sinOverFrequency;
            p10 = -e * w * w * sinOverFrequency;
            p11 = e * (cos - decay * sinOverFrequency);
        }
        else if (z > 1 + CriticalDampingTolerance)
        {
            var root = Math.Sqrt(z * z - 1);
            var slow = -w / (z + root);
            var fast = -w * (z + root);
            var denominator = slow - fast;
            var slowE = Math.Exp(slow * seconds);
            var fastE = Math.Exp(fast * seconds);
            p00 = (-fast * slowE + slow * fastE) / denominator;
            p01 = (slowE - fastE) / denominator;
            p10 = slow * fast * (fastE - slowE) / denominator;
            p11 = (slow * slowE - fast * fastE) / denominator;
        }
        else
        {
            var e = Math.Exp(-w * seconds);
            p00 = e * (1 + w * seconds);
            p01 = e * seconds;
            p10 = -e * w * w * seconds;
            p11 = e * (1 - w * seconds);
        }
        var displacement = state.Position - target;
        return new SpringState(target + p00 * displacement + p01 * state.Velocity, p10 * displacement + p11 * state.Velocity);
    }

    /// <summary>Whether the value is within the tolerance of its target and moving slower than it per radian.</summary>
    internal bool IsSettled(SpringState state, double target) =>
        Math.Abs(state.Position - target) <= Epsilon && Math.Abs(state.Velocity) <= Epsilon * Frequency;
}

/// <summary>Where a spring is and how fast it moves, in the value's units (per second).</summary>
internal readonly record struct SpringState(double Position, double Velocity);
