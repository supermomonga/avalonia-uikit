using AvaloniaUIKit.Demo.ControlCatalog.Xaml;

namespace AvaloniaUIKit.Demo.ControlCatalog.Code;

/// <summary>What a span of code is colored as (sites/app/lib/highlight.ts).</summary>
public enum CodeColor
{
    /// <summary>Plain text.</summary>
    Foreground,

    /// <summary>Tags and keywords.</summary>
    Keyword,

    /// <summary>Attribute names and types.</summary>
    Type,

    /// <summary>Strings and attribute values.</summary>
    String,

    /// <summary>Comments.</summary>
    Comment,

    /// <summary>Numbers and calls.</summary>
    Function,
}

/// <summary>A colored span of code; <see cref="IsChanged"/> marks code an edit wrote.</summary>
public readonly record struct CodeSpan(int Start, int Length, CodeColor Color, bool IsChanged = false);

/// <summary>Splits XAML and C# into colored spans, as the site's highlighter colors them.</summary>
public static class Highlighter
{
    /// <summary>
    /// The spans of a XAML <paramref name="document"/>. Attributes in <paramref name="changed"/>
    /// (element index, name) and the text of elements in <paramref name="changedTexts"/> are
    /// marked; <paramref name="indexOffset"/> is added to the document's element indices first
    /// (1 when the document is the root element's content).
    /// </summary>
    public static IReadOnlyList<CodeSpan> Xaml(
        XamlDocument document,
        IReadOnlySet<(int Element, string Name)>? changed = null,
        IReadOnlySet<int>? changedTexts = null,
        int indexOffset = 0)
    {
        var spans = new List<CodeSpan>(document.Tokens.Count);
        foreach (var token in document.Tokens)
        {
            var color = token.Kind switch
            {
                XamlTokenKind.Comment => CodeColor.Comment,
                XamlTokenKind.Delimiter or XamlTokenKind.ElementName => CodeColor.Keyword,
                XamlTokenKind.AttributeName => CodeColor.Type,
                XamlTokenKind.AttributeValue => CodeColor.String,
                _ => CodeColor.Foreground,
            };
            var element = token.Element < 0 ? -1 : token.Element + indexOffset;
            var isChanged = token.Kind switch
            {
                XamlTokenKind.AttributeName or XamlTokenKind.Equals or XamlTokenKind.AttributeValue =>
                    changed is not null && token.Attribute is not null && changed.Contains((element, token.Attribute)),
                XamlTokenKind.Text => changedTexts is not null && element >= 0 && changedTexts.Contains(element)
                    && !string.IsNullOrWhiteSpace(document.Text.AsSpan(token.Start, token.Length).ToString()),
                _ => false,
            };
            spans.Add(new CodeSpan(token.Start, token.Length, color, isChanged));
        }
        return spans;
    }

    private static readonly HashSet<string> CSharpKeywords =
    [
        "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit",
        "extern", "false", "finally", "fixed", "float", "for", "foreach", "get", "goto", "if", "implicit", "in", "init",
        "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out",
        "override", "params", "partial", "private", "protected", "public", "readonly", "record", "ref", "required",
        "return", "sbyte", "sealed", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "value",
        "var", "virtual", "void", "volatile", "when", "where", "while", "with", "yield",
    ];

    /// <summary>The spans of C# <paramref name="code"/>: comments, strings, keywords, numbers, types and calls.</summary>
    public static IReadOnlyList<CodeSpan> CSharp(string code)
    {
        var spans = new List<CodeSpan>();
        var plain = 0;
        void Add(int start, int end, CodeColor color)
        {
            if (start > plain)
            {
                spans.Add(new CodeSpan(plain, start - plain, CodeColor.Foreground));
            }
            spans.Add(new CodeSpan(start, end - start, color));
            plain = end;
        }

        var i = 0;
        while (i < code.Length)
        {
            var c = code[i];
            if (c == '/' && i + 1 < code.Length && code[i + 1] == '/')
            {
                var end = code.IndexOf('\n', i);
                end = end < 0 ? code.Length : end;
                Add(i, end, CodeColor.Comment);
                i = end;
            }
            else if (c == '/' && i + 1 < code.Length && code[i + 1] == '*')
            {
                var end = code.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? code.Length : end + 2;
                Add(i, end, CodeColor.Comment);
                i = end;
            }
            else if (c == '"' || ((c is '$' or '@') && i + 1 < code.Length && code[i + 1] is '"' or '$' or '@'))
            {
                var start = i;
                var verbatim = false;
                while (i < code.Length && code[i] is '$' or '@')
                {
                    verbatim |= code[i] == '@';
                    i++;
                }
                i++;
                while (i < code.Length && code[i] != '"' && (verbatim || code[i] != '\n'))
                {
                    i += !verbatim && code[i] == '\\' ? 2 : 1;
                }
                i = Math.Min(i + 1, code.Length);
                Add(start, i, CodeColor.String);
            }
            else if (c == '\'')
            {
                var start = i++;
                while (i < code.Length && code[i] != '\'' && code[i] != '\n')
                {
                    i += code[i] == '\\' ? 2 : 1;
                }
                i = Math.Min(i + 1, code.Length);
                Add(start, i, CodeColor.String);
            }
            else if (char.IsDigit(c))
            {
                var start = i;
                while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] is '.' or '_'))
                {
                    i++;
                }
                Add(start, i, CodeColor.Function);
            }
            else if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_'))
                {
                    i++;
                }
                var word = code[start..i];
                var next = i;
                while (next < code.Length && code[next] == ' ')
                {
                    next++;
                }
                if (CSharpKeywords.Contains(word))
                {
                    Add(start, i, CodeColor.Keyword);
                }
                else if (next < code.Length && code[next] == '(')
                {
                    Add(start, i, CodeColor.Function);
                }
                else if (char.IsUpper(word[0]))
                {
                    Add(start, i, CodeColor.Type);
                }
            }
            else
            {
                i++;
            }
        }
        if (code.Length > plain)
        {
            spans.Add(new CodeSpan(plain, code.Length - plain, CodeColor.Foreground));
        }
        return spans;
    }
}
