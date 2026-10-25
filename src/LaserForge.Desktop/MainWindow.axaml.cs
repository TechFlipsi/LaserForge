using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using LaserForge.Desktop.Controls;

namespace LaserForge.Desktop;

/// <summary>
/// M5-a: Hauptfenster-Aufbau nach docs/design-ziel-bild.md — dunkles Chrom,
/// helle Arbeitsfläche, Rechts-Panel mit Konsole/6 Tabs/Laser-Panel,
/// Layer-Leiste 00..30 + T1/T2 und Statusleiste. Logik folgt in M5-b.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Tab-Namen in Bibel-Reihenfolge (eigene Bezeichner).</summary>
    private static readonly string[] TabNamen =
    {
        "Schnitte/Ebenen",
        "Bewegen",
        "Konsole",
        "Makros",
        "Kamera/Overlays",
        "Seiten",
    };

    /// <summary>Layer-Palette aus docs/design-ziel-bild.md (eigene Festlegung, 31 Chips + T1/T2).</summary>
    private static readonly (string Nummer, string Hex)[] LayerPalette =
    {
        ("00", "#000000"), ("01", "#FF0000"), ("02", "#00C000"), ("03", "#0060FF"),
        ("04", "#FF80FF"), ("05", "#FF8000"), ("06", "#00C0FF"), ("07", "#FFFF00"),
        ("08", "#800060"), ("09", "#802000"), ("10", "#404040"), ("11", "#FFC000"),
        ("12", "#6080FF"), ("13", "#C0C0C0"), ("14", "#FF0080"), ("15", "#008060"),
        ("16", "#806000"), ("17", "#006080"), ("18", "#FF6020"), ("19", "#808080"),
        ("20", "#C00000"), ("21", "#0080FF"), ("22", "#8000FF"), ("23", "#00A0A0"),
        ("24", "#A06000"), ("25", "#006000"), ("26", "#800000"), ("27", "#0040A0"),
        ("28", "#A000A0"), ("29", "#404080"), ("30", "#206040"), ("T1", "#202020"), ("T2", "#606060"),
    };

    /// <summary>6 eigengebaute Tab-Knöpfe (eine Zeile, kein Zeilenumbruch).</summary>
    private Button[] TabKnopfe = null!;

    /// <summary>Aktiver Tab (Bibelvorgabe: Konsole = 2).</summary>
    private int aktiverTab = 2;

    public MainWindow()
    {
        InitializeComponent();
        TabKnopfe = new[] { TabKnopf0, TabKnopf1, TabKnopf2, TabKnopf3, TabKnopf4, TabKnopf5 };
        BaueLayerleiste();
        AktualisiereTabs();
    }

    /// <summary>Baut die untere Layer-Leiste: Farbchips 00..30 + T1/T2 mit Kontrast-Schrift.</summary>
    private void BaueLayerleiste()
    {
        foreach (var (nummer, hex) in LayerPalette)
        {
            var farbe = Color.Parse(hex);
            Layerleiste.Children!.Add(new Border
            {
                Width = 30,
                Height = 22,
                Margin = new Thickness(1, 0),
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(farbe),
                BorderBrush = new SolidColorBrush(Color.Parse("#5A5A5A")),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = nummer,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(ChipTextfarbe(farbe)),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                },
            });
        }
    }

    /// <summary>Schriftfarbe je Chip: schwarz auf heller, weiß auf dunkler Grundfarbe.</summary>
    private static Color ChipTextfarbe(Color c)
    {
        double helligkeit = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
        return helligkeit > 0.55 ? Colors.Black : Colors.White;
    }

    /// <summary>Klick auf einen der 6 Tab-Knöpfe → Inhalt wechseln.</summary>
    private void OnTabKnopf(object? sender, RoutedEventArgs e)
    {
        if (sender is Button knopf && knopf.Parent is StackPanel leiste)
        {
            aktiverTab = leiste.Children.IndexOf(knopf);
            AktualisiereTabs();
        }
    }

    /// <summary>Sichtbarkeiten + Knopfmarkierung je aktiver Tab (Null-robust beim Init).</summary>
    private void AktualisiereTabs()
    {
        if (TabKnopfe is null)
        {
            return;
        }
        aktiverTab = Math.Clamp(aktiverTab, 0, TabNamen.Length - 1);
        if (TabTitel is not null)
        {
            TabTitel.Text = TabNamen[aktiverTab];
        }
        if (TabEbenen is not null)
        {
            TabEbenen.IsVisible = aktiverTab == 0;
        }
        if (TabBewegen is not null)
        {
            TabBewegen.IsVisible = aktiverTab == 1;
        }
        if (TabKonsole is not null)
        {
            TabKonsole.IsVisible = aktiverTab == 2;
        }
        if (TabMakros is not null)
        {
            TabMakros.IsVisible = aktiverTab == 3;
        }
        if (TabKamera is not null)
        {
            TabKamera.IsVisible = aktiverTab == 4;
        }
        if (TabSeiten is not null)
        {
            TabSeiten.IsVisible = aktiverTab == 5;
        }
        for (int i = 0; i < TabKnopfe.Length; i++)
        {
            var knopf = TabKnopfe[i];
            if (knopf is null)
            {
                continue;
            }
            knopf.Classes.Remove("aktiv");
            if (i == aktiverTab)
            {
                knopf.Classes.Add("aktiv");
            }
        }
    }
}