using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's color picker and color field looks for Avalonia.Controls.ColorPicker.
/// Add it to the application's styles after <see cref="UIKitTheme"/>.
/// </summary>
public class UIKitColorPickerTheme : Styles
{
    /// <summary>Loads the themes.</summary>
    public UIKitColorPickerTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
    }
}
