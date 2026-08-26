using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Core
{
    public enum FurnitureCategory
    {
        Furniture,  // Mobilyalar (Raflar, Kasa, Dolaplar)
        Decoration, // Dekorasyonlar (Bitki, Otomat, Heykel, Bank vb.)
        Workshop    // Atölye Makineleri (Reçel Kazanı, İçecek Presi vb.)
    }

    public enum FurnitureType
    {
        // --- MOBİLYALAR ---
        Shelf,              // Raf (Seviye 1, Mağaza)
        ProduceShelf,       // Manav Rafı (Seviye 1, Mağaza)
        GourmetShelf,       // 🥫 Gurme Rafı (Seviye 1, Mağaza - Sadece Atölye Ürünleri)
        ShoppingCart,       // Alışveriş Sepeti Stantı (Seviye 1, Mağaza)
        StorageShelf,       // Depo Rafı (Seviye 1, Depo)
        Cashier,            // Kasa (Seviye 1, Mağaza)
        CustomerServiceDesk, // Müşteri Hizmetleri Masası (Seviye 1, Mağaza)
        Fridge,             // Buzdolabı (Seviye 1, Mağaza)
        Freezer,            // Dondurucu (Seviye 2, Mağaza)
        CosmeticShelf,      // Kozmetik Ürün Rafı (Seviye 2, Mağaza)
        BakeryCounter,      // Fırın Tezgahı (Seviye 2, Mağaza)
        ButcherCounter,     // Kasap Reyonu (Seviye 3, Mağaza)
        ElectronicsShelf,   // Elektronik Rafı (Seviye 3, Mağaza)

        // --- ATÖLYE MAKİNELERİ (6 Adet) ---
        WorkshopJamMaker,       // 🍓 Reçel & Marmelat Kazanı
        WorkshopJuicePress,      // 🧃 Meyve & Sebze Sıkma / İçecek Presi
        WorkshopCannery,         // 🥫 Sos, Salça & Konserve Makinesi
        WorkshopDehydrator,      // 🍿 Kurutma & Cips Fırını
        WorkshopOilPress,        // 🫒 Soğuk Sıkım Yağ Presi
        WorkshopSaladStation,    // 🥗 Gurme Salata & Fermente Meze İstasyonu

        // --- SEVİYE 1 DEKORASYONLAR (10 Adet) ---
        PlantPot,           // Saksılı İç Mekan Bitkisi
        PottedPalm,         // Palmiye Saksısı
        TrashCan,           // Paslanmaz Çöp Kovası
        BenchWood,          // Ahşap Mağaza Bankı
        WelcomeMat,         // Hoş Geldiniz Paspası
        WallClock,          // Neon Duvar Saati
        AdBanner,           // Dekoratif Reklam Panosu
        CeilingSpotlight,   // Tavan Projektör Spotu
        DividerFence,       // Ahşap Seperatör Çit
        WaterDispenser,     // Su Sebili

        // --- SEVİYE 2 DEKORASYONLAR (10 Adet) ---
        CoffeeMachine,      // Kahve Otomatı Standı
        NeonSign,           // Neon Mağaza Logosu
        FountainSmall,      // Dekoratif Mermer Havuz
        GumballMachine,     // Şeker Otomatı
        VendingSnack,       // Atıştırmalık Otomatı
        IceCreamCart,       // Dondurma Arabası
        RedCarpet,          // Kırmızı Protokol Halısı
        DigitalMenuBoard,   // Dijital Ekran Panosu
        BonsaiTree,         // Japon Bonsai Ağacı Stantı
        AtmMachine,         // Bankamatik (ATM)

        // --- SEVİYE 3 DEKORASYONLAR (10 Adet) ---
        ArcadeMachine,      // Arcade Oyun Makinesi
        AquariumGrand,      // Dev Tropikal Akvaryum
        Jukebox,            // Nostaljik Müzik Kutusu
        GoldenStatue,       // Altın Başarı Heykeli
        ChandelierCrystal,  // Lüks Kristal Avize
        SlushieMachine,     // Buzlu İçecek Otomatı
        MassageChair,       // Lüks Masaj Koltuğu
        DonutDispenser,     // Taze Donut Dolabı
        HologramProjector,  // 3D Hologram Projektörü
        FlowerArch          // Lüks Çiçek Kemeri
    }

    public enum FurnitureZone
    {
        StoreAndStorage, // Mağaza ve Depo (Hem dükkan içine hem depoya kurulabilir)
        StoreOnly,       // Sadece Mağaza Kısmı
        StorageOnly,     // Sadece Depo Kısmı
        WorkshopOnly     // Sadece Atölye Binası İçi
    }

    [System.Serializable]
    public class FurnitureItemDef
    {
        public FurnitureType type;
        public string name;
        public string nameEn;
        public string description;
        public string descriptionEn;
        public FurnitureZone zone;
        public FurnitureCategory category;
        public int requiredLevel;
        public int price;
        public string iconEmoji;
        public int passiveIncomePerUse;

        // Dinamik Yerelleştirilmiş Özellikler
        public string LocalizedName => LocalizationManager.L("Furn_Name_" + type, name, !string.IsNullOrEmpty(nameEn) ? nameEn : name);
        public string LocalizedDescription => LocalizationManager.L("Furn_Desc_" + type, description, !string.IsNullOrEmpty(descriptionEn) ? descriptionEn : description);

        public FurnitureItemDef(FurnitureType type, string name, string description, FurnitureZone zone, FurnitureCategory category, int requiredLevel, int price, string iconEmoji, int passiveIncomePerUse = 0)
            : this(type, name, name, description, description, zone, category, requiredLevel, price, iconEmoji, passiveIncomePerUse)
        {
        }

        public FurnitureItemDef(FurnitureType type, string name, string nameEn, string description, string descriptionEn, FurnitureZone zone, FurnitureCategory category, int requiredLevel, int price, string iconEmoji, int passiveIncomePerUse = 0)
        {
            this.type = type;
            this.name = name;
            this.nameEn = nameEn;
            this.description = description;
            this.descriptionEn = descriptionEn;
            this.zone = zone;
            this.category = category;
            this.requiredLevel = requiredLevel;
            this.price = price;
            this.iconEmoji = iconEmoji;
            this.passiveIncomePerUse = passiveIncomePerUse;
        }

        public string GetZoneText()
        {
            switch (zone)
            {
                case FurnitureZone.StoreAndStorage:
                    return LocalizationManager.L("Zone_StoreAndStorage", "📍 Mağaza & Depo", "📍 Store & Storage");
                case FurnitureZone.StoreOnly:
                    return LocalizationManager.L("Zone_StoreOnly", "📍 Mağaza", "📍 Store Only");
                case FurnitureZone.StorageOnly:
                    return LocalizationManager.L("Zone_StorageOnly", "📦 Depo", "📦 Storage Only");
                case FurnitureZone.WorkshopOnly:
                    return LocalizationManager.L("Zone_WorkshopOnly", "🏭 Atölye", "🏭 Workshop Only");
                default:
                    return LocalizationManager.L("Zone_StoreAndStorage", "📍 Mağaza & Depo", "📍 Store & Storage");
            }
        }
    }

    public static class FurnitureDatabase
    {
        private static readonly Dictionary<FurnitureType, FurnitureItemDef> database = new Dictionary<FurnitureType, FurnitureItemDef>()
        {
            // ==================== MOBİLYALAR ====================
            {
                FurnitureType.Shelf,
                new FurnitureItemDef(FurnitureType.Shelf, "Raf", "Shelf", "Mağaza içi için standart 4 katlı ahşap-metal teşhir rafı.", "Standard 4-tier wooden-metal display shelf for store interior.", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 1, 500, "🗄️", 0)
            },
            {
                FurnitureType.ProduceShelf,
                new FurnitureItemDef(FurnitureType.ProduceShelf, "Manav Rafı", "Produce Display", "Çiftlikten gelen taze meyve ve sebzeler için özel 3 katlı eğimli ahşap manav teşhir reyonu (%40 Kâr Marjı).", "Special 3-tier angled wooden produce stand for farm-fresh fruits & vegetables (+40% Profit Margin).", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 1, 600, "🧺", 0)
            },
            {
                FurnitureType.GourmetShelf,
                new FurnitureItemDef(FurnitureType.GourmetShelf, "Lüks Gurme Reyonu", "Luxury Gourmet Shelf", "Yalnızca atölyede üretilen reçel, konserve, cips, meyve suyu ve soğuk sıkım yağlar gibi yüksek kârlı gurme ürünler için LED aydınlatmalı ceviz ağacı reyon.", "LED-lit luxury walnut display rack strictly for high-profit workshop-crafted gourmet goods (jams, juices, oils, preserves).", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 1, 800, "🥫", 0)
            },
            {
                FurnitureType.WorkshopJamMaker,
                new WorkshopMachineFurnitureDef(FurnitureType.WorkshopJamMaker, "Reçel & Marmelat Kazanı", "Jam & Marmalade Boiler", "Çilek, kavun, karpuz, üzüm ve bal kabağını lüks gurme reçellere dönüştüren endüstriyel bakır kazan.", "Industrial copper boiler that turns strawberries, melons, grapes, and pumpkins into luxury gourmet jams.", 2500, "🍓", WorkshopMachineType.JamMaker)
            },
            {
                FurnitureType.WorkshopJuicePress,
                new WorkshopMachineFurnitureDef(FurnitureType.WorkshopJuicePress, "Meyve & Sebze Sıkma Presi", "Juice & Beverage Extractor", "Havuç, pancar, karpuz, kavun ve şalgamdan %100 doğal taze meyve suları ve nektarlar sıkan hidrolik pres.", "Hydraulic press that extracts pure 100% natural juices from carrots, beets, melons, and turnips.", 2200, "🧃", WorkshopMachineType.JuiceExtractor)
            },
            {
                FurnitureType.WorkshopCannery,
                new WorkshopMachineFurnitureDef(FurnitureType.WorkshopCannery, "Sos & Salça & Konserve Ünitesi", "Sauce & Paste Cannery", "Domates, biber, salatalık, sarımsak, enginar ve fasulyeden salça, sos ve konserveler üreten konserveleme ünitesi.", "Complete cannery that produces pure pastes, roasted sauces, and pickled preserves from tomatoes, peppers, and garlic.", 2800, "🥫", WorkshopMachineType.Cannery)
            },
            {
                FurnitureType.WorkshopDehydrator,
                new WorkshopMachineFurnitureDef(FurnitureType.WorkshopDehydrator, "Kurutma & Cips Fırını", "Dehydrator & Snack Oven", "Patates, mısır, ayçiçeği, kabak ve brokoliden çıtır gurme cips ve kurutulmuş çerezler pişiren konveksiyonel fırın.", "Convection oven that bakes crispy gourmet chips and dehydrated snacks from potatoes, corn, zucchini, and broccoli.", 2600, "🍿", WorkshopMachineType.Dehydrator)
            },
            {
                FurnitureType.WorkshopOilPress,
                new WorkshopMachineFurnitureDef(FurnitureType.WorkshopOilPress, "Soğuk Sıkım Yağ Presi", "Cold-Press Oil Press", "Ayçiçeği, bal kabağı çekirdeği, sarımsak ve acı biberden değerli soğuk sıkım yağlar çıkaran endüstriyel burgulu pres.", "Screw press that extracts precious cold-pressed oils and gourmet infused seasonings from sunflowers and pumpkin seeds.", 3200, "🫒", WorkshopMachineType.OilPress)
            },
            {
                FurnitureType.WorkshopSaladStation,
                new WorkshopMachineFurnitureDef(FurnitureType.WorkshopSaladStation, "Gurme Salata & Meze İstasyonu", "Gourmet Fermentation & Salad Station", "Marul, ıspanak, pazı, roka, lahana ve turptan taze paketli salatalar ve fermente mezeler hazırlayan hijyenik meze tezgahı.", "Hygienic prep station for packaged gourmet salads and fermented delicacies from fresh lettuce, spinach, arugula, and cabbage.", 2100, "🥗", WorkshopMachineType.SaladStation)
            },
            {
                FurnitureType.ShoppingCart,
                new FurnitureItemDef(FurnitureType.ShoppingCart, "Alışveriş Sepeti Stantı", "Shopping Cart Stand", "Müşterilerin dükkan girişinde alacağı alışveriş sepetleri stantı.", "Shopping cart rack placed near store entrance for customers.", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 1, 350, "🛒", 0)
            },
            {
                FurnitureType.StorageShelf,
                new FurnitureItemDef(FurnitureType.StorageShelf, "Depo Rafı", "Storage Rack", "Sadece depo kısmı için 200 koli kapasiteli dayanıklı turuncu endüstriyel raf.", "Heavy-duty orange industrial rack with 200 box capacity strictly for warehouse storage.", FurnitureZone.StorageOnly, FurnitureCategory.Furniture, 1, 450, "📦", 0)
            },
            {
                FurnitureType.Cashier,
                new FurnitureItemDef(FurnitureType.Cashier, "Kasa", "Cash Register Counter", "Müşteri ödemeleri için barkod okuyuculu ve bantlı modern kasa tezgahı.", "Modern checkout counter with barcode scanner and conveyor belt.", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 1, 1200, "🖥️", 0)
            },
            {
                FurnitureType.CustomerServiceDesk,
                new FurnitureItemDef(FurnitureType.CustomerServiceDesk, "Müşteri Hizmetleri Masası", "Customer Service Desk", "Müşteri hizmetleri çalışanının oturduğu ve gelen müşterilerin alışverişlerini %25 hızlandırıp ekstra ürün almalarını sağlayan modern masa.", "Modern desk for customer service agent to speed up shopping by 25% and boost extra purchases.", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 1, 750, "💁‍♂️", 0)
            },
            {
                FurnitureType.Fridge,
                new FurnitureItemDef(FurnitureType.Fridge, "Buzdolabı", "Commercial Fridge", "Süt ve soğuk içecekler için cam kapaklı ticari buzdolabı.", "Glass-door commercial refrigerator for dairy and cold drinks.", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 1, 1500, "🧊", 0)
            },
            {
                FurnitureType.Freezer,
                new FurnitureItemDef(FurnitureType.Freezer, "Dondurucu", "Horizontal Freezer", "Dondurulmuş ürünler için sürgülü üst camlı yatay dondurucu.", "Sliding glass-top freezer for frozen foods.", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 2, 2500, "❄️", 0)
            },
            {
                FurnitureType.CosmeticShelf,
                new FurnitureItemDef(FurnitureType.CosmeticShelf, "Kozmetik Ürün Rafı", "Cosmetic Display Shelf", "Kozmetik ve kişisel bakım ürünleri için LED ışıklı lüks stant.", "Luxury LED-illuminated shelf for cosmetics and personal care products.", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 2, 1800, "💄", 0)
            },
            {
                FurnitureType.BakeryCounter,
                new FurnitureItemDef(FurnitureType.BakeryCounter, "Fırın Tezgahı", "Bakery Counter", "Taze ekmek ve unlu mamuller için kavisli cam teşhir tezgahı.", "Curved glass display counter for fresh bread and baked goods.", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 2, 3200, "🥐", 0)
            },
            {
                FurnitureType.ButcherCounter,
                new FurnitureItemDef(FurnitureType.ButcherCounter, "Kasap Reyonu", "Butcher Counter", "Et ve tavuk ürünleri için soğutmalı paslanmaz çelik reyon.", "Refrigerated stainless steel counter for meat and poultry products.", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 3, 4500, "🥩", 0)
            },
            {
                FurnitureType.ElectronicsShelf,
                new FurnitureItemDef(FurnitureType.ElectronicsShelf, "Elektronik Rafı", "Electronics Display Shelf", "Elektronik cihaz ve aksesuarlar için modern titanyum teşhir rafı.", "Modern titanium display rack for electronic devices and accessories.", FurnitureZone.StoreOnly, FurnitureCategory.Furniture, 3, 4000, "🎧", 0)
            },

            // ==================== SEVİYE 1 DEKORASYONLAR (10 Adet) ====================
            {
                FurnitureType.PlantPot,
                new FurnitureItemDef(FurnitureType.PlantPot, "Saksılı İç Mekan Bitkisi", "Indoor Potted Plant", "Mağaza köşeleri için dekoratif yeşil yapraklı saksı çiçeği.", "Decorative green leafy potted plant for store corners.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 1, 120, "🪴", 0)
            },
            {
                FurnitureType.PottedPalm,
                new FurnitureItemDef(FurnitureType.PottedPalm, "Palmiye Saksısı", "Potted Palm Tree", "Giriş ve reyon araları için şık dekoratif küçük palmiye.", "Elegant decorative mini palm tree for entrances and aisles.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 1, 220, "🌴", 0)
            },
            {
                FurnitureType.TrashCan,
                new FurnitureItemDef(FurnitureType.TrashCan, "Paslanmaz Çöp Kovası", "Stainless Trash Can", "Müşteriler için geri dönüşümlü krom çöp kovası.", "Recycling chrome trash bin for customers.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 1, 150, "🗑️", 0)
            },
            {
                FurnitureType.BenchWood,
                new FurnitureItemDef(FurnitureType.BenchWood, "Ahşap Mağaza Bankı", "Wooden Store Bench", "Müşterilerin dinlenmesi için konforlu ahşap bank.", "Comfortable wooden bench for customer resting.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 1, 280, "🪑", 0)
            },
            {
                FurnitureType.WelcomeMat,
                new FurnitureItemDef(FurnitureType.WelcomeMat, "Hoş Geldiniz Paspası", "Welcome Floor Mat", "Mağaza ana girişi için özel desenli paspas.", "Custom patterned entrance mat for store front.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 1, 80, "🚪", 0)
            },
            {
                FurnitureType.WallClock,
                new FurnitureItemDef(FurnitureType.WallClock, "Neon Duvar Saati", "Neon Wall Clock", "Duvarlar için LED aydınlatmalı büyük analog saat.", "Large LED-illuminated analog wall clock.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 1, 190, "🕒", 0)
            },
            {
                FurnitureType.AdBanner,
                new FurnitureItemDef(FurnitureType.AdBanner, "Dekoratif Reklam Panosu", "Decorative Ad Banner", "Mağaza önü indirim ve duyuru panosu.", "Storefront promotional and discount announcement banner.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 1, 310, "🪧", 0)
            },
            {
                FurnitureType.CeilingSpotlight,
                new FurnitureItemDef(FurnitureType.CeilingSpotlight, "Tavan Projektör Spotu", "Ceiling Spotlight Track", "Reyonları aydınlatan 3'lü dekoratif projektör.", "3-light decorative spotlight tracking aisle illumination.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 1, 250, "💡", 0)
            },
            {
                FurnitureType.DividerFence,
                new FurnitureItemDef(FurnitureType.DividerFence, "Ahşap Seperatör Çit", "Wooden Partition Fence", "Bölüm ayırıcı modüler şık ahşap çit.", "Modular elegant wooden fence for section separation.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 1, 180, "🪵", 0)
            },
            {
                FurnitureType.WaterDispenser,
                new FurnitureItemDef(FurnitureType.WaterDispenser, "Su Sebili", "Water Dispenser", "Müşteri ve personel için soğuk/sıcak su sebili otomatı.", "Cold/hot water dispenser for customers and staff.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 1, 350, "🚰", 12)
            },

            // ==================== SEVİYE 2 DEKORASYONLAR (10 Adet) ====================
            {
                FurnitureType.CoffeeMachine,
                new FurnitureItemDef(FurnitureType.CoffeeMachine, "Kahve Otomatı Standı", "Coffee Vending Machine", "Müşteriler için taze espressolu kahve otomatı.", "Fresh espresso coffee vending machine for customers.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 2, 850, "☕", 35)
            },
            {
                FurnitureType.NeonSign,
                new FurnitureItemDef(FurnitureType.NeonSign, "Neon Mağaza Logosu", "Neon Store Sign", "Duvara asılan ışıl ışıl 'OPEN 24/7' neon yazısı.", "Bright wall-mounted 'OPEN 24/7' neon sign.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 2, 650, "🚨", 0)
            },
            {
                FurnitureType.FountainSmall,
                new FurnitureItemDef(FurnitureType.FountainSmall, "Dekoratif Mermer Havuz", "Marble Fountain", "Mağaza içi devridaimli fıskiyeli mini mermer havuz.", "Recirculating mini marble water fountain for store interior.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 2, 1200, "⛲", 0)
            },
            {
                FurnitureType.GumballMachine,
                new FurnitureItemDef(FurnitureType.GumballMachine, "Şeker Otomatı", "Candy Gumball Machine", "Çocuklar için renkli şekerleme ve sakız otomatı.", "Colorful candy and gumball dispenser for kids.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 2, 450, "🍬", 15)
            },
            {
                FurnitureType.VendingSnack,
                new FurnitureItemDef(FurnitureType.VendingSnack, "Atıştırmalık Otomatı", "Snack Vending Machine", "Cips, kraker ve çikolata otomatı.", "Automated snack machine for chips, crackers, and chocolates.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 2, 1500, "🍫", 50)
            },
            {
                FurnitureType.IceCreamCart,
                new FurnitureItemDef(FurnitureType.IceCreamCart, "Dondurma Arabası", "Ice Cream Cart", "Nostaljik tekerlekli taze dondurma aracı.", "Nostalgic wheeled fresh ice cream cart.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 2, 1100, "🍦", 25)
            },
            {
                FurnitureType.RedCarpet,
                new FurnitureItemDef(FurnitureType.RedCarpet, "Kırmızı Protokol Halısı", "Red Protocol Carpet", "Dükkan girişi için pirinç bariyerli kırmızı halı.", "Red carpet with brass stanchions for store entrance.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 2, 500, "👠", 0)
            },
            {
                FurnitureType.DigitalMenuBoard,
                new FurnitureItemDef(FurnitureType.DigitalMenuBoard, "Dijital Ekran Panosu", "Digital Menu Board", "Tavan tipi 3'lü dijital fiyat panosu.", "Ceiling-mounted 3-screen digital price board.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 2, 950, "🖥️", 0)
            },
            {
                FurnitureType.BonsaiTree,
                new FurnitureItemDef(FurnitureType.BonsaiTree, "Japon Bonsai Ağacı Stantı", "Japanese Bonsai Tree Stand", "Ahşap podyumlu lüks bonsai sanatı.", "Luxury bonsai tree displayed on wooden podium.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 2, 780, "🌳", 0)
            },
            {
                FurnitureType.AtmMachine,
                new FurnitureItemDef(FurnitureType.AtmMachine, "Bankamatik (ATM)", "ATM Cash Machine", "Müşteriler için hızlı nakit para çekme otomatı.", "Automated teller machine for fast customer cash withdrawals.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 2, 1800, "🏧", 0)
            },

            // ==================== SEVİYE 3 DEKORASYONLAR (10 Adet) ====================
            {
                FurnitureType.ArcadeMachine,
                new FurnitureItemDef(FurnitureType.ArcadeMachine, "Arcade Oyun Makinesi", "Retro Arcade Cabinet", "Müşterilerin vakit geçireceği retro jetonlu oyun kabini.", "Coin-operated arcade cabinet for customer entertainment.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 3, 2400, "🕹️", 75)
            },
            {
                FurnitureType.AquariumGrand,
                new FurnitureItemDef(FurnitureType.AquariumGrand, "Dev Tropikal Akvaryum", "Grand Tropical Aquarium", "İçinde renkli balıklar yüzen ışıklı dev akvaryum.", "Giant illuminated aquarium with swimming colorful tropical fish.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 3, 3500, "🐠", 0)
            },
            {
                FurnitureType.Jukebox,
                new FurnitureItemDef(FurnitureType.Jukebox, "Nostaljik Müzik Kutusu", "Nostalgic Jukebox", "Neon aydınlatmalı retro jukebox müzik otomatı.", "Retro neon-lit jukebox music machine.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 3, 2800, "📻", 80)
            },
            {
                FurnitureType.GoldenStatue,
                new FurnitureItemDef(FurnitureType.GoldenStatue, "Altın Başarı Heykeli", "Golden Achievement Statue", "Siyah mermer kaideli 24 ayar altın kaplama heykel.", "24k gold-plated trophy statue on black marble base.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 3, 4200, "🏆", 0)
            },
            {
                FurnitureType.ChandelierCrystal,
                new FurnitureItemDef(FurnitureType.ChandelierCrystal, "Lüks Kristal Avize", "Luxury Crystal Chandelier", "Tavan için sarkıt parlak kristal dev avize.", "Hanging sparkling crystal chandelier for ceiling.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 3, 3800, "✨", 0)
            },
            {
                FurnitureType.SlushieMachine,
                new FurnitureItemDef(FurnitureType.SlushieMachine, "Buzlu İçecek Otomatı", "Slushie Machine", "2 hazneli buzlu meyve suyu otomatı.", "2-tank frozen slushie beverage dispenser.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 3, 2100, "🍹", 40)
            },
            {
                FurnitureType.MassageChair,
                new FurnitureItemDef(FurnitureType.MassageChair, "Lüks Masaj Koltuğu", "Luxury Massage Chair", "Müşterilerin rahatlaması için deri masaj koltuğu.", "Leather massage chair for customer relaxation.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 3, 2600, "🛋️", 100)
            },
            {
                FurnitureType.DonutDispenser,
                new FurnitureItemDef(FurnitureType.DonutDispenser, "Taze Donut Dolabı", "Fresh Donut Cabinet", "Döner cam vitrinli ışıklı donut otomatı.", "Illuminated rotary glass display donut dispenser.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 3, 2200, "🍩", 45)
            },
            {
                FurnitureType.HologramProjector,
                new FurnitureItemDef(FurnitureType.HologramProjector, "3D Hologram Projektörü", "3D Hologram Projector", "Mağaza ortasında dönen 3D reklam hologramı.", "Futuristic 3D rotating ad hologram projector.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 3, 4500, "🔮", 0)
            },
            {
                FurnitureType.FlowerArch,
                new FurnitureItemDef(FurnitureType.FlowerArch, "Lüks Çiçek Kemeri", "Luxury Flower Arch", "Kırmızı ve beyaz güllerle kaplı ihtişamlı çiçek takı.", "Grand floral arch covered in red and white roses.", FurnitureZone.StoreOnly, FurnitureCategory.Decoration, 3, 3100, "💐", 0)
            }
        };

        public static FurnitureItemDef GetDef(FurnitureType type)
        {
            if (database.TryGetValue(type, out FurnitureItemDef def))
            {
                return def;
            }
            return null;
        }

        public static List<FurnitureItemDef> GetAllDefs()
        {
            return new List<FurnitureItemDef>(database.Values);
        }

        public static List<FurnitureItemDef> GetDefsByCategory(FurnitureCategory cat)
        {
            List<FurnitureItemDef> list = new List<FurnitureItemDef>();
            foreach (var kvp in database)
            {
                if (kvp.Value.category == cat)
                {
                    list.Add(kvp.Value);
                }
            }
            return list;
        }
    }

    [System.Serializable]
    public class WorkshopMachineFurnitureDef : FurnitureItemDef
    {
        public WorkshopMachineType machineType;

        public WorkshopMachineFurnitureDef(
            FurnitureType type,
            string name,
            string nameEn,
            string description,
            string descriptionEn,
            int price,
            string iconEmoji,
            WorkshopMachineType machineType
        ) : base(type, name, nameEn, description, descriptionEn, FurnitureZone.WorkshopOnly, FurnitureCategory.Workshop, 1, price, iconEmoji, 0)
        {
            this.machineType = machineType;
        }
    }
}
