using System;
using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;

namespace LaserForge.Desktop;

/// <summary>
/// M5-b: Zentrale des offenen Dokuments — hält das Core-Projekt, den Dateipfad
/// und die Undo/Redo-Stapel (tiefe Projekt-Klone). Alle UI-Änderungen laufen
/// live auf die Core-Objekte; vor jeder Mutation ein Klone-Abdruck in den Undo-Stapel.
/// </summary>
public sealed class Dokument
{
    /// <summary>Standard-Kultur für deutsch formatierte Zahlen (Komma).</summary>
    public static readonly System.Globalization.CultureInfo Deutsch =
        System.Globalization.CultureInfo.GetCultureInfo("de-DE");

    private readonly Stack<Projekt> undoStapel = new();
    private readonly Stack<Projekt> redoStapel = new();

    /// <summary>Aktuelles Core-Projekt (lebt live in Canvas/Tab).</summary>
    public Projekt Projekt { get; private set; } = NeueLeere();

    /// <summary>Gespeicherter Dateipfad ("" = noch nie gespeichert).</summary>
    public string Dateipfad { get; set; } = "";

    /// <summary>Fenster-Titel: &lt;Dateiname&gt; - GRBL - LaserForge.</summary>
    public string Titel =>
        (Dateipfad.Length > 0 ? System.IO.Path.GetFileName(Dateipfad) : "Unbenannt")
        + " - GRBL - LaserForge";

    /// <summary>UI-Ereignis nach Zustandswechsel (Canvas/Layer/Preview aktualisieren).</summary>
    public event Action? ZustandGeaendert;

    // ---------- Projekte

    /// <summary>Leeres Standardprojekt mit den beiden Bibel-Layern (00 Ein, 01 Aus).</summary>
    public static Projekt NeueLeere()
    {
        var p = new Projekt { BreiteMM = 400, HoeheMM = 400 };
        p.Schnitte.Add(new SchnittEinstellung { Index = 0, NameSektion = "Layer 00", Typ = "Cut" });
        p.Schnitte.Add(new SchnittEinstellung { Index = 1, NameSektion = "Layer 01", Typ = "Cut", Aktiviert = false });
        return p;
    }

    /// <summary>Tiefer Klone eines Core-Projekts (manuell, ohne Datei-Roundtrip).</summary>
    public static Projekt Klone(Projekt p)
    {
        var k = new Projekt
        {
            AppVersion = p.AppVersion,
            ThumbnailBasis64 = p.ThumbnailBasis64,
            FormatVersion = p.FormatVersion,
            MaterialHeight = p.MaterialHeight,
            MirrorX = p.MirrorX,
            MirrorY = p.MirrorY,
            BreiteMM = p.BreiteMM,
            HoeheMM = p.HoeheMM,
        };
        if (p.VariableText is { } vt)
            k.VariableText = new VariableTextEinstellungen
            {
                Start = vt.Start, Ende = vt.Ende, Aktueller = vt.Aktueller,
                Schritt = vt.Schritt, AutoWeiterschalten = vt.AutoWeiterschalten,
            };
        if (p.Notizen is { } nz)
            k.Notizen = new Notizen { BeiLadenZeigen = nz.BeiLadenZeigen, Text = nz.Text };
        foreach (var (name, wert) in p.UIPrefs) k.UIPrefs[name] = wert;
        foreach (var n in p.RestKnoten) k.RestKnoten.Add(new System.Xml.Linq.XElement(n));
        foreach (var s in p.Schnitte)
        {
            var c = new SchnittEinstellung
            {
                Index = s.Index, NameSektion = s.NameSektion, Typ = s.Typ,
                Aktiviert = s.Aktiviert, MinPower = s.MinPower, MaxPower = s.MaxPower,
                MinPower2 = s.MinPower2, MaxPower2 = s.MaxPower2, Speed = s.Speed,
                Prioritaet = s.Prioritaet, NumPasses = s.NumPasses,
                ZProPass = s.ZProPass, ZOffset = s.ZOffset, LineSpacing = s.LineSpacing,
                ScanAngle = s.ScanAngle, RunBlower = s.RunBlower, KerfWidth = s.KerfWidth,
                Overcut = s.Overcut, Frequency = s.Frequency, Versteckt = s.Versteckt,
            };
            foreach (var (name, wert) in s.RestParameter) c.RestParameter.Add((name, wert));
            k.Schnitte.Add(c);
        }
        foreach (var f in p.Formen) k.Formen.Add(KloneForm(f));
        return k;
    }

