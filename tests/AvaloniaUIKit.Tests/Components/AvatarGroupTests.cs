using Avalonia;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Components;

public class AvatarGroupTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["avatargroup"])]
    public Task AvatarGroup_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden, excluded: FallbackGlyphs);
        return Task.CompletedTask;
    }

    // R33: the ellipsis avatar's "⋯" (U+22EF) is not in the bundled Inter, so
    // each renderer draws it in its own fallback font. Its pixels inside the
    // ring are not compared; its color, and the avatar's fill and ring, are.
    private static IEnumerable<Rect> FallbackGlyphs(CaseHost host) =>
        host.Window.GetVisualDescendants().OfType<Avatar>()
            .Where(a => a.Name == "PART_Ellipsis" && a.IsEffectivelyVisible)
            .Select(a => a.GetTransformedBounds() is { } b ? b.Clip.Deflate(1.5) : default);
}
