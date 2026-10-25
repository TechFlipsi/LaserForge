using System.Globalization;

namespace LaserForge.Core.Geometrie;
/// <summary>
/// Affine Matrix in .lbrn-XForm-Zeilenform: a b c d e f
/// (Zeilen der 3x3-Matrize: [a c e; b d f; 0 0 1], Anwenden: x' = a*x + c*y + e, y' = b*x + d*y + f)
/// </summary>
public readonly record struct Transformationsmatrix(double A, double B, double C, double D, double E, double F)
{
    public static readonly Transformationsmatrix Identitaet = new(1, 0, 0, 1, 0, 0);
    public static Transformationsmatrix Verschieben(double x, double y) => new(1, 0, 0, 1, x, y);
    public static Transformationsmatrix Skalieren(double s) => new(s, 0, 0, s, 0, 0);
    public static Transformationsmatrix DrehenRadiant(double w)
    {
        double c = Math.Cos(w), s = Math.Sin(w);
        return new Transformationsmatrix(c, s, -s, c, 0, 0);
    }
    public static Transformationsmatrix SpiegelnX(bool aktiv) => aktiv ? new(-1, 0, 0, 1, 0, 0) : Identitaet;
    public static Transformationsmatrix SpiegelnY(bool aktiv) => aktiv ? new(1, 0, 0, -1, 0, 0) : Identitaet;
    public Punkt Anwenden(Punkt p) => new(p.X * A + p.Y * C + E, p.X * B + p.Y * D + F);
    public static Transformationsmatrix operator *(Transformationsmatrix a, Transformationsmatrix b) =>
        new(a.A * b.A + a.C * b.B, a.B * b.A + a.D * b.B,
            a.A * b.C + a.C * b.D, a.B * b.C + a.D * b.D,
            a.A * b.E + a.C * b.F + a.E, a.B * b.E + a.D * b.F + a.F);
    public string ZuXFormText() => string.Format(CultureInfo.InvariantCulture,
        "{0:G17} {1:G17} {2:G17} {3:G17} {4:G17} {5:G17}", A, B, C, D, E, F);
}
