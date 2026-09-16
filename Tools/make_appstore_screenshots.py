# -*- coding: utf-8 -*-
"""App Store 6.5\" landscape screenshots: 2778x1284, device frame, captions."""
from __future__ import annotations

import os
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

IPHONE_W, IPHONE_H = 2778, 1284
IPAD_W, IPAD_H = 2732, 2048  # App Store iPad 12.9" landscape
ASSETS = Path(
    r"C:\Users\ertug\.cursor\projects\c-Users-ertug-Desktop-Farm2Shelf\assets"
)
OUT = Path(r"C:\Users\ertug\Desktop\Farm2Shelf\AppStoreScreenshots")
FONT_DIR = OUT / "fonts"

# (filename substring, output stem, TR title, TR sub, EN title, EN sub)
SHOTS = [
    (
        "pasaport-92d2e4f7",
        "01_pasaport",
        "Her üründe pasaport",
        "Bugün hasat mı, toptan mı — rafta yazılı",
        "Every item has a passport",
        "Same-day harvest or wholesale — on the shelf",
    ),
    (
        "online_market-3d95ef8c",
        "02_mahalle_kontratlari",
        "Mahalle kontratları",
        "Kasaba siparişini kuryeyle kapıya teslim et",
        "Neighborhood contracts",
        "Town orders, delivered to the door",
    ),
    (
        "g_n_sonu-3b187842",
        "03_z_raporu",
        "Günün hikâyesi",
        "Yerel hasat, kontrat ve kurye — Z raporunda",
        "The story of your day",
        "Harvest, contracts and couriers on the Z-report",
    ),
    (
        "ana_men__2-b6c7ea43",
        "04_marka",
        "Kendi markanı kur",
        "Kimlik, tabela rengi ve slogan senin",
        "Build your brand",
        "Identity, sign color and slogan are yours",
    ),
    (
        "_iftlik-51d548c8",
        "05_hasat",
        "Hasadı izle",
        "Tarla ve ahır kaydı ürüne işlenir",
        "Trace the harvest",
        "Field and barn records travel with the goods",
    ),
    (
        "kurye_teslimat_-9357f6d2",
        "06_kapi_teslim",
        "Kapıya teslim",
        "Kuryen kasaba binalarına ulaşır",
        "Doorstep delivery",
        "Your courier reaches town buildings",
    ),
    (
        "sosyal_medya-f5a312f8",
        "07_sosyal",
        "Kasaba konuşuyor",
        "Markan tweet’te, itibarın puanında",
        "The town is talking",
        "Your brand on the feed, your reputation on the line",
    ),
    (
        "tablet-ede88bbd",
        "08_ekt_tablet",
        "Her şey EKT’te",
        "Çiftlik, atölye, kontrat ve marka tek tablette",
        "Everything in EKT",
        "Farm, workshop, contracts and brand in one tablet",
    ),
    (
        "takvim-83dbdf12",
        "09_mufettis",
        "Müfettiş günü",
        "Mevsimin 30. günü — vitrinin hazır olsun",
        "Inspector day",
        "Season day 30 — keep the shop ready",
    ),
    (
        "market-c7f7736b",
        "10_vitrin",
        "Vitrinin, senin vitrinin",
        "Rafta ürün, kapıda markan",
        "Your shop, your brand",
        "Goods on the shelf, your name on the street",
    ),
    (
        "anamen_-f16664b2",
        "11_ana_menu",
        "Farm2Shelf",
        "Hasat et, işle, teslim et — mahallenin gıda markası",
        "Farm2Shelf",
        "Harvest, craft, deliver — your neighborhood food brand",
    ),
    (
        "cami-3ff05b63",
        "12_kasaba",
        "Teslimat senin kasaban",
        "Binalar senden sipariş verir",
        "The town is your route",
        "Buildings send you the orders",
    ),
    (
        "hal_saha-db5966b9",
        "13_mahalle",
        "Mahalle canlı",
        "Kontratların bu sokaklara gider",
        "A living neighborhood",
        "Your contracts travel these streets",
    ),
    (
        "borsa-d0c7c97a",
        "14_finans",
        "Kasaba ekonomisi",
        "Gelirini, giderini ve yatırımı takip et",
        "Town economy",
        "Track income, costs and investments",
    ),
    (
        "ma_aza_y_netimi-015b525f",
        "15_dukkan",
        "Dükkanını büyüt",
        "Depo, personel ve teslimat kapasitesi",
        "Grow the shop",
        "Storage, staff and delivery capacity",
    ),
]


