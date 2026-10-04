using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's ShimmerText (shimmer.rs): <see cref="Text"/> with a highlight
/// sweeping across it every <see cref="Duration"/>. As in GPUI the highlight
/// is the glyphs painted again twelve times, each clipped to a narrower band
/// around the sweep's center, in a color mixed from the text color toward
/// the theme's background (foreground on a dark theme) at 20%. GPUI derives
/// a repeating sweep's phase from the app's clock so every instance moves
/// together; here each instance starts its own.
/// </summary>
[TemplatePart("PART_Layers", typeof(Panel))]
public class ShimmerText : TemplatedControl
{
    /// <summary>The text.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ShimmerText, string?>(nameof(Text));

    /// <summary>How long one sweep takes (GPUI: 2s).</summary>
    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<ShimmerText, TimeSpan>(nameof(Duration), TimeSpan.FromSeconds(2));

    /// <summary>Whether the sweep repeats (GPUI's default) or runs once.</summary>
    public static readonly StyledProperty<bool> RepeatsProperty =
        AvaloniaProperty.Register<ShimmerText, bool>(nameof(Repeats), true);

    /// <summary>Whether the sweep runs from the end to the start.</summary>
    public static readonly StyledProperty<bool> IsReversedProperty =
        AvaloniaProperty.Register<ShimmerText, bool>(nameof(IsReversed));

    /// <summary>Half the band's width as a share of the text's (GPUI: 0.3).</summary>
    public static readonly StyledProperty<double> SpreadProperty =
        AvaloniaProperty.Register<ShimmerText, double>(nameof(Spread), 0.3, coerce: (_, v) => Math.Clamp(v, 0.05, 1));

    /// <summary>The highlight's color; computed from the text color when unset.</summary>
    public static readonly StyledProperty<Color?> HighlightColorProperty =
        AvaloniaProperty.Register<ShimmerText, Color?>(nameof(HighlightColor));

    /// <summary>Where the sweep is, from 0 to 1 (animated).</summary>
    public static readonly StyledProperty<double> PhaseProperty =
        AvaloniaProperty.Register<ShimmerText, double>(nameof(Phase));

    private const int Layers = 12;
    private Panel? _layers;
    private CancellationTokenSource? _running;

    static ShimmerText()
    {
        AffectsArrange<ShimmerText>(PhaseProperty, SpreadProperty);
        foreach (var p in new AvaloniaProperty[] { HighlightColorProperty, ForegroundProperty, TextProperty })
        {
            p.Changed.AddClassHandler<ShimmerText>((s, _) => s.UpdateLayers());
        }
        foreach (var p in new AvaloniaProperty[] { DurationProperty, RepeatsProperty, IsReversedProperty })
        {
            p.Changed.AddClassHandler<ShimmerText>((s, _) => s.Restart());
        }
    }

    /// <inheritdoc cref="TextProperty"/>
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <inheritdoc cref="DurationProperty"/>
    public TimeSpan Duration { get => GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

    /// <inheritdoc cref="RepeatsProperty"/>
    public bool Repeats { get => GetValue(RepeatsProperty); set => SetValue(RepeatsProperty, value); }

    /// <inheritdoc cref="IsReversedProperty"/>
    public bool IsReversed { get => GetValue(IsReversedProperty); set => SetValue(IsReversedProperty, value); }

    /// <inheritdoc cref="SpreadProperty"/>
    public double Spread { get => GetValue(SpreadProperty); set => SetValue(SpreadProperty, value); }

    /// <inheritdoc cref="HighlightColorProperty"/>
    public Color? HighlightColor { get => GetValue(HighlightColorProperty); set => SetValue(HighlightColorProperty, value); }

    /// <inheritdoc cref="PhaseProperty"/>
    public double Phase { get => GetValue(PhaseProperty); set => SetValue(PhaseProperty, value); }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _layers = e.NameScope.Find<Panel>("PART_Layers");
        if (_layers is not null)
        {
            _layers.Children.Clear();
            for (var i = 0; i < Layers; i++)
            {
                var layer = new TextBlock { Name = "PART_Layer", IsHitTestVisible = false };
                layer.Bind(TextBlock.TextProperty, this.GetObservable(TextProperty));
                _layers.Children.Add(layer);
            }
        }
        UpdateLayers();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Restart();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _running?.Cancel();
        _running = null;
    }

