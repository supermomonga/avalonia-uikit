using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AvaloniaUIKit.Demo.ControlCatalog.Views;

/// <summary>Text with <c>`code`</c> spans, as the site's pages write it, made into inlines.</summary>
internal static class InlineText
{
    /// <summary>Fills <paramref name="block"/> with <paramref name="text"/>, the code spans monospaced on the muted background.</summary>
    public static T With<T>(this T block, string text)
        where T : TextBlock
    {
        var inlines = new InlineCollection();
        var parts = text.Split('`');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0)
            {
                continue;
            }
            if (i % 2 == 0)
            {
                inlines.Add(new Run(parts[i]));
                continue;
            }
            var code = new Run(parts[i]) { FontSize = block.FontSize * 0.88 };
            code.Bind(TextElement.FontFamilyProperty, code.GetResourceObservable("Catalog.Monospace"));
            code.Bind(TextElement.BackgroundProperty, code.GetResourceObservable("UIKit.Muted"));
            code.Bind(TextElement.ForegroundProperty, code.GetResourceObservable("UIKit.Foreground"));
            inlines.Add(code);
        }
        block.Inlines = inlines;
        return block;
    }

    /// <summary>A brush of the color resource <paramref name="key"/>, followed as the theme changes.</summary>
    public static void BindBrush(this Control control, Avalonia.AvaloniaProperty property, string key) =>
        control.Bind(property, control.GetResourceObservable(key));

    /// <summary>The geometry of a bundled icon (<c>UIKit.Icon.&lt;name&gt;</c>).</summary>
    public static Geometry? Icon(string name) => Xaml.Icons.Find(name);
}
