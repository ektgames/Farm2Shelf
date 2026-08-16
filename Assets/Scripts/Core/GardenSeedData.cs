using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Core
{
    public class GardenSeedDef
    {
        public string id;
        public string name;
        public string nameEn;
        public TimeManager.Season season;
        public int requiredLevel;
        public int packPrice;        // 10'lu paket fiyatı
        public int growthDays;        // 1 - 5 gün arası büyüme süresi
        public int yieldPerPlot;     // Tek tarladan çıkan ürün adedi (ör. 35 adet)
        public int unitSalePrice;    // Manav rafında adet satış fiyatı (%40 kâr marjlı)
        public Color cropColor;
        public string iconEmoji;

        // Dinamik Dil Desteği
        public string LocalizedName => LocalizationManager.L("Seed_" + id, name, !string.IsNullOrEmpty(nameEn) ? nameEn : name);

        public GardenSeedDef(string id, string name, TimeManager.Season season, int requiredLevel, int packPrice, int growthDays, int yieldPerPlot, int unitSalePrice, Color cropColor, string iconEmoji)
            : this(id, name, name, season, requiredLevel, packPrice, growthDays, yieldPerPlot, unitSalePrice, cropColor, iconEmoji)
        {
        }

        public GardenSeedDef(string id, string name, string nameEn, TimeManager.Season season, int requiredLevel, int packPrice, int growthDays, int yieldPerPlot, int unitSalePrice, Color cropColor, string iconEmoji)
        {
            this.id = id;
            this.name = name;
            this.nameEn = nameEn;
            this.season = season;
            this.requiredLevel = requiredLevel;
            this.packPrice = packPrice;
            this.growthDays = growthDays;
            this.yieldPerPlot = yieldPerPlot;
            this.unitSalePrice = unitSalePrice;
            this.cropColor = cropColor;
            this.iconEmoji = iconEmoji;
        }
    }

    public static class GardenSeedDatabase
    {
        private static List<GardenSeedDef> seeds;

        public static List<GardenSeedDef> GetAllSeeds()
        {
            if (seeds == null) InitDatabase();
            return seeds;
        }

        public static GardenSeedDef GetSeedById(string id)
        {
            if (seeds == null) InitDatabase();
            return seeds.Find(s => s.id == id);
        }

        public static List<GardenSeedDef> GetSeedsBySeason(TimeManager.Season season)
        {
            if (seeds == null) InitDatabase();
            return seeds.FindAll(s => s.season == season);
        }

        private static void InitDatabase()
        {
            seeds = new List<GardenSeedDef>();

            // --- İLKBAHAR TOHUMLARI (10 Adet) ---
            seeds.Add(new GardenSeedDef("spring_tomato", "Domates Tohumu", "Tomato Seeds", TimeManager.Season.İlkbahar, 1, 150, 1, 35, 12, new Color(0.92f, 0.18f, 0.15f), "🍅"));
            seeds.Add(new GardenSeedDef("spring_cucumber", "Salatalık Tohumu", "Cucumber Seeds", TimeManager.Season.İlkbahar, 1, 130, 1, 35, 10, new Color(0.20f, 0.75f, 0.30f), "🥒"));
            seeds.Add(new GardenSeedDef("spring_strawberry", "Çilek Tohumu", "Strawberry Seeds", TimeManager.Season.İlkbahar, 2, 280, 2, 40, 22, new Color(0.95f, 0.15f, 0.35f), "🍓"));
            seeds.Add(new GardenSeedDef("spring_carrot", "Havuç Tohumu", "Carrot Seeds", TimeManager.Season.İlkbahar, 2, 220, 2, 35, 18, new Color(0.95f, 0.50f, 0.10f), "🥕"));
            seeds.Add(new GardenSeedDef("spring_lettuce", "Marul Tohumu", "Lettuce Seeds", TimeManager.Season.İlkbahar, 1, 120, 1, 30, 9, new Color(0.35f, 0.85f, 0.25f), "🥬"));
            seeds.Add(new GardenSeedDef("spring_pea", "Bezelye Tohumu", "Pea Seeds", TimeManager.Season.İlkbahar, 3, 390, 3, 45, 28, new Color(0.30f, 0.80f, 0.35f), "🫛"));
            seeds.Add(new GardenSeedDef("spring_radish", "Turp Tohumu", "Radish Seeds", TimeManager.Season.İlkbahar, 2, 240, 2, 35, 19, new Color(0.85f, 0.20f, 0.40f), "🌱"));
            seeds.Add(new GardenSeedDef("spring_artichoke", "Enginar Tohumu", "Artichoke Seeds", TimeManager.Season.İlkbahar, 4, 580, 4, 50, 42, new Color(0.25f, 0.65f, 0.40f), "🫛"));
            seeds.Add(new GardenSeedDef("spring_spinach", "Ispanak Tohumu", "Spinach Seeds", TimeManager.Season.İlkbahar, 2, 210, 2, 35, 16, new Color(0.15f, 0.55f, 0.20f), "🍃"));
            seeds.Add(new GardenSeedDef("spring_asparagus", "Kuşkonmaz Tohumu", "Asparagus Seeds", TimeManager.Season.İlkbahar, 5, 820, 5, 60, 62, new Color(0.40f, 0.70f, 0.30f), "🎋"));

            // --- YAZ TOHUMLARI (10 Adet) ---
            seeds.Add(new GardenSeedDef("summer_watermelon", "Karpuz Tohumu", "Watermelon Seeds", TimeManager.Season.Yaz, 5, 850, 5, 60, 65, new Color(0.15f, 0.65f, 0.25f), "🍉"));
            seeds.Add(new GardenSeedDef("summer_melon", "Kavun Tohumu", "Melon Seeds", TimeManager.Season.Yaz, 4, 620, 4, 50, 45, new Color(0.95f, 0.82f, 0.20f), "🍈"));
            seeds.Add(new GardenSeedDef("summer_pepper", "Biber Tohumu", "Pepper Seeds", TimeManager.Season.Yaz, 2, 250, 2, 35, 20, new Color(0.85f, 0.15f, 0.15f), "🫑"));
            seeds.Add(new GardenSeedDef("summer_eggplant", "Patlıcan Tohumu", "Eggplant Seeds", TimeManager.Season.Yaz, 3, 420, 3, 45, 30, new Color(0.45f, 0.15f, 0.55f), "🍆"));
            seeds.Add(new GardenSeedDef("summer_corn", "Mısır Tohumu", "Corn Seeds", TimeManager.Season.Yaz, 3, 440, 3, 45, 32, new Color(0.95f, 0.85f, 0.15f), "🌽"));
            seeds.Add(new GardenSeedDef("summer_zucchini", "Kabak Tohumu", "Zucchini Seeds", TimeManager.Season.Yaz, 2, 230, 2, 35, 18, new Color(0.40f, 0.75f, 0.30f), "🥒"));
            seeds.Add(new GardenSeedDef("summer_greenbean", "Taze Fasulye", "Green Bean Seeds", TimeManager.Season.Yaz, 2, 240, 2, 35, 19, new Color(0.30f, 0.70f, 0.25f), "🫛"));
            seeds.Add(new GardenSeedDef("summer_sunflower", "Ayçiçeği Tohumu", "Sunflower Seeds", TimeManager.Season.Yaz, 4, 650, 4, 50, 48, new Color(0.95f, 0.75f, 0.10f), "🌻"));
            seeds.Add(new GardenSeedDef("summer_okra", "Bamya Tohumu", "Okra Seeds", TimeManager.Season.Yaz, 3, 410, 3, 45, 29, new Color(0.35f, 0.65f, 0.25f), "🌾"));
            seeds.Add(new GardenSeedDef("summer_grape", "Üzüm Tohumu", "Grape Seeds", TimeManager.Season.Yaz, 5, 890, 5, 60, 68, new Color(0.40f, 0.12f, 0.50f), "🍇"));

            // --- SONBAHAR TOHUMLARI (10 Adet) ---
            seeds.Add(new GardenSeedDef("autumn_pumpkin", "Balkabağı Tohumu", "Pumpkin Seeds", TimeManager.Season.Sonbahar, 5, 920, 5, 60, 72, new Color(0.95f, 0.45f, 0.08f), "🎃"));
            seeds.Add(new GardenSeedDef("autumn_potato", "Patates Tohumu", "Potato Seeds", TimeManager.Season.Sonbahar, 2, 260, 2, 40, 21, new Color(0.72f, 0.52f, 0.30f), "🥔"));
            seeds.Add(new GardenSeedDef("autumn_onion", "Soğan Tohumu", "Onion Seeds", TimeManager.Season.Sonbahar, 2, 230, 2, 35, 18, new Color(0.85f, 0.75f, 0.60f), "🧅"));
            seeds.Add(new GardenSeedDef("autumn_garlic", "Sarımsak Tohumu", "Garlic Seeds", TimeManager.Season.Sonbahar, 3, 450, 3, 45, 33, new Color(0.92f, 0.90f, 0.85f), "🧄"));
            seeds.Add(new GardenSeedDef("autumn_turnip", "Şalgam Tohumu", "Turnip Seeds", TimeManager.Season.Sonbahar, 3, 410, 3, 45, 29, new Color(0.80f, 0.30f, 0.60f), "🌱"));
            seeds.Add(new GardenSeedDef("autumn_beet", "Pancar Tohumu", "Beet Seeds", TimeManager.Season.Sonbahar, 3, 430, 3, 45, 31, new Color(0.65f, 0.12f, 0.25f), "🫚"));
            seeds.Add(new GardenSeedDef("autumn_cabbage", "Lahana Tohumu", "Cabbage Seeds", TimeManager.Season.Sonbahar, 3, 460, 3, 45, 34, new Color(0.35f, 0.75f, 0.30f), "🥬"));
            seeds.Add(new GardenSeedDef("autumn_broccoli", "Brokoli Tohumu", "Broccoli Seeds", TimeManager.Season.Sonbahar, 4, 640, 4, 50, 46, new Color(0.20f, 0.60f, 0.25f), "🥦"));
            seeds.Add(new GardenSeedDef("autumn_cauliflower", "Karnabahar Tohumu", "Cauliflower Seeds", TimeManager.Season.Sonbahar, 4, 660, 4, 50, 48, new Color(0.90f, 0.92f, 0.85f), "🥦"));
            seeds.Add(new GardenSeedDef("autumn_wintercarrot", "Kışlık Havuç", "Winter Carrot Seeds", TimeManager.Season.Sonbahar, 3, 420, 3, 45, 30, new Color(0.95f, 0.48f, 0.08f), "🥕"));

            // --- KIŞ TOHUMLARI (10 Adet) ---
            seeds.Add(new GardenSeedDef("winter_greenhousestrawberry", "Sera Çileği Tohumu", "Greenhouse Strawberry Seeds", TimeManager.Season.Kış, 4, 710, 4, 50, 52, new Color(0.95f, 0.12f, 0.30f), "🍓"));
            seeds.Add(new GardenSeedDef("winter_cabbage", "Kış Lahanası", "Winter Cabbage Seeds", TimeManager.Season.Kış, 3, 470, 3, 45, 35, new Color(0.25f, 0.65f, 0.30f), "🥬"));
            seeds.Add(new GardenSeedDef("winter_leek", "Pırasa Tohumu", "Leek Seeds", TimeManager.Season.Kış, 3, 440, 3, 45, 32, new Color(0.40f, 0.75f, 0.40f), "🫛"));
            seeds.Add(new GardenSeedDef("winter_brusselssprout", "Brüksel Lahanası", "Brussels Sprout Seeds", TimeManager.Season.Kış, 4, 680, 4, 50, 49, new Color(0.30f, 0.70f, 0.35f), "🥬"));
            seeds.Add(new GardenSeedDef("winter_radish", "Kış Turbu Tohumu", "Winter Radish Seeds", TimeManager.Season.Kış, 3, 430, 3, 45, 31, new Color(0.85f, 0.85f, 0.90f), "🌱"));
            seeds.Add(new GardenSeedDef("winter_carrot", "Kış Havucu", "Winter Carrot Seeds", TimeManager.Season.Kış, 3, 450, 3, 45, 33, new Color(0.92f, 0.42f, 0.05f), "🥕"));
            seeds.Add(new GardenSeedDef("winter_garlic", "Kış Sarımsağı", "Winter Garlic Seeds", TimeManager.Season.Kış, 4, 690, 4, 50, 50, new Color(0.90f, 0.88f, 0.85f), "🧄"));
            seeds.Add(new GardenSeedDef("winter_chard", "Pazı Tohumu", "Chard Seeds", TimeManager.Season.Kış, 2, 270, 2, 35, 20, new Color(0.80f, 0.18f, 0.25f), "🍃"));
            seeds.Add(new GardenSeedDef("winter_arugula", "Roka Tohumu", "Arugula Seeds", TimeManager.Season.Kış, 2, 250, 2, 35, 18, new Color(0.25f, 0.60f, 0.25f), "🌿"));
            seeds.Add(new GardenSeedDef("winter_cress", "Tere Tohumu", "Cress Seeds", TimeManager.Season.Kış, 2, 240, 2, 35, 17, new Color(0.30f, 0.65f, 0.28f), "🌿"));
        }
    }
}
