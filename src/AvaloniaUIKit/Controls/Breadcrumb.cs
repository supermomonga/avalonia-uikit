using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Breadcrumb (breadcrumb.rs): items joined by chevrons, the last
/// one in the foreground color. Items are <see cref="BreadcrumbItem"/>s, or
/// any content, which is wrapped in one.
/// </summary>
public class Breadcrumb : ItemsControl
{
    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new BreadcrumbItem();

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<BreadcrumbItem>(item, out recycleKey);
}

/// <summary>
/// One item of a <see cref="Breadcrumb"/>: a Button, so Click and Command
/// navigate. GPUI's items take no keyboard focus and show no hover; neither
/// do these.
/// </summary>
public class BreadcrumbItem : Button
{
    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(BreadcrumbItem);
}
