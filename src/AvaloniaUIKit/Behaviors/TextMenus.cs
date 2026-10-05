using Avalonia;
using Avalonia.Input;

namespace AvaloniaUIKit;

/// <summary>
/// Values for the right-click menu the TextBox theme attaches (input.rs): the
/// platform's Select All gesture, as TextBox.CutGesture, CopyGesture and
/// PasteGesture give the others.
/// </summary>
internal static class TextMenus
{
    /// <summary>The platform's first Select All gesture (Ctrl+A, Cmd+A on macOS).</summary>
    public static KeyGesture? SelectAllGesture =>
        Application.Current?.PlatformSettings?.HotkeyConfiguration.SelectAll.FirstOrDefault();
}
