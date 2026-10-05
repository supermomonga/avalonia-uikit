using System.Text;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's MaskPattern (crates/base/src/input/base/mask_pattern.rs): how a
/// single-line input formats its text while it is typed, set with
/// <see cref="Inputs.MaskPatternProperty"/>. A string in XAML is a
/// <see cref="PatternMask"/> (<c>uikit:Inputs.MaskPattern="(999)999-9999"</c>);
/// <see cref="NumberMask"/> groups a number's digits; <see cref="None"/> turns
/// masking off (a NumberInput masks numbers by default).
/// </summary>
public abstract class MaskPattern
{
    /// <summary>No mask: every text is valid and kept as typed (GPUI's MaskPattern::None).</summary>
    public static MaskPattern None { get; } = new NoMask();

    /// <summary>A <see cref="PatternMask"/> for <paramref name="pattern"/> (GPUI's MaskPattern::new).</summary>
    public static MaskPattern Parse(string pattern) => new PatternMask(pattern);

    /// <summary>Whether the mask leaves the text alone (GPUI's is_none).</summary>
    public abstract bool IsNone { get; }

    /// <summary>The placeholder the mask shows while the input is empty, if any: `_` for each slot.</summary>
    public virtual string? Placeholder => null;

    /// <summary>Whether <paramref name="text"/> fits the mask (GPUI's is_valid); an empty text always does.</summary>
    public abstract bool IsValid(string text);

    /// <summary>Formats <paramref name="text"/> (GPUI's mask): separators inserted, extra characters dropped.</summary>
    public abstract string Mask(string text);

    /// <summary>The text without the mask's separators (GPUI's unmask).</summary>
    public abstract string Unmask(string text);

    /// <summary>Rewrites what is typed before it is checked; a <see cref="NumberMask"/> turns full-width digits and signs into ASCII.</summary>
    public virtual string Normalize(string input) => input;

    private sealed class NoMask : MaskPattern
    {
        public override bool IsNone => true;

        public override bool IsValid(string text) => true;

        public override string Mask(string text) => text;

        public override string Unmask(string text) => text;
    }
}

/// <summary>
/// GPUI Kit's MaskPattern::Pattern: one slot per character of
/// <see cref="Pattern"/>. <c>9</c> takes a digit, <c>A</c> a letter, <c>#</c> a
/// letter or digit, <c>*</c> any character; any other character is a separator
/// the mask writes itself. "(999)999-9999" turns 1234567890 into
/// (123)456-7890; the empty input shows "(___)___-____".
/// </summary>
/// <remarks>
/// MaskedTextBox's syntax differs (<c>0</c> a required digit, <c>L</c> a
/// letter, <c>A</c> alphanumeric, <c>&amp;</c> any) and shows the whole template
/// with prompt characters; this mask grows with the text as GPUI's does.
/// </remarks>
public sealed class PatternMask : MaskPattern
{
    private readonly Token[] _tokens;

    /// <summary>Creates the mask for <paramref name="pattern"/>.</summary>
    public PatternMask(string pattern)
    {
        Pattern = pattern;
        _tokens = pattern.Select(c => c switch
        {
            '9' => new Token(TokenKind.Digit, c),
            'A' => new Token(TokenKind.Letter, c),
            '#' => new Token(TokenKind.LetterOrDigit, c),
            '*' => new Token(TokenKind.Any, c),
            _ => new Token(TokenKind.Separator, c),
        }).ToArray();
    }

    /// <summary>The pattern, as written.</summary>
    public string Pattern { get; }

    /// <inheritdoc />
    public override bool IsNone => _tokens.Length == 0;

    /// <inheritdoc />
    public override string? Placeholder => new(_tokens.Select(t => t.Kind == TokenKind.Separator ? t.Char : '_').ToArray());

    /// <inheritdoc />
    public override bool IsValid(string text)
    {
        if (IsNone)
        {
            return true;
        }
        // mask_pattern.rs is_valid: every character must be taken by a slot, in order.
        var index = 0;
        foreach (var token in _tokens)
        {
            if (index >= text.Length)
            {
                break;
            }
            if (token.Matches(text[index]))
            {
                index++;
            }
        }
        return index == text.Length;
    }

    /// <inheritdoc />
    public override string Mask(string text)
    {
        if (IsNone)
        {
            return text;
        }
        var result = new StringBuilder();
        var index = 0;
        for (var pos = 0; pos < _tokens.Length && index < text.Length; pos++)
        {
            var token = _tokens[pos];
            var ch = text[index];
            // A slot stops at the first character it cannot take; a separator writes itself.
            if (token.Kind != TokenKind.Separator && !IsValidAt(ch, pos))
            {
                break;
            }
            var masked = token.Kind == TokenKind.Separator ? token.Char : ch;
            result.Append(masked);
            if (ch == masked)
            {
                index++;
            }
        }
        return result.ToString();
    }

