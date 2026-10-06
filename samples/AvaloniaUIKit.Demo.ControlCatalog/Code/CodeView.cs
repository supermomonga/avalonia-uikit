using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaUIKit.Demo.ControlCatalog.Code;

/// <summary>
/// Highlighted, selectable code in the current theme's code colors, scrolling sideways
/// when a line is too long. Changed spans get a faint background.
/// </summary>
public sealed class CodeView : Border
{
    private readonly SelectableTextBlock _text;
    private string _code = "";
    private IReadOnlyList<CodeSpan> _spans = [];

    public CodeView()
    {
        _text = new SelectableTextBlock
        {
            FontSize = 12.5,
            LineHeight = 20,
            TextWrapping = TextWrapping.NoWrap,
        };
        Child = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = new Border { Padding = new Thickness(16, 14), Child = _text, HorizontalAlignment = HorizontalAlignment.Left },
        };
        // The monospaced families of the catalog's styles (CatalogStyles.axaml).
        _text.Bind(TextBlock.FontFamilyProperty, _text.GetResourceObservable("Catalog.Monospace"));
        ActualThemeVariantChanged += (_, _) => Render();
    }

    /// <summary>Shows <paramref name="code"/> colored by <paramref name="spans"/>.</summary>
    public void Show(string code, IReadOnlyList<CodeSpan> spans)
    {
        _code = code;
        _spans = spans;
        Render();
    }

    private void Render()
    {
        var palette = CodePalette.For(ActualThemeVariant);
        Background = CodePalette.Brush(palette.Background);
        var brushes = new Dictionary<CodeColor, IBrush>
        {
            [CodeColor.Foreground] = CodePalette.Brush(palette.Foreground),
            [CodeColor.Keyword] = CodePalette.Brush(palette.Keyword),
            [CodeColor.Type] = CodePalette.Brush(palette.Type),
            [CodeColor.String] = CodePalette.Brush(palette.String),
            [CodeColor.Comment] = CodePalette.Brush(palette.Comment),
            [CodeColor.Function] = CodePalette.Brush(palette.Function),
        };
        var changed = CodePalette.Brush(palette.Changed);
        _text.Foreground = brushes[CodeColor.Foreground];
        var inlines = new InlineCollection();
        foreach (var span in _spans)
        {
            if (span.Length == 0)
            {
                continue;
            }
            var run = new Run(_code.Substring(span.Start, span.Length)) { Foreground = brushes[span.Color] };
            if (span.IsChanged)
            {
                run.Background = changed;
            }
            inlines.Add(run);
        }
        _text.Inlines = inlines;
    }
}
