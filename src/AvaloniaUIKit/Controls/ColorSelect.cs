using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's ColorPicker and ColorSelect (color_picker.rs, base
/// color_picker.rs) as one control: a swatch of <see cref="Color"/> (or an
/// <see cref="Icon"/>) with an optional <see cref="Label"/>, or with the
/// <c>field</c> class a framed field with a small swatch, the hex and a caret.
/// Either opens a 288px popover with two panels: the palette (the
/// <see cref="FeaturedColors"/> row, then GPUI Kit's 9 x 11 palette) and the
/// HSLA sliders; below them the previewed color and its hex to edit.
/// </summary>
/// <remarks>
/// A null <see cref="Color"/> is no color (GPUI's <c>Option&lt;Hsla&gt;</c>):
/// the swatch is empty and the field shows <see cref="PlaceholderText"/>.
/// Pointing at a palette color previews it below the panels (GPUI keeps the
/// last preview after the pointer leaves); typing a hex previews it once it
/// parses. A click on a palette color, or Enter in the hex field, commits the
/// color and closes the popover; a slider commits as it moves and keeps it
/// open. Enter on the focused trigger opens and closes the popover, Escape
/// closes it.
/// </remarks>
[TemplatePart("PART_Popup", typeof(Popup))]
[TemplatePart("PART_Featured", typeof(Panel))]
[TemplatePart("PART_Palette", typeof(Panel))]
[TemplatePart("PART_HexTextBox", typeof(TextBox))]
[TemplatePart("PART_Hue", typeof(Slider))]
[TemplatePart("PART_Saturation", typeof(Slider))]
[TemplatePart("PART_Lightness", typeof(Slider))]
[TemplatePart("PART_Alpha", typeof(Slider))]
[PseudoClasses(":dropdownopen", ":pressed", ":has-color", ":has-preview", ":has-icon", ":hsla")]
public class ColorSelect : TemplatedControl
{
    /// <summary>The color, or null for none (GPUI's <c>Option&lt;Hsla&gt;</c>).</summary>
    public static readonly StyledProperty<Color?> ColorProperty =
        AvaloniaProperty.Register<ColorSelect, Color?>(nameof(Color), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// The color shown below the panels and in the hex field: the palette
    /// color last pointed at, a typed hex, or <see cref="Color"/>.
    /// </summary>
    public static readonly DirectProperty<ColorSelect, Color?> PreviewColorProperty =
        AvaloniaProperty.RegisterDirect<ColorSelect, Color?>(nameof(PreviewColor), o => o.PreviewColor);

    /// <summary><see cref="Color"/>'s hex (GPUI's <c>to_hex</c>), or null without a color.</summary>
    public static readonly DirectProperty<ColorSelect, string?> DisplayTextProperty =
        AvaloniaProperty.RegisterDirect<ColorSelect, string?>(nameof(DisplayText), o => o.DisplayText);

    /// <summary>Whether the popover is open.</summary>
    public static readonly DirectProperty<ColorSelect, bool> IsDropDownOpenProperty =
        AvaloniaProperty.RegisterDirect<ColorSelect, bool>(nameof(IsDropDownOpen), o => o.IsDropDownOpen, (o, v) => o.IsDropDownOpen = v,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The field's text while there is no color (GPUI's default is Select's "Please select").</summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<ColorSelect, string?>(nameof(PlaceholderText), "Please select");

    /// <summary>Text after the swatch (GPUI's <c>label</c>); the field does not show it.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<ColorSelect, string?>(nameof(Label));

    /// <summary>An icon in place of the swatch (GPUI's <c>icon</c>).</summary>
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<ColorSelect, Geometry?>(nameof(Icon));

    /// <summary>
    /// The colors of the row above the palette (GPUI's <c>featured_colors</c>),
    /// such as the last ones used; null for the theme's red, blue, green,
    /// yellow, cyan and magenta, each with its light shade.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<Color>?> FeaturedColorsProperty =
        AvaloniaProperty.Register<ColorSelect, IReadOnlyList<Color>?>(nameof(FeaturedColors));

    /// <summary>
    /// Where the popover opens (GPUI's <c>anchor</c>; below, left-aligned by
    /// default). GPUI Kit 2c5162f keeps the anchor without passing it to its
    /// popover, so only the default is GPUI's.
    /// </summary>
    public static readonly StyledProperty<PlacementMode> PlacementProperty =
        AvaloniaProperty.Register<ColorSelect, PlacementMode>(nameof(Placement), PlacementMode.BottomEdgeAlignedLeft);

    /// <summary>The popover's panel: 0 the palette, 1 the HSLA sliders (GPUI's <c>active_tab</c>).</summary>
    public static readonly StyledProperty<int> ActiveTabProperty =
        AvaloniaProperty.Register<ColorSelect, int>(nameof(ActiveTab), defaultBindingMode: BindingMode.TwoWay,
            coerce: (_, v) => Math.Clamp(v, 0, 1));

    // color_picker.rs render_palette_panel: the theme colors of the featured row.
    private static readonly string[] ThemeFeatured =
    [
        "UIKit.Red.Color", "UIKit.RedLight.Color", "UIKit.Blue.Color", "UIKit.BlueLight.Color",
        "UIKit.Green.Color", "UIKit.GreenLight.Color", "UIKit.Yellow.Color", "UIKit.YellowLight.Color",
        "UIKit.Cyan.Color", "UIKit.CyanLight.Color", "UIKit.Magenta.Color", "UIKit.MagentaLight.Color",
    ];

    // color_picker.rs render_slider_tab_panel: 96 flat steps per hue and lightness track.
    private const int TrackSteps = 96;

    private bool _isDropDownOpen;
    private Color? _previewColor;
    private string? _displayText;
    // The value as GPUI keeps it when it came from the palette (its hex is GPUI's), and the preview.
    private GpuiHsla? _exact;
    private GpuiHsla? _preview;
    private bool _committing;
    private bool _fromSlider;
    private bool _writingHex;
    private bool _writingSliders;
    private string _acceptedHex = string.Empty;
    private Popup? _popup;
    private Panel? _featured;
    private Panel? _palette;
    private TextBox? _hexBox;
    private Slider? _hue, _saturation, _lightness, _alpha;
    private TextBlock? _hueText, _saturationText, _lightnessText, _alphaText;
    private StripPanel? _hueTrack, _lightnessTrack;
    private Border? _saturationTrack, _alphaTrack;

    static ColorSelect()
    {
        FocusableProperty.OverrideDefaultValue<ColorSelect>(true);
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<ColorSelect>(AutomationControlType.Button);
    }

    /// <summary>Creates a color select.</summary>
    public ColorSelect()
    {
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>Raised when the popover opens.</summary>
    public event EventHandler? DropDownOpened;

    /// <summary>Raised when the popover closes.</summary>
    public event EventHandler? DropDownClosed;

    /// <inheritdoc cref="ColorProperty"/>
    public Color? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <inheritdoc cref="PreviewColorProperty"/>
    public Color? PreviewColor
    {
        get => _previewColor;
        private set => SetAndRaise(PreviewColorProperty, ref _previewColor, value);
    }

    /// <inheritdoc cref="DisplayTextProperty"/>
    public string? DisplayText
    {
        get => _displayText;
        private set => SetAndRaise(DisplayTextProperty, ref _displayText, value);
    }

    /// <inheritdoc cref="IsDropDownOpenProperty"/>
    public bool IsDropDownOpen
    {
        get => _isDropDownOpen;
        set
        {
            if (SetAndRaise(IsDropDownOpenProperty, ref _isDropDownOpen, value))
            {
                OnDropDownChanged(value);
            }
        }
    }

    /// <inheritdoc cref="PlaceholderTextProperty"/>
    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <inheritdoc cref="LabelProperty"/>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <inheritdoc cref="IconProperty"/>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <inheritdoc cref="FeaturedColorsProperty"/>
    public IReadOnlyList<Color>? FeaturedColors
    {
        get => GetValue(FeaturedColorsProperty);
        set => SetValue(FeaturedColorsProperty, value);
    }

    /// <inheritdoc cref="PlacementProperty"/>
    public PlacementMode Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    /// <inheritdoc cref="ActiveTabProperty"/>
    public int ActiveTab
    {
        get => GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    // The committed color as GPUI keeps it.
    private GpuiHsla? Value => _exact ?? (Color is { } color ? GpuiHsla.From(color) : null);

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_popup is not null)
        {
            _popup.Closed -= OnPopupClosed;
        }
        if (_hexBox is not null)
        {
            _hexBox.TextChanged -= OnHexChanged;
            _hexBox.KeyDown -= OnHexKeyDown;
        }
        foreach (var slider in new[] { _hue, _saturation, _lightness, _alpha })
        {
            if (slider is not null)
            {
                slider.ValueChanged -= OnSliderChanged;
            }
        }
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _featured = e.NameScope.Find<Panel>("PART_Featured");
        _palette = e.NameScope.Find<Panel>("PART_Palette");
        _hexBox = e.NameScope.Find<TextBox>("PART_HexTextBox");
        _hue = e.NameScope.Find<Slider>("PART_Hue");
        _saturation = e.NameScope.Find<Slider>("PART_Saturation");
        _lightness = e.NameScope.Find<Slider>("PART_Lightness");
        _alpha = e.NameScope.Find<Slider>("PART_Alpha");
        _hueText = e.NameScope.Find<TextBlock>("PART_HueText");
        _saturationText = e.NameScope.Find<TextBlock>("PART_SaturationText");
        _lightnessText = e.NameScope.Find<TextBlock>("PART_LightnessText");
        _alphaText = e.NameScope.Find<TextBlock>("PART_AlphaText");
        _hueTrack = Track(e.NameScope.Find<Panel>("PART_HueTrack"));
        _lightnessTrack = Track(e.NameScope.Find<Panel>("PART_LightnessTrack"));
        _saturationTrack = e.NameScope.Find<Border>("PART_SaturationTrack");
        _alphaTrack = e.NameScope.Find<Border>("PART_AlphaTrack");
        if (_popup is not null)
        {
            _popup.Closed += OnPopupClosed;
        }
        if (_hexBox is not null)
        {
            _hexBox.TextChanged += OnHexChanged;
            _hexBox.KeyDown += OnHexKeyDown;
        }
        foreach (var slider in new[] { _hue, _saturation, _lightness, _alpha })
        {
            if (slider is not null)
            {
                slider.ValueChanged += OnSliderChanged;
            }
        }
        BuildPalette();
        BuildFeatured();
        if (_hueTrack is not null)
        {
            // hsla(ix / 95, 1, 0.5, 1): the hue track does not depend on the color.
            _hueTrack.SetColors(Enumerable.Range(0, TrackSteps).Select(ix => new GpuiHsla(ix / (float)(TrackSteps - 1), 1, 0.5f, 1).ToColor()));
        }
        ShowValue();
        PseudoClasses.Set(":hsla", ActiveTab == 1);
    }

    private static StripPanel? Track(Panel? host)
    {
        if (host is null)
        {
            return null;
        }
        var strips = new StripPanel();
        host.Children.Clear();
        host.Children.Add(strips);
        return strips;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ColorProperty)
        {
            if (!_committing)
            {
                _exact = null;
            }
            // update_value: the preview, the hex field and the sliders follow the value.
            ShowValue();
        }
        else if (change.Property == FeaturedColorsProperty)
        {
            BuildFeatured();
        }
        else if (change.Property == IconProperty)
        {
            PseudoClasses.Set(":has-icon", Icon is not null);
        }
        else if (change.Property == ActiveTabProperty)
        {
            PseudoClasses.Set(":hsla", ActiveTab == 1);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || e.Source is not Visual source || _popup?.IsInsidePopup(source) == true ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }
        if (IsDropDownOpen)
        {
            IsDropDownOpen = false;
            e.Handled = true;
        }
        else
        {
            PseudoClasses.Set(":pressed", true);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!e.Handled && e.Source is Visual source && _popup?.IsInsidePopup(source) != true &&
            PseudoClasses.Contains(":pressed"))
        {
            IsDropDownOpen = !IsDropDownOpen;
            e.Handled = true;
        }
        PseudoClasses.Set(":pressed", false);
        base.OnPointerReleased(e);
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        PseudoClasses.Set(":pressed", false);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        IsDropDownOpen = false;
    }

    // base color_picker.rs: Confirm (Enter) toggles the focused trigger, Cancel (Escape) closes.
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || !IsEffectivelyEnabled || e.KeyModifiers != KeyModifiers.None)
        {
            return;
        }
        if (e.Key == Key.Escape && IsDropDownOpen)
        {
            IsDropDownOpen = false;
            RefocusAfterClose();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && ReferenceEquals(e.Source, this))
        {
            IsDropDownOpen = !IsDropDownOpen;
            if (IsDropDownOpen)
            {
                // The popover takes the focus (popover.rs): Tab goes on through its swatches.
                _featured?.GetVisualDescendants().OfType<ColorSwatch>().FirstOrDefault()?.Focus(NavigationMethod.Tab);
            }
            e.Handled = true;
        }
    }

    private void OnDropDownChanged(bool open)
    {
        PseudoClasses.Set(":dropdownopen", open);
        if (open)
        {
            DropDownOpened?.Invoke(this, EventArgs.Empty);
            return;
        }
        DropDownClosed?.Invoke(this, EventArgs.Empty);
    }

    private void OnPopupClosed(object? sender, EventArgs e) => IsDropDownOpen = false;

    // A close from the keyboard gives the trigger its focus back, ringed (popover.rs restores
    // the previous focus on dismiss). After a click in the palette GPUI's controlled close
    // leaves nothing focused; Avalonia gives the trigger the focus back, unringed.
    private void RefocusAfterClose()
    {
        if (TopLevel.GetTopLevel(this) is not { } topLevel || !IsEffectivelyEnabled)
        {
            return;
        }
        var focused = topLevel.FocusManager?.GetFocusedElement();
        if (focused is null || ReferenceEquals(focused, this) ||
            focused is Visual visual && (_popup?.IsInsidePopup(visual) == true || TopLevel.GetTopLevel(visual) is null))
        {
            Focus(NavigationMethod.Tab);
        }
    }

    // color_picker.rs color_palettes: each row from its darkest shade to its lightest.
    private void BuildPalette()
    {
        if (_palette is null)
        {
            return;
        }
        _palette.Children.Clear();
        foreach (var row in GpuiColor.Palettes)
        {
            var panel = new SwatchRowPanel();
            foreach (var hsla in row)
            {
                panel.Children.Add(Swatch(hsla.ToColor(), hsla));
            }
            _palette.Children.Add(panel);
        }
    }

    private void BuildFeatured()
    {
        if (_featured is null)
        {
            return;
        }
        _featured.Children.Clear();
        var row = new SwatchRowPanel();
        if (FeaturedColors is { } colors)
        {
            foreach (var color in colors)
            {
                row.Children.Add(Swatch(color, null));
            }
        }
        else
        {
            foreach (var key in ThemeFeatured)
            {
                var swatch = Swatch(default, null);
                swatch.Bind(ColorSwatch.ColorProperty, swatch.GetResourceObservable(key, v => v is Color c ? c : default));
                row.Children.Add(swatch);
            }
        }
        _featured.Children.Add(row);
    }

    private ColorSwatch Swatch(Color color, GpuiHsla? exact)
    {
        var swatch = new ColorSwatch { Exact = exact, Color = color };
        // render_item: pointing previews the color, a click commits it and closes the popover.
        swatch.PointerEntered += (_, _) => Preview(swatch.Exact ?? GpuiHsla.From(swatch.Color), swatch.Exact?.ToHex() ?? GpuiColor.Hex(swatch.Color));
        swatch.Click += (_, _) => Commit(swatch.Color, swatch.Exact, close: true);
        return swatch;
    }

    // ColorPickerState::preview_color: the preview and the hex field, not the value.
    private void Preview(GpuiHsla hsla, string hex)
    {
        _preview = hsla;
        PreviewColor = hsla.ToColor();
        PseudoClasses.Set(":has-preview", true);
        WriteHex(hex);
        // The sliders keep their values; their labels and tracks show the preview.
        UpdateSliders(write: false);
    }

    // select_color (a swatch or the hex: closes) or update_value_from_slider (stays open,
    // leaves the sliders alone): the value, the preview and the hex field.
    private void Commit(Color color, GpuiHsla? exact, bool close, bool fromSlider = false)
    {
        if (close)
        {
            IsDropDownOpen = false;
        }
        _exact = exact;
        _committing = true;
        _fromSlider = fromSlider;
        try
        {
            SetCurrentValue(ColorProperty, color);
            // The same color again raises no change: show it anyway.
            ShowValue();
        }
        finally
        {
            _committing = false;
            _fromSlider = false;
        }
    }

    // update_value: everything follows the committed color.
    private void ShowValue()
    {
        var value = Value;
        DisplayText = _exact?.ToHex() ?? (Color is { } c ? GpuiColor.Hex(c) : null);
        PseudoClasses.Set(":has-color", Color is not null);
        _preview = value;
        PreviewColor = Color;
        PseudoClasses.Set(":has-preview", value is not null);
        WriteHex(DisplayText ?? string.Empty);
        // No color writes nothing to the sliders (update_value writes Some only), nor a slider's own change.
        UpdateSliders(write: value is not null && !_fromSlider);
    }

    private void WriteHex(string text)
    {
        _acceptedHex = text;
        if (_hexBox is null || _hexBox.Text == text)
        {
            return;
        }
        _writingHex = true;
        try
        {
            _hexBox.Text = text;
            _hexBox.CaretIndex = text.Length;
        }
        finally
        {
            _writingHex = false;
        }
    }

    // ColorPickerState's hex field: only text HEX_PATTERN accepts; a parsed hex previews.
    private void OnHexChanged(object? sender, TextChangedEventArgs e)
    {
        if (_writingHex || _hexBox is null)
        {
            return;
        }
        var text = _hexBox.Text ?? string.Empty;
        // Our own write (TextChanged may come after it): GPUI's suppress_input_change.
        if (text == _acceptedHex)
        {
            return;
        }
        if (text.Length > 0 && !GpuiColor.IsHexInput(text))
        {
            var caret = Math.Max(0, _hexBox.CaretIndex - (text.Length - _acceptedHex.Length));
            WriteHex(_acceptedHex);
            _hexBox.CaretIndex = Math.Min(caret, _acceptedHex.Length);
            return;
        }
        _acceptedHex = text;
        if (GpuiColor.ParseHex(text) is { } color)
        {
            // preview_hex: the preview without rewriting the field.
            _preview = GpuiHsla.From(color);
            PreviewColor = color;
            PseudoClasses.Set(":has-preview", true);
            UpdateSliders(write: true);
        }
    }

    // InputEvent::PressEnter: commit_hex, which closes the popover when the hex parses.
    private void OnHexKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None && _hexBox is not null)
        {
            if (GpuiColor.ParseHex(_hexBox.Text) is { } color)
            {
                Commit(color, null, close: true);
                RefocusAfterClose();
            }
            e.Handled = true;
        }
    }

    // The labels and tracks show the previewed color (displayed_color), black without one;
    // the thumbs move only with a committed or typed color (HslaSliders::write).
    private void UpdateSliders(bool write)
    {
        var color = _preview ?? new GpuiHsla(0, 0, 0, 1);
        if (write)
        {
            _writingSliders = true;
            try
            {
                SetSlider(_hue, color.H);
                SetSlider(_saturation, color.S);
                SetSlider(_lightness, color.L);
                SetSlider(_alpha, color.A);
            }
            finally
            {
                _writingSliders = false;
            }
        }
        static string Number(float v) => Math.Round(v, MidpointRounding.ToEven).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_hueText is not null)
        {
            _hueText.Text = Number(color.H * 360f);
        }
        if (_saturationText is not null)
        {
            _saturationText.Text = Number(color.S * 100f);
        }
        if (_lightnessText is not null)
        {
            _lightnessText.Text = Number(color.L * 100f);
        }
        if (_alphaText is not null)
        {
            _alphaText.Text = Number(color.A * 100f);
        }
        if (_saturationTrack is not null)
        {
            _saturationTrack.Background = Gradient(new GpuiHsla(color.H, 0, color.L, 1), new GpuiHsla(color.H, 1, color.L, 1));
        }
        if (_alphaTrack is not null)
        {
            _alphaTrack.Background = Gradient(color with { A = 0 }, color with { A = 1 });
        }
        _lightnessTrack?.SetColors(Enumerable.Range(0, TrackSteps).Select(ix => new GpuiHsla(color.H, 1, ix / (float)(TrackSteps - 1), 1).ToColor()));
    }

    private static void SetSlider(Slider? slider, float value)
    {
        if (slider is not null)
        {
            slider.Value = value;
        }
    }

    // linear_gradient(90deg, start 0%, end 100%).
    private static LinearGradientBrush Gradient(GpuiHsla start, GpuiHsla end) => new()
    {
        StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
        GradientStops = { new GradientStop(start.ToColor(), 0), new GradientStop(end.ToColor(), 1) },
    };

    // update_value_from_slider: a moved slider commits the four components and keeps the popover open.
    private void OnSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_writingSliders || _hue is null || _saturation is null || _lightness is null || _alpha is null)
        {
            return;
        }
        var hsla = new GpuiHsla((float)_hue.Value, (float)_saturation.Value, (float)_lightness.Value, (float)_alpha.Value);
        Commit(hsla.ToColor(), hsla, close: false, fromSlider: true);
    }
}

