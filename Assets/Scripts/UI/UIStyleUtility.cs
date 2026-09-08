using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// UGUI elemanları için prosedürel olarak pürüzsüz kavisli yuvarlak (Pill / Capsule / Rounded)
    /// Sprite ve doku üreten stil yardımcısı.
    /// Mobilya, Dekorasyon, Toptancı Ürünleri ve Tohum paketleri için kendine has özgün 2D İllüstrasyon İkonları üretir.
    /// </summary>
    public static class UIStyleUtility
    {
        private static readonly Dictionary<FurnitureType, Sprite> furnitureIconCache = new Dictionary<FurnitureType, Sprite>();
        private static readonly Dictionary<LivestockType, Sprite> livestockIconCache = new Dictionary<LivestockType, Sprite>();
        private static readonly Dictionary<string, Sprite> wholesaleIconCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> seedIconCache = new Dictionary<string, Sprite>();
        private static Sprite motorcycleIconCache;
        private static readonly Dictionary<string, Sprite> pillSpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> outlineSpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<int, Font> fontCache = new Dictionary<int, Font>();
        private static Font defaultFallbackFont = null;
        private static Texture2D cachedGridTex = null;

        /// <summary>
        /// Yerleştirme modunda zeminde beliren neon ızgara çizgileri dokusu üretir.
        /// </summary>
        public static Texture2D GetFloorGridTexture()
        {
            if (cachedGridTex != null) return cachedGridTex;

            int size = 128;
            cachedGridTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] cols = new Color[size * size];
            Color gridLineColor = new Color(0.20f, 0.90f, 0.85f, 0.45f); // Neon Cyan Izgara
            Color subLineColor = new Color(0.20f, 0.90f, 0.85f, 0.18f);  // İnce Ara Çizgi
            Color bgColor = new Color(0.05f, 0.12f, 0.18f, 0.08f);       // Çok hafif zemin rengi

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isMainBorder = (x < 2 || x >= size - 2 || y < 2 || y >= size - 2);
                    bool isSubLine = (x == size / 2 || y == size / 2);

                    if (isMainBorder)
                    {
                        cols[y * size + x] = gridLineColor;
                    }
                    else if (isSubLine)
                    {
                        cols[y * size + x] = subLineColor;
                    }
                    else
                    {
                        cols[y * size + x] = bgColor;
                    }
                }
            }

            cachedGridTex.SetPixels(cols);
            cachedGridTex.wrapMode = TextureWrapMode.Repeat;
            cachedGridTex.filterMode = FilterMode.Bilinear;
            cachedGridTex.Apply();
            return cachedGridTex;
        }

        /// <summary>
        /// Bellek sızıntılarını ve iOS çökmesini önleyen statik önbellekli global Font sağlayıcı.
        /// </summary>
        public static Font GetGlobalFont(int fontSize = 20)
        {
            if (fontCache.TryGetValue(fontSize, out Font cached) && cached != null)
            {
                return cached;
            }

            Font font = null;
            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch {}

            if (font == null && defaultFallbackFont != null)
            {
                font = defaultFallbackFont;
            }

            if (font == null)
            {
                try { font = Font.CreateDynamicFontFromOSFont("Arial", fontSize); } catch {}
                if (font == null) { try { font = Font.CreateDynamicFontFromOSFont("Helvetica", fontSize); } catch {} }
                if (font == null) { try { font = Font.CreateDynamicFontFromOSFont("Segoe UI", fontSize); } catch {} }
                if (font != null && defaultFallbackFont == null)
                {
                    defaultFallbackFont = font;
                }
            }

            if (font != null)
            {
                fontCache[fontSize] = font;
            }

            return font;
        }

        public static Sprite CreateRoundedPillSprite(int width, int height, int cornerRadius, Color color)
        {
            string cacheKey = $"{width}_{height}_{cornerRadius}_{color.r:F3}_{color.g:F3}_{color.b:F3}_{color.a:F3}";
            if (pillSpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            float r = cornerRadius;
            float w = width;
            float h = height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0f;
                    float dy = 0f;

                    if (x < r) dx = r - x;
                    else if (x > w - r) dx = x - (w - r);

                    if (y < r) dy = r - y;
                    else if (y > h - r) dy = y - (h - r);

                    float distSq = dx * dx + dy * dy;
                    float radiusSq = r * r;

                    if (distSq > radiusSq)
                    {
                        pixels[y * width + x] = Color.clear;
                    }
                    else
                    {
                        float dist = Mathf.Sqrt(distSq);
                        float alpha = Mathf.Clamp01((r - dist) + 0.5f);
                        Color pColor = color;
                        pColor.a *= alpha;
                        pixels[y * width + x] = pColor;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            pillSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        public static Sprite CreateOutlinePillSprite(int width, int height, int cornerRadius, int borderWidth, Color strokeColor, Color fillColor)
        {
            string cacheKey = $"{width}_{height}_{cornerRadius}_{borderWidth}_{strokeColor.r:F3}_{strokeColor.g:F3}_{strokeColor.b:F3}_{strokeColor.a:F3}_{fillColor.r:F3}_{fillColor.g:F3}_{fillColor.b:F3}_{fillColor.a:F3}";
            if (outlineSpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            float r = cornerRadius;
            float w = width;
            float h = height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0f;
                    float dy = 0f;

                    if (x < r) dx = r - x;
                    else if (x > w - r) dx = x - (w - r);

                    if (y < r) dy = r - y;
                    else if (y > h - r) dy = y - (h - r);

                    float distSq = dx * dx + dy * dy;
                    float radiusSq = r * r;

                    if (distSq > radiusSq)
                    {
                        pixels[y * width + x] = Color.clear;
                    }
                    else
                    {
                        bool isBorder = (x < borderWidth || x >= w - borderWidth || y < borderWidth || y >= h - borderWidth);
                        if (x < r || x > w - r || y < r || y > h - r)
                        {
                            float dist = Mathf.Sqrt(distSq);
                            if (dist > r - borderWidth) isBorder = true;
                        }

                        Color pixelColor = isBorder ? strokeColor : fillColor;
                        pixels[y * width + x] = pixelColor;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            outlineSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static readonly Dictionary<string, Sprite> arrowSpriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Nizami, yüksek kaliteli, anti-aliased vektörel yön oku (D-Pad) Sprite'ı üretir.
        /// </summary>
        public static Sprite CreateArrowSprite(string direction, int size = 64, Color? color = null)
        {
            Color col = color ?? Color.white;
            string cacheKey = $"{direction}_{size}_{col.r:F2}_{col.g:F2}_{col.b:F2}_{col.a:F2}";
            if (arrowSpriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            float center = (size - 1) / 2f;
            float half = size * 0.42f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / half;
                    float ny = (y - center) / half;

                    // Koordinatları UP yönüne göre hizala
                    float rx = nx;
                    float ry = ny;
                    if (direction == "DOWN") { rx = nx; ry = -ny; }
                    else if (direction == "LEFT") { rx = ny; ry = -nx; }
                    else if (direction == "RIGHT") { rx = -ny; ry = nx; }

                    // Ok geometrisi:
                    // Üçgen Tepe Noktası: ry = 0.88
                    // Üçgen Tabanı: ry = 0.0, Genişlik rx [-0.80, 0.80]
                    // Gövde (Stem): ry = [-0.80, 0.0], Genişlik rx [-0.26, 0.26]
                    bool insideHead = (ry >= -0.05f && ry <= 0.88f && Mathf.Abs(rx) <= (1f - (ry + 0.05f) / 0.93f) * 0.80f);
                    bool insideStem = (ry >= -0.80f && ry <= 0.05f && Mathf.Abs(rx) <= 0.26f);

                    if (insideHead || insideStem)
                    {
                        pixels[y * size + x] = col;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            arrowSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>
        /// Mobilya ve Dekorasyon ögelerine özel kendine has detaylı 2D Vektör çizim illüstrasyonu üretici.
        /// </summary>
        public static Sprite CreateFurnitureIconSprite(FurnitureType type)
        {
            if (furnitureIconCache.TryGetValue(type, out Sprite cached) && cached != null)
            {
                return cached;
            }

            int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[sz * sz];

            Color bgCol = new Color(0.18f, 0.22f, 0.30f);
            Color frameCol = new Color(0.80f, 0.85f, 0.90f);
            Color accentCol = Color.yellow;

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - 31.5f) - 24f);
                    float dy = Mathf.Max(0, Mathf.Abs(y - 31.5f) - 24f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 8f)
                    {
                        pixels[y * sz + x] = Color.clear;
                        continue;
                    }

                    Color pCol = bgCol;

                    switch (type)
                    {
                        case FurnitureType.Shelf:
                            bgCol = new Color(0.18f, 0.22f, 0.32f);
                            accentCol = new Color(0.78f, 0.52f, 0.28f);
                            frameCol = new Color(0.85f, 0.88f, 0.92f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y == 16 || y == 32 || y == 48) pCol = accentCol;
                            else if (x == 10 || x == 53) pCol = frameCol;
                            else if ((y >= 18 && y <= 24 && x >= 14 && x <= 22) || (y >= 34 && y <= 40 && x >= 36 && x <= 46)) pCol = new Color(0.95f, 0.40f, 0.30f);
                            else if (y >= 18 && y <= 26 && x >= 26 && x <= 32) pCol = new Color(0.30f, 0.75f, 0.40f);
                            break;

                        case FurnitureType.StorageShelf:
                            bgCol = new Color(0.24f, 0.18f, 0.16f);
                            accentCol = new Color(0.95f, 0.45f, 0.05f);
                            frameCol = new Color(0.65f, 0.45f, 0.25f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (x <= 12 || x >= 51 || y <= 10 || y == 32 || y >= 54) pCol = accentCol;
                            else if (y == 14 || y == 36) pCol = frameCol;
                            else if (y >= 15 && y <= 28 && x >= 16 && x <= 47) pCol = new Color(0.82f, 0.65f, 0.42f);
                            else if (y >= 37 && y <= 50 && x >= 20 && x <= 43) pCol = new Color(0.75f, 0.58f, 0.35f);
                            break;

                        case FurnitureType.Fridge:
                        case FurnitureType.OrganicFridge:
                            bgCol = new Color(0.12f, 0.28f, 0.42f);
                            accentCol = new Color(0.40f, 0.85f, 0.98f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (x == 8 || x == 55 || y == 8 || y == 55) pCol = accentCol;
                            else if (x == 50 && y >= 20 && y <= 44) pCol = Color.white;
                            else if (y == 22 || y == 38) pCol = new Color(0.60f, 0.90f, 1.0f);
                            else if (y >= 24 && y <= 34 && (x == 16 || x == 24 || x == 32 || x == 40)) pCol = (x == 24) ? Color.yellow : Color.red;
                            break;

                        case FurnitureType.Freezer:
                            bgCol = new Color(0.10f, 0.32f, 0.48f);
                            accentCol = new Color(0.70f, 0.92f, 1.0f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y <= 38 && x >= 10 && x <= 53) pCol = new Color(0.85f, 0.90f, 0.95f);
                            else if (y >= 40 && y <= 48 && x >= 10 && x <= 53) pCol = accentCol;
                            else if ((x == 31 || y == 24) && Mathf.Abs(x - 31) <= 8 && Mathf.Abs(y - 24) <= 8) pCol = Color.cyan;
                            break;

                        case FurnitureType.BakeryCounter:
                            bgCol = new Color(0.35f, 0.22f, 0.12f);
                            accentCol = new Color(0.95f, 0.72f, 0.25f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y <= 24 && x >= 8 && x <= 55) pCol = new Color(0.55f, 0.35f, 0.18f);
                            else if (y >= 26 && x >= 12 && x <= 51) pCol = new Color(0.95f, 0.85f, 0.55f);
                            else if (y >= 14 && y <= 20 && (x >= 16 && x <= 26 || x >= 34 && x <= 44)) pCol = accentCol;
                            break;

                        case FurnitureType.ProduceShelf:
                            bgCol = new Color(0.15f, 0.32f, 0.18f);
                            accentCol = new Color(0.35f, 0.88f, 0.45f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y <= 20 && x >= 8 && x <= 55) pCol = new Color(0.48f, 0.32f, 0.18f);
                            else if (y >= 22 && y <= 44 && x >= 12 && x <= 51) pCol = ((x + y) % 6 < 3) ? new Color(0.95f, 0.30f, 0.25f) : new Color(0.98f, 0.82f, 0.20f);
                            break;

                        case FurnitureType.ButcherCounter:
                            bgCol = new Color(0.38f, 0.14f, 0.18f);
                            accentCol = new Color(0.95f, 0.30f, 0.35f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y <= 26 && x >= 8 && x <= 55) pCol = new Color(0.28f, 0.10f, 0.12f);
                            else if (y >= 28 && y <= 32 && x >= 6 && x <= 57) pCol = new Color(0.85f, 0.88f, 0.92f);
                            else if (y >= 34 && y <= 44 && x >= 16 && x <= 48) pCol = accentCol;
                            break;

                        case FurnitureType.CosmeticShelf:
                            bgCol = new Color(0.32f, 0.12f, 0.28f);
                            accentCol = new Color(0.95f, 0.45f, 0.80f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (x == 10 || x == 53 || y == 16 || y == 34 || y == 50) pCol = accentCol;
                            else if (y >= 18 && y <= 30 && x >= 16 && x <= 22) pCol = new Color(0.85f, 0.20f, 0.50f);
                            else if (y >= 18 && y <= 30 && x >= 32 && x <= 42) pCol = new Color(0.98f, 0.75f, 0.90f);
                            break;

                        case FurnitureType.ElectronicsShelf:
                            bgCol = new Color(0.10f, 0.18f, 0.35f);
                            accentCol = new Color(0.10f, 0.85f, 0.98f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (x <= 10 || x >= 53 || y <= 10 || y >= 53) pCol = accentCol;
                            else if (y >= 20 && y <= 44 && x >= 16 && x <= 47) pCol = new Color(0.05f, 0.10f, 0.20f);
                            else if (y >= 24 && y <= 40 && x >= 20 && x <= 43) pCol = new Color(0.20f, 0.60f, 0.95f);
                            break;

                        case FurnitureType.Cashier:
                            bgCol = new Color(0.14f, 0.22f, 0.35f);
                            accentCol = new Color(0.95f, 0.80f, 0.20f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y <= 24 && x >= 8 && x <= 55) pCol = new Color(0.20f, 0.25f, 0.32f);
                            else if (y >= 12 && y <= 20 && x >= 12 && x <= 36) pCol = Color.black;
                            else if (y >= 26 && y <= 46 && x >= 34 && x <= 50) pCol = accentCol;
                            else if (y >= 38 && y <= 44 && x >= 36 && x <= 48) pCol = Color.cyan;
                            break;

                        case FurnitureType.CustomerServiceDesk:
                            bgCol = new Color(0.12f, 0.32f, 0.38f);
                            accentCol = new Color(0.30f, 0.88f, 0.95f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y <= 22 && x >= 8 && x <= 55) pCol = new Color(0.22f, 0.28f, 0.35f);
                            else if (y >= 24 && y <= 44 && x >= 18 && x <= 38) pCol = new Color(0.10f, 0.15f, 0.22f);
                            else if (y >= 28 && y <= 40 && x >= 22 && x <= 34) pCol = accentCol;
                            break;

                        case FurnitureType.ShoppingCart:
                            bgCol = new Color(0.35f, 0.18f, 0.22f);
                            accentCol = new Color(0.95f, 0.35f, 0.40f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y >= 20 && y <= 48 && x >= 16 && x <= 48) pCol = ((x + y) % 4 == 0) ? Color.white : accentCol;
                            else if ((y >= 10 && y <= 16) && (x == 18 || x == 46)) pCol = Color.gray;
                            break;

                        case FurnitureType.WelcomeMat:
                            bgCol = new Color(0.42f, 0.28f, 0.16f);
                            accentCol = new Color(0.95f, 0.85f, 0.60f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y >= 20 && y <= 44 && x >= 10 && x <= 53) pCol = accentCol;
                            break;

                        case FurnitureType.RedCarpet:
                            bgCol = new Color(0.55f, 0.12f, 0.18f);
                            accentCol = new Color(0.95f, 0.80f, 0.25f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y >= 12 && y <= 52 && x >= 14 && x <= 49) pCol = (x < 18 || x > 45) ? accentCol : new Color(0.85f, 0.15f, 0.22f);
                            break;

                        case FurnitureType.PlantPot:
                        case FurnitureType.PottedPalm:
                        case FurnitureType.BonsaiTree:
                            bgCol = new Color(0.15f, 0.32f, 0.18f);
                            accentCol = new Color(0.30f, 0.88f, 0.40f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y <= 24 && x >= 20 && x <= 43) pCol = new Color(0.78f, 0.42f, 0.22f);
                            else if (y >= 25 && y <= 54 && Mathf.Abs(x - 31.5f) <= (54 - y) * 0.7f) pCol = accentCol;
                            break;

                        case FurnitureType.NeonSign:
                            bgCol = new Color(0.08f, 0.08f, 0.12f);
                            accentCol = new Color(0.95f, 0.25f, 0.75f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y >= 20 && y <= 44 && x >= 12 && x <= 51) pCol = (y >= 30 && y <= 34) ? Color.cyan : accentCol;
                            break;

                        default:
                            bgCol = new Color(0.20f, 0.25f, 0.35f);
                            accentCol = new Color(0.95f, 0.75f, 0.25f);
                            if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59) pCol = accentCol;
                            else if (y == 20 || y == 40 || x == 12 || x == 51) pCol = accentCol;
                            break;
                    }

                    pixels[y * sz + x] = pCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            furnitureIconCache[type] = sprite;
            return sprite;
        }

        /// <summary>
        /// Toptancı 50'li Koli ürünlerine özel 3D İzometrik Ambalaj Koli illüstrasyon görseli üretici.
        /// </summary>
        public static Sprite CreateWholesaleIconSprite(string productId, string iconEmoji, Color categoryColor)
        {
            if (wholesaleIconCache.TryGetValue(productId, out Sprite cached) && cached != null)
            {
                return cached;
            }

            int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[sz * sz];

            Color boxBrown = new Color(0.80f, 0.60f, 0.38f);
            Color boxSide = new Color(0.65f, 0.48f, 0.28f);
            Color tapeColor = new Color(0.95f, 0.85f, 0.45f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - 31.5f) - 24f);
                    float dy = Mathf.Max(0, Mathf.Abs(y - 31.5f) - 24f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 8f)
                    {
                        pixels[y * sz + x] = Color.clear;
                        continue;
                    }

                    Color pCol = new Color(0.16f, 0.18f, 0.24f);

                    if (y >= 12 && y <= 52 && x >= 12 && x <= 52)
                    {
                        if (dist > 6f || x == 12 || x == 52 || y == 12 || y == 52)
                        {
                            pCol = categoryColor;
                        }
                        else if (y >= 44 && y <= 50)
                        {
                            pCol = categoryColor;
                        }
                        else if (y >= 26 && y <= 30)
                        {
                            pCol = tapeColor;
                        }
                        else if (x < 32)
                        {
                            pCol = boxBrown;
                        }
                        else
                        {
                            pCol = boxSide;
                        }
                    }

                    pixels[y * sz + x] = pCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            wholesaleIconCache[productId] = sprite;
            return sprite;
        }

        /// <summary>
        /// Tohum Paketlerine özel renkli tohum zarfı illüstrasyon görseli üretici.
        /// </summary>
        public static Sprite CreateSeedIconSprite(string seedId, string iconEmoji, Color cropColor)
        {
            if (seedIconCache.TryGetValue(seedId, out Sprite cached) && cached != null)
            {
                return cached;
            }

            int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[sz * sz];

            Color paperPaper = new Color(0.92f, 0.88f, 0.78f);
            Color foldColor = new Color(0.82f, 0.76f, 0.65f);

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - 31.5f) - 24f);
                    float dy = Mathf.Max(0, Mathf.Abs(y - 31.5f) - 24f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 8f)
                    {
                        pixels[y * sz + x] = Color.clear;
                        continue;
                    }

                    Color pCol = new Color(0.14f, 0.16f, 0.22f);

                    if (y >= 10 && y <= 54 && x >= 14 && x <= 50)
                    {
                        if (dist > 6f || x == 14 || x == 50 || y == 10 || y == 54)
                        {
                            pCol = cropColor;
                        }
                        else if (y >= 42)
                        {
                            pCol = cropColor;
                        }
                        else if (y >= 36 && y <= 41)
                        {
                            pCol = foldColor;
                        }
                        else if (Mathf.Abs(x - 32) + Mathf.Abs(y - 24) <= 10)
                        {
                            pCol = cropColor;
                        }
                        else
                        {
                            pCol = paperPaper;
                        }
                    }

                    pixels[y * sz + x] = pCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            seedIconCache[seedId] = sprite;
            return sprite;
        }

        private static Sprite diceIconCache = null;

        /// <summary>
        /// Prosedürel olarak çizilmiş, pürüzsüz kenarlı ve 5 noktalı yüksek kaliteli beyaz zar (Dice) ikonu üretir.
        /// </summary>
        public static Sprite CreateDiceIconSprite(int sz = 64)
        {
            if (diceIconCache != null) return diceIconCache;

            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[sz * sz];

            float cx = (sz - 1) * 0.5f;
            float cy = (sz - 1) * 0.5f;
            float halfExtent = sz * 0.38f;
            float cornerRadius = sz * 0.14f;
            float innerExtent = halfExtent - cornerRadius;

            Color dieFillColor = new Color(0.98f, 0.98f, 1.0f, 1.0f);
            Color dieBorderColor = new Color(0.72f, 0.76f, 0.84f, 1.0f);
            Color dieShadowColor = new Color(0.86f, 0.89f, 0.94f, 1.0f);
            Color dotColor = new Color(0.12f, 0.15f, 0.22f, 1.0f);

            float dotRadius = sz * 0.068f;
            float offset = sz * 0.19f;
            Vector2[] dots = new Vector2[]
            {
                new Vector2(cx, cy),
                new Vector2(cx - offset, cy + offset),
                new Vector2(cx + offset, cy + offset),
                new Vector2(cx - offset, cy - offset),
                new Vector2(cx + offset, cy - offset)
            };

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x - cx) - innerExtent);
                    float dy = Mathf.Max(0f, Mathf.Abs(y - cy) - innerExtent);
                    float distCorner = Mathf.Sqrt(dx * dx + dy * dy);

                    if (distCorner > cornerRadius + 0.6f)
                    {
                        pixels[y * sz + x] = Color.clear;
                        continue;
                    }

                    float edgeAlpha = Mathf.Clamp01((cornerRadius + 0.6f) - distCorner);

                    Color curColor;
                    if (distCorner > cornerRadius - 1.6f)
                    {
                        curColor = dieBorderColor;
                    }
                    else if (y < cy - innerExtent * 0.4f)
                    {
                        curColor = dieShadowColor;
                    }
                    else
                    {
                        curColor = dieFillColor;
                    }

                    for (int d = 0; d < dots.Length; d++)
                    {
                        float ddx = x - dots[d].x;
                        float ddy = y - dots[d].y;
                        float distDot = Mathf.Sqrt(ddx * ddx + ddy * ddy);

                        if (distDot <= dotRadius + 0.6f)
                        {
                            float dotAlpha = Mathf.Clamp01((dotRadius + 0.6f) - distDot);
                            curColor = Color.Lerp(curColor, dotColor, dotAlpha);
                        }
                    }

                    curColor.a *= edgeAlpha;
                    pixels[y * sz + x] = curColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            diceIconCache = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            return diceIconCache;
        }

        public static Sprite CreateLivestockIconSprite(LivestockType type)
        {
            if (livestockIconCache.TryGetValue(type, out Sprite cached) && cached != null)
            {
                return cached;
            }

            int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[sz * sz];

            bool chicken = type == LivestockType.WhiteChicken || type == LivestockType.BlackChicken;
            bool blackChicken = type == LivestockType.BlackChicken;
            bool holstein = type == LivestockType.HolsteinCow;

            Color bg = chicken
                ? (blackChicken ? new Color(0.16f, 0.18f, 0.14f) : new Color(0.22f, 0.28f, 0.16f))
                : (holstein ? new Color(0.18f, 0.20f, 0.22f) : new Color(0.28f, 0.18f, 0.10f));
            Color frame = chicken
                ? new Color(0.95f, 0.72f, 0.22f)
                : (holstein ? new Color(0.92f, 0.92f, 0.90f) : new Color(0.78f, 0.48f, 0.22f));

            Color feather = blackChicken ? new Color(0.12f, 0.11f, 0.13f) : new Color(0.96f, 0.94f, 0.88f);
            Color comb = new Color(0.88f, 0.14f, 0.16f);
            Color beak = new Color(0.95f, 0.62f, 0.12f);
            Color cowBase = holstein ? new Color(0.96f, 0.95f, 0.92f) : new Color(0.62f, 0.38f, 0.18f);
            Color cowPatch = holstein ? new Color(0.10f, 0.10f, 0.12f) : new Color(0.42f, 0.24f, 0.10f);
            Color muzzle = holstein ? new Color(0.98f, 0.78f, 0.78f) : new Color(0.90f, 0.72f, 0.55f);

            float cx = 31.5f, cy = 31.5f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - cx) - 24f);
                    float dy = Mathf.Max(0, Mathf.Abs(y - cy) - 24f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > 8f)
                    {
                        pixels[y * sz + x] = Color.clear;
                        continue;
                    }

                    Color pCol = bg;
                    if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59)
                    {
                        pCol = frame;
                    }
                    else if (chicken)
                    {
                        float hx = x - 32f;
                        float hy = y - 30f;
                        float head = hx * hx / 220f + hy * hy / 240f;
                        bool inHead = head <= 1f;
                        bool inComb = y >= 46 && y <= 58 && Mathf.Abs(x - 32) <= (y >= 52 ? 5 : 8) && (x == 32 || Mathf.Abs(x - 32) == 5 || (y >= 48 && y <= 54 && Mathf.Abs(x - 32) <= 3));
                        bool inBeak = y >= 26 && y <= 34 && x >= 28 && x <= 42 && Mathf.Abs(y - 30) <= (42 - x) * 0.45f + 1f;
                        bool inEyeL = (x - 24) * (x - 24) + (y - 36) * (y - 36) <= 10;
                        bool inEyeR = (x - 40) * (x - 40) + (y - 36) * (y - 36) <= 10;
                        bool inWattle = y >= 22 && y <= 28 && Mathf.Abs(x - 32) <= 4;

                        if (inComb) pCol = comb;
                        else if (inBeak) pCol = beak;
                        else if (inEyeL || inEyeR) pCol = Color.black;
                        else if (inWattle) pCol = comb;
                        else if (inHead) pCol = feather;
                    }
                    else
                    {
                        float hx = x - 32f;
                        float hy = y - 32f;
                        bool inHead = (hx * hx / 280f + hy * hy / 300f) <= 1f;
                        bool inEarL = (x - 14) * (x - 14) / 36f + (y - 46) * (y - 46) / 64f <= 1f;
                        bool inEarR = (x - 50) * (x - 50) / 36f + (y - 46) * (y - 46) / 64f <= 1f;
                        bool inMuzzle = (x - 32) * (x - 32) / 90f + (y - 22) * (y - 22) / 48f <= 1f && y <= 30;
                        bool inEyeL = (x - 23) * (x - 23) + (y - 36) * (y - 36) <= 12;
                        bool inEyeR = (x - 41) * (x - 41) + (y - 36) * (y - 36) <= 12;
                        bool inNostrilL = (x - 28) * (x - 28) + (y - 20) * (y - 20) <= 4;
                        bool inNostrilR = (x - 36) * (x - 36) + (y - 20) * (y - 20) <= 4;
                        bool patch = holstein
                            ? ((x < 22 && y > 34) || (x > 44 && y > 30 && y < 50) || (Mathf.Abs(x - 32) < 6 && y > 40))
                            : (y > 40 && Mathf.Abs(x - 32) > 10);

                        if (inEarL || inEarR) pCol = cowPatch;
                        else if (inEyeL || inEyeR) pCol = Color.black;
                        else if (inNostrilL || inNostrilR) pCol = new Color(0.35f, 0.18f, 0.18f);
                        else if (inMuzzle) pCol = muzzle;
                        else if (inHead) pCol = patch ? cowPatch : cowBase;
                    }

                    pixels[y * sz + x] = pCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            livestockIconCache[type] = sprite;
            return sprite;
        }

        public static Sprite CreateMotorcycleIconSprite()
        {
            if (motorcycleIconCache != null) return motorcycleIconCache;

            int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[sz * sz];

            Color bg = new Color(0.10f, 0.16f, 0.24f);
            Color frame = new Color(0.20f, 0.78f, 0.95f);
            Color body = new Color(0.95f, 0.78f, 0.12f);
            Color dark = new Color(0.12f, 0.12f, 0.14f);
            Color tire = new Color(0.18f, 0.18f, 0.20f);
            Color rim = new Color(0.75f, 0.78f, 0.82f);
            Color box = new Color(0.18f, 0.55f, 0.28f);
            Color light = new Color(1.0f, 0.92f, 0.45f);

            float cx = 31.5f, cy = 31.5f;
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - cx) - 24f);
                    float dy = Mathf.Max(0, Mathf.Abs(y - cy) - 24f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > 8f)
                    {
                        pixels[y * sz + x] = Color.clear;
                        continue;
                    }

                    Color pCol = bg;
                    if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59)
                    {
                        pCol = frame;
                    }
                    else
                    {
                        float dRear = (x - 18) * (x - 18) + (y - 18) * (y - 18);
                        float dFront = (x - 46) * (x - 46) + (y - 18) * (y - 18);
                        bool rearTire = dRear <= 100;
                        bool frontTire = dFront <= 90;
                        bool rearRim = dRear <= 36;
                        bool frontRim = dFront <= 30;
                        bool chassis = y >= 20 && y <= 28 && x >= 16 && x <= 48;
                        bool tank = y >= 28 && y <= 38 && x >= 22 && x <= 40;
                        bool seat = y >= 34 && y <= 40 && x >= 20 && x <= 30;
                        bool cargo = y >= 30 && y <= 50 && x >= 10 && x <= 22;
                        bool handle = y >= 38 && y <= 48 && x >= 40 && x <= 50 && Mathf.Abs((y - 43) - (x - 45) * 0.4f) <= 3;
                        bool lamp = (x - 50) * (x - 50) + (y - 30) * (y - 30) <= 16;

                        if (rearRim || frontRim) pCol = rim;
                        else if (rearTire || frontTire) pCol = tire;
                        else if (lamp) pCol = light;
                        else if (cargo) pCol = box;
                        else if (handle) pCol = dark;
                        else if (seat) pCol = dark;
                        else if (tank) pCol = body;
                        else if (chassis) pCol = dark;
                    }

                    pixels[y * sz + x] = pCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            motorcycleIconCache = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            return motorcycleIconCache;
        }

        private static Sprite closeXSpriteCache;

        /// <summary>
        /// Mobilde Unicode çarpı (✖/✕) fontta yok sayılır. Bu sprite her cihazda görünür.
        /// </summary>
        public static Sprite GetCloseXSprite()
        {
            if (closeXSpriteCache != null) return closeXSpriteCache;

            const int s = 64;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[s * s];
            float thickness = 5.5f;
            float pad = 14f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float nx = x + 0.5f;
                    float ny = y + 0.5f;
                    float d1 = DistanceToSegment(nx, ny, pad, pad, s - pad, s - pad);
                    float d2 = DistanceToSegment(nx, ny, pad, s - pad, s - pad, pad);
                    px[y * s + x] = (d1 <= thickness || d2 <= thickness) ? Color.white : new Color(0f, 0f, 0f, 0f);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            closeXSpriteCache = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            closeXSpriteCache.name = "UI_CloseX_Mark";
            return closeXSpriteCache;
        }

        private static float DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float lenSq = dx * dx + dy * dy;
            float t = (lenSq > 0.0001f) ? Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / lenSq) : 0f;
            float qx = ax + t * dx;
            float qy = ay + t * dy;
            return Vector2.Distance(new Vector2(px, py), new Vector2(qx, qy));
        }

        public static void BindCloseMark(Text label)
        {
            if (label == null) return;
            label.enabled = false;
            label.text = "X";
            ApplyCloseMarkGraphic(label.transform.parent);
        }

        public static void ApplyCloseMarkGraphic(Transform closeButtonRoot)
        {
            if (closeButtonRoot == null) return;
            if (closeButtonRoot.Find("CloseMarkGraphic") != null) return;

            GameObject mark = new GameObject("CloseMarkGraphic");
            mark.transform.SetParent(closeButtonRoot, false);
            RectTransform rt = mark.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.22f, 0.22f);
            rt.anchorMax = new Vector2(0.78f, 0.78f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            Image img = mark.AddComponent<Image>();
            img.sprite = GetCloseXSprite();
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;
        }

        public static GameObject CreateCornerCloseButton(Transform parent, UnityAction onClose, float size = 52f)
        {
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(parent, false);
            RectTransform clRect = closeObj.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(1f, 1f);
            clRect.anchorMax = new Vector2(1f, 1f);
            clRect.pivot = new Vector2(1f, 1f);
            clRect.anchoredPosition = new Vector2(-12f, -12f);
            clRect.sizeDelta = new Vector2(size, size);

            Image clBg = closeObj.AddComponent<Image>();
            int d = Mathf.RoundToInt(size);
            clBg.sprite = CreateRoundedPillSprite(d, d, d / 2, new Color(0.92f, 0.18f, 0.20f, 1f));
            clBg.raycastTarget = true;

            Button clBtn = closeObj.AddComponent<Button>();
            clBtn.targetGraphic = clBg;
            if (onClose != null) clBtn.onClick.AddListener(onClose);

            ApplyCloseMarkGraphic(closeObj.transform);
            closeObj.transform.SetAsLastSibling();
            return closeObj;
        }
    }
}
