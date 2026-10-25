using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using LaserForge.Core.Einstellungen;
using LaserForge.Core.Formate;
using LaserForge.Core.Formen;
using LaserForge.Core.Geometrie;
using LaserForge.Core.Pipeline;
using LaserForge.Desktop.Controls;

namespace LaserForge.Desktop;

/// <summary>
/// Hauptfenster M5-a/M5-b: Aufbau nach docs/design-ziel-bild.md.
/// M5-b-Welle: Ebenen-Tab mit echter Live-Liste aus dem Core-Projekt,
/// Canvas-Werkzeuge (Zeichnen/Auswahl/Verschieben) mit Undo/Redo,
/// Datei-IO über SchreiberLbrn/LeserLbrn, Preview-Tab mit dem echten
/// Core-PreviewSimulator, gesperrte Konsole ohne Verbindung und live Maus-mm.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Tab-Namen in Bibel-Reihenfolge + Vorschau-Zusatz (M5-b).</summary>
    private static readonly string[] TabNamen =
    {
        "Schnitte/Ebenen", "Bewegen", "Konsole", "Makros", "Kamera/Overlays", "Seiten", "Vorschau",
    };

    /// <summary>Modus-Werte des Ebenen-Tabs (Auftrag A).</summary>
    private static readonly string[] ModusWerte = { "Cut", "Scan", "Fill", "Score" };

    private readonly Dokument dokument = new();

    /// <summary>6 Bibel-Tabs + Vorschau-Knopf.</summary>
    private Button[] tabKnopfe = null!;

    private int aktiverTab = 2;

    /// <summary>Layer-Nummer für neue Formen (Klick in der Layer-Leiste).</summary>
    private int aktiverLayer = 0;

    /// <summary>Aktueller Zusatz-Drehwinkel der Auswahl (Eigenschaften-Leiste).</summary>
    private double drehWinkel;

    public MainWindow()
    {
        InitializeComponent();
        tabKnopfe = new[] { TabKnopf0, TabKnopf1, TabKnopf2, TabKnopf3, TabKnopf4, TabKnopf5, TabKnopf6 };
        dokument.ZustandGeaendert += OnDokZustand;
        dokument.DokumentGewechselt += OnDokGewechselt;

        Flaeche.Dok = dokument;
        Flaeche.Quelle = dokument.Projekt;
        Flaeche.MausMM += OnMausMM;
        Flaeche.Zustand += OnFlaecheZustand;

        BaueLayerleiste();
        BaueEbenenListe();
        KonsoleAnfang();
        PropLeisteVerdrahten();

        aktiverTab = Math.Clamp(StartEinstellung.Tab, 0, TabNamen.Length - 1);
        if (StartEinstellung.Maximieren)
        {
            WindowState = WindowState.Maximized;
        }
        AktualisiereTabs();

        // QA-/Komfort-Startdatei (echter LeserLbrn-Pfad).
        if (StartEinstellung.Datei.Length > 0)
        {
            LadeProjektdatei(StartEinstellung.Datei);
        }
        if (StartEinstellung.AuswahlAlle)
        {
            Flaeche.AlleAuswaehlen();
        }
        TitelAnzeigen();
        PropLeisteBeleben();
    }

    // ================================================== Layer-Leiste

    /// <summary>Baut die untere Layer-Leiste: Farbchips 00..30 + T1/T2, Klick = aktiven Layer wählen.</summary>
    private void BaueLayerleiste()
    {
        Layerleiste.Children!.Clear();
        for (int i = 0; i < LayerFarben.Palette.Length; i++)
        {
            var (nummer, hex) = LayerFarben.Palette[i];
            var farbe = Color.Parse(hex);
            int chipIndex = i > 30 ? i : i;   // T1/T2 = 31/32 (Werkzeug-Layer)
            var chip = new Border
            {
                Width = 30,
                Height = 22,
                Margin = new Thickness(1, 0),
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(farbe),
                BorderBrush = new SolidColorBrush(Color.Parse("#5A5A5A")),
                BorderThickness = new Thickness(1),
                Tag = chipIndex,
                Child = new TextBlock
                {
                    Text = nummer,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(LayerFarben.Textfarbe(farbe)),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                },
            };
            chip.Tapped += OnLayerChip;
            Layerleiste.Children.Add(chip);
        }
        MaLayerChipMarkieren();
    }

    /// <summary>Layer-Chip-Klick → aktive Layer-Nummer für neue Formen.</summary>
    private void OnLayerChip(object? sender, RoutedEventArgs e)
    {
        if (sender is Border chip && chip.Tag is int index)
        {
            aktiverLayer = index;
            Flaeche.AktiverLayer = index;
            MaLayerChipMarkieren();
        }
    }

    /// <summary>Aktiv-Markierung (orange Rand) über alle Layer-Chips.</summary>
    private void MaLayerChipMarkieren()
    {
        for (int i = 0; i < Layerleiste.Children!.Count; i++)
        {
            if (Layerleiste.Children[i] is Border chip)
            {
                bool aktiv = i == aktiverLayer;
                chip.BorderThickness = new Thickness(aktiv ? 2 : 1);
                chip.BorderBrush = new SolidColorBrush(aktiv ? Color.Parse("#FF9900") : Color.Parse("#5A5A5A"));
            }
        }
    }

    // ================================================== Tabs

    /// <summary>Klick auf einen der 7 Tab-Knöpfe → Inhalt wechseln.</summary>
    private void OnTabKnopf(object? sender, RoutedEventArgs e)
    {
        if (sender is Button knopf && knopf.Parent is StackPanel leiste)
        {
            aktiverTab = leiste.Children.IndexOf(knopf);
            AktualisiereTabs();
        }
    }

    /// <summary>Sichtbarkeiten + Knopfmarkierung je aktiver Tab; Vorschau bei Betritt frisch.</summary>
    private void AktualisiereTabs()
    {
        if (tabKnopfe is null)
        {
            return;
        }
        aktiverTab = Math.Clamp(aktiverTab, 0, TabNamen.Length - 1);
        if (TabTitel is not null)
        {
            TabTitel.Text = TabNamen[aktiverTab];
        }
        if (TabEbenen is not null) TabEbenen.IsVisible = aktiverTab == 0;
        if (TabBewegen is not null) TabBewegen.IsVisible = aktiverTab == 1;
        if (TabKonsole is not null) TabKonsole.IsVisible = aktiverTab == 2;
        if (TabMakros is not null) TabMakros.IsVisible = aktiverTab == 3;
        if (TabKamera is not null) TabKamera.IsVisible = aktiverTab == 4;
        if (TabSeiten is not null) TabSeiten.IsVisible = aktiverTab == 5;
        if (TabVorschau is not null) TabVorschau.IsVisible = aktiverTab == 6;
        for (int i = 0; i < tabKnopfe.Length; i++)
        {
            var knopf = tabKnopfe[i];
            if (knopf is null) continue;
            knopf.Classes.Remove("aktiv");
            if (i == aktiverTab)
            {
                knopf.Classes.Add("aktiv");
            }
        }
        if (aktiverTab == 6)
        {
            VorschauNeu();
        }
    }

    // ================================================== Werkzeuge

    private void OnWerkzeugAuswahl(object? s, RoutedEventArgs e) => SetzeWerkzeug("Auswahl");
    private void OnWerkzeugRechteck(object? s, RoutedEventArgs e) => SetzeWerkzeug("Rechteck");
    private void OnWerkzeugEllipse(object? s, RoutedEventArgs e) => SetzeWerkzeug("Ellipse");
    private void OnWerkzeugLinie(object? s, RoutedEventArgs e) => SetzeWerkzeug("Linie");

    /// <summary>Aktiv-Werkzeug markieren (4 zeichenfähige, Rest bleibt Platzhalter).</summary>
    private void SetzeWerkzeug(string werkzeug)
    {
        Flaeche.AktivesWerkzeug = werkzeug;
        foreach (var b in new[] { WzAuswahl, WzRechteck, WzEllipse, WzLinie })
        {
            b.Classes.Remove("aktiv");
        }
        var aktiv = werkzeug switch
        {
            "Rechteck" => WzRechteck,
            "Ellipse" => WzEllipse,
            "Linie" => WzLinie,
            _ => WzAuswahl,
        };
        aktiv.Classes.Add("aktiv");
        StatusWerkzeug.Text = "Werkzeug: " + werkzeug;
    }

    // ================================================== Tasten

    /// <summary>Werkzeug- und Befehls-Hotkeys (Bibel-F2/F3/F5 + Strg-Befehle).</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Source is TextBox)
        {
            base.OnKeyDown(e);
            return;
        }
        switch (e.Key)
        {
            case Key.F2: SetzeWerkzeug("Rechteck"); break;
            case Key.F3: SetzeWerkzeug("Ellipse"); break;
            case Key.F5: SetzeWerkzeug("Linie"); break;
            case Key.Delete: Flaeche.AuswahlLoeschen(); break;
            case Key.N when e.KeyModifiers.HasFlag(KeyModifiers.Control): NeuProjekt(); break;
            case Key.O when e.KeyModifiers.HasFlag(KeyModifiers.Control): OeffnenDialog(); break;
            case Key.S when e.KeyModifiers.HasFlag(KeyModifiers.Control): SpeichernDialog(); break;
            case Key.Z when e.KeyModifiers.HasFlag(KeyModifiers.Control): Undo(); break;
            case Key.Y when e.KeyModifiers.HasFlag(KeyModifiers.Control): Redo(); break;
            case Key.A when e.KeyModifiers.HasFlag(KeyModifiers.Control): Flaeche.AlleAuswaehlen(); break;
            default:
                base.OnKeyDown(e);
                return;
        }
        e.Handled = true;
    }

    // ================================================== Undo/Redo/Löschen

    private void OnUndo(object? s, RoutedEventArgs e) => Undo();
    private void OnRedo(object? s, RoutedEventArgs e) => Redo();
    private void OnAuswahlLoeschen(object? s, RoutedEventArgs e) => Flaeche.AuswahlLoeschen();

    private void Undo()
    {
        if (dokument.HatUndo)
        {
            dokument.Undo();
            StatusMeldung.Text = "Rückgängig";
        }
    }

    private void Redo()
    {
        if (dokument.HatRedo)
        {
            dokument.Redo();
            StatusMeldung.Text = "Wiederholt";
        }
    }

    // ================================================== Dokument-Bindung

    /// <summary>Leichte Dokument-Meldung → Canvas/Preview/Menüfähigkeit frisch.</summary>
    private void OnDokZustand()
    {
        Flaeche.InvalidateVisual();
        if (aktiverTab == 6)
        {
            VorschauNeu();
        }
        MenuUndo.IsEnabled = dokument.HatUndo;
        MenuRedo.IsEnabled = dokument.HatRedo;
        MenuLoeschen.IsEnabled = Flaeche.Ausgewaehlt.Count > 0;
    }

    /// <summary>Dokument gewechselt (Neu/Öffnen/Undo/Redo) → komplette Aufrüstung.</summary>
    private void OnDokGewechselt()
    {
        Flaeche.Quelle = dokument.Projekt;   // alte Formen-Objekte sind weg
        TitelAnzeigen();
        BaueEbenenListe();
        Flaeche.InvalidateVisual();
        if (aktiverTab == 6)
        {
            VorschauNeu();
        }
        Flaeche.AuswahlLeeren();      // feuert Flaeche.Zustand → Status/Propbar
        MenuUndo.IsEnabled = dokument.HatUndo;
        MenuRedo.IsEnabled = dokument.HatRedo;
        PropLeisteBeleben();
    }

    /// <summary>Canvas-Zustand (Auswahl/Geometrie) → Status + Eigenschaften-Leiste.</summary>
    private void OnFlaecheZustand()
    {
        PropLeisteBeleben();
        dokument.Melden();   // leichtgewichtig (kein Listen-Rebuild, kein Auswahl-Reset)
        MenuLoeschen.IsEnabled = Flaeche.Ausgewaehlt.Count > 0;
    }

    /// <summary>Fenster-Titel aus dem Dokument (&lt;Dateiname&gt; - GRBL - LaserForge).</summary>
    private void TitelAnzeigen()
    {
        var t = dokument.Titel;
        if (Title != t) Title = t;
    }

    // ================================================== Statusleiste

    private void OnMausMM(double x, double y)
    {
        StatusMaus.Text = string.Format(Dokument.Deutsch, "X {0:0.000} mm · Y {1:0.000} mm", x, y);
    }

    // ================================================== Datei-IO (C)

    private void OnNeu(object? s, RoutedEventArgs e) => NeuProjekt();
    private void OnOeffnen(object? s, RoutedEventArgs e) => OeffnenDialog();
    private void OnSpeichern(object? s, RoutedEventArgs e) => SpeichernDialog();
    private void OnBeenden(object? s, RoutedEventArgs e) => Close();

    /// <summary>Neues leeres Projekt (Layer 00/01 wie die Bibel).</summary>
    private void NeuProjekt()
    {
        dokument.Setze(Dokument.NeueLeere(), "");
        KonsoleHin("Neues Projekt angelegt.", Brushes.Silver);
        dokument.Melden();
    }

    /// <summary>Öffnen-Dialog → LeserLbrn.Lesen → Formen auf Canvas + Schnitte im Tab.</summary>
    private async void OeffnenDialog()
    {
        var dateien = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Projekt öffnen (.lbrn / .lbrn2)",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("LightBurn-Projekt (*.lbrn, *.lbrn2)")
                {
                    Patterns = new[] { "*.lbrn", "*.lbrn2" },
                },
            },
        });
        if (dateien.Count == 0)
        {
            return;
        }
        LadeProjektdatei(dateien[0].Path.LocalPath);
    }

    /// <summary>Datei lesen (Core-Leser) und das Fenster daran hängen.</summary>
    private void LadeProjektdatei(string pfad)
    {
        try
        {
            var projekt = LeserLbrn.Lesen(File.ReadAllText(pfad));
            dokument.Setze(projekt, pfad);       // feuert OnDokZustand (Titel, Liste, Canvas)
            dokument.FehlendeLayerAnlegen();
            dokument.Melden();
            KonsoleHin("Projekt geladen: " + pfad, Brushes.LightGreen);
            StatusMeldung.Text = "Geladen";
        }
        catch (Exception fehler)
        {
            KonsoleHin("Fehler beim Öffnen: " + fehler.Message, Brushes.Salmon);
            StatusMeldung.Text = "Öffnen fehlgeschlagen";
        }
    }

    /// <summary>Speichern über den Core-Schreiber; ohne Pfad → Speichern-Dialog (.lbrn).</summary>
    private async void SpeichernDialog()
    {
        try
        {
            string text = SchreiberLbrn.Schreiben(dokument.Projekt);
            if (dokument.Dateipfad.Length == 0)
            {
                var ziel = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveFileOptions
                {
                    Title = "Projekt speichern",
                    SuggestedFileName = "unbenannt",
                    DefaultExtension = "lbrn",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("LightBurn-Projekt (*.lbrn)")
                        {
                            Patterns = new[] { "*.lbrn" },
                        },
                    },
                });
                if (ziel is null)
                {
                    return;
                }
                var strom = await ziel.OpenWriteAsync();
                using (var schreiber = new StreamWriter(strom))
                {
                    await schreiber.WriteAsync(text);
                }
                dokument.Dateipfad = ziel.Path.LocalPath;
            }
            else
            {
                File.WriteAllText(dokument.Dateipfad, text);
            }
            TitelAnzeigen();
            KonsoleHin("Gespeichert: " + dokument.Dateipfad, Brushes.LightGreen);
            StatusMeldung.Text = "Gespeichert";
        }
        catch (Exception fehler)
        {
            KonsoleHin("Fehler beim Speichern: " + fehler.Message, Brushes.Salmon);
            StatusMeldung.Text = "Speichern fehlgeschlagen";
        }
    }

    // ================================================== Ebenen-Tab (A)

    /// <summary>Ebenen-Liste aus den Core-Schnitten bauen (Farbchip/Nummer/Modus/Power/Tempo/Aus/Sichtbar).</summary>
    private void BaueEbenenListe()
    {
        EbenenListe.Children!.Clear();
        foreach (var s in dokument.Projekt.GeordneteSchnitte())
        {
            EbenenListe.Children.Add(EbenenReihe(s));
        }
    }

    /// <summary>Eine Live-Zeile: Bearbeitungen schreiben direkt in die Core-Schnitt-Einstellung.</summary>
    private Control EbenenReihe(SchnittEinstellung s)
    {
        var farbe = LayerFarben.VonIndex(s.Index);
        var chip = new Border
        {
            Width = 16, Height = 16, CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(farbe),
            BorderBrush = new SolidColorBrush(Color.Parse("#5A5A5A")),
            BorderThickness = new Thickness(1),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = (s.Index <= 30 ? LayerFarben.Palette[Math.Clamp(s.Index, 0, 30)].Nummer : s.Index.ToString()),
                FontSize = 7,
                Foreground = new SolidColorBrush(LayerFarben.Textfarbe(farbe)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            },
        };

        var name = new TextBlock
        {
            Text = s.NameSektion.Length > 0 ? s.NameSektion : $"Layer {s.Index:00}",
            FontSize = 10, Foreground = Brushes.Gainsboro, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        var modus = new ComboBox
        {
            Width = 68, Height = 22, FontSize = 10,
            ItemsSource = ModusWerte,
            SelectedItem = Array.IndexOf(ModusWerte, s.Typ) >= 0 ? s.Typ : "Cut",
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };
        modus.SelectionChanged += (_, _) =>
        {
            if (modus.SelectedItem is string m)
            {
                s.Typ = m;   // live ins Core-Projekt
                EbenenLive();
            }
        };

        var power = new NumericUpDown
        {
            Width = 52, Height = 22, FontSize = 10,
            Minimum = 0m, Maximum = 100m, Increment = 1m,
            Value = (decimal)Math.Clamp(s.MaxPower, 0, 100),
        };
        power.ValueChanged += (_, _) =>
        {
            if (power.Value is { } v)
            {
                double w = (double)v;
                s.MinPower = w;
                s.MaxPower = w;   // live ins Core-Projekt
                EbenenLive();
            }
        };

        var tempo = new NumericUpDown
        {
            Width = 52, Height = 22, FontSize = 10,
            Minimum = 0.1m, Maximum = 1000m, Increment = 1m,
            Value = (decimal)Math.Clamp(Math.Max(0.1, s.Speed), 0.1, 1000),
        };
        tempo.ValueChanged += (_, _) =>
        {
            if (tempo.Value is { } v)
            {
                s.Speed = (double)v;   // live ins Core-Projekt
                EbenenLive();
            }
        };

        var ausS = new ToggleSwitch
        {
            MinWidth = 34, FontSize = 9, Padding = new Thickness(0),
            IsChecked = s.Aktiviert,
        };
        ausS.IsCheckedChanged += (_, _) =>
        {
            s.Aktiviert = ausS.IsChecked == true;   // live ins Core-Projekt
            EbenenLive();
        };

        var sichtbarS = new ToggleSwitch
        {
            MinWidth = 34, FontSize = 9, Padding = new Thickness(0),
            IsChecked = !s.Versteckt,
        };
        sichtbarS.IsCheckedChanged += (_, _) =>
        {
            bool sicht = sichtbarS.IsChecked != false;
            Flaeche.LayerSichtbar(s.Index, sicht);
        };

        var gitter = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("16,64,72,54,54,36,44"),
        };
        gitter.Children.Add(chip);          Grid.SetColumn(chip, 0);
        gitter.Children.Add(name);          Grid.SetColumn(name, 1);
        gitter.Children.Add(modus);         Grid.SetColumn(modus, 2);
        gitter.Children.Add(power);         Grid.SetColumn(power, 3);
        gitter.Children.Add(tempo);         Grid.SetColumn(tempo, 4);
        gitter.Children.Add(ausS);          Grid.SetColumn(ausS, 5);
        gitter.Children.Add(sichtbarS);     Grid.SetColumn(sichtbarS, 6);

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#262626")),
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(3, 2),
            Child = gitter,
        };
    }

    private void EbenenLive()
    {
        Flaeche.InvalidateVisual();
        if (aktiverTab == 6)
        {
            VorschauNeu();
        }
    }

    // ================================================== Vorschau (D)

    /// <summary>Echten Core-Simulator nutzen und Ergebnis als Bild + Zeit zeigen.</summary>
    private void VorschauNeu()
    {
        try
        {
            var erg = PreviewSimulator.Simuliere(dokument.Projekt);
            VorschauZeit.Text = string.Format(
                Dokument.Deutsch,
                "Zeit: {0:0.0} s Brennen · {1:0.0} s Fahrten · {2:0.0} s gesamt · {3} Formen",
                erg.Sekunden, erg.FahrtenSekunden, erg.Sekunden + erg.FahrtenSekunden,
                dokument.Projekt.FormenZahl());
            VorschauBild.Source = PreviewBitmap(erg);
        }
        catch (Exception fehler)
        {
            VorschauZeit.Text = "Simulationsfehler: " + fehler.Message;
        }
    }

    private void OnVorschauAktualisieren(object? s, RoutedEventArgs e) => VorschauNeu();

    /// <summary>Grau-Matrix in ein BGRA-WriteableBitmap (weiß = unbrannt, dunkel = gebrannt).</summary>
    private static WriteableBitmap PreviewBitmap(PreviewErg erg)
    {
        var bitmap = new WriteableBitmap(new PixelSize(erg.BreitePix, erg.HoehePix), new Vector(96, 96), PixelFormats.Bgra8888);
        using (var rahmen = bitmap.Lock())
        {
            var zeile = new byte[erg.BreitePix * 4];
            for (int y = 0; y < erg.HoehePix; y++)
            {
                for (int x = 0; x < erg.BreitePix; x++)
                {
                    byte g = erg.Grau[y * erg.BreitePix + x];
                    zeile[x * 4 + 0] = g;   // B
                    zeile[x * 4 + 1] = g;   // G
                    zeile[x * 4 + 2] = g;   // R
                    zeile[x * 4 + 3] = 255; // A
                }
                Marshal.Copy(zeile, 0, rahmen.Address + (long)y * rahmen.RowBytes, zeile.Length);
            }
        }
        return bitmap;
    }

    // ================================================== Konsole (E)

    /// <summary>Konsole: Startzustand (Warte auf Verbindung) + gesperrte Eingabe.</summary>
    private void KonsoleAnfang()
    {
        KonsoleHin("Auf Verbindung warten …", Brushes.IndianRed);
        KonsoleHinhalt.Text = "Keine Verbindung — Gerät verbinden";
        KonsoleEingabe.IsEnabled = false;
        KonsoleSenden.IsEnabled = false;
    }

    /// <summary>Eine Log-Zeile in die Konsole schreiben (Farbe frei wählbar).</summary>
    private void KonsoleHin(string text, IBrush farbe)
    {
        KonsoleLog.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 11,
            Foreground = farbe,
            Margin = new Thickness(0, 1, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        });
        while (KonsoleLog.Children.Count > 200)
        {
            KonsoleLog.Children.RemoveAt(0);
        }
        if (KonsoleLog.Parent is ScrollViewer sv)
        {
            sv.ScrollToEnd();
        }
    }

    /// <summary>M5-c-Anknopf: Verbindungszustand öffnet/schließt die Konsole-Eingabe (heute: immer zu).</summary>
    public void VerbindungZustand(bool verbunden, string gerät)
    {
        KonsoleEingabe.IsEnabled = verbunden;
        KonsoleSenden.IsEnabled = verbunden;
        KonsoleHinhalt.Text = verbunden ? "Verbunden mit " + gerät : "Keine Verbindung — Gerät verbinden";
        if (verbunden)
        {
            KonsoleHin("--- Verbunden mit " + gerät + " ---", Brushes.LightGreen);
        }
    }

    // ================================================== Eigenschaften-Leiste (B)

    /// <summary>Felder X/Y/B/H/Dreh der aktuellen Auswahl füllen (deutsch, Komma).</summary>
    private void PropLeisteBeleben()
    {
        if (Flaeche.Ausgewaehlt.Count == 0)
        {
            FeldXPos.Text = ""; FeldYPos.Text = ""; FeldBreite.Text = ""; FeldHoehe.Text = "";
            FeldDrehen.Text = "0,00";
            return;
        }
        var box = AuswahlUnion(0.0);
        FeldXPos.Text = string.Format(Dokument.Deutsch, "{0:0.000}", box.minX);
        FeldYPos.Text = string.Format(Dokument.Deutsch, "{0:0.000}", box.minY);
        FeldBreite.Text = string.Format(Dokument.Deutsch, "{0:0.000}", box.maxX - box.minX);
        FeldHoehe.Text = string.Format(Dokument.Deutsch, "{0:0.000}", box.maxY - box.minY);
        FeldDrehen.Text = string.Format(Dokument.Deutsch, "{0:0.00}", drehWinkel);
    }

    /// <summary>Zahl deutscher/engeliger Komma-/Punktform lesen.</summary>
    private static double Zahle(string text)
    {
        return double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Any,
            CultureInfo.InvariantCulture, out var wert) ? wert : double.NaN;
    }

    /// <summary>(minX, minY, maxX, maxY) der Auswahl ohne Toleranz.</summary>
    private (double minX, double minY, double maxX, double maxY) AuswahlUnion(double toleranz)
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        if (Flaeche.Ausgewaehlt.Count == 0)
        {
            return (0, 0, 0, 0);
        }
        foreach (var f in Flaeche.Ausgewaehlt)
        {
            var box = LayerFarben.BoxMM(f, toleranz);
            minX = Math.Min(minX, box[0]);
            minY = Math.Min(minY, box[1]);
            maxX = Math.Max(maxX, box[2]);
            maxY = Math.Max(maxY, box[3]);
        }
        return (minX, minY, maxX, maxY);
    }

    /// <summary>Alle Eigenschaften-Felder verdrahten (Enter-Commit + Fokuswechsel-Commit).</summary>
    private void PropLeisteVerdrahten()
    {
        foreach (var feld in new[] { FeldXPos, FeldYPos, FeldBreite, FeldHoehe, FeldDrehen })
        {
            feld.KeyDown += OnPropfeldTaste;
            feld.LostFocus += OnPropfeldVerlassen;
        }
    }

    private void OnPropfeldTaste(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            FeldUebernehmen(sender);
            e.Handled = true;
        }
    }

    private void OnPropfeldVerlassen(object? sender, RoutedEventArgs e) => FeldUebernehmen(sender);

    /// <summary>Eigenschaften-Feld in die Auswahl schreiben (live auf die Formen-XForm).</summary>
    private void FeldUebernehmen(object? sender)
    {
        if (Flaeche.Ausgewaehlt.Count == 0)
        {
            return;
        }
        var box = AuswahlUnion(0.0);
        switch (sender)
        {
            case TextBox t when ReferenceEquals(t, FeldXPos):
            {
                double nx = Zahle(t.Text);
                if (!double.IsNaN(nx) && Math.Abs(nx - box.minX) > 1e-9)
                {
                    dokument.Sichere("X-Position setzen");
                    VerschiebeAuswahl(0, nx - box.minX);
                }
                break;
            }
            case TextBox t when ReferenceEquals(t, FeldYPos):
            {
                double ny = Zahle(t.Text);
                if (!double.IsNaN(ny) && Math.Abs(ny - box.minY) > 1e-9)
                {
                    dokument.Sichere("Y-Position setzen");
                    VerschiebeAuswahl(ny - box.minY, 0);
                }
                break;
            }
            case TextBox t when ReferenceEquals(t, FeldBreite):
            {
                double nb = Zahle(t.Text), bw = box.maxX - box.minX;
                if (!double.IsNaN(nb) && nb > 0.05 && bw > 0.05 && Math.Abs(nb - bw) > 1e-9)
                {
                    dokument.Sichere("Breite setzen");
                    SkaliereAuswahl(nb / bw, 1.0, box.minX, box.minY);
                }
                break;
            }
            case TextBox t when ReferenceEquals(t, FeldHoehe):
            {
                double nh = Zahle(t.Text), hh = box.maxY - box.minY;
                if (!double.IsNaN(nh) && nh > 0.05 && hh > 0.05 && Math.Abs(nh - hh) > 1e-9)
                {
                    dokument.Sichere("Höhe setzen");
                    SkaliereAuswahl(1.0, nh / hh, box.minX, box.minY);
                }
                break;
            }
            case TextBox t when ReferenceEquals(t, FeldDrehen):
            {
                double wd = Zahle(t.Text);
                if (!double.IsNaN(wd) && Math.Abs(wd - drehWinkel) > 1e-9)
                {
                    dokument.Sichere("Drehen setzen");
                    DrehAuswahl(wd - drehWinkel);
                    drehWinkel = wd;
                }
                break;
            }
        }
        PropLeisteBeleben();
    }

    /// <summary>Alle Auswahl-Formen um (dx, dy) verschieben (Welt-Translation vor die XForm).</summary>
    private void VerschiebeAuswahl(double dx, double dy)
    {
        var schub = Transformationsmatrix.Verschieben(dx, dy);
        foreach (var f in Flaeche.Ausgewaehlt)
        {
            f.XForm = schub * f.XForm;
        }
        Flaeche.InvalidateVisual();
        dokument.Melden();
    }

    /// <summary>Auswahl am Ankerpunkt (X/Y separat) skalieren.</summary>
    private void SkaliereAuswahl(double sx, double sy, double ax, double ay)
    {
        var schub = Transformationsmatrix.Verschieben(ax, ay)
                    * new Transformationsmatrix(sx, 0, 0, sy, 0, 0)
                    * Transformationsmatrix.Verschieben(-ax, -ay);
        foreach (var f in Flaeche.Ausgewaehlt)
        {
            f.XForm = schub * f.XForm;
        }
        Flaeche.InvalidateVisual();
        dokument.Melden();
    }

    /// <summary>Auswahl um den Auswahl-Mittelpunkt drehen (+CCW, mm-Koordinaten Y nach oben).</summary>
    private void DrehAuswahl(double gradZusatz)
    {
        var box = AuswahlUnion(0.0);
        double cx = (box.minX + box.maxX) / 2, cy = (box.minY + box.maxY) / 2;
        var schub = Transformationsmatrix.Verschieben(cx, cy)
                    * Transformationsmatrix.DrehenRadiant(gradZusatz * Math.PI / 180.0)
                    * Transformationsmatrix.Verschieben(-cx, -cy);
        foreach (var f in Flaeche.Ausgewaehlt)
        {
            f.XForm = schub * f.XForm;
        }
        Flaeche.InvalidateVisual();
        dokument.Melden();
    }
}