using UnityEngine;
using System;
using Farm2Shelf.UI;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    public enum BrandIdentity
    {
        LocalProducer = 0,
        NeighborhoodMarket = 1,
        GourmetWorkshop = 2
    }

    /// <summary>
    /// Farm2Shelf Dükkan Açık / Kapalı Durum Yöneticisi.
    /// </summary>
    public class StoreStatusManager : MonoBehaviour
    {
        public const int SloganMaxChars = 32;

        public static StoreStatusManager Instance { get; private set; }

        [Header("Dükkan Durumu")]
        [SerializeField] private bool isOpen = false;

        public string PlayerName { get; private set; } = "Çiftçi Ali";
        public string CompanyName { get; private set; } = "Farm2Shelf Market";
        public BrandIdentity Identity { get; private set; } = BrandIdentity.LocalProducer;
        public Color BrandColor { get; private set; } = new Color(0.22f, 0.55f, 0.28f, 1f);
        public string Slogan { get; private set; } = "";

        public event Action<bool> OnStoreStatusChanged;
        public event Action<string> OnCompanyNameChanged;
        public event Action OnBrandChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            if (string.IsNullOrWhiteSpace(Slogan))
            {
                Slogan = GetDefaultSlogan(Identity);
            }
        }

        public void SetPlayerAndCompany(string playerName, string companyName)
        {
            if (!string.IsNullOrWhiteSpace(playerName)) PlayerName = playerName.Trim();
            if (!string.IsNullOrWhiteSpace(companyName))
            {
                CompanyName = companyName.Trim();
                OnCompanyNameChanged?.Invoke(CompanyName);
            }
            Debug.Log($"[Farm2Shelf] Yeni Oyuncu Kurulumu: {PlayerName} | {CompanyName}");
        }

        public void SetBrand(BrandIdentity identity, Color color, string slogan)
        {
            Identity = identity;
            BrandColor = SanitizeBrandColor(identity, color);
            Slogan = SanitizeSlogan(identity, slogan);
            NotifyBrandChanged();
        }

        public void RestoreBrand(int identityValue, Color color, string slogan)
        {
            BrandIdentity parsed = BrandIdentity.LocalProducer;
            if (Enum.IsDefined(typeof(BrandIdentity), identityValue))
            {
                parsed = (BrandIdentity)identityValue;
            }

            SetBrand(parsed, color, slogan);
        }

        public static Color GetDefaultBrandColor(BrandIdentity identity)
        {
            switch (identity)
            {
                case BrandIdentity.NeighborhoodMarket:
                    return new Color(0.18f, 0.48f, 0.78f, 1f);
                case BrandIdentity.GourmetWorkshop:
                    return new Color(0.62f, 0.22f, 0.28f, 1f);
                default:
                    return new Color(0.22f, 0.55f, 0.28f, 1f);
            }
        }

        public static string GetDefaultSlogan(BrandIdentity identity)
        {
            switch (identity)
            {
                case BrandIdentity.NeighborhoodMarket:
                    return LocalizationManager.L("Brand_Slogan_Neighborhood", "Her gün uygun fiyat", "Fair prices every day");
                case BrandIdentity.GourmetWorkshop:
                    return LocalizationManager.L("Brand_Slogan_Gourmet", "Atölyeden taze lezzet", "Crafted fresh in-house");
                default:
                    return LocalizationManager.L("Brand_Slogan_Local", "Tarladan sofraya", "From field to table");
            }
        }

        public static string GetIdentityDisplayName(BrandIdentity identity)
        {
            switch (identity)
            {
                case BrandIdentity.NeighborhoodMarket:
                    return LocalizationManager.L("Brand_Identity_Neighborhood", "Ekonomik mahalle marketi", "Neighborhood value market");
                case BrandIdentity.GourmetWorkshop:
                    return LocalizationManager.L("Brand_Identity_Gourmet", "Gurme atölye", "Gourmet workshop");
                default:
                    return LocalizationManager.L("Brand_Identity_Local", "Yerel üretici", "Local producer");
            }
        }

        public static string GetIdentityHint(BrandIdentity identity)
        {
            switch (identity)
            {
                case BrandIdentity.NeighborhoodMarket:
                    return LocalizationManager.L("Brand_Hint_Neighborhood", "Toptan ürün daha kabul görür; fiyat hassas müşteri.", "Wholesale sells easier; shoppers are price-sensitive.");
                case BrandIdentity.GourmetWorkshop:
                    return LocalizationManager.L("Brand_Hint_Gourmet", "İşlenmiş ve gurme üründe prim; toptan daha zayıf.", "Crafted & gourmet earn a premium; wholesale is weaker.");
                default:
                    return LocalizationManager.L("Brand_Hint_Local", "Taze hasat ve pasaportlu üründe prim.", "Fresh harvest and passport lots earn a premium.");
            }
        }

        public string GetResolvedSlogan()
        {
            return string.IsNullOrWhiteSpace(Slogan) ? GetDefaultSlogan(Identity) : Slogan;
        }

        public Color GetBrandWashColor()
        {
            return Color.Lerp(BrandColor, Color.white, 0.38f);
        }

        public Color GetBrandInteriorLightColor()
        {
            return Color.Lerp(new Color(1f, 0.96f, 0.88f), BrandColor, 0.32f);
        }

        public float GetBrandPriceFactor(ProductLot lot)
        {
            if (lot == null) return 1f;

            switch (Identity)
            {
                case BrandIdentity.NeighborhoodMarket:
                    if (lot.originKind == ProductOriginKind.Wholesale) return 1.04f;
                    if (ProductPassportService.IsLocalOrigin(lot)) return 0.98f;
                    return 1f;
                case BrandIdentity.GourmetWorkshop:
                    if (lot.originKind == ProductOriginKind.WorkshopCraft) return 1.14f;
                    if (lot.originKind == ProductOriginKind.FarmHarvest || lot.originKind == ProductOriginKind.Livestock) return 1.06f;
                    if (lot.originKind == ProductOriginKind.Wholesale) return 0.90f;
                    return 1f;
                default:
                    if (ProductPassportService.IsLocalOrigin(lot)) return 1.10f;
                    if (lot.originKind == ProductOriginKind.Wholesale) return 0.92f;
                    return 1f;
            }
        }

        public float GetOverpriceSkipChance(bool isFarmOrLocalProduct, bool isGourmetProduct)
        {
            switch (Identity)
            {
                case BrandIdentity.NeighborhoodMarket:
                    return 0.84f;
                case BrandIdentity.GourmetWorkshop:
                    return isGourmetProduct ? 0.48f : 0.74f;
                default:
                    return isFarmOrLocalProduct ? 0.52f : 0.78f;
            }
        }

        public float GetFarmOverpriceTolerance()
        {
            switch (Identity)
            {
                case BrandIdentity.NeighborhoodMarket:
                    return 1.18f;
                case BrandIdentity.GourmetWorkshop:
                    return 1.40f;
                default:
                    return 1.50f;
            }
        }

        private static Color SanitizeBrandColor(BrandIdentity identity, Color color)
        {
            float mag = color.r + color.g + color.b;
            if (mag < 0.08f) return GetDefaultBrandColor(identity);
            color.a = 1f;
            return color;
        }

        private static string SanitizeSlogan(BrandIdentity identity, string slogan)
        {
            string trimmed = slogan != null ? slogan.Trim() : "";
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return GetDefaultSlogan(identity);
            }

            if (trimmed.Length > SloganMaxChars)
            {
                trimmed = trimmed.Substring(0, SloganMaxChars);
            }

            return trimmed;
        }

        private void NotifyBrandChanged()
        {
            OnBrandChanged?.Invoke();
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.ApplyPlayerInteriorBrandTint(GetBrandInteriorLightColor());
            }

            if (StorefrontSignboardController.Instance != null)
            {
                StorefrontSignboardController.Instance.RefreshSignboard();
            }
        }

        public bool SetStoreStatus(bool status)
        {
            if (status && !isOpen)
            {
                if (TimeManager.Instance != null && TimeManager.Instance.Hour >= 24)
                {
                    ModalManager.ShowModal(
                        LocalizationManager.L("Store_MidnightLocked_Title", "Dükkan Açılamaz 🌙", "Store Cannot Open 🌙"),
                        LocalizationManager.L("Store_MidnightLocked_Body", "Saat 24:00 oldu. Dükkan tekrar açılamaz.\n\nGün Sonu Z Raporunda 'Ertesi Güne Atla' butonuna basarak sabah 06:00'ya geçmeniz gerekir.", "It is 24:00. The store cannot be reopened.\n\nPress 'Skip to Next Day' on the End of Day Z-Report to continue at 06:00 AM."),
                        LocalizationManager.L("Btn_OK", "Tamam", "OK")
                    );
                    return false;
                }

                // Dükkan AÇILMAYA çalışılıyor: Önceki vardiyadan ayrılan personeller henüz tamamen çıkış yapmadıysa engelle!
                if (StaffTaskController.Instance != null && StaffTaskController.Instance.HasLeavingStaff())
                {
                    bool isEnglish = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;

                    string title = isEnglish ? "Shift Exit In Progress! ⏳" : "Vardiya Bitiş Süreci Tamamlanmadı! ⏳";
                    string msg = isEnglish ?
                        "Staff members from the previous shift have not fully left the premises yet.\n\nPlease wait for staff members to reach the exit point and leave before opening the store again." :
                        "Önceki vardiyadan ayrılan personeller henüz dükkandan ve alandan tamamen çıkış yapmadı.\n\nLütfen personellerin çıkış noktasına ulaşıp ayrılmasını bekleyin ve ardından dükkanı tekrar açın.";
                    string btnText = isEnglish ? "OK" : "Tamam";

                    ModalManager.ShowModal(title, msg, btnText);
                    return false;
                }
            }

            if (isOpen != status)
            {
                isOpen = status;
                Debug.Log($"[Farm2Shelf] Dükkan Durumu: {(isOpen ? "AÇIK" : "KAPALI")}");
                if (isOpen && TimeManager.Instance != null)
                {
                    TimeManager.Instance.StartDayTimeFlow();
                }
                OnStoreStatusChanged?.Invoke(isOpen);
            }

            return true;
        }

        public void ToggleStoreStatus()
        {
            SetStoreStatus(!isOpen);
        }

        public void OpenStore() => SetStoreStatus(true);
        public void CloseStore() => SetStoreStatus(false);

        public void RestoreStoreStatus(bool status)
        {
            if (isOpen == status) return;
            isOpen = status;
            OnStoreStatusChanged?.Invoke(isOpen);
        }

        public bool IsOpen => isOpen;
        public bool IsStoreOpen => isOpen;
    }
}
