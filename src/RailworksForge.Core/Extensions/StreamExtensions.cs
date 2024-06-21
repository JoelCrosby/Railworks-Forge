using Avalonia.Media.Imaging;

namespace RailworksForge.Core.Extensions;

public static class StreamExtensions
{
    public static Bitmap? ReadBitmap(this Stream? stream)
    {
        try
        {
            return stream is null ? null : Bitmap.DecodeToWidth(stream, 256);
        }
        catch
        {
            return null;
        }
    }
}
