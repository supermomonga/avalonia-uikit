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

    private static double[]? Numbers(GoldenCase c, string key) =>
        c.Params.TryGetPropertyValue(key, out var node) && node is System.Text.Json.Nodes.JsonArray array
            ? [.. array.Select(n => n!.GetValue<double>())]
            : null;
}
