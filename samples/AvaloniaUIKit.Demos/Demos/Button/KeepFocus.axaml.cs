using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class ButtonKeepFocus
{
    // The buttons leave the focus and the selection in the TextBox, so they act on the selection.
    private void OnUpper(object? sender, RoutedEventArgs e) => Replace(s => s.ToUpperInvariant());

    private void OnLower(object? sender, RoutedEventArgs e) => Replace(s => s.ToLowerInvariant());

    private void Replace(Func<string, string> change)
    {
        var box = this.FindControl<TextBox>("Note")!;
        var (start, end) = (Math.Min(box.SelectionStart, box.SelectionEnd), Math.Max(box.SelectionStart, box.SelectionEnd));
        if (end > start && box.Text is { } text)
        {
            box.Text = text[..start] + change(text[start..end]) + text[end..];
            box.SelectionStart = start;
            box.SelectionEnd = end;
        }
    }
}
