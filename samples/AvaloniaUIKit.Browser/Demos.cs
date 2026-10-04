using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Styling;
using AvaloniaUIKit.Demos;

namespace AvaloniaUIKit.Browser;

/// <summary>
/// What the page's JavaScript calls (<c>exports.AvaloniaUIKit.Browser.Demos</c>):
/// mounts a demo into an element, lists the demos, follows the page's theme.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class Demos
{
    private static readonly Dictionary<string, AvaloniaView> Views = new(StringComparer.Ordinal);

    /// <summary>Every demo id, in order.</summary>
    [JSExport]
    public static string[] List() => DemoRegistry.Ids.ToArray();

    /// <summary>
    /// Creates a view in the element with <paramref name="hostId"/> and shows the demo <paramref name="demoId"/> in it,
    /// <paramref name="inset"/> in from the element's edges, calling <paramref name="heightChanged"/> with the height the
    /// demo takes at the width between the insets (<see cref="DemoRoot"/>).
    /// </summary>
    [JSExport]
    public static bool Mount(
        string hostId,
        string demoId,
        double inset,
        [JSMarshalAs<JSType.Function<JSType.Number>>] Action<double> heightChanged)
    {
        if (!DemoRegistry.Factories.TryGetValue(demoId, out var factory) || Views.ContainsKey(hostId))
        {
            return false;
        }
        try
        {
            var view = new AvaloniaView(hostId) { Content = new DemoRoot(factory(), inset, heightChanged) };
            Views[hostId] = view;
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"mount {demoId} into #{hostId}: {e}");
            return false;
        }
    }

    /// <summary>
    /// Shows every view in the theme GPUI Kit calls <paramref name="name"/>: Default Light, Default Dark
    /// or a bundled theme (<see cref="UIKitThemeVariants"/>); an unknown name shows Default Light.
    /// </summary>
    [JSExport]
    public static void SetTheme(string name)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = UIKitThemeVariants.Find(name) ?? ThemeVariant.Light;
        }
    }
}
