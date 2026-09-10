using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Environment;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    public enum ProductOriginKind
    {
        Legacy = 0,
        FarmHarvest = 1,
        Livestock = 2,
        WorkshopCraft = 3,
        Wholesale = 4
    }

    [Serializable]
    public class ProductLot
    {
        public string productId;
        public int quantity;
        public ProductOriginKind originKind;
        public string originLabel;
        public string sourceProductId;
        public string sourceProductName;
        public string weatherId;
        public int harvestYear = 1;
        public int harvestSeason;
        public int harvestDay = 1;
        public int harvestHour;
        public int harvestMinute;
        public int packageIndex = -1;

        public ProductLot Clone(int newQuantity = -1)
        {
            return new ProductLot
            {
                productId = productId,
                quantity = newQuantity >= 0 ? newQuantity : quantity,
                originKind = originKind,
                originLabel = originLabel,
                sourceProductId = sourceProductId,
                sourceProductName = sourceProductName,
                weatherId = weatherId,
                harvestYear = harvestYear,
                harvestSeason = harvestSeason,
                harvestDay = harvestDay,
                harvestHour = harvestHour,
                harvestMinute = harvestMinute,
                packageIndex = packageIndex
            };
        }

        public bool IsSamePassport(ProductLot other)
        {
            if (other == null) return false;
            return originKind == other.originKind
                && harvestYear == other.harvestYear
                && harvestSeason == other.harvestSeason
                && harvestDay == other.harvestDay
                && harvestHour == other.harvestHour
                && string.Equals(productId, other.productId, StringComparison.Ordinal)
                && string.Equals(originLabel, other.originLabel, StringComparison.Ordinal)
                && string.Equals(sourceProductId, other.sourceProductId, StringComparison.Ordinal)
                && string.Equals(weatherId, other.weatherId, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Ürün pasaportu, FIFO lot taşıma ve tazelik fiyatı. Raftaki adet her zaman lot toplamına eşitlenir.
    /// </summary>
    public static class ProductPassportService
    {
        public const float SameDayFarmMultiplier = 1.20f;
        public const float NextDayFarmMultiplier = 1.10f;
        public const float FreshFarmMultiplier = 1.00f;
        public const float AgingFarmMultiplier = 0.90f;
        public const float StaleFarmMultiplier = 0.75f;
        public const float WholesaleMultiplier = 0.92f;
        public const int StaleDayThreshold = 6;

        public static int GetCalendarIndex(int year, int season, int day)
        {
            return (Mathf.Max(1, year) - 1) * 120 + Mathf.Clamp(season, 0, 3) * 30 + Mathf.Clamp(day, 1, 30);
        }

        public static int GetCurrentCalendarIndex()
        {
            TimeManager time = TimeManager.Instance;
            if (time == null) return GetCalendarIndex(1, 0, 1);
            return GetCalendarIndex(time.Year, (int)time.CurrentSeason, time.Day);
        }

        public static void CaptureHarvestClock(ProductLot lot)
        {
            if (lot == null) return;
            TimeManager time = TimeManager.Instance;
            if (time == null)
            {
                lot.harvestYear = 1;
                lot.harvestSeason = 0;
                lot.harvestDay = 1;
                lot.harvestHour = 6;
                lot.harvestMinute = 0;
                return;
            }

            lot.harvestYear = time.Year;
            lot.harvestSeason = (int)time.CurrentSeason;
            lot.harvestDay = time.Day;
            lot.harvestHour = time.CurrentHour;
            lot.harvestMinute = time.CurrentMinute;
        }

        public static string CaptureWeatherId()
        {
            if (WeatherManager.Instance == null) return WeatherType.Sunny.ToString();
            return WeatherManager.Instance.CurrentWeather.ToString();
        }

        public static int GetFreshnessDays(ProductLot lot)
        {
            if (lot == null) return 0;
            int grown = GetCurrentCalendarIndex() - GetCalendarIndex(lot.harvestYear, lot.harvestSeason, lot.harvestDay);
            return Mathf.Max(0, grown);
        }

        public static int GetDominantFreshnessDays(List<ProductLot> lots)
        {
            ProductLot dominant = GetDominantLot(lots);
            return dominant != null ? GetFreshnessDays(dominant) : 0;
        }

        public static float GetPriceMultiplier(ProductLot lot)
        {
            if (lot == null) return 1f;
            float freshness;
            if (lot.originKind == ProductOriginKind.Wholesale)
            {
                int wDays = GetFreshnessDays(lot);
                if (wDays >= StaleDayThreshold) freshness = 0.80f;
                else if (wDays >= 4) freshness = 0.86f;
                else freshness = WholesaleMultiplier;
            }
            else if (lot.originKind == ProductOriginKind.Legacy)
            {
                freshness = 1f;
            }
            else
            {
                int days = GetFreshnessDays(lot);
                if (days <= 0) freshness = SameDayFarmMultiplier;
                else if (days == 1) freshness = NextDayFarmMultiplier;
                else if (days <= 3) freshness = FreshFarmMultiplier;
                else if (days <= 5) freshness = AgingFarmMultiplier;
                else freshness = StaleFarmMultiplier;
            }

            float brand = StoreStatusManager.Instance != null
                ? StoreStatusManager.Instance.GetBrandPriceFactor(lot)
                : 1f;
            return freshness * brand;
        }

        public static bool IsStale(ProductLot lot)
        {
            if (lot == null) return false;
            if (lot.originKind == ProductOriginKind.Wholesale) return GetFreshnessDays(lot) >= StaleDayThreshold;
            if (lot.originKind == ProductOriginKind.Legacy) return false;
            return GetFreshnessDays(lot) >= StaleDayThreshold;
        }

        public static bool IsLocalOrigin(ProductLot lot)
        {
            if (lot == null) return false;
            return lot.originKind == ProductOriginKind.FarmHarvest
                || lot.originKind == ProductOriginKind.Livestock
                || lot.originKind == ProductOriginKind.WorkshopCraft;
        }

        public static ProductLot CreateHarvestLot(string productId, int quantity, FieldPlotController plot)
        {
            ProductLot lot = new ProductLot
            {
                productId = productId,
                quantity = Mathf.Max(0, quantity),
                originKind = ProductOriginKind.FarmHarvest,
                originLabel = GetPlotDisplayName(plot),
                weatherId = CaptureWeatherId()
            };
            CaptureHarvestClock(lot);
            return lot;
        }

        public static ProductLot CreateLivestockLot(string productId, int quantity, bool isChicken)
        {
            ProductLot lot = new ProductLot
            {
                productId = productId,
                quantity = Mathf.Max(0, quantity),
                originKind = ProductOriginKind.Livestock,
                originLabel = isChicken
                    ? LocalizationManager.L("Passport_Coop", "Kümes", "Henhouse")
                    : LocalizationManager.L("Passport_DairyBarn", "Süt Ahırı", "Dairy Barn"),
                weatherId = CaptureWeatherId()
            };
            CaptureHarvestClock(lot);
            return lot;
        }

        public static ProductLot CreateWorkshopLot(string outputProductId, int quantity, WorkshopRecipeDef recipe, List<ProductLot> consumedInputs)
        {
            ProductLot source = GetDominantLot(consumedInputs);
            WorkshopMachineDef machine = recipe != null ? WorkshopMachineDatabase.GetMachineByType(recipe.machineType) : null;
            string machineName = machine != null
                ? machine.LocalizedName
                : LocalizationManager.L("Passport_WorkshopMachine", "Atölye makinesi", "Workshop machine");

            string sourceName = source != null && !string.IsNullOrEmpty(source.sourceProductName)
                ? source.sourceProductName
                : ResolveProductDisplayName(recipe != null ? recipe.cropId : "");
            if (source != null && source.originKind == ProductOriginKind.FarmHarvest && !string.IsNullOrEmpty(source.originLabel))
            {
                sourceName = source.originLabel + " • " + sourceName;
            }

            ProductLot lot = new ProductLot
            {
                productId = outputProductId,
                quantity = Mathf.Max(0, quantity),
                originKind = ProductOriginKind.WorkshopCraft,
                originLabel = machineName,
                sourceProductId = recipe != null ? recipe.cropId : (source != null ? source.productId : ""),
                sourceProductName = sourceName,
                weatherId = source != null && !string.IsNullOrEmpty(source.weatherId) ? source.weatherId : CaptureWeatherId()
            };
            CaptureHarvestClock(lot);
            return lot;
        }

        public static ProductLot CreateWholesaleLot(string productId, int quantity)
        {
            ProductLot lot = new ProductLot
            {
                productId = productId,
                quantity = Mathf.Max(0, quantity),
                originKind = ProductOriginKind.Wholesale,
                originLabel = LocalizationManager.L("Passport_Wholesale", "Toptan tedarik", "Wholesale supply"),
                weatherId = CaptureWeatherId()
            };
            CaptureHarvestClock(lot);
            return lot;
        }

        public static ProductLot CreateLegacyLot(string productId, int quantity)
        {
            ProductLot lot = new ProductLot
            {
                productId = productId,
                quantity = Mathf.Max(0, quantity),
                originKind = ProductOriginKind.Legacy,
                originLabel = LocalizationManager.L("Passport_LegacyStock", "Kayıtlı stok", "Saved stock")
            };
            CaptureHarvestClock(lot);
            return lot;
        }

        public static string GetPlotDisplayName(FieldPlotController plot)
        {
            int index = 1;
            if (plot != null && FieldPlotController.AllPlots != null)
            {
                int found = FieldPlotController.AllPlots.IndexOf(plot);
                if (found >= 0) index = found + 1;
            }

            if (plot != null && !string.IsNullOrEmpty(plot.gameObject.name))
            {
                string name = plot.gameObject.name;
                int us = name.LastIndexOf('_');
                if (us >= 0 && us < name.Length - 1 && int.TryParse(name.Substring(us + 1), out int parsed) && parsed > 0)
                {
                    index = parsed;
                }
            }

            return string.Format(LocalizationManager.L("Passport_FieldFmt", "Tarla {0}", "Field {0}"), index);
        }

        public static string GetCanonicalShelfName(string productId, string fallbackName = "")
        {
            if (!string.IsNullOrEmpty(productId))
            {
                GardenSeedDef seed = GardenSeedDatabase.GetSeedById(productId);
                if (seed != null)
                {
                    return StripCropSuffix(seed.name);
                }

                WorkshopRecipeDef recipe = WorkshopMachineDatabase.GetRecipeByOutputId(productId);
                if (recipe != null) return recipe.outputNameTr;

                WholesaleProductDef live = LivestockProductDatabase.GetById(productId);
                if (live != null) return live.name;
            }

            return StripCropSuffix(fallbackName);
        }

        public static string StripCropSuffix(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            return raw
                .Replace(" Tohumu", "")
                .Replace(" Tohum", "")
                .Replace(" Seeds", "")
                .Replace(" Seed", "")
                .Trim();
        }

        public static bool RowHoldsProduct(ShelfRowData row, string productId, string productName)
        {
            if (row == null) return false;
            if (!string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(row.productId) && row.productId == productId) return true;
            if (!string.IsNullOrEmpty(productName) && row.productName == productName) return true;
            string canonical = GetCanonicalShelfName(productId, productName);
            if (!string.IsNullOrEmpty(canonical) && row.productName == canonical) return true;
            return false;
        }

        public static WholesaleProductDef CreateFarmCropDeliveryPack(GardenSeedDef seed, int packAmount, List<ProductLot> lots)
        {
            if (seed == null || packAmount <= 0) return null;
            string nameTr = StripCropSuffix(seed.name);
            string nameEn = StripCropSuffix(!string.IsNullOrEmpty(seed.nameEn) ? seed.nameEn : seed.name);
            WholesaleProductDef pack = new WholesaleProductDef(
                seed.id,
                nameTr,
                nameEn,
                seed.iconEmoji,
                FurnitureType.ProduceShelf,
                seed.requiredLevel,
                seed.unitSalePrice,
                packAmount,
                40f,
                false);
            AttachConsumedLotsToPack(pack, lots);
            return pack;
        }

        public static int SumLots(List<ProductLot> lots)
        {
            if (lots == null) return 0;
            int total = 0;
            for (int i = 0; i < lots.Count; i++)
            {
                if (lots[i] != null) total += Mathf.Max(0, lots[i].quantity);
            }
            return total;
        }

        public static ProductLot GetDominantLot(List<ProductLot> lots)
        {
            if (lots == null) return null;
            ProductLot best = null;
            for (int i = 0; i < lots.Count; i++)
            {
                ProductLot lot = lots[i];
                if (lot == null || lot.quantity <= 0) continue;
                if (best == null || lot.quantity > best.quantity) best = lot;
            }
            return best;
        }

        public static void MergeAdd(List<ProductLot> dest, ProductLot incoming)
        {
            if (dest == null || incoming == null || incoming.quantity <= 0) return;
            for (int i = 0; i < dest.Count; i++)
            {
                if (dest[i] != null && dest[i].IsSamePassport(incoming))
                {
                    dest[i].quantity += incoming.quantity;
                    return;
                }
            }
            dest.Add(incoming.Clone());
        }

        public static List<ProductLot> CloneLots(List<ProductLot> source)
        {
            List<ProductLot> copy = new List<ProductLot>();
            if (source == null) return copy;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null && source[i].quantity > 0) copy.Add(source[i].Clone());
            }
            return copy;
        }

        public static List<ProductLot> TakeFifo(List<ProductLot> source, int amount)
        {
            List<ProductLot> taken = new List<ProductLot>();
            if (source == null || amount <= 0) return taken;

            int remaining = amount;
            for (int i = 0; i < source.Count && remaining > 0;)
            {
                ProductLot lot = source[i];
                if (lot == null || lot.quantity <= 0)
                {
                    source.RemoveAt(i);
                    continue;
                }

                int slice = Mathf.Min(lot.quantity, remaining);
                taken.Add(lot.Clone(slice));
                lot.quantity -= slice;
                remaining -= slice;
                if (lot.quantity <= 0) source.RemoveAt(i);
                else i++;
            }

            return taken;
        }

        public static void Compact(List<ProductLot> lots)
        {
            if (lots == null) return;
            for (int i = lots.Count - 1; i >= 0; i--)
            {
                if (lots[i] == null || lots[i].quantity <= 0) lots.RemoveAt(i);
            }
        }

        public static void EnsureRowLots(ShelfRowData row)
        {
            if (row == null) return;
            if (row.lots == null) row.lots = new List<ProductLot>();
            Compact(row.lots);
            int lotSum = SumLots(row.lots);
            if (row.currentStock > lotSum)
            {
                MergeAdd(row.lots, CreateLegacyLot(row.productId, row.currentStock - lotSum));
            }
            else if (row.currentStock < lotSum)
            {
                TakeFifo(row.lots, lotSum - Mathf.Max(0, row.currentStock));
            }
            row.currentStock = SumLots(row.lots);
        }

        public static void ClearRowContents(ShelfRowData row)
        {
            if (row == null) return;
            row.currentStock = 0;
            if (row.lots == null) row.lots = new List<ProductLot>();
            else row.lots.Clear();
        }

        public static List<ProductLot> RemoveStock(ShelfRowData row, int amount)
        {
            EnsureRowLots(row);
            if (row == null || amount <= 0) return new List<ProductLot>();
            List<ProductLot> taken = TakeFifo(row.lots, Mathf.Min(amount, row.currentStock));
            row.currentStock = SumLots(row.lots);
            return taken;
        }

        public static int AddStock(ShelfRowData row, List<ProductLot> incoming, int maxCapacity)
        {
            if (row == null) return 0;
            EnsureRowLots(row);
            if (incoming == null || incoming.Count == 0) return 0;

            int space = Mathf.Max(0, maxCapacity - row.currentStock);
            if (space <= 0) return 0;

            List<ProductLot> accepted = TakeFifo(incoming, space);
            for (int i = 0; i < accepted.Count; i++) MergeAdd(row.lots, accepted[i]);
            row.currentStock = SumLots(row.lots);
            return SumLots(accepted);
        }

        public static void AddStockExact(ShelfRowData row, List<ProductLot> incoming)
        {
            if (row == null) return;
            EnsureRowLots(row);
            if (incoming == null) return;
            for (int i = 0; i < incoming.Count; i++) MergeAdd(row.lots, incoming[i]);
            incoming.Clear();
            row.currentStock = SumLots(row.lots);
        }

        public static int CountStaleUnitsOnShelves()
        {
            int stale = 0;
            var shelves = PlacedFurnitureController.AllPlacedFurniture;
            if (shelves == null) return 0;
            for (int s = 0; s < shelves.Count; s++)
            {
                var furniture = shelves[s];
                if (furniture == null || furniture.rows == null) continue;
                if (furniture.FurnitureType == FurnitureType.StorageShelf) continue;
                for (int r = 0; r < furniture.rows.Length; r++)
                {
                    ShelfRowData row = furniture.rows[r];
                    if (row == null || row.lots == null) continue;
                    for (int i = 0; i < row.lots.Count; i++)
                    {
                        ProductLot lot = row.lots[i];
                        if (lot != null && IsStale(lot)) stale += Mathf.Max(0, lot.quantity);
                    }
                }
            }
            return stale;
        }

        public static List<ProductLot> CollectStoreLots(string productId, string productName)
        {
            List<ProductLot> all = new List<ProductLot>();
            var shelves = PlacedFurnitureController.AllPlacedFurniture;
            if (shelves == null) return all;
            for (int s = 0; s < shelves.Count; s++)
            {
                var furniture = shelves[s];
                if (furniture == null || furniture.rows == null) continue;
                if (furniture.FurnitureType == FurnitureType.StorageShelf) continue;
                for (int r = 0; r < furniture.rows.Length; r++)
                {
                    ShelfRowData row = furniture.rows[r];
                    if (row == null || row.IsUnassigned || row.currentStock <= 0) continue;
                    bool idMatch = !string.IsNullOrEmpty(productId) && row.productId == productId;
                    bool nameMatch = !string.IsNullOrEmpty(productName) && row.productName == productName;
                    if (!idMatch && !nameMatch) continue;
                    EnsureRowLots(row);
                    all.AddRange(CloneLots(row.lots));
                }
            }
            return all;
        }

        public static string GetShelfTag(ShelfRowData row)
        {
            EnsureRowLots(row);
            if (row == null || row.currentStock <= 0 || row.IsUnassigned) return "";
            return GetLotsTag(row.lots);
        }

        public static string GetLotsTag(List<ProductLot> lots)
        {
            if (lots == null || SumLots(lots) <= 0) return "";
            bool hasWholesale = false;
            bool hasLocal = false;
            for (int i = 0; i < lots.Count; i++)
            {
                if (lots[i] == null || lots[i].quantity <= 0) continue;
                if (lots[i].originKind == ProductOriginKind.Wholesale) hasWholesale = true;
                if (IsLocalOrigin(lots[i])) hasLocal = true;
            }

            if (hasWholesale && hasLocal)
            {
                return LocalizationManager.L("Passport_TagMixed", "Karışık", "Mixed");
            }

            ProductLot dominant = GetDominantLot(lots);
            if (dominant == null) return "";
            if (dominant.originKind == ProductOriginKind.Wholesale)
            {
                return LocalizationManager.L("Passport_TagWholesale", "Toptan", "Wholesale");
            }

            int days = GetFreshnessDays(dominant);
            if (days <= 0) return LocalizationManager.L("Passport_TagToday", "Bugün hasat", "Harvested today");
            if (days == 1) return LocalizationManager.L("Passport_TagYesterday", "Dün hasat", "Harvested yesterday");
            return string.Format(LocalizationManager.L("Passport_TagDaysFmt", "{0} gün", "{0} days"), days);
        }

        public static Color GetTagColor(List<ProductLot> lots)
        {
            ProductLot dominant = GetDominantLot(lots);
            if (dominant == null) return new Color(0.85f, 0.90f, 0.95f);
            if (dominant.originKind == ProductOriginKind.Wholesale) return new Color(0.95f, 0.72f, 0.28f);
            int days = GetFreshnessDays(dominant);
            if (days <= 0) return new Color(0.35f, 0.92f, 0.48f);
            if (days == 1) return new Color(0.55f, 0.88f, 0.40f);
            if (days <= 3) return new Color(0.90f, 0.90f, 0.40f);
            if (days <= 5) return new Color(0.95f, 0.70f, 0.28f);
            return new Color(0.95f, 0.40f, 0.32f);
        }

        public static string GetCardText(ShelfRowData row)
        {
            if (row == null) return "";
            EnsureRowLots(row);
            string product = string.IsNullOrEmpty(row.productName) ? ResolveProductDisplayName(row.productId) : row.productName;
            return GetCardText(product, row.lots);
        }

        public static string GetCardText(string productName, List<ProductLot> lots)
        {
            ProductLot lot = GetDominantLot(lots);
            if (lot == null)
            {
                return productName ?? "";
            }

            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(productName)) parts.Add(productName);

            if (lot.originKind == ProductOriginKind.FarmHarvest && !string.IsNullOrEmpty(lot.originLabel))
            {
                parts.Add(lot.originLabel);
            }
            else if (lot.originKind == ProductOriginKind.Livestock && !string.IsNullOrEmpty(lot.originLabel))
            {
                parts.Add(lot.originLabel);
            }
            else if (lot.originKind == ProductOriginKind.WorkshopCraft && !string.IsNullOrEmpty(lot.originLabel))
            {
                parts.Add(lot.originLabel);
            }
            else if (lot.originKind == ProductOriginKind.Wholesale)
            {
                parts.Add(LocalizationManager.L("Passport_TagWholesale", "Toptan", "Wholesale"));
            }

            parts.Add(FormatHarvestDate(lot));

            string weather = LocalizeWeather(lot.weatherId);
            if (!string.IsNullOrEmpty(weather) && lot.originKind != ProductOriginKind.Wholesale && lot.originKind != ProductOriginKind.Legacy)
            {
                parts.Add(weather);
            }

            if (lot.originKind == ProductOriginKind.WorkshopCraft && !string.IsNullOrEmpty(lot.sourceProductName))
            {
                parts.Add(lot.sourceProductName);
            }

            parts.Add(string.Format(
                LocalizationManager.L("Passport_ShelfAgeFmt", "Raf: {0} gün", "Shelf: {0} days"),
                GetFreshnessDays(lot)));

            return string.Join(" • ", parts);
        }

        public static string FormatHarvestDate(ProductLot lot)
        {
            if (lot == null) return "";
            TimeManager.Season season = (TimeManager.Season)Mathf.Clamp(lot.harvestSeason, 0, 3);
            string seasonName = TimeManager.Instance != null
                ? TimeManager.Instance.GetLocalizedSeasonName(season)
                : season.ToString();
            return string.Format(
                LocalizationManager.L("Passport_DateFmt", "{0} Gün {1}", "{0} Day {1}"),
                seasonName,
                lot.harvestDay);
        }

        public static string LocalizeWeather(string weatherId)
        {
            if (string.Equals(weatherId, WeatherType.Rainy.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return LocalizationManager.L("Weather_Rainy", "Yağmurlu", "Rainy");
            }
            if (string.Equals(weatherId, WeatherType.Snowy.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return LocalizationManager.L("Weather_Snowy", "Karlı", "Snowy");
            }
            if (string.Equals(weatherId, WeatherType.Sunny.ToString(), StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(weatherId))
            {
                return LocalizationManager.L("Weather_Sunny", "Güneşli", "Sunny");
            }
            return weatherId;
        }

        public static string ResolveProductDisplayName(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return LocalizationManager.L("Label_Product", "Ürün", "Product");
            GardenSeedDef seed = GardenSeedDatabase.GetSeedById(productId);
            if (seed != null)
            {
                return seed.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "");
            }

            WorkshopRecipeDef recipe = WorkshopMachineDatabase.GetRecipeByOutputId(productId);
            if (recipe != null) return recipe.LocalizedName;

            WholesaleProductDef live = LivestockProductDatabase.GetById(productId);
            if (live != null) return live.LocalizedName;

            WholesaleProductDef wholesale = WholesaleDatabase.GetProductById(productId);
            if (wholesale != null) return wholesale.LocalizedName;
            return productId;
        }

        public static int PriceForLots(int baseUnitPrice, List<ProductLot> soldLots)
        {
            if (soldLots == null || soldLots.Count == 0) return Mathf.Max(1, baseUnitPrice);
            int total = 0;
            int qty = 0;
            for (int i = 0; i < soldLots.Count; i++)
            {
                ProductLot lot = soldLots[i];
                if (lot == null || lot.quantity <= 0) continue;
                int unit = Mathf.Max(1, Mathf.RoundToInt(baseUnitPrice * GetPriceMultiplier(lot)));
                total += unit * lot.quantity;
                qty += lot.quantity;
            }
            if (qty <= 0) return Mathf.Max(1, baseUnitPrice);
            return total;
        }

        public static string GetReceiptOriginLabel(int localCount, int wholesaleCount)
        {
            if (localCount > 0 && wholesaleCount > 0)
            {
                return LocalizationManager.L("Passport_ReceiptMixed", "Yerel hasat + Toptan", "Local harvest + Wholesale");
            }
            if (localCount > 0)
            {
                return LocalizationManager.L("Passport_ReceiptLocal", "Yerel hasat", "Local harvest");
            }
            if (wholesaleCount > 0)
            {
                return LocalizationManager.L("Passport_ReceiptWholesale", "Toptan", "Wholesale");
            }
            return LocalizationManager.L("Payment_Success", "Ödeme Yapıldı", "Payment Completed");
        }

        public static WholesaleProductDef CreateTransitStub(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            WholesaleProductDef known = WholesaleDatabase.GetProductById(productId);
            if (known != null) return CloneForTransit(known);

            WholesaleProductDef live = LivestockProductDatabase.GetById(productId);
            if (live != null) return CloneForTransit(live);

            GardenSeedDef seed = GardenSeedDatabase.GetSeedById(productId);
            if (seed != null)
            {
                return new WholesaleProductDef(
                    seed.id,
                    seed.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", ""),
                    seed.iconEmoji,
                    FurnitureType.ProduceShelf,
                    1,
                    seed.unitSalePrice,
                    50,
                    40f,
                    false);
            }

            WorkshopRecipeDef recipe = WorkshopMachineDatabase.GetRecipeByOutputId(productId);
            if (recipe != null)
            {
                return new WholesaleProductDef(
                    recipe.outputProductId,
                    recipe.outputNameTr,
                    recipe.outputNameEn,
                    recipe.iconEmoji,
                    FurnitureType.GourmetShelf,
                    1,
                    Mathf.Max(1, Mathf.RoundToInt(recipe.unitSalePrice / 1.80f)),
                    50,
                    80f,
                    false);
            }

            return new WholesaleProductDef(productId, productId, productId, "📦", FurnitureType.Shelf, 1, 10, 50, 20f, false);
        }

        public static WholesaleProductDef CloneForTransit(WholesaleProductDef source)
        {
            if (source == null) return null;
            WholesaleProductDef clone = new WholesaleProductDef(
                source.id,
                source.name,
                source.nameEn,
                source.iconEmoji,
                source.targetShelfType,
                source.requiredLevel,
                source.wholesaleUnitPrice,
                source.packQuantity,
                source.profitMarginPercent,
                source.isOrderable);
            clone.attachedLots = CloneLots(source.attachedLots);
            return clone;
        }

        public static List<WholesaleProductDef> PrepareWholesaleTransit(List<WholesaleProductDef> orderList)
        {
            List<WholesaleProductDef> prepared = new List<WholesaleProductDef>();
            if (orderList == null) return prepared;
            for (int i = 0; i < orderList.Count; i++)
            {
                WholesaleProductDef clone = CloneForTransit(orderList[i]);
                if (clone == null) continue;
                if (clone.attachedLots == null || SumLots(clone.attachedLots) <= 0)
                {
                    clone.attachedLots = new List<ProductLot> { CreateWholesaleLot(clone.id, clone.packQuantity) };
                }
                prepared.Add(clone);
            }
            return prepared;
        }

        public static void AttachConsumedLotsToPack(WholesaleProductDef pack, List<ProductLot> lots)
        {
            if (pack == null) return;
            pack.attachedLots = lots != null ? CloneLots(lots) : new List<ProductLot>();
            int sum = SumLots(pack.attachedLots);
            if (sum < pack.packQuantity)
            {
                MergeAdd(pack.attachedLots, CreateLegacyLot(pack.id, pack.packQuantity - sum));
            }
            else if (sum > pack.packQuantity)
            {
                TakeFifo(pack.attachedLots, sum - pack.packQuantity);
            }
        }

        public static List<ProductLot> TakeAttachedLots(WholesaleProductDef pack, int amount)
        {
            if (pack == null || amount <= 0) return new List<ProductLot>();
            if (pack.attachedLots == null || SumLots(pack.attachedLots) <= 0)
            {
                ProductLot fallback = pack.isOrderable
                    ? CreateWholesaleLot(pack.id, amount)
                    : CreateLegacyLot(pack.id, amount);
                return new List<ProductLot> { fallback };
            }

            return TakeFifo(pack.attachedLots, amount);
        }

        public static void CapturePackageLots(DeliveryTruckSaveData data, IReadOnlyList<WholesaleProductDef> remaining, List<WholesaleProductDef> original)
        {
            if (data == null) return;
            data.remainingPackageLots = FlattenPackageLots(remaining);
            data.originalPackageLots = FlattenPackageLots(original);
        }

        public static List<ProductLot> FlattenPackageLots(IReadOnlyList<WholesaleProductDef> packages)
        {
            List<ProductLot> flat = new List<ProductLot>();
            if (packages == null) return flat;
            for (int i = 0; i < packages.Count; i++)
            {
                WholesaleProductDef pack = packages[i];
                if (pack == null) continue;
                List<ProductLot> lots = pack.attachedLots;
                if (lots == null || lots.Count == 0)
                {
                    ProductLot synthetic = pack.isOrderable ? CreateWholesaleLot(pack.id, pack.packQuantity) : CreateLegacyLot(pack.id, pack.packQuantity);
                    synthetic.packageIndex = i;
                    flat.Add(synthetic);
                    continue;
                }

                for (int l = 0; l < lots.Count; l++)
                {
                    if (lots[l] == null || lots[l].quantity <= 0) continue;
                    ProductLot copy = lots[l].Clone();
                    copy.packageIndex = i;
                    flat.Add(copy);
                }
            }
            return flat;
        }

        public static void ApplyPackageLots(List<WholesaleProductDef> packages, List<ProductLot> flatLots)
        {
            if (packages == null) return;
            for (int i = 0; i < packages.Count; i++)
            {
                if (packages[i] == null) continue;
                packages[i] = CloneForTransit(packages[i]);
                packages[i].attachedLots = new List<ProductLot>();
            }

            if (flatLots != null)
            {
                for (int i = 0; i < flatLots.Count; i++)
                {
                    ProductLot lot = flatLots[i];
                    if (lot == null || lot.packageIndex < 0 || lot.packageIndex >= packages.Count) continue;
                    if (packages[lot.packageIndex] == null) continue;
                    MergeAdd(packages[lot.packageIndex].attachedLots, lot.Clone());
                }
            }

            for (int i = 0; i < packages.Count; i++)
            {
                WholesaleProductDef pack = packages[i];
                if (pack == null) continue;
                if (SumLots(pack.attachedLots) <= 0)
                {
                    pack.attachedLots = new List<ProductLot>
                    {
                        pack.isOrderable ? CreateWholesaleLot(pack.id, pack.packQuantity) : CreateLegacyLot(pack.id, pack.packQuantity)
                    };
                }
            }
        }

        public static List<ProductLot> RestoreLotsOrLegacy(string productId, int count, List<ProductLot> savedLots)
        {
            List<ProductLot> lots = new List<ProductLot>();
            if (savedLots != null)
            {
                for (int i = 0; i < savedLots.Count; i++)
                {
                    if (savedLots[i] != null && savedLots[i].quantity > 0) lots.Add(savedLots[i].Clone());
                }
            }

            int sum = SumLots(lots);
            if (count > sum) MergeAdd(lots, CreateLegacyLot(productId, count - sum));
            else if (count < sum) TakeFifo(lots, sum - count);
            return lots;
        }

        public static void ShowPassportModal(string title, string body)
        {
            ModalManager.ShowModal(
                title,
                body,
                LocalizationManager.L("Btn_Ok", "Tamam", "OK"));
        }
    }

    public class WorldLabelBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            transform.rotation = cam.transform.rotation;
        }
    }
}
