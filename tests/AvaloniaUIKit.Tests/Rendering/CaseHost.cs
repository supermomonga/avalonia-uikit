using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// A headless window laid out like the GPUI reference window: the viewport
/// size, the theme background, and the control at the anchor.
/// </summary>
public sealed class CaseHost : IDisposable
{
    /// <summary>The GPUI headless window renders at scale 2.</summary>
    public const double Scale = 2;

    private CaseHost(Window window, Control control)
    {
        Window = window;
        Control = control;
    }

    public Window Window { get; }
    public Control Control { get; }

    public static CaseHost Open(GoldenCase golden, Control control)
    {
        var canvas = new Canvas();
        Canvas.SetLeft(control, golden.Anchor.X);
        Canvas.SetTop(control, golden.Anchor.Y);
        canvas.Children.Add(control);
        var window = new Window
        {
            Width = golden.Viewport.Width,
            Height = golden.Viewport.Height,
            SizeToContent = SizeToContent.Manual,
            RequestedThemeVariant = golden.Variant,
            Content = canvas,
        };
        TextOptions.SetTextRenderingMode(window, TextRenderingMode.Antialias);
        TextOptions.SetTextHintingMode(window, TextHintingMode.None);
        VirtualTime.Attach(window);
        // Scaled from the first layout on, as GPUI lays a window out once at its scale.
        window.SetRenderScaling(Scale);
        window.Show();
        var host = new CaseHost(window, control);
        host.Flush();
        return host;
    }

