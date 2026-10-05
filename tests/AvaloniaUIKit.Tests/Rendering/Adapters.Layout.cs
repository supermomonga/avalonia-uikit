using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUIKit.Tests.Golden;
using DockSide = Avalonia.Controls.Dock;
using UIKitSidebar = AvaloniaUIKit.Sidebar;

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

    /// <summary>
    /// reference/src/cases/sidebar.rs as a uikit:Sidebar beside the muted content:
    /// releasing the pointer on the content toggles IsCollapsed, as a click toggles
    /// collapsed in the GPUI case. `menu` builds the sidebar story's layout.
    /// </summary>
    private static Control SidebarCase(GoldenCase c)
    {
        var sidebar = c.Bool("menu") ? SidebarStory(c) : new UIKitSidebar
        {
            Collapsible = c.Str("collapsible", "icon") switch
            {
                "offcanvas" => SidebarCollapsible.Offcanvas,
                "none" => SidebarCollapsible.None,
                _ => SidebarCollapsible.Icon,
            },
            IsCollapsed = c.Bool("collapsed"),
        };
        sidebar.Side = c.Str("side", "left") == "right" ? Side.Right : Side.Left;
        if (c.Has("sidebar_width"))
        {
            sidebar.ExpandedWidth = c.Num("sidebar_width", 255);
        }
        if (c.Bool("blocks"))
        {
            Border Block() => new()
            {
                Width = 24,
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Left,
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("UIKit.SidebarPrimary"),
            };
            sidebar.Header = Block();
            sidebar.Footer = Block();
        }
        if (c.Bool("toggle"))
        {
            sidebar.Header = new SidebarToggleButton
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                [!!SidebarToggleButton.IsCollapsedProperty] = sidebar[!!UIKitSidebar.IsCollapsedProperty],
                [!SidebarToggleButton.SideProperty] = sidebar[!UIKitSidebar.SideProperty],
            };
        }
        var content = SidebarContent();
        content.PointerReleased += (_, _) => sidebar.IsCollapsed = !sidebar.IsCollapsed;
        DockPanel.SetDock(sidebar, sidebar.Side == Side.Right ? DockSide.Right : DockSide.Left);
        return new DockPanel
        {
            Width = c.Num("width", 400),
            Height = c.Num("height", 160),
            Children = { sidebar, content },
        };
    }

    /// <summary>The sidebar story's layout (sidebar_story.rs), as the menu cases build it.</summary>
    private static UIKitSidebar SidebarStory(GoldenCase c)
    {
        static T Dynamic<T>(T control, Avalonia.AvaloniaProperty property, string key) where T : Control
        {
            control[!property] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension(key);
            return control;
        }
        static PathIcon Lucide(string name, double size = 16) => new()
        {
            Width = size,
            Height = size,
            [!PathIcon.DataProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("UIKit.Icon." + name),
        };
        var active = c.Str("active", "Playground");
        var click = c.Str("click");
        SidebarMenuItem Item(string label, string icon) => new()
        {
            Label = label,
            Icon = (Avalonia.Media.Geometry)Avalonia.Application.Current!.FindResource("UIKit.Icon." + icon)!,
            IsActive = label == active,
            ClickToOpen = click == "open",
            ClickToToggle = click == "toggle",
        };
        var sidebar = new UIKitSidebar { ExpandedWidth = 220, IsCollapsed = c.Bool("collapsed") };

        // The company: a 32px success square with the logo, two lines and the chevrons;
        // collapsed to icons, the logo alone in the foreground color (the story's
        // header follows the collapsed state).
        Control Company(bool collapsed)
        {
            if (collapsed)
            {
                var alone = Dynamic(Lucide("GalleryVerticalEnd"), PathIcon.ForegroundProperty, "UIKit.Foreground");
                alone.HorizontalAlignment = HorizontalAlignment.Left;
                return alone;
            }
            var logo = Dynamic(new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new Avalonia.CornerRadius(6),
                Child = Dynamic(Lucide("GalleryVerticalEnd"), PathIcon.ForegroundProperty, "UIKit.SuccessForeground"),
            }, Border.BackgroundProperty, "UIKit.Success");
            var company = new StackPanel
            {
                Margin = new Avalonia.Thickness(8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = "Company Name", FontSize = 14, LineHeight = 17.5 },
                    new TextBlock { Text = "Enterprise", FontSize = 12, LineHeight = 15 },
                },
            };
            var chevrons = Lucide("ChevronsUpDown");
            chevrons.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(logo, DockSide.Left);
            DockPanel.SetDock(chevrons, DockSide.Right);
            return new DockPanel { Children = { logo, chevrons, company } };
        }

        // The user: the icon and name 8px apart, the chevrons at the end; collapsed,
        // the icon alone.
        Control User(bool collapsed)
        {
            var user = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { Lucide("CircleUser") } };
            user.Children[0].VerticalAlignment = VerticalAlignment.Center;
            var footer = new DockPanel();
            if (!collapsed)
            {
                user.Children.Add(new TextBlock { Text = "Jason Lee", LineHeight = 26, VerticalAlignment = VerticalAlignment.Center });
                var end = Lucide("ChevronsUpDown");
                end.VerticalAlignment = VerticalAlignment.Center;
                DockPanel.SetDock(end, DockSide.Right);
                footer.Children.Add(end);
            }
            footer.Children.Add(user);
            return footer;
        }

        var history = Item("History", "SquareTerminal");
        history.Icon = null;
        history.Suffix = new ToggleSwitch { Classes = { "xsmall" }, IsChecked = false };
        var playground = Item("Playground", "SquareTerminal");
        playground.IsOpen = !c.Bool("closed");
        playground.Items.Add(history);
        playground.Items.Add(new SidebarMenuItem { Label = "Starred" });
        var models = Item("Models", "Bot");
        models.Items.Add(new SidebarMenuItem { Label = "Genesis" });
        models.Items.Add(new SidebarMenuItem { Label = "Explorer" });
        var design = Item("Design Engineering", "Frame");
        design.Suffix = new Badge { IsDot = true, Count = 1, Content = new Border { Padding = new Avalonia.Thickness(2), Child = Lucide("Bell", 14) } };
        var sales = Item("Sales and Marketing", "ChartPie");
        sales.Suffix = Lucide("Settings2", 14);
        var travel = Item("Travel", "Map");
        travel.IsEnabled = false;
        sidebar.Items.Add(new SidebarGroup { Label = "Platform", Items = { new SidebarMenu { Items = { playground, models, Item("Documentation", "BookOpen") } } } });
        sidebar.Items.Add(new SidebarGroup { Label = "Projects", Items = { new SidebarMenu { Items = { design, sales, travel } } } });

        var header = new SidebarHeader { Content = Company(sidebar.IsCollapsed) };
        var footer = new SidebarFooter { Content = User(sidebar.IsCollapsed) };
        sidebar.PropertyChanged += (_, e) =>
        {
            if (e.Property == UIKitSidebar.IsCollapsedProperty)
            {
                header.Content = Company(sidebar.IsCollapsed);
                footer.Content = User(sidebar.IsCollapsed);
            }
        };
        sidebar.Header = header;
        sidebar.Footer = footer;
        return sidebar;
    }

    private static double[]? Numbers(GoldenCase c, string key) =>
        c.Params.TryGetPropertyValue(key, out var node) && node is System.Text.Json.Nodes.JsonArray array
            ? [.. array.Select(n => n!.GetValue<double>())]
            : null;
}
