using System.Globalization;
using TUnit.Core.Interfaces;

[assembly: AvaloniaUIKit.Tests.Infrastructure.Shard]

namespace AvaloniaUIKit.Tests.Infrastructure;

/// <summary>
/// Splits the tests between processes run side by side (scripts/verify.sh): a
/// process hosts one headless Avalonia session, so tests never run in parallel
/// within it (ADR 12). With <c>AVALONIA_UIKIT_SHARDS=N</c> set, every test gets
/// a <c>Shard</c> property from 0 to N - 1, the same in every process, and each
/// process runs its own with <c>--treenode-filter "/*/*/*/*[Shard=k]"</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ShardAttribute : Attribute, ITestDiscoveryEventReceiver
{
    private static readonly uint Count =
        uint.TryParse(Environment.GetEnvironmentVariable("AVALONIA_UIKIT_SHARDS"), CultureInfo.InvariantCulture, out var n) ? n : 0;

    public int Order => 0;

    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        if (Count > 1)
        {
            var name = $"{context.TestDetails.ClassType.FullName}.{context.GetDisplayName()}";
            context.AddProperty("Shard", (Hash(name) % Count).ToString(CultureInfo.InvariantCulture));
        }
        return ValueTask.CompletedTask;
    }

    /// <summary>FNV-1a: unlike <see cref="string.GetHashCode()"/>, the same in every process.</summary>
    private static uint Hash(string s)
    {
        var hash = 2166136261u;
        foreach (var c in s)
        {
            hash = (hash ^ c) * 16777619u;
        }
        return hash;
    }
}
