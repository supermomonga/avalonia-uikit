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

    /// <summary>Creates a view in the element with <paramref name="hostId"/> and shows the demo <paramref name="demoId"/> in it.</summary>
    [JSExport]
    public static bool Mount(string hostId, string demoId)
    {
        if (!DemoRegistry.Factories.TryGetValue(demoId, out var factory) || Views.ContainsKey(hostId))
        {
            return false;
        }
        try
        {
            var view = new AvaloniaView(hostId) { Content = factory() };
            Views[hostId] = view;
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"mount {demoId} into #{hostId}: {e}");
            return false;
        }
    }

    /// <summary>Switches every view between the light and dark theme.</summary>
    [JSExport]
    public static void SetTheme(bool dark)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}
