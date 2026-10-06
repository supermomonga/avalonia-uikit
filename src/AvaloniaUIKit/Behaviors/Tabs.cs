using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// The sliding indicator of GPUI Kit's TabBar (tab_bar.rs render_indicator),
/// for the TabStrip and TabControl templates, and the bar's prefix and suffix.
/// Closing, adding and dragging tabs are in Controls/Tabs.Editing.cs.
/// </summary>
public static partial class Tabs
{
    /// <summary>
    /// Content at the start of a TabStrip's or TabControl's bar, before the
    /// tabs (tab_bar.rs prefix), centered on the bar.
    /// </summary>
    public static readonly AttachedProperty<object?> PrefixProperty =
        AvaloniaProperty.RegisterAttached<SelectingItemsControl, object?>("Prefix", typeof(Tabs));

    /// <summary>
    /// Content at the end of a TabStrip's or TabControl's bar, after the tabs
    /// and the menu button (tab_bar.rs suffix). The tabs then end with 12px of
    /// space (last_empty_space), as with the menu class.
    /// </summary>
    public static readonly AttachedProperty<object?> SuffixProperty =
        AvaloniaProperty.RegisterAttached<SelectingItemsControl, object?>("Suffix", typeof(Tabs));

    /// <summary>
    /// Makes the element (in a Canvas of a TabStrip or TabControl template)
    /// the selected tab's indicator: it is placed over the selected tab, and
    /// its child springs to the tab's width (Motion.SpringTarget). Hidden
    /// while no tab is selected, and while a tab is dragged: the dragged tab,
    /// with the :dragging pseudo-class, draws it over the other tabs.
    /// </summary>
    public static readonly AttachedProperty<bool> IndicatorProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Indicator", typeof(Tabs));

    /// <summary>
    /// On a tab: when the tab becomes selected by a switch (not the first
    /// selection), its foreground runs this transition from
    /// <see cref="SelectionFadeFromProperty"/>, as a GPUI pill tab's label does.
    /// </summary>
    public static readonly AttachedProperty<ColorTransition?> SelectionFadeProperty =
        AvaloniaProperty.RegisterAttached<Control, ColorTransition?>("SelectionFade", typeof(Tabs));

    /// <summary>The foreground a <see cref="SelectionFadeProperty"/> transition starts from.</summary>
    public static readonly AttachedProperty<IBrush?> SelectionFadeFromProperty =
        AvaloniaProperty.RegisterAttached<Control, IBrush?>("SelectionFadeFrom", typeof(Tabs));

    private static readonly AttachedProperty<IndicatorTracker?> TrackerProperty =
        AvaloniaProperty.RegisterAttached<Control, IndicatorTracker?>("Tracker", typeof(Tabs));

    static Tabs()
    {
        IndicatorProperty.Changed.AddClassHandler<Control>((element, e) =>
        {
            if (e.GetNewValue<bool>() && element.GetValue(TrackerProperty) is null)
            {
                element.SetValue(TrackerProperty, new IndicatorTracker(element));
            }
        });
        RegisterEditing();
    }

    /// <summary>
    /// Raised by a tab drag on the bar whose tab it moves: with true after each
    /// move of the dragged tab, with false once the tabs are back in their places,
    /// just before the drop changes the items.
    /// </summary>
    private static event Action<SelectingItemsControl, bool>? Dragged;

    /// <summary>Gets the bar's prefix.</summary>
    public static object? GetPrefix(SelectingItemsControl element) => element.GetValue(PrefixProperty);

    /// <summary>Sets the bar's prefix.</summary>
    public static void SetPrefix(SelectingItemsControl element, object? value) => element.SetValue(PrefixProperty, value);

    /// <summary>Gets the bar's suffix.</summary>
    public static object? GetSuffix(SelectingItemsControl element) => element.GetValue(SuffixProperty);

    /// <summary>Sets the bar's suffix.</summary>
    public static void SetSuffix(SelectingItemsControl element, object? value) => element.SetValue(SuffixProperty, value);

    /// <summary>Gets whether the element is a tab indicator.</summary>
    public static bool GetIndicator(Control element) => element.GetValue(IndicatorProperty);

    /// <summary>Sets whether the element is a tab indicator.</summary>
    public static void SetIndicator(Control element, bool value) => element.SetValue(IndicatorProperty, value);

    /// <summary>Gets the tab's selection fade.</summary>
    public static ColorTransition? GetSelectionFade(Control element) => element.GetValue(SelectionFadeProperty);

