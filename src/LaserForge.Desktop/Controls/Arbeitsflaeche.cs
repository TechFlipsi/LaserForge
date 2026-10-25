using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;

namespace LaserForge.Desktop.Controls;

/// <summary>
/// M5-b: Interaktive Hauptarbeitsfläche nach docs/design-ziel-bild.md.
/// Zeichnet die Formen des Core-Projekts (Farbe je Layer-Palette), bietet
/// Klick-Auswahl mit Auswahlrahmen, Verschieben per Maus sowie das Ziehen
/// neuer Rechtecke/Ellipsen/Linien in Arbeitsflächen-mm (400×400, Ursprung
/// unten links). Maus-mm wird als Ereignis für die Statusleiste geliefert.
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
    private static readonly Color AuswahlFarbeT = Color.Parse("#FF9900");

    /// <summary>Projekt-Quelle für Zeichnung/Bearbeitung (aus dem Dokument der Fenster).</summary>
    public Projekt? Quelle { get ; set; }

    /// <summary>Dokument: Sichere() vor Mutation + Melden() nachher; vom Fenster gesetzt.</summary>
    public Dokument? Dok { get; set; }

    /// <summary>Aktives Werkzeug: "Auswahl" | "Rechteck" | "Ellipse" | "Linie".</summary>
    public string AktivesWerkzeug { get; set; } = "Auswahl";

    /// <summary>Layer-Index, in den neue Formen gezogen werden (Layer-Leiste-Klick).</summary>
    public int AktiverLayer { get; set; }

    /// <summary>Ausgewählte Formen (Weltkoordinaten-Bearbeitung).</summary>
    public readonly List<Form> Ausgewaehlt = new();

    /// <summary>Mausposition in mm — Statusleiste lebt davon.</summary>
    public event Action<double, double>? MausMM;

    /// <summary>Auswahl oder Geometrie geändert — Fenster aktualisiert Leisten/Preview.</summary>
    public event Action? Zustand;

    /// <summary>Rahmen (mm) der ARBEITSFLAECHE — für Preview-Zuschnitt u.ä.</summary>
    private const double lineal = 22.0;

    /// <summary>Aktuelle Anordnungsmaße: Ursprung in Pixel + Maßstab px/mm.</summary>
    private double ox, oy, masstab;

    // Zwischenzustand der Mausarbeit
    private enum MausModus { Nichts, FormZiehen, Verschieben }
    private MausModus modus = MausModus.Nichts;
    private Punkt startMM = new(0, 0);
    private Punkt vorMM = new(0, 0);
    private Form? geist;
    private List<Transformationsmatrix>? basisXFormen;
    private bool bewegt;

    private static FormattedText Beschriftung(string text) => new(
        text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 9.0, LinealText);

    // ================= Anordnung mm ⇄ Pixel =================

    /// <summary>Ursprung/Maßstab aus aktueller Größe berechnen (Render + Zeiger gleiche Logik).</summary>
    private void Berechnung(double w, double h)
    {
        double px = Math.Min((w - lineal - 10.0) / 400.0, (h - lineal - 10.0) / 400.0);
        if (px <= 0) px = 1;
        masstab = px;
        ox = lineal + 5.0 + (w - lineal - 10.0 - 400.0 * px) / 2.0;
        oy = (lineal + h) / 2.0 + 200.0 * px;
    }

    /// <summary>Arbeitsflächen-mm → Anzeige-Pixel (y gespiegelt: Origin unten links).</summary>
    public Point MMzuPx(Punkt p) => new(ox + p.X * masstab, oy - p.Y * masstab);

    /// <summary>Anzeige-Pixel → Arbeitsflächen-mm (kann außerhalb 0..400 liegen — erlaubt).</summary>
    public Punkt PxZuMM(Point p) => new((p.X - ox) / masstab, (oy - p.Y) / masstab);

    // ================= Zeichnen =================

    public override void Render(DrawingContext ctx)
    {
        double w = Math.Max(Bounds.Width, 1);
        double h = Math.Max(Bounds.Height, 1);
        Berechnung(w, h);

        // Helle Fläche füllen (alles unter den Linealen wird später überdeckt).
        ctx.DrawRectangle(FlaechenHell, null, new Rect(0, 0, w, h));

        // Feines Raster (5 mm), grobes Raster (10 mm) — wie M5-a.
        var rasterFeinPen = new Pen(RasterFein, 1);
        var rasterGrossPen = new Pen(RasterGross, 1.2);
        double schritt = 5.0 * masstab;
        int mmLinks = -(int)Math.Ceiling((ox - lineal) / schritt) * 5;
        int mmRechts = (int)Math.Floor((w - ox) / schritt) * 5;
        for (int mm = mmLinks; mm <= mmRechts; mm += 5)
        {
            double x = Math.Round(ox + mm * masstab) + 0.5;
            ctx.DrawLine(mm % 10 == 0 ? rasterGrossPen : rasterFeinPen, new Point(x, 0), new Point(x, h));
        }
        int mmUnten = -(int)Math.Ceiling((h - oy) / schritt) * 5;
        int mmOben = (int)Math.Floor((oy - lineal) / schritt) * 5;
        for (int mm = mmUnten; mm <= mmOben; mm += 5)
        {
            double y = Math.Round(oy - mm * masstab) + 0.5;
            ctx.DrawLine(mm % 10 == 0 ? rasterGrossPen : rasterFeinPen, new Point(lineal, y), new Point(w, y));
        }

        // Arbeitsbereich-Rahmen 400×400 mm hellgrau.
        ctx.DrawRectangle(null, new Pen(BereichRahmenFarbe, 2),
                          new Rect(ox, oy - 400.0 * masstab, 400.0 * masstab, 400.0 * masstab));

        // Grüner Ursprungspfeil (Startmarke, wie M5-a).
        var gruenPen = new Pen(PfeilGruen, 2);
        ctx.DrawLine(gruenPen, new Point(ox, oy), new Point(ox + 26, oy));
        Pfeildreieck(ctx, new Point(ox + 36, oy), new Point(ox + 25, oy - 4), new Point(ox + 25, oy + 4), PfeilGruen);
        ctx.DrawLine(gruenPen, new Point(ox, oy), new Point(ox, oy - 26));
        Pfeildreieck(ctx, new Point(ox, oy - 36), new Point(ox - 4, oy - 25), new Point(ox + 4, oy - 25), PfeilGruen);

        // ---- Formen des Core-Projekts: Farbe je Layer, verdeckte überspringen.
        if (Quelle is { } projekt)
        {
            foreach (var f in projekt.Formen)
            {
                if (f.Versteckt) continue;
                ZeichneForm(ctx, f, LayerFarben.VonIndex(f.SchnittIndex), false);
            }
        }

        // Geist des laufenden Ziehvorgangs (orange, gestrichelt).
        if (geist is { } g)
            ZeichneForm(ctx, g, AuswahlFarbeT, true);

        // ---- Auswahlrahmen (orange gestrichelt) je ausgewählter Form.
        var auswahlStift = new Pen(new SolidColorBrush(AuswahlFarbeT), 1, new DashStyle(new double[] { 4, 3 }, 0));
        foreach (var f in Ausgewaehlt)
        {
            var box = LayerFarben.BoxMM(f, 0.4);
            var ecke1 = MMzuPx(new Punkt(box[0], box[3]));
            var ecke2 = MMzuPx(new Punkt(box[2], box[1]));
            var rahmen = new Rect(Math.Min(ecke1.X, ecke2.X), Math.Min(ecke1.Y, ecke2.Y),
                                  Math.Abs(ecke2.X - ecke1.X), Math.Abs(ecke2.Y - ecke1.Y));
            ctx.DrawRectangle(null, auswahlStift, rahmen);

            // Ecken-Marken (kleine gefüllte Quadrate — visuelle Auswahl-Rahmen-Kanten).
            var eckeBrush = new SolidColorBrush(AuswahlFarbeT);
            foreach (var (ex, ey) in new (double, double)[]
                     { (rahmen.X, rahmen.Y), (rahmen.X + rahmen.Width, rahmen.Y),
                       (rahmen.X, rahmen.Y + rahmen.Height), (rahmen.X + rahmen.Width, rahmen.Y + rahmen.Height) })
                ctx.DrawRectangle(eckeBrush, null, new Rect(ex - 2.5, ey - 2.5, 5, 5));

            // Maß-Text der Auswahl oben am Rahmen (mm, deutsch).
            var ft = Beschriftung(
                string.Format(Dokument.Deutsch, "{0:0.0}×{1:0.0} mm", rahmen.Width / masstab, rahmen.Height / masstab));
            ctx.DrawText(ft, new Point(rahmen.X, rahmen.Y - ft.Height - 2));
        }

        // ---- Lineal oben + links (über allem).
        ctx.DrawRectangle(LinealFlaeche, null, new Rect(0, 0, w, lineal));
        ctx.DrawRectangle(LinealFlaeche, null, new Rect(0, 0, lineal, h));
        ctx.DrawLine(new Pen(LinealRand, 1), new Point(0, lineal + 0.5), new Point(w, lineal + 0.5));
        ctx.DrawLine(new Pen(LinealRand, 1), new Point(lineal + 0.5, 0), new Point(lineal + 0.5, h));

        double ticks = 10.0 * masstab;
        for (int mm = 0; ; mm += 10)
        {
            double x = ox + mm * masstab;
            if (x > w - 2) break;
            bool gross = mm % 40 == 0;
            ctx.DrawLine(new Pen(LinealStriche, 1.2),
                new Point(x + 0.5, lineal - (gross ? 10 : 6)), new Point(x + 0.5, lineal));
            if (gross)
            {
                var ft = Beschriftung(mm.ToString(CultureInfo.InvariantCulture));
                ctx.DrawText(ft, new Point(x - ft.Width / 2, 2));
            }
        }
        for (int mm = 0; ; mm += 10)
        {
            double y = oy - mm * masstab;
            if (y < lineal) break;
            bool gross = mm % 40 == 0;
            ctx.DrawLine(new Pen(LinealStriche, 1.2),
                new Point(lineal - (gross ? 10 : 6), y + 0.5), new Point(lineal, y + 0.5));
            if (gross)
            {
                var ft = Beschriftung(mm.ToString(CultureInfo.InvariantCulture));
                ctx.DrawText(ft, new Point(lineal - 12 - ft.Width, y - ft.Height / 2));
            }
        }
    }

    /// <summary>Zeichnet eine Form (Zuege → Pixel-Polylinien); optional gestrichelt (Geist).</summary>
    private void ZeichneForm(DrawingContext ctx, Form f, Color farbe, bool gestrichelt)
    {
        var stift = gestrichelt
            ? new Pen(new SolidColorBrush(farbe), 1.4, new DashStyle(new double[] { 4, 3 }, 0))
            : new Pen(new SolidColorBrush(farbe), 1.4);
        foreach (var z in f.ZuZuege())
        {
            if (z.Punkte.Count < 2) continue;
            var punkte = z.Punkte.Select(MMzuPx).ToArray();
            for (int i = 1; i < punkte.Length; i++)
                ctx.DrawLine(stift, punkte[i - 1], punkte[i]);
            if (z.Geschlossen && punkte.Length > 2)
                ctx.DrawLine(stift, punkte[^1], punkte[0]);
        }
    }

    /// <summary>Gefülltes Dreieck als Pfeilspitze.</summary>
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

    // ================= Auswahl-Hilfen =================

    /// <summary>Form an Punkt (mm) finden (von oben = zuletzt gezeichnet zuerst).</summary>
    public Form? Treffer(Punkt mm)
    {
        if (Quelle is null) return null;
        foreach (var f in Quelle.Formen.AsEnumerable().Reverse())
        {
            if (f.Versteckt) continue;
            var box = LayerFarben.BoxMM(f, 0.4);
            if (mm.X >= box[0] && mm.X <= box[2] && mm.Y >= box[1] && mm.Y <= box[3]) return f;
        }
        return null;
    }

    /// <summary>Auswahl neu setzen (Fenster aktualisiert Leisten via Zustand-Ereignis).</summary>
    public void AuswahlSetzen(IEnumerable<Form>? formen)
    {
        Ausgewaehlt.Clear();
        if (formen is not null) Ausgewaehlt.AddRange(formen);
        InvalidateVisual();
        Zustand?.Invoke();
    }

    public void AuswahlLeeren() => AuswahlSetzen(null);

    public void AlleAuswaehlen()
    {
        if (Quelle is { } p) AuswahlSetzen(p.Formen.Where(f => !f.Versteckt));
    }

    /// <summary>Ausgewählte Formen entfernen (Entf-Taste) mit Undo-Sicherung.</summary>
    public void AuswahlLoeschen()
    {
        if (Quelle is null || Dok is null || Ausgewaehlt.Count == 0) return;
        Dok.Sichere("Formen löschen");
        foreach (var f in Ausgewaehlt)
            Quelle.Formen.Remove(f);
        Ausgewaehlt.Clear();
        Dok.Melden();
        InvalidateVisual();
    }

    /// <summary>Form verschieben: Welt-Translation auf die aktuelle XForm schrauben.</summary>
    private static Transformationsmatrix SchraubeVerschoben(Transformationsmatrix alt, double dx, double dy) =>
        Transformationsmatrix.Verschieben(dx, dy) * alt;

    /// <summary>Alle Formen einer Layer-Nummer umschalten (Sichtbarkeit im Ebenen-Tab).</summary>
    public void LayerSichtbar(int index, bool sichtbar)
    {
        if (Quelle is null || Dok is null) return;
        Dok.Sichere("Layer sichtbar/versteckt");
        foreach (var f in Quelle.Formen.Where(f => f.SchnittIndex == index))
            f.Versteckt = !sichtbar;
        // Auch die Core-Schnitt-Zeile merkt sich den Sichtbarkeitsstand.
        var schnitt = Quelle.Schnitte.FirstOrDefault(s => s.Index == index);
        if (schnitt is not null) schnitt.Versteckt = !sichtbar;
        Dok.Melden();
        InvalidateVisual();
    }

    // ================= Maus-Interaktion =================

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetCurrentPoint(this).Position;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        e.Pointer.Capture(this);
        var mm = PxZuMM(pos);
        startMM = mm;
        vorMM = mm;
        bewegt = false;

        switch (AktivesWerkzeug)
        {
            case "Rechteck":
                geist = new RechteckForm { SchnittIndex = AktiverLayer };
                StarteGeist(mm);
                break;
            case "Ellipse":
                geist = new EllipseForm { SchnittIndex = AktiverLayer };
                StarteGeist(mm);
                break;
            case "Linie":
                var l = new PfadForm { SchnittIndex = AktiverLayer };
                l.Rohtuege.Add(new Zug(new List<Punkt> { mm, mm }, false));
                geist = l;
                StarteGeist(mm);
                break;
            default: // Auswahl
                var t = Treffer(mm);
                if (t is null)
                {
                    AuswahlLeeren();
                }
                else
                {
                    if (!Ausgewaehlt.Contains(t))
                        AuswahlSetzen(new[] { t });
                    modus = MausModus.Verschieben;
                    basisXFormen = Ausgewaehlt.Select(f => f.XForm).ToList();
                    e.Handled = true;
                }
                break;
        }
        InvalidateVisual();
    }

    /// <summary>Geist-Umgebung starten (Startpunkt merken, Modus setzen).</summary>
    private void StarteGeist(Punkt mm)
    {
        modus = MausModus.FormZiehen;
        startMM = mm;
        vorMM = mm;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetCurrentPoint(this).Position;
        var mm = PxZuMM(pos);
        MausMM?.Invoke(mm.X, mm.Y);

        if (modus == MausModus.Nichts) return;
        switch (modus)
        {
            case MausModus.FormZiehen:
                AktualisiereGeist(mm);
                break;
            case MausModus.Verschieben when Ausgewaehlt.Count > 0:
                {
                    // Undo-Abdruck EINMAL vor der ersten echten Verschiebung.
                    if (!bewegt && Dok is not null)
                        Dok.Sichere("Verschieben");
                    bewegt = true;
                    double dx = mm.X - startMM.X, dy = mm.Y - startMM.Y;
                    for (int i = 0; i < Ausgewaehlt.Count && basisXFormen is not null; i++)
                        Ausgewaehlt[i].XForm = SchraubeVerschoben(basisXFormen[i], dx, dy);
                    break;
                }
        }
        InvalidateVisual();
        vorMM = mm;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        e.Pointer.Capture(null);
        switch (modus)
        {
            case MausModus.FormZiehen:
                if (geist is { } g && GeistTauglich(g))
                {
                    Dok?.Sichere("Form zeichnen");
                    Quelle?.Formen.Add(g);
                    Dok?.FehlendeLayerAnlegen();
                    AuswahlSetzen(new[] { g });
                    Zustand?.Invoke();
                }
                geist = null;
                break;
            case MausModus.Verschieben:
                if (bewegt) Zustand?.Invoke();
                basisXFormen = null;
                break;
        }
        modus = MausModus.Nichts;
        InvalidateVisual();
    }

    /// <summary>Geist real groß genug (kein Mikrozucken-Formular).</summary>
    private static bool GeistTauglich(Form geistform) => geistform switch
    {
        RechteckForm r => r.W > 0.5 && r.H > 0.5,
        EllipseForm el => el.Rx > 0.25 && el.Ry > 0.25,
        PfadForm p => p.Rohtuege.Count > 0 && p.Rohtuege[0].Laenge() > 0.5,
        _ => false,
    };

    /// <summary>Geometrie des Geistes je Werkzeug an den neuen Maus-punkt anpassen.</summary>
    private void AktualisiereGeist(Punkt mm)
    {
        switch (geist)
        {
            case RechteckForm r:
            {
                double x0 = Math.Min(startMM.X, mm.X), x1 = Math.Max(startMM.X, mm.X);
                double y0 = Math.Min(startMM.Y, mm.Y), y1 = Math.Max(startMM.Y, mm.Y);
                r.W = x1 - x0;
                r.H = y1 - y0;
                r.Eckradius = 0;
                r.XForm = Transformationsmatrix.Verschieben((x0 + x1) / 2, (y0 + y1) / 2);
                break;
            }
            case EllipseForm el:
            {
                double x0 = Math.Min(startMM.X, mm.X), x1 = Math.Max(startMM.X, mm.X);
                double y0 = Math.Min(startMM.Y, mm.Y), y1 = Math.Max(startMM.Y, mm.Y);
                el.Rx = (x1 - x0) / 2;
                el.Ry = (y1 - y0) / 2;
                el.XForm = Transformationsmatrix.Verschieben((x0 + x1) / 2, (y0 + y1) / 2);
                break;
            }
            case PfadForm p when p.Rohtuege.Count == 1:
                var z = p.Rohtuege[0];
                z.Punkte[1] = mm;
                break;
        }
    }
}