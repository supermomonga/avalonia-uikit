using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Empty (empty.rs): a centered column of <see cref="Media"/>,
/// <see cref="Title"/> and <see cref="Description"/>, then the
/// <see cref="Actions"/>, then the content. Named EmptyState, as "Empty" says
/// little in an Avalonia app. The icon-media class frames the media as
/// GPUI's EmptyMediaVariant::Icon: a 32px muted square.
/// </summary>
public class EmptyState : ContentControl
{
    /// <summary>An icon, image or avatar above the title.</summary>
    public static readonly StyledProperty<object?> MediaProperty =
        AvaloniaProperty.Register<EmptyState, object?>(nameof(Media));

    /// <summary>The title.</summary>
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<EmptyState, object?>(nameof(Title));

    /// <summary>The supporting text under the title.</summary>
    public static readonly StyledProperty<object?> DescriptionProperty =
        AvaloniaProperty.Register<EmptyState, object?>(nameof(Description));

    /// <summary>Buttons or other controls under the header.</summary>
    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<EmptyState, object?>(nameof(Actions));

    /// <inheritdoc cref="MediaProperty"/>
    public object? Media
    {
        get => GetValue(MediaProperty);
        set => SetValue(MediaProperty, value);
    }

    /// <inheritdoc cref="TitleProperty"/>
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="DescriptionProperty"/>
    public object? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <inheritdoc cref="ActionsProperty"/>
    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }
}
