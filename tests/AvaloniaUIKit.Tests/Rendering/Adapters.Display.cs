using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>Adapters for uikit:TextLabel, uikit:Icon and uikit:AsyncImage (reference/src/cases/label.rs, icon.rs, image.rs).</summary>
public static partial class Adapters
{
    /// <summary>The uikit-label cases: the label cases' sizes, weights, colors and widths, highlights and the mask.</summary>
    public static TextLabel TextLabelCase(GoldenCase c)
    {
        var label = new TextLabel
        {
            Text = c.Str("text", "Label"),
            Secondary = c.Has("secondary") ? c.Str("secondary") : null,
            Highlights = c.Has("highlights") ? c.Str("highlights") : null,
            HighlightsMatch = c.Bool("prefix") ? HighlightsMatch.Prefix : HighlightsMatch.Full,
            IsMasked = c.Bool("masked"),
            FontSize = c.Str("text_size", "base") switch { "xs" => 12, "sm" => 14, "lg" => 18, "xl" => 20, "2xl" => 24, _ => 16 },
            FontWeight = c.Str("weight", "normal") switch
            {
                "medium" => FontWeight.Medium,
                "semibold" => FontWeight.SemiBold,
                "bold" => FontWeight.Bold,
                _ => FontWeight.Normal,
            },
        };
        if (c.Has("width"))
        {
            label.Width = c.Num("width", 0);
            label.TextAlignment = c.Str("align", "left") switch
            {
                "center" => TextAlignment.Center,
                "right" => TextAlignment.Right,
                _ => TextAlignment.Left,
            };
        }
        if (c.Str("leading", "default") == "relaxed")
        {
            label.LineHeight = 28.8;
        }
        if (c.Has("color"))
        {
            label.Foreground = ThemeBrush(c, c.Str("color") == "muted" ? "UIKit.MutedForeground" : "UIKit.Danger");
        }
        return label;
    }

    /// <summary>The IconName of a GPUI icon file name ("chevron-down" is ChevronDown).</summary>
    public static IconName IconKind(string name) =>
        Enum.Parse<IconName>(string.Concat(name.Split('-').Select(p => char.ToUpperInvariant(p[0]) + p[1..])));

    /// <summary>
    /// The uikit-icon cases: a uikit:Icon by Kind with the icon cases' size
    /// class, color and rotation, or every IconName in a grid of 12 a row.
    /// </summary>
    public static Control IconKindCase(GoldenCase c)
    {
        if (c.Bool("grid"))
        {
            var side = c.Str("size", "medium") switch { "xsmall" => 12, "small" => 14, "large" => 24, _ => 16 };
            var grid = new WrapPanel { Width = 12 * side + 11 * 8, ItemSpacing = 8, LineSpacing = 8 };
            foreach (var kind in Enum.GetValues<IconName>())
            {
                var item = new AvaloniaUIKit.Icon { Kind = kind };
                ClassFrom(item, c, "size", "medium");
                grid.Children.Add(item);
            }
            return grid;
        }
        var icon = new AvaloniaUIKit.Icon { Kind = IconKind(c.Str("icon")) };
        if (c.Has("size"))
        {
            ClassFrom(icon, c, "size", "medium");
        }
        if (c.Has("color"))
        {
            icon.Foreground = ThemeBrush(c, "UIKit." + string.Concat(c.Str("color").Split('-').Select(p => char.ToUpperInvariant(p[0]) + p[1..])));
        }
        if (c.Has("rotate"))
        {
            icon.RenderTransform = new RotateTransform(c.Num("rotate", 0));
        }
        return icon;
    }

    /// <summary>
    /// The uikit-image cases: an AsyncImage whose loader hands over the case's
    /// test image at once, never finishes, or fails, with the image cases' box,
    /// fit and radius, and the app's loading and fallback content.
    /// </summary>
    public static AsyncImage AsyncImageCase(GoldenCase c)
    {
        var file = c.Str("image", "wide") switch
        {
            "wide" => "wide-192x96.png",
            "tall" => "tall-96x192.png",
            _ => "small-48x48.png",
        };
        var image = new AsyncImage
        {
            Loader = new CaseImageLoader(c.Str("source", "loaded")),
            Loading = PendingContent(c, "loader"),
            Fallback = PendingContent(c, "circle-x"),
        };
        if (c.Num("width", 96) is > 0 and var width && c.Num("height", 96) is > 0 and var height)
        {
            image.Width = width;
            image.Height = height;
        }
        // GPUI's ObjectFit as Stretch (AsyncImage.axaml).
        switch (c.Str("fit", "contain"))
        {
            case "fill":
                image.Stretch = Stretch.Fill;
                break;
            case "cover":
                image.Stretch = Stretch.UniformToFill;
                break;
            case "scale-down":
                image.StretchDirection = StretchDirection.DownOnly;
                break;
            case "none":
                image.Stretch = Stretch.None;
                break;
        }
        if (c.Num("radius", 0) == 8)
        {
            image.Classes.Add("rounded-lg");
        }
        image.Source = new Uri(Path.Combine(AvaloniaUIKit.Tests.Infrastructure.Repo.Root, "assets", "images", file));
        return image;
    }

    /// <summary>The cases' loading and fallback content: a muted box with a muted 16px icon in the middle.</summary>
    private static Border PendingContent(GoldenCase c, string icon)
    {
        var mark = Icon(icon);
        mark.Foreground = ThemeBrush(c, "UIKit.MutedForeground");
        mark.HorizontalAlignment = HorizontalAlignment.Center;
        mark.VerticalAlignment = VerticalAlignment.Center;
        return new Border { Background = ThemeBrush(c, "UIKit.Muted"), Child = mark };
    }

    /// <summary>
    /// GPUI's case loaders: the decoded test image at once ("loaded", as GPUI's
    /// case hands img() a decoded image), a load that never ends ("loading"),
    /// or a failure ("failed").
    /// </summary>
    private sealed class CaseImageLoader(string source) : IImageLoader
    {
        public Task<IImage> LoadAsync(Uri uri, CancellationToken cancellationToken) => source switch
        {
            "loading" => new TaskCompletionSource<IImage>().Task,
            "failed" => Task.FromException<IImage>(new IOException("missing")),
            _ => Task.FromResult<IImage>(new Bitmap(uri.LocalPath)),
        };
    }
}
