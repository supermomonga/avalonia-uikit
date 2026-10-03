using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Rating (rating.rs): a row of <see cref="Maximum"/> stars. The
/// pointer previews the value under it in <see cref="ActiveBrush"/>; a click
/// sets <see cref="Value"/> to that star, or to the star before when the
/// star is already filled. A disabled rating shows its value only.
/// </summary>
[TemplatePart("PART_Items", typeof(Panel))]
public class Rating : TemplatedControl
{
    /// <summary>The number of filled stars.</summary>
    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<Rating, int>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The number of stars (GPUI's default is 5).</summary>
    public static readonly StyledProperty<int> MaximumProperty =
        AvaloniaProperty.Register<Rating, int>(nameof(Maximum), 5);

    /// <summary>The color of filled and previewed stars (the theme's yellow by default).</summary>
    public static readonly StyledProperty<IBrush?> ActiveBrushProperty =
        AvaloniaProperty.Register<Rating, IBrush?>(nameof(ActiveBrush));

    /// <summary>The size of a star's icon, set by the size classes.</summary>
    public static readonly StyledProperty<double> StarSizeProperty =
        AvaloniaProperty.Register<Rating, double>(nameof(StarSize), 16);

    private Panel? _items;
    private int _hovered;
    private int _pressed;

    static Rating()
    {
        AffectsStars(ValueProperty, MaximumProperty, ActiveBrushProperty, StarSizeProperty, IsEffectivelyEnabledProperty);
    }

    private static void AffectsStars(params AvaloniaProperty[] properties)
    {
        foreach (var property in properties)
        {
            property.Changed.AddClassHandler<Rating>((r, _) => r.UpdateStars());
        }
    }

    /// <inheritdoc cref="ValueProperty"/>
    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <inheritdoc cref="MaximumProperty"/>
    public int Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <inheritdoc cref="ActiveBrushProperty"/>
    public IBrush? ActiveBrush
    {
        get => GetValue(ActiveBrushProperty);
        set => SetValue(ActiveBrushProperty, value);
    }

    /// <inheritdoc cref="StarSizeProperty"/>
    public double StarSize
    {
        get => GetValue(StarSizeProperty);
        set => SetValue(StarSizeProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _items = e.NameScope.Find<Panel>("PART_Items");
        UpdateStars();
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        SetHovered(IsEffectivelyEnabled ? StarAt(e.Source) : 0);
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        SetHovered(0);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _pressed = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ? StarAt(e.Source) : 0;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var star = StarAt(e.Source);
        if (star > 0 && star == _pressed)
        {
            // rating.rs: a click on a filled star clears down to the one before.
            SetCurrentValue(ValueProperty, Value >= star ? star - 1 : star);
            e.Handled = true;
        }
        _pressed = 0;
    }

    private static int StarAt(object? source) =>
        (source as Visual)?.GetSelfAndVisualAncestors().OfType<RatingStar>().FirstOrDefault()?.Index ?? 0;

    private void SetHovered(int star)
    {
        if (_hovered != star)
        {
            _hovered = star;
            UpdateStars();
        }
    }

    private void UpdateStars()
    {
        if (_items is null)
        {
            return;
        }
        var count = Math.Max(0, Maximum);
        while (_items.Children.Count > count)
        {
            _items.Children.RemoveAt(_items.Children.Count - 1);
        }
        while (_items.Children.Count < count)
        {
            _items.Children.Add(new RatingStar { Index = _items.Children.Count + 1 });
        }
        var hovered = IsEffectivelyEnabled ? _hovered : 0;
        foreach (var star in _items.Children.OfType<RatingStar>())
        {
            star.IsFilled = star.Index <= Value;
            star.IsActive = star.IsFilled || star.Index <= hovered;
            star.IconSize = StarSize;
            star.ActiveBrush = ActiveBrush;
        }
    }
}

/// <summary>One star of a <see cref="Rating"/>, with the :filled and :active pseudo-classes.</summary>
public class RatingStar : TemplatedControl
{
    /// <summary>The size of the star's icon.</summary>
    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<RatingStar, double>(nameof(IconSize), 16);

    /// <summary>The color of a filled or previewed star.</summary>
    public static readonly StyledProperty<IBrush?> ActiveBrushProperty =
        AvaloniaProperty.Register<RatingStar, IBrush?>(nameof(ActiveBrush));

    private bool _isFilled;
    private bool _isActive;

    /// <summary>The star's position, from 1.</summary>
    public int Index { get; init; }

    /// <inheritdoc cref="IconSizeProperty"/>
    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <inheritdoc cref="ActiveBrushProperty"/>
    public IBrush? ActiveBrush
    {
        get => GetValue(ActiveBrushProperty);
        set => SetValue(ActiveBrushProperty, value);
    }

    /// <summary>Whether the star is within the value.</summary>
    public bool IsFilled
    {
        get => _isFilled;
        set
        {
            _isFilled = value;
            PseudoClasses.Set(":filled", value);
        }
    }

    /// <summary>Whether the star is filled or previewed.</summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            PseudoClasses.Set(":active", value);
        }
    }
}
