using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace AvaloniaUIKit;

/// <summary>How a <see cref="TextLabel"/> matches its <see cref="TextLabel.Highlights"/> (GPUI's HighlightsMatch).</summary>
public enum HighlightsMatch
{
    /// <summary>Every occurrence, overlapping ones included.</summary>
    Full,

    /// <summary>Only at the start of the text.</summary>
    Prefix,
}

/// <summary>
/// GPUI Kit's Label (label.rs), renamed because Avalonia has a Label: the
/// <see cref="TextBlock.Text"/> on the label's 20px line in the foreground
/// color, with the <see cref="Secondary"/> text after it, muted. The
/// case-insensitive matches of <see cref="Highlights"/> are blue, and a
/// <see cref="IsMasked"/> label shows a • for each character, all in the
/// foreground color. Sizes, weights, colors and alignment are the
/// TextBlock's, as for <c>TextBlock Classes="label"</c>.
/// </summary>
public class TextLabel : TextBlock
{
    /// <summary>The muted text after the label, separated by a space.</summary>
    public static readonly StyledProperty<string?> SecondaryProperty =
        AvaloniaProperty.Register<TextLabel, string?>(nameof(Secondary));

    /// <summary>The text whose matches are highlighted.</summary>
    public static readonly StyledProperty<string?> HighlightsProperty =
        AvaloniaProperty.Register<TextLabel, string?>(nameof(Highlights));

    /// <summary>Whether every match is highlighted, or only one at the start.</summary>
    public static readonly StyledProperty<HighlightsMatch> HighlightsMatchProperty =
        AvaloniaProperty.Register<TextLabel, HighlightsMatch>(nameof(HighlightsMatch));

    /// <summary>Whether the text shows as • characters.</summary>
    public static readonly StyledProperty<bool> IsMaskedProperty =
        AvaloniaProperty.Register<TextLabel, bool>(nameof(IsMasked));

    // label.rs MASKED.
    private const char Masked = '•';

    private bool _updating;

    /// <inheritdoc cref="SecondaryProperty"/>
    public string? Secondary
    {
        get => GetValue(SecondaryProperty);
        set => SetValue(SecondaryProperty, value);
    }

    /// <inheritdoc cref="HighlightsProperty"/>
    public string? Highlights
    {
        get => GetValue(HighlightsProperty);
        set => SetValue(HighlightsProperty, value);
    }

    /// <inheritdoc cref="HighlightsMatchProperty"/>
    public HighlightsMatch HighlightsMatch
    {
        get => GetValue(HighlightsMatchProperty);
        set => SetValue(HighlightsMatchProperty, value);
    }

    /// <inheritdoc cref="IsMaskedProperty"/>
    public bool IsMasked
    {
        get => GetValue(IsMaskedProperty);
        set => SetValue(IsMaskedProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == SecondaryProperty ||
            change.Property == HighlightsProperty || change.Property == HighlightsMatchProperty ||
            change.Property == IsMaskedProperty)
        {
            UpdateRuns();
        }
    }

    /// <summary>
    /// The text as runs in <see cref="TextBlock.Inlines"/>: the label, the
    /// secondary text (class secondary) and the matches (class highlight), or
    /// the mask; none when the label is the plain <see cref="TextBlock.Text"/>.
    /// </summary>
    private void UpdateRuns()
    {
        if (_updating || Inlines is not { } inlines)
        {
            return;
        }
        _updating = true;
        try
        {
            var label = Text ?? "";
            var secondary = Secondary;
            var full = secondary is null ? label : $"{label} {secondary}";
            var runs = new List<Inline>();
            if (IsMasked)
            {
                // label.rs: a • per character of the label and its secondary text, unhighlighted.
                var count = full.EnumerateRunes().Count();
                runs.Add(new Run(new string(Masked, count)));
            }
            else
            {
                var highlighted = new bool[full.Length];
                foreach (var (start, length) in HighlightRanges(full, Highlights, HighlightsMatch))
                {
                    Array.Fill(highlighted, true, start, length);
                }
                if (secondary is null && !highlighted.Contains(true))
                {
                    if (inlines.Count > 0)
                    {
                        Inlines = [];
                    }
                    return;
                }
                // Runs where the part (label or secondary) or the highlight changes.
                string? Kind(int i) => highlighted[i] ? "highlight" : secondary is not null && i >= label.Length ? "secondary" : null;
                for (var start = 0; start < full.Length;)
                {
                    var kind = Kind(start);
                    var end = start + 1;
                    while (end < full.Length && Kind(end) == kind)
                    {
                        end++;
                    }
                    var run = new Run(full[start..end]);
                    if (kind is not null)
                    {
                        run.Classes.Add(kind);
                    }
                    runs.Add(run);
                    start = end;
                }
            }
            // A new collection: its runs keep Text (a run added to the TextBlock's
            // collection would take Text over), and a changed Inlines makes
            // TextLines.RoundsWidthUp measure the text again.
            var collection = new InlineCollection();
            collection.AddRange(runs);
            Inlines = collection;
        }
        finally
        {
            _updating = false;
        }
    }

    /// <summary>
    /// GPUI's highlight_ranges (label.rs) without the secondary text's two
    /// ranges: the case-insensitive matches of <paramref name="highlights"/> in
    /// <paramref name="text"/> (the label, a space and the secondary text) as
    /// (start, length) in UTF-16 units. Full matches every occurrence, each
    /// search starting a character after the last match's start; Prefix only
    /// one at the start.
    /// </summary>
    internal static List<(int Start, int Length)> HighlightRanges(string text, string? highlights, HighlightsMatch match)
    {
        var ranges = new List<(int, int)>();
        if (string.IsNullOrEmpty(highlights))
        {
            return ranges;
        }
        var search = highlights.ToLowerInvariant();
        var lower = text.ToLowerInvariant();
        if (match == HighlightsMatch.Prefix)
        {
            if (lower.StartsWith(search, StringComparison.Ordinal))
            {
                ranges.Add((0, Math.Min(highlights.Length, text.Length)));
            }
            return ranges;
        }
        var from = 0;
        while (from < lower.Length && lower.IndexOf(search, from, StringComparison.Ordinal) is var at and >= 0)
        {
            if (at + highlights.Length <= text.Length)
            {
                ranges.Add((at, highlights.Length));
            }
            // The next search starts at the next character boundary.
            from = at + (char.IsHighSurrogate(lower[at]) && at + 1 < lower.Length ? 2 : 1);
        }
        return ranges;
    }
}
