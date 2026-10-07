using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AvaloniaUIKit.Generators;

/// <summary>
/// The icons a XAML file names. It reads the text rather than the XML, so a
/// file being edited still counts, and it errs on naming more: an icon found
/// by mistake only adds its geometry.
/// </summary>
internal static class XamlScanner
{
    // A resource key (UIKit.Icon.Bike) or an x:Static (IconName.Bike), anywhere.
    private static readonly Regex s_qualified = new(
        @"\b(?:UIKit\.Icon|IconName)\.([A-Za-z0-9]+)", RegexOptions.CultureInvariant);

    // Kind="Bike" (uikit:Icon) and Value="Bike" (a Setter of Kind).
    private static readonly Regex s_attribute = new(
        @"\b(?:Kind|Value)\s*=\s*[""']\s*([A-Za-z0-9]+)\s*[""']", RegexOptions.CultureInvariant);

    // <uikit:Icon.Kind>Bike</uikit:Icon.Kind>, and an x:Array's <uikit:IconName>Bike</uikit:IconName>.
    private static readonly Regex s_element = new(
        @"<(?:\w+:)?(?:IconName|\w+\.Kind)\s*>\s*([A-Za-z0-9]+)\s*<", RegexOptions.CultureInvariant);

    /// <summary>The icons the generator adds that <paramref name="xaml"/> names.</summary>
    public static IEnumerable<string> Names(string xaml)
    {
        foreach (var regex in new[] { s_qualified, s_attribute, s_element })
        {
            foreach (Match match in regex.Matches(xaml))
            {
                var name = match.Groups[1].Value;
                if (IconCatalog.Adds(name))
                {
                    yield return name;
                }
            }
        }
    }
}
