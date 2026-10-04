using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's TabBar menu (tab_bar.rs menu(true)): a button in a TabsControl
/// template that opens a menu of the tabs, checks the selected one and selects
/// the one that is clicked. It is a Button and takes the Button theme.
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
            if (container is HeaderedContentControl { Header: not Control } tab)
            {
                item.Header = tab.Header;
                item.HeaderTemplate = tab.HeaderTemplate;
            }
            else
            {
                // A header that is a control is already shown by its tab.
                item.Header = (container as HeaderedContentControl)?.Header is TextBlock { Text: { } text } ? text : $"Tab {index + 1}";
            }
            var selected = index;
            item.Click += (_, _) => owner.SelectedIndex = selected;
            _menu.Items.Add(item);
        }
    }
}
