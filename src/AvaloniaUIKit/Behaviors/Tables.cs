using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace AvaloniaUIKit;

/// <summary>
/// Styling hooks for GPUI Kit's Table and DataTable looks on TableView.
/// </summary>
/// <remarks>
/// The rows, cells and column headers of a TableView are created in code, so
/// the TableView's theme cannot reach them through its template. Its theme
/// sets these inherited values instead, and the row, cell and header themes
/// read them: one cell theme then serves both looks at every size.
/// </remarks>
public static class Tables
{
    /// <summary>The padding of a cell and of a column header (inherited).</summary>
    public static readonly AttachedProperty<Thickness> CellPaddingProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, Thickness>("CellPadding", typeof(Tables), new Thickness(8, 4), inherits: true);

    /// <summary>How a cell's content sits in its row (inherited).</summary>
    public static readonly AttachedProperty<VerticalAlignment> CellVerticalAlignmentProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, VerticalAlignment>("CellVerticalAlignment", typeof(Tables), VerticalAlignment.Top, inherits: true);

    /// <summary>
    /// The height of a row and of the header band, rule included, or NaN to
    /// fit the content (inherited).
    /// </summary>
    public static readonly AttachedProperty<double> RowHeightProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, double>("RowHeight", typeof(Tables), double.NaN, inherits: true);

    /// <summary>
    /// Whether a column header paints its resize handle (inherited). A hidden
    /// handle still resizes the column.
    /// </summary>
    public static readonly AttachedProperty<bool> ShowsResizeHandlesProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("ShowsResizeHandles", typeof(Tables), true, inherits: true);

    /// <summary>Makes <see cref="ShowsFocusRingProperty"/> follow focus and the last input.</summary>
    public static readonly AttachedProperty<bool> TracksKeyboardFocusProperty =
        AvaloniaProperty.RegisterAttached<InputElement, bool>("TracksKeyboardFocus", typeof(Tables));

    /// <summary>
    /// Whether focus is inside the table and the last input was the keyboard
    /// (set by the behavior). DataTable rings itself then: focus lands on a
    /// row in Avalonia, and arrow keys move it without :focus-visible.
    /// </summary>
    public static readonly AttachedProperty<bool> ShowsFocusRingProperty =
        AvaloniaProperty.RegisterAttached<InputElement, bool>("ShowsFocusRing", typeof(Tables));

    static Tables()
    {
        TracksKeyboardFocusProperty.Changed.AddClassHandler<InputElement>((element, e) =>
        {
            element.RemoveHandler(InputElement.GotFocusEvent, OnGotFocus);
            element.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            element.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            element.ClearValue(ShowsFocusRingProperty);
            if (e.GetNewValue<bool>())
            {
                element.AddHandler(InputElement.GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
                element.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
                element.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            }
        });
        InputElement.IsKeyboardFocusWithinProperty.Changed.AddClassHandler<InputElement>((element, e) =>
        {
            if (!e.GetNewValue<bool>() && element.GetValue(TracksKeyboardFocusProperty))
            {
                element.ClearValue(ShowsFocusRingProperty);
            }
        });
    }

    /// <summary>Gets whether the focus ring follows focus and the last input.</summary>
    public static bool GetTracksKeyboardFocus(InputElement element) => element.GetValue(TracksKeyboardFocusProperty);

    /// <summary>Sets whether the focus ring follows focus and the last input.</summary>
    public static void SetTracksKeyboardFocus(InputElement element, bool value) => element.SetValue(TracksKeyboardFocusProperty, value);

    /// <summary>Gets whether the table shows its focus ring.</summary>
    public static bool GetShowsFocusRing(InputElement element) => element.GetValue(ShowsFocusRingProperty);

    // Focus moved in by Tab or arrow keys rings the table, a click does not;
    // focus moved by the control itself keeps what the last input said.
    private static void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is not InputElement element)
        {
            return;
        }
        if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional)
        {
            element.SetValue(ShowsFocusRingProperty, true);
        }
        else if (e.NavigationMethod is NavigationMethod.Pointer)
        {
            element.ClearValue(ShowsFocusRingProperty);
        }
    }

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is InputElement { IsKeyboardFocusWithin: true } element)
        {
            element.SetValue(ShowsFocusRingProperty, true);
        }
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is InputElement element)
        {
            element.ClearValue(ShowsFocusRingProperty);
        }
    }

    /// <summary>Gets the padding of a cell.</summary>
    public static Thickness GetCellPadding(StyledElement element) => element.GetValue(CellPaddingProperty);

    /// <summary>Sets the padding of a cell.</summary>
    public static void SetCellPadding(StyledElement element, Thickness value) => element.SetValue(CellPaddingProperty, value);

    /// <summary>Gets how a cell's content sits in its row.</summary>
    public static VerticalAlignment GetCellVerticalAlignment(StyledElement element) => element.GetValue(CellVerticalAlignmentProperty);

    /// <summary>Sets how a cell's content sits in its row.</summary>
    public static void SetCellVerticalAlignment(StyledElement element, VerticalAlignment value) => element.SetValue(CellVerticalAlignmentProperty, value);

    /// <summary>Gets the height of a row.</summary>
    public static double GetRowHeight(StyledElement element) => element.GetValue(RowHeightProperty);

    /// <summary>Sets the height of a row.</summary>
    public static void SetRowHeight(StyledElement element, double value) => element.SetValue(RowHeightProperty, value);

    /// <summary>Gets whether a column header paints its resize handle.</summary>
    public static bool GetShowsResizeHandles(StyledElement element) => element.GetValue(ShowsResizeHandlesProperty);

    /// <summary>Sets whether a column header paints its resize handle.</summary>
    public static void SetShowsResizeHandles(StyledElement element, bool value) => element.SetValue(ShowsResizeHandlesProperty, value);
}
