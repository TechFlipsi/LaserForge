# Funktionsumfang-Abgleich: LightBurn-Doku (public) ↔ Binary-Scan (04.11.2026)

Frage von Sir: Deckt der Datei-/Binary-Scan den offiziellen Funktionsumfang — oder kamen
SCAN-NEU Funde dazu? Antwort: **Beides** — der Scan deckt den technischen Unterboden der
Doku UND enthält Extras, die LightBurn nicht dokumentiert. Plus: Sirs Hell-Modus-Anmerkung ist
offiziell verifiziert (§1).

## 1. Application Theme (Sirs weiß-Modus — offiziell verifiziert)
Docs 2.2 SettingsPreferences: **Application Theme** = Dropdown hell/dunkel, Default
**„System Theme"** (folgt der OS-Einstellung), Wechsel erfordert Neustart. Zusätzlich
**„Use Dark Background"** = Arbeitsfläche im Dark-Theme extra abdunkeln (das erklärt unsere
gemessene Arbeitsfläche #0c0c0c). Weitere UI-Prefs: Cursor/Node-Größe (Normal/Larger/Largest),
Curve Quality, Antialiasing-Toggle.
→ **LaserForge-Aktion:** eigenes Theme-System mit hell/dunkel/System-auto + separater Canvas-
Abdunkler-Schalter. Unsere Messwerte (§Icon-Redraw-Spec) = dunkle Palette; helle Palette
als zweite Ressourcen-Satz (Messung: LightBurn auf hell umstellen → Screencap-Runde 2).

## 2. Offizieller 2.2-Funktionsumfang (NewFeatures, kurz)
- True Nest (Pro-only, Kontur-Nesting, 4 Optimierungsstufen) + Quick Nest (2.1) + SVGNest (extern)
- Center Line Trace (Medial-Axis-Tracing), Multi-Page-Projekte (Pages-Window)
- Camera Print & Cut (Labs, Head-Kamera + Registrier-Marken)
- 3D-Modell-Import für Tiefenraster: .stl .3mf .amf .obj .off .ply → Depth-Map
- REST API (Labs) — offiziell dokumentiert auf GitHub (unsere Basis!)
- My Laser Tools im Web-Tools-Menü; User Tools (eigene Web-Tool-Einträge)
- Geräte: ACCELaser HD1 (flying galvo, Dual-Kamera, LiDAR), Bambu R1 (GCode-Export, Device-Config),
  Epilog (Labs), xTool-Verbesserungen (WiFi F1/F2/P2, Multi-Source F1, Autofokus P2/P2S-IR)
- Find My Laser erkennt Netzwerk-Geräte (Epilog/Creality/xTool)
- Duet/RRF-HTTP-Flavor im Custom-GCode-Editor; Macro-Hotkeys; In-App-Bug-Report;
  macOS-ARM-Builds (2.2 Pflicht ab macOS 28)
- EZCad3: Laser-Ready-Input, Power-Map-Import, UV/YAG-Pulsbreiten-Frequenz statt Power (2.2.01-Patches)

## 3. Vollständiger dokumentierter Werkzeugkatalog (Reference = 179 Seiten)
Gruppen (Sitemap 2.2, 343 Seiten gesamt):
- **Layout/Design:** Draw Lines/Shapes, Text + VariableText(+Formatting), BarCode, ArtLibrary,
  ShapeProperties, AddTabs, Measure, TangentCircle, ImageOptions/AdjustImage/ApplyMaskToImage,
  MultiFileTraceImage, TraceImage (inkl. Center-Line), ConvertToBitmap/Path, CylinderCorrection,
  Warp/TaperWarp, CreateRubberBandOutline, GeneratePathFromCut, RadiusFillet, MakeSameWidthHeight
- **Bearbeiten:** EditNodes, TrimShapes, AutoJoin, ClosePath(+Tolerance), DeleteDuplicates,
  BreakApart, OptimizeSelectedShapes, BooleanTools, CutShapes, FlipMirror, OffsetShapes,
  GridArray, CircularArray, CopyAlongPath, ApplyPathToText, TwoPointRotateScale, Grouping, LockShapes,
  UndoRedo, Clipboard, SetStartPoint, Break Apart, SplitMarking, RepeatMarking, Snapping,
  AutomaticGuidelines, DrawOrder, CenterFinder
- **Anordnung:** Align, Distribute, MoveSelectedObjects, PositionLaser/MoveLaserToSelection,
  Nesting (TrueNest/QuickNest/SVGNest), Pages, PrintAndCut, CameraPrintAndCut
- **Fenster/UI:** Cut/Layers, Move, Console, Camera(+Wizards: Add/Alignment/Calibration/HeadMounted/LiDAR/
  SaveBackgroundCapture), Move/CutsLayers/Cameras/FileList/Macros/HWC-Window, Dock-System,
  UI-Seiten je Menü/Toolbar (Arrange/Creation/Main/Menu/Modes/Modifiers/Status/Toolbars),
  Workspace, ViewStyle (Filled/Wireframe/Edge-Quality), BeginnerMode, Tooltips, LanguageMenu,
  EditHotkeys, HelpAndNotes, ShowNotes
