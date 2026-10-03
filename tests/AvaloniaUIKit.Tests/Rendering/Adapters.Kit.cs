using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>Adapters for the controls this library adds (ADR 19).</summary>
public static partial class Adapters
{
    private static Control? KitControl(GoldenCase c) => c.Component switch
    {
        "badge" => BadgeCase(c),
        "tag" => TagCase(c),
        "alert" => AlertCase(c),
        "skeleton" => SkeletonCase(c),
        "statusbar" => StatusBarCase(c),
        "breadcrumb" => BreadcrumbCase(c),
        "kbd" => KbdCase(c),
        "clipboard" => Sized(new AvaloniaUIKit.Clipboard { Text = c.Str("value", "Copied text") }, c),
        "rating" => RatingCase(c),
        "avatar" => AvatarCase(c),
        "avatargroup" => AvatarGroupCase(c),
        "empty" => EmptyCase(c),
        _ => null,
    };

    /// <summary>A theme brush for the case's theme.</summary>
    private static IBrush ThemeBrush(GoldenCase c, string key) =>
        (IBrush)Avalonia.Application.Current!.FindResource(c.IsDark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light, key)!;

    private static T Sized<T>(T control, GoldenCase c) where T : Control
    {
        ClassFrom(control, c, "size", "medium");
        return control;
    }

    private static Control BadgeCase(GoldenCase c)
    {
        var side = c.Str("size", "medium") switch { "large" => 48, "small" or "xsmall" => 24, _ => 32 };
        var badge = Sized(new Badge
        {
            Content = new Border { Width = side, Height = side, CornerRadius = new Avalonia.CornerRadius(6), Background = ThemeBrush(c, "Gpui.Secondary") },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        }, c);
        switch (c.Str("variant", "count"))
        {
            case "dot":
                badge.IsDot = true;
                break;
            case "icon":
                badge.Icon = Icon(c.Str("icon", "check")).Data;
                break;
            default:
                badge.Count = (int)c.Num("count", 3);
                break;
        }
        if (c.Has("max"))
        {
            badge.Maximum = (int)c.Num("max", 99);
        }
        if (c.Str("color") is { Length: > 0 } color)
        {
            badge.BadgeBackground = ThemeBrush(c, "Gpui." + char.ToUpperInvariant(color[0]) + color[1..]);
        }
        return badge;
    }

    private static Control TagCase(GoldenCase c)
    {
        var tag = Sized(new TagLabel { Content = c.Str("label", "Tag") }, c);
        ClassFrom(tag, c, "variant", "primary");
        FlagClass(tag, c, "outline");
        FlagClass(tag, c, "rounded_full", "rounded-full");
        return tag;
    }

    private static Control AlertCase(GoldenCase c)
    {
        var alert = Sized(new Alert
        {
            Content = c.Str("message", "This is an alert message."),
            Title = c.Has("title") ? c.Str("title") : null,
            IsClosable = c.Bool("closable"),
            Width = c.Num("width", 360),
        }, c);
        ClassFrom(alert, c, "variant", "default");
        FlagClass(alert, c, "banner");
        if (c.Has("icon"))
        {
            alert.Icon = Icon(c.Str("icon")).Data;
        }
        return alert;
    }

