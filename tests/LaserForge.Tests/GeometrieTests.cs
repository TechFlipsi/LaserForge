using LaserForge.Core.Geometrie;
using LaserForge.Core.Formate;
using Xunit;
namespace LaserForge.Tests;
public class GeometrieTests
{
    [Fact] public void Identitaet_Behaelt_Punkt() =>
        Assert.Equal(new Punkt(3, 4), Transformationsmatrix.Identitaet.Anwenden(new Punkt(3, 4)));
    [Fact] public void Verschieben_Plus_Skalieren()
    {
        var m = Transformationsmatrix.Skalieren(2) * Transformationsmatrix.Verschieben(1, 2); // (A*B)(p) = A(B(p)): erst Verschieben, dann Skalieren
        Assert.Equal(new Punkt(4, 6), m.Anwenden(new Punkt(1, 1)));
    }
    [Fact] public void Drehen_90_Grad()
    {
        var p = Transformationsmatrix.DrehenRadiant(Math.PI / 2).Anwenden(new Punkt(1, 0));
        Assert.Equal(0, p.X, 12);
        Assert.Equal(1, p.Y, 12);
    }
    [Fact] public void SpiegelnX() =>
        Assert.Equal(new Punkt(-5, 3), Transformationsmatrix.SpiegelnX(true).Anwenden(new Punkt(5, 3)));
    [Fact] public void XForm_Text_Roundtrip()
    {
        var m = new Transformationsmatrix(2, 0, 0, 2, 10.5, 20.25);
        Assert.Equal(m, LeserLbrn.LesereMatrix(m.ZuXFormText()));
    }
}
