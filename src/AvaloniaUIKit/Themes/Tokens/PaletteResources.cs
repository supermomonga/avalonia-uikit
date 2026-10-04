using Avalonia.Controls;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// A theme dictionary that serves one <see cref="Palette"/>: it creates a brush or a color
/// the first time its key is looked up and keeps it, so a theme that is never shown costs
/// one empty array.
/// </summary>
internal sealed class PaletteResources(Palette palette) : ResourceProvider, IThemeVariantProvider
{
    private object?[]? _values;

    /// <inheritdoc/>
    public ThemeVariant? Key { get; set; }

    /// <inheritdoc/>
    public override bool HasResources => true;

    /// <inheritdoc/>
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        var slot = Palette.Slot(key);
        if (slot < 0)
        {
            value = null;
            return false;
        }
        _values ??= new object?[Palette.Count];
        value = _values[slot] ??= palette.Create(slot);
        return true;
    }
}
