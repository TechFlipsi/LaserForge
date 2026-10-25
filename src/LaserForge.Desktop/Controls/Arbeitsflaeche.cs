using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LaserForge.Desktop.Controls;

/// <summary>
/// Hauptarbeitsfläche nach docs/design-ziel-bild.md:
/// heller Canvas (#FFFFFF), Lineale oben/links (#FAFAFA, schwarze Ziffern),
/// feines 5-mm-Raster (#E8E8E8) plus grobes 10-mm-Raster (#CFCFCF),
/// Arbeitsbereich-Rahmen 400×400 mm (hellgrau) und grüner Ursprungs-Pfeil unten links.
/// Reine Anzeige (M5-a) — Zoom/Interaktion folgt in späteren Meilensteinen.
/// </summary>
public class Arbeitsflaeche : Control
{
    private static readonly IBrush FlaechenHell = new SolidColorBrush(Color.Parse("#FFFFFF"));
    private static readonly IBrush RasterFein = new SolidColorBrush(Color.Parse("#E8E8E8"));
    private static readonly IBrush RasterGross = new SolidColorBrush(Color.Parse("#CFCFCF"));
    private static readonly IBrush BereichRahmenFarbe = new SolidColorBrush(Color.Parse("#A0A0A0"));
    private static readonly IBrush LinealFlaeche = new SolidColorBrush(Color.Parse("#FAFAFA"));
    private static readonly IBrush LinealRand = new SolidColorBrush(Color.Parse("#BFBFBF"));
    private static readonly IBrush LinealText = new SolidColorBrush(Color.Parse("#333333"));
    private static readonly IBrush LinealStriche = new SolidColorBrush(Color.Parse("#909090"));
    private static readonly IBrush PfeilGruen = new SolidColorBrush(Color.Parse("#00A000"));

    private static readonly Typeface Schrift = Typeface.Default;
    private static readonly Pen StrichPen = new(LinealStriche, 1.2);

