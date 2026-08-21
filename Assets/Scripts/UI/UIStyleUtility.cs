using System.Collections.Generic;
using UnityEngine;
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
        private static readonly Dictionary<string, Sprite> wholesaleIconCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> seedIconCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> pillSpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> outlineSpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<int, Font> fontCache = new Dictionary<int, Font>();
        private static Font defaultFallbackFont = null;

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
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
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
                        bool isBorder = (x < borderWidth || x > w - borderWidth || y < borderWidth || y > h - borderWidth);
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
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            outlineSpriteCache[cacheKey] = sprite;
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
    }
}
