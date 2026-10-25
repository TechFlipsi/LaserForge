using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LaserForge.Desktop.Controls;

/// <summary>
/// Job-Ausgangsposition als 3×3-Punkte-Raster — eigene einfache Geometrie (kein Fremd-Asset).
/// Der mittlere Punkt gilt als aktive Ausgangsposition (Orange, AUSWAHL-Token #FF9900).
/// wird in der Hauptleiste klein (2 Instanzen) und im Laser-Panel groß benutzt.
/// </summary>
public class PunkteRaster9 : Control
{
    private static readonly IBrush Punkt = new SolidColorBrush(Color.Parse("#9E9E9E"));
    private static readonly IBrush PunktAktiv = new SolidColorBrush(Color.Parse("#FF9900"));

    public override void Render(DrawingContext ctx)
    {
        double w = Math.Max(Bounds.Width, 1);
        double h = Math.Max(Bounds.Height, 1);
        double zelle = Math.Min(w, h) / 3.0;
        double r = zelle * 0.30;
        var pen = new Pen(Punkt, 1.3);

        for (int zeile = 0; zeile < 3; zeile++)
            for (int spalte = 0; spalte < 3; spalte++)
            {
                double cx = w / 2 + (spalte - 1) * zelle;
                double cy = h / 2 + (zeile - 1) * zelle;
                var rect = new Rect(cx - r, cy - r, 2 * r, 2 * r);
                if (zeile == 1 && spalte == 1)
                    ctx.DrawEllipse(PunktAktiv, pen, rect);
                else
                    ctx.DrawEllipse(null, pen, rect);
            }
    }
}