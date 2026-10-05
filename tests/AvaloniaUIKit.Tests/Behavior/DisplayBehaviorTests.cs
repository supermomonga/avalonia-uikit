using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What uikit:TextLabel, uikit:AsyncImage, uikit:Icon and ItemsScrolling do
/// beyond their look: GPUI's highlight_ranges and mask (label.rs), img()'s
/// loading delay and fallback (gpui elements/img.rs), the IconName catalog,
/// and scroll_to_item on a ListBox (base/virtual_list.rs).
/// </summary>
public class DisplayBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    private static List<(int, int)> Ranges(string text, string? highlights, HighlightsMatch match = HighlightsMatch.Full) =>
        TextLabel.HighlightRanges(text, highlights, match);

    // label.rs test_highlight_ranges: the matches without the secondary text's
    // two ranges, as UTF-16 (start, length) instead of UTF-8 ranges.
    [Test]
    public async Task Highlight_ranges_match_every_occurrence_case_insensitively()
    {
        await Assert.That(Ranges("Hello World", null)).IsEmpty();
        await Assert.That(Ranges("Hello World", "WORLD")).IsEquivalentTo([(6, 5)]);
        await Assert.That(Ranges("Hello Hello Hello", "Hello")).IsEquivalentTo([(0, 5), (6, 5), (12, 5)]);
        await Assert.That(Ranges("Hello World", "xyz")).IsEmpty();
        await Assert.That(Ranges("Hello World", "")).IsEmpty();
        // Label "Hello", secondary "World": the full text is "Hello World".
        await Assert.That(Ranges("Hello World", "llo")).IsEquivalentTo([(2, 3)]);
        await Assert.That(Ranges("Hello World", "World")).IsEquivalentTo([(6, 5)]);
        await Assert.That(Ranges("Hello World", "o W")).IsEquivalentTo([(4, 3)]);
        // Overlapping matches: each search starts a character after the last match.
        await Assert.That(Ranges("aaaa", "aa")).IsEquivalentTo([(0, 2), (1, 2), (2, 2)]);
        var text = "你好世界，Hello World";
        await Assert.That(Ranges(text, "世界")).IsEquivalentTo([(text.IndexOf("世界", StringComparison.Ordinal), 2)]);
        // The mixed ASCII/CJK case of GPUI's story.
        await Assert.That(Ranges("AAA中文BB", "a中")).IsEquivalentTo([(2, 2)]);
    }

    // label.rs test_highlight_ranges_prefix.
    [Test]
    public async Task Prefix_highlights_only_a_match_at_the_start()
    {
        await Assert.That(Ranges("aaaa", "aa", HighlightsMatch.Prefix)).IsEquivalentTo([(0, 2)]);
        await Assert.That(Ranges("Hello Hello", "Hello", HighlightsMatch.Full).Count).IsEqualTo(2);
        await Assert.That(Ranges("Hello Hello", "Hello", HighlightsMatch.Prefix)).IsEquivalentTo([(0, 5)]);
        await Assert.That(Ranges("Hello hello HELLO", "hello", HighlightsMatch.Prefix)).IsEquivalentTo([(0, 5)]);
        await Assert.That(Ranges("Hello World", "xyz", HighlightsMatch.Prefix)).IsEmpty();
        await Assert.That(Ranges("Hello World", "", HighlightsMatch.Prefix)).IsEmpty();
        await Assert.That(Ranges("Hello Hello World", "Hello", HighlightsMatch.Prefix)).IsEquivalentTo([(0, 5)]);
        await Assert.That(Ranges("abc def abc def", "abc", HighlightsMatch.Prefix)).IsEquivalentTo([(0, 3)]);
        await Assert.That(Ranges("你好世界你好", "你好", HighlightsMatch.Prefix)).IsEquivalentTo([(0, 2)]);
        await Assert.That(Ranges("abababab", "abab", HighlightsMatch.Prefix)).IsEquivalentTo([(0, 4)]);
        await Assert.That(Ranges("xyz Hello abc Hello", "Hello", HighlightsMatch.Prefix)).IsEmpty();
    }

    /// <summary>The label's runs as "text:class" ("" for none).</summary>
    private static string Runs(TextLabel label) =>
        string.Join("|", label.Inlines!.OfType<Run>().Select(r => $"{r.Text}:{string.Join(" ", r.Classes)}"));

    // label.rs: the secondary text after a space, muted; the matches blue; the
    // runs follow every change, and Text keeps the label's own text.
    [Test]
    public async Task A_label_builds_its_runs_from_the_text_the_secondary_text_and_the_matches()
    {
        var label = new TextLabel { Text = "Company address" };
        await Assert.That(label.Inlines!.Count).IsEqualTo(0);
        label.Secondary = "(optional)";
        await Assert.That(Runs(label)).IsEqualTo("Company address:| (optional):secondary");
        label.Highlights = "ADD";
        await Assert.That(Runs(label)).IsEqualTo("Company :|add:highlight|ress:| (optional):secondary");
        label.HighlightsMatch = HighlightsMatch.Prefix;
        await Assert.That(Runs(label)).IsEqualTo("Company address:| (optional):secondary");
        label.Highlights = "comp";
        label.Text = "Compact";
        await Assert.That(Runs(label)).IsEqualTo("Comp:highlight|act:| (optional):secondary");
        await Assert.That(label.Text).IsEqualTo("Compact");
        label.Secondary = null;
        label.Highlights = null;
        await Assert.That(label.Inlines!.Count).IsEqualTo(0);
        await Assert.That(label.Text).IsEqualTo("Compact");
    }

    // label.rs: masked shows a • per character of the label and its secondary
    // text, with neither the secondary color nor the matches (GPUI's
    // masked_secondary_text_and_highlights_render).
    [Test]
    public async Task A_masked_label_shows_a_bullet_per_character_unstyled()
    {
        var label = new TextLabel { Text = "é🙂", Secondary = "世界", Highlights = "🙂 世", IsMasked = true };
        await Assert.That(Runs(label)).IsEqualTo("•••••:");
        label.IsMasked = false;
        await Assert.That(label.Inlines!.OfType<Run>().Any(r => r.Classes.Contains("highlight"))).IsTrue();
    }

    // label.rs: Label is the foreground on a 20px line, as TextBlock.label; the
    // secondary run is muted and a match is blue.
    [Test]
    public async Task A_label_has_the_label_look_and_colors_its_runs()
    {
        var golden = Case("uikit-label/secondary-highlight.company/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var label = (TextLabel)host.Control;
        await Assert.That(label.LineHeight).IsEqualTo(20);
        await Assert.That(label.TextWrapping).IsEqualTo(TextWrapping.Wrap);
        Color ColorOf(string kind) => ((ISolidColorBrush)label.Inlines!.OfType<Run>().First(r => r.Classes.Contains(kind)).Foreground!).Color;
        await Assert.That(ColorOf("secondary")).IsEqualTo(((ISolidColorBrush)Application.Current!.FindResource(golden.Variant, "UIKit.MutedForeground")!).Color);
        await Assert.That(ColorOf("highlight")).IsEqualTo(((ISolidColorBrush)Application.Current!.FindResource(golden.Variant, "UIKit.Blue")!).Color);
    }

    /// <summary>A loader whose loads the test finishes: each source's task waits for the test.</summary>
    private sealed class ManualLoader : IImageLoader
    {
        public Dictionary<Uri, TaskCompletionSource<IImage>> Loads { get; } = [];

        public Task<IImage> LoadAsync(Uri source, CancellationToken cancellationToken)
        {
            var load = new TaskCompletionSource<IImage>();
            Loads[source] = load;
            return load.Task;
        }
    }

    private static readonly Uri Wide = new(Path.Combine(Repo.Root, "assets", "images", "wide-192x96.png"));

    private static (CaseHost Host, AsyncImage Image, ManualLoader Loader) OpenImage(Uri source)
    {
        var golden = Case("uikit-image/loading.base/normal/light");
        var loader = new ManualLoader();
        var image = new AsyncImage
        {
            Width = 96,
            Height = 96,
            Loader = loader,
            Loading = new TextBlock { Text = "Loading" },
            Fallback = new TextBlock { Text = "Unavailable" },
            Source = source,
        };
        return (CaseHost.Open(golden, image), image, loader);
    }

    private static bool Shows(AsyncImage image, string text) =>
        image.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == text && t.IsEffectivelyVisible);

    private static Image Drawn(AsyncImage image) => image.GetVisualDescendants().OfType<Image>().Single();

    // img.rs: nothing while the load is pending; the loading content only once it
    // has run 200ms (LOADING_DELAY); the image when it arrives.
    [Test]
    public async Task The_loading_content_shows_after_200ms_until_the_image_arrives()
    {
        var (host, image, loader) = OpenImage(Wide);
        using var _ = host;
        host.Drive(Case("uikit-image/loading.base/normal/light"), "wait-199ms");
        await Assert.That(Shows(image, "Loading")).IsFalse();
        await Assert.That(image.Classes.Contains(":loading")).IsFalse();
        host.Drive(Case("uikit-image/loading.base/normal/light"), "wait-1ms");
        await Assert.That(Shows(image, "Loading")).IsTrue();
        loader.Loads[Wide].SetResult(new Bitmap(Wide.LocalPath));
        host.Flush();
        await Assert.That(Shows(image, "Loading")).IsFalse();
        await Assert.That(image.Classes.Contains(":loaded")).IsTrue();
        await Assert.That(Drawn(image).Source).IsNotNull();
        // Contain: the wide image is 96x48 in the 96px box.
        await Assert.That(Drawn(image).Bounds.Size).IsEqualTo(new Size(96, 48));
    }

    // img.rs: a load that finishes within 200ms never flashes the loading content.
    [Test]
    public async Task A_fast_load_never_shows_the_loading_content()
    {
        var (host, image, loader) = OpenImage(Wide);
        using var _ = host;
        var golden = Case("uikit-image/loading.base/normal/light");
        host.Drive(golden, "wait-150ms");
        loader.Loads[Wide].SetResult(new Bitmap(Wide.LocalPath));
        host.Flush();
        host.Drive(golden, "wait-300ms");
        await Assert.That(image.Classes.Contains(":loading")).IsFalse();
        await Assert.That(image.Classes.Contains(":loaded")).IsTrue();
    }

    // img.rs: a failed load shows the fallback content and is not retried; a
    // new source loads again, and a stale load's result is ignored.
    [Test]
    public async Task A_failed_load_shows_the_fallback_and_a_new_source_starts_over()
    {
        var (host, image, loader) = OpenImage(Wide);
        using var _ = host;
        loader.Loads[Wide].SetException(new IOException("missing"));
        host.Flush();
        await Assert.That(Shows(image, "Unavailable")).IsTrue();
        await Assert.That(image.Classes.Contains(":failed")).IsTrue();
        var small = new Uri(Path.Combine(Repo.Root, "assets", "images", "small-48x48.png"));
        image.Source = small;
        await Assert.That(Shows(image, "Unavailable")).IsFalse();
        var stale = loader.Loads[small];
        image.Source = Wide;
        stale.SetResult(new Bitmap(small.LocalPath));
        host.Flush();
        await Assert.That(image.Classes.Contains(":loaded")).IsFalse();
        loader.Loads[Wide].SetResult(new Bitmap(Wide.LocalPath));
        host.Flush();
        await Assert.That(image.Classes.Contains(":loaded")).IsTrue();
        await Assert.That(Drawn(image).Bounds.Size).IsEqualTo(new Size(96, 48));
    }

    private static async Task<bool> WaitFor(CaseHost host, Func<bool> done)
    {
        for (var i = 0; i < 500 && !done(); i++)
        {
            await Task.Delay(10);
            host.Flush();
        }
        return done();
    }

    // The default loader: a file URI is read and decoded off the UI thread; a
    // missing file and an unsupported scheme fail; a source's image is kept and
    // shared, a failure is not.
    [Test]
    public async Task The_default_loader_reads_files_and_keeps_each_source()
    {
        var loader = new ImageLoader();
        var image = await loader.LoadAsync(Wide, CancellationToken.None);
        await Assert.That(image.Size).IsEqualTo(new Size(192, 96));
        await Assert.That(await loader.LoadAsync(Wide, CancellationToken.None)).IsSameReferenceAs(image);
        loader.Remove(Wide);
        await Assert.That(await loader.LoadAsync(Wide, CancellationToken.None)).IsNotSameReferenceAs(image);

        var missing = new Uri(Path.Combine(Repo.Root, "assets", "images", "missing.png"));
        await Assert.That(async () => await loader.LoadAsync(missing, CancellationToken.None)).Throws<Exception>();
        await Assert.That(async () => await loader.LoadAsync(new Uri("ftp://example.com/a.png"), CancellationToken.None)).Throws<NotSupportedException>();
        // The failure was dropped: the next load tries again (and fails again).
        await Assert.That(async () => await loader.LoadAsync(missing, CancellationToken.None)).Throws<Exception>();
    }

    [Test]
    public async Task An_image_with_the_default_loader_shows_a_file_or_its_fallback()
    {
        var golden = Case("uikit-image/loading.base/normal/light");
        var image = new AsyncImage { Width = 96, Height = 96, Source = Wide, Fallback = new TextBlock { Text = "Unavailable" } };
        using var host = CaseHost.Open(golden, image);
        await Assert.That(await WaitFor(host, () => image.Classes.Contains(":loaded"))).IsTrue();
        await Assert.That(Drawn(image).Bounds.Size).IsEqualTo(new Size(96, 48));
        image.Source = new Uri("file:///nonexistent/missing.png");
        await Assert.That(await WaitFor(host, () => image.Classes.Contains(":failed"))).IsTrue();
        await Assert.That(Shows(image, "Unavailable")).IsTrue();
    }

    // The IconName catalog: every variant names a geometry in the theme.
    [Test]
    public async Task Every_icon_name_has_its_geometry_in_the_theme()
    {
        var names = Enum.GetValues<IconName>();
        await Assert.That(names.Length).IsEqualTo(106);
        foreach (var name in names)
        {
            await Assert.That(Application.Current!.FindResource(name.ResourceKey())).IsTypeOf<StreamGeometry>();
        }
        await Assert.That(IconName.SortAscending.FaintOpacity()).IsEqualTo(0.2);
        await Assert.That(Application.Current!.FindResource(IconName.SortAscending.ResourceKey() + ".Faint")).IsNotNull();
        await Assert.That(IconName.Search.FaintOpacity()).IsEqualTo(0);
    }

    // icon.rs: an Icon is styled as a PathIcon, so a button's size reaches it;
    // its Kind sets the geometry and a cleared Kind leaves Data to the app.
    [Test]
    public async Task An_icon_takes_its_geometry_from_kind_and_the_sizes_of_a_path_icon()
    {
        var golden = Case("uikit-icon/inherit.base/normal/light");
        var icon = new AvaloniaUIKit.Icon { Kind = IconName.Search };
        using var host = CaseHost.Open(golden, new Button { Classes = { "small" }, Content = icon });
        await Assert.That(icon.Data).IsSameReferenceAs(Application.Current!.FindResource("UIKit.Icon.Search"));
        await Assert.That(icon.Bounds.Size).IsEqualTo(new Size(14, 14));
        icon.Kind = IconName.Copy;
        await Assert.That(icon.Data).IsSameReferenceAs(Application.Current!.FindResource("UIKit.Icon.Copy"));
        icon.Kind = null;
        await Assert.That(icon.Data).IsNull();
    }

    /// <summary>The ink of a window area: how far its pixels are from the background, summed.</summary>
    private static long Ink(RgbaImage frame, Rect area)
    {
        var background = frame.Pixel(0, 0).ToArray();
        long sum = 0;
        for (var y = (int)(area.Y * CaseHost.Scale); y < (int)(area.Bottom * CaseHost.Scale); y++)
        {
            for (var x = (int)(area.X * CaseHost.Scale); x < (int)(area.Right * CaseHost.Scale); x++)
            {
                var p = frame.Pixel(x, y);
                sum += Math.Abs(p[0] - background[0]) + Math.Abs(p[1] - background[1]) + Math.Abs(p[2] - background[2]);
            }
        }
        return sum;
    }

    // sort-ascending.svg: the faint arrow is painted under the icon, as GPUI's SVG draws it.
    [Test]
    public async Task A_two_tone_icon_paints_its_faint_part()
    {
        var golden = Case("uikit-icon/faint.sort-ascending/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var icon = (AvaloniaUIKit.Icon)host.Control;
        var bounds = host.ControlBounds();
        var withFaint = Ink(host.Capture(), bounds);
        var plain = new PathIcon { Data = icon.Data, Classes = { "large" } };
        using var plainHost = CaseHost.Open(golden, plain);
        var withoutFaint = Ink(plainHost.Capture(), plainHost.ControlBounds());
        await Assert.That(withFaint).IsGreaterThan(withoutFaint + withoutFaint / 10);
    }

    /// <summary>A ListBox of `count` rows 34px tall, 200px high (the virtual list cases' rows).</summary>
    private static (CaseHost Host, ListBox List) OpenList(int count = 1000, ITemplate<Panel?>? panel = null)
    {
        var golden = Case("virtual/uniform.base/normal/light");
        var list = new ListBox
        {
            Width = 240,
            Height = 200,
            ItemsSource = Enumerable.Range(0, count).ToList(),
            ItemTemplate = new FuncDataTemplate<int>((i, _) => new Border { Height = 34, Child = new TextBlock { Text = $"Row {i}" } }),
        };
        if (panel is not null)
        {
            list.ItemsPanel = panel;
        }
        return (CaseHost.Open(golden, list), list);
    }

    /// <summary>Where row <paramref name="index"/> is in the list: its top and bottom.</summary>
    private static (double Top, double Bottom) RowIn(ListBox list, int index)
    {
        var row = list.ContainerFromIndex(index)!;
        var top = row.TranslatePoint(default, list)!.Value.Y;
        return (top, top + row.Bounds.Height);
    }

    // virtual_list.rs: Center puts the row in the middle of the viewport, even a
    // row not realized yet; near the end it stops at the end.
    [Test]
    public async Task Scrolling_to_an_item_centers_it()
    {
        var (host, list) = OpenList();
        using var _ = host;
        list.ScrollToItem(500, ScrollStrategy.Center);
        host.Flush();
        var (top, bottom) = RowIn(list, 500);
        await Assert.That(Math.Abs((top + bottom) / 2 - list.Bounds.Height / 2)).IsLessThan(0.51);
        list.ScrollToItem(999, ScrollStrategy.Center);
        host.Flush();
        await Assert.That(Math.Abs(RowIn(list, 999).Bottom - list.Bounds.Height)).IsLessThan(0.51);
    }

    // virtual_list.rs: any other strategy moves a row out of view to the nearer
    // edge and leaves a row in view where it is.
    [Test]
    public async Task Scrolling_to_an_item_out_of_view_takes_the_nearer_edge()
    {
        var (host, list) = OpenList();
        using var _ = host;
        list.ScrollToItem(500, ScrollStrategy.Top);
        host.Flush();
        await Assert.That(Math.Abs(RowIn(list, 500).Bottom - list.Bounds.Height)).IsLessThan(0.51);
        list.ScrollToItem(498, ScrollStrategy.Bottom);
        host.Flush();
        await Assert.That(Math.Abs(RowIn(list, 500).Bottom - list.Bounds.Height)).IsLessThan(0.51);
        list.ScrollToItem(100, ScrollStrategy.Bottom);
        host.Flush();
        await Assert.That(Math.Abs(RowIn(list, 100).Top)).IsLessThan(0.51);
    }

    // A scroll asked for before the first layout waits for it, on a list that
    // does not virtualize as on one that does.
    [Test]
    public async Task A_scroll_before_the_first_layout_waits_for_it()
    {
        var golden = Case("virtual/uniform.base/normal/light");
        var list = new ListBox
        {
            Width = 240,
            Height = 200,
            ItemsSource = Enumerable.Range(0, 100).ToList(),
            ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel()),
            ItemTemplate = new FuncDataTemplate<int>((i, _) => new Border { Height = 34, Child = new TextBlock { Text = $"Row {i}" } }),
        };
        list.ScrollToItem(50, ScrollStrategy.Center);
        using var host = CaseHost.Open(golden, list);
        host.Flush();
        var (top, bottom) = RowIn(list, 50);
        await Assert.That(Math.Abs((top + bottom) / 2 - list.Bounds.Height / 2)).IsLessThan(0.51);
    }
}
