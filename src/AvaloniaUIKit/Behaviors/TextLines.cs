using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// Text placement helpers. GPUI centers a line's glyphs in its line height
/// even when the line is shorter than the font (text_xl on a 20px line): the
/// glyphs then overflow above and below. Avalonia aligns such a line's glyphs
/// to its top, so they overflow below only.
/// </summary>
public static class TextLines
{
    /// <summary>
    /// Shifts a TextBlock's glyphs up by half of the font height its line
    /// height cannot hold, so they sit centered on the line as GPUI draws them.
    /// Layout is unchanged.
    /// </summary>
    public static readonly AttachedProperty<bool> CentersTallGlyphsProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("CentersTallGlyphs", typeof(TextLines));

    static TextLines()
    {
        CentersTallGlyphsProperty.Changed.AddClassHandler<TextBlock>((text, e) =>
        {
            if (e.GetNewValue<bool>())
            {
                text.PropertyChanged += OnTextPropertyChanged;
                Update(text);
            }
            else
            {
                text.PropertyChanged -= OnTextPropertyChanged;
                text.ClearValue(Visual.RenderTransformProperty);
            }
        });
    }

    /// <summary>Gets whether the TextBlock centers glyphs taller than its lines.</summary>
    public static bool GetCentersTallGlyphs(TextBlock text) => text.GetValue(CentersTallGlyphsProperty);

    /// <summary>Sets whether the TextBlock centers glyphs taller than its lines.</summary>
    public static void SetCentersTallGlyphs(TextBlock text, bool value) => text.SetValue(CentersTallGlyphsProperty, value);

    private static void OnTextPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBlock.LineHeightProperty || e.Property == TextBlock.FontSizeProperty ||
            e.Property == TextBlock.FontFamilyProperty || e.Property == TextBlock.FontWeightProperty ||
            e.Property == TextBlock.FontStyleProperty || e.Property == TextBlock.FontStretchProperty)
        {
            Update((TextBlock)sender!);
        }
    }

    private static void Update(TextBlock text)
    {
        var lineHeight = text.LineHeight;
        var typeface = new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch);
        if (double.IsNaN(lineHeight) || !FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphs))
        {
            text.ClearValue(Visual.RenderTransformProperty);
            return;
        }
        var metrics = glyphs.Metrics;
        var scale = text.FontSize / metrics.DesignEmHeight;
        var glyphHeight = (metrics.Descent - metrics.Ascent) * scale;
        var natural = glyphHeight + metrics.LineGap * scale;
        // Avalonia's TextLineImpl: a line no taller than the font keeps its
        // glyphs at the top; a taller one already centers them.
        if (lineHeight > natural)
        {
            text.ClearValue(Visual.RenderTransformProperty);
            return;
        }
        text.RenderTransform = new TranslateTransform(0, (lineHeight - glyphHeight) / 2);
    }
}
