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

        public static string PackName(CursorLook look, int percent) =>
            $"Fluent {look} {Math.Clamp(percent, MinPercent, MaxPercent)}%";

        public static Dictionary<string, byte[]> Generate(CursorLook look, int percent, Color accent)
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

            byte[] arrow = Render(size, dc => DrawArrow(dc, size, u, fill, edge));
            byte[] beam = Render(size, dc => DrawBeam(dc, size, u, fill, edge));
            byte[] lockRing = Render(size, dc => DrawReticle(dc, size, u, fill, edge));

            return new Dictionary<string, byte[]>
            {
                ["ArrowCursor.png"] = arrow,
                ["ArrowFarCursor.png"] = arrow,
                ["IBeamCursor.png"] = beam,
                ["MouseLockedCursor.png"] = lockRing,
            };
        }

        private static byte[] Render(int size, Action<DrawingContext> draw)
        {
            var visual = new DrawingVisual();
            RenderOptions.SetEdgeMode(visual, EdgeMode.Unspecified);

            using (var dc = visual.RenderOpen())
                draw(dc);

            var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

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

            for (int i = 3; i >= 1; i--)
            {
                var pen = MakePen(Color.FromArgb(18, 0, 0, 0), baseWidth + i * 1.6 * u);
                dc.DrawGeometry(fillBrush, pen, shape);
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

            // Pointer outline, tip at (0,0). Corners get rounded by the round-joined strokes below.
            Geometry shape = Poly(u,
                (0, 0), (0, 24), (6.5, 18.5), (10.5, 27.5), (14.6, 25.8), (10.8, 17), (19, 17));

            dc.PushTransform(new TranslateTransform(c + 1.2 * u, c + 1.2 * u));

            var fillBrush = new SolidColorBrush(fill);
            fillBrush.Freeze();

            Shadow(dc, shape, u, 2.6 * u, fillBrush);
            dc.DrawGeometry(fillBrush, MakePen(edge, 3.0 * u), shape);      // outline
            dc.DrawGeometry(fillBrush, MakePen(fill, 1.4 * u), shape);      // rounds and softens the inner corners

            dc.Pop();
        }

        private static void DrawBeam(DrawingContext dc, int size, double u, Color fill, Color edge)
        {
            double c = size / 2.0;
            const double half = 11.0;   // half height of the stem
            const double serif = 4.2;   // half width of the serifs

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
            Shadow(dc, stem, u, 2.4 * u, null);
            dc.DrawGeometry(null, MakePen(edge, 4.0 * u), stem);
            dc.DrawGeometry(null, MakePen(fill, 1.8 * u), stem);
            dc.Pop();
        }

        private static void DrawReticle(DrawingContext dc, int size, double u, Color fill, Color edge)
        {
            double c = size / 2.0;
            var center = new Point(c, c);

            var ring = new EllipseGeometry(center, 9 * u, 9 * u);
            var dot = new EllipseGeometry(center, 1.6 * u, 1.6 * u);
            ring.Freeze();
            dot.Freeze();

            var fillBrush = new SolidColorBrush(fill);
            var edgeBrush = new SolidColorBrush(edge);
            fillBrush.Freeze();
            edgeBrush.Freeze();

            Shadow(dc, ring, u, 2.4 * u, null);
            dc.DrawGeometry(null, MakePen(edge, 4.2 * u), ring);
            dc.DrawGeometry(null, MakePen(fill, 2.0 * u), ring);

            dc.DrawGeometry(edgeBrush, null, new EllipseGeometry(center, 3.0 * u, 3.0 * u));
            dc.DrawGeometry(fillBrush, null, dot);
        }
    }
}
