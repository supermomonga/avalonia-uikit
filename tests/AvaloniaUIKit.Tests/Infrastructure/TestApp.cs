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
        Styles.Add(new UIKitTheme());
        Styles.Add(new UIKitColorPickerTheme());
        Styles.Add(new UIKitDataGridTheme());
        Styles.Add(new UIKitDockTheme());
        Resources["UIKit.FontFamily"] = new FontFamily(FontFamilyName);
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
            .With(new FontManagerOptions { DefaultFamilyName = FontFamilyName })
            .AfterSetup(_ => UseMacKeyNotation());

    /// <summary>
    /// The GPUI reference renders on macOS: menus show shortcuts with the macOS
    /// modifier symbols, as Avalonia.Native registers them (AvaloniaNativePlatform),
    /// where the headless platform would write "Ctrl+". The symbols themselves are
    /// not compared in pixels (R22).
    /// </summary>
    private static void UseMacKeyNotation()
    {
        var notation = new Avalonia.Input.Platform.KeyGestureFormatInfo(new Dictionary<Avalonia.Input.Key, string>
        {
            { Avalonia.Input.Key.Back, "⌫" }, { Avalonia.Input.Key.Down, "↓" }, { Avalonia.Input.Key.End, "↘" },
            { Avalonia.Input.Key.Escape, "⎋" }, { Avalonia.Input.Key.Home, "↖" }, { Avalonia.Input.Key.Left, "←" },
            { Avalonia.Input.Key.Return, "↩" }, { Avalonia.Input.Key.PageDown, "⇟" }, { Avalonia.Input.Key.PageUp, "⇞" },
            { Avalonia.Input.Key.Right, "→" }, { Avalonia.Input.Key.Space, "␣" }, { Avalonia.Input.Key.Tab, "⇥" },
            { Avalonia.Input.Key.Up, "↑" },
        }, ctrl: "⌃", meta: "⌘", shift: "⇧", alt: "⌥");
        // AvaloniaLocator's registration API is private (R7); its registry is the same table.
        Registry(CurrentMutable(null))[typeof(Avalonia.Input.Platform.KeyGestureFormatInfo)] = () => notation;
    }

    [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod, Name = "get_CurrentMutable")]
    private static extern AvaloniaLocator CurrentMutable(AvaloniaLocator? type);

    [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = "_registry")]
    private static extern ref Dictionary<Type, Func<object?>> Registry(AvaloniaLocator locator);
}
