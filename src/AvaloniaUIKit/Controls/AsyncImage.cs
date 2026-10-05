using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI's img() of a resource (gpui elements/img.rs; GPUI Kit's Image page):
/// an image loaded from <see cref="Source"/> (http, https, file or avares)
/// by the <see cref="Loader"/> while the control lays out from its own size
/// and draws nothing. When the load is still pending 200ms after it started,
/// the <see cref="Loading"/> content shows (img.rs LOADING_DELAY), so a fast
/// load never flashes it; when the load fails, the <see cref="Fallback"/>
/// content does. Neither is retried. The image is drawn as an
/// <see cref="Image"/>: <see cref="Stretch"/> and <see cref="StretchDirection"/>
/// give GPUI's ObjectFit, and the classes rounded, rounded-lg and rounded-full
/// round the drawn image. Pseudo-classes: :loading (the loading content is
/// shown), :loaded and :failed.
/// </summary>
[TemplatePart("PART_Image", typeof(Image))]
[PseudoClasses(":loading", ":loaded", ":failed")]
public class AsyncImage : TemplatedControl
{
    /// <summary>The image's URI: http, https, file or avares.</summary>
    public static readonly StyledProperty<Uri?> SourceProperty =
        AvaloniaProperty.Register<AsyncImage, Uri?>(nameof(Source));

    /// <summary>The loader of the image (by default <see cref="ImageLoader.Default"/>).</summary>
    public static readonly StyledProperty<IImageLoader?> LoaderProperty =
        AvaloniaProperty.Register<AsyncImage, IImageLoader?>(nameof(Loader));

    /// <summary>How the image fills the control (GPUI's ObjectFit; Uniform is Contain).</summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        Image.StretchProperty.AddOwner<AsyncImage>();

    /// <summary>Whether the image may grow, shrink or both (DownOnly with Uniform is ScaleDown).</summary>
    public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
        Image.StretchDirectionProperty.AddOwner<AsyncImage>();

    /// <summary>The content shown while the image loads, from 200ms after the load started.</summary>
    public static readonly StyledProperty<object?> LoadingProperty =
        AvaloniaProperty.Register<AsyncImage, object?>(nameof(Loading));

    /// <summary>The content shown when the image fails to load.</summary>
    public static readonly StyledProperty<object?> FallbackProperty =
        AvaloniaProperty.Register<AsyncImage, object?>(nameof(Fallback));

    /// <summary>img.rs LOADING_DELAY: how long a load runs before the loading content shows.</summary>
    internal static readonly TimeSpan LoadingDelay = TimeSpan.FromMilliseconds(200);

    private Image? _image;
    private IImage? _loaded;
    private int _version;
    private CancellationTokenSource? _cancel;
    private IDisposable? _loadingTimer;
    private bool _pending;
    private bool _attached;

    static AsyncImage()
    {
        SourceProperty.Changed.AddClassHandler<AsyncImage>((image, _) => image.Restart());
        LoaderProperty.Changed.AddClassHandler<AsyncImage>((image, _) => image.Restart());
    }

    /// <inheritdoc cref="SourceProperty"/>
    public Uri? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <inheritdoc cref="LoaderProperty"/>
    public IImageLoader? Loader
    {
        get => GetValue(LoaderProperty);
        set => SetValue(LoaderProperty, value);
    }

    /// <inheritdoc cref="StretchProperty"/>
    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <inheritdoc cref="StretchDirectionProperty"/>
    public StretchDirection StretchDirection
    {
        get => GetValue(StretchDirectionProperty);
        set => SetValue(StretchDirectionProperty, value);
    }

    /// <inheritdoc cref="LoadingProperty"/>
    public object? Loading
    {
        get => GetValue(LoadingProperty);
        set => SetValue(LoadingProperty, value);
    }

    /// <inheritdoc cref="FallbackProperty"/>
    public object? Fallback
    {
        get => GetValue(FallbackProperty);
        set => SetValue(FallbackProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _image = e.NameScope.Find<Image>("PART_Image");
        if (_image is not null)
        {
            _image.Source = _loaded;
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        // GPUI starts loading when the image is first laid out.
        if (_loaded is null && !_pending && !PseudoClasses.Contains(":failed"))
        {
            Load();
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        // A pending load starts again when the image is shown again.
        if (_pending)
        {
            Cancel();
            PseudoClasses.Set(":loading", false);
        }
    }

    private void Restart()
    {
        Cancel();
        _loaded = null;
        if (_image is not null)
        {
            _image.Source = null;
        }
        SetState(loading: false, loaded: false, failed: false);
        if (_attached)
        {
            Load();
        }
    }

    private void Cancel()
    {
        _version++;
        _pending = false;
        _loadingTimer?.Dispose();
        _loadingTimer = null;
        _cancel?.Cancel();
        _cancel?.Dispose();
        _cancel = null;
    }

    private void Load()
    {
        if (Source is not { } source)
        {
            return;
        }
        var version = ++_version;
        _cancel = new CancellationTokenSource();
        Task<IImage> task;
        try
        {
            task = (Loader ?? ImageLoader.Default).LoadAsync(source, _cancel.Token);
        }
        catch (Exception error)
        {
            task = Task.FromException<IImage>(error);
        }
        if (task.IsCompleted)
        {
            Complete(task);
            return;
        }
        _pending = true;
        _loadingTimer = DispatcherTimer.RunOnce(() =>
        {
            if (version == _version && _pending)
            {
                SetState(loading: true, loaded: false, failed: false);
            }
        }, LoadingDelay);
        Await(task, version);
    }

    private async void Await(Task<IImage> task, int version)
    {
        try
        {
            await task;
        }
        catch
        {
            // Complete reads the failure from the task.
        }
        if (version == _version)
        {
            Complete(task);
        }
    }

    private void Complete(Task<IImage> task)
    {
        _pending = false;
        _loadingTimer?.Dispose();
        _loadingTimer = null;
        if (task.IsCompletedSuccessfully)
        {
            _loaded = task.Result;
            if (_image is not null)
            {
                _image.Source = _loaded;
            }
            SetState(loading: false, loaded: true, failed: false);
            return;
        }
        if (task.Exception is { } error)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "Could not load the image {Source}: {Error}", Source, error.GetBaseException().Message);
        }
        SetState(loading: false, loaded: false, failed: true);
    }

    private void SetState(bool loading, bool loaded, bool failed)
    {
        PseudoClasses.Set(":loading", loading);
        PseudoClasses.Set(":loaded", loaded);
        PseudoClasses.Set(":failed", failed);
    }
}
