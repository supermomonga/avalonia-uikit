using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's TabBar menu (tab_bar.rs menu(true)): a button in a TabStrip,
/// TabControl or Tabalonia TabsControl template that opens a menu of the
/// tabs, checks the selected one, disables the disabled ones and selects the
/// one that is clicked. It is a Button and takes the Button theme.
/// </summary>
public class TabsMenuButton : Button
{
    private readonly MenuFlyout _menu = new() { Placement = PlacementMode.BottomEdgeAlignedRight };

    /// <summary>Creates the button and its menu.</summary>
    public TabsMenuButton()
    {
        Flyout = _menu;
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(Button);

    /// <inheritdoc />
    protected override void OnClick()
    {
        // Before the menu opens: the items it is measured with.
        Fill();
        base.OnClick();
    }

    private void Fill()
    {
        _menu.Items.Clear();
        if (TemplatedParent is not SelectingItemsControl owner)
        {
            return;
        }
        for (var index = 0; index < owner.ItemCount; index++)
        {
            var container = owner.ContainerFromIndex(index);
            var item = new MenuItem
            {
                ToggleType = MenuItemToggleType.CheckBox,
                IsChecked = owner.SelectedIndex == index,
                IsEnabled = container?.IsEnabled ?? true,
            };
            // A TabItem's label is its header, a TabStripItem's its content.
            var (label, template) = container switch
            {
                HeaderedContentControl tab => (tab.Header, tab.HeaderTemplate),
                ContentControl tab => (tab.Content, tab.ContentTemplate),
                _ => (null, null),
            };
            switch (label)
            {
                case not Control and not null:
                    item.Header = label;
                    item.HeaderTemplate = template;
                    break;
                // tab_bar.rs: an icon tab is listed by its icon. A control is already
                // shown by its tab, so the menu gets its own copy of the glyph.
                case PathIcon icon:
                    item.Header = new PathIcon { Data = icon.Data };
                    break;
                case TextBlock { Text: { } text }:
                    item.Header = text;
                    break;
                default:
                    item.Header = $"Tab {index + 1}";
                    break;
            }
            var selected = index;
            item.Click += (_, _) => owner.SelectedIndex = selected;
            _menu.Items.Add(item);
        }
    }
}
