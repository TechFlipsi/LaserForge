using System.IO.Compression;
using LaserForge.Core.Geometrie;
namespace LaserForge.Core.Formate;
/// <summary>
/// Eigene Thumbnail-Rasterung: Projekt-Zuege -> 250x250 PNG (Graustufe, 8 Bit),
/// eigener minimaler PNG-Enkoder (Deflate + CRC32/Adler32) — keine Fremd-Abhaengigkeit.
/// </summary>
public static class ThumbnailErzeuger
{
    public const int StandardGroesse = 250;
    public static string ErzeugeBasis64(Projekt projekt, int groesse = StandardGroesse)
    {
        var zuege = projekt.Formen.SelectMany(f => f.ZuZuege()).Where(z => z.Punkte.Count > 1).ToList();
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var z in zuege)
            foreach (var p in z.Punkte)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
        // Grau-Bitmap
        var grau = new byte[groesse * groesse];
        Array.Fill(grau, (byte)255);
        if (minX <= maxX)
        {
            double breite = Math.Max(1e-6, maxX - minX);
            double hoehe = Math.Max(1e-6, maxY - minY);
            double rand = 8; // Pixel
            double maßstab = Math.Min((groesse - 2 * rand) / breite, (groesse - 2 * rand) / hoehe);
            double verschiebeX = rand + (groesse - 2 * rand - breite * maßstab) / 2.0;
            double verschiebeY = rand + (groesse - 2 * rand - hoehe * maßstab) / 2.0;
            foreach (var z in zuege)
                for (int i = 1; i < z.Punkte.Count; i++)
                    LinieZeichnen(grau, groesse,
                        verschiebeX + (z.Punkte[i - 1].X - minX) * maßstab,
                        groesse - (verschiebeY + (z.Punkte[i - 1].Y - minY) * maßstab),
                        verschiebeX + (z.Punkte[i].X - minX) * maßstab,
                        groesse - (verschiebeY + (z.Punkte[i].Y - minY) * maßstab));
        }
        return KonvertiereBasis64(grau, groesse);
    }
    /// <summary>Breisenham-Linie ins Grau-Bild (dunkel = Strich).</summary>
    private static void LinieZeichnen(byte[] bild, int breiteBild, double x0, double y0, double x1, double y1)
    {
        int x = (int)Math.Round(x0), y = (int)Math.Round(y0);
        int xe = (int)Math.Round(x1), ye = (int)Math.Round(y1);
        int dx = Math.Abs(xe - x), dy = -Math.Abs(ye - y);
        int sx = x < xe ? 1 : -1, sy = y < ye ? 1 : -1;
        int fehler = dx + dy;
        while (true)
        {
            if (x >= 0 && y >= 0 && x < breiteBild && y < breiteBild) bild[y * breiteBild + x] = 0;
            if (x == xe && y == ye) break;
            int e2 = 2 * fehler;
            if (e2 >= dy) { fehler += dy; x += sx; }
            if (e2 <= dx) { fehler += dx; y += sy; }
        }
    }
    /// <summary>Minimale PNG-Chunks (IHDR/IDAT/IEND) — Graustufe colorType 0, bitDepth 8, kein Filter.</summary>
    private static string KonvertiereBasis64(byte[] grau, int groesse)
    {
        // IDAT: zlib-Fassung (0x78 0x01 Kopf + Roh-Deflate + Adler32)
        var roh = new MemoryStream();
        for (int y = 0; y < groesse; y++)
        {
            roh.WriteByte(0); // Filtertyp 0 je Zeile
            roh.Write(grau, y * groesse, groesse);
        }
        var rohArray = roh.ToArray();
        var ms = new MemoryStream();
        ms.WriteByte(0x78); ms.WriteByte(0x01);
        using (var defl = new DeflateStream(ms, CompressionLevel.Fastest, true))
        {
            defl.Write(rohArray, 0, rohArray.Length);
        }
        uint adler = Adler32(rohArray);
        ms.WriteByte((byte)(adler >> 24)); ms.WriteByte((byte)(adler >> 16));
        ms.WriteByte((byte)(adler >> 8)); ms.WriteByte((byte)adler);
        var idat = ms.ToArray();
        var png = new MemoryStream();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);
        var ihdr = new byte[13];
        ihdr[0] = (byte)(groesse >> 24); ihdr[1] = (byte)(groesse >> 16); ihdr[2] = (byte)(groesse >> 8); ihdr[3] = (byte)groesse;
        ihdr[4] = (byte)(groesse >> 24); ihdr[5] = (byte)(groesse >> 16); ihdr[6] = (byte)(groesse >> 8); ihdr[7] = (byte)groesse;
        ihdr[8] = 8;   // bitTiefe
        ihdr[9] = 0;   // colorType = Graustufe
        ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0; // Komprimierung/Filter/Interlace
        SchreibeChunk(png, "IHDR", ihdr);
        SchreibeChunk(png, "IDAT", idat);
        SchreibeChunk(png, "IEND", Array.Empty<byte>());
        return Convert.ToBase64String(png.ToArray());
    }
    private static void SchreibeChunk(Stream s, string typ, byte[] daten)
    {
        var len = BitConverter.GetBytes(daten.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(len);
        var typB = System.Text.Encoding.ASCII.GetBytes(typ);
        s.Write(len, 0, 4); s.Write(typB, 0, typB.Length);
        if (daten.Length > 0) s.Write(daten, 0, daten.Length);
        uint crc = Crc32(typB, daten);
        var crcB = BitConverter.GetBytes(crc);
        if (BitConverter.IsLittleEndian) Array.Reverse(crcB);
        s.Write(crcB, 0, 4);
    }
    private static uint[] Tabelle = ErzeugeTabelle();
    private static uint[] ErzeugeTabelle()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
                c = ((c & 1) != 0) ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }
    private static uint Crc32(byte[] a, byte[] b)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (var daten in new[] { a, b })
            foreach (var byteWert in daten)
                crc = Tabelle[(crc ^ byteWert) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }
    private static uint Adler32(byte[] daten)
    {
        uint a = 1, b = 0;
        foreach (var Wert in daten)
        {
            a = (a + Wert) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }
}
