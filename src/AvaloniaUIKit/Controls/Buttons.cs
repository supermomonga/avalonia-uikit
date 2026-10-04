using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Button states that Avalonia's buttons lack (button.rs), as
/// attached properties for <see cref="Button"/> and the types derived from it
/// (ToggleButton, RepeatButton, DropDownButton, HyperlinkButton, the buttons
/// in a TextBox's inner content), and <see cref="SplitButton"/>, whose action
/// half they apply to (GPUI's DropdownButton with a loading Button).
/// <list type="bullet">
/// <item><see cref="IsLoadingProperty"/>: the button ignores the pointer, Enter
/// and Space, and raises no Click and runs no Command for an access key,
/// IsDefault or IsCancel, without the disabled look. The theme fades it to 80%,
/// drops the hover look and turns its icon into a spinner.</item>
/// <item><see cref="TakesFocusOnPointerProperty"/>: false keeps the focus where
/// it was when the button is pressed with the pointer; Tab still focuses it.</item>
/// </list>
/// </summary>
public static class Buttons
{
    /// <summary>
    /// Whether the button is loading (GPUI's <c>Button::loading</c>): as inert as a
    /// disabled one, but it keeps its own look, fades to 80% and shows a spinner in
    /// place of its icon (a PathIcon that is the content, or the first child of a
    /// panel that is).
    /// </summary>
    public static readonly AttachedProperty<bool> IsLoadingProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsLoading", typeof(Buttons));

    /// <summary>
    /// The icon a loading button spins (GPUI's <c>Button::loading_icon</c>); the
    /// Lucide loader by default. Inherited, so a container can set it for its buttons.
    /// </summary>
    public static readonly AttachedProperty<Geometry?> LoadingIconProperty =
        AvaloniaProperty.RegisterAttached<Control, Geometry?>("LoadingIcon", typeof(Buttons), inherits: true);

    /// <summary>
    /// Whether a pointer press focuses the button (Avalonia's default). GPUI's
    /// buttons do not take the focus on mouse down (button.rs: <c>prevent_default</c>),
    /// so a TextBox keeps its focus and selection while one of its buttons is
    /// pressed. Inherited, so a container (a TextBox, a toolbar, a window) can set
    /// it for every button inside.
    /// </summary>
    public static readonly AttachedProperty<bool> TakesFocusOnPointerProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("TakesFocusOnPointer", typeof(Buttons), true, inherits: true);

    static Buttons()
    {
        // Tunnel handlers run before the press reaches the button's content, so a
        // handled press neither focuses nor presses the button (FocusManager and
        // Button.OnPointerPressed skip handled events).
        InputElement.PointerPressedEvent.AddClassHandler<Button>(BlockPointer, RoutingStrategies.Tunnel);
        InputElement.PointerReleasedEvent.AddClassHandler<Button>(BlockPointer, RoutingStrategies.Tunnel);
        InputElement.KeyDownEvent.AddClassHandler<Button>(BlockKey, RoutingStrategies.Tunnel);
        InputElement.KeyUpEvent.AddClassHandler<Button>(BlockKey, RoutingStrategies.Tunnel);
        // Access keys, IsDefault, IsCancel and a HotKey without a Command reach
        // Button.OnClick directly: a handled Click runs neither the app's handlers
        // nor the Command.
        Button.ClickEvent.AddClassHandler<Button>(BlockClick);
        InputElement.GettingFocusEvent.AddClassHandler<Button>(KeepFocus);
        // A SplitButton loads its action half (the template passes IsLoading to it);
        // its own Enter and Space click that half, and its menu half stays live.
        InputElement.KeyDownEvent.AddClassHandler<SplitButton>(BlockKey, RoutingStrategies.Tunnel);
        InputElement.KeyUpEvent.AddClassHandler<SplitButton>(BlockKey, RoutingStrategies.Tunnel);
        SplitButton.ClickEvent.AddClassHandler<SplitButton>(BlockClick);
        InputElement.GettingFocusEvent.AddClassHandler<SplitButton>(KeepFocus);
    }

    /// <summary>Gets whether the button is loading.</summary>
    public static bool GetIsLoading(Control control) => control.GetValue(IsLoadingProperty);

    /// <summary>Sets whether the button is loading.</summary>
    public static void SetIsLoading(Control control, bool value) => control.SetValue(IsLoadingProperty, value);

    /// <summary>Gets the icon a loading button spins.</summary>
    public static Geometry? GetLoadingIcon(Control control) => control.GetValue(LoadingIconProperty);

    /// <summary>Sets the icon a loading button spins.</summary>
    public static void SetLoadingIcon(Control control, Geometry? value) => control.SetValue(LoadingIconProperty, value);

    /// <summary>Gets whether a pointer press focuses the button.</summary>
    public static bool GetTakesFocusOnPointer(Control control) => control.GetValue(TakesFocusOnPointerProperty);

    /// <summary>Sets whether a pointer press focuses the button.</summary>
    public static void SetTakesFocusOnPointer(Control control, bool value) => control.SetValue(TakesFocusOnPointerProperty, value);

    // button.rs: a loading button stops a left mouse down and a click from going
    // on, "it is not waiting for another click".
    private static void BlockPointer(Button button, PointerEventArgs e)
    {
        var left = e is PointerReleasedEventArgs released
            ? released.InitialPressMouseButton == MouseButton.Left
            : e.GetCurrentPoint(button).Properties.IsLeftButtonPressed;
        if (left && GetIsLoading(button))
        {
            e.Handled = true;
        }
    }

    // Button.OnKeyDown and SplitButton.OnKeyUp click on Enter, and on Space while focused.
    private static void BlockKey(Control button, KeyEventArgs e)
    {
        if (GetIsLoading(button) && (e.Key == Key.Enter || (e.Key == Key.Space && button.IsFocused)))
        {
            e.Handled = true;
        }
    }

    private static void BlockClick(Control button, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.Source, button) && GetIsLoading(button))
        {
            e.Handled = true;
        }
    }

    // FocusManager focuses the first focusable element up from what the pointer
    // pressed; cancelling it there keeps the focus where it was.
    private static void KeepFocus(Control button, FocusChangingEventArgs e)
    {
        if (e.NavigationMethod == NavigationMethod.Pointer &&
            ReferenceEquals(e.NewFocusedElement, button) &&
            !GetTakesFocusOnPointer(button))
        {
            e.TryCancel();
        }
    }
}
