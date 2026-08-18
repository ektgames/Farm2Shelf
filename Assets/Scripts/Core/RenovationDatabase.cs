using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    public enum RenovationType
    {
        WallPaint,
        FloorStyle
    }

    public class RenovationItemDef
    {
        public string id;
        public string nameTr;
        public string nameEn;
        public RenovationType type;
        public int requiredLevel; // 1, 2, 3
        public int price;
        public string iconEmoji;
        public Color itemColor;

        public string Name => LocalizationManager.L("Reno_" + id, nameTr, nameEn);

        public RenovationItemDef(string id, string nameTr, string nameEn, RenovationType type, int requiredLevel, int price, string iconEmoji, Color itemColor)
        {
            this.id = id;
            this.nameTr = nameTr;
            this.nameEn = nameEn;
            this.type = type;
            this.requiredLevel = requiredLevel;
            this.price = price;
            this.iconEmoji = iconEmoji;
            this.itemColor = itemColor;
        }
    }

    public static class RenovationDatabase
    {
        private static readonly List<RenovationItemDef> wallPaints = new List<RenovationItemDef>();
        private static readonly List<RenovationItemDef> floorStyles = new List<RenovationItemDef>();

        static RenovationDatabase()
        {
            // === DUVAR BOYALARI (15 ADET - Seviye 1, 2, 3 için 5'er tane) ===
            // Level 1:
            wallPaints.Add(new RenovationItemDef("Wall_White", "Klasik Beyaz Boya", "Classic White Paint", RenovationType.WallPaint, 1, 200, "🎨", new Color(0.95f, 0.95f, 0.95f)));
            wallPaints.Add(new RenovationItemDef("Wall_Cream", "Krem İpek Boya", "Silk Cream Paint", RenovationType.WallPaint, 1, 250, "🎨", new Color(0.95f, 0.90f, 0.80f)));
            wallPaints.Add(new RenovationItemDef("Wall_WarmYellow", "Sıcak Güneş Sarı", "Warm Sun Yellow", RenovationType.WallPaint, 1, 300, "🎨", new Color(0.95f, 0.88f, 0.55f)));
            wallPaints.Add(new RenovationItemDef("Wall_MintGreen", "Ferah Nane Yeşili", "Fresh Mint Green", RenovationType.WallPaint, 1, 350, "🎨", new Color(0.60f, 0.88f, 0.70f)));
            wallPaints.Add(new RenovationItemDef("Wall_IceBlue", "Buz Mavisi Boya", "Ice Blue Paint", RenovationType.WallPaint, 1, 400, "🎨", new Color(0.65f, 0.82f, 0.95f)));

            // Level 2:
            wallPaints.Add(new RenovationItemDef("Wall_SlateGrey", "Modern Kayrak Gri", "Modern Slate Grey", RenovationType.WallPaint, 2, 500, "🎨", new Color(0.35f, 0.40f, 0.45f)));
            wallPaints.Add(new RenovationItemDef("Wall_TuscanRed", "Tuğla Kırmızısı", "Tuscan Brick Red", RenovationType.WallPaint, 2, 600, "🎨", new Color(0.70f, 0.25f, 0.20f)));
            wallPaints.Add(new RenovationItemDef("Wall_RoyalNavy", "Kraliyet Laciverti", "Royal Navy Blue", RenovationType.WallPaint, 2, 700, "🎨", new Color(0.12f, 0.20f, 0.40f)));
            wallPaints.Add(new RenovationItemDef("Wall_Mocha", "Moka Kahvesi", "Mocha Brown", RenovationType.WallPaint, 2, 800, "🎨", new Color(0.45f, 0.32f, 0.22f)));
            wallPaints.Add(new RenovationItemDef("Wall_Lavender", "Lavantalı Mor", "Lavender Purple", RenovationType.WallPaint, 2, 900, "🎨", new Color(0.55f, 0.40f, 0.65f)));

            // Level 3:
            wallPaints.Add(new RenovationItemDef("Wall_Emerald", "Lüks Zümrüt Yeşili", "Luxury Emerald Green", RenovationType.WallPaint, 3, 1100, "🎨", new Color(0.08f, 0.45f, 0.28f)));
            wallPaints.Add(new RenovationItemDef("Wall_Obsidian", "Mat Obsidyen Siyahı", "Matte Obsidian Black", RenovationType.WallPaint, 3, 1300, "🎨", new Color(0.12f, 0.14f, 0.16f)));
            wallPaints.Add(new RenovationItemDef("Wall_GoldAccent", "Altın Varaklı Beyaz", "Gold Accent White", RenovationType.WallPaint, 3, 1500, "🎨", new Color(0.98f, 0.95f, 0.85f)));
            wallPaints.Add(new RenovationItemDef("Wall_DeepRuby", "Derin Yakut Bordo", "Deep Ruby Burgundy", RenovationType.WallPaint, 3, 1750, "🎨", new Color(0.50f, 0.10f, 0.18f)));
            wallPaints.Add(new RenovationItemDef("Wall_CyberTurquoise", "Siber Turkuaz", "Cyber Turquoise", RenovationType.WallPaint, 3, 2000, "🎨", new Color(0.10f, 0.85f, 0.85f)));

            // === ZEMİN KAPLAMALARI (15 ADET - Seviye 1, 2, 3 için 5'er tane) ===
            // Level 1:
            floorStyles.Add(new RenovationItemDef("Floor_LightOak", "Açık Meşe Parke", "Light Oak Parquet", RenovationType.FloorStyle, 1, 250, "🧱", new Color(0.85f, 0.72f, 0.52f)));
            floorStyles.Add(new RenovationItemDef("Floor_GreyConcrete", "Gri Cilalı Beton", "Polished Grey Concrete", RenovationType.FloorStyle, 1, 300, "🧱", new Color(0.60f, 0.62f, 0.65f)));
            floorStyles.Add(new RenovationItemDef("Floor_Terracotta", "Pişmiş Kil Karo", "Terracotta Tile", RenovationType.FloorStyle, 1, 350, "🧱", new Color(0.78f, 0.42f, 0.28f)));
            floorStyles.Add(new RenovationItemDef("Floor_BeigeCeramic", "Bej Seramik Karo", "Beige Ceramic Tile", RenovationType.FloorStyle, 1, 400, "🧱", new Color(0.90f, 0.85f, 0.75f)));
            floorStyles.Add(new RenovationItemDef("Floor_RusticPlank", "Rustik Ahşap Zemin", "Rustic Wood Floor", RenovationType.FloorStyle, 1, 450, "🧱", new Color(0.65f, 0.48f, 0.32f)));

            // Level 2:
            floorStyles.Add(new RenovationItemDef("Floor_DarkWalnut", "Koyu Ceviz Parke", "Dark Walnut Hardwood", RenovationType.FloorStyle, 2, 600, "🧱", new Color(0.35f, 0.22f, 0.14f)));
            floorStyles.Add(new RenovationItemDef("Floor_CarraraMarble", "Beyaz Carrara Mermer", "White Carrara Marble", RenovationType.FloorStyle, 2, 750, "🧱", new Color(0.92f, 0.94f, 0.95f)));
            floorStyles.Add(new RenovationItemDef("Floor_CheckeredBW", "Dama Siyah-Beyaz", "Checkered Black & White", RenovationType.FloorStyle, 2, 900, "🧱", new Color(0.40f, 0.40f, 0.42f)));
            floorStyles.Add(new RenovationItemDef("Floor_GranitePolished", "Cilalı Siyah Granit", "Polished Black Granite", RenovationType.FloorStyle, 2, 1050, "🧱", new Color(0.20f, 0.22f, 0.25f)));
            floorStyles.Add(new RenovationItemDef("Floor_IndustrialEpoxy", "Mavi Epoksi Zemin", "Blue Industrial Epoxy", RenovationType.FloorStyle, 2, 1200, "🧱", new Color(0.18f, 0.45f, 0.70f)));

            // Level 3:
            floorStyles.Add(new RenovationItemDef("Floor_RoyalCarpet", "Kraliyet Kırmızı Halı", "Royal Red Carpet", RenovationType.FloorStyle, 3, 1400, "🧱", new Color(0.65f, 0.10f, 0.15f)));
            floorStyles.Add(new RenovationItemDef("Floor_GoldMarble", "Altın Damarlı Mermer", "Gold Veined Marble", RenovationType.FloorStyle, 3, 1650, "🧱", new Color(0.95f, 0.88f, 0.65f)));
            floorStyles.Add(new RenovationItemDef("Floor_ChevronParket", "Fransız Chevron Ahşap", "French Chevron Parquet", RenovationType.FloorStyle, 3, 1900, "🧱", new Color(0.52f, 0.36f, 0.22f)));
            floorStyles.Add(new RenovationItemDef("Floor_HexMosaic", "Siyah Heksagon Mozaik", "Black Hexagon Mosaic", RenovationType.FloorStyle, 3, 2200, "🧱", new Color(0.15f, 0.17f, 0.20f)));
            floorStyles.Add(new RenovationItemDef("Floor_GlassMirror", "Ayna Cam Zemin", "Mirror Glass Flooring", RenovationType.FloorStyle, 3, 2500, "🧱", new Color(0.30f, 0.85f, 0.95f)));
        }

        public static List<RenovationItemDef> GetWallPaints() => wallPaints;
        public static List<RenovationItemDef> GetFloorStyles() => floorStyles;
    }
}
