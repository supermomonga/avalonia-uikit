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

    public static CaseHost Open(GoldenCase golden, Control control, bool freezeMotion = true)
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
            RequestedThemeVariant = golden.IsDark ? ThemeVariant.Dark : ThemeVariant.Light,
            Content = canvas,
        };
        TextOptions.SetTextRenderingMode(window, TextRenderingMode.Antialias);
        TextOptions.SetTextHintingMode(window, TextHintingMode.None);
        window.Show();
        window.SetRenderScaling(Scale);
        var host = new CaseHost(window, control);
        if (freezeMotion)
        {
            host.FreezeMotion();
        }
        host.Flush();
        return host;
    }

    /// <summary>Removes every transition so a state change shows its end state at once.</summary>
    public void FreezeMotion()
    {
        Flush();
        foreach (var visual in Window.GetSelfAndVisualDescendants().OfType<Animatable>())
        {
            visual.Transitions = null;
            if (visual is ToggleSwitch toggle)
            {
                toggle.KnobTransitions = new Transitions();
            }
        }
    }

    public void Flush()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The control's bounds in window coordinates.</summary>
    public Rect ControlBounds()
    {
        Flush();
        var origin = Control.TranslatePoint(default, Window) ?? default;
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
        foreach (var part in state.Split('+'))
        {
            switch (part)
            {
                case "normal":
                case "disabled":
                    break;
                case "hover":
                    Window.MouseMove(at);
                    break;
                case "pressed":
                    Window.MouseMove(at);
                    Window.MouseDown(at, MouseButton.Left);
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
                case var p when p.StartsWith("at-", StringComparison.Ordinal) || p.StartsWith("click-at-", StringComparison.Ordinal) || p.StartsWith("right-click-at-", StringComparison.Ordinal):
                    var split = p.IndexOf("at-", StringComparison.Ordinal);
                    var kind = p[..split];
                    var xy = p[(split + 3)..].Split('-');
                    var point = new Point(double.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture), double.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture));
                    Window.MouseMove(point);
                    if (kind is "click-" or "right-click-")
                    {
                        var button = kind == "click-" ? MouseButton.Left : MouseButton.Right;
                        Window.MouseDown(point, button);
                        Window.MouseUp(point, button);
                    }
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
