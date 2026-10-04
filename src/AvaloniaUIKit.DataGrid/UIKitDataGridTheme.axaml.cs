using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's data table look for Avalonia.Controls.DataGrid. Add it to the
/// application's styles after <see cref="UIKitTheme"/> (and after DataGrid's own
/// Fluent styles, if those are included).
/// </summary>
public class UIKitDataGridTheme : Styles
{
    /// <summary>Loads the themes.</summary>
    public UIKitDataGridTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
    }
}
