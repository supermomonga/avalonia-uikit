using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// Adapters for the navigation work: the slots added to existing controls
/// (uikit:GroupBoxes, Separators, Spinners, ProgressCircles), uikit:Pagination,
/// the carousel's buttons and pointer input, and the tab bar's parts.
/// </summary>
public static partial class Adapters
{
    /// <summary>
    /// Fills the slots a case names (reference/src/cases/display.rs and
    /// progress.rs): a group's footer, a separator's label, a spinner's icon
    /// and the content of a progress circle.
    /// </summary>
    private static T Slotted<T>(T control, GoldenCase c) where T : Control
    {
        switch (control)
        {
            case GroupBox group when c.Has("footer"):
                GroupBoxes.SetFooter(group, c.Str("footer"));
                break;
            case Separator separator when c.Has("label"):
                Separators.SetLabel(separator, c.Str("label"));
                break;
            case ProgressBar spinner when c.Component == "spinner" && c.Has("icon"):
                Spinners.SetIcon(spinner, Icon(c.Str("icon")).Data);
                break;
            case ProgressBar circle when c.Has("content"):
                ProgressCircles.SetContent(circle, new TextBlock { Text = c.Str("content"), FontWeight = FontWeight.SemiBold });
                break;
        }
        return control;
    }

    /// <summary>
    /// uikit:Pagination for the uikit-pagination cases (reference/src/cases/pagination.rs):
    /// `total` pages, `current`, `visible` pages before ellipses, the size and compact classes.
    /// </summary>
    public static global::AvaloniaUIKit.Pagination PaginationCase(GoldenCase c)
    {
        var pagination = new global::AvaloniaUIKit.Pagination
        {
            TotalPages = (int)c.Num("total", 5),
            CurrentPage = (int)c.Num("current", 3),
            VisiblePages = (int)c.Num("visible", 5),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(pagination, c, "size", "medium");
        FlagClass(pagination, c, "compact");
        return pagination;
    }

    /// <summary>
    /// The tabs-bar cases (reference/src/cases/tabs.rs): a tabs case's TabStrip
    /// or TabControl with the menu class and the tabs story's prefix and suffix
    /// as uikit:Tabs.Prefix and Suffix.
    /// </summary>
    public static T TabBarParts<T>(T tabs, GoldenCase c) where T : Avalonia.Controls.Primitives.SelectingItemsControl
    {
        FlagClass(tabs, c, "menu");
        if (c.Bool("bar_prefix"))
        {
            Tabs.SetPrefix(tabs, BarButtons("chevron-left", "chevron-right"));
        }
        if (c.Bool("bar_suffix"))
        {
            Tabs.SetSuffix(tabs, BarButtons("inbox", "ellipsis"));
        }
        return tabs;
    }

    /// <summary>The tabs story's prefix and suffix: two ghost xsmall icon buttons in an mx_1 row.</summary>
    private static StackPanel BarButtons(string first, string second)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0) };
        foreach (var name in new[] { first, second })
        {
            var button = new Button { Content = Icon(name) };
            button.Classes.AddRange(["ghost", "xsmall", "icon-only"]);
            ((PathIcon)button.Content).Classes.Add("xsmall");
            row.Children.Add(button);
        }
        return row;
    }

    /// <summary>
    /// reference/src/cases/carousel.rs as a Carousel on the track
    /// (uikit:Carousels.TracksPointer) with uikit:CarouselPrevious and
    /// uikit:CarouselNext beside it and the pages 16px below: the usage the
    /// theme documents. `looping` is WrapSelection, `vertical` a vertical
    /// PageSlide (the carousel then takes the slides' height).
    /// </summary>
    public static StackPanel UikitCarouselCase(GoldenCase c)
    {
        var count = (int)c.Num("count", 3);
        var height = c.Num("height", 120);
        var vertical = c.Bool("vertical");
        var carousel = new Carousel
        {
            Width = c.Num("width", 240),
            Focusable = true,
            WrapSelection = c.Bool("looping"),
            ItemsSource = Enumerable.Range(1, count).Select(i => Slide(i, height)).ToList(),
            SelectedIndex = (int)c.Num("selected", 0),
        };
        Carousels.SetTracksPointer(carousel, true);
        if (vertical)
        {
            carousel.Height = height;
            carousel.PageTransition = new SpringSlide { Orientation = Avalonia.Animation.PageSlide.SlideAxis.Vertical, Gap = 16 };
        }
        CarouselButton Nav(CarouselButton button)
        {
            button.Carousel = carousel;
            ClassFrom(button, c, "size", "medium");
            return button;
        }
        var panel = new StackPanel
        {
            Spacing = 16,
            Width = carousel.Width,
            Children = { new Panel { Children = { carousel, Nav(new CarouselPrevious()), Nav(new CarouselNext()) } } },
        };
        if (!c.Bool("no_pagination"))
        {
            var pager = new PipsPager
            {
                Classes = { "carousel" },
                NumberOfPages = count,
                SelectedPageIndex = carousel.SelectedIndex,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };
            carousel.SelectionChanged += (_, _) => pager.SelectedPageIndex = carousel.SelectedIndex;
            pager.PropertyChanged += (_, e) =>
            {
                if (e.Property == PipsPager.SelectedPageIndexProperty)
                {
                    carousel.SelectedIndex = pager.SelectedPageIndex;
                }
            };
            panel.Children.Add(pager);
        }
        return panel;
    }
}

