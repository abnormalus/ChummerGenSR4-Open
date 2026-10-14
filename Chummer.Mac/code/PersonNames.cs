using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace Chummer
{
    internal static class PersonNames
    {
        internal sealed class Entry
        {
            public string Name { get; set; }
            public long Count { get; set; }
            public long Male { get; set; }
            public long Female { get; set; }
        }
        private sealed class Data
        {
            public Entry[] First { get; set; }
            public Entry[] Last { get; set; }
        }
        private sealed class Pool
        {
            private readonly string[] names;
            private readonly long[] cumulative;
            internal Pool(Entry[] entries, Func<Entry, long> weight)
            {
                names = entries.Select(e => e.Name).ToArray();
                cumulative = new long[entries.Length];
                long total = 0;
                for (int i = 0; i < entries.Length; i++) cumulative[i] = total += weight(entries[i]);
                if (total <= 0) throw new InvalidDataException("Empty person-name pool");
            }
            internal string Pick(Random random)
            {
                long target = random.NextInt64(cumulative[cumulative.Length - 1]) + 1;
                int index = Array.BinarySearch(cumulative, target);
                return names[index >= 0 ? index : ~index];
            }
        }
        private sealed class Pools
        {
            internal Pool Male, Female, Mixed, Last;
            internal Entry[] First;
        }
        private static readonly Lazy<Pools> Loaded = new Lazy<Pools>(() => {
            using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "data", "person-names.json.gz"));
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            var data = JsonSerializer.Deserialize<Data>(gzip, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return new Pools {
                First = data.First,
                Male = new Pool(data.First.Where(e => e.Male > 0 && e.Male >= e.Female).ToArray(), e => e.Male),
                Female = new Pool(data.First.Where(e => e.Female > 0 && e.Female >= e.Male).ToArray(), e => e.Female),
                Mixed = new Pool(data.First, e => e.Count), Last = new Pool(data.Last, e => e.Count)
            };
        });
        private static readonly Lazy<Majorsilence.Forms.Drawing.Image> Icon = new Lazy<Majorsilence.Forms.Drawing.Image>(() => {
            using var bitmap = new SkiaSharp.SKBitmap(18, 18);
            using var canvas = new SkiaSharp.SKCanvas(bitmap);
            canvas.Clear(SkiaSharp.SKColors.Transparent);
            using var paint = new SkiaSharp.SKPaint { IsAntialias = true, Color = SkiaSharp.SKColors.White };
            canvas.DrawRoundRect(new SkiaSharp.SKRect(1, 1, 17, 17), 3, 3, paint);
            paint.Color = SkiaSharp.SKColors.Black;
            paint.Style = SkiaSharp.SKPaintStyle.Stroke;
            paint.StrokeWidth = 1.2f;
            canvas.DrawRoundRect(new SkiaSharp.SKRect(1, 1, 17, 17), 3, 3, paint);
            paint.Style = SkiaSharp.SKPaintStyle.Fill;
            foreach (var point in new[] { new SkiaSharp.SKPoint(5, 5), new SkiaSharp.SKPoint(13, 5), new SkiaSharp.SKPoint(9, 9), new SkiaSharp.SKPoint(5, 13), new SkiaSharp.SKPoint(13, 13) })
                canvas.DrawCircle(point, 1.35f, paint);
            using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
            using var bytes = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(bytes.ToArray());
            return Majorsilence.Forms.Drawing.Image.FromStream(stream, true);
        });
        internal static Majorsilence.Forms.Drawing.Image DiceIcon => Icon.Value;
        internal static string GenderPool(string gender)
        {
            switch ((gender ?? "").Trim().ToLowerInvariant())
            {
                case "male": case "m": case "man": case "masculine": case "trans man": case "transgender man": return "male";
                case "female": case "f": case "woman": case "feminine": case "trans woman": case "transgender woman": return "female";
                default: return "mixed";
            }
        }
        internal static string Generate(string gender, Random random = null)
        {
            random ??= Random.Shared;
            var pools = Loaded.Value;
            var first = GenderPool(gender) == "male" ? pools.Male : GenderPool(gender) == "female" ? pools.Female : pools.Mixed;
            return first.Pick(random) + " " + pools.Last.Pick(random);
        }
        internal static bool FirstNameFits(string first, string gender) => Loaded.Value.First.Any(e => e.Name == first &&
            (GenderPool(gender) == "mixed" || (GenderPool(gender) == "male" ? e.Male >= e.Female : e.Female >= e.Male)));
    }
}