    /// <summary>Sets the tab's selection fade.</summary>
    public static void SetSelectionFade(Control element, ColorTransition? value) => element.SetValue(SelectionFadeProperty, value);

    /// <summary>Gets the foreground the tab's selection fade starts from.</summary>
    public static IBrush? GetSelectionFadeFrom(Control element) => element.GetValue(SelectionFadeFromProperty);

    /// <summary>Sets the foreground the tab's selection fade starts from.</summary>
    public static void SetSelectionFadeFrom(Control element, IBrush? value) => element.SetValue(SelectionFadeFromProperty, value);

    /// <summary>
    /// Follows the owner's selected tab after every layout. The springs on the
    /// indicator take the first place at once and travel to later ones.
    /// </summary>
    private sealed class IndicatorTracker
    {
        private readonly Control _indicator;
        private SelectingItemsControl? _owner;
        private bool _placed;
        private bool _dragging;
        private bool _dropping;
        private CancellationTokenSource? _fade;

        public IndicatorTracker(Control indicator)
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
            // A drop while detached went unseen; a drag still going says so at its next move.
            _dragging = false;
            _owner = _indicator.TemplatedParent as SelectingItemsControl;
            if (_owner is null)
            {
                return;
            }
            _owner.SelectionChanged += OnSelectionChanged;
            _owner.LayoutUpdated += OnLayoutUpdated;
            Dragged += OnDragged;
            Place();
        }

        private void Detach()
        {
            if (_owner is null)
            {
                return;
            }
            _owner.SelectionChanged -= OnSelectionChanged;
            _owner.LayoutUpdated -= OnLayoutUpdated;
            Dragged -= OnDragged;
            _owner = null;
        }

        private void OnLayoutUpdated(object? sender, EventArgs e) => Place();

        // The dragged tab draws the indicator itself (:dragging), over the tabs it passes,
        // while this one hides under them. The drop puts the tab back in its place, by its
        // render transform, which lays nothing out: take the place at once, before the drop
        // changes the items. The drop reselects the tab, which is no switch to fade or spring
        // to; the springs travel again once the dropped tabs are laid out.
        private void OnDragged(SelectingItemsControl owner, bool moving)
        {
            if (owner != _owner)
            {
                return;
            }
            _dragging = moving;
            Travel(false);
            Place();
            if (moving)
            {
                return;
            }
            _dropping = true;
            Dispatcher.UIThread.Post(() =>
            {
                _dropping = false;
                Travel(true);
            }, DispatcherPriority.Background);
        }

        private void Travel(bool travel)
        {
            foreach (var element in _indicator.GetSelfAndVisualDescendants().OfType<Layoutable>())
            {
                Motion.SetSpringTravel(element, travel);
            }
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.Source != _owner)
            {
                return;
            }
            _fade?.Cancel();
            _fade = null;
            if (_placed && !_dropping && _owner!.ContainerFromIndex(_owner.SelectedIndex) is { IsEffectivelyEnabled: true } tab &&
                GetSelectionFade(tab) is { } fade &&
                GetSelectionFadeFrom(tab) is ISolidColorBrush from && tab.GetValue(TemplatedControl.ForegroundProperty) is ISolidColorBrush to)
            {
                _fade = new CancellationTokenSource();
                _ = fade.Build(TemplatedControl.ForegroundProperty, from.Color, to.Color).RunAsync(tab, _fade.Token);
            }
            Place();
        }

        private void Place()
        {
            var layer = _indicator.GetVisualParent();
            var tab = _owner?.ContainerFromIndex(_owner.SelectedIndex);
            if (_dragging || layer is null || tab is null || !tab.IsVisible || !tab.IsArrangeValid ||
                tab.TranslatePoint(default, layer) is not { } origin)
            {
                _indicator.IsVisible = false;
                return;
            }
            _indicator.IsVisible = true;
            Set(_indicator, Canvas.LeftProperty, origin.X);
            Set(_indicator, Canvas.TopProperty, origin.Y);
            Set(_indicator, Layoutable.HeightProperty, tab.Bounds.Height);
            foreach (var child in _indicator.GetVisualChildren().OfType<Layoutable>())
            {
                Set(child, Motion.SpringTargetProperty, tab.Bounds.Width);
            }
            _placed = true;
        }

        private static void Set(AvaloniaObject target, StyledProperty<double> property, double value)
        {
            if (target.GetValue(property) != value)
            {
                target.SetValue(property, value);
            }
        }
    }
}
