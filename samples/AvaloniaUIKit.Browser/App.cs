using Avalonia;
using AvaloniaUIKit.Demos;

namespace AvaloniaUIKit.Browser;

/// <summary>The browser application: the themes and the bundled Inter; views are created on demand (<see cref="Demos"/>).</summary>
public sealed class App : Application
{
    public override void Initialize() => DemoApp.ApplyTheme(this);
}
