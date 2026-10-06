using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit.Demo.ControlCatalog;

/// <summary>The catalog's own styles (CatalogStyles.axaml).</summary>
public sealed class CatalogStyles : Styles
{
    public CatalogStyles() => AvaloniaXamlLoader.Load(this);
}
