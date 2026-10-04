using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using Dock.Avalonia.Controls;
using Dock.Model.Avalonia;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core;

namespace AvaloniaUIKit.Tests.Rendering;

public static partial class Adapters
{
    /// <summary>
    /// The dock cases (reference/src/cases/dock.rs) as Dock.Avalonia's DockControl:
    /// the dock story's groups as tool docks in proportional docks, sized as GPUI
    /// sized them (Explorer and Search 240px wide; the top row 219px high). With
    /// close buttons the tabbed groups are document docks, whose tabs have them.
    /// </summary>
    public static DockControl DockCase(GoldenCase c)
    {
        var width = c.Num("width", 640);
        var height = c.Num("height", 360);
        var left = c.Num("left", 240);
        var top = c.Num("top", 219);
        static Tool Panel(string title) => new()
        {
            Id = title,
            Title = title,
            // The story's panel body: p_4, GPUI's 16px text on a 26px line.
            Content = new TextBlock { Text = title, Margin = new Thickness(16), LineHeight = 26 },
        };
        static ToolDock Group(string id, double proportion, params Tool[] tools)
        {
            var dock = new ToolDock { Id = id, Proportion = proportion, VisibleDockables = [.. tools] };
            dock.ActiveDockable = tools[0];
            return dock;
        }
        static DocumentDock Documents(string id, double proportion, params string[] titles)
        {
            var documents = titles.Select(title => new Document
            {
                Id = title,
                Title = title,
                Content = new TextBlock { Text = title, Margin = new Thickness(16), LineHeight = 26 },
            }).ToArray();
            var dock = new DocumentDock { Id = id, Proportion = proportion, VisibleDockables = [.. documents] };
            dock.ActiveDockable = documents[0];
            return dock;
        }
        var documents = c.Bool("close_buttons");
        IDockable Tabs(string id, double proportion, string first, string second) =>
            documents ? Documents(id, proportion, first, second) : Group(id, proportion, Panel(first), Panel(second));
        var row = new ProportionalDock
        {
            Id = "Top",
            Orientation = Orientation.Horizontal,
            Proportion = top / height,
            VisibleDockables =
            [
                Tabs("Left", left / width, "Explorer", "Search"),
                new ProportionalDockSplitter(),
                Group("Center", 1 - left / width, Panel("Editor")),
            ],
        };
        var main = new ProportionalDock
        {
            Id = "Main",
            Orientation = Orientation.Vertical,
            VisibleDockables = [row, new ProportionalDockSplitter(), Tabs("Bottom", 1 - top / height, "Terminal", "Problems")],
        };
        var root = new RootDock
        {
            Id = "Root",
            IsCollapsable = false,
            VisibleDockables = [main],
            DefaultDockable = main,
            ActiveDockable = main,
            // The drag preview inside the dock control, as GPUI draws it (not in a window of its own).
            FloatingWindowHostMode = DockFloatingWindowHostMode.Managed,
        };
        var dock = new DockControl
        {
            Width = width,
            Height = height,
            Factory = new Factory(),
            Layout = root,
            InitializeLayout = true,
            InitializeFactory = true,
        };
        return dock;
    }

    /// <summary>
    /// Puts Dock's drag preview where GPUI draws its own (R35): the dragged tab's
    /// corner, moved with the pointer from where the drag began (GPUI starts it on
    /// the first move), above the drop placeholders. On the desktop Dock shows it in
    /// a window of its own, above everything; the cases use its managed preview,
    /// which Dock 12.1.0.6 leaves at its layer's origin under Avalonia 12 (its
    /// position lookup expects the visual root to be the TopLevel) and under the
    /// adorner layer. Where the preview goes is Dock's; the card's look is the theme's.
    /// </summary>
    private static void PlaceDockDragPreview(GoldenCase c, CaseHost host)
    {
        var moves = System.Text.RegularExpressions.Regex.Matches(c.State, @"(pressed|drag)-at-(\d+)-(\d+)");
        if (moves.Count < 3)
        {
            return;
        }
        Point At(int i) => new(double.Parse(moves[i].Groups[2].Value), double.Parse(moves[i].Groups[3].Value));
        var preview = host.Window.GetVisualDescendants().OfType<DragPreviewControl>().FirstOrDefault();
        var layer = preview?.FindAncestorOfType<ManagedWindowLayer>();
        var tab = host.Window.GetVisualDescendants().OfType<Control>()
            .Where(t => t is ToolTabStripItem or DocumentTabStripItem)
            .FirstOrDefault(t => new Rect(t.TranslatePoint(default, host.Window)!.Value, t.Bounds.Size).Contains(At(0)));
        if (preview is null || layer is null || tab is null)
        {
            return;
        }
        var corner = tab.TranslatePoint(default, host.Window)!.Value;
        var place = At(moves.Count - 1) - (At(1) - corner);
        var overlay = Avalonia.Controls.Primitives.OverlayLayer.GetOverlayLayer(host.Window)!;
        ((Panel)preview.GetVisualParent()!).Children.Remove(preview);
        overlay.Children.Add(preview);
        Canvas.SetLeft(preview, place.X);
        Canvas.SetTop(preview, place.Y);
        host.Flush();
    }
}
