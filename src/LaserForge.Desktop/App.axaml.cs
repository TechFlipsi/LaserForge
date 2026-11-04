using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
namespace LaserForge.Desktop;
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (StartEinstellung.KonfigDialog)
            {
                var liste = Core.Geraete.GeraeteSpeicher.Laden();
                var g = liste.FirstOrDefault(x => x.IstStandard) ?? liste.FirstOrDefault();
                if (!string.IsNullOrEmpty(StartEinstellung.KonfigPort))
                    g = g is null
                        ? new Core.Geraete.Geraet { Name = "QA-Drill", Protokoll = "GRBL" }
                        : g;
                var drill = new Core.Geraete.Geraet { Name = g!.Name, Protokoll = g.Protokoll, Anschluss = StartEinstellung.KonfigPort };
                desktop.MainWindow = new KonfigFenster(drill);
            }
            else
                desktop.MainWindow = StartEinstellung.GeraeteDialog
                    ? new GeraeteFenster() as Window
                    : new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
