using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's StatusBar (status_bar.rs): a bar with a top border and three
/// regions. <see cref="Left"/> and <see cref="Right"/> pin to the ends; the
/// content fills the middle, centered when both ends are set, at the end
/// when only <see cref="Left"/> is, and at the start otherwise. Each region
/// holds one element; put several in a horizontal StackPanel with Spacing 8.
/// </summary>
public class StatusBar : ContentControl
{
    /// <summary>The region pinned to the start.</summary>
    public static readonly StyledProperty<object?> LeftProperty =
        AvaloniaProperty.Register<StatusBar, object?>(nameof(Left));

    /// <summary>The region pinned to the end.</summary>
    public static readonly StyledProperty<object?> RightProperty =
        AvaloniaProperty.Register<StatusBar, object?>(nameof(Right));

    static StatusBar()
    {
        LeftProperty.Changed.AddClassHandler<StatusBar>((b, _) => b.Update());
        RightProperty.Changed.AddClassHandler<StatusBar>((b, _) => b.Update());
    }

    /// <inheritdoc cref="LeftProperty"/>
    public object? Left
    {
        get => GetValue(LeftProperty);
        set => SetValue(LeftProperty, value);
    }

    /// <inheritdoc cref="RightProperty"/>
    public object? Right
    {
        get => GetValue(RightProperty);
        set => SetValue(RightProperty, value);
    }

    private void Update()
    {
        PseudoClasses.Set(":left", Left is not null);
        PseudoClasses.Set(":right", Right is not null);
    }
}
