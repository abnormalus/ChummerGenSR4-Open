using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Forms = Majorsilence.Forms;

namespace Chummer
{
    internal sealed class PortraitCropView : Control
    {
        private readonly Bitmap image;
        internal readonly SKBitmap Pixels;
        internal SKRect Crop;
        internal double Zoom = 1;
        internal bool CropMode = true;
        private Vector pan;
        private Point start;
        private Vector oldPan;
        private bool dragging;
        internal event Action Changed;
        internal event Action<double> ZoomRequested;
        internal PortraitCropView(SKBitmap pixels, Bitmap image)
        {
            Pixels = pixels; this.image = image;
            Crop = new SKRect(0, 0, pixels.Width, pixels.Height);
            ClipToBounds = true;
        }
        private double Scale => Math.Max(.001, Math.Min((Bounds.Width - 32) / Pixels.Width, (Bounds.Height - 32) / Pixels.Height) * Zoom);
        private Rect ImageBounds => new Rect((Bounds.Width - Pixels.Width * Scale) / 2 + pan.X,
            (Bounds.Height - Pixels.Height * Scale) / 2 + pan.Y, Pixels.Width * Scale, Pixels.Height * Scale);
        private Point ToImage(Point point) => new Point((point.X - ImageBounds.X) / Scale, (point.Y - ImageBounds.Y) / Scale);
        internal void Reset()
        {
            Crop = new SKRect(0, 0, Pixels.Width, Pixels.Height); Zoom = 1; pan = default;
            InvalidateVisual(); Changed?.Invoke();
        }
        internal void Preset(double aspect)
        {
            float width = Pixels.Width, height = Pixels.Height;
            if (width / height > aspect) width = (float)(height * aspect); else height = (float)(width / aspect);
            Crop = new SKRect((Pixels.Width - width) / 2, (Pixels.Height - height) / 2,
                (Pixels.Width + width) / 2, (Pixels.Height + height) / 2);
            InvalidateVisual(); Changed?.Invoke();
        }
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            context.FillRectangle(Brushes.DimGray, new Rect(Bounds.Size));
            var bounds = ImageBounds;
            context.DrawImage(image, new Rect(0, 0, Pixels.Width, Pixels.Height), bounds);
            var selected = new Rect(bounds.X + Crop.Left * Scale, bounds.Y + Crop.Top * Scale, Crop.Width * Scale, Crop.Height * Scale);
            var shade = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0));
            context.FillRectangle(shade, new Rect(bounds.X, bounds.Y, bounds.Width, Math.Max(0, selected.Y - bounds.Y)));
            context.FillRectangle(shade, new Rect(bounds.X, selected.Bottom, bounds.Width, Math.Max(0, bounds.Bottom - selected.Bottom)));
            context.FillRectangle(shade, new Rect(bounds.X, selected.Y, Math.Max(0, selected.X - bounds.X), selected.Height));
            context.FillRectangle(shade, new Rect(selected.Right, selected.Y, Math.Max(0, bounds.Right - selected.Right), selected.Height));
            context.DrawRectangle(null, new Pen(Brushes.LimeGreen, 2), selected);
        }
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            start = e.GetPosition(this); oldPan = pan; dragging = true;
            e.Pointer.Capture(this); e.Handled = true;
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (!dragging) return;
            var point = e.GetPosition(this);
            if (CropMode)
            {
                var a = ToImage(start); var b = ToImage(point);
                Crop = PortraitImage.ClampCrop(new SKRect((float)a.X, (float)a.Y, (float)b.X, (float)b.Y), Pixels.Width, Pixels.Height);
            }
            else pan = oldPan + (point - start);
            InvalidateVisual(); Changed?.Invoke();
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e); dragging = false; e.Pointer.Capture(null);
        }
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { dragging = false; base.OnPointerCaptureLost(e); }
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e); ZoomRequested?.Invoke(Math.Clamp(Zoom * Math.Pow(1.15, e.Delta.Y), .25, 5)); e.Handled = true;
        }
    }

    internal static class PortraitEditor
    {
        private static string Text(string key) => LanguageManager.Instance.GetString(key);
        internal static async Task<byte[]> EditAsync(Forms.Form owner, byte[] bytes)
        {
            using var pixels = PortraitImage.Decode(bytes);
            using var normalized = SKImage.FromBitmap(pixels);
            using var encoded = normalized.Encode(SKEncodedImageFormat.Png, 100);
            using var bitmap = new Bitmap(new MemoryStream(encoded.ToArray()));
            var view = new PortraitCropView(pixels, bitmap) { MinHeight = 320, Cursor = new Cursor(StandardCursorType.Cross) };
            var window = new Window { Title = Text("Portrait_Title"), Width = 820, Height = 760, MinWidth = 650, MinHeight = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "branding", "chummer.ico")) };
            window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
            var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"), Margin = new Thickness(20), RowSpacing = 12 };
            var instruction = new TextBlock { Text = Text("Portrait_Instructions"), TextWrapping = TextWrapping.Wrap };
            layout.Children.Add(instruction);
            Grid.SetRow(view, 1); layout.Children.Add(view);
            var controls = new StackPanel { Spacing = 10 };
            var modes = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
            void AddButton(string key, Action action)
            {
                var button = new Button { Content = Text(key) }; button.Click += (_, _) => action(); modes.Children.Add(button);
            }
            AddButton("Portrait_Crop", () => { view.CropMode = true; view.Cursor = new Cursor(StandardCursorType.Cross); });
            AddButton("Portrait_Move", () => { view.CropMode = false; view.Cursor = new Cursor(StandardCursorType.SizeAll); });
            AddButton("Portrait_Portrait", () => view.Preset(21.0 / 31));
            AddButton("Portrait_Square", () => view.Preset(1));
            AddButton("Portrait_Full", view.Reset);
            controls.Children.Add(modes);
            var settings = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto"), ColumnSpacing = 10 };
            var zoomLabel = new TextBlock { Text = Text("Portrait_Zoom"), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            settings.Children.Add(zoomLabel);
            var zoom = new Slider { Minimum = .25, Maximum = 5, Value = 1, MinWidth = 150 };
            Grid.SetColumn(zoom, 1); settings.Children.Add(zoom);
            zoom.ValueChanged += (_, _) => { view.Zoom = zoom.Value; view.InvalidateVisual(); };
            view.ZoomRequested += value => zoom.Value = value;
            var outputLabel = new TextBlock { Text = Text("Portrait_Scale"), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            Grid.SetColumn(outputLabel, 2); settings.Children.Add(outputLabel);
            var scale = new NumericUpDown { Minimum = 10, Maximum = 200, Value = 100, Increment = 10, Width = 140, Height = 36, FormatString = "0'%'" };
            Grid.SetColumn(scale, 3); settings.Children.Add(scale);
            var dimensions = new TextBlock { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            Grid.SetColumn(dimensions, 4); settings.Children.Add(dimensions);
            void UpdateSize() { var size = PortraitImage.OutputSize(view.Crop, (double)(scale.Value ?? 100)); dimensions.Text = $"{size.Width} × {size.Height}"; }
            scale.ValueChanged += (_, _) => UpdateSize(); view.Changed += UpdateSize;
            view.Changed += () => { if (view.Zoom == 1) zoom.Value = 1; };
            UpdateSize(); controls.Children.Add(settings);
            Grid.SetRow(controls, 2); layout.Children.Add(controls);
            var footer = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
            var cancel = new Button { Content = Text("Portrait_Cancel") }; cancel.Click += (_, _) => window.Close(null);
            var apply = new Button { Content = Text("Portrait_Apply") };
            apply.Click += (_, _) =>
            {
                try { window.Close(PortraitImage.Export(pixels, view.Crop, (double)(scale.Value ?? 100))); }
                catch (Exception error) { AppErrors.OnThreadException(null, new System.Threading.ThreadExceptionEventArgs(error)); }
            };
            footer.Children.Add(cancel); footer.Children.Add(apply); Grid.SetRow(footer, 3); layout.Children.Add(footer);
            window.Content = layout;
            window.KeyDown += (_, e) => { if (e.Key == Key.Escape) { window.Close(null); e.Handled = true; } };
            return await window.ShowDialog<byte[]>(Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner.MdiParent ?? owner));
        }
    }
}
