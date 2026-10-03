using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's DataTable look for Avalonia.Controls.DataGrid. Add it to the
/// application's styles after <see cref="GpuiTheme"/> (and after DataGrid's own
/// Fluent styles, if those are included).
/// </summary>
public class GpuiDataGridTheme : Styles
{
    /// <summary>Loads the themes.</summary>
    public GpuiDataGridTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
    }
}
