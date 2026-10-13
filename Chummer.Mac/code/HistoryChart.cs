using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Majorsilence.Forms;
using SkiaSharp;

namespace Chummer
{
    // Replaces the Windows-only DataVisualization control with an actual Skia chart.
    public sealed class HistoryChart : Control, ISupportInitialize
    {
        private int[] _intValues = Array.Empty<int>();
        private string _strTitle = "";
        private SKColor _objColor = SKColors.Blue;

        public void BeginInit() { }
        public void EndInit() { }

        public void SetEntries(IEnumerable<ExpenseLogEntry> entries, ExpenseType type, string title)
        {
            int intTotal = 0;
            _intValues = entries.Where(x => x.Type == type).OrderBy(x => x.Date)
                .Select(x => intTotal += x.Amount).Prepend(0).ToArray();
            _strTitle = title;
            _objColor = type == ExpenseType.Karma ? SKColors.RoyalBlue : SKColors.ForestGreen;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var canvas = e.Canvas;
            using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White };
            canvas.DrawRect(0, 0, Width, Height, paint);
            paint.Color = SKColors.Black;
            using var font = new SKFont(SKTypeface.Default, 12);
            canvas.DrawText(_strTitle, 8, 17, font, paint);
            if (_intValues.Length == 0 || Width < 90 || Height < 60) return;
            float min = Math.Min(0, _intValues.Min()), max = Math.Max(1, _intValues.Max());
            float left = 65, top = 28, right = Width - 12, bottom = Height - 25;
            paint.Color = SKColors.Gray;
            canvas.DrawLine(left, top, left, bottom, paint);
            canvas.DrawLine(left, bottom, right, bottom, paint);
            canvas.DrawText(max.ToString("0"), 3, top + 10, font, paint);
            canvas.DrawText(min.ToString("0"), 3, bottom, font, paint);
            using var path = new SKPath();
            for (int i = 0; i < _intValues.Length; i++)
            {
                float x = left + (right - left) * i / Math.Max(1, _intValues.Length - 1);
                float y = bottom - (bottom - top) * (_intValues[i] - min) / (max - min);
                if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
            }
            paint.Color = _objColor;
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 2;
            canvas.DrawPath(path, paint);
        }
    }
}
