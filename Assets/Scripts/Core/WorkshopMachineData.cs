using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Core
{
    public enum WorkshopMachineType
    {
        JamMaker,        // 🍓 Reçel & Marmelat Kazanı
        JuiceExtractor,  // 🧃 Meyve & Sebze Sıkma / İçecek Presi
        Cannery,         // 🥫 Sos, Salça & Konserve Makinesi
        Dehydrator,      // 🍿 Kurutma & Cips Fırını
        OilPress,        // 🫒 Soğuk Sıkım Yağ Presi
        SaladStation     // 🥗 Gurme Salata & Fermente Meze Ünitesi
    }

    [System.Serializable]
    public class WorkshopRecipeDef
    {
        public string recipeId;
        public string cropId; // Girdi hammadde tohum/mahsul ID'si (Örn: spring_strawberry)
        public string outputProductId;
        public string outputNameTr;
        public string outputNameEn;
        public string iconEmoji;
        public WorkshopMachineType machineType;
        public int requiredCropKg; // Gerekli hammadde (Örn: 25 KG)
        public int outputPackCount; // Çıkan gurme ürün adedi (Örn: 25 Adet)
        public float durationSeconds; // Üretim süresi (120 sn - 600 sn arası)
        public int unitSalePrice; // Dükkanda birim satış fiyatı (Normal mahsule göre 3x - 5x kârlı)

        public string LocalizedName => LocalizationManager.L("Recipe_" + recipeId, outputNameTr, outputNameEn);

        public WorkshopRecipeDef(
            string recipeId,
            string cropId,
            string outputProductId,
            string outputNameTr,
            string outputNameEn,
            string iconEmoji,
            WorkshopMachineType machineType,
            int requiredCropKg,
            int outputPackCount,
            float durationSeconds,
            int unitSalePrice)
        {
            this.recipeId = recipeId;
            this.cropId = cropId;
            this.outputProductId = outputProductId;
            this.outputNameTr = outputNameTr;
            this.outputNameEn = outputNameEn;
            this.iconEmoji = iconEmoji;
            this.machineType = machineType;
            this.requiredCropKg = requiredCropKg;
            this.outputPackCount = outputPackCount;
            this.durationSeconds = durationSeconds;
            this.unitSalePrice = unitSalePrice;
        }
    }

    [System.Serializable]
    public class WorkshopMachineDef
    {
        public WorkshopMachineType type;
        public string nameTr;
        public string nameEn;
        public string descTr;
        public string descEn;
        public string iconEmoji;
        public int price;
        public int requiredWorkshopLevel;

        public string LocalizedName => LocalizationManager.L("Machine_Name_" + type, nameTr, nameEn);
        public string LocalizedDesc => LocalizationManager.L("Machine_Desc_" + type, descTr, descEn);

        public WorkshopMachineDef(WorkshopMachineType type, string nameTr, string nameEn, string descTr, string descEn, string iconEmoji, int price, int requiredWorkshopLevel)
        {
            this.type = type;
            this.nameTr = nameTr;
            this.nameEn = nameEn;
            this.descTr = descTr;
            this.descEn = descEn;
            this.iconEmoji = iconEmoji;
            this.price = price;
            this.requiredWorkshopLevel = requiredWorkshopLevel;
        }
    }

    /// <summary>
    /// Atölye makineleri ve oyundaki 40 mahsulün tamamını kapsayan tarif veritabanı.
    /// </summary>
    public static class WorkshopMachineDatabase
    {
        private static List<WorkshopMachineDef> machines;
        private static List<WorkshopRecipeDef> recipes;

        public static List<WorkshopMachineDef> GetAllMachines()
        {
            if (machines == null) InitDatabase();
            return machines;
        }

        public static WorkshopMachineDef GetMachineByType(WorkshopMachineType type)
        {
            if (machines == null) InitDatabase();
            return machines.Find(m => m.type == type);
        }

        public static List<WorkshopRecipeDef> GetAllRecipes()
        {
            if (recipes == null) InitDatabase();
            return recipes;
        }

        public static List<WorkshopRecipeDef> GetRecipesForMachine(WorkshopMachineType machineType)
        {
            if (recipes == null) InitDatabase();
            return recipes.FindAll(r => r.machineType == machineType);
        }

        public static WorkshopRecipeDef GetRecipeById(string recipeId)
        {
            if (recipes == null) InitDatabase();
            return recipes.Find(r => r.recipeId == recipeId);
        }

        public static WorkshopRecipeDef GetRecipeByOutputId(string outputProductId)
        {
            if (recipes == null) InitDatabase();
            return recipes.Find(r => r.outputProductId == outputProductId);
        }

        private static void InitDatabase()
        {
            // 1. MAKİNE TANIMLARI (6 Adet Endüstriyel Makine)
            machines = new List<WorkshopMachineDef>
            {
                new WorkshopMachineDef(
                    WorkshopMachineType.JamMaker,
                    "Reçel & Marmelat Kazanı",
                    "Jam & Marmalade Boiler",
                    "Çilek, kavun, karpuz, üzüm ve bal kabağını lüks gurme reçellere ve pekmezlere dönüştürür.",
                    "Turns strawberries, melons, watermelons, grapes, and pumpkins into luxury jams and molasses.",
                    "🍓",
                    2500,
                    1
                ),
                new WorkshopMachineDef(
                    WorkshopMachineType.JuiceExtractor,
                    "Meyve & Sebze Sıkma Presi",
                    "Juice & Beverage Extractor",
                    "Havuç, pancar, karpuz, kavun, üzüm ve şalgamdan %100 doğal taze meyve suları ve nektarlar sıkar.",
                    "Extracts 100% natural juices and nectars from carrots, beets, melons, grapes, and turnips.",
                    "🧃",
                    2200,
                    1
                ),
                new WorkshopMachineDef(
                    WorkshopMachineType.Cannery,
                    "Sos & Salça & Konserve Ünitesi",
                    "Sauce & Paste Cannery",
                    "Domates, biber, salatalık, sarımsak, enginar, bezelye, fasulye ve patlıcandan salça, sos ve konserveler üretir.",
                    "Produces pure pastes, sauces, and gourmet preserves from tomatoes, peppers, cucumbers, garlic, and beans.",
                    "🥫",
                    2800,
                    1
                ),
                new WorkshopMachineDef(
                    WorkshopMachineType.Dehydrator,
                    "Kurutma & Cips Fırını",
                    "Dehydrator & Snack Oven",
                    "Patates, mısır, ayçiçeği, kabak, brokoli, karnabahar ve kuşkonmazdan çıtır gurme cips ve çerezler pişirir.",
                    "Bakes crispy gourmet chips and dehydrated snacks from potatoes, corn, sunflower, zucchini, and broccoli.",
                    "🍿",
                    2600,
                    1
                ),
                new WorkshopMachineDef(
                    WorkshopMachineType.OilPress,
                    "Soğuk Sıkım Yağ Presi",
                    "Cold-Press Oil Press",
                    "Ayçiçeği, bal kabağı çekirdeği, sarımsak ve acı biberden değerli soğuk sıkım yağlar ve çeşniler elde eder.",
                    "Extracts precious cold-pressed oils and gourmet infused seasonings from sunflowers, pumpkin seeds, and garlic.",
                    "🫒",
                    3200,
                    1
                ),
                new WorkshopMachineDef(
                    WorkshopMachineType.SaladStation,
                    "Gurme Salata & Meze Ünitesi",
                    "Gourmet Fermentation & Salad Station",
                    "Marul, ıspanak, pazı, roka, tere, lahana, pırasa ve turptan paketli gurme salatalar ve fermente mezeler hazırlar.",
                    "Prepares packaged gourmet salads and fermented delicacies from lettuce, spinach, chard, arugula, and cabbage.",
                    "🥗",
                    2100,
                    1
                )
            };

            // 2. 40 MAHSULÜN TAMAMINI KAPSAYAN GURME TARİFLERİ
            recipes = new List<WorkshopRecipeDef>();

            // --- MAKİNE 1: REÇEL & MARMELAT KAZANI ---
            recipes.Add(new WorkshopRecipeDef("rec_strawberry_jam", "spring_strawberry", "gourmet_strawberry_jam", "🍓 Gurme Çilek Reçeli", "🍓 Gourmet Strawberry Jam", "🍓", WorkshopMachineType.JamMaker, 25, 25, 300f, 95));
            recipes.Add(new WorkshopRecipeDef("rec_gh_strawberry_marmalade", "winter_greenhousestrawberry", "gourmet_gh_strawberry_marmalade", "🍓 Lüks Sera Çileği Marmelatı", "🍓 Luxury Strawberry Marmalade", "🍓", WorkshopMachineType.JamMaker, 25, 25, 480f, 160));
            recipes.Add(new WorkshopRecipeDef("rec_pumpkin_jam", "autumn_pumpkin", "gourmet_pumpkin_dessert", "🎃 Fırınlanmış Bal Kabağı Reçeli", "🎃 Baked Pumpkin Jam", "🎃", WorkshopMachineType.JamMaker, 25, 25, 420f, 150));
            recipes.Add(new WorkshopRecipeDef("rec_melon_marmalade", "summer_melon", "gourmet_melon_marmalade", "🍈 Gurme Kavun Marmelatı", "🍈 Gourmet Melon Marmalade", "🍈", WorkshopMachineType.JamMaker, 25, 25, 270f, 90));
            recipes.Add(new WorkshopRecipeDef("rec_watermelon_rind_jam", "summer_watermelon", "gourmet_watermelon_jam", "🍉 Karpuz Kabuğu Reçeli", "🍉 Watermelon Rind Jam", "🍉", WorkshopMachineType.JamMaker, 25, 25, 450f, 145));
            recipes.Add(new WorkshopRecipeDef("rec_grape_molasses", "summer_grape", "gourmet_grape_molasses", "🍇 Taş Fırın Köy Pekmezi", "🍇 Traditional Grape Molasses", "🍇", WorkshopMachineType.JamMaker, 25, 25, 540f, 180));

            // --- MAKİNE 2: MEYVE & SEBZE SIKMA İÇECEK PRESİ ---
            recipes.Add(new WorkshopRecipeDef("rec_carrot_juice", "spring_carrot", "gourmet_carrot_juice", "🥕 %100 Doğal Havuç Suyu", "🥕 100% Natural Carrot Juice", "🥕", WorkshopMachineType.JuiceExtractor, 25, 25, 240f, 75));
            recipes.Add(new WorkshopRecipeDef("rec_winter_carrot_juice", "winter_carrot", "gourmet_winter_carrot_juice", "🥕 Vitaminli Kış Havucu Suyu", "🥕 Vitamin Winter Carrot Juice", "🥕", WorkshopMachineType.JuiceExtractor, 25, 25, 240f, 80));
            recipes.Add(new WorkshopRecipeDef("rec_autumn_carrot_nectar", "autumn_wintercarrot", "gourmet_autumn_carrot_nectar", "🥕 Zencefilli Havuç Nektarı", "🥕 Ginger Carrot Nectar", "🥕", WorkshopMachineType.JuiceExtractor, 25, 25, 240f, 75));
            recipes.Add(new WorkshopRecipeDef("rec_beet_detox", "autumn_beet", "gourmet_beet_detox", "🫚 Kırmızı Detoks Pancar Suyu", "🫚 Red Detox Beet Juice", "🫚", WorkshopMachineType.JuiceExtractor, 25, 25, 260f, 85));
            recipes.Add(new WorkshopRecipeDef("rec_watermelon_smoothie", "summer_watermelon", "gourmet_watermelon_coldjuice", "🍉 Soğuk Karpuzlu İçecek", "🍉 Chilled Watermelon Juice", "🍉", WorkshopMachineType.JuiceExtractor, 25, 25, 360f, 135));
            recipes.Add(new WorkshopRecipeDef("rec_melon_smoothie", "summer_melon", "gourmet_melon_smoothie", "🍈 Taze Kavunlu Smoothie", "🍈 Fresh Melon Smoothie", "🍈", WorkshopMachineType.JuiceExtractor, 25, 25, 250f, 88));
            recipes.Add(new WorkshopRecipeDef("rec_grape_juice", "summer_grape", "gourmet_grape_juice", "🍇 Saf Sıkım Üzüm Suyu", "🍇 Pure Grape Juice", "🍇", WorkshopMachineType.JuiceExtractor, 25, 25, 480f, 165));
            recipes.Add(new WorkshopRecipeDef("rec_turnip_juice", "autumn_turnip", "gourmet_turnip_juice", "🌱 Acılı Gurme Şalgam Suyu", "🌱 Spicy Gourmet Turnip Juice", "🌱", WorkshopMachineType.JuiceExtractor, 25, 25, 180f, 55));

            // --- MAKİNE 3: SOS, SALÇA & KONSERVE ÜNİTESİ ---
            recipes.Add(new WorkshopRecipeDef("rec_tomato_paste", "spring_tomato", "gourmet_tomato_paste", "🥫 Katkısız Köy Domates Salçası", "🥫 Pure Tomato Paste", "🥫", WorkshopMachineType.Cannery, 25, 25, 240f, 65));
            recipes.Add(new WorkshopRecipeDef("rec_pepper_paste", "summer_pepper", "gourmet_pepper_paste", "🌶️ Köz Biber Salçası", "🌶️ Roasted Pepper Paste", "🌶️", WorkshopMachineType.Cannery, 25, 25, 240f, 65));
            recipes.Add(new WorkshopRecipeDef("rec_cucumber_pickle", "spring_cucumber", "gourmet_cucumber_pickle", "🫙 Çıtır Kornişon Turşusu", "🫙 Crispy Pickles", "🫙", WorkshopMachineType.Cannery, 25, 25, 180f, 55));
            recipes.Add(new WorkshopRecipeDef("rec_garlic_puree", "autumn_garlic", "gourmet_garlic_puree", "🧄 Sarımsak Ezmesi Kavanozu", "🧄 Garlic Puree Jar", "🧄", WorkshopMachineType.Cannery, 25, 25, 260f, 85));
            recipes.Add(new WorkshopRecipeDef("rec_black_garlic_paste", "winter_garlic", "gourmet_black_garlic_paste", "🧄 Gurme Siyah Sarımsak Ezmesi", "🧄 Black Garlic Paste", "🧄", WorkshopMachineType.Cannery, 25, 25, 450f, 155));
            recipes.Add(new WorkshopRecipeDef("rec_onion_sauce", "autumn_onion", "gourmet_onion_sauce", "🧅 Karamelize Soğan Sosu", "🧅 Caramelized Onion Sauce", "🧅", WorkshopMachineType.Cannery, 25, 25, 180f, 55));
            recipes.Add(new WorkshopRecipeDef("rec_roasted_eggplant", "summer_eggplant", "gourmet_roasted_eggplant", "🍆 Közlenmiş Patlıcan Konservesi", "🍆 Roasted Eggplant Preserve", "🍆", WorkshopMachineType.Cannery, 25, 25, 280f, 90));
            recipes.Add(new WorkshopRecipeDef("rec_artichoke_preserve", "spring_artichoke", "gourmet_artichoke_preserve", "🫙 Zeytinyağlı Enginar Kalbi", "🫙 Artichoke Hearts in Oil", "🫙", WorkshopMachineType.Cannery, 25, 25, 450f, 150));
            recipes.Add(new WorkshopRecipeDef("rec_sweet_peas", "spring_pea", "gourmet_sweet_peas", "🫙 Tatlı Bezelye Konservesi", "🫙 Sweet Peas Preserve", "🫙", WorkshopMachineType.Cannery, 25, 25, 340f, 115));
            recipes.Add(new WorkshopRecipeDef("rec_green_beans", "summer_greenbean", "gourmet_green_beans", "🫙 Gurme Taze Fasulye Konservesi", "🫙 Green Bean Preserve", "🫙", WorkshopMachineType.Cannery, 25, 25, 220f, 65));
            recipes.Add(new WorkshopRecipeDef("rec_okra_preserve", "summer_okra", "gourmet_okra_preserve", "🫙 Ege Usulü Bamya Konservesi", "🫙 Pickled Okra Jar", "🫙", WorkshopMachineType.Cannery, 25, 25, 260f, 85));
            recipes.Add(new WorkshopRecipeDef("rec_brussels_pickle", "winter_brusselssprout", "gourmet_brussels_pickle", "🫙 Brüksel Lahanası Turşusu", "🫙 Pickled Brussels Sprouts", "🫙", WorkshopMachineType.Cannery, 25, 25, 390f, 140));

            // --- MAKİNE 4: KURUTMA & CİPS FIRINI ---
            recipes.Add(new WorkshopRecipeDef("rec_potato_chips", "autumn_potato", "gourmet_potato_chips", "🥔 Çıtır Gurme Patates Cipsi", "🥔 Crispy Gourmet Potato Chips", "🥔", WorkshopMachineType.Dehydrator, 25, 25, 200f, 60));
            recipes.Add(new WorkshopRecipeDef("rec_popcorn_snack", "summer_corn", "gourmet_popcorn_snack", "🍿 Gurme Mısır Cipsi & Popcorn", "🍿 Gourmet Popcorn Snack", "🍿", WorkshopMachineType.Dehydrator, 25, 25, 270f, 90));
            recipes.Add(new WorkshopRecipeDef("rec_roasted_sunflower", "summer_sunflower", "gourmet_roasted_sunflower", "🌻 Çifte Kavrulmuş Çekirdek", "🌻 Double Roasted Sunflower Seeds", "🌻", WorkshopMachineType.Dehydrator, 25, 25, 380f, 135));
            recipes.Add(new WorkshopRecipeDef("rec_asparagus_bites", "spring_asparagus", "gourmet_asparagus_bites", "🎋 Fırınlanmış Kuşkonmaz Çerezi", "🎋 Baked Asparagus Snack", "🎋", WorkshopMachineType.Dehydrator, 25, 25, 500f, 175));
            recipes.Add(new WorkshopRecipeDef("rec_zucchini_chips", "summer_zucchini", "gourmet_zucchini_chips", "🥒 Fırınlanmış Kabak Cipsi", "🥒 Baked Zucchini Chips", "🥒", WorkshopMachineType.Dehydrator, 25, 25, 200f, 58));
            recipes.Add(new WorkshopRecipeDef("rec_broccoli_bites", "autumn_broccoli", "gourmet_broccoli_bites", "🥦 Çıtır Brokoli Cipsi", "🥦 Crispy Broccoli Bites", "🥦", WorkshopMachineType.Dehydrator, 25, 25, 360f, 130));
            recipes.Add(new WorkshopRecipeDef("rec_cauliflower_bites", "autumn_cauliflower", "gourmet_cauliflower_bites", "🥦 Baharatlı Karnabahar Atıştırmalığı", "🥦 Spiced Cauliflower Bites", "🥦", WorkshopMachineType.Dehydrator, 25, 25, 380f, 135));

            // --- MAKİNE 5: SOĞUK SIKIM YAĞ PRESİ ---
            recipes.Add(new WorkshopRecipeDef("rec_sunflower_oil", "summer_sunflower", "gourmet_sunflower_oil", "🫒 Saf Soğuk Sıkım Ayçiçek Yağı", "🫒 Pure Sunflower Oil", "🫒", WorkshopMachineType.OilPress, 25, 25, 420f, 155));
            recipes.Add(new WorkshopRecipeDef("rec_pumpkin_seed_oil", "autumn_pumpkin", "gourmet_pumpkin_seed_oil", "🫒 Değerli Balkabağı Çekirdeği Yağı", "🫒 Cold-Pressed Pumpkin Seed Oil", "🫒", WorkshopMachineType.OilPress, 25, 25, 480f, 185));
            recipes.Add(new WorkshopRecipeDef("rec_garlic_infused_oil", "autumn_garlic", "gourmet_garlic_infused_oil", "🫒 Aromatik Sarımsaklı Çeşni Yağı", "🫒 Garlic Infused Olive Oil", "🫒", WorkshopMachineType.OilPress, 25, 25, 350f, 125));
            recipes.Add(new WorkshopRecipeDef("rec_chili_infused_oil", "summer_pepper", "gourmet_chili_infused_oil", "🌶️ Acı Biberli Gurme Yağ", "🌶️ Hot Chili Infused Oil", "🌶️", WorkshopMachineType.OilPress, 25, 25, 280f, 85));

            // --- MAKİNE 6: GURME SALATA & FERMENTE MEZE ÜNİTESİ ---
            recipes.Add(new WorkshopRecipeDef("rec_med_salad", "spring_lettuce", "gourmet_med_salad", "🥗 Paketli Gurme Akdeniz Salatası", "🥗 Packaged Mediterranean Salad", "🥗", WorkshopMachineType.SaladStation, 25, 25, 140f, 48));
            recipes.Add(new WorkshopRecipeDef("rec_baby_spinach", "spring_spinach", "gourmet_baby_spinach", "🍃 Yıkanmış Bebek Ispanak Paketi", "🍃 Washed Baby Spinach Pack", "🍃", WorkshopMachineType.SaladStation, 25, 25, 220f, 72));
            recipes.Add(new WorkshopRecipeDef("rec_chard_rolls", "winter_chard", "gourmet_chard_rolls", "🍃 Gurme Zeytinyağlı Pazı Mezesi", "🍃 Gourmet Chard Appetizer", "🍃", WorkshopMachineType.SaladStation, 25, 25, 160f, 50));
            recipes.Add(new WorkshopRecipeDef("rec_wild_arugula", "winter_arugula", "gourmet_wild_arugula", "🌿 Yabani Roka Salatası Paketi", "🌿 Wild Arugula Salad Pack", "🌿", WorkshopMachineType.SaladStation, 25, 25, 140f, 48));
            recipes.Add(new WorkshopRecipeDef("rec_fresh_cress", "winter_cress", "gourmet_fresh_cress", "🌿 Taze Gurme Bahçe Teresi", "🌿 Fresh Gourmet Garden Cress", "🌿", WorkshopMachineType.SaladStation, 25, 25, 140f, 48));
            recipes.Add(new WorkshopRecipeDef("rec_sauerkraut", "autumn_cabbage", "gourmet_sauerkraut", "🥬 Fermente Lahana Turşusu (Sauerkraut)", "🥬 Fermented Sauerkraut Jar", "🥬", WorkshopMachineType.SaladStation, 25, 25, 260f, 85));
            recipes.Add(new WorkshopRecipeDef("rec_red_cabbage_slaw", "winter_cabbage", "gourmet_red_cabbage_slaw", "🥬 Gurme Kırmızı Lahana Mezesi", "🥬 Red Cabbage Slaw", "🥬", WorkshopMachineType.SaladStation, 25, 25, 260f, 85));
            recipes.Add(new WorkshopRecipeDef("rec_braised_leeks", "winter_leek", "gourmet_braised_leeks", "🫛 Zeytinyağlı Gurme Pırasa", "🫛 Braised Leeks in Olive Oil", "🫛", WorkshopMachineType.SaladStation, 25, 25, 240f, 80));
            recipes.Add(new WorkshopRecipeDef("rec_pink_radish_pickle", "spring_radish", "gourmet_pink_radish_pickle", "🌱 Pembe Turp Mezesi", "🌱 Pink Pickled Radish", "🌱", WorkshopMachineType.SaladStation, 25, 25, 220f, 75));
            recipes.Add(new WorkshopRecipeDef("rec_winter_radish_slaw", "winter_radish", "gourmet_winter_radish_slaw", "🌱 Şifalı Kış Turbu Salatası", "🌱 Winter Radish Salad", "🌱", WorkshopMachineType.SaladStation, 25, 25, 220f, 75));
        }
    }
}
