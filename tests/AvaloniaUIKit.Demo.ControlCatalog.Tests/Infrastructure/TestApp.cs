using Avalonia;
using Avalonia.Headless;
using AvaloniaUIKit.Demos;

namespace AvaloniaUIKit.Demo.ControlCatalog.Tests.Infrastructure;

/// <summary>The catalog's themes and styles on the headless platform, with Skia and the demos' Inter.</summary>
public sealed class TestApp : Application
{
    public override void Initialize()
    {
        DemoApp.ApplyTheme(this);
        Styles.Add(new CatalogStyles());
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHarfBuzz()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithDemoFonts();
}
