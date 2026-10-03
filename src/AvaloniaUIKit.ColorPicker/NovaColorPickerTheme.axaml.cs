using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// The Nova color picker and color field looks for Avalonia.Controls.ColorPicker.
/// Add it to the application's styles after <see cref="NovaTheme"/>.
/// </summary>
public class NovaColorPickerTheme : Styles
{
    /// <summary>Loads the themes.</summary>
    public NovaColorPickerTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
    }
}