    /// <summary>The template part named <paramref name="name"/>, anywhere in the window.</summary>
    public T Part<T>(string name) where T : Control =>
        Window.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    /// <summary>Runs what is due now (timers, jobs, motions at their current moment) and renders a frame.</summary>
    public void Flush()
    {
        VirtualTime.Tick();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The control's bounds in window coordinates.</summary>
    public Rect ControlBounds()
    {
        Flush();
        // The laid-out box: the control's own RenderTransform (a rotated icon) does not move it.
        var parent = Control.GetVisualParent() ?? Window;
        var origin = parent.TranslatePoint(Control.Bounds.Position, Window) ?? default;
        return new Rect(origin, Control.Bounds.Size);
    }

    public Point PointAt(double fx, double fy)
    {
        var b = ControlBounds();
        return new Point(b.X + b.Width * fx, b.Y + b.Height * fy);
    }

    /// <summary>Puts the control into a state named like the GPUI case states.</summary>
    public void Drive(GoldenCase golden, string state)
    {
        var at = PointAt(golden.Num("pointer_x", 0.5), golden.Num("pointer_y", 0.5));
        // Where the pointer last went, for "release".
        var last = at;
        foreach (var part in state.Split('+'))
        {
            switch (part)
            {
                case "normal":
                case "disabled":
                // GPUI's harness activates its window so it paints carets and selections; Avalonia's is active.
                case "activate":
                    break;
                case var w when w.StartsWith("wait-", StringComparison.Ordinal):
                    var ms = int.Parse(w["wait-".Length..].Replace("ms", "", StringComparison.Ordinal), System.Globalization.CultureInfo.InvariantCulture);
                    VirtualTime.Advance(TimeSpan.FromMilliseconds(ms));
                    break;
                case "hover":
                    Window.MouseMove(at);
                    break;
                case "pressed":
                    Window.MouseMove(at);
                    Window.MouseDown(at, MouseButton.Left);
                    last = at;
                    break;
                case "release":
                    Window.MouseUp(last, MouseButton.Left);
                    break;
                case var p when p.StartsWith("drag-at-", StringComparison.Ordinal):
                    // Moves the pointer with the left button held.
                    var dxy = p["drag-at-".Length..].Split('-');
                    last = new Point(double.Parse(dxy[0], System.Globalization.CultureInfo.InvariantCulture), double.Parse(dxy[1], System.Globalization.CultureInfo.InvariantCulture));
                    Window.MouseMove(last, RawInputModifiers.LeftMouseButton);
                    break;
                case "focus":
                    Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
                    Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
                    break;
                case "click":
                    Window.MouseMove(at);
                    Window.MouseDown(at, MouseButton.Left);
                    Window.MouseUp(at, MouseButton.Left);
                    break;
                case "right-click":
                    Window.MouseMove(at);
                    Window.MouseDown(at, MouseButton.Right);
                    Window.MouseUp(at, MouseButton.Right);
                    break;
                case "leave":
                    var b = ControlBounds();
                    Window.MouseMove(new Point(b.Right + 40, b.Bottom + 40));
                    break;
                case var p when p.StartsWith("wheel-at-", StringComparison.Ordinal):
                    // Scrolls the content under the point down by 40px (50px per wheel step).
                    var wxy = p["wheel-at-".Length..].Split('-');
                    var wheelAt = new Point(double.Parse(wxy[0], System.Globalization.CultureInfo.InvariantCulture), double.Parse(wxy[1], System.Globalization.CultureInfo.InvariantCulture));
                    Window.MouseMove(wheelAt);
                    Window.MouseWheel(wheelAt, new Vector(0, -0.8));
                    break;
                case var p when p.StartsWith("pressed-at-", StringComparison.Ordinal):
                    var pxy = p["pressed-at-".Length..].Split('-');
                    var pressAt = new Point(double.Parse(pxy[0], System.Globalization.CultureInfo.InvariantCulture), double.Parse(pxy[1], System.Globalization.CultureInfo.InvariantCulture));
                    Window.MouseMove(pressAt);
                    Window.MouseDown(pressAt, MouseButton.Left);
                    last = pressAt;
                    break;
                case var p when p.StartsWith("at-", StringComparison.Ordinal) || p.StartsWith("click-at-", StringComparison.Ordinal) || p.StartsWith("right-click-at-", StringComparison.Ordinal):
                    var split = p.IndexOf("at-", StringComparison.Ordinal);
                    var kind = p[..split];
                    var xy = p[(split + 3)..].Split('-');
                    var point = new Point(double.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture), double.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture));
                    Window.MouseMove(point);
                    last = point;
                    if (kind is "click-" or "right-click-")
                    {
                        var button = kind == "click-" ? MouseButton.Left : MouseButton.Right;
                        Window.MouseDown(point, button);
                        Window.MouseUp(point, button);
                    }
                    break;
                case var k when k.StartsWith("key-", StringComparison.Ordinal):
                    PressKey(k["key-".Length..]);
                    break;
                default:
                    if (!DriveExtra(part, at))
                    {
                        throw new NotSupportedException($"state {part} in {golden.Id}");
                    }
                    break;
            }
            Flush();
        }
    }

    /// <summary>
    /// Presses a key named as GPUI's Keystroke::parse names it: modifiers joined
    /// with '-' before the key ("down", "enter", "shift-tab", "a").
    /// </summary>
    public void PressKey(string keystroke)
    {
        var parts = keystroke.Split('-');
        var modifiers = RawInputModifiers.None;
        foreach (var m in parts[..^1])
        {
            modifiers |= m switch
            {
                "shift" => RawInputModifiers.Shift,
                "ctrl" => RawInputModifiers.Control,
                "alt" => RawInputModifiers.Alt,
                "cmd" => RawInputModifiers.Meta,
                _ => throw new NotSupportedException($"modifier {m}"),
            };
        }
        var name = parts[^1];
        var key = name switch
        {
            "up" => PhysicalKey.ArrowUp,
            "down" => PhysicalKey.ArrowDown,
            "left" => PhysicalKey.ArrowLeft,
            "right" => PhysicalKey.ArrowRight,
            "enter" => PhysicalKey.Enter,
            "escape" => PhysicalKey.Escape,
            "tab" => PhysicalKey.Tab,
            "space" => PhysicalKey.Space,
            "home" => PhysicalKey.Home,
            "end" => PhysicalKey.End,
            "backspace" => PhysicalKey.Backspace,
            "delete" => PhysicalKey.Delete,
            "pageup" => PhysicalKey.PageUp,
            "pagedown" => PhysicalKey.PageDown,
            { Length: 1 } c when char.IsAsciiLetter(c[0]) => Enum.Parse<PhysicalKey>("Key" + char.ToUpperInvariant(c[0])),
            { Length: 1 } c when char.IsAsciiDigit(c[0]) => Enum.Parse<PhysicalKey>("Digit" + c),
            _ => throw new NotSupportedException($"key {name}"),
        };
        Window.KeyPressQwerty(key, modifiers);
        Window.KeyReleaseQwerty(key, modifiers);
    }

    /// <summary>Component-specific states, registered by the component's case adapter.</summary>
    public Func<string, Point, bool>? Extra { get; set; }

    private bool DriveExtra(string part, Point at) => Extra?.Invoke(part, at) ?? false;

    public RgbaImage Capture()
    {
        Flush();
        using var frame = Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("no frame was rendered");
        return RgbaImage.FromFrame(frame);
    }

    /// <summary>The whole tree rendered from scratch by the immediate renderer (popups excluded).</summary>
    public RgbaImage CaptureImmediate()
    {
        Flush();
        var size = new PixelSize((int)Math.Round(Window.Bounds.Width * Scale), (int)Math.Round(Window.Bounds.Height * Scale));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96 * Scale, 96 * Scale));
        bitmap.Render(Window);
        using var frame = new WriteableBitmap(size, new Vector(96 * Scale, 96 * Scale), Avalonia.Platform.PixelFormat.Rgba8888, Avalonia.Platform.AlphaFormat.Premul);
        using (var buffer = frame.Lock())
        {
            bitmap.CopyPixels(buffer);
        }
        return RgbaImage.FromFrame(frame);
    }



    public void Dispose() => Window.Close();
}
