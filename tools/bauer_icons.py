#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""LaserForge Icon-Bauer (P1-Kernset) — eigene Glyphen (Clean-Room, keine LightBurn-Bytes).
Erzeugt assets/icons/lf_<name>.png (128px, Linien-Style) + Kontaktbogen."""
from PIL import Image, ImageDraw
import os

W = 128          # Ziel
S = 4            # Supersampling
V = W * S
FARBE = (230, 230, 230, 255)   # #E6E6E6
STARK = 10                     # Strichstaerke in Ziel-px (→ 40 in SSAA)
AUS = os.path.join(os.path.dirname(__file__), '..', 'assets', 'icons')
os.makedirs(AUS, exist_ok=True)

def leinwand():
    i = Image.new('RGBA', (V, V), (0, 0, 0, 0))
    return i, ImageDraw.Draw(i)

def l(d, x1, y1, x2, y2, w=STARK):
    d.line([(x1*S, y1*S), (x2*S, y2*S)], fill=FARBE, width=w*S)

def re(d, x1, y1, x2, y2, w=STARK):
    d.rectangle([x1*S, y1*S, x2*S, y2*S], outline=FARBE, width=w*S)

def kr(d, x1, y1, x2, y2, w=STARK):
    d.ellipse([x1*S, y1*S, x2*S, y2*S], outline=FARBE, width=w*S)

def pol(d, punkte, w=STARK, schliessen=True):
    pp = [(x*S, y*S) for x, y in punkte]
    if schliessen: pp.append(pp[0])
    d.line(pp, fill=FARBE, width=w*S, joint='curve')

def speichere(im, name):
    im = im.resize((W, W), Image.LANCZOS)
    im.save(os.path.join(AUS, f'lf_{name}.png'))
    return im

bilder = {}

# --- neu: Blatt mit umgeknickter Ecke
i, d = leinwand(); pol(d, [(30,18),(78,18),(98,38),(98,110),(30,110)], 9); pol(d, [(78,18),(78,38),(98,38)], 7)
bilder['neu'] = speichere(i, 'neu')

# --- oeffnen: Ordner
i, d = leinwand(); pol(d, [(16,42),(48,42),(58,30),(112,30),(112,98),(16,98)], 9); l(d, 16,58,112,58, 7)
bilder['oeffnen'] = speichere(i, 'oeffnen')

# --- sichern: Diskette
i, d = leinwand(); re(d, 20,20,108,108,9); re(d, 44,20,84,52,7); re(d, 40,72,96,108,7)
bilder['sichern'] = speichere(i, 'sichern')

# --- undo: Pfeil-links-bogen
i, d = leinwand(); d.arc([28*S,40*S,104*S,118*S], 190, 340, fill=FARBE, width=STARK*S); d.polygon([(20*S,50*S),(38*S,58*S),(24*S,70*S)], fill=FARBE)
bilder['undo'] = speichere(i, 'undo')

# --- redo (gespiegelt)
i, d = leinwand(); d.arc([24*S,40*S,100*S,118*S], 200, 350, fill=FARBE, width=STARK*S); d.polygon([(108*S,50*S),(90*S,58*S),(104*S,70*S)], fill=FARBE)
bilder['redo'] = speichere(i, 'redo')

# --- auswahl: Pfeil-Cursor
i, d = leinwand(); pol(d, [(34,16),(34,104),(56,80),(70,108),(84,100),(70,72),(96,70)], 8)
bilder['auswahl'] = speichere(i, 'auswahl')

# --- knoten: Linie mit 3 Punkten
i, d = leinwand(); l(d, 20,100,108,28,8)
for x, y in [(20,100),(64,64),(108,28)]: d.ellipse([x*S-9, y*S-9, x*S+9, y*S+9], fill=FARBE)
bilder['knoten'] = speichere(i, 'knoten')

# --- pen: Stift schraeg
i, d = leinwand(); pol(d, [(36,92),(88,40),(100,52),(48,104),(32,110)], 8)
bilder['pen'] = speichere(i, 'pen')

# --- maschine: Geräterahmen + Laserkopf auf Achse
i, d = leinwand(); re(d, 12,26,116,112,9); l(d, 12,64,116,64,7)
d.ellipse([54*S,52*S,74*S,76*S], fill=FARBE)  # Laserkopf auf der Achse
bilder['maschine'] = speichere(i, 'maschine')

# --- einstellungen: Zahnrad (Kreis + kurze, kräftige Zapfen)
i, d = leinwand(); kr(d, 40,40,88,88,9)
import math
for winkel in range(0, 360, 45):
    r1, r2 = 33, 45
    mx, my = 64, 64
    x1 = mx + r1*math.cos(math.radians(winkel)); y1 = my + r1*math.sin(math.radians(winkel))
    x2 = mx + r2*math.cos(math.radians(winkel)); y2 = my + r2*math.sin(math.radians(winkel))
    l(d, x1, y1, x2, y2, 10)
bilder['einstellungen'] = speichere(i, 'einstellungen')

# --- gruppe: 3 Kasten-Verband
i, d = leinwand(); re(d, 16,16,52,52,8); re(d, 76,16,112,52,8); re(d, 16,76,52,112,8); re(d, 76,76,112,112,8)
bilder['gruppe'] = speichere(i, 'gruppe')

# --- zoom-in: Lupe mit +
i, d = leinwand(); kr(d, 20,20,84,84,9); l(d, 92,92,116,116,10); l(d, 38,52,66,52,7); l(d, 52,38,52,66,7)
bilder['zoom-in'] = speichere(i, 'zoom-in')

# Kontaktbogen 4x3
raster = list(bilder.values())
spalten, zeilen = 4, 3
bogen = Image.new('RGBA', (spalten*W, zeilen*W), (30, 30, 30, 255))
for i2, im in enumerate(raster[:spalten*zeilen]):
    bogen.paste(im, ((i2 % spalten)*W, (i2 // spalten)*W), im)
bogen.save(os.path.join(AUS, 'kontaktbogen_p1.png'))
print('ICONS_OK:', ', '.join(bilder.keys()))
print('Bogen:', os.path.join(AUS, 'kontaktbogen_p1.png'))