/// <summary>
/// GPUI Kit's ColorSwatch (base color_picker.rs): a selectable color in
/// <see cref="ColorSelect"/>'s palette, a button filled with
/// <see cref="Color"/> whose border is the color darkened; under the pointer
/// it lightens, pressed it darkens (color_picker.rs render_item).
/// </summary>
public class ColorSwatch : Button
{
    /// <summary>The swatch's color.</summary>
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<ColorSwatch, Color>(nameof(Color));

    static ColorSwatch()
    {
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<ColorSwatch>(AutomationControlType.RadioButton);
    }

    /// <inheritdoc cref="ColorProperty"/>
    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>The color as GPUI Kit keeps it, for a palette color (its hex is GPUI's).</summary>
    internal GpuiHsla? Exact { get; set; }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(ColorSwatch);

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ColorProperty)
        {
            AutomationProperties.SetName(this, Exact?.ToHex() ?? GpuiColor.Hex(Color));
        }
    }
}

/// <summary>
/// A row of swatches 4px apart (h_flex gap_1 of w_5 h_5 swatches): each is 20px
/// while the row has room, and all shrink alike when it has not, as the
/// featured row's twelve do (taffy's flex shrink), edges on device pixels.
/// </summary>
internal sealed class SwatchRowPanel : Panel
{
    private const double Gap = 4;
    private const double Side = 20;

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
        {
            child.Measure(new Size(Side, Side));
        }
        var width = Side * Children.Count + Gap * Math.Max(0, Children.Count - 1);
        return new Size(double.IsInfinity(availableSize.Width) ? width : Math.Min(width, availableSize.Width), Children.Count > 0 ? Side : 0);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var n = Children.Count;
        if (n == 0)
        {
            return finalSize;
        }
        // Equal swatches shrink alike, never below their 2px of border.
        var shrink = Math.Max(0, Side * n + Gap * (n - 1) - finalSize.Width) / n;
        var widths = Enumerable.Repeat(Math.Max(2, Side - shrink), n).ToList();
        var runs = LayoutSnap.GpuiRuns(this, 0, widths, Gap);
        for (var i = 0; i < n; i++)
        {
            Children[i].Arrange(new Rect(runs[i].Start, 0, runs[i].Length, Side));
        }
        return finalSize;
    }
}

