using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace AvaloniaUIKit.Demos;

/// <summary>
/// What every host of the demos (the browser app, the preview renderer)
/// shares: the themes, and the bundled Inter as the UI font.
/// </summary>
public static class DemoApp
{
    /// <summary>The bundled Inter (assets/fonts/inter), as the tests use it.</summary>
    public const string FontFamilyName = "fonts:Inter#Inter";

    /// <summary>Registers the bundled Inter and makes it the default font.</summary>
    public static AppBuilder WithDemoFonts(this AppBuilder builder) =>
        builder
            .ConfigureFonts(fonts => fonts.AddFontCollection(new EmbeddedFontCollection(
                new Uri("fonts:Inter", UriKind.Absolute),
                new Uri("avares://AvaloniaUIKit.Demos/Assets/Fonts", UriKind.Absolute))))
            .With(new FontManagerOptions { DefaultFamilyName = FontFamilyName });

    /// <summary>Adds the themes and points the theme font at Inter.</summary>
    public static void ApplyTheme(Application app)
    {
        app.Styles.Add(new UIKitTheme());
        app.Styles.Add(new UIKitColorPickerTheme());
        app.Styles.Add(new UIKitDataGridTheme());
        app.Styles.Add(new UIKitTabaloniaTheme());
        app.Styles.Add(new UIKitDockTheme());
        app.Resources["UIKit.FontFamily"] = new FontFamily(FontFamilyName);
        // GPUI opens a submenu as soon as its item is hovered (usage contract, docs/testing.md).
        Avalonia.Controls.Platform.DefaultMenuInteractionHandler.MenuShowDelay = TimeSpan.Zero;
    }
}
