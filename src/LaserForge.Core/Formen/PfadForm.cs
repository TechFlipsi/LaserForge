using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formen;
/// <summary>Orginal-Vertex eines lbrn-Pfads (inkl. Bezier-Kontrollpunke, Node-Bearbeitungs-Gruende).</summary>
public sealed record PfadVertex(Punkt Punkt, Punkt? C0, Punkt? C1, bool Glatt);
/// <summary>Orginal-Primitive: T = "L" Linie oder "B" Bezier zwischen Vertexindizes.</summary>
public sealed record PfadPrimitive(string Typ, int P0, int P1);
public sealed class PfadForm : Form
{
    /// <summary>Tessellierte Zuege (schnelles Zeichnen/Laserpfad).</summary>
    public List<Zug> Rohtuege { get; set; } = new();
    /// <summary>Orginal-Vertexliste (wenn vorhanden — Bezier-Erhalt + Node-Bearbeitung).</summary>
    public List<PfadVertex> RoheVerte { get; set; } = new();
    /// <summary>Orginal-Primitives.</summary>
    public List<PfadPrimitive> RohePrimse { get; set; } = new();
    public bool HatRoheVerte => RoheVerte.Count > 0 && RohePrimse.Count > 0;
    public override string Rohtyp() => "Path";
    protected override List<Zug> Rohezuege() => Rohtuege;
}
