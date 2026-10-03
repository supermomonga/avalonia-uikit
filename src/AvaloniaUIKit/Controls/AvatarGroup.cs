using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's AvatarGroup (avatar/avatar_group.rs): <see cref="Avatar"/>s in a
/// row, each overlapping the one before by 30% of its size with the first on
/// top, cut after <see cref="Limit"/>; with <see cref="ShowsEllipsis"/> a
/// "⋯" avatar follows a cut group. The group's size class (xsmall, small,
/// large) is given to every avatar.
/// </summary>
public class AvatarGroup : ItemsControl
{
    /// <summary>The number of avatars shown (GPUI's default is 3).</summary>
    public static readonly StyledProperty<int> LimitProperty =
        AvaloniaProperty.Register<AvatarGroup, int>(nameof(Limit), 3);

    /// <summary>Whether a "⋯" avatar follows when avatars are cut.</summary>
    public static readonly StyledProperty<bool> ShowsEllipsisProperty =
        AvaloniaProperty.Register<AvatarGroup, bool>(nameof(ShowsEllipsis));

    /// <summary>Whether the ellipsis avatar shows (set by the group).</summary>
    public static readonly DirectProperty<AvatarGroup, bool> IsCutProperty =
        AvaloniaProperty.RegisterDirect<AvatarGroup, bool>(nameof(IsCut), g => g.IsCut);

    private static readonly string[] SizeClasses = ["xsmall", "small", "large"];

    private bool _isCut;

    static AvatarGroup()
    {
        LimitProperty.Changed.AddClassHandler<AvatarGroup>((g, _) => g.Refresh());
        ShowsEllipsisProperty.Changed.AddClassHandler<AvatarGroup>((g, _) => g.Refresh());
    }

    /// <summary>Creates an avatar group.</summary>
    public AvatarGroup()
    {
        Classes.CollectionChanged += (_, _) => Refresh();
        Items.CollectionChanged += (_, _) => Refresh();
    }

    /// <inheritdoc cref="LimitProperty"/>
    public int Limit
    {
        get => GetValue(LimitProperty);
        set => SetValue(LimitProperty, value);
    }

    /// <inheritdoc cref="ShowsEllipsisProperty"/>
    public bool ShowsEllipsis
    {
        get => GetValue(ShowsEllipsisProperty);
        set => SetValue(ShowsEllipsisProperty, value);
    }

    /// <inheritdoc cref="IsCutProperty"/>
    public bool IsCut
    {
        get => _isCut;
        private set => SetAndRaise(IsCutProperty, ref _isCut, value);
    }

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new Avatar();

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Avatar>(item, out recycleKey);

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        Arrange(container, index);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (e.NameScope.Find<Avatar>("PART_Ellipsis") is { } ellipsis)
        {
            CopySize(ellipsis);
        }
        Refresh();
    }

    /// <summary>The size of an avatar in this group.</summary>
    private double AvatarSize =>
        Classes.Contains("xsmall") ? 16 : Classes.Contains("small") ? 24 : Classes.Contains("large") ? 80 : 48;

    private void Refresh()
    {
        IsCut = ShowsEllipsis && ItemCount > Limit;
        for (var i = 0; i < ItemCount; i++)
        {
            if (ContainerFromIndex(i) is { } container)
            {
                Arrange(container, i);
            }
        }
        if (this.FindNameScope()?.Find<Avatar>("PART_Ellipsis") is { } ellipsis)
        {
            CopySize(ellipsis);
        }
    }

    // avatar_group.rs: avatars after the first move left by 30% of their size;
    // the first is painted last, on top.
    private void Arrange(Control container, int index)
    {
        container.IsVisible = index < Limit;
        container.Margin = index > 0 ? new Thickness(-AvatarSize * 0.3, 0, 0, 0) : default;
        container.ZIndex = ItemCount - index;
        CopySize(container);
    }

    private void CopySize(Control avatar)
    {
        foreach (var size in SizeClasses)
        {
            avatar.Classes.Set(size, Classes.Contains(size));
        }
    }
}
