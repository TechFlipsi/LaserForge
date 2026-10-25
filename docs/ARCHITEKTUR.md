# Architektur — LaserForge

```mermaid
flowchart TD
    CLI[Cli: laserforge-cli] --> CORE
    GUI[Desktop: Avalonia-Hauptfenster] --> CORE
    GUI --> DEV
    API[Api: QA-REST :19530] --> CORE
    CORE[Core: Geometrie / Formen / Format-IO / Optimierer / GCode]
    DEV[Devices: Serial-Transport + GRBL] --> CORE
```

Kern-Entscheidungen: Core ohne Fremdpakete (BCL) — alles auf Zug/SchnittEinstellung/Projekt;
legacy-Format als Schreibnorm; GRBL mit M4/S-Skala/mm-min; Transport-Abstraktion fuer spaetere
Protokolle; QA-Harnisch am Stand-PC via schtasks + PrintWindow-DWM.
Funktionstabelle: docs/parity-checklist.md.
