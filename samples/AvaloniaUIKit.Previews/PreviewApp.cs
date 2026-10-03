using Avalonia;
using Avalonia.Headless;
using AvaloniaUIKit.Demos;

namespace AvaloniaUIKit.Previews;

/// <summary>The headless application the previews render in: the themes, Skia, the bundled Inter.</summary>
public sealed class PreviewApp : Application
{
    public override void Initialize() => DemoApp.ApplyTheme(this);

    public static AppBuilder BuildAvaloniaApp()
    {
        // The demos' labels are English: month and weekday names come from the culture.
        var english = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = english;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = english;
        return AppBuilder.Configure<PreviewApp>()
            .UseSkia()
            .UseHarfBuzz()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithDemoFonts();
    }
}
