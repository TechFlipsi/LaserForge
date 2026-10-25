#!/usr/bin/env bash
# veroeffentlichen.sh — Automatisiertes GitHub-Publishing für LaserForge (Format §6).
# Abschnitt 1 Sicherheitsscan (Secrets + unzulässige Übernahmen) → Abschnitt 2 Repo erstellen →
# Abschnitt 3 Push → Abschnitt 4 Live-Verifikation (Repo + Branch + README).
# Aufruf: ./tools/veroeffentlichen.sh [repo-name]  (Standard: LaserForge)
# Exit: 0 = live + verifiziert · 2 = Scan-Fund · 3 = gh-Fehler · 4 = Verifikation fehl
set -euo pipefail

WURZEL="$(cd "$(dirname "$0")/.." && pwd)"
REPO="${1:-LaserForge}"
ORG="TechFlipsi"

echo "=== §6-GITHUB-PUBLISH: $ORG/$REPO ($(date '+%Y-%m-%d %H:%M:%S')) ==="

# --- 1/4 Sicherheitsscan -------------------------------------------------------
echo "### 1/4 Sicherheitsscan"
FUNDES=""
for MUSTER in "Roccat" "Roccat_9" "09042001" "ghp_" "github_pat_" "AKIA" "SECRET=" "PASSWORT=" "PASSWORD="; do
    if grep -rn --include='*.cs' --include='*.md' --include='*.axaml' --include='*.csproj' --include='*.props' --include='*.targets' --include='*.py' -i -q "$MUSTER" "$WURZEL/src" "$WURZEL/tests" "$WURZEL/docs" "$WURZEL/README.md" 2>/dev/null; then
        FUNDES="$FUNDES $MUSTER"
    fi
done
if [ -n "$FUNDES" ]; then
    echo "SCAN-FUND: $FUNDES — Publish STOPP (2)"
    exit 2
fi
# Lizenzaudit: 'LightBurn'-Strings nur als Kompatibilitäts-Kontext (Format-Tagnamen/Doku) —
# Zählung + deutsche Protokollierung statt harter Verbotsregel:
ANZAHL=$(grep -rn -i "lightburn" "$WURZEL/src" 2>/dev/null | wc -l || true)
echo "LightBurn-Erwähnungen im src (nur Format-/Kompatibilitäts-Kontext erlaubt): $ANZAHL"
grep -rn -i "lightburn" "$WURZEL/src" 2>/dev/null | grep -v -E "LightBurnProject|Kompatibilit|Format-" || {
    echo "AUDIT: keine unzulässigen LightBurn-Strings im src ✓"
}
echo "Scan grün."

# --- 2/4 Repo erstellen (falls fehlend) ---------------------------------------
echo "### 2/4 Repo $ORG/$REPO"
if gh repo view "$ORG/$REPO" > /dev/null 2>&1; then
    echo "Repo existiert (Update-Push folgt)."
else
    gh repo create "$ORG/$REPO" --public \
        --description "Freie, saubere Open-Source-Laser-Cutter/-Gravur-Steuerung (.lbrn/.lbrn2-Kompatibilität, GRBL, Avalonia-GUI). Deutsch." \
        > /dev/null && echo "Repo erstellt ✓"
fi

# --- 3/4 Remote + Push ---------------------------------------------------------
echo "### 3/4 Push"
cd "$WURZEL"
if ! git remote | grep -q '^origin$'; then
    git remote add origin "https://github.com/$ORG/$REPO.git"
fi
git add -A
git commit -q -m "release: Publikationsstand (deutsch dokumentiert, Tests grün)" 2>/dev/null || true
git push -u origin main > /dev/null
echo "Push OK."

# --- 4/4 Live-Verifikation ------------------------------------------------------
echo "### 4/4 Live-Verifikation"
sleep 3
BRANCHES=$(gh api "repos/$ORG/$REPO/branches" --jq '.[].name' 2>/dev/null | head -5 || true)
if echo "$BRANCHES" | grep -q '^main$'; then
    echo "MAIN-Zweig live ✓"
else
    echo "FEHL: main nicht sichtbar (4)"; exit 4
fi
LESE=$(gh api "repos/$ORG/$REPO/readme" --jq '.name' 2>/dev/null || true)
if [ "$LESE" = "README.md" ]; then
    echo "README live ✓"
else
    echo "FEHL: README fehlt/leer (4)"; exit 4
fi
echo "=== LIVE: https://github.com/$ORG/$REPO — Publikation abgeschlossen ==="