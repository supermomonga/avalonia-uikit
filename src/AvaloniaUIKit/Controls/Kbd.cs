using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Kbd (kbd.rs): a keystroke on a small muted key cap, written as
/// GPUI writes it on the running platform (macOS: symbols, no separator;
/// elsewhere: names joined by "+"). Resolving an action's key binding is not
/// included; the app passes the gesture. The outline class draws the cap
/// with a border on the background.
/// </summary>
public class Kbd : TemplatedControl
{
    /// <summary>The keystroke to show.</summary>
    public static readonly StyledProperty<KeyGesture?> GestureProperty =
        AvaloniaProperty.Register<Kbd, KeyGesture?>(nameof(Gesture));

    /// <summary>The gesture in GPUI's notation.</summary>
    public static readonly DirectProperty<Kbd, string> TextProperty =
        AvaloniaProperty.RegisterDirect<Kbd, string>(nameof(Text), k => k.Text);

    private string _text = "";

    static Kbd()
    {
        GestureProperty.Changed.AddClassHandler<Kbd>((k, e) =>
            k.Text = e.GetNewValue<KeyGesture?>() is { } g ? Format(g, OperatingSystem.IsMacOS()) : "");
    }

    /// <inheritdoc cref="GestureProperty"/>
    public KeyGesture? Gesture
    {
        get => GetValue(GestureProperty);
        set => SetValue(GestureProperty, value);
    }

    /// <inheritdoc cref="TextProperty"/>
    public string Text
    {
        get => _text;
        private set => SetAndRaise(TextProperty, ref _text, value);
    }

    /// <summary>
    /// Writes a gesture as GPUI's <c>Kbd::format</c> does: modifiers in the
    /// order Control, Alt, Shift, platform key (Command on macOS, Windows
    /// elsewhere), then the key.
    /// </summary>
    public static string Format(KeyGesture gesture, bool mac)
    {
        var parts = new List<string>();
        var m = gesture.KeyModifiers;
        if (m.HasFlag(KeyModifiers.Control))
        {
            parts.Add(mac ? "⌃" : "Ctrl");
        }
        if (m.HasFlag(KeyModifiers.Alt))
        {
            parts.Add(mac ? "⌥" : "Alt");
        }
        if (m.HasFlag(KeyModifiers.Shift))
        {
            parts.Add(mac ? "⇧" : "Shift");
        }
        if (m.HasFlag(KeyModifiers.Meta))
        {
            parts.Add(mac ? "⌘" : "Win");
        }
        parts.Add(KeyText(GpuiKey(gesture.Key), mac));
        return string.Join(mac ? "" : "+", parts);
    }

    // GPUI's key names (Keystroke::key) for Avalonia's keys.
    private static string GpuiKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => ((char)('a' + (key - Key.A))).ToString(),
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        >= Key.NumPad0 and <= Key.NumPad9 => ((char)('0' + (key - Key.NumPad0))).ToString(),
        >= Key.F1 and <= Key.F24 => "f" + (key - Key.F1 + 1).ToString(CultureInfo.InvariantCulture),
        Key.Space => "space",
        Key.Back => "backspace",
        Key.Delete => "delete",
        Key.Escape => "escape",
        Key.Enter => "enter",
        Key.PageDown => "pagedown",
        Key.PageUp => "pageup",
        Key.Left => "left",
        Key.Right => "right",
        Key.Up => "up",
        Key.Down => "down",
        Key.Tab => "tab",
        Key.Home => "home",
        Key.End => "end",
        Key.Insert => "insert",
        Key.OemPlus or Key.Add => "+",
        Key.OemMinus or Key.Subtract => "-",
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        Key.OemQuestion or Key.Divide => "/",
        Key.OemSemicolon => ";",
        Key.OemQuotes => "'",
        Key.OemOpenBrackets => "[",
        Key.OemCloseBrackets => "]",
        Key.OemPipe => "\\",
        Key.OemTilde => "`",
        Key.Multiply => "*",
        _ => key.ToString().ToLowerInvariant(),
    };

    private static string KeyText(string key, bool mac) => (key, mac) switch
    {
        ("space", true) => "Space",
        ("backspace", true) or ("delete", true) => "⌫",
        ("backspace", false) => "Backspace",
        ("delete", false) => "Delete",
        ("escape", true) => "⎋",
        ("escape", false) => "Esc",
        ("enter", true) => "⏎",
        ("enter", false) => "Enter",
        ("pagedown", _) => "Page Down",
        ("pageup", _) => "Page Up",
        ("left", true) => "←",
        ("right", true) => "→",
        ("up", true) => "↑",
        ("down", true) => "↓",
        ("left", false) => "Left",
        ("right", false) => "Right",
        ("up", false) => "Up",
        ("down", false) => "Down",
        _ when key.Length == 1 => key.ToUpperInvariant(),
        _ => char.ToUpperInvariant(key[0]) + key[1..],
    };
}
