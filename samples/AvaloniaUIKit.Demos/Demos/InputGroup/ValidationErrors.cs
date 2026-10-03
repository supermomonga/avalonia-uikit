namespace AvaloniaUIKit.Demos;

/// <summary>
/// The validation errors the demos put on a control through
/// <c>DataValidationErrors.Errors</c>, which turns on its invalid look.
/// </summary>
public static class ValidationErrors
{
    public static IEnumerable<object> InvalidEmail { get; } = ["Enter a valid email address"];

    public static IEnumerable<object> OutsideBusinessHours { get; } = ["Outside business hours"];
}
