using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class CarouselDemo
{
    private void OnPrevious(object? sender, RoutedEventArgs e) => this.GetControl<Carousel>("Slides").Previous();

    private void OnNext(object? sender, RoutedEventArgs e) => this.GetControl<Carousel>("Slides").Next();
}