def find_src(key: str) -> Path:
    matches = list(ASSETS.glob(f"*{key}*"))
    if not matches:
        raise FileNotFoundError(key)
    return matches[0]


def load_font(name: str, size: int) -> ImageFont.FreeTypeFont:
    path = FONT_DIR / name
    if path.exists():
        return ImageFont.truetype(str(path), size)
    for fallback in (
        r"C:\Windows\Fonts\segoeuib.ttf",
        r"C:\Windows\Fonts\segoeui.ttf",
        r"C:\Windows\Fonts\arialbd.ttf",
        r"C:\Windows\Fonts\arial.ttf",
    ):
        if os.path.exists(fallback):
            return ImageFont.truetype(fallback, size)
    return ImageFont.load_default()


def cover_crop(img: Image.Image, tw: int, th: int) -> Image.Image:
    img = img.convert("RGB")
    sw, sh = img.size
    scale = max(tw / sw, th / sh)
    nw, nh = max(1, int(sw * scale)), max(1, int(sh * scale))
    img = img.resize((nw, nh), Image.Resampling.LANCZOS)
    left = (nw - tw) // 2
    top = (nh - th) // 2
    return img.crop((left, top, left + tw, top + th))


def rounded_mask(size: tuple[int, int], radius: int) -> Image.Image:
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius=radius, fill=255)
    return mask


def paste_rounded(base: Image.Image, fg: Image.Image, xy: tuple[int, int], radius: int) -> None:
    mask = rounded_mask(fg.size, radius)
    base.paste(fg, xy, mask)


def text_size(draw: ImageDraw.ImageDraw, text: str, font: ImageFont.FreeTypeFont) -> tuple[int, int]:
    box = draw.textbbox((0, 0), text, font=font)
    return box[2] - box[0], box[3] - box[1]


def fit_font(draw: ImageDraw.ImageDraw, text: str, names: list[str], size: int, max_w: int) -> ImageFont.FreeTypeFont:
    while size >= 28:
        font = load_font(names[0], size)
        tw, _ = text_size(draw, text, font)
        if tw <= max_w:
            return font
        size -= 4
    return load_font(names[0], 28)


def draw_text_shadow(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    font: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int],
    shadow: tuple[int, int, int, int] = (0, 0, 0, 180),
) -> None:
    x, y = xy
    for dx, dy in ((3, 3), (2, 4), (4, 2)):
        draw.text((x + dx, y + dy), text, font=font, fill=shadow)
    draw.text((x, y), text, font=font, fill=fill)


