namespace LaserForge.Core.Geometrie;
/// <summary>Punkt in Millimetern (Arbeitsflaeche-Welt). Basis-Geometrietyp.</summary>
public readonly record struct Punkt(double X, double Y)
{
    public static readonly Punkt Null = new(0, 0);
    public Punkt Verschieben(double dx, double dy) => new(X + dx, Y + dy);
    public double EntfernungZu(Punkt o) => Math.Sqrt((X-o.X)*(X-o.X) + (Y-o.Y)*(Y-o.Y));
    public override string ToString() => $"({X:F3}; {Y:F3})";
}
