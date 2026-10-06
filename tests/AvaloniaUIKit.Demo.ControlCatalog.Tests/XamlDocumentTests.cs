using AvaloniaUIKit.Demo.ControlCatalog.Code;
using AvaloniaUIKit.Demo.ControlCatalog.Xaml;

namespace AvaloniaUIKit.Demo.ControlCatalog.Tests;

/// <summary>Reading XAML with where its parts are, changing attributes in place, and highlighting it.</summary>
public class XamlDocumentTests
{
    private const string Demo = """
        <UserControl xmlns="https://github.com/avaloniaui"
                     xmlns:uikit="using:AvaloniaUIKit"
                     x:Class="AvaloniaUIKit.Demos.ButtonVariants">
          <!-- Buttons -->
          <WrapPanel ItemSpacing="8">
            <Button Content="Default" />
            <Button Classes="primary" Content="A &amp; B" />
            <uikit:Badge Count="3">
              <Button.Flyout><Flyout /></Button.Flyout>
            </uikit:Badge>
            <TextBlock>Hello</TextBlock>
            <TextBox Width="240"
                     Watermark="Name" />
          </WrapPanel>
        </UserControl>
        """;

    [Test]
    public async Task Elements_are_numbered_in_document_order_with_their_attributes()
    {
        var document = XamlDocument.Parse(Demo);
        var names = document.Elements.Select(e => e.Name).ToArray();
        await Assert.That(names).IsEquivalentTo(
            ["UserControl", "WrapPanel", "Button", "Button", "uikit:Badge", "Button.Flyout", "Flyout", "TextBlock", "TextBox"]);
        var badge = document.Elements[4];
        await Assert.That(badge.Prefix).IsEqualTo("uikit");
        await Assert.That(badge.LocalName).IsEqualTo("Badge");
        await Assert.That(badge.Children[0].IsPropertyElement).IsTrue();
        await Assert.That(document.Elements[3].Attribute("Content")!.Value).IsEqualTo("A & B");
        await Assert.That(document.Elements[7].Text).IsEqualTo("Hello");
        await Assert.That(document.Elements[2].IsEmpty).IsTrue();
    }

    [Test]
    public async Task Tokens_cover_the_text_without_gaps()
    {
        var document = XamlDocument.Parse(Demo);
        var at = 0;
        foreach (var token in document.Tokens)
        {
            await Assert.That(token.Start).IsEqualTo(at);
            at += token.Length;
        }
        await Assert.That(at).IsEqualTo(Demo.Length);
    }

    [Test]
    public async Task A_changed_value_keeps_its_place_and_quotes()
    {
        var document = XamlDocument.Parse(Demo);
        var edits = new XamlEdits();
        edits.SetAttribute(3, "Classes", "danger small");
        var text = edits.Apply(document);
        await Assert.That(text).Contains("""<Button Classes="danger small" Content="A &amp; B" />""");
    }

    [Test]
    public async Task A_new_attribute_follows_the_last_one()
    {
        var document = XamlDocument.Parse(Demo);
        var edits = new XamlEdits();
        edits.SetAttribute(2, "IsEnabled", "False");
        edits.SetAttribute(1, "Orientation", "Vertical");
        var text = edits.Apply(document);
        await Assert.That(text).Contains("""<Button Content="Default" IsEnabled="False" />""");
        await Assert.That(text).Contains("""<WrapPanel ItemSpacing="8" Orientation="Vertical">""");
    }

    [Test]
    public async Task A_new_attribute_of_a_tag_with_an_attribute_per_line_gets_a_line()
    {
        var document = XamlDocument.Parse(Demo);
        var edits = new XamlEdits();
        edits.SetAttribute(8, "IsEnabled", "False");
        var text = edits.Apply(document);
        await Assert.That(text).Contains("""
                <TextBox Width="240"
                         Watermark="Name"
                         IsEnabled="False" />
            """);
    }

    [Test]
    public async Task A_removed_attribute_takes_its_space_along()
    {
        var document = XamlDocument.Parse(Demo);
        var edits = new XamlEdits();
        edits.SetAttribute(3, "Classes", null);
        var text = edits.Apply(document);
        await Assert.That(text).Contains("""<Button Content="A &amp; B" />""");
    }

    [Test]
    public async Task Text_content_is_changed_inside_the_element()
    {
        var document = XamlDocument.Parse(Demo);
        var edits = new XamlEdits();
        edits.SetText(7, "Hi <you>");
        var text = edits.Apply(document);
        await Assert.That(text).Contains("<TextBlock>Hi &lt;you&gt;</TextBlock>");
    }

    [Test]
    public async Task Reverting_a_change_restores_the_text()
    {
        var document = XamlDocument.Parse(Demo);
        var edits = new XamlEdits();
        edits.SetAttribute(3, "Classes", "danger");
        edits.Revert(3, "Classes");
        await Assert.That(edits.IsEmpty).IsTrue();
        await Assert.That(edits.Apply(document)).IsEqualTo(Demo);
    }

    [Test]
    public async Task The_code_is_the_root_element_s_content_dedented_as_the_site_shows_it()
    {
        var inner = XamlEdits.Inner(XamlDocument.Parse(Demo));
        await Assert.That(inner.Split('\n')[0]).IsEqualTo("<!-- Buttons -->");
        await Assert.That(inner.Split('\n')[1]).IsEqualTo("<WrapPanel ItemSpacing=\"8\">");
        await Assert.That(inner.Split('\n')[^1]).IsEqualTo("</WrapPanel>");
    }

    [Test]
    public async Task Highlighting_marks_the_changed_attribute_of_the_root_s_content()
    {
        var document = XamlDocument.Parse(Demo);
        var edits = new XamlEdits();
        edits.SetAttribute(3, "Classes", "danger");
        var inner = XamlEdits.Inner(XamlDocument.Parse(edits.Apply(document)));
        var spans = Highlighter.Xaml(XamlDocument.Parse(inner), edits.SetAttributes.ToHashSet(), null, indexOffset: 1);
        var marked = spans.Where(s => s.IsChanged).Select(s => inner.Substring(s.Start, s.Length)).ToArray();
        await Assert.That(marked).IsEquivalentTo(["Classes", "=", "\"danger\""]);
        var tag = spans.First(s => inner.Substring(s.Start, s.Length) == "WrapPanel");
        await Assert.That(tag.Color).IsEqualTo(CodeColor.Keyword);
    }

    [Test]
    public async Task CSharp_is_colored_by_kind()
    {
        const string code = "// note\nvar tabs = this.GetControl<TabControl>(\"Tabs\");";
        var spans = Highlighter.CSharp(code);
        string ColorOf(string word) => spans.First(s => code.Substring(s.Start, s.Length) == word).Color.ToString();
        await Assert.That(ColorOf("// note")).IsEqualTo(nameof(CodeColor.Comment));
        await Assert.That(ColorOf("var")).IsEqualTo(nameof(CodeColor.Keyword));
        await Assert.That(ColorOf("\"Tabs\"")).IsEqualTo(nameof(CodeColor.String));
        await Assert.That(ColorOf("TabControl")).IsEqualTo(nameof(CodeColor.Type));
    }
}
