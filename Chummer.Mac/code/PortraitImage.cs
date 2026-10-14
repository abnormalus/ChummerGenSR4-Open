using System;
using System.IO;
using SkiaSharp;

namespace Chummer
{
    // Image processing stays independent of the editor and character file format.
    internal static class PortraitImage
    {
        internal static SKBitmap Decode(byte[] bytes)
        {
            using var stream = new SKMemoryStream(bytes);
            using var codec = SKCodec.Create(stream);
            if (codec == null) throw new InvalidDataException("Unsupported image");
            if ((long)codec.Info.Width * codec.Info.Height > 64000000)
                throw new InvalidDataException("Image exceeds decoding memory budget");
            using var decoded = SKBitmap.Decode(bytes);
            if (decoded == null) throw new InvalidDataException("Invalid image");
            var origin = codec.EncodedOrigin;
            bool swapped = (int)origin >= 5;
            var result = new SKBitmap(swapped ? decoded.Height : decoded.Width, swapped ? decoded.Width : decoded.Height);
            using var canvas = new SKCanvas(result);
            var m = SKMatrix.CreateIdentity();
            switch (origin)
            {
                case SKEncodedOrigin.TopRight: m.ScaleX = -1; m.TransX = decoded.Width; break;
                case SKEncodedOrigin.BottomRight: m.ScaleX = -1; m.ScaleY = -1; m.TransX = decoded.Width; m.TransY = decoded.Height; break;
                case SKEncodedOrigin.BottomLeft: m.ScaleY = -1; m.TransY = decoded.Height; break;
                case SKEncodedOrigin.LeftTop: m.ScaleX = m.ScaleY = 0; m.SkewX = m.SkewY = 1; break;
                case SKEncodedOrigin.RightTop: m.ScaleX = m.ScaleY = 0; m.SkewX = -1; m.SkewY = 1; m.TransX = decoded.Height; break;
                case SKEncodedOrigin.RightBottom: m.ScaleX = m.ScaleY = 0; m.SkewX = m.SkewY = -1; m.TransX = decoded.Height; m.TransY = decoded.Width; break;
                case SKEncodedOrigin.LeftBottom: m.ScaleX = m.ScaleY = 0; m.SkewX = 1; m.SkewY = -1; m.TransY = decoded.Width; break;
            }
            canvas.SetMatrix(m);
            canvas.DrawBitmap(decoded, 0, 0);
            return result;
        }
        internal static SKRect ClampCrop(SKRect crop, int width, int height)
        {
            float left = Math.Clamp(Math.Min(crop.Left, crop.Right), 0, width - 1);
            float top = Math.Clamp(Math.Min(crop.Top, crop.Bottom), 0, height - 1);
            return new SKRect(left, top, Math.Clamp(Math.Max(crop.Left, crop.Right), left + 1, width),
                Math.Clamp(Math.Max(crop.Top, crop.Bottom), top + 1, height));
        }
        internal static (int Width, int Height) OutputSize(SKRect crop, double percent)
        {
            double factor = Math.Clamp(percent, 10, 200) / 100;
            // Keep stored portraits practical, without prescribing an input or output aspect ratio.
            factor = Math.Min(factor, 4096 / Math.Max(crop.Width, crop.Height));
            return (Math.Max(1, (int)Math.Round(crop.Width * factor)), Math.Max(1, (int)Math.Round(crop.Height * factor)));
        }
        internal static byte[] Export(SKBitmap source, SKRect crop, double percent)
        {
            crop = ClampCrop(crop, source.Width, source.Height);
            var size = OutputSize(crop, percent);
            using var output = new SKBitmap(size.Width, size.Height);
            using (var canvas = new SKCanvas(output))
            using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(source, crop, new SKRect(0, 0, size.Width, size.Height), paint);
            }
            using var image = SKImage.FromBitmap(output);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }
    }
}
