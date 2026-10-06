using System.Text;

namespace AvaloniaUIKit.Demo.ControlCatalog.Xaml;

/// <summary>
/// Changes to a <see cref="XamlDocument"/>'s attributes and texts, by element index,
/// written back into the text in place: an attribute keeps its position and quotes, a
/// new one follows the element's last attribute (on a line of its own when the
/// attributes are one per line), and the rest of the text stays as it was.
/// </summary>
public sealed class XamlEdits
{
    // element index → attribute name → value (null removes the attribute).
    private readonly Dictionary<int, Dictionary<string, string?>> _attributes = [];
    private readonly Dictionary<int, string> _texts = [];

    /// <summary>Raised after every change.</summary>
    public event EventHandler? Changed;

    /// <summary>Whether nothing is changed.</summary>
    public bool IsEmpty => _attributes.Count == 0 && _texts.Count == 0;

    /// <summary>Sets an attribute of the element at <paramref name="element"/>; null removes it.</summary>
    public void SetAttribute(int element, string name, string? value)
    {
        if (!_attributes.TryGetValue(element, out var attributes))
        {
            _attributes[element] = attributes = new Dictionary<string, string?>(StringComparer.Ordinal);
        }
        attributes[name] = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Sets the text inside the element at <paramref name="element"/>.</summary>
    public void SetText(int element, string text)
    {
        _texts[element] = text;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Drops the change to an attribute, so that the original text shows again.</summary>
    public void Revert(int element, string name)
    {
        if (_attributes.TryGetValue(element, out var attributes) && attributes.Remove(name))
        {
            if (attributes.Count == 0)
            {
                _attributes.Remove(element);
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Drops the change to the text inside an element.</summary>
    public void RevertText(int element)
    {
        if (_texts.Remove(element))
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Drops every change.</summary>
    public void Clear()
    {
        if (!IsEmpty)
        {
            _attributes.Clear();
            _texts.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>The document's text with the changes made.</summary>
    public string Apply(XamlDocument document)
    {
        var text = document.Text;
        // (offset, length removed, text inserted), applied from the end so offsets hold.
        var operations = new List<(int Offset, int Length, string Insert)>();
        foreach (var (index, attributes) in _attributes)
        {
            if (index >= document.Elements.Count)
            {
                continue;
            }
            var element = document.Elements[index];
            var added = new StringBuilder();
            var (separator, anchor) = Insertion(text, element);
            foreach (var (name, value) in attributes)
            {
                var existing = element.Attribute(name);
                if (existing is null)
                {
                    if (value is not null)
                    {
                        added.Append(separator).Append(name).Append("=\"").Append(XamlDocument.EscapeAttribute(value)).Append('"');
                    }
                }
                else if (value is not null)
                {
                    operations.Add((existing.ValueStart, existing.ValueLength, XamlDocument.EscapeAttribute(value, existing.Quote)));
                }
                else
                {
                    var start = existing.NameStart;
                    while (start > element.NameEnd && char.IsWhiteSpace(text[start - 1]))
                    {
                        start--;
                    }
                    operations.Add((start, existing.ValueStart + existing.ValueLength + 1 - start, ""));
                }
            }
            if (added.Length > 0)
            {
                operations.Add((anchor, 0, added.ToString()));
            }
        }
        foreach (var (index, value) in _texts)
        {
            if (index >= document.Elements.Count)
            {
                continue;
            }
            var element = document.Elements[index];
            if (element.IsEmpty || element.Children.Count > 0)
            {
                continue;
            }
            var inner = text[element.StartTagEnd..element.EndTagStart];
            var lead = inner.Length - inner.TrimStart().Length;
            var trail = inner.Length - inner.TrimEnd().Length;
            var length = Math.Max(0, inner.Length - lead - trail);
            operations.Add((element.StartTagEnd + lead, length, XamlDocument.EscapeText(value)));
        }
        if (operations.Count == 0)
        {
            return text;
        }
        // Later offsets first; at one offset, removals before insertions.
        operations.Sort((a, b) => a.Offset != b.Offset ? b.Offset.CompareTo(a.Offset) : b.Length.CompareTo(a.Length));
        var builder = new StringBuilder(text);
        foreach (var (offset, length, insert) in operations)
        {
            builder.Remove(offset, length).Insert(offset, insert);
        }
        return builder.ToString();
    }

    /// <summary>The attributes that changes set, as (element index, name).</summary>
    public IEnumerable<(int Element, string Name)> SetAttributes =>
        _attributes.SelectMany(e => e.Value.Where(a => a.Value is not null).Select(a => (e.Key, a.Key)));

    /// <summary>The elements whose text changes set.</summary>
    public IEnumerable<int> SetTexts => _texts.Keys;

    // New attributes go after the last one: on a line of their own, at its column, when the
    // element puts its attributes on lines of their own; after a space otherwise.
    private static (string Separator, int Anchor) Insertion(string text, XamlElement element)
    {
        if (element.Attributes.Count == 0)
        {
            return (" ", element.NameEnd);
        }
        var last = element.Attributes[^1];
        var anchor = last.ValueStart + last.ValueLength + 1;
        var lineStart = text.LastIndexOf('\n', last.NameStart) + 1;
        if (lineStart > element.Start)
        {
            var indent = text[lineStart..last.NameStart];
            if (indent.All(char.IsWhiteSpace))
            {
                return ("\n" + indent, anchor);
            }
        }
        return (" ", anchor);
    }

    /// <summary>
    /// The content of the root element with the common indentation removed: the code
    /// example the site shows for a demo (sites/app/lib/demos.ts).
    /// </summary>
    public static string Inner(XamlDocument document)
    {
        if (document.Root is not { IsEmpty: false } root)
        {
            return document.Text.Trim();
        }
        return Dedent(document.Text[root.StartTagEnd..root.EndTagStart]);
    }

    /// <summary>Removes blank lines at the ends and the indentation every line shares.</summary>
    public static string Dedent(string text)
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
        {
            lines.RemoveAt(0);
        }
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }
        var indent = lines.Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Length - l.TrimStart(' ', '\t').Length)
            .DefaultIfEmpty(0)
            .Min();
        return string.Join('\n', lines.Select(l => string.IsNullOrWhiteSpace(l) ? "" : l[indent..]));
    }
}