- **Maschinen:** Devices, DeviceSettings (Basic/Additional/EzCad3/CustomGCode/Dimensions/ExternalAxes/
  GCode/GalvoBasic/GalvoPorts), DeviceTypes (ACCELaser/EZCad3/Epilog/xTool), GalvoFraming,
  CalibrateGalvoLens, FocusTest, IntervalTest, MaterialTest, DualLaserControl, RotaryMode
  (DSP/GCode/Galvo), FeederSetup, GetControllerInfo, LaserWindow, Preview, Print, LightBurnBridge,
  RestAPI (+FreeCAD-/Inkscape-Extension), Optimierung (OptimizationSettings), Licensing,
  ManagingPreferences (inkl. Application Theme), EnableDebugLog, ViewUsageData, Hidden-Seiten
  (Branding/DebugDrawing/GenerateSupportData/VendorBundles — interne Doku), UserBundles

## 4. Deckung: Wo der Scan die Doku bestätigt/erweitert
| Dokumentiert | Scan-Nachweis | Wert für LaserForge |
|---|---|---|
| REST API (18 Routen) | openapi + Registrator dekompiliert | Parität M7/M7.5 ✓ (+Mehr, §5) |
| Geräte-Flavors (xTool/Epilog/EZCad3/ACCELaser/Bambu) | RTTI-Protokoll-Klassen + Flavor-Configs embedded | Geräte-Roadmap-Gerüst |
| 3D-Import | `acclplanner.dll` (12 Imports) + ico3D_*-Icons | Format-Roadmap (.stl/3mf/…) |
| Nesting-Gruppe | Icons: grid-three-up/tabs + Nesting-Doku | GUI-Baustein |
| Camera/LiDAR | camera-*/lidar-* Icons + Wizards-Doku + Kamera-Snapshot-URLs im Code | Kamera-Ebene-Plan |
| LightBurn Bridge | lb_bridge-128 + Connection_TCP_Relay-Klasse | Netz-Geräte-Roadmap |
| Multi-Page | page-add/duplicate/remove/settings-256-Icons | Pages-Feature |
| Macros + Hotkeys | MacrosWindow-Doku + key_glyphs | Automatisierungs-Layer |
| Print&Cut | print_and_cut.png + pnc-target-1/2 | Labs-Feature spät |
| Material/Art-Library | filetype-*-256-Icons + MaterialLibrary-Doku | Library-Dialoge |
| Theme/Ul-Feats | QSS-Fragmente + 246 Icon-Namen | Theme-System (§1) |

## 5. SCAN-NEU (im Binary gefunden, NICHT öffentlich dokumentiert)
1. **~11 API-Routen nur im Code**: jog, jog/stop, home, zero, zero/edit, goto, goto/saved/add,
   goto/saved/go, spindle/toggle|speed, coolant/vacuum/toggle, aux/stop, override/speed|power,
   job/start|pause|stop, bind, _internal/standby (+ Forbidden-localhost-Zeile)
2. **65-Klassen-Routing-Inventar** (Connections incl. CDM/TCP_Packet/Relay, Protokolle incl.
   Worker_XTool_V1/V2-Trennung, GCode-Thread-Runner je Stack) — die vollständige
   Architekturkarte, die in der Doku nicht auftaucht
3. **WebAPI-Flavor-Configs embeded** (xtool_v1/v2/ws/probe, creality_falcon, rrf, wecreat,
   credentials_xtool_v1.dfc) inkl. api/laser/*-Route-Sets und Kamera-Snapshot-URLs
4. **246 Icon-Namen + QSS-Style-Fragmente** (Dock-Verlauf schwarz→rot→orange, 16px-Indicators,
   on=grün/off=rot) — die komplette Asset-Liste + Stylewerte für den Redraw
5. **EzCad-SDK-Layer** (param/executor/crypto-DLLs mit BslGalvo-Kalibrier-Funktionen)
6. **Geräte-Erkennungslketten** („Uno: Probably GRBL", „FTDI: Probable Ruida/TopWisdom")
7. **Konfig-Interna**: prefs.ini-Struktur, webtools.json (Cuttle/MyLaserTools-Domains), telemetry.db

## 6. Fazit für den Nachbau
- Der Scan deckt **nicht die komplette UX-Oberfläche** — dafür ist die 179-Seiten-Referenz die
  Feature-Checkliste (übernommen als Quellen-Zeile in parity-checklist).
- Der Scan deckt **plus Extras** die API-/Architektur-/Asset-Ebene — wir kennen interne
  Routen, Klassen und Stylewerte, die in keiner Doku stehen.
- Für LaserForge: Feature-Backlog = Kapitel 2+3 (Doku); Umsetzungs-Beweise = Kapitel 4/5 (Scan).
- Theme-System hell/dunkel/System + „Use Dark Background"-Schalter als eigene Baustein-Tour
  (Sir-Anforderung weiß-Modus) — Messrunde für die helle Palette offen (Screencap nach Sirs Umstellung).
