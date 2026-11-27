using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bloxstrap.LXR.Customization
{
    public enum CursorLook
    {
        Light,
        Dark,
        Red,
        Accent
    }

    /// <summary>
    /// Draws smooth, rounded, Fluent / Windows 11 style cursors at any size and returns them as PNG files for
    /// Roblox's cursor slots. This is our own original drawing, an approximation of that style; it is not a copy
    /// of any specific third-party cursor set. Roblox treats the image centre as the click point, so every shape
    /// is drawn with its hotspot exactly at the canvas centre.
    /// </summary>
    public static class CursorGenerator
    {
        public const int MinPercent = 50;
        public const int MaxPercent = 300;

        /// <summary>Canvas edge in pixels for a size percentage. 100% = 64 px, like Roblox's own arrows.</summary>
        public static int CanvasFor(int percent)
        {
            int size = (int)Math.Round(64 * Math.Clamp(percent, MinPercent, MaxPercent) / 100.0);
            size = Math.Clamp(size, 16, CursorService.MaxSize);
            return size % 2 == 0 ? size : size + 1; // even, so the centre sits on a pixel edge
        }

        public static string PackName(CursorLook look, int percent, bool gameBarFriendly = false) =>
            $"Fluent {look} {Math.Clamp(percent, MinPercent, MaxPercent)}%" + (gameBarFriendly ? " GameBar" : "");

        // The crosshair is drawn within 13 units of the click point, plus half its outline.
        private const double CrosshairReach = 15.5;

        /// <summary>Canvas edge of the shift-lock image. Game Bar friendly mode crops it as tight as the drawing allows.</summary>
        public static int ShiftLockCanvasFor(int percent, bool gameBarFriendly)
        {
            if (!gameBarFriendly)
                return CanvasFor(percent);

            double perUnit = Math.Clamp(percent, MinPercent, MaxPercent) / 100.0;
            int size = (int)Math.Ceiling(CrosshairReach * 2 * perUnit) + 2;
            return size % 2 == 0 ? size : size + 1;
        }

        public static Dictionary<string, byte[]> Generate(CursorLook look, int percent, Color accent, bool gameBarFriendly = false)
        {
            int size = CanvasFor(percent);
            double u = size / 64.0;

            (Color fill, Color edge) = look switch
            {
                CursorLook.Dark => (Color.FromRgb(0x0B, 0x0B, 0x0D), Color.FromRgb(0xFF, 0xFF, 0xFF)),
                CursorLook.Red => (Color.FromRgb(0xFF, 0x3B, 0x4E), Color.FromRgb(0xFF, 0xFF, 0xFF)),
                CursorLook.Accent => (accent, Color.FromRgb(0xFF, 0xFF, 0xFF)),
                _ => (Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0x0B, 0x0B, 0x0D)),
            };

            double perUnit = size / 64.0;
            byte[] arrow = Render(size, perUnit, (dc, big, u) => DrawArrow(dc, big, u, fill, edge));
            byte[] beam = Render(size, perUnit, (dc, big, u) => DrawBeam(dc, big, u, fill, edge));

            int lockSize = ShiftLockCanvasFor(percent, gameBarFriendly);
            double lockPerUnit = gameBarFriendly ? Math.Clamp(percent, MinPercent, MaxPercent) / 100.0 : perUnit;
            byte[] crosshair = Render(lockSize, lockPerUnit, (dc, big, u) => DrawReticle(dc, big, u, fill, edge, !gameBarFriendly));

            return new Dictionary<string, byte[]>
            {
                ["ArrowCursor.png"] = arrow,
                ["ArrowFarCursor.png"] = arrow,
                ["IBeamCursor.png"] = beam,
                ["MouseLockedCursor.png"] = crosshair,
            };
        }

        private const int Oversample = 4;

        /// <summary>
        /// Draws the shape at 4x the final size and averages it down. That gives clean, evenly smooth edges at every
        /// size instead of the jagged or blurry edges you get from drawing straight at a small size.
        /// </summary>
        private static byte[] Render(int size, double pxPerUnit, Action<DrawingContext, int, double> draw)
        {
            int big = size * Oversample;

            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
                draw(dc, big, pxPerUnit * Oversample);

            var bitmap = new RenderTargetBitmap(big, big, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            int bigStride = big * 4;
            var source = new byte[bigStride * big];
            bitmap.CopyPixels(source, bigStride, 0);

            int stride = size * 4;
            var result = new byte[stride * size];
            int samples = Oversample * Oversample;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int b = 0, g = 0, r = 0, a = 0;

                    for (int sy = 0; sy < Oversample; sy++)
                    {
                        int row = (y * Oversample + sy) * bigStride + x * Oversample * 4;

                        for (int sx = 0; sx < Oversample; sx++)
                        {
                            int i = row + sx * 4;
                            b += source[i];
                            g += source[i + 1];
                            r += source[i + 2];
                            a += source[i + 3];
                        }
                    }

                    // premultiplied values average correctly
                    int o = y * stride + x * 4;
                    result[o] = (byte)((b + samples / 2) / samples);
                    result[o + 1] = (byte)((g + samples / 2) / samples);
                    result[o + 2] = (byte)((r + samples / 2) / samples);
                    result[o + 3] = (byte)((a + samples / 2) / samples);
                }
            }

            var final = BitmapSource.Create(size, size, 96, 96, PixelFormats.Pbgra32, null, result, stride);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(final));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        private static Pen MakePen(Color color, double thickness, PenLineCap cap = PenLineCap.Round)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();

            var pen = new Pen(brush, thickness)
            {
                LineJoin = PenLineJoin.Round,
                StartLineCap = cap,
                EndLineCap = cap
            };
            pen.Freeze();
            return pen;
        }

        // Soft shadow: a few wide, very faint strokes stacked, drawn slightly down and right.
        private static void Shadow(DrawingContext dc, Geometry shape, double u, double baseWidth, Brush? fillBrush)
        {
            dc.PushTransform(new TranslateTransform(1.0 * u, 1.6 * u));

            // faint black, never the cursor's own colour
            Brush? shadowFill = null;
            if (fillBrush is not null)
            {
                shadowFill = new SolidColorBrush(Color.FromArgb(18, 0, 0, 0));
                shadowFill.Freeze();
            }

            for (int i = 3; i >= 1; i--)
            {
                var pen = MakePen(Color.FromArgb(18, 0, 0, 0), baseWidth + i * 1.2 * u);
                dc.DrawGeometry(shadowFill, pen, shape);
            }

            dc.Pop();
        }

        private static Geometry Poly(double u, params (double X, double Y)[] points)
        {
            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(points[0].X * u, points[0].Y * u), true, true);

                for (int i = 1; i < points.Length; i++)
                    ctx.LineTo(new Point(points[i].X * u, points[i].Y * u), true, true);
            }

            geometry.Freeze();
            return geometry;
        }

        private static void DrawArrow(DrawingContext dc, int size, double u, Color fill, Color edge)
        {
            double c = size / 2.0;

            // Tailless rounded pointer in the Windows 11 / Fluent style: a soft triangle with the tip at the top-left.
            Geometry shape = Poly(u, (0, 0), (0, 25), (21, 16.8));

            dc.PushTransform(new TranslateTransform(c + 1.4 * u, c + 1.4 * u));

            var fillBrush = new SolidColorBrush(fill);
            fillBrush.Freeze();

            Shadow(dc, shape, u, 3.4 * u, fillBrush);
            dc.DrawGeometry(fillBrush, MakePen(edge, 3.6 * u), shape);   // outline, rounded by the round joins
            dc.DrawGeometry(fillBrush, MakePen(fill, 2.2 * u), shape);   // softens the inside corners

            dc.Pop();
        }

        private static void DrawBeam(DrawingContext dc, int size, double u, Color fill, Color edge)
        {
            double c = size / 2.0;
            const double half = 11.5;   // half height of the stem
            const double serif = 4.6;   // half width of the serifs

            var stem = new StreamGeometry();
            using (var ctx = stem.Open())
            {
                ctx.BeginFigure(new Point(0, -half * u), false, false);
                ctx.LineTo(new Point(0, half * u), true, true);
                ctx.BeginFigure(new Point(-serif * u, -half * u), false, false);
                ctx.LineTo(new Point(serif * u, -half * u), true, true);
                ctx.BeginFigure(new Point(-serif * u, half * u), false, false);
                ctx.LineTo(new Point(serif * u, half * u), true, true);
            }
            stem.Freeze();

            dc.PushTransform(new TranslateTransform(c, c));
            Shadow(dc, stem, u, 3.4 * u, null);
            dc.DrawGeometry(null, MakePen(edge, 4.4 * u), stem);
            dc.DrawGeometry(null, MakePen(fill, 2.0 * u), stem);
            dc.Pop();
        }

        // Precision crosshair: a ring with four ticks and a centre dot, centred exactly on the click point.
        private static void DrawReticle(DrawingContext dc, int size, double u, Color fill, Color edge, bool withShadow)
        {
            double c = size / 2.0;
            var centre = new Point(c, c);

            var shape = new GeometryGroup { FillRule = FillRule.Nonzero };
            shape.Children.Add(new EllipseGeometry(centre, 8.5 * u, 8.5 * u));

            foreach (var (dx, dy) in new[] { (0, -1), (1, 0), (0, 1), (-1, 0) })
            {
                shape.Children.Add(new LineGeometry(
                    new Point(c + dx * 4.0 * u, c + dy * 4.0 * u),
                    new Point(c + dx * 13.0 * u, c + dy * 13.0 * u)));
            }

            shape.Freeze();

            var fillBrush = new SolidColorBrush(fill);
            var edgeBrush = new SolidColorBrush(edge);
            fillBrush.Freeze();
            edgeBrush.Freeze();

            if (withShadow)
                Shadow(dc, shape, u, 3.0 * u, null);

            dc.DrawGeometry(null, MakePen(edge, 4.0 * u), shape);
            dc.DrawGeometry(null, MakePen(fill, 1.7 * u), shape);

            dc.DrawEllipse(edgeBrush, null, centre, 2.8 * u, 2.8 * u);
            dc.DrawEllipse(fillBrush, null, centre, 1.4 * u, 1.4 * u);
        }
    }
}
