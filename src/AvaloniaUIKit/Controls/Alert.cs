using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Alert (alert.rs): a message (the content) with an icon and an
/// optional title, in a bordered box. Style classes pick the variant (info,
/// success, warning, error), the size (xsmall, small, large) and banner (full
/// width, no corners, no title). With <see cref="IsClosable"/> a close button
/// raises <see cref="CloseRequestedEvent"/>; hiding the alert is the app's,
/// as GPUI's on_close leaves it to the app.
/// </summary>
[TemplatePart("PART_CloseButton", typeof(Button))]
public class Alert : ContentControl
{
    /// <summary>The title above the message (not shown as a banner).</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<Alert, string?>(nameof(Title));

    /// <summary>The icon before the text; GPUI's default is the info icon.</summary>
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<Alert, Geometry?>(nameof(Icon));

    /// <summary>Whether the alert shows a close button.</summary>
    public static readonly StyledProperty<bool> IsClosableProperty =
        AvaloniaProperty.Register<Alert, bool>(nameof(IsClosable));

    /// <summary>Raised when the close button is clicked.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CloseRequestedEvent =
        RoutedEvent.Register<Alert, RoutedEventArgs>(nameof(CloseRequested), RoutingStrategies.Bubble);

    private Button? _closeButton;

    /// <inheritdoc cref="TitleProperty"/>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="IconProperty"/>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <inheritdoc cref="IsClosableProperty"/>
    public bool IsClosable
    {
        get => GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    /// <inheritdoc cref="CloseRequestedEvent"/>
    public event EventHandler<RoutedEventArgs>? CloseRequested
    {
        add => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_closeButton is not null)
        {
            _closeButton.Click -= OnCloseClick;
        }
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");
        if (_closeButton is not null)
        {
            _closeButton.Click += OnCloseClick;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(CloseRequestedEvent, this));
}