    /// <summary>Beschriftung im Lineal (schwarze Ziffern, klein).</summary>
    private static FormattedText Beschriftung(string text) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Schrift, 9.0, LinealText);

    public override void Render(DrawingContext ctx)
    {
        double w = Math.Max(Bounds.Width, 1);
        double h = Math.Max(Bounds.Height, 1);
        const double lineal = 22.0;   // Lineal-Band oben/links (Pixel)

        // Maßstab: 400-mm-Bereich muss in Breite wie Höhe passen (je minus Rand).
        double px = Math.Min((w - lineal - 10.0) / 400.0, (h - lineal - 10.0) / 400.0);
        if (px <= 0) px = 1;

        // Weiße Fläche füllen (alles unter den Linealen wird später überdeckt).
        ctx.DrawRectangle(FlaechenHell, null, new Rect(0, 0, w, h));

        // Ursprung (0|0) unten links; 400-mm-Bereich in der Fläche zentrieren.
        double ox = lineal + 5.0 + (w - lineal - 10.0 - 400.0 * px) / 2.0;
        double oy = (lineal + h) / 2.0 + 200.0 * px;

        // Feines Raster (5 mm) über die ganze sichtbare Fläche, grobes Raster (10 mm) sichtbar.
        double schritt = 5.0 * px;
        int mmLinks = -(int)Math.Ceiling((ox - lineal) / schritt) * 5;
        int mmRechts = (int)Math.Floor((w - ox) / schritt) * 5;
        var rasterFeinPen = new Pen(RasterFein, 1);
        var rasterGrossPen = new Pen(RasterGross, 1.2);
        for (int mm = mmLinks; mm <= mmRechts; mm += 5)
        {
            double x = Math.Round(ox + mm * px) + 0.5;
            ctx.DrawLine(mm % 10 == 0 ? rasterGrossPen : rasterFeinPen,
                         new Point(x, 0), new Point(x, h));
        }
        int mmUnten = -(int)Math.Ceiling((h - oy) / schritt) * 5;
        int mmOben = (int)Math.Floor((oy - lineal) / schritt) * 5;
        for (int mm = mmUnten; mm <= mmOben; mm += 5)
        {
            double y = Math.Round(oy - mm * px) + 0.5;
            ctx.DrawLine(mm % 10 == 0 ? rasterGrossPen : rasterFeinPen,
                         new Point(lineal, y), new Point(w, y));
        }

        // Arbeitsbereich-Rahmen 400×400 mm hellgrau.
        ctx.DrawRectangle(null, new Pen(BereichRahmenFarbe, 2),
                          new Rect(ox, oy - 400.0 * px, 400.0 * px, 400.0 * px));

        // Ursprüngliche Startmarke: grüner Pfeil entlang X plus Z-Marke nach oben.
        var gruenPen = new Pen(PfeilGruen, 2);
        ctx.DrawLine(gruenPen, new Point(ox, oy), new Point(ox + 26, oy));
        Pfeildreieck(ctx, new Point(ox + 36, oy), new Point(ox + 25, oy - 4), new Point(ox + 25, oy + 4), PfeilGruen);
        ctx.DrawLine(gruenPen, new Point(ox, oy), new Point(ox, oy - 26));
        Pfeildreieck(ctx, new Point(ox, oy - 36), new Point(ox - 4, oy - 25), new Point(ox + 4, oy - 25), PfeilGruen);

        // Lineal oben + links.
        ctx.DrawRectangle(LinealFlaeche, null, new Rect(0, 0, w, lineal));
        ctx.DrawRectangle(LinealFlaeche, null, new Rect(0, 0, lineal, h));
        ctx.DrawLine(new Pen(LinealRand, 1), new Point(0, lineal + 0.5), new Point(w, lineal + 0.5));
        ctx.DrawLine(new Pen(LinealRand, 1), new Point(lineal + 0.5, 0), new Point(lineal + 0.5, h));

        // Ticks + Ziffern oben (0.. sichtbar, Zehner-Strich, Vierzig-Zahl).
        double ticks = 10.0 * px;
        for (int mm = 0; ; mm += 10)
        {
            double x = ox + mm * px;
            if (x > w - 2) break;
            bool groß = mm % 40 == 0;
            ctx.DrawLine(StrichPen,
                new Point(x + 0.5, lineal - (groß ? 10 : 6)),
                new Point(x + 0.5, lineal));
            if (groß)
            {
                var ft = Beschriftung(mm.ToString(CultureInfo.InvariantCulture));
                ctx.DrawText(ft, new Point(x - ft.Width / 2, 2));
            }
        }

        // Ticks + Ziffern links (400..0 abwärts: 0 am Ursprung unten).
        for (int mm = 0; ; mm += 10)
        {
            double y = oy - mm * px;
            if (y < lineal) break;
            bool groß = mm % 40 == 0;
            ctx.DrawLine(StrichPen,
                new Point(lineal - (groß ? 10 : 6), y + 0.5),
                new Point(lineal, y + 0.5));
            if (groß)
            {
                var ft = Beschriftung(mm.ToString(CultureInfo.InvariantCulture));
                ctx.DrawText(ft, new Point(lineal - 12 - ft.Width, y - ft.Height / 2));
            }
        }
    }

    /// <summary>Gefülltes Dreieck als Pfeilspitze (einfache eigene Geometrie).</summary>
    private static void Pfeildreieck(DrawingContext ctx, Point spitze, Point a, Point b, IBrush farbe)
    {
        var geo = new StreamGeometry();
        using (var gctx = geo.Open())
        {
            gctx.BeginFigure(a, false);
            gctx.LineTo(b);
            gctx.LineTo(spitze);
            gctx.EndFigure(true);
        }
        ctx.DrawGeometry(farbe, null, geo);
    }
}