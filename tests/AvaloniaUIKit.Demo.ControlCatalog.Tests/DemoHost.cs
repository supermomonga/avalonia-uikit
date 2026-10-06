using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaUIKit.Demo.ControlCatalog.Inspector;

namespace AvaloniaUIKit.Demo.ControlCatalog.Tests;

/// <summary>A demo of the catalog shown in a headless window, as a card shows it.</summary>
internal sealed class DemoHost : IDisposable
{
    private readonly Window _window;

    private DemoHost(DemoSession session)
    {
        Session = session;
        _window = new Window { Width = 1000, Height = 800, Content = new Decorator { Child = session.Root } };
        session.Recreated += (_, _) => ((Decorator)_window.Content!).Child = session.Root;
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    public DemoSession Session { get; }

    /// <summary>Every demo id of the catalog.</summary>
    public static IEnumerable<string> Ids() => Catalog.Components.SelectMany(c => c.Demos).Select(d => d.Id);

    public static DemoHost Open(string id)
    {
        var component = Catalog.Components.First(c => c.Demos.Any(d => d.Id == id));
        return new DemoHost(new DemoSession(component, component.Demos.First(d => d.Id == id)));
    }

    public void Dispose()
    {
        _window.Close();
        Dispatcher.UIThread.RunJobs();
    }
}
