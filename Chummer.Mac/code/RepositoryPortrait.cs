#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SkiaSharp;

namespace Chummer;

internal static class RepositoryPortrait
{
    internal const int MaximumCharacterBytes = 8 * 1024 * 1024;
    private const int MaximumEncodedImage = 4 * 1024 * 1024;

    internal static byte[]? ReadThumbnail(byte[] character)
    {
        if (character.Length > MaximumCharacterBytes)
            return null;

        try
        {
            using var stream = new MemoryStream(character, writable: false);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                MaxCharactersInDocument = MaximumCharacterBytes
            });
            var root = XDocument.Load(reader).Root;
            if (root?.Name != "character")
                return null;

            var portraits = root.Element("mugshots")?.Elements("mugshot").ToArray() ?? [];
            int.TryParse(root.Element("mainmugshotindex")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index);
            string? encoded = index >= 0 && index < portraits.Length ? portraits[index].Value : null;
            if (string.IsNullOrWhiteSpace(encoded))
                encoded = portraits.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(encoded))
                encoded = root.Element("mugshotbase64")?.Value;
            if (string.IsNullOrWhiteSpace(encoded))
                encoded = root.Element("mugshot")?.Value;
            if (string.IsNullOrWhiteSpace(encoded) || encoded.Length > MaximumEncodedImage)
                return null;

            byte[] bytes = Convert.FromBase64String(encoded);
            using var imageStream = new SKMemoryStream(bytes);
            using var codec = SKCodec.Create(imageStream);
            if (codec == null || codec.EncodedFormat is not (SKEncodedImageFormat.Png or SKEncodedImageFormat.Jpeg
                or SKEncodedImageFormat.Gif or SKEncodedImageFormat.Webp) ||
                (long)codec.Info.Width * codec.Info.Height > 16_000_000)
                return null;

            using var bitmap = PortraitImage.Decode(bytes);
            int side = Math.Min(bitmap.Width, bitmap.Height);
            float left = (bitmap.Width - side) / 2f, top = (bitmap.Height - side) / 2f;
            using var thumbnail = new SKBitmap(96, 96);
            using (var canvas = new SKCanvas(thumbnail))
            using (var image = SKImage.FromBitmap(bitmap))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawImage(image, new SKRect(left, top, left + side, top + side), new SKRect(0, 0, 96, 96),
                    new SKSamplingOptions(SKCubicResampler.Mitchell));
            }
            using var result = thumbnail.Encode(SKEncodedImageFormat.Png, 100);
            return result.ToArray();
        }
        catch (Exception error) when (error is XmlException or FormatException or InvalidDataException)
        {
            return null;
        }
    }

    internal static string Initials(string filename)
    {
        var words = Path.GetFileNameWithoutExtension(filename)
            .Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 0 ? "?" : string.Concat(words.Take(2).Select(word =>
            StringInfo.GetNextTextElement(word).ToUpperInvariant()));
    }
}
