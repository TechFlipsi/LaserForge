using Avalonia.Controls;
using Avalonia.Interactivity;
using LaserForge.Core.Geraete;
using LaserForge.Devices.Grbl;
using LaserForge.Devices.Transport;
using System.IO;

namespace LaserForge.Desktop;

/// <summary>Eine $-Maschineinstellungs-Zeile des Konfig-Dialogs.</summary>
public sealed class KonfigZeile
{
    public string Schluessel { get; init; } = "";
    /// <summary>Vom Gerät gelesener Wert (Referenz bei Änderungs-Prüfung).</summary>
    public string Original { get; set; } = "";
    public string Wert { get; set; } = "";
    public string Geaendert => Wert != Original ? "→ geändert" : "";
}

/// <summary>
/// Geräte-Konfiguration direkt am Laser (Paritaet zu LightBurns „Machine Settings →
/// GRBL Configuration“): $-Einstellungen lesen, ändern, zum Gerät schreiben (EEPROM).
/// Eigener Anlageweg: serieller Transport je aktiver Geräte-Port; kein Batch-Reset ($RST)
/// — bewusst nicht im Dialog (Sicherheit).
/// </summary>
public partial class KonfigFenster : Window
{
    private GrblAusruestung? _ausruestung;
    private SeriellTransport? _transport;

    public KonfigFenster(LaserForge.Core.Geraete.Geraet geraet)
    {
        InitializeComponent();
        StatusZeile.Text = $"Verbinde {geraet.Anschluss} @ {geraet.Baud} …";
        Title = $"Geräte-Konfiguration — {geraet.Name} ({geraet.Anschluss})";
        Task.Run(() => LesenAuftrag(geraet));
    }

    private void LesenAuftrag(LaserForge.Core.Geraete.Geraet geraet)
    {
        try
        {
            _transport = new SeriellTransport(geraet.Anschluss, geraet.Baud);
            _transport.Oeffnen();
            var verbindung = new GrblVerbindung(_transport);
            _ausruestung = new GrblAusruestung(verbindung);
            var werte = _ausruestung.EinstellungenLesen(5000);
            var zeilen = werte
                .OrderBy(k => int.TryParse(k.Key.TrimStart('$'), out var n) ? n : int.MaxValue)
                .Select(k => new KonfigZeile { Schluessel = k.Key, Original = k.Value, Wert = k.Value })
                .ToList();
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                StatusZeile.Text = $"Am Gerät ({(_ausruestung!.Version ?? "GRBL")}) gelesen: {zeilen.Count} Einstellungen.";
                KonfigListe.ItemsSource = zeilen;
            });
            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "LaserForge", "qa-konfig-dialog.txt"),
                    "KF_OK\nversion=" + (_ausruestung.Version ?? "?") + "\neinstellungen=" + werte.Count +
                    "\n$100=" + (werte.TryGetValue("$100", out var v100) ? v100 : "?") +
                    "\n$110=" + (werte.TryGetValue("$110", out var v110) ? v110 : "?") +
                    "\n$32=" + (werte.TryGetValue("$32", out var v32) ? v32 : "?") + "\n");
            }
            catch { /* QA-Hilfslinie nicht kritisch */ }
        }
        catch (Exception ex)
        {
            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "LaserForge", "qa-konfig-dialog.txt"),
                    "KF_FEHLER\n" + ex.GetType().Name + ": " + ex.Message +
                    "\nports=" + string.Join(",", System.IO.Ports.SerialPort.GetPortNames()) + "\n");
            }
            catch { /* QA-Hilfslinie nicht kritisch */ }
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                StatusZeile.Text = "Fehler: " + ex.Message);
        }
    }

    private void OnLesen(object? sender, RoutedEventArgs e)
    {
        var g = AktuellesGeraet();
        if (g is not null) { KonfigListe.ItemsSource = null; Task.Run(() => LesenAuftrag(g)); }
    }

    private async void OnSchreiben(object? sender, RoutedEventArgs e)
    {
        if (_ausruestung is null || _transport is not { Verbunden: true })
        {
            StatusZeile.Text = "Keine Verbindung — zuerst „Aus Gerät lesen“.";
            return;
        }
        if (KonfigListe.ItemsSource is not IEnumerable<KonfigZeile> zeilen) return;
        var alle = AlleBox.IsChecked == true;
        var zuSetzen = zeilen.Where(z => alle || z.Wert != z.Original).ToList();
        if (zuSetzen.Count == 0)
        {
            StatusZeile.Text = "Keine Änderungen zum Schreiben.";
            return;
        }
        LesenBtn.IsEnabled = false; SchreibenBtn.IsEnabled = false;
        var gut = 0; var fehler = new List<string>();
        await Task.Run(() =>
        {
            foreach (var z in zuSetzen)
            {
                var ok = _ausruestung!.EinstellungSetzen(z.Schluessel, z.Wert);
                if (ok) { gut++; z.Original = z.Wert; }
                else fehler.Add($"{z.Schluessel}: {_ausruestung.LetzteFehlerzeile}");
                Thread.Sleep(120); // GRBL-EEPROM-Schreibtakt
            }
        });
        KonfigListe.ItemsSource = null;           // Neu-Render (Geaendert-Marker)
        KonfigListe.ItemsSource = zeilen.ToList();
        StatusZeile.Text = $"Geschrieben: {gut} OK" + (fehler.Count > 0 ? " · Fehler: " + string.Join(" | ", fehler) : "");
        LesenBtn.IsEnabled = true; SchreibenBtn.IsEnabled = true;
    }

    private static LaserForge.Core.Geraete.Geraet? AktuellesGeraet() =>
        GeraeteSpeicher.Laden().FirstOrDefault(g => g.IstStandard) ?? GeraeteSpeicher.Laden().FirstOrDefault();

    private void OnSchliessen(object? sender, RoutedEventArgs e)
    {
        try { _transport?.Schliessen(); } catch { /* war zu */ }
        Close();
    }
}