    private static Control SkeletonCase(GoldenCase c)
    {
        var skeleton = new Skeleton
        {
            Width = c.Num("width", 240),
            CornerRadius = new Avalonia.CornerRadius(c.Num("radius", 0)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        if (c.Num("height", 0) is > 0 and var height)
        {
            skeleton.Height = height;
        }
        FlagClass(skeleton, c, "secondary");
        return skeleton;
    }

    private static object? Region(GoldenCase c, string key)
    {
        var texts = c.Str(key).Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (texts.Length == 0)
        {
            return null;
        }
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var text in texts)
        {
            row.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
        }
        return row;
    }

    private static Control StatusBarCase(GoldenCase c) => new StatusBar
    {
        Left = Region(c, "left"),
        Content = Region(c, "center"),
        Right = Region(c, "right"),
        Width = c.Num("width", 400),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
    };

    private static Control BreadcrumbCase(GoldenCase c)
    {
        var breadcrumb = new Breadcrumb { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var disabled = c.Has("disabled_item") ? (int)c.Num("disabled_item", -1) : -1;
        var labels = c.Str("items", "Home|Documents|Report").Split('|');
        for (var i = 0; i < labels.Length; i++)
        {
            breadcrumb.Items.Add(new BreadcrumbItem { Content = labels[i], IsEnabled = i != disabled });
        }
        return breadcrumb;
    }

    private static Control KbdCase(GoldenCase c)
    {
        var kbd = new Kbd { Gesture = GpuiGesture(c.Str("keys", "a")) };
        FlagClass(kbd, c, "outline");
        return kbd;
    }

    /// <summary>A GPUI keystroke ("cmd-shift-a", "pagedown") as an Avalonia gesture.</summary>
    public static KeyGesture GpuiGesture(string stroke)
    {
        var parts = stroke.Split('-');
        var modifiers = KeyModifiers.None;
        foreach (var part in parts[..^1])
        {
            modifiers |= part switch
            {
                "ctrl" => KeyModifiers.Control,
                "alt" => KeyModifiers.Alt,
                "shift" => KeyModifiers.Shift,
                "cmd" or "win" or "platform" => KeyModifiers.Meta,
                _ => throw new NotSupportedException(part),
            };
        }
        var key = parts[^1] switch
        {
            "space" => Key.Space,
            "pagedown" => Key.PageDown,
            "pageup" => Key.PageUp,
            "tab" => Key.Tab,
            "enter" => Key.Enter,
            "escape" => Key.Escape,
            "backspace" => Key.Back,
            "delete" => Key.Delete,
            "left" => Key.Left,
            "right" => Key.Right,
            "up" => Key.Up,
            "down" => Key.Down,
            var k when k.Length == 1 && char.IsLetter(k[0]) => Key.A + (char.ToLowerInvariant(k[0]) - 'a'),
            var k when k.Length == 1 && char.IsDigit(k[0]) => Key.D0 + (k[0] - '0'),
            var k when k.StartsWith('f') && int.TryParse(k[1..], out var n) => Key.F1 + (n - 1),
            var k => throw new NotSupportedException(k),
        };
        return new KeyGesture(key, modifiers);
    }

    private static Control RatingCase(GoldenCase c)
    {
        var rating = Sized(new Rating
        {
            Value = (int)c.Num("value", 3),
            Maximum = (int)c.Num("max", 5),
            IsEnabled = !c.State.Split('+').Contains("disabled"),
        }, c);
        if (c.Str("color") == "red")
        {
            rating.ActiveBrush = ThemeBrush(c, "Gpui.Red");
        }
        return rating;
    }

    private static Control AvatarCase(GoldenCase c)
    {
        var avatar = Sized(new Avatar(), c);
        if (c.Has("name"))
        {
            avatar.UserName = c.Str("name");
        }
        if (c.Has("image"))
        {
            avatar.Source = new Avalonia.Media.Imaging.Bitmap(Path.Combine(AvaloniaUIKit.Tests.Infrastructure.Repo.Root, "assets", "images", "small-48x48.png"));
        }
        return avatar;
    }

    private static Control AvatarGroupCase(GoldenCase c)
    {
        var group = Sized(new AvatarGroup { Limit = (int)c.Num("limit", 3), ShowsEllipsis = c.Bool("ellipsis") }, c);
        foreach (var name in c.Str("names", "Alice|Bob|Carol").Split('|'))
        {
            group.Items.Add(new Avatar { UserName = name });
        }
        return group;
    }

    private static Control EmptyCase(GoldenCase c)
    {
        var empty = new EmptyState { Width = c.Num("width", 360), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        switch (c.Str("media", "icon"))
        {
            case "icon":
                empty.Classes.Add("icon-media");
                empty.Media = Icon("folder");
                break;
            case "plain":
                var inbox = Icon("inbox");
                inbox.Width = inbox.Height = 40;
                empty.Media = inbox;
                break;
        }
        if (c.Has("title"))
        {
            empty.Title = c.Str("title");
        }
        if (c.Has("description"))
        {
            empty.Description = c.Str("description");
        }
        var actions = c.Str("actions").Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (actions.Length > 0)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
            for (var i = 0; i < actions.Length; i++)
            {
                row.Children.Add(new Button { Content = actions[i], Classes = { i == 0 ? "primary" : "outline" } });
            }
            empty.Actions = row;
        }
        return empty;
    }
}
