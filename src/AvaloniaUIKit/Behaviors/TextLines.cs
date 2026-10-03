using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// Text placement helpers. GPUI centers a line's glyphs in its line height
/// even when the line is shorter than the font (text_xl on a 20px line): the
/// glyphs then overflow above and below. Avalonia aligns such a line's glyphs
/// to its top, so they overflow below only. GPUI also rounds a text's width up
/// to whole pixels (text.rs TextLayout::layout), which moves whatever follows it.
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

    /// <summary>
    /// Rounds the width of TextBlocks (inherited: every one below the element)
    /// up to whole logical pixels, as GPUI sizes a text element, through the
    /// TextBlock's MinWidth at style priority.
    /// </summary>
    public static readonly AttachedProperty<bool> RoundsWidthUpProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("RoundsWidthUp", typeof(TextLines), inherits: true);

    private static readonly AttachedProperty<IDisposable?> RoundedWidthProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, IDisposable?>("RoundedWidth", typeof(TextLines));

    static TextLines()
    {
        RoundsWidthUpProperty.Changed.AddClassHandler<TextBlock>((text, e) =>
        {
            text.SizeChanged -= OnSizeChanged;
            text.PropertyChanged -= OnRoundedTextChanged;
            Unround(text);
            if (e.GetNewValue<bool>())
            {
                text.SizeChanged += OnSizeChanged;
                text.PropertyChanged += OnRoundedTextChanged;
            }
        });
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

    /// <summary>Gets whether TextBlocks below the element round their width up.</summary>
    public static bool GetRoundsWidthUp(Control element) => element.GetValue(RoundsWidthUpProperty);

    /// <summary>Sets whether TextBlocks below the element round their width up.</summary>
    public static void SetRoundsWidthUp(Control element, bool value) => element.SetValue(RoundsWidthUpProperty, value);

    // The text's own width, before layout rounding, and up to whole pixels.
    private static void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var text = (TextBlock)sender!;
        var width = Math.Ceiling(text.TextLayout.WidthIncludingTrailingWhitespace - 1e-4) + text.Padding.Left + text.Padding.Right;
        if (text.MinWidth != width && width > 0)
        {
            Unround(text);
            text.SetValue(RoundedWidthProperty, text.SetValue(Layoutable.MinWidthProperty, width, BindingPriority.Style));
        }
    }

    // A new text starts from its natural width again.
    private static void OnRoundedTextChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBlock.TextProperty || e.Property == TextBlock.InlinesProperty ||
            e.Property == TextBlock.FontSizeProperty || e.Property == TextBlock.FontFamilyProperty ||
            e.Property == TextBlock.FontWeightProperty || e.Property == TextBlock.FontStyleProperty ||
            e.Property == TextBlock.FontStretchProperty || e.Property == TextBlock.LetterSpacingProperty)
        {
            Unround((TextBlock)sender!);
        }
    }

    private static void Unround(TextBlock text)
    {
        text.GetValue(RoundedWidthProperty)?.Dispose();
        text.ClearValue(RoundedWidthProperty);
    }

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
