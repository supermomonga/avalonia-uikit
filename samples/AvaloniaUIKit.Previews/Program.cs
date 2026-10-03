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
/// into PNG files plus a manifest of their logical sizes, and the social
/// image. See docs/site.md.
///
///   --out &lt;dir&gt;        previews directory (sites/public/previews)
///   --og &lt;file&gt;        also render the 1200×630 social image
///   --only &lt;component&gt; only the demos of one component slug
/// </summary>
internal static class Program
{
    /// <summary>Previews render at scale 2, as the GPUI reference does.</summary>
    private const double Scale = 2;

    /// <summary>The widest a demo gets, in logical pixels: the docs column.</summary>
    private const double MaxWidth = 640;

    public static int Main(string[] args)
    {
        string? outDir = null, ogPath = null, only = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out": outDir = args[++i]; break;
                case "--og": ogPath = args[++i]; break;
                case "--only": only = args[++i]; break;
                default:
                    Console.Error.WriteLine($"unknown argument {args[i]}");
                    return 2;
            }
        }
        if (outDir is null && ogPath is null)
        {
            Console.Error.WriteLine("usage: --out <dir> [--og <file>] [--only <component>]");
            return 2;
        }
        using var session = HeadlessUnitTestSession.StartNew(typeof(PreviewApp));
        return session.Dispatch(() => Run(outDir, ogPath, only), CancellationToken.None).GetAwaiter().GetResult();
    }

    private static int Run(string? outDir, string? ogPath, string? only)
    {
        var failures = 0;
        if (outDir is not null)
        {
            var manifestPath = Path.Combine(outDir, "manifest.json");
            var manifest = only is not null && File.Exists(manifestPath)
                ? JsonSerializer.Deserialize(File.ReadAllText(manifestPath), ManifestContext.Default.SortedDictionaryStringPreviewSize) ?? new()
                : new SortedDictionary<string, PreviewSize>(StringComparer.Ordinal);
            foreach (var (id, factory) in DemoRegistry.Factories)
            {
                if (only is not null && !id.StartsWith(only + "/", StringComparison.Ordinal)) continue;
                try
                {
                    var dir = Path.Combine(outDir, id[..id.IndexOf('/')]);
                    Directory.CreateDirectory(dir);
                    var name = id[(id.IndexOf('/') + 1)..];
                    var light = Render(factory(), dark: false, Path.Combine(dir, $"{name}.light.png"));
                    var dark = Render(factory(), dark: true, Path.Combine(dir, $"{name}.dark.png"));
                    manifest[id] = new PreviewSize(Math.Max(light.Width, dark.Width), Math.Max(light.Height, dark.Height));
                    Console.WriteLine($"{id}: {light.Width}×{light.Height}");
                }
                catch (Exception e)
                {
                    failures++;
                    Console.Error.WriteLine($"{id}: {e.GetType().Name}: {e.Message}");
                }
            }
            Directory.CreateDirectory(outDir);
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, ManifestContext.Default.SortedDictionaryStringPreviewSize) + "\n");
        }
        if (ogPath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ogPath))!);
            RenderFixed(new OgImage(), dark: false, 1200, 630, scale: 1, ogPath);
            Console.WriteLine($"og: {ogPath}");
        }
        return failures == 0 ? 0 : 1;
    }

    /// <summary>Renders a demo at its natural size (at most <see cref="MaxWidth"/> wide) and returns that size in logical pixels.</summary>
    private static PreviewSize Render(Control content, bool dark, string path)
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
        Settle();
        // Lock the window to the content's laid-out size, in case the platform did not follow SizeToContent.
        var width = Math.Max(1, Math.Ceiling(root.Bounds.Width));
        var height = Math.Max(1, Math.Ceiling(root.Bounds.Height));
        window.SizeToContent = SizeToContent.Manual;
        window.Width = width;
        window.Height = height;
        Tick();
        Capture(window, path);
        window.Close();
        return new PreviewSize((int)width, (int)height);
    }

    private static void RenderFixed(Control content, bool dark, double width, double height, double scale, string path)
    {
        var window = new Window
        {
            Width = width,
            Height = height,
            SizeToContent = SizeToContent.Manual,
            CanResize = false,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light,
            Content = content,
        };
        Prepare(window, scale);
        window.Show();
        Settle();
        Capture(window, path);
        window.Close();
    }

    private static void Prepare(Window window, double scale = Scale)
    {
        TextOptions.SetTextRenderingMode(window, TextRenderingMode.Antialias);
        TextOptions.SetTextHintingMode(window, TextHintingMode.None);
        window.SetRenderScaling(scale);
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

    private static void Capture(Window window, string path)
    {
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("no frame was rendered");
        frame.Save(path, new PngBitmapEncoderOptions());
    }
}

/// <summary>A demo's logical size, as the site sizes its preview and live view.</summary>
public sealed record PreviewSize(
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SortedDictionary<string, PreviewSize>))]
internal sealed partial class ManifestContext : JsonSerializerContext;
