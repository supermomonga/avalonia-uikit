using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUIKit.Demo.ControlCatalog.Inspector;

namespace AvaloniaUIKit.Demo.ControlCatalog.Views;

/// <summary>
/// A component's page, as the site has it: the section and kind, the title and
/// description, the controls it covers, and a card for each demo under the page's
/// headings.
/// </summary>
public sealed class ComponentPage : StackPanel
{
    private readonly List<DemoCard> _cards = [];
    private DemoCard? _active;

    public ComponentPage(CatalogComponent component)
    {
        Component = component;
        Spacing = 16;
        Margin = new Thickness(40, 28, 40, 48);
        MaxWidth = 960;

        var eyebrow = $"{Catalog.Heading(component.Section)} · {component.Group}".ToUpperInvariant();
        Children.Add(new TextBlock { Classes = { "eyebrow" }, Text = eyebrow });
        var docs = new HyperlinkButton
        {
            NavigateUri = component.DocsUri,
            VerticalAlignment = VerticalAlignment.Center,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = "Documentation", FontSize = 13 },
                    new PathIcon { Data = InlineText.Icon("ExternalLink"), Width = 12, Height = 12 },
                },
            },
        };
        DockPanel.SetDock(docs, Avalonia.Controls.Dock.Right);
        Children.Add(new DockPanel
        {
            Margin = new Thickness(0, -8, 0, 0),
            Children = { docs, new TextBlock { Classes = { "page-title" }, Text = component.Title } },
        });
        Children.Add(new SelectableTextBlock { Classes = { "page-description" }, Margin = new Thickness(0, -8, 0, 0) }.With(component.Description));

        var chips = new WrapPanel { ItemSpacing = 6, LineSpacing = 6 };
        foreach (var control in component.Controls)
        {
            chips.Children.Add(new Border { Classes = { "chip" }, Child = new TextBlock { Text = control } });
        }
        if (component.Package is { } package)
        {
            chips.Children.Add(new TagLabel { Classes = { "outline", "small" }, Content = package, Margin = new Thickness(4, 0, 0, 0) });
        }
        if (component.Library is { } library)
        {
            chips.Children.Add(new TagLabel { Classes = { "info", "small" }, Content = library });
        }
        Children.Add(chips);

        string? heading = null;
        foreach (var demo in component.Demos)
        {
            // Headings other than Examples group the demos as the page does.
            if (demo.Heading is { } title && title != heading && title is not "Examples" and not "Usage")
            {
                Children.Add(new TextBlock { Classes = { "section-heading" }, Text = title });
            }
            heading = demo.Heading;
            var card = new DemoCard(new DemoSession(component, demo));
            card.Activated += (_, _) => ActiveCard = card;
            _cards.Add(card);
            Children.Add(card);
        }
    }

    /// <summary>The component shown.</summary>
    public CatalogComponent Component { get; }

    /// <summary>The demo cards, in order.</summary>
    public IReadOnlyList<DemoCard> Cards => _cards;

    /// <summary>The card the property grid works on.</summary>
    public DemoCard? ActiveCard
    {
        get => _active;
        set
        {
            if (value == _active)
            {
                return;
            }
            if (_active is not null)
            {
                _active.IsActive = false;
            }
            _active = value;
            if (value is not null)
            {
                value.IsActive = true;
            }
            ActiveCardChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Raised when another card becomes the active one.</summary>
    public event EventHandler? ActiveCardChanged;

    /// <summary>The card whose live demo <paramref name="visual"/> is part of, or null.</summary>
    public DemoCard? CardOf(Visual? visual) => _cards.FirstOrDefault(c => c.Contains(visual));
}
