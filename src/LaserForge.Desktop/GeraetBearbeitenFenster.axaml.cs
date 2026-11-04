using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LaserForge.Core.Geraete;
using LaserForge.Devices.Grbl;
using System.IO;

namespace LaserForge.Desktop;

/// <summary>Geraet anlegen/ändern („Manuell erstellen" und „Bearbeiten" im Geräte-Dialog).</summary>
public partial class GeraetBearbeitenFenster : Window
{
    public Geraet Geraet { get; }

    /// <summary>true, wenn OK gedrückt wurde (Änderungen übernehmen).</summary>
    public bool Bestaetigt { get; private set; }

    public GeraetBearbeitenFenster(Geraet geraet, bool neu)
    {
        InitializeComponent();
        Geraet = geraet;
        Title = neu ? "Gerät manuell erstellen" : "Gerät bearbeiten";
        NameBox.Text = geraet.Name;
        PortCombo.Items.Clear();
        foreach (var p in SeriellSuche.Portliste())
            PortCombo.Items.Add(new ComboBoxItem { Content = p });
        if (geraet.Anschluss.Length > 0)
            PortCombo.SelectedItem = new ComboBoxItem { Content = geraet.Anschluss };
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        if (NameBox.Text is string n && n.Trim().Length > 0) Geraet.Name = n.Trim();
        if (PortCombo.SelectedItem is ComboBoxItem c && c.Content is string p) Geraet.Anschluss = p;
        if (BaudCombo.SelectedItem is ComboBoxItem b && b.Content is string bs && int.TryParse(bs, out var bd))
            Geraet.Baud = bd;
        Geraet.HomeBeimStart = HomeBox.IsChecked == true;
        Bestaetigt = true;
        Close(true);
    }

    private void OnAbbrechen(object? sender, RoutedEventArgs e) => Close(false);

    /// <summary>Datei-Öffnen für den Import ( prefs-Datei je Benutzer wählen).</summary>
    public static async System.Threading.Tasks.Task<string?> DateiWaehlen(Window owner)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Geräte-Datei importieren",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Konfig-Dateien (prefs.ini, JSON)")
                { Patterns = new[] { "prefs.ini", "*.ini", "*.json" } },
            },
        });
        var f = files.Count > 0 ? files[0] : null;
        if (f is null) return null;
        using var st = await f.OpenReadAsync();
        using var r = new StreamReader(st);
        return await r.ReadToEndAsync();
    }
}
