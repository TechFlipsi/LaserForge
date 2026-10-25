using System.Globalization;
using System.Xml.Linq;
namespace LaserForge.Core.Formate;
/// <summary>Deutsche Kuerz-Helfer fuer XML-Attributzugriff mit InvariantCulture.</summary>
public static class XmlHelfer
{
    public static string? GetString(this XElement e, string name) => e.Attribute(name)?.Value;
    public static double? GetDoppel(this XElement e, string name) =>
        double.TryParse(e.GetString(name), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
}
