#!/usr/bin/env bash
# release.sh — Tag → Windows-Publish-zip → GitHub-Release mit deutscher Notiz (Format §6-Automatik).
# Aufruf: ./tools/release.sh v0.1.0 ["Titel"]
set -euo pipefail
WURZEL="$(cd "$(dirname "$0")/.." && pwd)"
TAG="${1:-v0.1.0}"
TITEL="${2:-LaserForge $TAG — Erstveröffentlichung}"
REPO="TechFlipsi/LaserForge"
AUSGABE="$WURZEL/out"
mkdir -p "$AUSGABE"

echo "=== RELEASE $TAG ($(date '+%Y-%m-%d %H:%M:%S')) ==="

cd "$WURZEL"
git add -A
git commit -q -m "release: $TAG" 2>/dev/null || true
git tag -f "$TAG" -m "$TITEL"
git push origin "$TAG" > /dev/null

echo "### Windows-Publish — am stand-pc (kibot hat kein dotnet)"
ssh -o BatchMode=yes stand-pc "cd /d C:\\Users\\Fabia\\ForgeKern && dotnet publish C:\\Users\\Fabia\\ForgeKern\\src\\LaserForge.Desktop\\LaserForge.Desktop.csproj -c Release -r win-x64 --self-contained false -o C:\\Users\\Fabia\\ForgeKern\\publish -v q"
scp -q -r "stand-pc:C:/Users/Fabia/ForgeKern/publish/*" "$AUSGABE/publish/"

echo "### Zip bauen"
rm -f "$AUSGABE/LaserForge-$TAG-win-x64.zip"
cd "$AUSGABE/publish" && zip -qr "../LaserForge-$TAG-win-x64.zip" . && cd "$WURZEL"

NOTIZ="$AUSGABE/notiz.md"
cat > "$NOTIZ" <<EOF
# LaserForge $TAG

Freie, saubere Open-Source-Steuerung für Laser-Cutter und Graveur (Dioden-/CO2).

## Neu in $TAG
- Kernbibliothek: Geometrie/XForm, Formen (Rechteck/Ellipse/Polygon/Pfad mit Bezier-/Knotendaten, Bild, Gruppe)
- Formate: .lbrn/.lbrn2-Lesen und Schreiben (verlustfrei), SVG-Import, DXF-Import, Bild-Pixelraster, Thumbnail mit eigenem PNG-Enkoder
- Optimierung: Innen-erst, Nächste-Nachbar-Kette, By-Layer/By-Priorität, Pfad-Vereinfachung
- GRBL-Flavor: Modalblock (G21/G90/M4/S0), Konturen mit Passe-Wiederholung, bidi-Scan-Raster, Rahmen-Fahrt, Parken
- Gerät: serieller Transport (CH340/CDC), Status-Auslesung, Jogging, Rahmen, \$\$-Einstellungen, ALARM-Erkennung, Baud-Auto-Erkennung, COM-Scan
- Automatisierung: QA-REST (eigener Port), CLI (info/status/konvert/mcp), MCP-stdio
- Avalonia-GUI mit 2.2-nahem Hauptfenster-Layout

## Sicherheitshinweis
Lasern ist gefährlich: Die Software sendet nur Befehle, die derBenutzer auslöst. Erste Inbetriebnahme nur mit geschlossener Gehäusehaube/Aufsicht.

## Bauen
    dotnet restore LaserForge.sln
    dotnet build LaserForge.sln
    dotnet test tests/LaserForge.Tests

Lizenz: GPL-3.0-only — siehe LICENSE.
EOF

echo "### GitHub-Release"
gh release create "$TAG" "$AUSGABE/LaserForge-$TAG-win-x64.zip" \
    --repo "$REPO" --title "$TITEL" --notes-file "$NOTIZ" > /dev/null 2>&1 || \
gh release create "$TAG" --repo "$REPO" --title "$TITEL" --notes-file "$NOTIZ"

echo "LIVE-PRÜFUNG:"
gh release view "$TAG" --repo "$REPO" --json name,isDraft --jq '"Release \'"'"'" + .name + "\'"'"' live, draft=" + (.isDraft|tostring)'
echo "=== RELEASE abgeschlossen ==="