using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Core
{
    [System.Serializable]
    public class WholesaleProductDef
    {
        public string id;
        public string name;
        public string nameEn;
        public string iconEmoji;
        public FurnitureType targetShelfType;
        public int requiredLevel;
        public int wholesaleUnitPrice;   // Adet Başı Toptan Alış Fiyatı (Örn: 10C)
        public int packQuantity;          // Koli İçi Adedi (Sabit 50 Adet)
        public float profitMarginPercent; // %20 Kar Marjı

        // Dinamik Dil Desteği
        public string LocalizedName => LocalizationManager.L("Prod_" + id, name, !string.IsNullOrEmpty(nameEn) ? nameEn : name);

        // Koli Bazlı Hesaplamalar (50 Adet Ürün İçin)
        public int TotalPackCost => wholesaleUnitPrice * packQuantity;
        public int SalePricePerUnit => Mathf.RoundToInt(wholesaleUnitPrice * (1f + profitMarginPercent / 100f));
        public int UnitProfit => SalePricePerUnit - wholesaleUnitPrice;
        public int TotalPackSales => SalePricePerUnit * packQuantity;
        public int TotalPackProfit => UnitProfit * packQuantity;

        // Oyuncu Tarafından Ayarlanan Dinamik Satış Fiyatı
        public int CurrentSalePrice => WholesaleDatabase.GetProductSalePrice(id);
        public float CurrentProfitMarginPercent => wholesaleUnitPrice > 0 ? (((float)CurrentSalePrice - wholesaleUnitPrice) / (float)wholesaleUnitPrice) * 100f : 0f;
        public bool IsOverpriced => CurrentSalePrice > Mathf.RoundToInt(wholesaleUnitPrice * 1.30f);

        public WholesaleProductDef(string id, string name, string iconEmoji, FurnitureType targetShelfType, int requiredLevel, int wholesaleUnitPrice, int packQuantity = 50, float profitMarginPercent = 20f)
            : this(id, name, name, iconEmoji, targetShelfType, requiredLevel, wholesaleUnitPrice, packQuantity, profitMarginPercent)
        {
        }

        public WholesaleProductDef(string id, string name, string nameEn, string iconEmoji, FurnitureType targetShelfType, int requiredLevel, int wholesaleUnitPrice, int packQuantity = 50, float profitMarginPercent = 20f)
        {
            this.id = id;
            this.name = name;
            this.nameEn = nameEn;
            this.iconEmoji = iconEmoji;
            this.targetShelfType = targetShelfType;
            this.requiredLevel = requiredLevel;
            this.wholesaleUnitPrice = wholesaleUnitPrice;
            this.packQuantity = packQuantity;
            this.profitMarginPercent = profitMarginPercent;
        }

        public string GetTargetShelfText()
        {
            switch (targetShelfType)
            {
                case FurnitureType.Shelf: return LocalizationManager.L("Shelf_Display", "🗄️ Teşhir Rafı", "🗄️ Display Shelf");
                case FurnitureType.Fridge: return LocalizationManager.L("Shelf_Fridge", "🧊 Buzdolabı", "🧊 Refrigerator");
                case FurnitureType.Freezer: return LocalizationManager.L("Shelf_Freezer", "❄️ Dondurucu", "❄️ Freezer");
                case FurnitureType.BakeryCounter: return LocalizationManager.L("Shelf_Bakery", "🥐 Fırın Tezgahı", "🥐 Bakery Counter");
                case FurnitureType.CosmeticShelf: return LocalizationManager.L("Shelf_Cosmetic", "💄 Kozmetik Rafı", "💄 Cosmetic Shelf");
                case FurnitureType.ButcherCounter: return LocalizationManager.L("Shelf_Butcher", "🥩 Kasap Reyonu", "🥩 Butcher Counter");
                case FurnitureType.ElectronicsShelf: return LocalizationManager.L("Shelf_Electronics", "🎧 Elektronik Rafı", "🎧 Electronics Shelf");
                default: return LocalizationManager.L("Shelf_Display", "🗄️ Teşhir Rafı", "🗄️ Display Shelf");
            }
        }
    }

    public static class WholesaleDatabase
    {
        private static readonly List<WholesaleProductDef> products = new List<WholesaleProductDef>()
        {
            // ==================== SEVİYE 1 TOPTAN ÜRÜNLER (20 ADET - 50'Lİ KOLİLER) ====================
            new WholesaleProductDef("p1", "Somun Ekmek (500g)", "Bread Loaf (500g)", "🍞", FurnitureType.Shelf, 1, 10, 50),
            new WholesaleProductDef("p2", "Tam Yağlı Süt 1L", "Whole Milk 1L", "🥛", FurnitureType.Fridge, 1, 20, 50),
            new WholesaleProductDef("p3", "Süzme Peynir (250g)", "Strained Cheese (250g)", "🧀", FurnitureType.Fridge, 1, 50, 50),
            new WholesaleProductDef("p4", "Gezen Tavuk Yumurtası (10'lu)", "Free-Range Eggs (10 Pack)", "🥚", FurnitureType.Fridge, 1, 35, 50),
            new WholesaleProductDef("p5", "Çubuk Makarna 500g", "Spaghetti Pasta 500g", "🍝", FurnitureType.Shelf, 1, 15, 50),
            new WholesaleProductDef("p6", "Baldo Pirinç 1kg", "Baldo Rice 1kg", "🌾", FurnitureType.Shelf, 1, 40, 50),
            new WholesaleProductDef("p7", "Buğday Unu 1kg", "Wheat Flour 1kg", "🌾", FurnitureType.Shelf, 1, 25, 50),
            new WholesaleProductDef("p8", "Toz Şeker 1kg", "Granulated Sugar 1kg", "🍬", FurnitureType.Shelf, 1, 30, 50),
            new WholesaleProductDef("p9", "Ayçiçek Yağı 1L", "Sunflower Oil 1L", "🌻", FurnitureType.Shelf, 1, 45, 50),
            new WholesaleProductDef("p10", "Siyah Zeytin 500g", "Black Olives 500g", "🫒", FurnitureType.Fridge, 1, 60, 50),
            new WholesaleProductDef("p11", "Doğal Kaynak Suyu 5L", "Natural Spring Water 5L", "💧", FurnitureType.Fridge, 1, 18, 50),
            new WholesaleProductDef("p12", "Çıtır Sokak Simiti", "Crispy Turkish Bagel (Simit)", "🥖", FurnitureType.BakeryCounter, 1, 10, 50),
            new WholesaleProductDef("p13", "Peynirli Poğaça", "Cheese Pastry (Poğaça)", "🥨", FurnitureType.BakeryCounter, 1, 12, 50),
            new WholesaleProductDef("p14", "İspir Kuru Fasulye 1kg", "White Dry Beans 1kg", "🫘", FurnitureType.Shelf, 1, 55, 50),
            new WholesaleProductDef("p15", "Kırmızı Mercimek 1kg", "Red Lentils 1kg", "🥣", FurnitureType.Shelf, 1, 32, 50),
            new WholesaleProductDef("p16", "Domates Salçası 830g", "Tomato Paste 830g", "🥫", FurnitureType.Shelf, 1, 38, 50),
            new WholesaleProductDef("p17", "İyotlu Sofra Tuzu 1kg", "Iodized Table Salt 1kg", "🧂", FurnitureType.Shelf, 1, 8, 50),
            new WholesaleProductDef("p18", "Rize Siyah Çay 500g", "Black Tea 500g", "☕", FurnitureType.Shelf, 1, 65, 50),
            new WholesaleProductDef("p19", "Türk Kahvesi 100g", "Turkish Coffee 100g", "☕", FurnitureType.Shelf, 1, 28, 50),
            new WholesaleProductDef("p20", "%100 Şeftali Meyve Suyu 1L", "100% Peach Juice 1L", "🧃", FurnitureType.Fridge, 1, 22, 50),

            // ==================== SEVİYE 2 TOPTAN ÜRÜNLER (20 ADET - 50'Lİ KOLİLER) ====================
            new WholesaleProductDef("p21", "Trabzon Tereyağı 500g", "Farm Butter 500g", "🧈", FurnitureType.Fridge, 2, 120, 50),
            new WholesaleProductDef("p22", "Taze Kaşar Peyniri 500g", "Fresh Cheddar Cheese 500g", "🧀", FurnitureType.Fridge, 2, 95, 50),
            new WholesaleProductDef("p23", "Dondurulmuş Patates 1kg", "Frozen French Fries 1kg", "🍟", FurnitureType.Freezer, 2, 40, 50),
            new WholesaleProductDef("p24", "Karışık Donuk Pizza 450g", "Frozen Mixed Pizza 450g", "🍕", FurnitureType.Freezer, 2, 75, 50),
            new WholesaleProductDef("p25", "Maraş Usulü Dondurma 1L", "Traditional Ice Cream 1L", "🍦", FurnitureType.Freezer, 2, 60, 50),
            new WholesaleProductDef("p26", "Donuk Somon Fileto 500g", "Frozen Salmon Fillet 500g", "🐟", FurnitureType.Freezer, 2, 130, 50),
            new WholesaleProductDef("p27", "Tereyağlı Kruvasan", "Butter Croissant", "🥐", FurnitureType.BakeryCounter, 2, 20, 50),
            new WholesaleProductDef("p28", "Çikolatalı Pasta Dilimi", "Chocolate Cake Slice", "🍰", FurnitureType.BakeryCounter, 2, 45, 50),
            new WholesaleProductDef("p29", "Besleyici Şampuan 500ml", "Nourishing Shampoo 500ml", "🧴", FurnitureType.CosmeticShelf, 2, 70, 50),
            new WholesaleProductDef("p30", "Zeytinyağlı Sıvı Sabun 500ml", "Olive Oil Liquid Soap 500ml", "🧼", FurnitureType.CosmeticShelf, 2, 35, 50),
            new WholesaleProductDef("p31", "Beyazlatıcı Diş Macunu", "Whitening Toothpaste", "🪥", FurnitureType.CosmeticShelf, 2, 50, 50),
            new WholesaleProductDef("p32", "Şeftalili Soğuk Çay 1L", "Peach Iced Tea 1L", "🥤", FurnitureType.Fridge, 2, 24, 50),
            new WholesaleProductDef("p33", "Enerji İçeceği 250ml", "Energy Drink 250ml", "⚡", FurnitureType.Fridge, 2, 30, 50),
            new WholesaleProductDef("p34", "Doğal Maden Suyu 6'lı", "Sparkling Water 6-Pack", "🍾", FurnitureType.Fridge, 2, 36, 50),
            new WholesaleProductDef("p35", "Sütlü Çikolata 100g", "Milk Chocolate 100g", "🍫", FurnitureType.Shelf, 2, 25, 50),
            new WholesaleProductDef("p36", "Baharatlı Patates Cipsi", "Spiced Potato Chips", "🥔", FurnitureType.Shelf, 2, 22, 50),
            new WholesaleProductDef("p37", "Kremalı Bisküvi 3'lü", "Cream Biscuits 3-Pack", "🍪", FurnitureType.Shelf, 2, 18, 50),
            new WholesaleProductDef("p38", "Sızma Zeytinyağı 1L", "Extra Virgin Olive Oil 1L", "🫒", FurnitureType.Shelf, 2, 180, 50),
            new WholesaleProductDef("p39", "Çifte Kavrulmuş Kaju 150g", "Double Roasted Cashews 150g", "🥜", FurnitureType.Shelf, 2, 65, 50),
            new WholesaleProductDef("p40", "Antep Fıstığı 150g", "Pistachios 150g", "🥜", FurnitureType.Shelf, 2, 90, 50),

            // ==================== SEVİYE 3 TOPTAN ÜRÜNLER (20 ADET - 50'Lİ KOLİLER) ====================
            new WholesaleProductDef("p41", "Taze Dana Kıyma 1kg", "Fresh Minced Beef 1kg", "🥩", FurnitureType.ButcherCounter, 3, 320, 50),
            new WholesaleProductDef("p42", "Dana Kuşbaşı 1kg", "Diced Beef Cubes 1kg", "🥩", FurnitureType.ButcherCounter, 3, 360, 50),
            new WholesaleProductDef("p43", "Organik Tavuk Göğsü 1kg", "Organic Chicken Breast 1kg", "🍗", FurnitureType.ButcherCounter, 3, 140, 50),
            new WholesaleProductDef("p44", "Kangal Fermente Sucuk 500g", "Fermented Beef Sausage 500g", "🌭", FurnitureType.ButcherCounter, 3, 220, 50),
            new WholesaleProductDef("p45", "Dana Antrikot 1kg", "Prime Beef Ribeye 1kg", "🥩", FurnitureType.ButcherCounter, 3, 480, 50),
            new WholesaleProductDef("p46", "Kablosuz Bluetooth Kulaklık", "Wireless Bluetooth Earbuds", "🎧", FurnitureType.ElectronicsShelf, 3, 350, 50),
            new WholesaleProductDef("p47", "10.000 mAh Hızlı Powerbank", "10,000 mAh Fast Powerbank", "🔋", FurnitureType.ElectronicsShelf, 3, 280, 50),
            new WholesaleProductDef("p48", "Type-C Hızlı Şarj Kablosu", "Type-C Fast Charge Cable", "🔌", FurnitureType.ElectronicsShelf, 3, 90, 50),
            new WholesaleProductDef("p49", "Ergonomik Kablosuz Fare", "Ergonomic Wireless Mouse", "🖱️", FurnitureType.ElectronicsShelf, 3, 220, 50),
            new WholesaleProductDef("p50", "Fit Spor Akıllı Saat", "Fitness Smartwatch", "⌚", FurnitureType.ElectronicsShelf, 3, 600, 50),
            new WholesaleProductDef("p51", "Nemlendirici Cilt Kremi 100ml", "Moisturizing Face Cream 100ml", "💄", FurnitureType.CosmeticShelf, 3, 110, 50),
            new WholesaleProductDef("p52", "EDP Lüks Parfüm 50ml", "EDP Luxury Perfume 50ml", "✨", FurnitureType.CosmeticShelf, 3, 250, 50),
            new WholesaleProductDef("p53", "Hyalüronik Yüz Serumu 30ml", "Hyaluronic Face Serum 30ml", "🧴", FurnitureType.CosmeticShelf, 3, 180, 50),
            new WholesaleProductDef("p54", "Donuk Karaköy Böreği 1kg", "Frozen Layered Pastry 1kg", "🥐", FurnitureType.Freezer, 3, 110, 50),
            new WholesaleProductDef("p55", "Kasap İnegöl Köfte 1kg", "Butcher Meatballs 1kg", "🧆", FurnitureType.Freezer, 3, 240, 50),
            new WholesaleProductDef("p56", "Kuzu Pirzola / Kafes 1kg", "Lamb Chops Rack 1kg", "🥩", FurnitureType.ButcherCounter, 3, 520, 50),
            new WholesaleProductDef("p57", "Füme İskoç Somon 200g", "Smoked Salmon 200g", "🐟", FurnitureType.Fridge, 3, 190, 50),
            new WholesaleProductDef("p58", "İthal Parmigiano Peyniri 200g", "Imported Parmigiano Cheese 200g", "🧀", FurnitureType.Fridge, 3, 160, 50),
            new WholesaleProductDef("p59", "64GB USB 3.2 Bellek", "64GB USB 3.2 Flash Drive", "💾", FurnitureType.ElectronicsShelf, 3, 120, 50),
            new WholesaleProductDef("p60", "Bluetooth Su Geçirmez Hoparlör", "Waterproof Bluetooth Speaker", "🔊", FurnitureType.ElectronicsShelf, 3, 450, 50)
        };

        private static readonly Dictionary<string, int> customPrices = new Dictionary<string, int>();

        public static List<WholesaleProductDef> GetAllProducts() => products;

        public static WholesaleProductDef GetProductById(string id)
        {
            return products.Find(p => p.id == id);
        }

        public static int GetProductSalePrice(string productId)
        {
            if (customPrices.TryGetValue(productId, out int price))
            {
                return price;
            }
            WholesaleProductDef def = GetProductById(productId);
            return def != null ? def.SalePricePerUnit : 0;
        }

        public static void SetProductSalePrice(string productId, int price)
        {
            WholesaleProductDef def = GetProductById(productId);
            int minPrice = def != null ? Mathf.Max(1, def.wholesaleUnitPrice) : 1;
            customPrices[productId] = Mathf.Max(minPrice, price);
        }

        public static string GetLocalizedProductName(string rawNameOrId)
        {
            if (string.IsNullOrEmpty(rawNameOrId)) return "";
            WholesaleProductDef def = products.Find(p => p.id == rawNameOrId || p.name == rawNameOrId || p.nameEn == rawNameOrId);
            return def != null ? def.LocalizedName : rawNameOrId;
        }

        public static void ResetAllPricesToDefault()
        {
            customPrices.Clear();
        }
    }
}
