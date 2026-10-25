using System.Globalization;
using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Pipeline;
/// <summary>
/// Optionen fuer die GRBL-Flavor-Erzeugung. S-Skala 0..1000 (GRBL-Standard, $30=1000).
/// Mengen intern in %/mm/mm/s; ausgegeben mm, mm/min. Goldene Formatregeln (Spec Abschnitt 11):
/// Koordinaten %.3f, F %.1f, S %.1f.
/// </summary>
public sealed class GCodeOptionen
{
    public double MaxPowerS { get; set; } = 1000;
    public bool ParkenAmEnde { get; set; } = true;
    public double RahmenPower { get; set; } = 5;     // % (Spec Abschnitt 7 Standard)
    public double RahmenSpeed { get; set; } = 10;    // mm/s = 600 mm/min
    public bool RahmenEinfuegen { get; set; }
}
/// <summary>
/// GRBL-Flavor: Kopf (G21/G90/M4/S0), Konturen (G0 - M4 S - G1 F - G1 ... - M5),
/// Scan-Raster (bidi, Zeilen via G0-Y-Schritten), optional Rahmen, Fuss (M5 + Parken).
/// </summary>
public static class GCodeErzeuger
{
    private static readonly CultureInfo E = CultureInfo.InvariantCulture;
    public static string Zahl(double v) => v.ToString("0.000", E);     // Koordinaten
    public static string TextS(double v) => v.ToString("0.0", E);      // S-Wert
    public static string TextF(double v) => v.ToString("0.0", E);      // Feed mm/min

    public static List<string> Erzeuge(Projekt projekt, GCodeOptionen? opt = null)
    {
        var o = opt ?? new GCodeOptionen();
        var zeilen = new List<string>
        {
            "G21", // Millimeter
            "G90", // absolut
            "M4",  // Lasermodus dynamisch
            "S0",  // Leistung 0 als Startzustand
        };
        foreach (var form in projekt.Formen)
        {
            if (form.Versteckt) continue;
            var e = projekt.Schnitte.FirstOrDefault(s => s.Index == form.SchnittIndex)
                    ?? new SchnittEinstellung { Index = form.SchnittIndex };
            if (!e.Aktiviert) continue;
            if (form is LaserForge.Core.Formen.BildForm bild)
            {
                zeilen.AddRange(BildRaster(bild, e, o));
                continue;
            }
            var zuege = Optimierer.Ordne(form.ZuZuege());
            if (zuege.Count == 0) continue;
            if (e.Typ is "Scan" or "Fill")
                zeilen.AddRange(Raster(zuege, e, o));
            else
                foreach (var z in zuege)
                    zeilen.AddRange(Kontur(z, e, o));
        }
        if (o.RahmenEinfuegen)
            zeilen.AddRange(Rahmen(0, 0, projekt.BreiteMM, projekt.HoeheMM, o));
        zeilen.Add("M5");
        if (o.ParkenAmEnde) zeilen.Add("G0 X0.000 Y0.000");
        return zeilen;
    }

    /// <summary>Eine Kontur (Spec Abschnitt 5) inklusive Passe-Wiederholung.</summary>
    public static List<string> Kontur(Zug zug, SchnittEinstellung e, GCodeOptionen o)
    {
        var zeilen = new List<string>();
        if (zug.Punkte.Count < 2) return zeilen;
        var s = TextS(SMacht(e));
        var f = TextF(e.Speed * 60.0);
        var start = zug.Punkte[0];
        int passMax = Math.Max(1, e.NumPasses);
        zeilen.Add($"G0 X{Zahl(start.X)} Y{Zahl(start.Y)}");
        for (int pass = 0; pass < passMax; pass++)
        {
            zeilen.Add($"M4 S{s}");
            zeilen.Add($"G1 F{f}");
            for (int j = 1; j < zug.Punkte.Count; j++)
            {
                var p = zug.Punkte[j];
                zeilen.Add($"G1 X{Zahl(p.X)} Y{Zahl(p.Y)}");
            }
            zeilen.Add("M5");
            if (pass + 1 < passMax)
                zeilen.Add($"G0 X{Zahl(start.X)} Y{Zahl(start.Y)}");
        }
        return zeilen;
    }