    /// <summary>Creates a shimmering text.</summary>
    public ShimmerText() => ActualThemeVariantChanged += (_, _) => UpdateLayers();

    private void Restart()
    {
        _running?.Cancel();
        _running = null;
        if (VisualRoot is null)
        {
            return;
        }
        // shimmer.rs: a linear phase from 0 to 1 (1 to 0 reversed) per duration.
        var (from, to) = IsReversed ? (1.0, 0.0) : (0.0, 1.0);
        var animation = new Animation
        {
            Duration = Duration,
            IterationCount = Repeats ? IterationCount.Infinite : new IterationCount(1),
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(PhaseProperty, from) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(PhaseProperty, to) } },
            },
        };
        _running = new CancellationTokenSource();
        _ = animation.RunAsync(this, _running.Token);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        if (_layers is not null)
        {
            for (var i = 0; i < _layers.Children.Count; i++)
            {
                var band = Band(size, Phase, Spread, i);
                // A layer whose band is off the text paints nothing at all.
                _layers.Children[i].IsVisible = band is not null;
                _layers.Children[i].Clip = band is { } b ? new RectangleGeometry(b) : null;
            }
        }
        return size;
    }

    /// <summary>shimmer.rs shimmer_band_bounds: layer <paramref name="layer"/>'s band, or null.</summary>
    public static Rect? Band(Size size, double phase, double spread, int layer)
    {
        var width = size.Width;
        if (width <= 0 || size.Height <= 0)
        {
            return null;
        }
        var half = width * spread;
        var padding = half / width + 0.05;
        var center = (phase * (1 + padding * 2) - padding) * width;
        var radius = half * (1 - (double)layer / Layers);
        var left = Math.Max(0, center - radius);
        var right = Math.Min(width, center + radius);
        return right > left ? new Rect(left, 0, right - left, size.Height) : null;
    }

    private void UpdateLayers()
    {
        if (_layers is null)
        {
            return;
        }
        var brush = new SolidColorBrush(LayerColor());
        foreach (var layer in _layers.Children.OfType<TextBlock>())
        {
            layer.Foreground = brush;
        }
        InvalidateArrange();
    }

    // shimmer.rs shimmer_highlight_color: the text mixed 20% with the theme's
    // background (light) or foreground (dark), or with the other one when the
    // text already has that lightness; each layer at 1 - (1 - peak)^(1/12).
    private Color LayerColor()
    {
        var dark = UIKitThemeVariants.IsDark(ActualThemeVariant);
        var text = (Foreground as ISolidColorBrush)?.Color ?? Colors.Black;
        var highlight = HighlightColor ?? Mixed(text, dark);
        var peak = dark ? 0.6 : 0.75;
        var layer = 1 - Math.Pow(1 - peak, 1.0 / Layers);
        return Color.FromArgb((byte)Math.Round(highlight.A * layer), highlight.R, highlight.G, highlight.B);
    }

    private Color Mixed(Color text, bool dark)
    {
        var background = Lookup("UIKit.Background.Color");
        var foreground = Lookup("UIKit.Foreground.Color");
        var (target, opposite) = dark ? (foreground, background) : (background, foreground);
        if (Math.Abs(target.ToHsl().L - text.ToHsl().L) < 0.1)
        {
            target = opposite;
        }
        return OkLab.Mix(text, target, 0.2);
    }

    private Color Lookup(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out var value) && value is Color c ? c : Colors.Transparent;
}