    private static Form KloneForm(Form f) => f switch
    {
        RechteckForm r => new RechteckForm
        {
            W = r.W, H = r.H, Eckradius = r.Eckradius,
            XForm = r.XForm, SchnittIndex = r.SchnittIndex, Versteckt = r.Versteckt, Name = r.Name,
        },
        EllipseForm e => new EllipseForm
        {
            Rx = e.Rx, Ry = e.Ry,
            XForm = e.XForm, SchnittIndex = e.SchnittIndex, Versteckt = e.Versteckt, Name = e.Name,
        },
        PolygonForm q => new PolygonForm
        {
            Rx = q.Rx, Ry = q.Ry, Seiten = q.Seiten,
            XForm = q.XForm, SchnittIndex = q.SchnittIndex, Versteckt = q.Versteckt, Name = q.Name,
        },
        TextForm t => new TextForm
        {
            Zeichensatz = t.Zeichensatz, Text = t.Text, Hoehe = t.Hoehe,
            XForm = t.XForm, SchnittIndex = t.SchnittIndex, Versteckt = t.Versteckt, Name = t.Name,
        },
        BildForm b => new BildForm
        {
            Grauwerte = (byte[])b.Grauwerte.Clone(), BreitePix = b.BreitePix,
            HoehePix = b.HoehePix, PixelMM = b.PixelMM, Schwelle = b.Schwelle,
            XForm = b.XForm, SchnittIndex = b.SchnittIndex, Versteckt = b.Versteckt, Name = b.Name,
        },
        PfadForm p => new PfadForm
        {
            Rohtuege = p.Rohtuege.Select(z => new Zug(z.Punkte.ToList(), z.Geschlossen)).ToList(),
            RoheVerte = p.RoheVerte.Select(v => new PfadVertex(v.Punkt, v.C0, v.C1, v.Glatt)).ToList(),
            RohePrimse = p.RohePrimse.Select(x => new PfadPrimitive(x.Typ, x.P0, x.P1)).ToList(),
            XForm = p.XForm, SchnittIndex = p.SchnittIndex, Versteckt = p.Versteckt, Name = p.Name,
        },
        GruppeForm g => new GruppeForm
        {
            Kinder = g.Kinder.Select(KloneForm).ToList(),
            XForm = g.XForm, SchnittIndex = g.SchnittIndex, Versteckt = g.Versteckt, Name = g.Name,
        },
        _ => throw new NotSupportedException("Unbekannte Form kann nicht geklont werden: " + f.Rohtyp()),
    };

    // ---------- Undo/Redo

    /// <summary>Vor einer Mutation aufrufen: aktuelle Projekt-Kopie in den Undo-Stapel.</summary>
    public void Sichere(string _grund)
    {
        undoStapel.Push(Klone(Projekt));
        if (undoStapel.Count > 60) undoStapel.TrimExcess();   // Grenze halten (Klone-Speicher)
        redoStapel.Clear();
    }

    public bool HatUndo => undoStapel.Count > 0;
    public bool HatRedo => redoStapel.Count > 0;

    /// <summary>Zurück zum letzten Abdruck; aktueller Stand wandert in den Redo-Stapel.</summary>
    public void Undo()
    {
        if (undoStapel.Count > 0)
        {
            redoStapel.Push(Klone(Projekt));
            Projekt = undoStapel.Pop();
            ZustandGeaendert?.Invoke();
        }
    }

    /// <summary>Wiederherstellen einer zurückgenommenen Änderung.</summary>
    public void Redo()
    {
        if (redoStapel.Count > 0)
        {
            undoStapel.Push(Klone(Projekt));
            Projekt = redoStapel.Pop();
            ZustandGeaendert?.Invoke();
        }
    }

    // ---------- Zustand

    /// <summary>Projekt-Referenz setzen (Neu/Öffnen/Laden) + UI-Abonnenten.</summary>
    public void Setze(Projekt p, string dateipfad)
    {
        Projekt = p;
        Dateipfad = dateipfad;
        undoStapel.Clear();
        redoStapel.Clear();
        ZustandGeaendert?.Invoke();
    }

    /// <summary>Weitreichende Zustandsmeldung (Canvas/Preview/Layer frisch zeichnen).</summary>
    public void Melden() => ZustandGeaendert?.Invoke();

    /// <summary>Layer-Einträge nachtragen, auf die Formen verweisen (z.B. fremde .lbrn).</summary>
    public void FehlendeLayerAnlegen()
    {
        foreach (var index in Projekt.Formen.Select(f => f.SchnittIndex).Distinct())
        {
            if (!Projekt.Schnitte.Any(s => s.Index == index))
                Projekt.Schnitte.Add(new SchnittEinstellung
                {
                    Index = index,
                    NameSektion = $"Layer {index:00}",
                    Typ = "Cut",
                });
        }
    }
}