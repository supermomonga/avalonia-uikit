using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// The sliding indicator of GPUI Kit's TabBar (tab_bar.rs render_indicator),
/// for the TabStrip and TabControl templates, and the bar's prefix and suffix.
/// </summary>
public static class Tabs
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
    /// while no tab is selected.
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
    }

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
            _owner = _indicator.TemplatedParent as SelectingItemsControl;
            if (_owner is null)
            {
                return;
            }
            _owner.SelectionChanged += OnSelectionChanged;
            _owner.LayoutUpdated += OnLayoutUpdated;
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
            _owner = null;
        }

        private void OnLayoutUpdated(object? sender, EventArgs e) => Place();

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.Source != _owner)
            {
                return;
            }
            _fade?.Cancel();
            _fade = null;
            if (_placed && _owner!.ContainerFromIndex(_owner.SelectedIndex) is { IsEffectivelyEnabled: true } tab &&
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
            if (layer is null || tab is null || !tab.IsVisible || !tab.IsArrangeValid ||
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
