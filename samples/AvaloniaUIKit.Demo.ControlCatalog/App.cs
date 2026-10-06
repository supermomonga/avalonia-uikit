using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaUIKit.Demos;

namespace AvaloniaUIKit.Demo.ControlCatalog;

/// <summary>The catalog application: the themes and Inter as the demos have them, and the catalog's own styles.</summary>
public sealed class App : Application
{
    /// <summary>The command line's choices.</summary>
    public static CatalogOptions Options { get; set; } = new();

    public override void Initialize()
    {
        DemoApp.ApplyTheme(this);
        Styles.Add(new CatalogStyles());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = CatalogSettings.Load();
            if (Options.Theme is { } theme)
            {
                settings.Theme = theme;
            }
            if (Options.Component is { } component)
            {
                settings.Component = component;
            }
            desktop.MainWindow = new MainWindow(settings);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
