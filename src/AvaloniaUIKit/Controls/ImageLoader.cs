using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvaloniaUIKit;

/// <summary>Loads the images <see cref="AsyncImage"/>s show.</summary>
public interface IImageLoader
{
    /// <summary>Loads and decodes the image at <paramref name="source"/>; a failed load faults the task.</summary>
    Task<IImage> LoadAsync(Uri source, CancellationToken cancellationToken);
}

/// <summary>
/// The loader <see cref="AsyncImage"/> uses by default, as GPUI's img() loads
/// a resource (gpui elements/img.rs): an <c>http</c> or <c>https</c> URI through
/// an <see cref="HttpClient"/>, a <c>file</c> URI from the file system and an
/// <c>avares</c> URI from the app's assets, decoded off the UI thread. Like
/// GPUI's asset cache, it keeps each source's image for every image that
/// shows it, until <see cref="Remove"/>; a failed load is not kept, so the
/// next image of that source tries again.
/// </summary>
public class ImageLoader : IImageLoader
{
    private static readonly Lazy<HttpClient> s_http = new(() => new HttpClient());

    private readonly HttpClient? _http;
    private readonly ConcurrentDictionary<Uri, Task<IImage>> _cache = new();

    /// <summary>Creates a loader with a shared <see cref="HttpClient"/>.</summary>
    public ImageLoader()
    {
    }

    /// <summary>Creates a loader that fetches http and https URIs with <paramref name="http"/>.</summary>
    public ImageLoader(HttpClient http) => _http = http;

    /// <summary>The loader of every <see cref="AsyncImage"/> without its own.</summary>
    public static ImageLoader Default { get; } = new();

    /// <inheritdoc />
    public Task<IImage> LoadAsync(Uri source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        var task = _cache.GetOrAdd(source, LoadAndForgetFailure);
        if (task.IsFaulted)
        {
            // It failed before it was added, so its continuation found no entry to drop.
            _cache.TryRemove(new KeyValuePair<Uri, Task<IImage>>(source, task));
        }
        return task.WaitAsync(cancellationToken);
    }

    /// <summary>Forgets the image of <paramref name="source"/>, so the next load fetches it again.</summary>
    public void Remove(Uri source) => _cache.TryRemove(source, out _);

    /// <summary>Forgets every image.</summary>
    public void Clear() => _cache.Clear();

    /// <summary>Fetches the bytes of <paramref name="source"/>.</summary>
    protected virtual async Task<Stream> OpenAsync(Uri source)
    {
        if (!source.IsAbsoluteUri)
        {
            throw new NotSupportedException($"The image source {source} is not an absolute URI.");
        }
        switch (source.Scheme)
        {
            case "http":
            case "https":
                return new MemoryStream(await (_http ?? s_http.Value).GetByteArrayAsync(source).ConfigureAwait(false));
            case "file":
                return new MemoryStream(await File.ReadAllBytesAsync(source.LocalPath).ConfigureAwait(false));
            case "avares":
                return AssetLoader.Open(source);
            default:
                throw new NotSupportedException($"The image source {source} has an unsupported scheme.");
        }
    }

    private Task<IImage> LoadAndForgetFailure(Uri source)
    {
        // Off the UI thread: the fetch and the decoding.
        var task = Task.Run(() => LoadCoreAsync(source));
        // A failure is not cached: drop it once it is known (if it is still the entry).
        task.ContinueWith(
            t => _cache.TryRemove(new KeyValuePair<Uri, Task<IImage>>(source, t)),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return task;
    }

    private async Task<IImage> LoadCoreAsync(Uri source)
    {
        await using var stream = await OpenAsync(source).ConfigureAwait(false);
        return new Bitmap(stream);
    }
}
