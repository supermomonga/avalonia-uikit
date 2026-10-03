using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace AvaloniaUIKit.Tests.Infrastructure;

/// <summary>
/// The application every test runs in: the GPUI theme, rendered by Skia
/// (not the headless stub), with the bundled Inter fonts the GPUI reference
/// renders with.
/// </summary>
public sealed class TestApp : Application
{
    /// <summary>The family the GPUI reference harness sets as the theme font.</summary>
    public const string FontFamilyName = "fonts:Inter#Inter";

    public override void Initialize()
    {
        Styles.Add(new GpuiTheme());
        Resources["Gpui.FontFamily"] = new FontFamily(FontFamilyName);
        // GPUI opens a submenu as soon as its item is hovered (usage contract, docs/testing.md).
        Avalonia.Controls.Platform.DefaultMenuInteractionHandler.MenuShowDelay = TimeSpan.Zero;
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        // GPUI's labels are English: month and weekday names come from the culture.
        var english = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = english;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = english;
        System.Globalization.CultureInfo.CurrentCulture = english;
        System.Globalization.CultureInfo.CurrentUICulture = english;
        return Configure();
    }

    private static AppBuilder Configure() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHarfBuzz()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .ConfigureFonts(fonts => fonts.AddFontCollection(new EmbeddedFontCollection(
                new Uri("fonts:Inter", UriKind.Absolute),
                new Uri("avares://AvaloniaUIKit.Tests/Assets/Fonts", UriKind.Absolute))))
            .With(new FontManagerOptions { DefaultFamilyName = FontFamilyName });
}
