using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Select titles its trigger with the item under the list cursor
/// while the list is open (select.rs display_title reads the list's selected
/// index); the committed item keeps its check mark. Avalonia's cursor is the
/// focused item, so <see cref="CursorItemProperty"/> follows it while the
/// dropdown is open, for the theme to show before the selection.
/// </summary>
public static class Selects
{
    /// <summary>Set by the ComboBox theme to track the cursor item.</summary>
    public static readonly AttachedProperty<bool> TracksCursorProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("TracksCursor", typeof(Selects));

    /// <summary>The content of the focused item while the dropdown is open, otherwise null.</summary>
    public static readonly AttachedProperty<object?> CursorItemProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, object?>("CursorItem", typeof(Selects));

    private static readonly AttachedProperty<bool> HandlersProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("Handlers", typeof(Selects));

    static Selects()
    {
        TracksCursorProperty.Changed.AddClassHandler<ComboBox>((box, e) =>
        {
            if (!e.GetNewValue<bool>())
            {
                box.ClearValue(CursorItemProperty);
                return;
            }
            if (box.GetValue(HandlersProperty))
            {
                return;
            }
            box.SetValue(HandlersProperty, true);
            box.AddHandler(InputElement.GotFocusEvent, (_, args) =>
            {
                if (GetTracksCursor(box) && box.IsDropDownOpen && args.Source is Visual source &&
                    source.FindAncestorOfType<ComboBoxItem>(includeSelf: true) is { } item &&
                    box.IndexFromContainer(item) >= 0)
                {
                    box.SetValue(CursorItemProperty, item.Content ?? box.ItemFromContainer(item));
                }
            }, RoutingStrategies.Bubble, handledEventsToo: true);
            box.PropertyChanged += (_, change) =>
            {
                if (change.Property == ComboBox.IsDropDownOpenProperty && !change.GetNewValue<bool>())
                {
                    box.ClearValue(CursorItemProperty);
                }
            };
        });
    }

    /// <summary>Gets whether the ComboBox tracks its cursor item.</summary>
    public static bool GetTracksCursor(ComboBox box) => box.GetValue(TracksCursorProperty);

    /// <summary>Sets whether the ComboBox tracks its cursor item.</summary>
    public static void SetTracksCursor(ComboBox box, bool value) => box.SetValue(TracksCursorProperty, value);

    /// <summary>Gets the item under the open list's cursor.</summary>
    public static object? GetCursorItem(ComboBox box) => box.GetValue(CursorItemProperty);
}
