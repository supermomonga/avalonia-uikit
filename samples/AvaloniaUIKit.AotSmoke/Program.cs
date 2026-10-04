using Avalonia;

namespace AvaloniaUIKit.AotSmoke;

/// <summary>
/// A gallery of every control the theme covers, published with NativeAOT to
/// prove the theme needs no reflection. <c>--smoke</c> renders the gallery in
/// light, dark, Aurora Light and Tokyo Night and exits; <c>--dark</c> starts in
/// the dark theme.
/// </summary>
internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        App.Smoke = args.Contains("--smoke");
        App.Dark = args.Contains("--dark");
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .LogToTrace();
}
