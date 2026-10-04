using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Tabalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>
/// The tab indicator of a Tabalonia TabsControl while its tabs are dragged.
/// </summary>
public static class DragTabs
{
    /// <summary>
    /// On the indicator in a TabsControl template (a Tabs.Indicator element):
    /// while a tab is dragged, the indicator and its children take their places
    /// at once (Motion.SpringTravel off), so the selected tab's indicator moves
    /// with the tab instead of springing after it. The springs travel again once
    /// the dropped tabs are laid out.
    /// </summary>
    public static readonly AttachedProperty<bool> FollowsDragProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("FollowsDrag", typeof(DragTabs));

    private static readonly AttachedProperty<DragFollower?> FollowerProperty =
        AvaloniaProperty.RegisterAttached<Control, DragFollower?>("Follower", typeof(DragTabs));

    static DragTabs()
    {
        FollowsDragProperty.Changed.AddClassHandler<Control>((element, e) =>
        {
            if (e.GetNewValue<bool>() && element.GetValue(FollowerProperty) is null)
            {
                element.SetValue(FollowerProperty, new DragFollower(element));
            }
        });
    }

    /// <summary>Gets whether the indicator follows dragged tabs at once.</summary>
    public static bool GetFollowsDrag(Control element) => element.GetValue(FollowsDragProperty);

    /// <summary>Sets whether the indicator follows dragged tabs at once.</summary>
    public static void SetFollowsDrag(Control element, bool value) => element.SetValue(FollowsDragProperty, value);

    private sealed class DragFollower
    {
        private readonly Control _indicator;
        private TabsControl? _owner;

        public DragFollower(Control indicator)
        {
            _indicator = indicator;
            indicator.AttachedToVisualTree += (_, _) => Attach();
            indicator.DetachedFromVisualTree += (_, _) => Detach();
            if (indicator.IsAttachedToVisualTree())
            {
                Attach();
            }
        }

        private void Attach()
        {
            Detach();
            _owner = _indicator.TemplatedParent as TabsControl;
            _owner?.AddHandler(DragTabItem.DragStarted, OnStarted, RoutingStrategies.Bubble, handledEventsToo: true);
            _owner?.AddHandler(DragTabItem.DragCompleted, OnCompleted, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        private void Detach()
        {
            _owner?.RemoveHandler(DragTabItem.DragStarted, OnStarted);
            _owner?.RemoveHandler(DragTabItem.DragCompleted, OnCompleted);
            _owner = null;
        }

        private void OnStarted(object? sender, RoutedEventArgs e) => Travel(false);

        // The drop reorders the items and lays the tabs out again: travel once that settles.
        private void OnCompleted(object? sender, RoutedEventArgs e) =>
            Dispatcher.UIThread.Post(() => Travel(true), DispatcherPriority.Background);

        private void Travel(bool travel)
        {
            foreach (var element in _indicator.GetSelfAndVisualDescendants().OfType<Layoutable>())
            {
                Motion.SetSpringTravel(element, travel);
            }
        }
    }
}
