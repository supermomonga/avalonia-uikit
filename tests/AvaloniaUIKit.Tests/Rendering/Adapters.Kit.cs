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
        _ => KitControl2(c),
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
            Content = new Border { Width = side, Height = side, CornerRadius = new Avalonia.CornerRadius(6), Background = ThemeBrush(c, "UIKit.Secondary") },
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
            badge.BadgeBackground = ThemeBrush(c, "UIKit." + char.ToUpperInvariant(color[0]) + color[1..]);
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
            rating.ActiveBrush = ThemeBrush(c, "UIKit.Red");
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

public static partial class Adapters
{
    private static Control? KitControl2(GoldenCase c) => c.Component switch
    {
        "descriptionlist" => DescriptionListCase(c),
        "stepper" => StepperCase(c),
        "form" => FormCase(c),
        "hovercard" => HoverCardCase(c),
        "shimmer" => ShimmerCase(c),
        "marker" => MarkerCase(c),
        "bubble" => BubbleCase(c),
        "message" => MessageCase(c),
        _ => null,
    };

    private static Control DescriptionListCase(GoldenCase c)
    {
        var list = Sized(new DescriptionList
        {
            Orientation = c.Str("layout", "horizontal") == "vertical" ? Orientation.Vertical : Orientation.Horizontal,
            IsBordered = !c.Bool("borderless"),
            Columns = (int)c.Num("columns", 3),
            Width = c.Num("width", 520),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        }, c);
        if (c.Has("label_width"))
        {
            list.LabelWidth = c.Num("label_width", 120);
        }
        foreach (var item in c.Str("items", "Name:GPUI Kit|Version:0.1.0|License:Apache-2.0").Split('|'))
        {
            if (item == "-")
            {
                list.Items.Add(new DescriptionSeparator());
                continue;
            }
            var parts = item.Split(':', 3);
            list.Items.Add(new DescriptionItem
            {
                Label = parts[0],
                Value = parts.Length > 1 ? parts[1] : "",
                Span = parts.Length > 2 ? int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture) : 1,
            });
        }
        return list;
    }

    private static Control StepperCase(GoldenCase c)
    {
        var vertical = c.Str("layout", "horizontal") == "vertical";
        var stepper = Sized(new Stepper
        {
            SelectedIndex = (int)c.Num("selected", 0),
            Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal,
            CentersSteps = c.Bool("text_center"),
            IsEnabled = !c.State.Split('+').Contains("disabled"),
            Width = c.Num("width", 480),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        }, c);
        if (vertical)
        {
            stepper.Height = c.Num("height", 200);
        }
        var icons = c.Str("icons").Split('|');
        var labels = c.Str("labels", "Step 1|Step 2|Step 3").Split('|');
        for (var i = 0; i < labels.Length; i++)
        {
            var step = new StepperItem { Content = labels[i] };
            if (i < icons.Length && icons[i].Length > 0)
            {
                step.Icon = Icon(icons[i]).Data;
            }
            stepper.Items.Add(step);
        }
        return stepper;
    }

    private static Control FormCase(GoldenCase c)
    {
        var size = c.Str("size", "medium");
        var form = Sized(new Form
        {
            LabelOrientation = c.Str("layout", "vertical") == "horizontal" ? Orientation.Horizontal : Orientation.Vertical,
            Columns = (int)c.Num("columns", 1),
            Width = c.Num("width", 360),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        }, c);
        if (c.Has("label_width"))
        {
            form.LabelWidth = c.Num("label_width", 140);
        }
        foreach (var spec in c.Str("fields", "Name|Email").Split('|'))
        {
            var rest = spec;
            var span = 1;
            if (rest.Split('^') is [var r, var s])
            {
                rest = r;
                span = int.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            }
            string? description = null;
            if (rest.Split('~') is [var l, var d])
            {
                rest = l;
                description = d;
            }
            var label = rest.TrimEnd('*');
            var box = new TextBox { PlaceholderText = "Enter " + label.ToLowerInvariant() };
            if (size != "medium")
            {
                box.Classes.Add(size);
            }
            form.Items.Add(new FormField { Label = label, IsRequired = rest.EndsWith('*'), Description = description, ColumnSpan = span, Content = box });
        }
        if (c.Has("footer"))
        {
            var button = new Button { Content = c.Str("footer"), Classes = { "primary" } };
            if (size != "medium")
            {
                button.Classes.Add(size);
            }
            form.Footer = button;
        }
        return form;
    }

    private static Control HoverCardCase(GoldenCase c) => new HoverCard
    {
        Placement = c.Str("anchor", "top-center") switch
        {
            "top-left" => Avalonia.Controls.PlacementMode.BottomEdgeAlignedLeft,
            "top-right" => Avalonia.Controls.PlacementMode.BottomEdgeAlignedRight,
            "bottom-center" => Avalonia.Controls.PlacementMode.Top,
            _ => Avalonia.Controls.PlacementMode.Bottom,
        },
        Content = new TextBlock { Text = "Hover over me", FontSize = 14, LineHeight = 22.5, Foreground = ThemeBrush(c, "UIKit.Primary") },
        Card = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = c.Str("title", "This is a hover card"), FontSize = 14, LineHeight = 22.5, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = c.Str("body", "Rich content on hover."), FontSize = 14, LineHeight = 22.5, Foreground = ThemeBrush(c, "UIKit.MutedForeground") },
            },
        },
    };

    private static Control ShimmerCase(GoldenCase c)
    {
        var shimmer = new ShimmerText
        {
            Text = c.Str("text", "Thinking…"),
            Repeats = false,
            IsReversed = c.Bool("reverse"),
            Duration = TimeSpan.FromMilliseconds(c.Num("duration", 2000)),
        };
        if (c.Str("text_size", "base") == "sm")
        {
            shimmer.FontSize = 14;
            shimmer.SetValue(TextBlock.LineHeightProperty, 22.5);
        }
        if (c.Str("color") == "muted")
        {
            shimmer.Foreground = ThemeBrush(c, "UIKit.MutedForeground");
        }
        return shimmer;
    }

    private static Control MarkerCase(GoldenCase c)
    {
        var marker = new Marker
        {
            Content = c.Str("text", "Conversation archived"),
            IsLoading = c.Bool("loading"),
            Width = c.Num("width", 320),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        ClassFrom(marker, c, "variant", "plain");
        if (c.Str("loading_style", "spinner") == "shimmer")
        {
            marker.Classes.Add("shimmer");
        }
        if (c.Str("icon") is { Length: > 0 } icon)
        {
            marker.Icon = Icon(icon);
        }
        switch (c.Str("align"))
        {
            case "start":
                marker.HorizontalContentAlignment = HorizontalAlignment.Left;
                break;
            case "center":
                marker.HorizontalContentAlignment = HorizontalAlignment.Center;
                break;
            case "end":
                marker.HorizontalContentAlignment = HorizontalAlignment.Right;
                break;
        }
        return marker;
    }

    private static Bubble MakeBubble(GoldenCase c, string text)
    {
        var bubble = new Bubble { Content = text };
        ClassFrom(bubble, c, "variant", "filled");
        return bubble;
    }

    private static Control BubbleCase(GoldenCase c)
    {
        var bubble = MakeBubble(c, c.Str("text", "Can you review this draft?"));
        ClassFrom(bubble, c, "align", "");
        if (c.Has("reaction"))
        {
            bubble.Reaction = c.Str("reaction");
            if (c.Str("reaction_side", "bottom") == "top")
            {
                bubble.Classes.Add("reaction-top");
            }
            if (c.Str("reaction_align", "end") == "start")
            {
                bubble.Classes.Add("reaction-start");
            }
        }
        return new Panel { Width = c.Num("width", 360), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { bubble } };
    }

    private static Control MessageCase(GoldenCase c)
    {
        var message = new Message { Width = c.Num("width", 400), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        if (c.Str("align", "start") == "end")
        {
            message.Classes.Add("end");
        }
        if (c.Has("avatar"))
        {
            message.Avatar = new Avatar { UserName = c.Str("avatar"), Width = 32, Height = 32 };
        }
        if (c.Has("header"))
        {
            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            foreach (var part in c.Str("header").Split('·'))
            {
                header.Children.Add(new TextBlock { Text = part.Trim() });
            }
            message.Header = header;
        }
        foreach (var text in c.Str("texts", "Can you review this draft?").Split('|'))
        {
            if (c.Bool("plain"))
            {
                message.Items.Add(text);
            }
            else
            {
                var bubble = new Bubble { Content = text };
                ClassFrom(bubble, c, "variant", "secondary");
                if (!bubble.Classes.Any())
                {
                    bubble.Classes.Add("secondary");
                }
                message.Items.Add(bubble);
            }
        }
        if (c.Has("footer"))
        {
            message.Footer = c.Str("footer");
        }
        return message;
    }
}
