using SkiaSharp;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Infrastructure.Imaging;

namespace StuffInABox.Infrastructure.Tests.Imaging;

public class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor _processor = new();

    private static byte[] MakePng(int w = 8, int h = 8)
    {
        using var bmp = new SKBitmap(w, h);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.CornflowerBlue);
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public void ProcessAndStripMetadata_ValidPng_ReturnsReencodedPng()
    {
        var input = MakePng();
        var result = _processor.ProcessAndStripMetadata(input);

        Assert.Equal(".png", result.Extension);
        Assert.Equal("image/png", result.ContentType);
        Assert.True(result.Bytes.Length > 0);
        // Output is still decodable
        using var decoded = SKBitmap.Decode(result.Bytes);
        Assert.NotNull(decoded);
    }

    [Fact]
    public void ProcessAndStripMetadata_GarbageBytes_Throws()
    {
        var garbage = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };
        Assert.Throws<InvalidImageException>(() => _processor.ProcessAndStripMetadata(garbage));
    }

    [Fact]
    public void ProcessAndStripMetadata_PdfMagicBytes_Throws()
    {
        // "%PDF" — a non-image file masquerading
        var pdf = "%PDF-1.4 fake"u8.ToArray();
        Assert.Throws<InvalidImageException>(() => _processor.ProcessAndStripMetadata(pdf));
    }
}
