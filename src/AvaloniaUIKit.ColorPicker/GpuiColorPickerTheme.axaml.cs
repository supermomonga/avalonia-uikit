using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's ColorPicker and ColorSelect looks for Avalonia.Controls.ColorPicker.
/// Add it to the application's styles after <see cref="GpuiTheme"/>.
/// </summary>
public class GpuiColorPickerTheme : Styles
{
    /// <summary>Loads the themes.</summary>
    public GpuiColorPickerTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
    }
}
