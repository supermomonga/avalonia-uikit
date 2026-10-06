using System.Text;

namespace AvaloniaUIKit.Demo.ControlCatalog.Xaml;

/// <summary>What a span of XAML text is, for highlighting.</summary>
public enum XamlTokenKind
{
    /// <summary>Text between elements.</summary>
    Text,

    /// <summary><c>&lt;</c>, <c>&lt;/</c>, <c>&gt;</c>, <c>/&gt;</c>.</summary>
    Delimiter,

    /// <summary>An element's name, with its prefix.</summary>
    ElementName,

    /// <summary>An attribute's name, with its prefix.</summary>
    AttributeName,

    /// <summary>The <c>=</c> between an attribute's name and value.</summary>
    Equals,

    /// <summary>An attribute's value, with its quotes.</summary>
    AttributeValue,

    /// <summary>A comment, a processing instruction or a CDATA section.</summary>
    Comment,
}

/// <summary>A span of XAML text, and the element it belongs to (-1 outside tags).</summary>
public readonly record struct XamlToken(XamlTokenKind Kind, int Start, int Length, int Element = -1, string? Attribute = null);

/// <summary>An attribute of a <see cref="XamlElement"/>, with where its value is in the text.</summary>
public sealed class XamlAttribute(string name, int nameStart, int valueStart, int valueLength, char quote)
{
    /// <summary>The name as written (<c>Classes</c>, <c>uikit:Buttons.IsLoading</c>).</summary>
    public string Name { get; } = name;

    /// <summary>Where the name starts.</summary>
    public int NameStart { get; } = nameStart;

    /// <summary>Where the value starts, after the opening quote.</summary>
    public int ValueStart { get; } = valueStart;

    /// <summary>The value's length in the text, without the quotes.</summary>
    public int ValueLength { get; } = valueLength;

    /// <summary>The quote character.</summary>
    public char Quote { get; } = quote;

    /// <summary>The value with entities resolved.</summary>
    public string Value { get; internal set; } = "";

    /// <summary>Whether the value is a markup extension (<c>{Binding}</c>, <c>{DynamicResource}</c>).</summary>
    public bool IsMarkupExtension => Value.StartsWith('{') && !Value.StartsWith("{}", StringComparison.Ordinal);
}

/// <summary>An element of a XAML document, with where its start tag is in the text.</summary>
public sealed class XamlElement
{
    internal XamlElement(string name, int index, int start, XamlElement? parent)
    {
        Name = name;
        Index = index;
        Start = start;
        Parent = parent;
        var colon = name.IndexOf(':');
        Prefix = colon < 0 ? null : name[..colon];
        LocalName = colon < 0 ? name : name[(colon + 1)..];
    }

    /// <summary>The name as written (<c>uikit:Badge</c>, <c>Button.Flyout</c>).</summary>
    public string Name { get; }

    /// <summary>The prefix, or null for the default namespace.</summary>
    public string? Prefix { get; }

    /// <summary>The name without its prefix (<c>Badge</c>, <c>Button.Flyout</c>).</summary>
    public string LocalName { get; }

    /// <summary>The element's position among the document's elements, in document order.</summary>
    public int Index { get; }

    /// <summary>Where the start tag's <c>&lt;</c> is.</summary>
    public int Start { get; }

    /// <summary>Where the element ends, after its end tag (or its <c>/&gt;</c>).</summary>
    public int End { get; internal set; }

    /// <summary>Where the start tag ends, after its <c>&gt;</c>.</summary>
    public int StartTagEnd { get; internal set; }

    /// <summary>Where the end tag's <c>&lt;/</c> is; for an empty element, <see cref="End"/>.</summary>
    public int EndTagStart { get; internal set; }

    /// <summary>Where the name ends in the start tag.</summary>
    public int NameEnd { get; internal set; }

    /// <summary>Whether the element is written <c>&lt;Name /&gt;</c>.</summary>
    public bool IsEmpty { get; internal set; }

    /// <summary>The element that contains this one.</summary>
    public XamlElement? Parent { get; }

    /// <summary>The attributes, in order.</summary>
    public List<XamlAttribute> Attributes { get; } = [];

    /// <summary>The child elements, in order.</summary>
    public List<XamlElement> Children { get; } = [];

    /// <summary>The text directly inside the element, trimmed (<c>&lt;x:String&gt;Apple&lt;/x:String&gt;</c>).</summary>
    public string Text { get; internal set; } = "";

