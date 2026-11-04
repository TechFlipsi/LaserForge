# Kenntnisstand & Abweichungen LightBurn ↔ LaserForge (04.11.2026)

Quellen: REA-Befund 25.10. (rea-lightburn-core-20261025.md), Craft-Anything-Deep-Session 04.11.
(NAS: Fabian/LightBurn/references/craft-anything-lightburn-20261104.md), offizielle openapi.yaml
(v2.2, LightBurn-REST-Repo), Craft-Icon/Theme-Fund (dieses Dokiment, §Icons).

## 1. NEU ERWORBENE KENNTNISSE (seit REA)

### 1.1 REST-API
- Offizielle openapi.yaml umfasst **18 Routen** (neu dokumentiert: file/open, overlay/metadata,
  material-library, events/poll) — LaserForge spiegelt heute 10.
- **~11 weitere Routen nur im Code** (Registrator sub_140103f60 dekompiliert, 658 Zeilen):
  jog, jog/stop, home, zero, zero/edit, goto, goto/saved/add, goto/saved/go,
  spindle/toggle, spindle/speed, coolant/toggle, vacuum/toggle, aux/stop,
  override/speed, override/power, job/start, job/pause, job/stop, bind, _internal/standby.
- Gesamtschema-Inventur: PollSnapshot {position, job, overrides, aux, connection, settings},
  JobState (idle|running|paused + progress), Overrides (speed_percent/power_percent),
  AuxState (spindle_on/spindle_rpm/coolant_on/vacuum_on), JogSettings (FLAT: xy_distance,
  z_distance, xy_speed, z_speed, units, distance_units), ConnectionStatus, LaserCut
  (in_use/shape_count/profile galvo|gantry|unknown/global_passes/tabs), Position (mm immer).
- file/open: Header X-Filename, 202 {"status":"accepted","filename"} + file_imported-SSE;
  success-Flag nur bei Nativen (.lbrn/.lbrn2) verlässlich.
- material-library: {"product":"lightburn","name","filename","entries":[{guid, material,
  description, no_thick_title, thickness(-1), locked, cut(index=-1,layer_index=-1)}]}.
- override-Grenze: GRBL-Override-Bereich 10–200 % (F/S-Realtime).
- "Forbidden: /api/connect is only available from localhost" im Code verifiziert.

### 1.2 Protokoll-/Geräteebene (RTTI-Klassen, 65)
- 14 Connection-Klassen (Serial Async/Sync, TCP±Packet/Relay, UDP, LibUSB, HTTP_Sync, WebApi, Epilog, CDM).
- 33 Protokoll-Klassen: GRBL(+LPC/M3/iLaser), grblHAL, GCode Thread-Runner je Stack
  (Grbl/Marlin/RepRap/Smoothie/XTool/Emblaser3), EZCad3(+Thread), BSLFiber(+Thread),
  JCZFiber(+Thread), Ruida, Topwisdom, Trocen, Galvo, CanCam, Cubiio, ACCELaser, LBP,
  SnapMaker, LinuxCNC, Gerbil_STM, FabKit, Emblaser 1A3/1A4/2/3, Flavor_Creality_Falcon/
  RRF_HTTP/WeCreat_Http, XTool(+HTTP), Worker_XTool_Base/V1/V2, Epilog.
- WebAPI-Flavor-Configs EMBEDDED: webapi_xtool_v1|v1_ws|v2|probe.json, creality_falcon_v1, rrf_v1,
  wecreat_v1.json, credentials_xtool_v1.dfc (in der EXE als Qt-Ressource :/res/webapi/).
- Geräte-Erkennungs-Strings: „Uno: Probably GRBL", „Atmega: Probably GRBL, could be LPC",
  „FTDI: Probable Ruida / TopWisdom (possibly GRBL)".
- Kamera-Snapshot-HTTP: /api/module/camera/snapshot?name=global|th… (Flavor-APIs).

### 1.3 GUI-Ressourcen-Ebene (NEU = dieses Fund)
- **246 Icon-Ressourcen benannt** (§3 = Redraw-Katalog) + key_glyphs + 14 Hersteller-Logos.
- **QSS-Theme-Fragmente im Klartext**: QDockWidget-Header-Verlauf
  (0,0,0 → 190,50,50 → 220,120,50, l-n-r), QCheckBox/QGroupBox-Indicator 16px mit
  switch-on-128-g / switch-off-128-r, QToolBar-Handle 12×24 (h) / 24×12 (v), QDockWidget-Weißtext.
- Toggle-Semantik im Theme: ON=grün, OFF=rot (Farbcode des Originals).

## 2. ABWEICHUNGS-MATRIX (wo unser Nachbau steht)

