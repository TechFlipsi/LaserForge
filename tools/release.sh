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
git push origin "$TAG" --force > /dev/null

echo "### Windows-Publish — am stand-pc (kibot hat kein dotnet)"
ssh -o BatchMode=yes stand-pc "dotnet publish C:\\ForgeKern\\laserforge\\src\\LaserForge.Desktop\\LaserForge.Desktop.csproj -c Release -r win-x64 --self-contained false -o C:\\ForgeKern\\publish -v q"
scp -q -r "stand-pc:C:/ForgeKern/publish/*" "$AUSGABE/publish/"

echo "### Zip bauen"
rm -f "$AUSGABE/LaserForge-$TAG-win-x64.zip"
cd "$AUSGABE/publish" && zip -qr "../LaserForge-$TAG-win-x64.zip" . && cd "$WURZEL"

NOTIZ="$AUSGABE/notiz.md"
cat > "$NOTIZ" <<EOF
# LaserForge $TAG

Freie, saubere Open-Source-Steuerung für Laser-Cutter und Graveur (Dioden-/CO2).

## Neu in $TAG
- REST-API (Port-Parität 19540): ~28 Endpunkte nach der LightBurn-2.2-API (offizielle Spec +
  Clean-Room-Recherche) incl. PollSnapshot, file/open, material-library, bind; HMAC-
  Minuten-Bearer, Pairing nur Loopback
- Geräte-Dialog: Geräteliste, COM-Scan, Manuell erstellen, Import (LightBurn-prefs-DeviceList,
  Secrets ausdrücklich ausgesperrt), Standard/Bearbeiten/Entfernen/Exportieren
- Geräte-Konfiguration direkt am Laser (GRBL): $-Einstellungen lesen/ändern (EEPROM),
  Injektions-Schranke, saubere Fehler-Meldungen bei belegtem Port
- Maschinenrouten mit echter Ansteuerung: jog/home/zero/goto (+gespeicherte Positionen),
  Jog-Stop (0x85), Position aus echtem GRBL-Status
- Job-Engine: Projekt-GCode (Golden-Spec-Flavor) zeilenweise mit ok-Sync, Pause/Resume/Abort
  (Realtime 0x21/0x7E/0x18), Laser-Modus-\$32-Pflicht-Gate, Fortschritt im Poll
- Eigene Icon-Suite (P1-Set) + Theme hell/dunkel als Grundlage; eigene Material-Bibliothek

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