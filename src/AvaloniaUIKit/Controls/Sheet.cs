using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Sheet (sheet.rs, root.rs): a surface that slides in from an edge
/// of the window over a dimming overlay, opened from code with
/// <see cref="Show"/> (GPUI's <c>window.open_sheet_at</c>). It sits on the
/// window's <see cref="OverlayLayer"/>, so popups opened from it stay above
/// it. One sheet is open per window: showing another closes the open one and
/// takes over the focus to give back (root.rs <c>active_sheet</c>).
/// <para>
/// The surface is <see cref="Size"/> wide (Left, Right) or tall (Top, Bottom):
/// 350px by default, or a part of the window (<c>"50%"</c>). It has a title
/// row with the <see cref="Title"/> and a close button, the content in a
/// scrolling body inset by <see cref="TemplatedControl.Padding"/> (16px at the
/// sides), and the <see cref="Footer"/> when there is one. Opening focuses the
/// sheet and keeps Tab inside it; Escape, the close button and a press on the
/// overlay close it (the overlay only when <see cref="IsOverlayClosable"/>),
/// and the focus goes back to where it was. Without <see cref="HasOverlay"/>
/// the overlay is clear and a press on it does nothing (GPUI's
/// <c>overlay(false)</c>). <see cref="Closed"/> is raised however it closes.
/// </para>
/// </summary>
[TemplatePart("PART_Overlay", typeof(Control))]
[TemplatePart("PART_CloseButton", typeof(Button))]
[TemplatePart("PART_Root", typeof(Control))]
[TemplatePart("PART_Surface", typeof(Control))]
[PseudoClasses(":open", ":left", ":right", ":top", ":bottom")]
public class Sheet : ContentControl
{
    /// <summary>The title before the close button (GPUI's <c>title</c>).</summary>
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<Sheet, object?>(nameof(Title));

    /// <summary>The row below the body (GPUI's <c>footer</c>); none by default.</summary>
    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<Sheet, object?>(nameof(Footer));

    /// <summary>The window edge the sheet slides in from (GPUI's placement; Right by default).</summary>
    public static readonly StyledProperty<DrawerPlacement> PlacementProperty =
        AvaloniaProperty.Register<Sheet, DrawerPlacement>(nameof(Placement), DrawerPlacement.Right);

    /// <summary>
    /// The sheet's width (Left, Right) or height (Top, Bottom): pixels, or a part
    /// of the window such as <c>"50%"</c> (GPUI's <c>size</c>, 350px by default).
    /// </summary>
    public static readonly StyledProperty<RelativeScalar> SizeProperty =
        AvaloniaProperty.Register<Sheet, RelativeScalar>(nameof(Size), new RelativeScalar(350, RelativeUnit.Absolute));

    /// <summary>Whether the overlay dims the window (GPUI's <c>overlay</c>, true by default).</summary>
    public static readonly StyledProperty<bool> HasOverlayProperty =
        AvaloniaProperty.Register<Sheet, bool>(nameof(HasOverlay), true);

    /// <summary>Whether a press on the overlay closes the sheet (GPUI's <c>overlay_closable</c>, true by default).</summary>
    public static readonly StyledProperty<bool> IsOverlayClosableProperty =
        AvaloniaProperty.Register<Sheet, bool>(nameof(IsOverlayClosable), true);

    /// <summary>Whether the sheet is shown.</summary>
    public static readonly DirectProperty<Sheet, bool> IsOpenProperty =
        AvaloniaProperty.RegisterDirect<Sheet, bool>(nameof(IsOpen), s => s.IsOpen);

    private bool _isOpen;
    private OverlayLayer? _layer;
    private IInputElement? _restoreFocus;
    private Control? _overlay;
    private Button? _closeButton;
    private Control? _root;
    private Control? _surface;

    static Sheet()
    {
        KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<Sheet>(KeyboardNavigationMode.Cycle);
        AffectsMeasure<Sheet>(SizeProperty, PlacementProperty);
        PlacementProperty.Changed.AddClassHandler<Sheet>((sheet, _) => sheet.UpdatePlacement());
    }

    /// <summary>Creates a sheet.</summary>
    public Sheet() => UpdatePlacement();

    /// <inheritdoc cref="TitleProperty"/>
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="FooterProperty"/>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    /// <inheritdoc cref="PlacementProperty"/>
    public DrawerPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    /// <inheritdoc cref="SizeProperty"/>
    public RelativeScalar Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <inheritdoc cref="HasOverlayProperty"/>
    public bool HasOverlay
    {
        get => GetValue(HasOverlayProperty);
        set => SetValue(HasOverlayProperty, value);
    }

