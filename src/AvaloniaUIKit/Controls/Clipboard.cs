using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Clipboard (clipboard.rs): a ghost icon button that copies
/// <see cref="Text"/> to the clipboard, then shows a check for two seconds,
/// during which another click does nothing. Size classes are the Button's.
/// </summary>
public class Clipboard : Button
{
    /// <summary>The text to copy.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<Clipboard, string?>(nameof(Text));

    /// <summary>Whether the text was copied less than two seconds ago (the :copied pseudo-class).</summary>
    public static readonly DirectProperty<Clipboard, bool> IsCopiedProperty =
        AvaloniaProperty.RegisterDirect<Clipboard, bool>(nameof(IsCopied), c => c.IsCopied);

    /// <summary>Raised after the text is copied.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CopiedEvent =
        RoutedEvent.Register<Clipboard, RoutedEventArgs>(nameof(Copied), RoutingStrategies.Bubble);

    // clipboard.rs: the check shows for two seconds.
    private static readonly TimeSpan CopiedDuration = TimeSpan.FromSeconds(2);

    private bool _isCopied;
    private DispatcherTimer? _timer;

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(Clipboard);

    /// <inheritdoc cref="TextProperty"/>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <inheritdoc cref="IsCopiedProperty"/>
    public bool IsCopied
    {
        get => _isCopied;
        private set
        {
            SetAndRaise(IsCopiedProperty, ref _isCopied, value);
            PseudoClasses.Set(":copied", value);
        }
    }

    /// <inheritdoc cref="CopiedEvent"/>
    public event EventHandler<RoutedEventArgs>? Copied
    {
        add => AddHandler(CopiedEvent, value);
        remove => RemoveHandler(CopiedEvent, value);
    }

    /// <inheritdoc />
    protected override void OnClick()
    {
        if (IsCopied)
        {
            return;
        }
        base.OnClick();
        TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(Text ?? "");
        IsCopied = true;
        _timer?.Stop();
        _timer = new DispatcherTimer(CopiedDuration, DispatcherPriority.Normal, (_, _) =>
        {
            _timer?.Stop();
            IsCopied = false;
        });
        _timer.Start();
        RaiseEvent(new RoutedEventArgs(CopiedEvent, this));
    }
}
