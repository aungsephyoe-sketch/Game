#!/usr/bin/env python3
"""Paints the accessory icons (Assets/Resources/Gear/<id>.png).

Every accessory gets its own drawn design on a rarity-coloured tile: shaded shapes (light top, dark bottom),
dark ink outlines, glossy highlights, faceted gems and metal rims. Legendary tiles get light rays, Mythic tiles
sparkles. Drawn at 4x and downsampled for smooth edges.

    python3 tools/gen_accessory_art.py [preview.png]
"""
import math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

S = 256
SS = 4
W = S * SS
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Gear")

RARITY = {2: (191, 199, 204), 3: (89, 166, 255), 4: (191, 102, 255), 5: (255, 199, 64), 6: (255, 77, 115)}
INK = (22, 16, 32)


def C(x, y):
    return (x * W, y * W)


def mask():
    return Image.new("L", (W, W), 0)


def shade(m, top, bot, hl=True, ol=True, olw=5, glow=None):
    """A filled shape with a vertical gradient, an ink outline and a glossy highlight."""
    layer = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    bbox = m.getbbox()
    if bbox is None:
        return layer
    if ol:
        # Fast dilation: blur the shape and keep everything the blur touched.
        om = m.filter(ImageFilter.GaussianBlur(olw * SS * 0.6)).point(lambda v: 255 if v > 12 else 0)
        layer.paste(Image.new("RGBA", (W, W), INK + (255,)), (0, 0), om)
    y0, y1 = bbox[1], bbox[3]
    t = np.clip((np.arange(W) - y0) / max(1, (y1 - y0)), 0, 1)[:, None]
    top = np.array(top, float); bot = np.array(bot, float)
    rgb = (top * (1 - t) + bot * t)[:, None, :].repeat(W, axis=1).reshape(W, W, 3)
    grad = Image.fromarray(np.dstack([rgb, np.full((W, W), 255.0)]).astype(np.uint8), "RGBA")
    layer.paste(grad, (0, 0), m)
    if hl:
        x0, y0b, x1, y1b = bbox
        h = mask()
        ImageDraw.Draw(h).ellipse([x0 + (x1 - x0) * 0.12, y0b + (y1b - y0b) * 0.06, x0 + (x1 - x0) * 0.55, y0b + (y1b - y0b) * 0.34], fill=120)
        h = Image.fromarray((np.array(h, float) * np.array(m, float) / 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(SS * 3))
        layer.paste(Image.new("RGBA", (W, W), (255, 255, 255, 255)), (0, 0), h)
    return layer


def comp(base, *layers):
    for l in layers:
        base.alpha_composite(l)
    return base


def ell(m, cx, cy, rx, ry, fill=255):
    ImageDraw.Draw(m).ellipse([C(cx - rx, cy - ry), C(cx + rx, cy + ry)], fill=fill)
    return m


def poly(m, pts, fill=255):
    ImageDraw.Draw(m).polygon([C(x, y) for x, y in pts], fill=fill)
    return m


def line(m, pts, w, fill=255):
    ImageDraw.Draw(m).line([C(x, y) for x, y in pts], fill=fill, width=int(w * W), joint="curve")
    for x, y in (pts[0], pts[-1]):
        ell(m, x, y, w / 2, w / 2, fill)
    return m


def sub(a, b):
    return Image.fromarray(np.clip(np.array(a, int) - np.array(b, int), 0, 255).astype(np.uint8))


def gem(img, cx, cy, r, col, sides=6, rot=0.0, glow=True):
    """A faceted gem: glow, dark base, a lighter crown facet and a bright glint."""
    if glow:
        g = ell(mask(), cx, cy, r * 1.8, r * 1.8, 150).filter(ImageFilter.GaussianBlur(r * W * 0.5))
        img.paste(Image.new("RGBA", (W, W), col + (255,)), (0, 0), g)
    pts = [(cx + math.cos(rot + i * 2 * math.pi / sides) * r, cy + math.sin(rot + i * 2 * math.pi / sides) * r) for i in range(sides)]
    dark = tuple(int(c * 0.45) for c in col)
    comp(img, shade(poly(mask(), pts), tuple(min(255, int(c * 1.2)) for c in col), dark, hl=False, olw=4))
    inner = [(cx + (x - cx) * 0.55, cy + (y - cy) * 0.55 - r * 0.08) for x, y in pts]
    comp(img, shade(poly(mask(), inner), tuple(min(255, int(c * 0.7 + 90)) for c in col), col, hl=False, ol=False))
    comp(img, shade(ell(mask(), cx - r * 0.3, cy - r * 0.35, r * 0.2, r * 0.13), (255, 255, 255), (230, 240, 255), hl=False, ol=False))


def metal_ring(img, cx, cy, r, w, top=(255, 226, 120), bot=(170, 110, 30)):
    m = sub(ell(mask(), cx, cy, r, r), ell(mask(), cx, cy, r - w, r - w))
    comp(img, shade(m, top, bot, hl=True, olw=4))


def chain(img, pts, col=(220, 200, 150)):
    for i in range(len(pts) - 1):
        (x0, y0), (x1, y1) = pts[i], pts[i + 1]
        n = max(2, int(math.hypot(x1 - x0, y1 - y0) / 0.035))
        for k in range(n):
            t = k / n
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            m = sub(ell(mask(), x, y, 0.017, 0.012), ell(mask(), x, y, 0.009, 0.005))
            comp(img, shade(m, col, tuple(int(c * 0.55) for c in col), hl=False, olw=2))


def sparkle(img, cx, cy, s, col=(255, 255, 240)):
    m = mask()
    poly(m, [(cx, cy - s), (cx + s * 0.18, cy - s * 0.18), (cx + s, cy), (cx + s * 0.18, cy + s * 0.18), (cx, cy + s), (cx - s * 0.18, cy + s * 0.18), (cx - s, cy), (cx - s * 0.18, cy - s * 0.18)])
    img.paste(Image.new("RGBA", (W, W), col + (255,)), (0, 0), m.filter(ImageFilter.GaussianBlur(SS)))


def tile(rarity, seed):
    rc = RARITY[rarity]
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    yy, xx = np.mgrid[0:W, 0:W] / W
    d = np.sqrt((xx - 0.5) ** 2 + (yy - 0.45) ** 2)
    k = np.clip(d * 1.7, 0, 1)[..., None]
    center = np.array([c * 0.85 for c in rc], float)
    edge = np.array((14, 14, 26), float)
    rgb = center * (1 - k) + edge * k
    bg = Image.fromarray(np.dstack([rgb, np.full((W, W), 255.0)]).astype(np.uint8), "RGBA")
    if rarity >= 5:
        rays = mask()
        rd = ImageDraw.Draw(rays)
        for i in range(12):
            a0 = i * math.pi / 6 + seed
            rd.polygon([C(0.5, 0.45), C(0.5 + math.cos(a0) * 1.0, 0.45 + math.sin(a0) * 1.0), C(0.5 + math.cos(a0 + 0.14) * 1.0, 0.45 + math.sin(a0 + 0.14) * 1.0)], fill=45)
        bg.paste(Image.new("RGBA", (W, W), (255, 255, 255, 255)), (0, 0), rays.filter(ImageFilter.GaussianBlur(SS * 2)))
    tm = mask()
    ImageDraw.Draw(tm).rounded_rectangle([C(0.03, 0.03), C(0.97, 0.97)], radius=int(0.13 * W), fill=255)
    img.paste(bg, (0, 0), tm)
    # Frame: rarity rim with an inner light line.
    fm = mask()
    ImageDraw.Draw(fm).rounded_rectangle([C(0.03, 0.03), C(0.97, 0.97)], radius=int(0.13 * W), outline=255, width=int(0.028 * W))
    img.paste(Image.new("RGBA", (W, W), rc + (255,)), (0, 0), fm)
    im = mask()
    ImageDraw.Draw(im).rounded_rectangle([C(0.065, 0.065), C(0.935, 0.935)], radius=int(0.1 * W), outline=110, width=int(0.006 * W))
    img.paste(Image.new("RGBA", (W, W), (255, 255, 255, 255)), (0, 0), im)
    return img


def shadow(img, cx, cy, rx, ry):
    m = ell(mask(), cx, cy, rx, ry, 120).filter(ImageFilter.GaussianBlur(SS * 6))
    img.paste(Image.new("RGBA", (W, W), (0, 0, 0, 255)), (0, 0), m)


# ---------------------------------------------------------------------------------------------- designs

def d_charm(img):
    shadow(img, 0.5, 0.86, 0.22, 0.04)
    comp(img, shade(ImageDraw_rrect(0.32, 0.3, 0.68, 0.84, 0.06), (230, 60, 70), (140, 20, 40)))
    comp(img, shade(ell(mask(), 0.5, 0.52, 0.11, 0.11), (255, 225, 130), (190, 130, 40)))
    comp(img, shade(ell(mask(), 0.5, 0.52, 0.06, 0.06), (230, 60, 70), (160, 30, 50), hl=False, ol=False))
    comp(img, shade(line(mask(), [(0.42, 0.3), (0.5, 0.2), (0.58, 0.3)], 0.035), (255, 225, 130), (190, 130, 40)))
    comp(img, shade(ell(mask(), 0.5, 0.2, 0.04, 0.035), (255, 225, 130), (190, 130, 40)))


def ImageDraw_rrect(x0, y0, x1, y1, r):
    m = mask()
    ImageDraw.Draw(m).rounded_rectangle([C(x0, y0), C(x1, y1)], radius=int(r * W), fill=255)
    return m


def d_mask(img):
    shadow(img, 0.5, 0.88, 0.2, 0.04)
    face = poly(mask(), [(0.27, 0.22), (0.36, 0.36), (0.64, 0.36), (0.73, 0.22), (0.72, 0.5), (0.62, 0.72), (0.5, 0.84), (0.38, 0.72), (0.28, 0.5)])
    comp(img, shade(face, (255, 255, 250), (200, 205, 220)))
    for s in (-1, 1):
        comp(img, shade(poly(mask(), [(0.5 + s * 0.07, 0.5), (0.5 + s * 0.2, 0.44), (0.5 + s * 0.18, 0.5)]), (40, 20, 30), (20, 10, 20), hl=False, olw=2))
        comp(img, shade(poly(mask(), [(0.5 + s * 0.1, 0.62), (0.5 + s * 0.2, 0.56), (0.5 + s * 0.14, 0.66)]), (235, 50, 60), (170, 20, 40), hl=False, ol=False))
        comp(img, shade(poly(mask(), [(0.5 + s * 0.17, 0.26), (0.5 + s * 0.2, 0.34), (0.5 + s * 0.12, 0.34)]), (235, 50, 60), (170, 20, 40), hl=False, ol=False))
    comp(img, shade(ell(mask(), 0.5, 0.77, 0.03, 0.02), (40, 20, 30), (20, 10, 20), hl=False, ol=False))


def d_earrings(img):
    for s in (-1, 1):
        x = 0.5 + s * 0.15
        metal_ring(img, x, 0.2, 0.05, 0.018)
        comp(img, shade(line(mask(), [(x, 0.25), (x, 0.34)], 0.014), (255, 225, 130), (190, 130, 40), olw=2))
        comp(img, shade(ImageDraw_rrect(x - 0.1, 0.34, x + 0.1, 0.8, 0.02), (255, 250, 240), (215, 205, 190)))
        comp(img, shade(ell(mask(), x, 0.5, 0.06, 0.06), (240, 60, 60), (170, 20, 30), ol=False))
        comp(img, shade(ImageDraw_rrect(x - 0.08, 0.65, x + 0.08, 0.72, 0.01), (70, 170, 80), (30, 110, 50), hl=False, ol=False))


def d_bell(img):
    shadow(img, 0.5, 0.86, 0.2, 0.04)
    comp(img, shade(line(mask(), [(0.5, 0.12), (0.5, 0.26)], 0.03), (230, 60, 70), (150, 20, 40)))
    body = poly(mask(), [(0.34, 0.66), (0.38, 0.4), (0.44, 0.3), (0.56, 0.3), (0.62, 0.4), (0.66, 0.66)])
    ell(body, 0.5, 0.68, 0.2, 0.08)
    comp(img, shade(body, (255, 230, 130), (180, 110, 30)))
    comp(img, shade(ImageDraw_rrect(0.36, 0.52, 0.64, 0.56, 0.01), (200, 140, 50), (150, 90, 20), hl=False, ol=False))
    comp(img, shade(ell(mask(), 0.5, 0.72, 0.05, 0.035), (60, 40, 20), (30, 20, 10), hl=False))
    comp(img, shade(line(mask(), [(0.5, 0.7), (0.5, 0.86)], 0.02), (230, 60, 70), (150, 20, 40), olw=2))


def d_beads(img):
    shadow(img, 0.5, 0.84, 0.25, 0.05)
    for i in range(12):
        a = i * math.pi * 2 / 12
        x, y = 0.5 + math.cos(a) * 0.26, 0.5 + math.sin(a) * 0.22
        big = i == 3
        col = (240, 200, 90) if big else (90, 200, 130)
        comp(img, shade(ell(mask(), x, y, 0.07 if big else 0.058, 0.07 if big else 0.058), col, tuple(int(c * 0.45) for c in col), olw=3))


def d_ribbon(img):
    shadow(img, 0.5, 0.86, 0.22, 0.04)
    col, dk = (255, 120, 170), (190, 50, 110)
    for s in (-1, 1):
        comp(img, shade(poly(mask(), [(0.5, 0.45), (0.5 + s * 0.3, 0.28), (0.5 + s * 0.33, 0.5), (0.5 + s * 0.28, 0.62)]), col, dk))
        comp(img, shade(poly(mask(), [(0.5, 0.48), (0.5 + s * 0.12, 0.84), (0.5 + s * 0.06, 0.78), (0.5 + s * 0.02, 0.86)]), col, dk))
    comp(img, shade(ell(mask(), 0.5, 0.46, 0.07, 0.08), (255, 160, 200), dk))


def d_suncrest(img):
    for i in range(12):
        a = i * math.pi / 6
        comp(img, shade(poly(mask(), [(0.5 + math.cos(a - 0.14) * 0.2, 0.5 + math.sin(a - 0.14) * 0.2), (0.5 + math.cos(a) * 0.36, 0.5 + math.sin(a) * 0.36), (0.5 + math.cos(a + 0.14) * 0.2, 0.5 + math.sin(a + 0.14) * 0.2)]), (255, 225, 120), (200, 120, 30), hl=False, olw=3))
    metal_ring(img, 0.5, 0.5, 0.2, 0.06)
    gem(img, 0.5, 0.5, 0.12, (255, 70, 60), 8)


def d_phoenixheart(img):
    for s in (-1, 1):
        for k in range(3):
            comp(img, shade(poly(mask(), [(0.5 + s * 0.12, 0.5 - k * 0.04), (0.5 + s * (0.38 - k * 0.04), 0.26 + k * 0.1), (0.5 + s * (0.26 - k * 0.03), 0.52 + k * 0.05)]), (255, 210 - k * 40, 80), (230, 70, 30), hl=False, olw=3))
    heart = ell(ell(mask(), 0.42, 0.44, 0.1, 0.1), 0.58, 0.44, 0.1, 0.1)
    poly(heart, [(0.325, 0.47), (0.675, 0.47), (0.5, 0.74)])
    g = heart.filter(ImageFilter.GaussianBlur(SS * 10))
    img.paste(Image.new("RGBA", (W, W), (255, 120, 60, 255)), (0, 0), g)
    comp(img, shade(heart, (255, 150, 120), (200, 20, 50)))
    sparkle(img, 0.44, 0.42, 0.04)


def d_dragonscale(img):
    chain(img, [(0.3, 0.12), (0.5, 0.3), (0.7, 0.12)])
    sc = poly(mask(), [(0.5, 0.3), (0.72, 0.42), (0.68, 0.64), (0.5, 0.84), (0.32, 0.64), (0.28, 0.42)])
    comp(img, shade(sc, (255, 225, 130), (170, 110, 30)))
    inner = poly(mask(), [(0.5, 0.36), (0.65, 0.45), (0.62, 0.62), (0.5, 0.76), (0.38, 0.62), (0.35, 0.45)])
    comp(img, shade(inner, (90, 230, 200), (20, 110, 110), ol=False))
    for k in range(3):
        comp(img, shade(line(mask(), [(0.4, 0.5 + k * 0.07), (0.5, 0.46 + k * 0.07), (0.6, 0.5 + k * 0.07)], 0.012), (40, 150, 140), (20, 90, 90), hl=False, ol=False))


def d_abyss_eye(img):
    for s in (-1, 1):
        comp(img, shade(line(mask(), [(0.5 + s * 0.2, 0.5), (0.5 + s * 0.32, 0.36), (0.5 + s * 0.38, 0.22)], 0.03), (90, 60, 120), (40, 20, 60), hl=False, olw=3))
        comp(img, shade(line(mask(), [(0.5 + s * 0.2, 0.55), (0.5 + s * 0.33, 0.68), (0.5 + s * 0.36, 0.82)], 0.03), (90, 60, 120), (40, 20, 60), hl=False, olw=3))
    eye = ell(mask(), 0.5, 0.52, 0.24, 0.14)
    comp(img, shade(eye, (80, 50, 110), (30, 15, 50)))
    gem(img, 0.5, 0.52, 0.1, (200, 90, 255), 10)
    comp(img, shade(ell(mask(), 0.5, 0.52, 0.02, 0.07), (20, 5, 30), (10, 0, 20), hl=False, ol=False))


def d_fang(img):
    chain(img, [(0.22, 0.14), (0.5, 0.34), (0.78, 0.14)], (90, 80, 100))
    for s in (-1, 1):
        comp(img, shade(poly(mask(), [(0.5 + s * 0.04, 0.36), (0.5 + s * 0.16, 0.36), (0.5 + s * 0.09, 0.78)]), (255, 252, 240), (200, 190, 175)))
    gem(img, 0.5, 0.42, 0.06, (220, 20, 50), 4, math.pi / 4)
    sparkle(img, 0.63, 0.6, 0.03)


def d_ember(img):
    for k in range(5):
        a = -math.pi / 2 + (k - 2) * 0.45
        comp(img, shade(poly(mask(), [(0.5 + math.cos(a - 0.25) * 0.18, 0.52 + math.sin(a - 0.25) * 0.18), (0.5 + math.cos(a) * 0.4, 0.52 + math.sin(a) * 0.4), (0.5 + math.cos(a + 0.25) * 0.18, 0.52 + math.sin(a + 0.25) * 0.18)]), (255, 200, 80), (230, 60, 20), hl=False, olw=3))
    metal_ring(img, 0.5, 0.56, 0.2, 0.05)
    heart = ell(ell(mask(), 0.44, 0.52, 0.07, 0.07), 0.56, 0.52, 0.07, 0.07)
    poly(heart, [(0.37, 0.54), (0.63, 0.54), (0.5, 0.7)])
    comp(img, shade(heart, (255, 170, 60), (220, 40, 20)))


def d_frost_lotus(img):
    shadow(img, 0.5, 0.84, 0.25, 0.05)
    for layer, (r, n, off) in enumerate([(0.34, 8, 0.2), (0.26, 6, 0.0), (0.17, 5, 0.3)]):
        for i in range(n):
            a = off + i * math.pi * 2 / n
            px, py = 0.5 + math.cos(a) * r * 0.55, 0.55 + math.sin(a) * r * 0.45
            petal = poly(mask(), [(0.5, 0.55), (px + math.cos(a + 1.2) * 0.06, py + math.sin(a + 1.2) * 0.06), (0.5 + math.cos(a) * r, 0.55 + math.sin(a) * r * 0.8), (px + math.cos(a - 1.2) * 0.06, py + math.sin(a - 1.2) * 0.06)])
            comp(img, shade(petal, (230 - layer * 10, 250, 255), (90 + layer * 30, 170 + layer * 20, 240), hl=False, olw=3))
    gem(img, 0.5, 0.55, 0.06, (150, 230, 255), 6)
    for p in [(0.25, 0.3), (0.75, 0.28), (0.72, 0.78)]:
        sparkle(img, p[0], p[1], 0.035, (220, 245, 255))


def d_thunder_drum(img):
    for s in (-1, 1):
        x = 0.5 + s * 0.17
        metal_ring(img, x, 0.2, 0.045, 0.016)
        comp(img, shade(line(mask(), [(x, 0.24), (x, 0.33)], 0.012), (255, 225, 130), (190, 130, 40), olw=2))
        body = ImageDraw_rrect(x - 0.12, 0.36, x + 0.12, 0.66, 0.06)
        comp(img, shade(body, (220, 70, 50), (140, 30, 30)))
        comp(img, shade(ell(mask(), x, 0.37, 0.12, 0.04), (255, 240, 210), (220, 200, 170), hl=False))
        comp(img, shade(poly(mask(), [(x - 0.01, 0.42), (x + 0.05, 0.42), (x + 0.01, 0.5), (x + 0.05, 0.5), (x - 0.04, 0.62), (x - 0.01, 0.52), (x - 0.05, 0.52)]), (255, 240, 90), (240, 180, 20), hl=False, olw=2))
        for k in range(4):
            comp(img, shade(ell(mask(), x - 0.09 + k * 0.06, 0.64, 0.012, 0.012), (255, 225, 130), (190, 130, 40), hl=False, ol=False))


def d_jade_turtle(img):
    shadow(img, 0.5, 0.84, 0.25, 0.05)
    for p in [(0.28, 0.38), (0.72, 0.38), (0.3, 0.7), (0.7, 0.7)]:
        comp(img, shade(ell(mask(), p[0], p[1], 0.07, 0.05), (130, 220, 150), (40, 130, 80)))
    comp(img, shade(ell(mask(), 0.5, 0.24, 0.08, 0.07), (130, 220, 150), (40, 130, 80)))
    comp(img, shade(ell(mask(), 0.5, 0.54, 0.24, 0.2), (80, 200, 130), (20, 110, 70)))
    for i in range(6):
        a = i * math.pi / 3
        hexm = poly(mask(), [(0.5 + math.cos(a) * 0.14 + math.cos(b * math.pi / 3) * 0.055, 0.54 + math.sin(a) * 0.11 + math.sin(b * math.pi / 3) * 0.045) for b in range(6)])
        comp(img, shade(hexm, (160, 240, 180), (60, 170, 110), hl=False, olw=2))
    comp(img, shade(poly(mask(), [(0.5 + math.cos(b * math.pi / 3) * 0.06, 0.54 + math.sin(b * math.pi / 3) * 0.05) for b in range(6)]), (255, 225, 130), (190, 130, 40), olw=2))


def d_phoenix_feather(img):
    shadow(img, 0.5, 0.88, 0.2, 0.04)
    vane = poly(mask(), [(0.66, 0.12), (0.78, 0.2), (0.74, 0.4), (0.58, 0.66), (0.42, 0.8), (0.36, 0.74), (0.44, 0.52), (0.56, 0.28)])
    comp(img, shade(vane, (255, 220, 90), (220, 40, 40)))
    for k in range(6):
        t = 0.2 + k * 0.1
        x, y = 0.7 - t * 0.55, 0.12 + t * 0.8
        comp(img, shade(line(mask(), [(x, y), (x + 0.1, y - 0.02)], 0.01), (255, 240, 180), (255, 200, 120), hl=False, ol=False))
    comp(img, shade(line(mask(), [(0.72, 0.14), (0.3, 0.88)], 0.018), (255, 245, 200), (220, 170, 60), olw=2))
    sparkle(img, 0.3, 0.3, 0.04, (255, 220, 150))


def d_moon_rabbit(img):
    chain(img, [(0.14, 0.4), (0.3, 0.6), (0.5, 0.66), (0.7, 0.6), (0.86, 0.4)], (220, 225, 240))
    moon = sub(ell(mask(), 0.5, 0.44, 0.17, 0.17), ell(mask(), 0.58, 0.4, 0.15, 0.15))
    comp(img, shade(moon, (255, 250, 210), (220, 190, 100)))
    body = ell(ell(mask(), 0.55, 0.52, 0.07, 0.055), 0.62, 0.45, 0.04, 0.035)
    ell(body, 0.61, 0.36, 0.014, 0.05)
    ell(body, 0.645, 0.37, 0.014, 0.05)
    comp(img, shade(body, (255, 255, 255), (200, 205, 225), olw=3))
    sparkle(img, 0.3, 0.26, 0.035, (230, 240, 255))


def d_sakura_pin(img):
    shadow(img, 0.5, 0.88, 0.22, 0.04)
    comp(img, shade(line(mask(), [(0.24, 0.84), (0.66, 0.3)], 0.03), (120, 70, 50), (70, 40, 30), olw=3))
    for (cx, cy, r) in [(0.62, 0.32, 0.12), (0.76, 0.46, 0.09), (0.5, 0.2, 0.08)]:
        for i in range(5):
            a = -math.pi / 2 + i * 2 * math.pi / 5
            petal = ell(mask(), cx + math.cos(a) * r * 0.55, cy + math.sin(a) * r * 0.55, r * 0.45, r * 0.45)
            comp(img, shade(petal, (255, 215, 230), (240, 130, 170), hl=False, olw=3))
        comp(img, shade(ell(mask(), cx, cy, r * 0.22, r * 0.22), (255, 230, 120), (220, 150, 60), hl=False, olw=2))
    for k in range(3):
        comp(img, shade(line(mask(), [(0.34 + k * 0.03, 0.72 - k * 0.04), (0.28 + k * 0.03, 0.84)], 0.008), (255, 225, 130), (190, 130, 40), hl=False, olw=1))


def d_oni_mask(img):
    shadow(img, 0.5, 0.88, 0.22, 0.04)
    for s in (-1, 1):
        comp(img, shade(poly(mask(), [(0.5 + s * 0.14, 0.3), (0.5 + s * 0.26, 0.08), (0.5 + s * 0.22, 0.32)]), (255, 245, 220), (200, 180, 150)))
    face = ell(mask(), 0.5, 0.52, 0.26, 0.28)
    comp(img, shade(face, (240, 70, 60), (150, 20, 30)))
    for s in (-1, 1):
        comp(img, shade(poly(mask(), [(0.5 + s * 0.05, 0.44), (0.5 + s * 0.2, 0.36), (0.5 + s * 0.17, 0.48)]), (255, 230, 90), (230, 150, 30), hl=False, olw=3))
        comp(img, shade(poly(mask(), [(0.5 + s * 0.03, 0.4), (0.5 + s * 0.2, 0.3), (0.5 + s * 0.21, 0.34)]), (40, 20, 20), (20, 10, 10), hl=False, ol=False))
    comp(img, shade(ImageDraw_rrect(0.36, 0.62, 0.64, 0.72, 0.03), (60, 20, 20), (30, 10, 10), hl=False))
    for s in (-1, 1):
        comp(img, shade(poly(mask(), [(0.5 + s * 0.06, 0.62), (0.5 + s * 0.11, 0.62), (0.5 + s * 0.085, 0.54)]), (255, 250, 240), (210, 200, 190), hl=False, olw=2))


def d_star_compass(img):
    shadow(img, 0.5, 0.86, 0.24, 0.05)
    comp(img, shade(ell(mask(), 0.5, 0.52, 0.3, 0.3), (60, 70, 130), (20, 20, 60)))
    metal_ring(img, 0.5, 0.52, 0.3, 0.05)
    for i in range(4):
        a = i * math.pi / 2
        comp(img, shade(poly(mask(), [(0.5 + math.cos(a) * 0.22, 0.52 + math.sin(a) * 0.22), (0.5 + math.cos(a + 0.8) * 0.05, 0.52 + math.sin(a + 0.8) * 0.05), (0.5, 0.52), (0.5 + math.cos(a - 0.8) * 0.05, 0.52 + math.sin(a - 0.8) * 0.05)]), (255, 240, 180) if i == 3 else (200, 210, 255), (200, 150, 60) if i == 3 else (120, 130, 200), hl=False, olw=2))
    gem(img, 0.5, 0.52, 0.035, (120, 200, 255), 6)
    comp(img, shade(ell(mask(), 0.5, 0.2, 0.05, 0.035), (255, 225, 130), (190, 130, 40)))
    for p in [(0.3, 0.38), (0.7, 0.66), (0.66, 0.36)]:
        sparkle(img, p[0], p[1], 0.022, (230, 240, 255))


def d_serpent_ring(img):
    shadow(img, 0.5, 0.84, 0.24, 0.05)
    ring = sub(ell(mask(), 0.5, 0.55, 0.26, 0.24), ell(mask(), 0.5, 0.55, 0.16, 0.14))
    comp(img, shade(ring, (120, 220, 110), (30, 110, 50)))
    for i in range(10):
        a = i * math.pi / 5
        comp(img, shade(ell(mask(), 0.5 + math.cos(a) * 0.21, 0.55 + math.sin(a) * 0.19, 0.022, 0.018), (170, 240, 150), (80, 160, 80), hl=False, ol=False))
    head = ell(mask(), 0.5, 0.3, 0.1, 0.08)
    comp(img, shade(head, (130, 230, 120), (40, 120, 60)))
    gem(img, 0.47, 0.28, 0.022, (255, 40, 60), 4, math.pi / 4, glow=False)
    gem(img, 0.53, 0.28, 0.022, (255, 40, 60), 4, math.pi / 4, glow=False)
    comp(img, shade(line(mask(), [(0.5, 0.37), (0.5, 0.43), (0.47, 0.46)], 0.008), (230, 60, 80), (160, 20, 40), hl=False, ol=False))


def d_thorn_bracelet(img):
    shadow(img, 0.5, 0.84, 0.25, 0.05)
    ring = sub(ell(mask(), 0.5, 0.52, 0.28, 0.24), ell(mask(), 0.5, 0.52, 0.2, 0.17))
    comp(img, shade(ring, (140, 95, 60), (70, 45, 25)))
    for i in range(10):
        a = i * math.pi / 5 + 0.2
        bx, by = 0.5 + math.cos(a) * 0.28, 0.52 + math.sin(a) * 0.24
        comp(img, shade(poly(mask(), [(bx + math.cos(a + 1.5) * 0.02, by + math.sin(a + 1.5) * 0.02), (bx + math.cos(a) * 0.07, by + math.sin(a) * 0.07), (bx + math.cos(a - 1.5) * 0.02, by + math.sin(a - 1.5) * 0.02)]), (200, 180, 150), (120, 90, 60), hl=False, olw=2))
    for a in (0.8, 2.6, 4.4):
        lx, ly = 0.5 + math.cos(a) * 0.24, 0.52 + math.sin(a) * 0.2
        comp(img, shade(ell(mask(), lx, ly, 0.05, 0.03), (120, 210, 90), (40, 120, 40), hl=False, olw=2))
    gem(img, 0.5, 0.28, 0.045, (220, 40, 70), 5)


def d_koi_coin(img):
    shadow(img, 0.5, 0.86, 0.24, 0.05)
    comp(img, shade(ell(mask(), 0.5, 0.52, 0.3, 0.3), (255, 230, 120), (200, 130, 30)))
    comp(img, shade(sub(ell(mask(), 0.5, 0.52, 0.24, 0.24), ell(mask(), 0.5, 0.52, 0.22, 0.22)), (230, 170, 60), (180, 110, 30), hl=False, ol=False))
    fish = ell(mask(), 0.5, 0.6, 0.12, 0.05)
    poly(fish, [(0.36, 0.6), (0.3, 0.55), (0.3, 0.66)])
    comp(img, shade(fish, (255, 140, 60), (220, 70, 30), hl=False, olw=2))
    comp(img, shade(ell(mask(), 0.58, 0.59, 0.012, 0.012), (30, 20, 20), (10, 5, 5), hl=False, ol=False))
    sq = ImageDraw_rrect(0.46, 0.34, 0.54, 0.42, 0.01)
    img.paste(Image.new("RGBA", (W, W), (0, 0, 0, 0)), (0, 0), sq)
    comp(img, shade(sub(ImageDraw_rrect(0.44, 0.32, 0.56, 0.44, 0.01), sq), (200, 140, 40), (150, 100, 20), hl=False, ol=False))
    sparkle(img, 0.32, 0.36, 0.04)


def d_scholar_tassel(img):
    comp(img, shade(line(mask(), [(0.5, 0.1), (0.5, 0.32)], 0.02), (230, 60, 70), (150, 20, 40), olw=3))
    disk = sub(ell(mask(), 0.5, 0.42, 0.13, 0.13), ell(mask(), 0.5, 0.42, 0.04, 0.04))
    comp(img, shade(disk, (150, 240, 180), (40, 140, 90)))
    comp(img, shade(ell(mask(), 0.5, 0.58, 0.05, 0.04), (255, 225, 130), (190, 130, 40)))
    tassel = poly(mask(), [(0.45, 0.6), (0.55, 0.6), (0.62, 0.9), (0.38, 0.9)])
    comp(img, shade(tassel, (240, 70, 70), (150, 20, 40)))
    for k in range(6):
        x = 0.41 + k * 0.036
        comp(img, shade(line(mask(), [(x + 0.01 - k * 0.002, 0.64), (x, 0.88)], 0.006), (255, 140, 140), (200, 60, 70), hl=False, ol=False))


def d_storm_bell(img):
    shadow(img, 0.5, 0.86, 0.2, 0.04)
    comp(img, shade(line(mask(), [(0.5, 0.1), (0.5, 0.26)], 0.03), (120, 150, 255), (50, 60, 170)))
    body = poly(mask(), [(0.32, 0.68), (0.36, 0.4), (0.44, 0.28), (0.56, 0.28), (0.64, 0.4), (0.68, 0.68)])
    ell(body, 0.5, 0.7, 0.2, 0.08)
    comp(img, shade(body, (210, 230, 255), (80, 100, 170)))
    comp(img, shade(poly(mask(), [(0.49, 0.36), (0.57, 0.36), (0.52, 0.48), (0.58, 0.48), (0.45, 0.66), (0.49, 0.52), (0.43, 0.52)]), (255, 245, 110), (240, 190, 30), hl=False, olw=3))
    comp(img, shade(ell(mask(), 0.5, 0.74, 0.05, 0.035), (60, 60, 100), (30, 30, 60), hl=False))
    for p in [(0.24, 0.3), (0.78, 0.32)]:
        sparkle(img, p[0], p[1], 0.03, (200, 220, 255))


def d_shadow_pin(img):
    shadow(img, 0.5, 0.88, 0.2, 0.04)
    for k in range(4):
        m = ell(mask(), 0.36 + k * 0.1, 0.72 - k * 0.12, 0.09, 0.05, 90).filter(ImageFilter.GaussianBlur(SS * 4))
        img.paste(Image.new("RGBA", (W, W), (120, 70, 180, 255)), (0, 0), m)
    blade = poly(mask(), [(0.5, 0.1), (0.57, 0.28), (0.54, 0.58), (0.46, 0.58), (0.43, 0.28)])
    comp(img, shade(blade, (170, 150, 220), (50, 30, 90)))
    comp(img, shade(ImageDraw_rrect(0.36, 0.58, 0.64, 0.63, 0.02), (90, 60, 130), (40, 20, 60)))
    comp(img, shade(ImageDraw_rrect(0.47, 0.63, 0.53, 0.82, 0.02), (60, 40, 80), (30, 15, 45)))
    gem(img, 0.5, 0.86, 0.045, (190, 110, 255), 6)


def d_void_crown(img):
    shadow(img, 0.5, 0.84, 0.28, 0.05)
    crown = poly(mask(), [(0.2, 0.74), (0.2, 0.36), (0.33, 0.52), (0.42, 0.24), (0.5, 0.46), (0.58, 0.24), (0.67, 0.52), (0.8, 0.36), (0.8, 0.74)])
    comp(img, shade(crown, (110, 90, 150), (30, 20, 50)))
    comp(img, shade(ImageDraw_rrect(0.2, 0.68, 0.8, 0.76, 0.02), (255, 225, 130), (190, 130, 40)))
    for x in (0.2, 0.42, 0.58, 0.8):
        comp(img, shade(ell(mask(), x, 0.36 if x in (0.2, 0.8) else 0.24, 0.028, 0.028), (255, 225, 130), (190, 130, 40), hl=False, olw=2))
    g = ell(mask(), 0.5, 0.55, 0.14, 0.14, 180).filter(ImageFilter.GaussianBlur(SS * 8))
    img.paste(Image.new("RGBA", (W, W), (255, 120, 200, 255)), (0, 0), g)
    comp(img, shade(sub(ell(mask(), 0.5, 0.55, 0.1, 0.1), ell(mask(), 0.5, 0.55, 0.075, 0.075)), (255, 240, 220), (255, 150, 200), hl=False, olw=2))
    comp(img, shade(ell(mask(), 0.5, 0.55, 0.075, 0.075), (15, 5, 25), (5, 0, 10), hl=False, ol=False))
    for p in [(0.3, 0.2), (0.72, 0.16), (0.86, 0.6)]:
        sparkle(img, p[0], p[1], 0.03, (255, 200, 240))


# id -> (rarity tier 2..6, painter)
DESIGNS = {
    "acc_charm": (2, d_charm), "acc_bell": (2, d_bell), "acc_sakura_pin": (2, d_sakura_pin), "acc_koi_coin": (2, d_koi_coin), "acc_scholar_tassel": (2, d_scholar_tassel),
    "acc_mask": (3, d_mask), "acc_beads": (3, d_beads), "acc_ribbon": (3, d_ribbon), "acc_ember_brooch": (3, d_ember), "acc_frost_lotus": (3, d_frost_lotus),
    "acc_jade_turtle": (3, d_jade_turtle), "acc_moon_rabbit": (3, d_moon_rabbit), "acc_thorn_bracelet": (3, d_thorn_bracelet),
    "acc_earrings": (4, d_earrings), "acc_fang_pendant": (4, d_fang), "acc_thunder_drum": (4, d_thunder_drum), "acc_oni_mask": (4, d_oni_mask),
    "acc_star_compass": (4, d_star_compass), "acc_serpent_ring": (4, d_serpent_ring), "acc_storm_bell": (4, d_storm_bell), "acc_shadow_pin": (4, d_shadow_pin),
    "acc_suncrest": (5, d_suncrest), "acc_abyss_eye": (5, d_abyss_eye), "acc_phoenix_feather": (5, d_phoenix_feather),
    "acc_phoenixheart": (6, d_phoenixheart), "acc_dragonscale": (6, d_dragonscale), "acc_void_crown": (6, d_void_crown),
}


def render(aid):
    rarity, fn = DESIGNS[aid]
    img = tile(rarity, (hash(aid) % 100) / 100.0)
    fn(img)
    if rarity >= 6:
        rng = random.Random(aid)
        for _ in range(6):
            sparkle(img, rng.uniform(0.12, 0.88), rng.uniform(0.12, 0.88), rng.uniform(0.015, 0.03))
    return img.resize((S, S), Image.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)
    tiles = []
    for aid in DESIGNS:
        im = render(aid)
        im.save(os.path.join(OUT, aid + ".png"))
        tiles.append(im)
    if len(sys.argv) > 1:
        cols = 7
        rows = (len(tiles) + cols - 1) // cols
        sheet = Image.new("RGBA", (cols * (S + 12) + 12, rows * (S + 12) + 12), (18, 18, 28, 255))
        for i, t in enumerate(tiles):
            sheet.alpha_composite(t, (12 + (i % cols) * (S + 12), 12 + (i // cols) * (S + 12)))
        sheet.save(sys.argv[1])
    print("painted", len(tiles), "accessories ->", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
