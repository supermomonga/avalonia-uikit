using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AvaloniaUIKit.AotSmoke;

public sealed class App : Application
{
    public static bool Smoke { get; set; }
    public static bool Dark { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // GPUI opens a submenu as soon as its item is hovered (usage contract, docs/testing.md).
        Avalonia.Controls.Platform.DefaultMenuInteractionHandler.MenuShowDelay = TimeSpan.Zero;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            RequestedThemeVariant = Dark ? ThemeVariant.Dark : ThemeVariant.Light;
            var window = new MainWindow();
            desktop.MainWindow = window;
            if (Smoke)
            {
                // Render the gallery in both themes, then exit: a theme that needed
                // reflection would have failed to load or to apply by now.
                DispatcherTimer.RunOnce(() =>
                {
                    RequestedThemeVariant = RequestedThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
                    DispatcherTimer.RunOnce(() =>
                    {
                        var ok = window.Background is ISolidColorBrush && window.IsVisible;
                        Console.WriteLine(ok ? "smoke: ok" : "smoke: the gallery did not render");
                        desktop.Shutdown(ok ? 0 : 1);
                    }, TimeSpan.FromMilliseconds(800));
                }, TimeSpan.FromMilliseconds(800));
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
