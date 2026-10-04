using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using AvaloniaUIKit.Demos;
using Dock.Settings;

[assembly: SupportedOSPlatform("browser")]

namespace AvaloniaUIKit.Browser;

/// <summary>
/// Starts one .NET runtime per page and waits: the site's loader
/// (sites/app/avalonia-demo.ts) then mounts demos into its own elements
/// through <see cref="Demos"/>. See docs/site.md.
/// </summary>
internal static partial class Program
{
    // Dock binds its splitters' CanResize and ResizePreview by name: keep them on the
    // splitter model the Dock demos declare (Dock.Model.Avalonia) through trimming.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Dock.Model.Avalonia.Controls.ProportionalDockSplitter))]
    public static Task Main(string[] args)
    {
        // A page has no windows: Dock draws floating windows inside the dock control.
        DockSettings.UseManagedWindows = true;
        return BuildAvaloniaApp()
            .LogToTrace()
            .SetupBrowserAppAsync(new BrowserPlatformOptions
            {
                RenderingMode = [BrowserRenderingMode.WebGL2, BrowserRenderingMode.WebGL1, BrowserRenderingMode.Software2D],
            });
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().WithDemoFonts();
}
