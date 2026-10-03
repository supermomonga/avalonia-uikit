using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// The Nova data table look for Avalonia.Controls.DataGrid. Add it to the
/// application's styles after <see cref="NovaTheme"/> (and after DataGrid's own
/// Fluent styles, if those are included).
/// </summary>
public class NovaDataGridTheme : Styles
{
    /// <summary>Loads the themes.</summary>
    public NovaDataGridTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
    }
}
