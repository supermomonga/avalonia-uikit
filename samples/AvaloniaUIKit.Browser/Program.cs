using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using AvaloniaUIKit.Demos;

[assembly: SupportedOSPlatform("browser")]

namespace AvaloniaUIKit.Browser;

/// <summary>
/// Starts one .NET runtime per page and waits: the site's loader
/// (sites/app/avalonia-demo.ts) then mounts demos into its own elements
/// through <see cref="Demos"/>. See docs/site.md.
/// </summary>
internal static partial class Program
{
    public static Task Main(string[] args) =>
        BuildAvaloniaApp()
            .LogToTrace()
            .SetupBrowserAppAsync(new BrowserPlatformOptions
            {
                RenderingMode = [BrowserRenderingMode.WebGL2, BrowserRenderingMode.WebGL1, BrowserRenderingMode.Software2D],
            });

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().WithDemoFonts();
}
