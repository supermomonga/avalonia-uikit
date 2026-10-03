using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// GPUI Kit's time field texts from Avalonia's TimePicker parts: a zero-padded
/// hour (Avalonia writes "9"), and the 12-hour clock's period labels.
/// </summary>
public static class TimeConverters
{
    /// <summary>A number as two digits ("09"); any other text unchanged.</summary>
    public static readonly IValueConverter TwoDigits = new FuncValueConverter<string?, string?>(text =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var n) ? n.ToString("00", CultureInfo.CurrentCulture) : text);

    /// <summary>Whether a TimePicker clock identifier is the 12-hour clock.</summary>
    public static readonly IValueConverter Is12Hour = new FuncValueConverter<string?, bool>(clock => clock == "12HourClock");

    /// <summary>The culture's AM designator, as Avalonia's TimePicker shows it.</summary>
    public static readonly IValueConverter AmDesignator = new FuncValueConverter<object?, string>(_ => Designator(am: true));

    /// <summary>The culture's PM designator, as Avalonia's TimePicker shows it.</summary>
    public static readonly IValueConverter PmDesignator = new FuncValueConverter<object?, string>(_ => Designator(am: false));

    private static string Designator(bool am)
    {
        var format = CultureInfo.CurrentCulture.DateTimeFormat;
        var text = am ? format.AMDesignator : format.PMDesignator;
        return string.IsNullOrEmpty(text) ? (am ? CultureInfo.InvariantCulture.DateTimeFormat.AMDesignator : CultureInfo.InvariantCulture.DateTimeFormat.PMDesignator) : text;
    }
}