    /// <summary>Whether this is a property element (<c>&lt;Button.Flyout&gt;</c>).</summary>
    public bool IsPropertyElement => LocalName.Contains('.');

    /// <summary>The attribute with <paramref name="name"/>, or null.</summary>
    public XamlAttribute? Attribute(string name) => Attributes.Find(a => a.Name == name);

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>
/// A XAML document read for the catalog: its elements and attributes with where they
/// are in the text (to change attributes in place), and the tokens to highlight it.
/// It reads well-formed XML only as far as the catalog needs; it does not resolve
/// namespaces or validate.
/// </summary>
public sealed class XamlDocument
{
    private XamlDocument(string text, XamlElement? root, List<XamlElement> elements, List<XamlToken> tokens)
    {
        Text = text;
        Root = root;
        Elements = elements;
        Tokens = tokens;
    }

    /// <summary>The text that was read.</summary>
    public string Text { get; }

    /// <summary>The root element, or null for a document without one.</summary>
    public XamlElement? Root { get; }

    /// <summary>Every element, in document order (an element's <see cref="XamlElement.Index"/>).</summary>
    public IReadOnlyList<XamlElement> Elements { get; }

    /// <summary>The text's tokens, in order, covering it without gaps.</summary>
    public IReadOnlyList<XamlToken> Tokens { get; }

    /// <summary>Reads <paramref name="text"/>.</summary>
    public static XamlDocument Parse(string text)
    {
        var elements = new List<XamlElement>();
        var tokens = new List<XamlToken>();
        XamlElement? root = null;
        XamlElement? current = null;
        var textStart = 0;
        var i = 0;

        void AddText(int end)
        {
            if (end > textStart)
            {
                tokens.Add(new XamlToken(XamlTokenKind.Text, textStart, end - textStart, current?.Index ?? -1));
                if (current is not null)
                {
                    var inner = text[textStart..end].Trim();
                    if (inner.Length > 0)
                    {
                        current.Text = current.Text.Length == 0 ? Unescape(inner) : current.Text + " " + Unescape(inner);
                    }
                }
            }
        }

        while (i < text.Length)
        {
            if (text[i] != '<')
            {
                i++;
                continue;
            }
            AddText(i);
            if (Starts(text, i, "<!--"))
            {
                var end = IndexAfter(text, i + 4, "-->");
                tokens.Add(new XamlToken(XamlTokenKind.Comment, i, end - i));
                i = end;
            }
            else if (Starts(text, i, "<![CDATA["))
            {
                var end = IndexAfter(text, i + 9, "]]>");
                tokens.Add(new XamlToken(XamlTokenKind.Comment, i, end - i));
                i = end;
            }
            else if (Starts(text, i, "<?") || Starts(text, i, "<!"))
            {
                var end = IndexAfter(text, i + 2, ">");
                tokens.Add(new XamlToken(XamlTokenKind.Comment, i, end - i));
                i = end;
            }
            else if (Starts(text, i, "</"))
            {
                var nameStart = i + 2;
                var nameEnd = NameEnd(text, nameStart);
                var close = text.IndexOf('>', nameEnd);
                close = close < 0 ? text.Length : close + 1;
                var index = current?.Index ?? -1;
                tokens.Add(new XamlToken(XamlTokenKind.Delimiter, i, 2, index));
                tokens.Add(new XamlToken(XamlTokenKind.ElementName, nameStart, nameEnd - nameStart, index));
                if (close > nameEnd)
                {
                    tokens.Add(new XamlToken(XamlTokenKind.Delimiter, nameEnd, close - nameEnd, index));
                }
                if (current is not null)
                {
                    current.EndTagStart = i;
                    current.End = close;
                    current = current.Parent;
                }
                i = close;
            }
            else
            {
                i = ReadStartTag(text, i, ref current, ref root, elements, tokens);
            }
            textStart = i;
        }
        AddText(text.Length);
        return new XamlDocument(text, root, elements, tokens);
    }

