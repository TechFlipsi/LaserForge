using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Pipeline;
/// <summary>
/// Brenn-Preview-Simulation (M6-Kern): Konturen + Scan-Raster auf Materialfarbe,
/// plus Fahrtzeit-Abschaetzung (Konturen/Scanlaufen + Luftfahrten via Optimierer-Kette).
/// Output = Grauwerte (0..255, 0 = gebrannt) in Pixel pro mm (aufslich).
/// </summary>
public sealed class PreviewErg
{
    public byte[] Grau { get; init; } = Array.Empty<byte>();
    public int BreitePix { get; init; }
    public int HoehePix { get; init; }
    public double Sekunden { get; init; }
    public double FahrtenSekunden { get; init; }
}
public static class PreviewSimulator
{
    private const double PixelProMM = 1.0;   // Preview-aufloesung (feiner via Option spater)
    public static PreviewErg Simuliere(Projekt projekt)
    {
        double breiteMM = projekt.BreiteMM, hoeheMM = projekt.HoeheMM;
        int breitePix = (int)Math.Ceiling(breiteMM * PixelProMM);
        int hoehePix = (int)Math.Ceiling(hoeheMM * PixelProMM);
        var grau = new byte[breitePix * hoehePix];
        Array.Fill(grau, (byte)255);
        double sekunden = 0;
        double fahrten = 0;
        Punkt letzterPunkt = new(0, hoeheMM);  // Start = unten links Ursprung
        foreach (var form in projekt.Formen)
        {
            if (form.Versteckt) continue;
            var e = projekt.Schnitte.FirstOrDefault(s => s.Index == form.SchnittIndex)
                    ?? new SchnittEinstellung { Index = form.SchnittIndex };
            if (!e.Aktiviert) continue;
            if (form is BildForm bild)
            {
                // Bild-Segmente auf Preview (mm-Aufloesung)
                var zuege = GCodeBildZuege(bild, e);
                foreach (var z in zuege)
                {
                    sekunden += z.Laenge() / Math.Max(1e-3, e.Speed);
                    Zeichne(grau, breitePix, z, breiteMM, hoeheMM);
                }
                continue;
            }
            var pfadzuege = Optimierer.Ordne(form.ZuZuege());
            if (pfadzuege.Count == 0) continue;
            if (e.Typ is "Scan" or "Fill")
            {
                // Raster-Zeilen: Abstand = max(0.1, lineSpacing)
                double abstand = Math.Max(0.1, e.LineSpacing);
                double minY = pfadzuege.SelectMany(z => z.Punkte).Min(p => p.Y);
                double maxY = pfadzuege.SelectMany(z => z.Punkte).Max(p => p.Y);
                double minX = pfadzuege.SelectMany(z => z.Punkte).Min(p => p.X);
                double maxX = pfadzuege.SelectMany(z => z.Punkte).Max(p => p.X);
                for (double ry = minY; ry < maxY - 1e-9; ry += abstand)
                {
                    var zug = new Zug(new[] { new Punkt(minX, ry), new Punkt(maxX, ry) }, false);
                    sekunden += zug.Laenge() / Math.Max(1e-3, e.Speed);
                    sekunden += abstand / 60.0; // Spaltenfahrt (G0 im Scan, schnelle Achse)
                    Zeichne(grau, breitePix, zug, breiteMM, hoeheMM);
                }
            }
            else
            {
                foreach (var z in pfadzuege)
                {
                    fahrten += z.Startpunkt().EntfernungZu(letzterPunkt);
                    for (int pass = 0; pass < Math.Max(1, e.NumPasses); pass++)
                        sekunden += z.Laenge() / Math.Max(1e-3, e.Speed);
                    Zeichne(grau, breitePix, z, breiteMM, hoeheMM);
                    letzterPunkt = z.Startpunkt();
                }
            }
        }
        fahrten += new Punkt(0, 0).EntfernungZu(letzterPunkt) * 0.5;
        double fahrtenSekunden = fahrten / 120.0;  // Luftfahrt-Standard 120 mm/s
        return new PreviewErg { Grau = grau, BreitePix = breitePix, HoehePix = hoehePix,
                                Sekunden = sekunden, FahrtenSekunden = fahrtenSekunden };
    }
    private static List<Zug> GCodeBildZuege(BildForm bild, SchnittEinstellung e)
    {
        var liste = new List<Zug>();
        double abstand = Math.Max(bild.PixelMM, e.LineSpacing);
        var xf = bild.XForm;
        for (double my = 0; my < bild.HoehePix * bild.PixelMM - 1e-9; my += abstand)
        {
            int pixZeile = Math.Clamp((int)Math.Round(my / bild.PixelMM), 0, bild.HoehePix - 1);
            double? startX = null;
            for (int px = 0; px < bild.BreitePix; px++)
            {
                bool dunkel = bild.PixelDunkel(px, pixZeile);
                if (dunkel && startX is null) startX = px * bild.PixelMM;
                else if (!dunkel && startX is not null)
                {
                    liste.Add(new Zug(new[] { xf.Anwenden(new Punkt(startX.Value, my)), xf.Anwenden(new Punkt(px * bild.PixelMM, my)) }, false));
                    startX = null;
                }
            }
            if (startX is not null)
                liste.Add(new Zug(new[] { xf.Anwenden(new Punkt(startX.Value, my)), xf.Anwenden(new Punkt(bild.BreitePix * bild.PixelMM, my)) }, false));
        }
        return liste;
    }
    private static void Zeichne(byte[] grau, int breitePix, Zug z, double breiteMM, double hoeheMM)
    {
        double maßstab = PixelProMM;
        for (int i = 1; i < z.Punkte.Count; i++)
        {
            var a = z.Punkte[i - 1]; var b = z.Punkte[i];
            if (a.Y < 0 || a.X < 0 || a.X > breiteMM || a.Y > hoeheMM) continue;  // aussen = ignorieren
            Rasterer.Linie(grau, breitePix, a.X * maßstab, hoeheMM - a.Y * maßstab, b.X * maßstab, hoeheMM - b.Y * maßstab);
        }
    }
}