/// <summary>
/// A slider track of flat color steps sharing its width (render_slider_track:
/// an h_flex of flex_1 divs). GPUI lays the steps out in device pixels in
/// single precision, each one's left the sum of the widths before it, and
/// snaps its absolute edges (taffy.rs layout_bounds); the steps here are
/// placed by the same sums from the track's place in the window.
/// </summary>
internal sealed class StripPanel : Panel
{
    private float _origin;

    public StripPanel()
    {
        // The window position is known after a layout pass: lay out again when it was not the one used.
        LayoutUpdated += (_, _) =>
        {
            if (Origin() is { } origin && origin != _origin)
            {
                _origin = origin;
                InvalidateArrange();
            }
        };
    }

    public void SetColors(IEnumerable<Color> colors)
    {
        var list = colors.ToList();
        while (Children.Count > list.Count)
        {
            Children.RemoveAt(Children.Count - 1);
        }
        while (Children.Count < list.Count)
        {
            Children.Add(new Border());
        }
        for (var i = 0; i < list.Count; i++)
        {
            ((Border)Children[i]).Background = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(list[i]);
        }
    }

    // The track's left edge in the window, in device pixels.
    private float? Origin()
    {
        var root = TopLevel.GetTopLevel(this);
        return root is not null && this.TranslatePoint(default, root) is { } at
            ? (float)(at.X * LayoutHelper.GetLayoutScale(this))
            : null;
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    protected override Size ArrangeOverride(Size finalSize)
    {
        var n = Children.Count;
        if (n == 0)
        {
            return finalSize;
        }
        var scale = LayoutHelper.GetLayoutScale(this);
        var step = (float)(finalSize.Width * scale) / n;
        static float Snap(float v) => MathF.CopySign(MathF.Ceiling(MathF.Abs(v) - 0.5f), v);
        var start = Snap(_origin);
        var x = 0f;
        for (var i = 0; i < n; i++)
        {
            var left = Snap(_origin + x) - start;
            x += step;
            var right = i == n - 1 ? (float)Math.Round(finalSize.Width * scale) : Snap(_origin + x) - start;
            Children[i].Arrange(new Rect(left / scale, 0, (right - left) / scale, finalSize.Height));
        }
        return finalSize;
    }
}
