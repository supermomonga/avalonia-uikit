using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class NumberInputSteps
{
    // At 1.0 the step is 0.1 going down and 0.5 going up.
    private void OnLoaded(object? sender, RoutedEventArgs e) =>
        ((NumberInput)sender!).StepBy = (value, action) => action == StepAction.Increment
            ? (value < 1 ? 0.1m : 0.5m)
            : (value <= 1 ? 0.1m : 0.5m);
}
