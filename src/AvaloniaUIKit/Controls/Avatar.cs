using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Avatar (avatar/avatar.rs): a circle showing <see cref="Source"/>,
/// else the initials of <see cref="UserName"/> on a color picked from them
/// (one of GPUI's twelve hues, the same as GPUI picks), else the
/// <see cref="Placeholder"/> icon. Size classes: xsmall (16px), small (24),
/// the default (48) and large (80).
/// </summary>
public class Avatar : TemplatedControl
{
    /// <summary>The user's name; its initials show when there is no image.</summary>
    public static readonly StyledProperty<string?> UserNameProperty =
        AvaloniaProperty.Register<Avatar, string?>(nameof(UserName));

    /// <summary>The image to show.</summary>
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<Avatar, IImage?>(nameof(Source));

    /// <summary>The icon shown without a name or image (GPUI's default is the user icon).</summary>
    public static readonly StyledProperty<Geometry?> PlaceholderProperty =
        AvaloniaProperty.Register<Avatar, Geometry?>(nameof(Placeholder));

    /// <summary>The initials of <see cref="UserName"/>, as GPUI extracts them.</summary>
    public static readonly DirectProperty<Avatar, string> InitialsProperty =
        AvaloniaProperty.RegisterDirect<Avatar, string>(nameof(Initials), a => a.Initials);

    /// <summary>Which of the twelve identity hues the initials pick, or -1 without a name.</summary>
    public static readonly DirectProperty<Avatar, int> HueProperty =
        AvaloniaProperty.RegisterDirect<Avatar, int>(nameof(Hue), a => a.Hue);

    private string _initials = "";
    private int _hue = -1;

    static Avatar()
    {
        UserNameProperty.Changed.AddClassHandler<Avatar>((a, _) => a.Update());
        SourceProperty.Changed.AddClassHandler<Avatar>((a, _) => a.Update());
    }

    /// <summary>Creates an avatar.</summary>
    public Avatar() => Update();

    /// <inheritdoc cref="UserNameProperty"/>
    public string? UserName
    {
        get => GetValue(UserNameProperty);
        set => SetValue(UserNameProperty, value);
    }

    /// <inheritdoc cref="SourceProperty"/>
    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <inheritdoc cref="PlaceholderProperty"/>
    public Geometry? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <inheritdoc cref="InitialsProperty"/>
    public string Initials
    {
        get => _initials;
        private set => SetAndRaise(InitialsProperty, ref _initials, value);
    }

    /// <inheritdoc cref="HueProperty"/>
    public int Hue
    {
        get => _hue;
        private set => SetAndRaise(HueProperty, ref _hue, value);
    }

    /// <summary>
    /// GPUI's extract_text_initials: the first letters of the first two words,
    /// or the first two characters of a single word, upper-cased.
    /// </summary>
    public static string ExtractInitials(string name)
    {
        var words = name.Split(' ');
        var result = string.Concat(words.Where(w => w.Length > 0).Take(2).Select(w => FirstChar(w)));
        // GPUI compares the UTF-8 length with 1: a single ASCII letter.
        if (System.Text.Encoding.UTF8.GetByteCount(result) == 1)
        {
            result = string.Concat(Chars(name).Take(2));
        }
        return result.ToUpperInvariant();
    }

    private static string FirstChar(string word) => Chars(word).First();

    private static IEnumerable<string> Chars(string text)
    {
        for (var i = 0; i < text.Length; i += char.IsSurrogatePair(text, i) ? 2 : 1)
        {
            yield return char.IsSurrogatePair(text, i) ? text.Substring(i, 2) : text[i].ToString();
        }
    }

    private void Update()
    {
        var name = UserName;
        Initials = name is null ? "" : ExtractInitials(name);
        // avatar.rs IdentityColor::new: the hash of the initials picks one of 12 hues.
        Hue = name is null ? -1 : (int)(FxHash.Hash(Initials) % 12);
        PseudoClasses.Set(":image", Source is not null);
        PseudoClasses.Set(":initials", Source is null && name is not null);
        PseudoClasses.Set(":placeholder", Source is null && name is null);
    }
}
