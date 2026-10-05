using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>Adapters for the layout controls of ADR 30 (uikit:ResizablePanelGroup, Sheet, Sidebar).</summary>
public static partial class Adapters
{
    /// <summary>
    /// reference/src/cases/resizable.rs as a uikit:ResizablePanelGroup: empty
    /// panels with the case's sizes (0 for none), size ranges and hidden panel,
    /// or a vertical group whose first panel holds the horizontal pair.
    /// </summary>
    private static Control ResizableCase(GoldenCase c)
    {
        var first = c.Num("first", 120);
        var sizes = Numbers(c, "sizes") ?? [first, 0];
        var maxs = Numbers(c, "maxs") ?? [];
        var hidden = (int)c.Num("hidden", -1);
        ResizablePanelGroup Group(Orientation orientation)
        {
            var group = new ResizablePanelGroup { Orientation = orientation };
            for (var i = 0; i < sizes.Length; i++)
            {
                var panel = new ResizablePanel { IsVisible = i != hidden };
                if (sizes[i] > 0)
                {
                    panel.Size = sizes[i];
                }
                if (i < maxs.Length && maxs[i] > 0)
                {
                    panel.MaxSize = maxs[i];
                }
                group.Children.Add(panel);
            }
            return group;
        }
        ResizablePanelGroup root;
        if (c.Bool("nested"))
        {
            root = new ResizablePanelGroup
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new ResizablePanel { Size = first, Content = Group(Orientation.Horizontal) },
                    new ResizablePanel(),
                },
            };
        }
        else
        {
            root = Group(c.Bool("vertical") ? Orientation.Vertical : Orientation.Horizontal);
        }
        root.Width = c.Num("width", 320);
        root.Height = c.Num("height", 120);
        return root;
    }

    /// <summary>
    /// reference/src/cases/sheet.rs: a window-sized area whose tap shows a
    /// uikit:Sheet at the case's placement, with the 40px muted block (or the
    /// case's rows), the title, size, footer and overlay settings.
    /// </summary>
    private static Control SheetCase(GoldenCase c)
    {
        Border Fill(string brush, double width, double height) => new()
        {
            Width = width,
            Height = height,
            HorizontalAlignment = double.IsNaN(width) ? HorizontalAlignment.Stretch : HorizontalAlignment.Left,
            [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension(brush),
        };
        var area = new Border
        {
            Width = c.Num("width", 560),
            Height = c.Num("height", 400),
            Background = Avalonia.Media.Brushes.Transparent,
        };
        area.Tapped += (_, _) =>
        {
            var sheet = new Sheet
            {
                Placement = c.Str("placement", "right") switch
                {
                    "left" => DrawerPlacement.Left,
                    "top" => DrawerPlacement.Top,
                    "bottom" => DrawerPlacement.Bottom,
                    _ => DrawerPlacement.Right,
                },
                HasOverlay = !c.Bool("no_overlay") && c.Str("mode") != "no-overlay",
                IsOverlayClosable = c.Str("mode") != "unclosable",
            };
            var rows = (int)c.Num("rows", 0);
            if (rows > 0)
            {
                var stack = new StackPanel();
                for (var i = 0; i < rows; i++)
                {
                    stack.Children.Add(i % 2 == 0 ? Fill("UIKit.Muted", double.NaN, 40) : new Border { Height = 40 });
                }
                sheet.Content = stack;
            }
            else
            {
                sheet.Content = Fill("UIKit.Muted", double.NaN, 40);
            }
            if (c.Has("size"))
            {
                sheet.Size = new Avalonia.RelativeScalar(c.Num("size", 350), Avalonia.RelativeUnit.Absolute);
            }
            if (c.Has("relative"))
            {
                sheet.Size = new Avalonia.RelativeScalar(c.Num("relative", 1), Avalonia.RelativeUnit.Relative);
            }
            if (c.Has("title"))
            {
                sheet.Title = c.Str("title");
            }
            if (c.Bool("footer"))
            {
                sheet.Footer = Fill("UIKit.Primary", 80, 24);
            }
            sheet.Show(area);
        };
        return area;
    }

    private static double[]? Numbers(GoldenCase c, string key) =>
        c.Params.TryGetPropertyValue(key, out var node) && node is System.Text.Json.Nodes.JsonArray array
            ? [.. array.Select(n => n!.GetValue<double>())]
            : null;
}
