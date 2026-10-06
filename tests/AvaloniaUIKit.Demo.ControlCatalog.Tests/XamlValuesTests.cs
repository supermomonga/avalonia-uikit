using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUIKit.Demo.ControlCatalog.Xaml;

namespace AvaloniaUIKit.Demo.ControlCatalog.Tests;

/// <summary>Values written as XAML writes them and read back.</summary>
public class XamlValuesTests
{
    [Test]
    public async Task Values_are_written_as_XAML_writes_them()
    {
        await Assert.That(XamlValues.Format(true)).IsEqualTo("True");
        await Assert.That(XamlValues.Format(12.5)).IsEqualTo("12.5");
        await Assert.That(XamlValues.Format(double.NaN)).IsEqualTo("NaN");
        await Assert.That(XamlValues.Format(HorizontalAlignment.Left)).IsEqualTo("Left");
        await Assert.That(XamlValues.Format(new Thickness(8))).IsEqualTo("8");
        await Assert.That(XamlValues.Format(new Thickness(8, 4))).IsEqualTo("8,4");
        await Assert.That(XamlValues.Format(new Thickness(1, 2, 3, 4))).IsEqualTo("1,2,3,4");
        await Assert.That(XamlValues.Format(new CornerRadius(6))).IsEqualTo("6");
        await Assert.That(XamlValues.Format(Color.Parse("#2563EB"))).IsEqualTo("#2563EB");
        await Assert.That(XamlValues.Format(new SolidColorBrush(Color.Parse("#802563EB")))).IsEqualTo("#802563EB");
        await Assert.That(XamlValues.Format(null)).IsEqualTo("{x:Null}");
        await Assert.That(XamlValues.Format(new Button())).IsNull();
    }

    [Test]
    [Arguments("12", typeof(double), 12.0)]
    [Arguments("auto", typeof(double), double.NaN)]
    [Arguments("3", typeof(int), 3)]
    [Arguments("true", typeof(bool), true)]
    [Arguments("Right", typeof(HorizontalAlignment), HorizontalAlignment.Right)]
    [Arguments("hello", typeof(string), "hello")]
    public async Task Text_reads_back_as_the_property_s_type(string text, Type type, object expected)
    {
        await Assert.That(XamlValues.TryParse(text, type, out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(expected);
    }

    [Test]
    public async Task Structured_values_read_back()
    {
        await Assert.That(XamlValues.TryParse("8,4", typeof(Thickness), out var thickness)).IsTrue();
        await Assert.That(thickness).IsEqualTo(new Thickness(8, 4));
        await Assert.That(XamlValues.TryParse("#F59E0B", typeof(IBrush), out var brush)).IsTrue();
        await Assert.That(((ISolidColorBrush)brush!).Color).IsEqualTo(Color.Parse("#F59E0B"));
        await Assert.That(XamlValues.TryParse("Ctrl+Shift+P", typeof(Avalonia.Input.KeyGesture), out var gesture)).IsTrue();
        await Assert.That(gesture!.ToString()).IsEqualTo("Ctrl+Shift+P");
    }

    [Test]
    public async Task Empty_text_is_null_only_where_null_is_allowed()
    {
        await Assert.That(XamlValues.TryParse("", typeof(double?), out var nullable)).IsTrue();
        await Assert.That(nullable).IsNull();
        await Assert.That(XamlValues.TryParse("", typeof(double), out _)).IsFalse();
        await Assert.That(XamlValues.TryParse("x", typeof(int), out _)).IsFalse();
    }

    [Test]
    public async Task A_bundled_icon_is_written_as_its_resource()
    {
        var bell = Icons.Find("Bell");
        await Assert.That(bell).IsNotNull();
        await Assert.That(XamlValues.Format(bell)).IsEqualTo("{StaticResource UIKit.Icon.Bell}");
        await Assert.That(Icons.Keys).Contains("UIKit.Icon.Bell");
        await Assert.That(Icons.Keys.Any(k => k.EndsWith(".Faint", StringComparison.Ordinal))).IsFalse();
    }
}
