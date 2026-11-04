using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LaserForge.Core.Geraete;
using LaserForge.Devices.Grbl;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LaserForge.Desktop;

/// <summary>
/// Geräte-Dialog (Paritaet zum LightBurn-Geraete-Dialog, eigene Umsetzung):
/// Liste, Laser suchen, Manuell erstellen, Importieren (LightBurn prefs-Datei),
/// Als Standard setzen, Bearbeiten, Entfernen, Exportieren. Bridge folgt spaeter.
/// </summary>
public partial class GeraeteFenster : Window
{
    public List<LaserForge.Core.Geraete.Geraet> Geraete { get; private set; }
    /// <summary>true, wenn OK gedrückt wurde (Liste + Standard übernehmen).</summary>
    public bool Bestaetigt { get; private set; }
    public LaserForge.Core.Geraete.Geraet? AktivesGeraet =>
        Geraete.FirstOrDefault(g => g.IstStandard) ?? Geraete.FirstOrDefault();

    public GeraeteFenster()
    {
        InitializeComponent();
        Background = Avalonia.Media.Brush.Parse(LaserForge.Core.Thema.ThemeFarben.Fensterrahmen);
        Liste.Background = Avalonia.Media.Brush.Parse(LaserForge.Core.Thema.ThemeFarben.Liste);
        Geraete = GeraeteSpeicher.Laden();
        if (Geraete.Count == 0)
            Geraete.Add(new LaserForge.Core.Geraete.Geraet { Name = "Neues Gerät", Anschluss = "COM0" });
        ListeAktualisieren();
        QaStatus();
    }

    /// <summary>QA-Nachweis: Dialog-Zustand auf Datei (Items + Markierung) für den Fern-Drill.</summary>
    private void QaStatus()
    {
        try
        {
            var z = new System.Text.StringBuilder();
            z.AppendLine("GF_OK");
            z.AppendLine("items=" + Geraete.Count);
            foreach (var g in Geraete)
                z.AppendLine("item=" + g.Name + "|" + g.Anschluss + "|" + g.TypZeile);
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LaserForge", "qa-geraete-dialog.txt"), z.ToString());
        }
        catch { /* QA-Hilfslinie: Fehler hier sind nicht kritisch */ }
    }

    private void ListeAktualisieren()
    {
        var ausgewaehlt = Liste.SelectedItem as LaserForge.Core.Geraete.Geraet;
        Liste.ItemsSource = null;
        Liste.ItemsSource = Geraete;
        if (ausgewaehlt is not null && Geraete.Contains(ausgewaehlt))
            Liste.SelectedItem = ausgewaehlt;
    }

    private LaserForge.Core.Geraete.Geraet? Auswahl =>
        Liste.SelectedItem as LaserForge.Core.Geraete.Geraet;

    private async void OnLaserSuchen(object? sender, RoutedEventArgs e)
    {
        // Einfache Suche: Ports listen, Auswahl als serielles GRBL-Gerät anlegen
        // (Voll-Assistent mit Handshake kommt mit der Devices-Integration).
        var ports = System.IO.Ports.SerialPort.GetPortNames().OrderBy(p => p).ToList();
        if (ports.Count == 0)
        {
            await Meldung("Laser suchen", "Keine seriellen Anschlüsse gefunden.");
            return;
        }
        var fenster = new GeraetBearbeitenFenster(
            new LaserForge.Core.Geraete.Geraet { Name = "Suchergebnis " + (Geraete.Count + 1), Anschluss = ports.First() }, true);
        await fenster.ShowDialog(this);
        if (fenster.Bestaetigt is true)
        {
            Geraete.Add(fenster.Geraet);
            GeraeteSpeicher.Speichern(Geraete);
            ListeAktualisieren();
        }
    }

    private async void OnManuellErstellen(object? sender, RoutedEventArgs e)
    {
        var fenster = new GeraetBearbeitenFenster(
            new LaserForge.Core.Geraete.Geraet { Name = "Gerät " + (Geraete.Count + 1) }, true);
        await fenster.ShowDialog(this);
        if (fenster.Bestaetigt is true)
        {
            Geraete.Add(fenster.Geraet);
            GeraeteSpeicher.Speichern(Geraete);
            ListeAktualisieren();
        }
    }

    private async void OnImportieren(object? sender, RoutedEventArgs e)
    {
        var text = await GeraetBearbeitenFenster.DateiWaehlen(this);
        if (text is null) return;
        try
        {
            var (importiert, zahl) = LightBurnGeraeteImport.Lesen(text);
            if (zahl == 0)
            {
                await Meldung("Importieren", "Keine Geräte in der Datei gefunden (DeviceList fehlt oder leer).");
                return;
            }
            foreach (var g in importiert)
                if (!Geraete.Any(x => x.Name.Equals(g.Name, StringComparison.OrdinalIgnoreCase) &&
                                       x.Anschluss.Equals(g.Anschluss, StringComparison.OrdinalIgnoreCase)))
                    Geraete.Add(g);
            GeraeteSpeicher.Speichern(Geraete);
            ListeAktualisieren();
            await Meldung("Importieren", $"{zahl} Gerät(e) gelesen, Liste aktualisiert (Secrets werden nicht übernommen).");
        }
        catch (Exception ex)
        {
            await Meldung("Importieren", "Import fehlgeschlagen: " + ex.Message);
        }
    }

    private void OnAlsStandard(object? sender, RoutedEventArgs e)
    {
        if (Auswahl is null) return;
        GeraeteSpeicher.SetzeStandard(Geraete, Auswahl);
        ListeAktualisieren();
    }

    private async void OnBearbeiten(object? sender, RoutedEventArgs e)
    {
        if (Auswahl is null) return;
        var fenster = new GeraetBearbeitenFenster(Auswahl, false);
        await fenster.ShowDialog(this);
        GeraeteSpeicher.Speichern(Geraete);
        ListeAktualisieren();
    }

    private void OnEntfernen(object? sender, RoutedEventArgs e)
    {
        if (Auswahl is null) return;
        Geraete.Remove(Auswahl);
        GeraeteSpeicher.Speichern(Geraete);
        ListeAktualisieren();
    }

    private async void OnExportieren(object? sender, RoutedEventArgs e)
    {
        var datei = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Geräte exportieren",
            SuggestedFileName = "geraete",
            FileTypeChoices = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } },
        });
        if (datei is null) return;
        using var st = await datei.OpenWriteAsync();
        await System.Text.Json.JsonSerializer.SerializeAsync(
            st, Geraete, new JsonSerializerOptions { WriteIndented = true });
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Bestaetigt = true;
        GeraeteSpeicher.Speichern(Geraete);
        Close(Bestaetigt);
    }

    private void OnAbbrechen(object? sender, RoutedEventArgs e) => Close(false);

    private async Task Meldung(string titel, string text) =>
        await new Window
        {
            Title = titel, Width = 380, Height = 140, CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Avalonia.Media.Brush.Parse("#1e1e1e"),
            Content = new TextBlock
            {
                Text = text, Margin = new Avalonia.Thickness(14),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground = Avalonia.Media.Brushes.White,
            },
        }.ShowDialog(this);
}
