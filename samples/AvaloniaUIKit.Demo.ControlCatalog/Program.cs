using Avalonia;
using AvaloniaUIKit.Demos;

namespace AvaloniaUIKit.Demo.ControlCatalog;

/// <summary>
/// The control catalog: every demo of the documentation site, live, with its XAML and
/// a property grid that edits the demo's controls (docs/control-catalog.md).
/// <c>--component &lt;slug&gt;</c> opens a component, <c>--theme &lt;name&gt;</c> a GPUI Kit theme.
/// </summary>
internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        App.Options = CatalogOptions.Parse(args);
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithDemoFonts()
#if DEBUG
            .WithDeveloperTools()
#endif
            .LogToTrace();
}
