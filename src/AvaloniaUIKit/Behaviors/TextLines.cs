using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
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
    /// TextBlock's MinWidth at style priority. A stretched text wider than the
    /// room it is arranged in keeps Avalonia's own width, so it overflows from
    /// its start (or trims) rather than centering on the room.
    /// </summary>
    public static readonly AttachedProperty<bool> RoundsWidthUpProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("RoundsWidthUp", typeof(TextLines), inherits: true);

    private static readonly AttachedProperty<IDisposable?> RoundedWidthProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, IDisposable?>("RoundedWidth", typeof(TextLines));

    /// <summary>
    /// On a ContentPresenter that centers its text: while the text is trimmed,
    /// it is aligned to the start instead. GPUI shrinks an ellipsized label's
    /// box to the room it has (a tab under TabBar's max_width) and draws the
    /// text from the box's start; Avalonia would center the shorter, trimmed text.
    /// </summary>
    public static readonly AttachedProperty<bool> StartsTrimmedTextProperty =
        AvaloniaProperty.RegisterAttached<ContentPresenter, bool>("StartsTrimmedText", typeof(TextLines));


    static TextLines()
    {
        StartsTrimmedTextProperty.Changed.AddClassHandler<ContentPresenter>((presenter, e) =>
        {
            presenter.LayoutUpdated -= OnPresenterLaidOut;
            if (e.GetNewValue<bool>())
            {
                presenter.LayoutUpdated += OnPresenterLaidOut;
            }
            else
            {
                presenter.ClearValue(ContentPresenter.HorizontalContentAlignmentProperty);
            }
        });
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

    /// <summary>Gets whether the presenter aligns its text to the start while it is trimmed.</summary>
    public static bool GetStartsTrimmedText(ContentPresenter presenter) => presenter.GetValue(StartsTrimmedTextProperty);

    /// <summary>Sets whether the presenter aligns its text to the start while it is trimmed.</summary>
    public static void SetStartsTrimmedText(ContentPresenter presenter, bool value) => presenter.SetValue(StartsTrimmedTextProperty, value);

    // Whether the text is trimmed does not depend on where it is aligned, so this settles at once.
    private static void OnPresenterLaidOut(object? sender, EventArgs e)
    {
        var presenter = (ContentPresenter)sender!;
        var trimmed = presenter.Child is TextBlock text && text.TextLayout.TextLines.Any(line => line.HasCollapsed);
        if (trimmed && presenter.HorizontalContentAlignment != HorizontalAlignment.Left)
        {
            presenter.HorizontalContentAlignment = HorizontalAlignment.Left;
        }
        else if (!trimmed && presenter.IsSet(ContentPresenter.HorizontalContentAlignmentProperty))
        {
            presenter.ClearValue(ContentPresenter.HorizontalContentAlignmentProperty);
        }
    }

    /// <summary>Gets whether TextBlocks below the element round their width up.</summary>
    public static bool GetRoundsWidthUp(Control element) => element.GetValue(RoundsWidthUpProperty);

    /// <summary>Sets whether TextBlocks below the element round their width up.</summary>
    public static void SetRoundsWidthUp(Control element, bool value) => element.SetValue(RoundsWidthUpProperty, value);

    // The text's own width, before layout rounding, and up to whole pixels.
    private static void OnSizeChanged(object? sender, SizeChangedEventArgs e) => Round((TextBlock)sender!);

    private static void Round(TextBlock text)
    {
        var padding = text.Padding.Left + text.Padding.Right;
        var natural = text.TextLayout.WidthIncludingTrailingWhitespace + padding;
        var width = Math.Ceiling(natural - padding - 1e-4) + padding;
        // A stretched text wider than its slot overflows (or trims) at Avalonia's own
        // width: held at its rounded width, it would center on the slot instead. One
        // aligned to a side overflows from that side either way.
        var slot = LayoutInformation.GetPreviousArrangeBounds(text) is { } arranged
            ? arranged.Width - text.Margin.Left - text.Margin.Right
            : double.PositiveInfinity;
        if (natural > slot + 1e-4 && text.HorizontalAlignment == HorizontalAlignment.Stretch)
        {
            Unround(text);
            return;
        }
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
        // Moved without resizing: held wider than a room that shrank, it now centers there.
        else if (e.Property == Visual.BoundsProperty &&
                 e.GetOldValue<Rect>().Size == e.GetNewValue<Rect>().Size)
        {
            Round((TextBlock)sender!);
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
