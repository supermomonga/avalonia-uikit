using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's SettingGroup (crates/component/src/setting/group.rs):
/// <see cref="SettingItem"/>s 16px apart in the look of the GroupBox theme,
/// with the <see cref="Title"/> (and the <see cref="Description"/> under it)
/// muted above the surface and the <see cref="Footer"/> as small muted text
/// below it, 16px of room above and below the group. The surface is the
/// <see cref="Variant"/>: the settings' <see cref="Settings.GroupVariantProperty"/>
/// unless the group sets its own. A titled group is a menu item under its page
/// in the sidebar when the page has more than one group. The search hides a
/// group without matching items, with its footer.
/// </summary>
[PseudoClasses(":fill", ":outline", ":titled", ":unmatched")]
public class SettingGroup : ItemsControl
{
    /// <summary>The group's title (GPUI's <c>title</c>), also its label in the sidebar.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SettingGroup, string?>(nameof(Title));

    /// <summary>Small muted text under the title (GPUI's <c>description</c>); shown only with a title.</summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingGroup, string?>(nameof(Description));

    /// <summary>Content under the surface (GPUI's <c>footer</c>), as small muted text; it does not match the search.</summary>
    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<SettingGroup, object?>(nameof(Footer));

    /// <summary>The group's surface (GPUI's <c>variant</c>); inherits the settings' <see cref="Settings.GroupVariantProperty"/>.</summary>
    public static readonly StyledProperty<GroupBoxVariant> VariantProperty =
        Settings.GroupVariantProperty.AddOwner<SettingGroup>();

    static SettingGroup()
    {
        TitleProperty.Changed.AddClassHandler<SettingGroup>((x, e) =>
        {
            x.PseudoClasses.Set(":titled", e.GetNewValue<string?>() is not null);
            x.Changed();
        });
        VariantProperty.Changed.AddClassHandler<SettingGroup>((x, _) => x.UpdateVariant());
    }

    /// <summary>Creates a group.</summary>
    public SettingGroup()
    {
        Items.CollectionChanged += (_, _) => Changed();
        UpdateVariant();
    }

    /// <inheritdoc cref="TitleProperty"/>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="DescriptionProperty"/>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <inheritdoc cref="FooterProperty"/>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    /// <inheritdoc cref="VariantProperty"/>
    public GroupBoxVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<SettingItem>(item, out recycleKey);

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new SettingItem();

    /// <summary>Marks the items by <paramref name="query"/>, hides the group without matches and says whether it has any.</summary>
    internal bool Filter(string query)
    {
        var any = false;
        foreach (var item in Items.OfType<SettingItem>())
        {
            any |= item.Filter(query);
        }
        PseudoClasses.Set(":unmatched", !any);
        return any;
    }

    /// <summary>Tells the page that what the group shows or matches has changed.</summary>
    internal void Changed()
    {
        if (Parent is SettingPage page)
        {
            page.Changed();
        }
    }

    private void UpdateVariant()
    {
        PseudoClasses.Set(":fill", Variant == GroupBoxVariant.Fill);
        PseudoClasses.Set(":outline", Variant == GroupBoxVariant.Outline);
    }
}
