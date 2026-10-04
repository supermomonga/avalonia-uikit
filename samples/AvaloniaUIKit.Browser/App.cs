using Avalonia;
using Avalonia.Styling;
using AvaloniaUIKit.Demos;
using Dock.Avalonia.Controls;

namespace AvaloniaUIKit.Browser;

/// <summary>The browser application: the themes and the bundled Inter; views are created on demand (<see cref="Demos"/>).</summary>
public sealed class App : Application
{
    public override void Initialize()
    {
        DemoApp.ApplyTheme(this);
        // Dock 12.1.0.6 leaves a managed drag preview at the dock's corner under
        // Avalonia 12 (docs/testing.md R35): the page shows the drop targets only.
        Styles.Add(new Style(x => x.OfType<ManagedWindowLayer>().Descendant().OfType<DragPreviewControl>())
        {
            Setters = { new Setter(Visual.IsVisibleProperty, false) },
        });
    }
}