| Ebene | LightBurn 2.2 | LaserForge v0.1.0 | Aktion (Priorität) |
|---|---|---|---|
| REST-Endpunkte | 18 dokumentiert + ~11 code-only | 10 | M7.5: +14 Routen (vorbereite Plan fertig) |
| Auth/Pairing | HMAC-Minuten-Bearer, connect nur Loopback | identisch ✓ | — |
| PollSnapshot (events/poll) | position/job/overrides/aux/connection/settings | fehlt | M7.5 |
| JobState/Overrides/Aux | State-Maschine im Dienst | fehlt | M7.5 (State-Layer) |
| file/open | 202 + SSE-Ergebnis | fehlt | M7.5 |
| material-library | CutLibrary-Flatliste | fehlt | M7.5 (leere Bibliothek) |
| overlay/metadata | Kamera-Overlay + revision | fehlt (kein Kamera) | 404-Parität reicht |
| .lbrn2-Import | voll (inkl. Templates .lbt) | Import ja (LeserLbrn), Sirs Echte-Probe offen | O2 aktiv |
| .lbrn2-Export/Save | nativ | offen prüfen | M-next |
| Geräte-Protokolle | 33 Klassen, 14 Transporte | GRBL (COM4-MKS live), Flavor offene | Geräte-Roadmap nach RTTI-Liste |
| WebAPI-Flavors | xTool V1/V2/WS/Probe, Creality, RRF, WeCreat | fehlt | später (Protokoll-Ebene 2) |
| Werkzeug-Tabs | 2.2 = 6 Tabs | teils (5) | M-next GUI |
| Icons/Theme | 246 Icons + QSS-Verlauf schwarz→rot→orange | eigenes Design | §3 Redraw-Plan |
| Material-Library GUI | Library-Dialog | fehlt | später |
| Kamera/LiDAR | Overlay/Wizards/2.2-LiDAR-Höhenausgleich | fehlt | weit hinten |
| Print&Cut, Array/Offset/Weld-Tools | vorhanden | fehlt | GUI-Roadmap |
| Undo/Redo, Konsole, Preview, Jog-Panel | vorhanden | vorhanden (M5) ✓ | — |

## 3. ICON-STRATEGIE LEAGAL 1:1-ANSEHN (Sir-Ziel)

Rechtslage festgestellt:
- LightBurn-Icons = proprietäre Eigenartwork (im Install-Ordner NUR Code-Lizenzen: OpenCV,
  zstd, jagua-rs, Sparrow; KEINE Icon-Pack-Lizenz). **Direktkopie der Pixel = Urheberrechtsverstoß.**
- Legaler Weg zu „denselben Icons": **Clean-Room-Redraw** — Original ansehen (Screenshots des
  laufenden, von Sir lizenzierten LightBurn), eigene SVG/PNG-Glyphen im selben Stil zeichnen
  (gleiche Formensprache, eigene Bytes). Namen/Formenliste darf als Funktions-Referenz dienen.
- **AUSGEKLAMMERT (Marken/Fremdrecht):** alle Vendor-Logos (grbl/marlin/ruida/topwisdom/
  trocen/zhiyuan/xtool/epilog/JCZ/BSL/EZC3/Cubiio/LotusLaser/fabcreator/accelaser/iLaser/
  gerbil/smoothie/linuxcnc/lb_bridge/lightburn-square) — wir zeigen NUR NAMEN als Text, keine
  Nachbauten der Logos.

Redraw-Katalog (Priorität 1 = sichtbar in der Haupt-Leiste):
P1 Werkzeuge: select(cursor-128), edit(Pencil edit-2), move/pan(hand-3), zoom(zoom-in/out-2),
  frame(frame-all), square/circle/line/polygon/text(edit-C/letter-a), image(picture-size-*),
  offset(offset/offset-C/offsetfill), nodes(add/del/break-node, curve-to-line, line-to-curve,
  corner/smooth), array(grid-three-up/tabs-128), weld/bool-union/difference/intersection,
  flip-h/v, rotate-128, mirror-along-line, copy/paste/delete, undo/redo.
P2 Bedienfeld: play-128, startpoint, focus, unidirectional/bidirectional, laserpos,
  switches on/off(g/r), align-* (12), distribute-* (6), padlock, pin, lower/upper-left/right,
  center-both, circled Varianten (-C-128 = Kreis-Überlagerung).
P3 Tabs/Dialoge: page-add/duplicate/remove/settings, filetype-*-256 (8), camera-*,
  3D (ico3D_*), alerts (icoAlert*), Icons_*_128 (3), arrow-*(4), plus/minus/hashtag/help/question/bug/user,
  wireless/usb-off/ethernet-off, cnc-machine, conference, labs, barcode, hexagon, ghostie, shx,
  ruler-120, menu-header, print_and_cut, zoom-page/selection, refresh-256, close-window, desktop-2,
  ban, feature, letter-a, square/circle/hexagon/polygon-C, pnc, keyGlyphs (3).
Umsetzung: SVG-Quelle pro Glyph (24px-Gitter, 2px-Linie, Theme-Farben), Export 128/64/32/16 PNG;
QSS-Werte als AVALONIA-Styles (eigene Datei, abgelesene Werte: Dock-Verlauf, 16px-Indicators).

## 4. NÄCHSTE SCHRITTE
1. M7.5 REST-Parität (+14 Routen, State-Layer Overrides/Aux/Job, PollSnapshot) — Spec vorliegen. [läuft]
2. Icon-Redraw: P1-Katalog (~40 Glyphen) — braucht Screenshots vom laufenden Fensters
   (LightBurn-Fenster nach vorn holen → Capture via Craft runtime-ui-read --screenshot).
3. Theme-Umsetzung (Avalonia-Styles nach abgelesenen QSS-Werten).
4. .lbrn2-Roundtrip (O2) mit Sirs echter Probe.
5. Geräte-Roadmap nach RTTI-Liste (GRBL-Varianten zuerst: grblHAL/LPC/M3/iLaser).
