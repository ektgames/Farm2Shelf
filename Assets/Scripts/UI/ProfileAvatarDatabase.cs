using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Müşteri ve Personeller için stüdyo kalitesinde gerçekçi vesikalık fotoğrafları
    /// yöneten, personel ve müşteri havuzlarını %100 birbirinden izole eden Avatar Veritabanı.
    /// </summary>
    public static class ProfileAvatarDatabase
    {
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        // ==========================================
        // 1. PERSONEL (STAFF) ÖZEL ÜNİFORMALI FOTOĞRAF HAVUZU
        // ==========================================
        private static readonly HashSet<string> staffAvatarKeys = new HashSet<string>()
        {
            "avatar_female_cashier",
            "avatar_male_cashier",
            "avatar_female_worker",
            "avatar_male_worker",
            "avatar_female_cleaner",
            "avatar_male_cleaner",
            "avatar_female_security",
            "avatar_male_security",
            "avatar_female_service",
            "avatar_male_service",
            "avatar_female_farmer",
            "avatar_male_farmer"
        };

        // ==========================================
        // 2. MÜŞTERİ (CUSTOMER) SİVİL FOTOĞRAF HAVUZLARI (ASLA PERSONEL ÇIKMAZ)
        // ==========================================
        private static readonly string[] femaleCustomerYoung = new string[]
        {
            "avatar_female_young_1",
            "avatar_female_young_2",
            "avatar_female_bookworm"
        };

        private static readonly string[] femaleCustomerAdult = new string[]
        {
            "avatar_female_artist",
            "avatar_female_barista",
            "avatar_female_business",
            "avatar_female_doctor",
            "avatar_female_homemaker",
            "avatar_female_supermodel",
            "avatar_female_vet",
            "avatar_female_yoga",
            "avatar_female_collector"
        };

        private static readonly string[] femaleCustomerSenior = new string[]
        {
            "avatar_female_senior",
            "avatar_female_homemaker"
        };

        private static readonly string[] maleCustomerYoung = new string[]
        {
            "avatar_male_young_1",
            "avatar_male_young_2",
            "avatar_male_musician"
        };

        private static readonly string[] maleCustomerAdult = new string[]
        {
            "avatar_male_architect",
            "avatar_male_athletic",
            "avatar_male_baker",
            "avatar_male_business",
            "avatar_male_chef",
            "avatar_male_critic",
            "avatar_male_fisherman",
            "avatar_male_gardener",
            "avatar_male_investor",
            "avatar_male_opera",
            "avatar_male_postman"
        };

        private static readonly string[] maleCustomerSenior = new string[]
        {
            "avatar_male_senior",
            "avatar_male_gardener"
        };

        /// <summary>
        /// Müşteri profiline en uygun SİVİL vesikalık fotoğrafı getirir (Personel fotoğrafı çıkmaz).
        /// </summary>
        public static Sprite GetCustomerAvatar(CustomerProfileData profile)
        {
            if (profile == null) return GetDefaultCustomerAvatar(false);

            string avatarKey = GetAvatarKeyForCustomer(profile.customerType, profile.isFemale, profile.age, profile.fullName);

            // Güvenlik Kilidi: Eğer bir şekilde personel fotoğraf anahtarı seçilirse derhal müşteri havuzuna yönlendir
            if (staffAvatarKeys.Contains(avatarKey))
            {
                avatarKey = GetDiverseCustomerAvatar(profile.isFemale, profile.age, Mathf.Abs((profile.fullName ?? "Cust").GetHashCode()));
            }

            return GetAvatarSprite(avatarKey);
        }

        /// <summary>
        /// Personel profiline en uygun ÜNİFORMALI vesikalık fotoğrafı getirir (Müşteri fotoğrafı çıkmaz).
        /// </summary>
        public static Sprite GetStaffAvatar(StaffMember staff)
        {
            if (staff == null) return GetDefaultStaffAvatar(false);

            bool isFemale = staff.isFemale || StaffManager.IsFemaleName(staff.name);
            string avatarKey = GetAvatarKeyForStaff(staff.role, isFemale, staff.name);

            // Güvenlik Kilidi: Personel her zaman üniformalı personel havuzundan fotoğraf alır
            if (!staffAvatarKeys.Contains(avatarKey))
            {
                avatarKey = isFemale ? "avatar_female_worker" : "avatar_male_worker";
            }

            return GetAvatarSprite(avatarKey);
        }

        /// <summary>
        /// Müşteri tipine, yaşına ve cinsiyetine göre SADECE müşteri fotoğraf havuzundan seçim yapar.
        /// </summary>
        private static string GetAvatarKeyForCustomer(CustomerType type, bool isFemale, int age, string nameSeed)
        {
            int seed = Mathf.Abs((nameSeed ?? type.ToString()).GetHashCode());

            switch (type)
            {
                case CustomerType.L1_CasualBoy:
                    return (seed % 2 == 0) ? "avatar_male_young_1" : "avatar_male_young_2";

                case CustomerType.L1_StudentGirl:
                    return (seed % 2 == 0) ? "avatar_female_young_1" : "avatar_female_young_2";

                case CustomerType.L1_FarmerUncle:
                    return "avatar_male_gardener";

                case CustomerType.L1_VillageGirl:
                    return "avatar_female_homemaker";

                case CustomerType.L1_GrandpaDede:
                    return "avatar_male_senior";

                case CustomerType.L1_GrandmaTeyze:
                    return "avatar_female_senior";

                case CustomerType.L1_BakeryCustomer:
                    return "avatar_male_baker";

                case CustomerType.L1_Workman:
                    return "avatar_male_athletic";

                case CustomerType.L1_SportsMan:
                    return "avatar_male_athletic";

                case CustomerType.L1_NeighborhoodMom:
                    return "avatar_female_homemaker";

                case CustomerType.L1_GardenerGrandpa:
                    return "avatar_male_gardener";

                case CustomerType.L1_BookwormGirl:
                    return "avatar_female_bookworm";

                case CustomerType.L1_MusicianGuy:
                    return "avatar_male_musician";

                case CustomerType.L1_PostmanUncle:
                    return "avatar_male_postman";

                case CustomerType.L1_Fisherman:
                    return "avatar_male_fisherman";

                case CustomerType.L2_OfficeWorker:
                    return isFemale ? "avatar_female_business" : "avatar_male_business";

                case CustomerType.L2_HipsterGuy:
                    return "avatar_male_young_2";

                case CustomerType.L2_FashionWoman:
                case CustomerType.L3_BoutiqueLady:
                    return (seed % 2 == 0) ? "avatar_female_business" : "avatar_female_artist";

                case CustomerType.L2_DeliveryCourier:
                    return isFemale ? "avatar_female_young_1" : "avatar_male_postman";

                case CustomerType.L2_BusinessWoman:
                    return "avatar_female_business";

                case CustomerType.L2_GymBro:
                    return "avatar_male_athletic";

                case CustomerType.L2_ArtistGirl:
                    return "avatar_female_artist";

                case CustomerType.L3_VIP_Influencer:
                    return "avatar_female_supermodel";

                case CustomerType.L2_DoctorWoman:
                    return "avatar_female_doctor";

                case CustomerType.L2_ChefMaster:
                    return "avatar_male_chef";

                case CustomerType.L2_YogaInstructor:
                    return "avatar_female_yoga";

                case CustomerType.L2_ArchitectGuy:
                    return "avatar_male_architect";

                case CustomerType.L2_BaristaGirl:
                    return "avatar_female_barista";

                case CustomerType.L2_Veterinarian:
                    return "avatar_female_vet";

                case CustomerType.L3_JewelryLady:
                    return "avatar_female_business";

                case CustomerType.L3_GourmetCritic:
                    return "avatar_male_critic";

                case CustomerType.L3_Supermodel:
                    return "avatar_female_supermodel";

                case CustomerType.L3_TechInvestor:
                    return "avatar_male_investor";

                case CustomerType.L3_OperaSinger:
                    return "avatar_male_opera";

                case CustomerType.L3_LuxuryCollector:
                    return "avatar_female_collector";

                default:
                    return GetDiverseCustomerAvatar(isFemale, age, seed);
            }
        }

        /// <summary>
        /// Personel mesleğine ve cinsiyetine göre SADECE üniformalı personel havuzundan seçim yapar.
        /// </summary>
        public static Sprite GetStaffAvatarSprite(StaffRole role, bool isFemale, string nameSeed = "")
        {
            string key = GetAvatarKeyForStaff(role, isFemale, nameSeed);
            return GetAvatarSprite(key);
        }

        private static string GetAvatarKeyForStaff(StaffRole role, bool isFemale, string nameSeed)
        {
            switch (role)
            {
                case StaffRole.Kasiyer:
                    return isFemale ? "avatar_female_cashier" : "avatar_male_cashier";

                case StaffRole.Reyoncu:
                    return isFemale ? "avatar_female_worker" : "avatar_male_worker";

                case StaffRole.Temizlikçi:
                    return isFemale ? "avatar_female_cleaner" : "avatar_male_cleaner";

                case StaffRole.Güvenlik:
                    return isFemale ? "avatar_female_security" : "avatar_male_security";

                case StaffRole.MüşteriHizmetlisi:
                case StaffRole.Maskot:
                case StaffRole.Kurye:
                    return isFemale ? "avatar_female_service" : "avatar_male_service";

                case StaffRole.Çiftçi:
                case StaffRole.DeneyimliÇiftçi:
                case StaffRole.UstaÇiftlikSorumlusu:
                case StaffRole.TarımOtomasyonUzmanı:
                    return isFemale ? "avatar_female_farmer" : "avatar_male_farmer";

                default:
                    return isFemale ? "avatar_female_worker" : "avatar_male_worker";
            }
        }

        private static string GetDiverseCustomerAvatar(bool isFemale, int age, int seed)
        {
            if (isFemale)
            {
                if (age <= 24) return femaleCustomerYoung[seed % femaleCustomerYoung.Length];
                if (age >= 60) return femaleCustomerSenior[seed % femaleCustomerSenior.Length];
                return femaleCustomerAdult[seed % femaleCustomerAdult.Length];
            }
            else
            {
                if (age <= 25) return maleCustomerYoung[seed % maleCustomerYoung.Length];
                if (age >= 60) return maleCustomerSenior[seed % maleCustomerSenior.Length];
                return maleCustomerAdult[seed % maleCustomerAdult.Length];
            }
        }

        /// <summary>
        /// İstenen isimdeki avatar Sprite'ını önbellekten veya diskten yükler.
        /// </summary>
        public static Sprite GetAvatarSprite(string avatarName)
        {
            if (string.IsNullOrEmpty(avatarName)) return GetDefaultCustomerAvatar(false);

            if (spriteCache.TryGetValue(avatarName, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            // 1. Resources.Load<Sprite> Denemesi
            Sprite resSprite = Resources.Load<Sprite>("Avatars/" + avatarName);
            if (resSprite != null)
            {
                spriteCache[avatarName] = resSprite;
                return resSprite;
            }

            // 2. Resources.Load<Texture2D> Denemesi
            Texture2D resTex = Resources.Load<Texture2D>("Avatars/" + avatarName);
            if (resTex != null)
            {
                Sprite created = Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f), 100f);
                created.name = avatarName;
                spriteCache[avatarName] = created;
                return created;
            }

            // 3. Application.dataPath üzerinden doğrudan dosya okuma (Editor & Dev Fallback)
            try
            {
                string[] extensions = new string[] { ".jpg", ".png", ".jpeg" };
                string resourcesDir = Path.Combine(Application.dataPath, "Resources", "Avatars");

                foreach (var ext in extensions)
                {
                    string fullPath = Path.Combine(resourcesDir, avatarName + ext);
                    if (File.Exists(fullPath))
                    {
                        byte[] fileData = File.ReadAllBytes(fullPath);
                        Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (diskTex.LoadImage(fileData))
                        {
                            diskTex.name = avatarName;
                            Sprite diskSprite = Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f), 100f);
                            diskSprite.name = avatarName;
                            spriteCache[avatarName] = diskSprite;
                            return diskSprite;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ProfileAvatarDatabase] Avatar dosyasından okuma hatası ({avatarName}): {ex.Message}");
            }

            return GetDefaultCustomerAvatar(avatarName.Contains("female"));
        }

        private static Sprite GetDefaultCustomerAvatar(bool isFemale)
        {
            string fallbackKey = isFemale ? "avatar_female_young_1" : "avatar_male_young_1";
            if (spriteCache.TryGetValue(fallbackKey, out Sprite fallback) && fallback != null)
            {
                return fallback;
            }

            Texture2D solidTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            Color fill = isFemale ? new Color(0.85f, 0.40f, 0.65f) : new Color(0.25f, 0.50f, 0.85f);
            Color[] pixels = new Color[128 * 128];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
            solidTex.SetPixels(pixels);
            solidTex.Apply();

            Sprite solidSprite = Sprite.Create(solidTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);
            spriteCache[fallbackKey] = solidSprite;
            return solidSprite;
        }

        private static Sprite GetDefaultStaffAvatar(bool isFemale)
        {
            string fallbackKey = isFemale ? "avatar_female_worker" : "avatar_male_worker";
            return GetAvatarSprite(fallbackKey);
        }
    }
}

