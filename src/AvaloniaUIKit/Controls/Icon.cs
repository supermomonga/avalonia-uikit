using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Path = Avalonia.Controls.Shapes.Path;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Icon (icon.rs) for an <see cref="IconName"/>: a
/// <see cref="PathIcon"/> whose <see cref="PathIcon.Data"/> is the icon's
/// geometry from the theme (the resource <see cref="IconNames.ResourceKey"/>
/// names). It is styled as a PathIcon: 16px (Size::Medium), the classes
/// xsmall, small and large (12, 14 and 24px), the Foreground as its color,
/// and the sizes the controls give the icons in them. A two-tone icon's faint
/// part is painted under it at the SVG's opacity, as GPUI's SVG draws it.
/// </summary>
public class Icon : PathIcon
{
    /// <summary>The icon to show; without one, <see cref="PathIcon.Data"/> is the app's.</summary>
    public static readonly StyledProperty<IconName?> KindProperty =
        AvaloniaProperty.Register<Icon, IconName?>(nameof(Kind));

    private IDisposable? _data;
    private Path? _path;

    static Icon()
    {
        AffectsRender<Icon>(KindProperty, ForegroundProperty);
        KindProperty.Changed.AddClassHandler<Icon>((icon, _) => icon.UpdateData());
    }

    /// <inheritdoc cref="KindProperty"/>
    public IconName? Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(PathIcon);

    /// <inheritdoc />
    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _path = null;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        // sort-ascending.svg: the arrow GPUI fades to opacity 0.2 is the resource "<key>.Faint".
        if (Kind is not { } kind)
        {
            return;
        }
        var opacity = kind.FaintOpacity();
        if (opacity <= 0 || !this.TryFindResource(kind.ResourceKey() + ".Faint", ActualThemeVariant, out var resource) ||
            resource is not Geometry faint)
        {
            return;
        }
        // Drawn in the space of the template's Path, which the template's Viewbox scales.
        _path ??= this.GetVisualDescendants().OfType<Path>().FirstOrDefault();
        if (_path?.TransformToVisual(this) is not { } transform)
        {
            return;
        }
        using (context.PushTransform(transform))
        using (context.PushOpacity(opacity))
        {
            context.DrawGeometry(Foreground, null, faint);
        }
    }

    private void UpdateData()
    {
        _data?.Dispose();
        _data = Kind is { } kind ? Bind(DataProperty, this.GetResourceObservable(kind.ResourceKey())) : null;
    }
}
