using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaUIKit.Demos;

namespace AvaloniaUIKit.Previews;

/// <summary>
/// Renders every demo of the documentation site, light and dark at scale 2,
/// into PNG files plus a manifest of their logical sizes. See docs/site.md.
///
///   --out &lt;dir&gt;        previews directory (sites/public/previews)
///   --only &lt;component&gt; only the demos of one component slug
///   --manifest-only    only the manifest, no PNG (the site's build needs no more)
/// </summary>
internal static class Program
{
    /// <summary>Previews render at scale 2, as the GPUI reference does.</summary>
    private const double Scale = 2;

    /// <summary>The widest a demo gets, in logical pixels: the docs column.</summary>
    private const double MaxWidth = 640;

    /// <summary>
    /// How many demos are shown at once. They settle together, so the half
    /// second of <see cref="Settle"/> is spent once per batch, not per demo.
    /// </summary>
    private const int BatchSize = 32;

    public static int Main(string[] args)
    {
        string? outDir = null, only = null;
        var manifestOnly = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out": outDir = args[++i]; break;
                case "--only": only = args[++i]; break;
                case "--manifest-only": manifestOnly = true; break;
                default:
                    Console.Error.WriteLine($"unknown argument {args[i]}");
                    return 2;
            }
        }
        if (outDir is null)
        {
            Console.Error.WriteLine("usage: --out <dir> [--only <component>] [--manifest-only]");
            return 2;
        }
        using var session = HeadlessUnitTestSession.StartNew(typeof(PreviewApp));
        return session.Dispatch(() => Run(outDir, only, manifestOnly), CancellationToken.None).GetAwaiter().GetResult();
    }

    private static int Run(string outDir, string? only, bool manifestOnly)
    {
        var manifestPath = Path.Combine(outDir, "manifest.json");
        var manifest = only is not null && File.Exists(manifestPath)
            ? JsonSerializer.Deserialize(File.ReadAllText(manifestPath), ManifestContext.Default.SortedDictionaryStringPreviewSize) ?? new()
            : new SortedDictionary<string, PreviewSize>(StringComparer.Ordinal);
        var demos = DemoRegistry.Factories
            .Where(demo => only is null || demo.Key.StartsWith(only + "/", StringComparison.Ordinal))
            .ToList();
        var failures = 0;
        foreach (var batch in demos.Chunk(BatchSize))
            failures += RenderBatch(batch, outDir, manifestOnly, manifest);
        Directory.CreateDirectory(outDir);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, ManifestContext.Default.SortedDictionaryStringPreviewSize) + "\n");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// Shows the demos light and dark, lets them settle together, then records
    /// each one's size and captures it. Returns the number of demos that failed.
    /// </summary>
    private static int RenderBatch(
        IReadOnlyList<KeyValuePair<string, Func<Control>>> demos,
        string outDir,
        bool manifestOnly,
        SortedDictionary<string, PreviewSize> manifest)
    {
        var failures = 0;
        var shots = new List<Shot>();
        foreach (var demo in demos)
        {
            Window? light = null;
            try
            {
                light = Show(demo.Value(), dark: false);
                shots.Add(new Shot(demo, light, Show(demo.Value(), dark: true)));
            }
            catch (Exception e)
            {
                light?.Close();
                failures += Fail(demo.Key, e);
            }
        }
        if (shots.Count == 0) return failures;
        try
        {
            Settle();
        }
        catch (Exception e)
        {
            shots.ForEach(shot => shot.Close());
            if (shots.Count == 1) return failures + Fail(shots[0].Demo.Key, e);
            // One of them broke the others' settling too: render them one by one to tell which.
            return failures + shots.Sum(shot => RenderBatch([shot.Demo], outDir, manifestOnly, manifest));
        }
        foreach (var shot in shots)
        {
            var id = shot.Demo.Key;
            try
            {
                var light = Measure(shot.Light);
                var dark = Measure(shot.Dark);
                if (!manifestOnly)
                {
                    var dir = Path.Combine(outDir, id[..id.IndexOf('/')]);
                    Directory.CreateDirectory(dir);
                    var name = id[(id.IndexOf('/') + 1)..];
                    Capture(shot.Light, light, Path.Combine(dir, $"{name}.light.png"));
                    Capture(shot.Dark, dark, Path.Combine(dir, $"{name}.dark.png"));
                }
                manifest[id] = new PreviewSize(Math.Max(light.Width, dark.Width), Math.Max(light.Height, dark.Height));
                Console.WriteLine($"{id}: {light.Width}×{light.Height}");
            }
            catch (Exception e)
            {
                failures += Fail(id, e);
            }
            finally
            {
                shot.Close();
            }
        }
        return failures;
    }

    private static int Fail(string id, Exception e)
    {
        Console.Error.WriteLine($"{id}: {e.GetType().Name}: {e.Message}");
        return 1;
    }

    /// <summary>Shows a demo at its natural size, at most <see cref="MaxWidth"/> wide.</summary>
    private static Window Show(Control content, bool dark)
    {
        var root = new Border { Child = content, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        var window = new Window
        {
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = MaxWidth,
            CanResize = false,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light,
            Content = root,
        };
        Prepare(window);
        window.Show();
        return window;
    }

    /// <summary>The settled demo's laid-out size in logical pixels.</summary>
    private static PreviewSize Measure(Window window)
    {
        var root = (Control)window.Content!;
        return new PreviewSize(
            (int)Math.Max(1, Math.Ceiling(root.Bounds.Width)),
            (int)Math.Max(1, Math.Ceiling(root.Bounds.Height)));
    }

    private static void Prepare(Window window)
    {
        TextOptions.SetTextRenderingMode(window, TextRenderingMode.Antialias);
        TextOptions.SetTextHintingMode(window, TextHintingMode.None);
        window.SetRenderScaling(Scale);
    }

    /// <summary>
    /// Runs layout, timers and frames for about half a second of real time,
    /// so the transitions that start on load (a notification card's entrance,
    /// a sheet sliding in, a spring settling) have finished before the capture.
    /// </summary>
    private static void Settle()
    {
        for (var i = 0; i < 6; i++)
        {
            Tick();
            Thread.Sleep(80);
        }
        Tick();
    }

    /// <summary>Runs what is due now (layout, timers, jobs) and renders a frame.</summary>
    private static void Tick()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static void Capture(Window window, PreviewSize size, string path)
    {
        // Lock the window to the content's laid-out size, in case the platform did not follow SizeToContent.
        window.SizeToContent = SizeToContent.Manual;
        window.Width = size.Width;
        window.Height = size.Height;
        Tick();
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("no frame was rendered");
        frame.Save(path, new PngBitmapEncoderOptions());
    }

    /// <summary>A demo's light and dark windows.</summary>
    private sealed record Shot(KeyValuePair<string, Func<Control>> Demo, Window Light, Window Dark)
    {
        public void Close()
        {
            Light.Close();
            Dark.Close();
        }
    }
}

/// <summary>A demo's logical size, as the site sizes its preview and live view.</summary>
public sealed record PreviewSize(
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SortedDictionary<string, PreviewSize>))]
internal sealed partial class ManifestContext : JsonSerializerContext;
