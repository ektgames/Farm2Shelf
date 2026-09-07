using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Core
{
    public enum LivestockType
    {
        WhiteChicken,
        BlackChicken,
        HolsteinCow,
        BrownCow
    }

    public class LivestockShopDef
    {
        public LivestockType type;
        public string id;
        public string nameTr;
        public string nameEn;
        public string iconEmoji;
        public int unitPrice;
        public bool isChicken;

        public string LocalizedName => LocalizationManager.L("Animal_" + id, nameTr, nameEn);

        public LivestockShopDef(LivestockType type, string id, string nameTr, string nameEn, string iconEmoji, int unitPrice, bool isChicken)
        {
            this.type = type;
            this.id = id;
            this.nameTr = nameTr;
            this.nameEn = nameEn;
            this.iconEmoji = iconEmoji;
            this.unitPrice = unitPrice;
            this.isChicken = isChicken;
        }
    }

    public static class LivestockProductDatabase
    {
        public const string EggId = "livestock_egg";
        public const string MilkId = "livestock_milk";

        public const int PackSize = 50;
        public const int MaxChickens = 12;
        public const int MaxCows = 6;
        public const int MaxEggStorage = PackSize * 8;
        public const int MaxMilkStorage = PackSize * 6;
        public const float DaytimeProductionHours = 12f;
        public const float DaysPerCratePerAnimal = 2.5f;
        public const int ProductionHourStart = 7;
        public const int ProductionHourEnd = 18;

        private static readonly List<LivestockShopDef> shopAnimals = new List<LivestockShopDef>
        {
            new LivestockShopDef(LivestockType.WhiteChicken, "white_chicken", "Beyaz Tavuk", "White Chicken", "🐔", 380, true),
            new LivestockShopDef(LivestockType.BlackChicken, "black_chicken", "Siyah Tavuk", "Black Chicken", "🐓", 450, true),
            new LivestockShopDef(LivestockType.HolsteinCow, "holstein_cow", "Siyah-Beyaz İnek", "Black & White Cow", "🐄", 2200, false),
            new LivestockShopDef(LivestockType.BrownCow, "brown_cow", "Kahverengi İnek", "Brown Cow", "🐮", 2500, false)
        };

        public static WholesaleProductDef EggProduct => new WholesaleProductDef(
            EggId,
            "Organik Çiftlik Yumurtası",
            "Organic Farm Eggs",
            "🥚",
            FurnitureType.OrganicFridge,
            1,
            25,
            PackSize,
            40f,
            isOrderable: false);

        public static WholesaleProductDef MilkProduct => new WholesaleProductDef(
            MilkId,
            "Organik Çiftlik Sütü 1L",
            "Organic Farm Milk 1L",
            "🥛",
            FurnitureType.OrganicFridge,
            1,
            32,
            PackSize,
            40f,
            isOrderable: false);

        public static List<LivestockShopDef> GetShopAnimals() => shopAnimals;

        public static LivestockShopDef GetShopDef(LivestockType type) => shopAnimals.Find(a => a.type == type);

        public static LivestockShopDef GetShopDefById(string id) => shopAnimals.Find(a => a.id == id);

        public static bool IsLivestockProduct(string idOrName)
        {
            if (string.IsNullOrEmpty(idOrName)) return false;
            if (idOrName == EggId || idOrName == MilkId) return true;
            WholesaleProductDef egg = EggProduct;
            WholesaleProductDef milk = MilkProduct;
            return string.Equals(idOrName, egg.name, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(idOrName, egg.nameEn, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(idOrName, milk.name, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(idOrName, milk.nameEn, System.StringComparison.OrdinalIgnoreCase)
                || idOrName.IndexOf("Organik Çiftlik", System.StringComparison.OrdinalIgnoreCase) >= 0
                || idOrName.IndexOf("Organic Farm", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static WholesaleProductDef GetById(string id)
        {
            if (id == EggId) return EggProduct;
            if (id == MilkId) return MilkProduct;
            return null;
        }

        public static List<WholesaleProductDef> GetAllProducts()
        {
            return new List<WholesaleProductDef> { EggProduct, MilkProduct };
        }

        public static WholesaleProductDef CreateDeliveryPack(string productId, int packAmount)
        {
            WholesaleProductDef source = GetById(productId);
            if (source == null) return null;
            int qty = (packAmount / PackSize) * PackSize;
            if (qty < PackSize) return null;
            return new WholesaleProductDef(
                source.id,
                source.name,
                source.nameEn,
                source.iconEmoji,
                FurnitureType.OrganicFridge,
                1,
                source.wholesaleUnitPrice,
                qty,
                40f,
                isOrderable: false);
        }

        public static float GetUnitsPerHourPerAnimal()
        {
            float hours = DaytimeProductionHours * DaysPerCratePerAnimal;
            if (hours < 0.01f) return 0f;
            return PackSize / hours;
        }

        public static int GetUnitsPerHour(int animalCount)
        {
            if (animalCount <= 0) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(animalCount * GetUnitsPerHourPerAnimal()));
        }

        public static float GetExactUnitsPerHour(int animalCount)
        {
            if (animalCount <= 0) return 0f;
            return animalCount * GetUnitsPerHourPerAnimal();
        }

        public static int GetQuickSellUnitPrice(string productId)
        {
            WholesaleProductDef def = GetById(productId);
            if (def == null) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(def.wholesaleUnitPrice * 1.20f));
        }

        public static int GetMarketUnitPrice(string productId)
        {
            WholesaleProductDef def = WholesaleDatabase.GetProductById(productId) ?? GetById(productId);
            if (def == null) return 0;
            int custom = WholesaleDatabase.GetProductSalePrice(productId);
            return custom > 0 ? custom : def.SalePricePerUnit;
        }

        public static int GetQuickSellCratePrice(string productId) => GetQuickSellUnitPrice(productId) * PackSize;

        public static int GetMarketCratePrice(string productId) => GetMarketUnitPrice(productId) * PackSize;
    }
}
