using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>Straight-alpha RGBA8 pixels, row-major without padding.</summary>
public sealed class RgbaImage
{
    public RgbaImage(int width, int height, byte[]? data = null)
    {
        Width = width;
        Height = height;
        Data = data ?? new byte[width * height * 4];
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Data { get; }

    public Span<byte> Pixel(int x, int y) => Data.AsSpan((y * Width + x) * 4, 4);

    public static RgbaImage Load(string path)
    {
        using var decoded = SKBitmap.Decode(path) ?? throw new IOException($"cannot decode {path}");
        var info = new SKImageInfo(decoded.Width, decoded.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var converted = new SKBitmap(info);
        if (!decoded.CopyTo(converted, SKColorType.Rgba8888))
        {
            using var canvas = new SKCanvas(converted);
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(decoded, 0, 0);
        }
        return new RgbaImage(info.Width, info.Height, converted.Bytes.ToArray());
    }

    /// <summary>Copies a frame captured from a headless window (opaque, so premultiplied equals straight).</summary>
    public static RgbaImage FromFrame(WriteableBitmap frame)
    {
        using var buffer = frame.Lock();
        var width = buffer.Size.Width;
        var height = buffer.Size.Height;
        var image = new RgbaImage(width, height);
        var row = new byte[buffer.RowBytes];
        for (var y = 0; y < height; y++)
        {
            Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, buffer.RowBytes);
            for (var x = 0; x < width; x++)
            {
                var src = x * 4;
                var dst = (y * width + x) * 4;
                if (buffer.Format == PixelFormat.Bgra8888)
                {
                    image.Data[dst] = row[src + 2];
                    image.Data[dst + 1] = row[src + 1];
                    image.Data[dst + 2] = row[src];
                }
                else
                {
                    image.Data[dst] = row[src];
                    image.Data[dst + 1] = row[src + 1];
                    image.Data[dst + 2] = row[src + 2];
                }
                image.Data[dst + 3] = row[src + 3];
            }
        }
        return image;
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var info = new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var bitmap = new SKBitmap(info);
        Marshal.Copy(Data, 0, bitmap.GetPixels(), Data.Length);
        using var file = File.Create(path);
        bitmap.Encode(file, SKEncodedImageFormat.Png, 100);
    }
}
