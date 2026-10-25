using LaserForge.Core.Einstellungen;
namespace LaserForge.Core.Geometrie;
/// <summary>Einfacher Grau-Rasterer (Bresenham) — gemeinsamer Weg fuer Thumbnail und Preview.</summary>
public static class Rasterer
{
    /// <summary>Linie ins Grau-Bild (Zeilenhauptordnung, quadratisch): dunkel = 0, hell = 255.</summary>
    public static void Linie(byte[] bild, int breiteBild, double x0, double y0, double x1, double y1, byte grau = 0)
    {
        int x = (int)Math.Round(x0), y = (int)Math.Round(y0);
        int xe = (int)Math.Round(x1), ye = (int)Math.Round(y1);
        int dx = Math.Abs(xe - x), dy = -Math.Abs(ye - y);
        int sx = x < xe ? 1 : -1, sy = y < ye ? 1 : -1;
        int fehler = dx + dy;
        while (true)
        {
            if (x >= 0 && y >= 0 && x < breiteBild && y < breiteBild) bild[y * breiteBild + x] = grau;
            if (x == xe && y == ye) break;
            int e2 = 2 * fehler;
            if (e2 >= dy) { fehler += dy; x += sx; }
            if (e2 <= dx) { fehler += dx; y += sy; }
        }
    }
}
