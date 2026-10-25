using System.Globalization;
using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Pipeline;
/// <summary>
/// Optionen fuer die GRBL-Flavor-Erzeugung. S-Skala 0..1000 (GRBL-Standard PWM, $30=1000).
/// Alle Mengen intern in mm (%), mm/s; ausgegeben werden mm und mm/min.
/// </summary>
public sealed class GCodeOptionen
{
    public double MaxPowerS { get; set; } = 1000;
    public bool HomingAmStart { get; set; }
    public bool ParkenAmEnde { get; set; } = true;
    public double RahmenPower { get; set; } = 2;
    public double RahmenSpeed { get; set; } = 25;
    public bool RahmenEinfuegen { get; set; }
}
/// <summary>
/// GRBL-Flavor: Kopf (G21/G90/G17, optional $H), Konturen (G0 - M4 S - G1 ... - M5),
/// Scan-Raster (bidi-Zeilen nach lineSpacing), optional Rahmen, Fuss (M5 + Parken).
/// </summary>
public static class GCodeErzeuger
{
    private static readonly CultureInfo E = CultureInfo.InvariantCulture;
    public static string Zahl(double v) => v.ToString("0.###", E);

    public static List<string> Erzeuge(Projekt projekt, GCodeOptionen? opt = null)
    {
        opt ??= new();
        var zeilen = new List<string> { "G21", "G90", "G17" };
        if (opt.HomingAmStart) zeilen.Add("$H");
        foreach (var form in projekt.Formen)
        {
            if (form.Versteckt) continue;
            var e = projekt.Schnitte.FirstOrDefault(s => s.Index == form.SchnittIndex)
                    ?? new SchnittEinstellung { Index = form.SchnittIndex };
            if (!e.Aktiviert) continue;
            var zuege = Optimierer.Ordne(form.ZuZuege());
            if (zuege.Count == 0) continue;
            if (e.Typ is "Scan" or "Fill") zeilen.AddRange(Raster(zuege, e, opt));
            else foreach (var z in zuege) zeilen.AddRange(Kontur(z, e, opt));
        }
        if (opt.RahmenEinfuegen)
            zeilen.AddRange(Rahmen(0, 0, projekt.BreiteMM, projekt.HoeheMM, opt));
        zeilen.Add("M5");
        if (opt.ParkenAmEnde) zeilen.Add("G0 X0 Y0");
        return zeilen;
    }

    /// <summary>Eine Kontur als GCode (inkl. Passe-Wiederholung).</summary>
    public static List<string> Kontur(Zug zug, SchnittEinstellung e, GCodeOptionen o)
    {
        var zeilen = new List<string>();
        if (zug.Punkte.Count < 2) return zeilen;
        var s = Zahl(SMacht(e));
        var f = Zahl(e.Speed * 60.0);
        var start = zug.Punkte[0];
        int passMax = Math.Max(1, e.NumPasses);
        zeilen.Add($"G0 X{Zahl(start.X)} Y{Zahl(start.Y)}");
        for (int pass = 0; pass < passMax; pass++)
        {
            zeilen.Add($"M4 S{s}");
            for (int j = 1; j < zug.Punkte.Count; j++)
            {
                var p = zug.Punkte[j];
                var zeile = $"G1 X{Zahl(p.X)} Y{Zahl(p.Y)}";
                if (j == 1 && pass == 0) zeile += $" F{f}";
                zeilen.Add(zeile);
            }
            zeilen.Add("M5");
            if (pass + 1 < passMax) zeilen.Add($"G0 X{Zahl(start.X)} Y{Zahl(start.Y)}");
        }
        return zeilen;
    }

    /// <summary>Scan-Raster ueber die Umriesse (bidi-Zeilen, lineSpacing); M3: Kontur-Clip.</summary>
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
        bool bidi = true;
        for (double ry = minY; ry <= maxY + 1e-9; ry += abstand)
        {
            var von = bidi ? new Punkt(minX, ry) : new Punkt(maxX, ry);
            var bis = bidi ? new Punkt(maxX, ry) : new Punkt(minX, ry);
            zeilen.Add($"G0 X{Zahl(von.X)} Y{Zahl(von.Y)}");
            zeilen.Add($"M4 S{Zahl(SMacht(e))}");
            zeilen.Add($"G1 X{Zahl(bis.X)} Y{Zahl(bis.Y)} F{Zahl(e.Speed * 60.0)}");
            zeilen.Add("M5");
            bidi = !bidi;
        }
        return zeilen;
    }

    /// <summary>Arbeitsflaeche-Rahmen (Frame) mit geringer Power.</summary>
    public static List<string> Rahmen(double x, double y, double w, double h, GCodeOptionen o)
    {
        var zeilen = new List<string>();
        var ecken = new[] { new Punkt(x, y), new Punkt(x + w, y), new Punkt(x + w, y + h), new Punkt(x, y + h), new Punkt(x, y) };
        zeilen.Add($"G0 X{Zahl(x)} Y{Zahl(y)}");
        zeilen.Add($"M4 S{Zahl(Math.Clamp(o.RahmenPower, 0, 100) / 100 * o.MaxPowerS)}");
        for (int i = 1; i < ecken.Length; i++)
            zeilen.Add($"G1 X{Zahl(ecken[i].X)} Y{Zahl(ecken[i].Y)} F{Zahl(o.RahmenSpeed * 60.0)}");
        zeilen.Add("M5");
        return zeilen;
    }
    private static double SMacht(SchnittEinstellung e) => Math.Clamp(e.MaxPower, 0, 100) / 100.0 * 1000;
}
