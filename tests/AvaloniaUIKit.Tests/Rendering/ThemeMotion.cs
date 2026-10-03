using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// Removes the keyframe animations from this test's theme instance, so a
/// control shows a fixed frame that a test can pose. Each test runs in its own
/// application and theme (per-test isolation), so nothing leaks between tests.
/// </summary>
public static class ThemeMotion
{
    public static void StripAnimations()
    {
        foreach (var styles in Application.Current!.Styles.OfType<Styles>())
        {
            Strip(styles.Resources, []);
        }
    }

    private static void Strip(IResourceDictionary dictionary, HashSet<object> seen)
    {
        if (!seen.Add(dictionary))
        {
            return;
        }
        foreach (var key in dictionary.Keys.ToList())
        {
            if (dictionary.TryGetValue(key, out var value) && value is ControlTheme theme)
            {
                StripStyle(theme);
            }
        }
        foreach (var merged in dictionary.MergedDictionaries)
        {
            if (merged is IResourceDictionary nested)
            {
                Strip(nested, seen);
            }
            else if (merged is MergeResourceInclude include && include.Loaded is IResourceDictionary loaded)
            {
                Strip(loaded, seen);
            }
        }
        foreach (var themed in dictionary.ThemeDictionaries.Values.OfType<IResourceDictionary>())
        {
            Strip(themed, seen);
        }
    }

    private static void StripStyle(IStyle style)
    {
        if (style is StyleBase b)
        {
            b.Animations.Clear();
            foreach (var child in b.Children)
            {
                StripStyle(child);
            }
        }
    }
}
