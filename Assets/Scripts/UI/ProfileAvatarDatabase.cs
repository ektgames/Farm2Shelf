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
    /// yöneten, cinsiyet ve mesleklere göre dağıtan merkezi Avatar Veritabanı.
    /// </summary>
    public static class ProfileAvatarDatabase
    {
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        // Kadın Avatar Listeleri
        private static readonly string[] femaleYoungAvatars = new string[] { "avatar_female_young_1", "avatar_female_young_2" };
        private static readonly string[] femaleSeniorAvatars = new string[] { "avatar_female_senior", "avatar_female_homemaker" };
        private static readonly string[] femaleAdultAvatars = new string[] { "avatar_female_business", "avatar_female_artist", "avatar_female_doctor", "avatar_female_farmer", "avatar_female_cleaner", "avatar_female_cashier" };

        // Erkek Avatar Listeleri
        private static readonly string[] maleYoungAvatars = new string[] { "avatar_male_young_1", "avatar_male_young_2" };
        private static readonly string[] maleSeniorAvatars = new string[] { "avatar_male_senior", "avatar_male_farmer" };
        private static readonly string[] maleAdultAvatars = new string[] { "avatar_male_business", "avatar_male_athletic", "avatar_male_worker", "avatar_male_security", "avatar_male_baker", "avatar_male_cashier" };

        /// <summary>
        /// Müşteri profiline en uygun gerçekçi vesikalık fotoğrafı getirir.
        /// </summary>
        public static Sprite GetCustomerAvatar(CustomerProfileData profile)
        {
            if (profile == null) return GetDefaultAvatar(false);

            string avatarKey = GetAvatarKeyForCustomer(profile.customerType, profile.isFemale, profile.age, profile.fullName);
            return GetAvatarSprite(avatarKey);
        }

        /// <summary>
        /// Personel profiline en uygun gerçekçi vesikalık fotoğrafı getirir.
        /// </summary>
        public static Sprite GetStaffAvatar(StaffMember staff)
        {
            if (staff == null) return GetDefaultAvatar(false);

            bool isFemale = staff.isFemale || StaffManager.IsFemaleName(staff.name);
            string avatarKey = GetAvatarKeyForStaff(staff.role, isFemale, staff.name);
            return GetAvatarSprite(avatarKey);
        }

        /// <summary>
        /// Müşteri tipine ve yaşına göre spesifik avatar anahtarını belirler.
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
                    return "avatar_male_farmer";

                case CustomerType.L1_VillageGirl:
                    return "avatar_female_farmer";

                case CustomerType.L1_GrandpaDede:
                    return "avatar_male_senior";

                case CustomerType.L1_GrandmaTeyze:
                    return "avatar_female_senior";

                case CustomerType.L1_BakeryCustomer:
                    return "avatar_male_baker";

                case CustomerType.L1_Workman:
                    return "avatar_male_worker";

                case CustomerType.L1_SportsMan:
                    return "avatar_male_athletic";

                case CustomerType.L1_NeighborhoodMom:
                    return "avatar_female_homemaker";

                case CustomerType.L2_OfficeWorker:
                    return isFemale ? "avatar_female_business" : "avatar_male_business";

                case CustomerType.L2_HipsterGuy:
                    return "avatar_male_young_2";

                case CustomerType.L2_FashionWoman:
                case CustomerType.L3_BoutiqueLady:
                    return (seed % 2 == 0) ? "avatar_female_business" : "avatar_female_artist";

                case CustomerType.L2_DeliveryCourier:
                    return isFemale ? "avatar_female_young_1" : "avatar_male_young_1";

                case CustomerType.L2_BusinessWoman:
                    return "avatar_female_business";

                case CustomerType.L2_GymBro:
                    return "avatar_male_athletic";

                case CustomerType.L2_ArtistGirl:
                case CustomerType.L3_VIP_Influencer:
                    return (seed % 2 == 0) ? "avatar_female_artist" : "avatar_female_young_1";

                case CustomerType.L2_DoctorWoman:
                    return "avatar_female_doctor";

                case CustomerType.L3_JewelryLady:
                    return "avatar_female_business";

                default:
                    return GetDiverseAvatarByAgeAndGender(isFemale, age, seed);
            }
        }

        /// <summary>
        /// Personel mesleğine ve cinsiyetine göre spesifik avatar anahtarını belirler.
        /// </summary>
        private static string GetAvatarKeyForStaff(StaffRole role, bool isFemale, string nameSeed)
        {
            int seed = Mathf.Abs((nameSeed ?? role.ToString()).GetHashCode());

            switch (role)
            {
                case StaffRole.Kasiyer:
                    return isFemale ? "avatar_female_cashier" : "avatar_male_cashier";

                case StaffRole.Reyoncu:
                    return isFemale ? "avatar_female_young_1" : "avatar_male_worker";

                case StaffRole.Temizlikçi:
                    return isFemale ? "avatar_female_cleaner" : "avatar_male_worker";

                case StaffRole.Güvenlik:
                    return isFemale ? "avatar_female_business" : "avatar_male_security";

                case StaffRole.Çiftçi:
                case StaffRole.DeneyimliÇiftçi:
                case StaffRole.UstaÇiftlikSorumlusu:
                case StaffRole.TarımOtomasyonUzmanı:
                    return isFemale ? "avatar_female_farmer" : "avatar_male_farmer";

                default:
                    return isFemale 
                        ? femaleAdultAvatars[seed % femaleAdultAvatars.Length] 
                        : maleAdultAvatars[seed % maleAdultAvatars.Length];
            }
        }

        private static string GetDiverseAvatarByAgeAndGender(bool isFemale, int age, int seed)
        {
            if (isFemale)
            {
                if (age <= 24) return femaleYoungAvatars[seed % femaleYoungAvatars.Length];
                if (age >= 60) return femaleSeniorAvatars[seed % femaleSeniorAvatars.Length];
                return femaleAdultAvatars[seed % femaleAdultAvatars.Length];
            }
            else
            {
                if (age <= 25) return maleYoungAvatars[seed % maleYoungAvatars.Length];
                if (age >= 60) return maleSeniorAvatars[seed % maleSeniorAvatars.Length];
                return maleAdultAvatars[seed % maleAdultAvatars.Length];
            }
        }

        /// <summary>
        /// İstenen isimdeki avatar Sprite'ını önbellekten veya diskten yükler.
        /// </summary>
        public static Sprite GetAvatarSprite(string avatarName)
        {
            if (string.IsNullOrEmpty(avatarName)) return GetDefaultAvatar(false);

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

            return GetDefaultAvatar(avatarName.Contains("female"));
        }

        private static Sprite GetDefaultAvatar(bool isFemale)
        {
            string fallbackKey = isFemale ? "avatar_female_young_1" : "avatar_male_young_1";
            if (spriteCache.TryGetValue(fallbackKey, out Sprite fallback) && fallback != null)
            {
                return fallback;
            }

            // Temiz usulü 1x1 renkli yedek doku üret
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
    }
}