def make_shot(
    src: Path,
    title: str,
    subtitle: str,
    dest: Path,
    footer: str,
    canvas_w: int = IPHONE_W,
    canvas_h: int = IPHONE_H,
    device: str = "iphone",
) -> None:
    shot = Image.open(src).convert("RGB")
    canvas = Image.new("RGB", (canvas_w, canvas_h), (8, 22, 16))
    bg = cover_crop(shot, canvas_w, canvas_h).filter(ImageFilter.GaussianBlur(28))
    bg = Image.eval(bg, lambda p: int(p * 0.28))
    canvas.paste(bg, (0, 0))

    header_h = 300 if device == "iphone" else 360
    overlay = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 0))
    od = ImageDraw.Draw(overlay)
    od.rectangle((0, 0, canvas_w, header_h), fill=(6, 18, 12, 214))
    od.rectangle((0, canvas_h - 64, canvas_w, canvas_h), fill=(6, 18, 12, 150))
    canvas = Image.alpha_composite(canvas.convert("RGBA"), overlay).convert("RGB")

    if device == "ipad":
        body_h = canvas_h - header_h - 48
        body_w = int(body_h * 1.38)
        if body_w > canvas_w - 80:
            body_w = canvas_w - 80
            body_h = int(body_w / 1.38)
        body_x = (canvas_w - body_w) // 2
        body_y = header_h - 28
        bezel = 42
        radius_body = 52
        radius_screen = 36
    else:
        body_w, body_h = 2548, 1024
        body_x = (canvas_w - body_w) // 2
        body_y = 232
        bezel = 38
        radius_body = 78
        radius_screen = 58

    screen_w = body_w - bezel * 2
    screen_h = body_h - bezel * 2

    shadow = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.rounded_rectangle(
        (body_x + 18, body_y + 28, body_x + body_w + 18, body_y + body_h + 28),
        radius=radius_body,
        fill=(0, 0, 0, 160),
    )
    shadow = shadow.filter(ImageFilter.GaussianBlur(22))
    canvas = Image.alpha_composite(canvas.convert("RGBA"), shadow).convert("RGB")

    body = Image.new("RGBA", (body_w, body_h), (0, 0, 0, 0))
    bd = ImageDraw.Draw(body)
    bd.rounded_rectangle((0, 0, body_w - 1, body_h - 1), radius=radius_body, fill=(18, 18, 22, 255))
    bd.rounded_rectangle((3, 3, body_w - 4, body_h - 4), radius=radius_body - 3, fill=(8, 8, 10, 255))
    canvas = canvas.convert("RGBA")
    canvas.paste(body, (body_x, body_y), body)

    screen = cover_crop(shot, screen_w, screen_h).convert("RGBA")
    paste_rounded(canvas, screen, (body_x + bezel, body_y + bezel), radius_screen)

    idraw = ImageDraw.Draw(canvas)
    if device == "ipad":
        cam_r = 11
        cx = body_x + body_w // 2
        cy = body_y + 18
        idraw.ellipse((cx - cam_r, cy - cam_r, cx + cam_r, cy + cam_r), fill=(6, 6, 8, 255))
        bar_w, bar_h = 160, 10
        bx = body_x + (body_w - bar_w) // 2
        by = body_y + body_h - 22
        idraw.rounded_rectangle((bx, by, bx + bar_w, by + bar_h), radius=5, fill=(40, 40, 48, 255))
    else:
        island_w, island_h = 28, 210
        ix = body_x + 14
        iy = body_y + (body_h - island_h) // 2
        idraw.rounded_rectangle((ix, iy, ix + island_w, iy + island_h), radius=12, fill=(6, 6, 8, 255))
        idraw.rounded_rectangle(
            (body_x + 220, body_y - 8, body_x + 340, body_y + 6),
            radius=4,
            fill=(40, 40, 48, 255),
        )
        idraw.rounded_rectangle(
            (body_x + 400, body_y - 8, body_x + 560, body_y + 6),
            radius=4,
            fill=(40, 40, 48, 255),
        )

    draw = ImageDraw.Draw(canvas)
    gold = (232, 197, 71)
    cream = (247, 241, 225)
    mint = (90, 230, 170)

    title_size = 92 if device == "iphone" else 108
    sub_size = 44 if device == "iphone" else 52
    brand_size = 36 if device == "iphone" else 42
    brand_font = load_font("Outfit-SemiBold.ttf", brand_size)
    title_font = fit_font(draw, title, ["Outfit-Bold.ttf"], title_size, canvas_w - 220)
    sub_font = fit_font(draw, subtitle, ["Outfit-Medium.ttf"], sub_size, canvas_w - 240)

    title_y = 88 if device == "iphone" else 96
    draw_text_shadow(draw, (110, 42 if device == "iphone" else 48), "FARM2SHELF", brand_font, mint)
    tw, th = text_size(draw, title, title_font)
    draw_text_shadow(draw, ((canvas_w - tw) // 2, title_y), title, title_font, cream)
    line_y = title_y + th + 18
    lw = min(420, tw)
    draw.rounded_rectangle(
        ((canvas_w - lw) // 2, line_y, (canvas_w + lw) // 2, line_y + 8),
        radius=4,
        fill=gold,
    )
    sw, _ = text_size(draw, subtitle, sub_font)
    draw_text_shadow(draw, ((canvas_w - sw) // 2, line_y + 22), subtitle, sub_font, gold)

    foot = load_font("Outfit-Medium.ttf", 28 if device == "iphone" else 32)
    fw, _ = text_size(draw, footer, foot)
    draw_text_shadow(draw, ((canvas_w - fw) // 2, canvas_h - 52), footer, foot, (180, 200, 188))

    dest.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(dest, "PNG", optimize=True)


def download_fonts() -> None:
    FONT_DIR.mkdir(parents=True, exist_ok=True)
    files = {
        "Outfit-Bold.ttf": "https://github.com/JulietaUla/Montserrat/raw/master/fonts/ttf/Montserrat-Bold.ttf",
        "Outfit-SemiBold.ttf": "https://github.com/JulietaUla/Montserrat/raw/master/fonts/ttf/Montserrat-SemiBold.ttf",
        "Outfit-Medium.ttf": "https://github.com/JulietaUla/Montserrat/raw/master/fonts/ttf/Montserrat-Medium.ttf",
    }
    try:
        import urllib.request

        for name, url in files.items():
            dest = FONT_DIR / name
            if dest.exists() and dest.stat().st_size > 1000:
                continue
            urllib.request.urlretrieve(url, dest)
    except Exception as exc:
        print("Font indirilemedi, sistem fontu kullanılacak:", exc)


def export_set(device: str) -> None:
    if device == "ipad":
        tr_dir = OUT / "iPad_12_9" / "TR_2732x2048"
        en_dir = OUT / "iPad_12_9" / "EN_2732x2048"
        upload_tr = OUT / "iPad_12_9" / "YUKLE_ILK_10" / "TR"
        upload_en = OUT / "iPad_12_9" / "YUKLE_ILK_10" / "EN"
        cw, ch = IPAD_W, IPAD_H
    else:
        tr_dir = OUT / "TR_2778x1284"
        en_dir = OUT / "EN_2778x1284"
        upload_tr = OUT / "YUKLE_ILK_10" / "TR"
        upload_en = OUT / "YUKLE_ILK_10" / "EN"
        cw, ch = IPHONE_W, IPHONE_H

    upload_tr.mkdir(parents=True, exist_ok=True)
    upload_en.mkdir(parents=True, exist_ok=True)

    for key, stem, tr_t, tr_s, en_t, en_s in SHOTS:
        src = find_src(key)
        print("İşleniyor:", device, stem, src.name)
        tr_path = tr_dir / f"{stem}.png"
        en_path = en_dir / f"{stem}.png"
        make_shot(src, tr_t, tr_s, tr_path, "Mahallenin gıda markası", cw, ch, device)
        make_shot(src, en_t, en_s, en_path, "Your neighborhood food brand", cw, ch, device)
        if stem.startswith(("01_", "02_", "03_", "04_", "05_", "06_", "07_", "08_", "09_", "10_")):
            Image.open(tr_path).save(upload_tr / f"{stem}.png")
            Image.open(en_path).save(upload_en / f"{stem}.png")


def main() -> None:
    import sys

    download_fonts()
    OUT.mkdir(parents=True, exist_ok=True)
    only = sys.argv[1] if len(sys.argv) > 1 else "all"
    if only in ("all", "iphone"):
        export_set("iphone")
    if only in ("all", "ipad"):
        export_set("ipad")

    note = OUT / "YUKLEME_SIRASI.txt"
    note.write_text(
        "\n".join(
            [
                "iPhone 6.5\" Display → yatay PNG 2778x1284",
                "  AppStoreScreenshots/YUKLE_ILK_10/TR ve EN",
                "",
                "iPad 12.9\" Display → yatay PNG 2732x2048",
                "  AppStoreScreenshots/iPad_12_9/YUKLE_ILK_10/TR ve EN",
                "",
                "En fazla 10 görsel. İlk 3 kare inceleme için kritik.",
                "Sıra: 1 Pasaport  2 Kontrat  3 Z raporu  4 Marka  5 Hasat",
                "      6 Kapı teslim  7 Sosyal  8 EKT  9 Müfettiş  10 Vitrin",
            ]
        ),
        encoding="utf-8",
    )
    print("Bitti:", OUT)


if __name__ == "__main__":
    main()