    /// <inheritdoc cref="IsOverlayClosableProperty"/>
    public bool IsOverlayClosable
    {
        get => GetValue(IsOverlayClosableProperty);
        set => SetValue(IsOverlayClosableProperty, value);
    }

    /// <inheritdoc cref="IsOpenProperty"/>
    public bool IsOpen
    {
        get => _isOpen;
        private set
        {
            SetAndRaise(IsOpenProperty, ref _isOpen, value);
            PseudoClasses.Set(":open", value);
        }
    }

    /// <summary>Raised when the sheet closes: by Escape, the close button, the overlay, <see cref="Close"/> or another sheet.</summary>
    public event EventHandler? Closed;

    /// <summary>The sheet open in the window of <paramref name="visual"/>, if any.</summary>
    public static Sheet? GetActive(Visual visual) =>
        OverlayLayer.GetOverlayLayer(visual)?.Children.OfType<Sheet>().FirstOrDefault(s => s.IsOpen);

    /// <summary>
    /// Shows the sheet over the window of <paramref name="visual"/> (any visual in
    /// it, or the window itself), closing the sheet open there.
    /// </summary>
    public void Show(Visual visual)
    {
        var layer = OverlayLayer.GetOverlayLayer(visual)
            ?? throw new InvalidOperationException("The visual is not in a window with an overlay layer.");
        if (IsOpen)
        {
            if (layer == _layer)
            {
                return;
            }
            Close();
        }
        var focus = TopLevel.GetTopLevel(visual)?.FocusManager?.GetFocusedElement();
        // root.rs open_sheet_at: the open sheet gives way, and its focus to give back
        // is the new sheet's.
        if (GetActive(layer) is { } active)
        {
            focus = active._restoreFocus ?? focus;
            active._restoreFocus = null;
            active.Close();
        }
        _restoreFocus = focus;
        _layer = layer;
        layer.Children.Add(this);
        IsOpen = true;
        // root.rs: the sheet takes the focus (its surface, which is no tab stop, so Tab
        // goes round the sheet's controls).
        ApplyTemplate();
        _surface?.Focus();
    }

    /// <summary>Closes the sheet and gives the focus back to where it was before it opened (root.rs close_sheet).</summary>
    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }
        var restore = _restoreFocus;
        _restoreFocus = null;
        _layer?.Children.Remove(this);
        _layer = null;
        IsOpen = false;
        if (restore is Visual visual && visual.IsAttachedToVisualTree() && restore.Focusable)
        {
            restore.Focus();
        }
        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _overlay?.RemoveHandler(PointerPressedEvent, OnOverlayPressed);
        _closeButton?.RemoveHandler(Button.ClickEvent, OnCloseClick);
        _overlay = e.NameScope.Find<Control>("PART_Overlay");
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");
        _root = e.NameScope.Find<Control>("PART_Root");
        _surface = e.NameScope.Find<Control>("PART_Surface");
        _overlay?.AddHandler(PointerPressedEvent, OnOverlayPressed);
        _closeButton?.AddHandler(Button.ClickEvent, OnCloseClick);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // sheet.rs: Escape is Cancel in the sheet's key context, which closes it.
        if (e.Key == Key.Escape && IsOpen)
        {
            Close();
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override Avalonia.Size MeasureOverride(Avalonia.Size availableSize)
    {
        // The surface's length: pixels, or a part of the window (DefiniteLength).
        if (_root is not null)
        {
            var horizontal = Placement is DrawerPlacement.Left or DrawerPlacement.Right;
            var whole = horizontal ? availableSize.Width : availableSize.Height;
            var length = Size.ToValue(double.IsFinite(whole) ? whole : 0);
            _root.Width = horizontal ? length : double.NaN;
            _root.Height = horizontal ? double.NaN : length;
        }
        var desired = base.MeasureOverride(availableSize);
        // The sheet covers the whole layer, as GPUI's host covers the window.
        return new Avalonia.Size(
            double.IsFinite(availableSize.Width) ? availableSize.Width : desired.Width,
            double.IsFinite(availableSize.Height) ? availableSize.Height : desired.Height);
    }

    private void UpdatePlacement()
    {
        var placement = Placement;
        PseudoClasses.Set(":left", placement == DrawerPlacement.Left);
        PseudoClasses.Set(":right", placement == DrawerPlacement.Right);
        PseudoClasses.Set(":top", placement == DrawerPlacement.Top);
        PseudoClasses.Set(":bottom", placement == DrawerPlacement.Bottom);
    }

    // base/sheet.rs: the overlay takes every press; the left button closes the sheet
    // when there is an overlay and it is closable.
    private void OnOverlayPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        if (HasOverlay && IsOverlayClosable && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            Close();
        }
    }

    // sheet.rs: the close button dispatches Cancel.
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Close();
    }
}
