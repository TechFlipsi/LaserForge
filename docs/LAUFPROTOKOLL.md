# Laufprotokoll — LaserForge

## 25.10.2026 — Neuanfang (Sir-Order)
- Repo /root/laserforge neu (LaserForge, GPL-3.0-only). Keine Reste aus dem geloschten Alt-Projekt.
- Track-A-Basis: LightBurn Core 2.2.00 am Stand-PC (Elegoo/GRBL/COM3/115200, 400x400 mm),
  REST 19520 (offizielle API) live abgefragt: status/units/position/layers/project/cuts = 200 OK,
  CutSettings-Referenz (80 KB) gesichert. Format-Spec + Fixtures auf NAS.
- GUI-Benchmark: LightBurn-Hauptfenster 1920x1080 erfasst (PrintWindow);
  Layout-Messlatte verankert (6 Rechts-Tabs, rote Konsole-Band, Laser-Panel COM/Elegoo,
  9-Punkte-Grid, Layer-Leiste 00..30 + T1/T2).
- Sub-Agent-1 (Recherche/REA-Track) + Sub-Agent-2 (Parity + GCode-Spec) parallel gestartet (Abschnitt 2 des Format-Plans).
- M1-Skelett komplett (deutsche Bezeichner): Solution, Core, Devices, Api, Cli, Desktop (Avalonia 12), Tests.
- QA-Lernen: $pid-Falle im Windows-PowerShell-Fenstercallback behoben; LightBurn braucht ~70s bis ein Hauptfenster mit Handle existiert.

### 18:00-18:20 — M4/M7-Live-Beweise + Automatik-Zyklus
- MCP-stdio LIVE: initialize → protocolVersion 2024-11-05 + serverInfo ✓ · tools/list → 3 Werkzeuge (deutsch) ✓ ·
  tools/call projekt_konvertieren: nameplate.lbrn → 2 Formen → 889 GRBL-Zeilen ✓.
- REST-Protokollparität LIVE (eigener Dienst :19540 via Task forge_api — SSH-Kinder sterben beim Sitzungsende,
  geplante Tasks überleben = bewährtes Rezept!): Pairing → Secret ✓ · HMAC-Minuten-Bearer ✓ ·
  status/position/units/layers/cuts/project = 200 ✓ · ohne Bearer 401 ✓ — der lightburn_rest-Client
  (am echten LightBurn geprüft) spricht die eigene API ohne Änderung = Protokoll-Interoperabilität bewiesen.
- Automatik-Zyklus (tools/qa/lauf.sh) = GRÜN mit Screenshot: Bau (0 Fehler) + 55/55 Tests + GUI-Start
  (Session 1) + PrintWindow-Capture + NAS-Beweisablage.
- QA-Kollisionslehre: gleiche EXE-Namen töteten sich gegenseitig → Sandbox-eigene Namen
  (ForgeGui = LaserForgeGui.exe via Directory.Build.targets — Achtung stale-Kombinations-Falle: bin/obj räumen!) +
  QA-Sandbox = LaserForge.Desktop.exe; taskkill NUR den eigenen Namen.
- App-Icon (eigenes „Schmiede-Strahl“-Design) ✓ Vision-QA-Runde: bestanden.