    private static int ReadStartTag(
        string text, int start, ref XamlElement? current, ref XamlElement? root, List<XamlElement> elements, List<XamlToken> tokens)
    {
        var nameStart = start + 1;
        var nameEnd = NameEnd(text, nameStart);
        var element = new XamlElement(text[nameStart..nameEnd], elements.Count, start, current) { NameEnd = nameEnd };
        elements.Add(element);
        current?.Children.Add(element);
        root ??= element;
        tokens.Add(new XamlToken(XamlTokenKind.Delimiter, start, 1, element.Index));
        tokens.Add(new XamlToken(XamlTokenKind.ElementName, nameStart, nameEnd - nameStart, element.Index));

        var i = nameEnd;
        var spaceStart = i;
        void Space(int end)
        {
            if (end > spaceStart)
            {
                tokens.Add(new XamlToken(XamlTokenKind.Text, spaceStart, end - spaceStart, element.Index));
            }
        }

        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }
            Space(i);
            if (c == '>' || Starts(text, i, "/>"))
            {
                var empty = c == '/';
                var length = empty ? 2 : 1;
                tokens.Add(new XamlToken(XamlTokenKind.Delimiter, i, length, element.Index));
                i += length;
                element.StartTagEnd = i;
                element.IsEmpty = empty;
                if (empty)
                {
                    element.End = i;
                    element.EndTagStart = i;
                }
                else
                {
                    current = element;
                }
                return i;
            }
            // An attribute: name, optional spaces, '=', optional spaces, quoted value.
            var attrStart = i;
            var attrEnd = NameEnd(text, attrStart);
            if (attrEnd == attrStart)
            {
                attrEnd = attrStart + 1;
            }
            var name = text[attrStart..attrEnd];
            tokens.Add(new XamlToken(XamlTokenKind.AttributeName, attrStart, attrEnd - attrStart, element.Index, name));
            i = attrEnd;
            spaceStart = i;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }
            if (i < text.Length && text[i] == '=')
            {
                Space(i);
                tokens.Add(new XamlToken(XamlTokenKind.Equals, i, 1, element.Index, name));
                i++;
                spaceStart = i;
                while (i < text.Length && char.IsWhiteSpace(text[i]))
                {
                    i++;
                }
                Space(i);
                if (i < text.Length && text[i] is '"' or '\'')
                {
                    var quote = text[i];
                    var close = text.IndexOf(quote, i + 1);
                    close = close < 0 ? text.Length : close;
                    var attribute = new XamlAttribute(name, attrStart, i + 1, close - i - 1, quote)
                    {
                        Value = Unescape(text[(i + 1)..close]),
                    };
                    element.Attributes.Add(attribute);
                    var end = Math.Min(close + 1, text.Length);
                    tokens.Add(new XamlToken(XamlTokenKind.AttributeValue, i, end - i, element.Index, name));
                    i = end;
                }
            }
            spaceStart = i;
        }
        Space(i);
        element.StartTagEnd = i;
        element.End = i;
        element.EndTagStart = i;
        element.IsEmpty = true;
        return i;
    }

    private static bool Starts(string text, int index, string value) =>
        string.CompareOrdinal(text, index, value, 0, value.Length) == 0;

    private static int IndexAfter(string text, int from, string value)
    {
        var index = text.IndexOf(value, from, StringComparison.Ordinal);
        return index < 0 ? text.Length : index + value.Length;
    }

    private static int NameEnd(string text, int start)
    {
        var i = start;
        while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is ':' or '.' or '_' or '-'))
        {
            i++;
        }
        return i;
    }

    /// <summary>Resolves the predefined and numeric entities.</summary>
    public static string Unescape(string value)
    {
        if (!value.Contains('&'))
        {
            return value;
        }
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var semicolon = value[i] == '&' ? value.IndexOf(';', i) : -1;
            if (semicolon < 0)
            {
                builder.Append(value[i]);
                continue;
            }
            var entity = value[(i + 1)..semicolon];
            string? resolved = entity switch
            {
                "amp" => "&",
                "lt" => "<",
                "gt" => ">",
                "quot" => "\"",
                "apos" => "'",
                _ when entity.StartsWith("#x", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(entity[2..], System.Globalization.NumberStyles.HexNumber, null, out var hex) => char.ConvertFromUtf32(hex),
                _ when entity.StartsWith('#') && int.TryParse(entity[1..], out var code) => char.ConvertFromUtf32(code),
                _ => null,
            };
            if (resolved is null)
            {
                builder.Append(value[i]);
                continue;
            }
            builder.Append(resolved);
            i = semicolon;
        }
        return builder.ToString();
    }

    /// <summary>Escapes <paramref name="value"/> for an attribute quoted with <paramref name="quote"/>.</summary>
    public static string EscapeAttribute(string value, char quote = '"')
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(c switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '"' when quote == '"' => "&quot;",
                '\'' when quote == '\'' => "&apos;",
                _ => c.ToString(),
            });
        }
        return builder.ToString();
    }

    /// <summary>Escapes <paramref name="value"/> for text content.</summary>
    public static string EscapeText(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);
}