    /// <inheritdoc />
    public override string Unmask(string text)
    {
        var result = new StringBuilder();
        for (var i = 0; i < _tokens.Length && i < text.Length; i++)
        {
            if (_tokens[i].Kind != TokenKind.Separator)
            {
                result.Append(text[i]);
            }
        }
        return result.ToString();
    }

    /// <summary>mask_pattern.rs is_valid_at: the slot takes the character, or it is a separator and the next slot does.</summary>
    private bool IsValidAt(char ch, int pos)
    {
        if (pos >= _tokens.Length)
        {
            return false;
        }
        var token = _tokens[pos];
        return token.Matches(ch) || (token.Kind == TokenKind.Separator && pos + 1 < _tokens.Length && _tokens[pos + 1].Matches(ch));
    }

    private enum TokenKind
    {
        Digit,
        Letter,
        LetterOrDigit,
        Any,
        Separator,
    }

    private readonly record struct Token(TokenKind Kind, char Char)
    {
        public bool Matches(char ch) => Kind switch
        {
            TokenKind.Digit => char.IsAsciiDigit(ch),
            TokenKind.Letter => char.IsAsciiLetter(ch),
            TokenKind.LetterOrDigit => char.IsAsciiLetterOrDigit(ch),
            TokenKind.Any => true,
            _ => ch == Char,
        };
    }
}

/// <summary>
/// GPUI Kit's MaskPattern::Number: a number with an optional sign, digits and
/// one decimal point. With a <see cref="Separator"/> the integer digits are
/// grouped by three while typing ("1234567.891" becomes "1,234,567.891") and
/// at most <see cref="Fraction"/> digits follow the point (none at 0). Without
/// a separator the text is kept as typed. Full-width digits, signs, points and
/// commas are typed as their ASCII forms.
/// </summary>
public sealed class NumberMask : MaskPattern
{
    /// <summary>The group separator, e.g. ',' or ' '; none by default.</summary>
    public char? Separator { get; set; }

    /// <summary>The most digits after the decimal point; unlimited by default.</summary>
    public int? Fraction { get; set; }

    /// <inheritdoc />
    public override bool IsNone => false;

    /// <inheritdoc />
    public override bool IsValid(string text)
    {
        if (text.Length == 0)
        {
            return true;
        }
        var parts = text.Split('.');
        if (parts.Length > 2)
        {
            return false;
        }
        var integer = parts[0];
        // One sign, and only at the start.
        var signs = 0;
        for (var i = 0; i < integer.Length; i++)
        {
            if (IsSign(integer[i]))
            {
                if (i > 0 || ++signs > 1)
                {
                    return false;
                }
            }
            else if (!char.IsAsciiDigit(integer[i]) && integer[i] != Separator)
            {
                return false;
            }
        }
        return parts.Length < 2 || parts[1].All(c => char.IsAsciiDigit(c) || c == Separator);
    }

    /// <inheritdoc />
    public override string Mask(string text)
    {
        if (Separator is not { } separator)
        {
            return text;
        }
        var parts = text.Replace(separator.ToString(), string.Empty, StringComparison.Ordinal).Split('.');
        var integer = parts[0];
        var fraction = parts.Length > 1 ? parts[1][..Math.Min(parts[1].Length, Fraction ?? int.MaxValue)] : null;
        // The sign stays out of the grouping (no "-,123").
        string? sign = null;
        if (integer.IndexOfAny(['+', '-']) is var at and >= 0)
        {
            sign = integer[at].ToString();
            integer = integer.Remove(at, 1);
        }
        var grouped = new StringBuilder();
        for (var i = 0; i < integer.Length; i++)
        {
            if (i > 0 && (integer.Length - i) % 3 == 0)
            {
                grouped.Append(separator);
            }
            grouped.Append(integer[i]);
        }
        var result = fraction is null || Fraction == 0 ? grouped.ToString() : $"{grouped}.{fraction}";
        return sign + result;
    }

    /// <inheritdoc />
    public override string Unmask(string text)
    {
        if (Separator is not { } separator)
        {
            return text;
        }
        var result = text.Replace(separator.ToString(), string.Empty, StringComparison.Ordinal);
        // mask_pattern.rs unmask trims a fraction's trailing zeros.
        return result.Contains('.', StringComparison.Ordinal) ? result.TrimEnd('0') : result;
    }

    /// <inheritdoc />
    public override string Normalize(string input)
    {
        // mask_pattern.rs normalize_number_input: one character for one, so offsets stay valid.
        static char Ascii(char c) => c switch
        {
            >= '０' and <= '９' => (char)(c - '０' + '0'),
            '＋' => '+',
            '－' or '−' => '-',
            '．' or '。' => '.',
            '，' => ',',
            _ => c,
        };
        return string.Create(input.Length, input, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                span[i] = Ascii(source[i]);
            }
        });
    }

    private static bool IsSign(char c) => c is '+' or '-';
}
