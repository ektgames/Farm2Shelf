using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// UGUI elemanları için prosedürel olarak pürüzsüz kavisli yuvarlak (Pill / Capsule / Rounded)
    /// Sprite ve doku üreten stil yardımcısı.
    /// Ayrıca hem Mobil hem PC build'larında %100 sorunsuz görünen 2D Vektör Mobilya İkonları üretir.
    /// </summary>
    public static class UIStyleUtility
    {
        private static readonly Dictionary<FurnitureType, Sprite> iconCache = new Dictionary<FurnitureType, Sprite>();

        public static Sprite CreateRoundedPillSprite(int width, int height, int cornerRadius, Color color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];

            float r = cornerRadius;
            float w = width;
            float h = height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Köşe kavis hesabı
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
                        pixels[y * width + x] = Color.clear; // Kavis dışı şeffaf
                    }
                    else
                    {
                        // Kenar yumuşatma (Anti-Aliasing)
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

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        }

        public static Sprite CreateOutlinePillSprite(int width, int height, int cornerRadius, int borderWidth, Color strokeColor, Color fillColor)
        {
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
                        // Kenar konturu ve dolgu
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

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Mobilya türüne özel hem PC Standalone hem Mobil APK/iOS buildlarında %100 çalışan 2D Prosedürel İkon Üretici.
        /// </summary>
        public static Sprite CreateFurnitureIconSprite(FurnitureType type)
        {
            if (iconCache.TryGetValue(type, out Sprite cached) && cached != null)
            {
                return cached;
            }

            int sz = 64;
            Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[sz * sz];

            // Arka Plan Rengi
            Color bgCol = new Color(0.18f, 0.22f, 0.30f);
            Color frameCol = new Color(0.80f, 0.85f, 0.90f);
            Color accentCol = Color.yellow;

            switch (type)
            {
                case FurnitureType.Shelf:
                    bgCol = new Color(0.20f, 0.25f, 0.35f);
                    accentCol = new Color(0.72f, 0.48f, 0.28f);
                    break;
                case FurnitureType.ShoppingCart:
                    bgCol = new Color(0.20f, 0.25f, 0.35f);
                    accentCol = new Color(0.95f, 0.30f, 0.35f);
                    break;
                case FurnitureType.StorageShelf:
                    bgCol = new Color(0.25f, 0.20f, 0.18f);
                    accentCol = new Color(0.90f, 0.40f, 0.05f);
                    break;
                case FurnitureType.Cashier:
                    bgCol = new Color(0.15f, 0.25f, 0.35f);
                    accentCol = new Color(0.95f, 0.80f, 0.20f);
                    break;
                case FurnitureType.Fridge:
                    bgCol = new Color(0.12f, 0.28f, 0.42f);
                    accentCol = new Color(0.40f, 0.85f, 0.95f);
                    break;
                case FurnitureType.Freezer:
                    bgCol = new Color(0.10f, 0.30f, 0.45f);
                    accentCol = new Color(0.60f, 0.90f, 1.00f);
                    break;
                case FurnitureType.CosmeticShelf:
                    bgCol = new Color(0.35f, 0.15f, 0.30f);
                    accentCol = new Color(0.95f, 0.40f, 0.75f);
                    break;
                case FurnitureType.BakeryCounter:
                    bgCol = new Color(0.35f, 0.25f, 0.15f);
                    accentCol = new Color(0.95f, 0.70f, 0.25f);
                    break;
                case FurnitureType.ProduceShelf:
                    bgCol = new Color(0.15f, 0.30f, 0.18f);
                    accentCol = new Color(0.30f, 0.85f, 0.40f);
                    break;
                case FurnitureType.ButcherCounter:
                    bgCol = new Color(0.35f, 0.15f, 0.18f);
                    accentCol = new Color(0.95f, 0.25f, 0.30f);
                    break;
                case FurnitureType.ElectronicsShelf:
                    bgCol = new Color(0.10f, 0.20f, 0.35f);
                    accentCol = new Color(0.10f, 0.85f, 0.95f);
                    break;
            }

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    // Yuvarlak Köşeli Çerçeve Hesabı
                    float dx = Mathf.Max(0, Mathf.Abs(x - 31.5f) - 24f);
                    float dy = Mathf.Max(0, Mathf.Abs(y - 31.5f) - 24f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 8f)
                    {
                        pixels[y * sz + x] = Color.clear;
                    }
                    else
                    {
                        Color pCol = bgCol;

                        // Çerçeve Konturu
                        if (dist > 6f || x < 4 || x > 59 || y < 4 || y > 59)
                        {
                            pCol = accentCol;
                        }
                        else
                        {
                            // Mobilyaya Özel Çizim Deseni (Katlar / Raflar)
                            if (y >= 14 && y <= 17 && x >= 10 && x <= 53) pCol = accentCol; // Raf 1
                            else if (y >= 30 && y <= 33 && x >= 10 && x <= 53) pCol = accentCol; // Raf 2
                            else if (y >= 46 && y <= 49 && x >= 10 && x <= 53) pCol = accentCol; // Raf 3
                            else if ((x >= 8 && x <= 12) || (x >= 51 && x <= 55)) pCol = frameCol; // Yan Sütunlar
                        }

                        pixels[y * sz + x] = pCol;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            iconCache[type] = sprite;
            return sprite;
        }
    }
}
