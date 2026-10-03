using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaloniaUIKit.AotSmoke;

public sealed partial class MainWindow : Window
{
    public MainWindow() => AvaloniaXamlLoader.Load(this);
}