    /// <summary>Scan-Raster (Spec Abschnitt 6): erste Zeile M4 S + G1 F; weitere Zeilen G0-Y-Schritt + G1; bidi.</summary>
    public static List<string> Raster(IReadOnlyList<Zug> zuege, SchnittEinstellung e, GCodeOptionen o)
    {
        var zeilen = new List<string>();
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var z in zuege)
            foreach (var p in z.Punkte)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
        if (minX > maxX) return zeilen;
        double abstand = Math.Max(0.05, e.LineSpacing);
        var f = TextF(e.Speed * 60.0);
        var s = TextS(SMacht(e));
        bool bidi = true;
        bool erste = true;
        for (double ry = minY; ry <= maxY + 1e-9; ry += abstand)
        {
            if (bidi)
            {
                if (erste)
                {
                    zeilen.Add($"G0 X{Zahl(minX)} Y{Zahl(ry)}");
                    zeilen.Add($"M4 S{s}");
                    Zeile(zeilen, $"G1 F{f} X{Zahl(maxX)}");
                    erste = false;
                }
                else
                {
                    Zeile(zeilen, $"G0 Y{Zahl(ry)}");
                    Zeile(zeilen, $"G1 F{f} X{Zahl(maxX)}");
                }
            }
            else
            {
                Zeile(zeilen, $"G0 Y{Zahl(ry)}");
                Zeile(zeilen, $"G1 F{f} X{Zahl(minX)}");
            }
            bidi = !bidi;
        }
        return zeilen;
    }
    private static void Zeile(List<string> zeilen, string zeile)
    {
        zeilen.Add(zeile);
    }

    /// <summary>Arbeitsflaeche-/Job-Rahmen (Spec Abschnitt 7): 5 % @ 600 mm/min Standard.</summary>
    public static List<string> Rahmen(double x, double y, double w, double h, GCodeOptionen o)
    {
        var zeilen = new List<string>
        {
            $"G0 X{Zahl(x)} Y{Zahl(y)}",
            $"M4 S{TextS(Math.Clamp(o.RahmenPower, 0, 100) / 100 * o.MaxPowerS)}",
            $"G1 F{TextF(o.RahmenSpeed * 60.0)} X{Zahl(x + w)}",
            $"G1 Y{Zahl(y + h)}",
            $"G1 X{Zahl(x)}",
            $"G1 Y{Zahl(y)}",
            "M5",
        };
        return zeilen;
    }
    /// <summary>
    /// Bild-Scan-Gravur: dunkle Pixel (unter Schwelle) = Laser-Segmente, S je Bild einmal (Spec Abschnitt 6),
    /// Zeilenabstand = max(PixelMM, lineSpacing), bidi-Alternation, XForm angewendet.
    /// </summary>
    public static List<string> BildRaster(LaserForge.Core.Formen.BildForm bild, SchnittEinstellung e, GCodeOptionen o)
    {
        var zeilen = new List<string>();
        if (bild.BreitePix <= 0 || bild.HoehePix <= 0 || bild.Grauwerte.Length == 0) return zeilen;
        double abstand = Math.Max(bild.PixelMM, e.LineSpacing);
        var f = TextF(e.Speed * 60.0);
        var xf = bild.XForm;
        bool erste = true;
        void Segment(double startXmm, double endXmm, double my)
        {
            if (endXmm - startXmm < 1e-9) return;
            var a = xf.Anwenden(new Punkt(startXmm, my));
            var b = xf.Anwenden(new Punkt(endXmm, my));
            zeilen.Add($"G0 X{Zahl(a.X)} Y{Zahl(a.Y)}");
            zeilen.Add($"G1 F{f} X{Zahl(b.X)} Y{Zahl(b.Y)}");
        }
        for (double my = 0; my < bild.HoehePix * bild.PixelMM - 1e-9; my += abstand)
        {
            if (erste) { zeilen.Add($"M4 S{TextS(SMacht(e))}"); erste = false; }
            int pixZeile = Math.Clamp((int)Math.Round(my / bild.PixelMM), 0, bild.HoehePix - 1);
            double? startX = null;
            for (int px = 0; px < bild.BreitePix; px++)
            {
                bool dunkel = bild.PixelDunkel(px, pixZeile);
                if (dunkel && startX is null) startX = px * bild.PixelMM;
                else if (!dunkel && startX is not null) { Segment(startX.Value, px * bild.PixelMM, my); startX = null; }
            }
            if (startX is not null) Segment(startX.Value, bild.BreitePix * bild.PixelMM, my);
        }
        return zeilen;
    }

    private static double SMacht(SchnittEinstellung e) => Math.Clamp(e.MaxPower, 0, 100) / 100.0 * 1000;
}
