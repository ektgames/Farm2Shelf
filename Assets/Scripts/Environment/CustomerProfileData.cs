using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;
using Random = UnityEngine.Random;

namespace Farm2Shelf.Environment
{
    [Serializable]
    public class CustomerProfileData
    {
        public CustomerType customerType;
        public string fullName;       // İsim Soyisim (ör. Ahmet Yılmaz)
        public int age;               // Yaş (ör. 32)
        public bool isFemale;         // Cinsiyet Bayrak
        public string genderText;     // Cinsiyet Metni (ör. Erkek ♂ / Kadın ♀)
        public string occupation;     // Meslek (ör. Yazılımcı)
        public string avatarEmoji;    // Profil Fotoğrafı Emoji Simgesi (ör. 👨‍💻)
        public Color avatarBgColor;   // Profil Çerçeve Rengi

        public string LocalizedGenderText => isFemale 
            ? LocalizationManager.L("Gender_Female_Short", "Kadın ♀", "Female ♀") 
            : LocalizationManager.L("Gender_Male_Short", "Erkek ♂", "Male ♂");

        public string LocalizedOccupationText => CustomerProfileGenerator.GetLocalizedOccupation(customerType);
    }

    public static class CustomerProfileGenerator
    {
        public static string GetLocalizedOccupation(CustomerType type)
        {
            switch (type)
            {
                case CustomerType.L1_CasualBoy: return LocalizationManager.L("Occ_Student", "Üniversite Öğrencisi", "College Student");
                case CustomerType.L1_FarmerUncle: return LocalizationManager.L("Occ_Farmer", "Çiftçi / Üretici", "Farmer / Producer");
                case CustomerType.L1_GrandmaTeyze: return LocalizationManager.L("Occ_RetiredTeacher", "Emekli Öğretmen", "Retired Teacher");
                case CustomerType.L1_StudentGirl: return LocalizationManager.L("Occ_StudentGirl", "Öğrenci", "Student");
                case CustomerType.L1_BakeryCustomer: return LocalizationManager.L("Occ_Baker", "Fırın Ustası", "Baker");
                case CustomerType.L1_Workman: return LocalizationManager.L("Occ_Workman", "İnşaat Ustası", "Construction Worker");
                case CustomerType.L1_GrandpaDede: return LocalizationManager.L("Occ_RetiredOfficer", "Emekli Memur", "Retired Civil Servant");
                case CustomerType.L1_VillageGirl: return LocalizationManager.L("Occ_AgriEnt", "Tarım Girişimcisi", "Agri-Entrepreneur");
                case CustomerType.L1_SportsMan: return LocalizationManager.L("Occ_PETeacher", "Beden Eğitimi Öğretmeni", "P.E. Teacher");
                case CustomerType.L1_NeighborhoodMom: return LocalizationManager.L("Occ_Homemaker", "Ev Hanımı", "Homemaker");
                case CustomerType.L2_OfficeWorker: return LocalizationManager.L("Occ_Accountant", "Muhasebe Uzmanı", "Accountant");
                case CustomerType.L2_HipsterGuy: return LocalizationManager.L("Occ_Designer", "Grafik Tasarımcı", "Graphic Designer");
                case CustomerType.L2_FashionWoman: return LocalizationManager.L("Occ_Stylist", "Moda Stilisti", "Fashion Stylist");
                case CustomerType.L2_DeliveryCourier: return LocalizationManager.L("Occ_Courier", "Lojistik Kurye", "Delivery Courier");
                case CustomerType.L2_BusinessWoman: return LocalizationManager.L("Occ_MktManager", "Pazarlama Müdürü", "Marketing Manager");
                case CustomerType.L2_GymBro: return LocalizationManager.L("Occ_FitnessTrainer", "Fitness Eğitmeni", "Fitness Trainer");
                case CustomerType.L2_ArtistGirl: return LocalizationManager.L("Occ_Painter", "Ressam & Sanatçı", "Painter & Artist");
                case CustomerType.L2_DoctorWoman: return LocalizationManager.L("Occ_Doctor", "Uzman Doktor", "Specialist Doctor");
                case CustomerType.L3_VIP_Influencer: return LocalizationManager.L("Occ_Influencer", "Sosyal Medya Fenomeni", "Social Media Influencer");
                case CustomerType.L3_BoutiqueLady: return LocalizationManager.L("Occ_BoutiqueOwner", "Butik Sahibi", "Boutique Owner");
                case CustomerType.L3_JewelryLady: return LocalizationManager.L("Occ_Jeweler", "Mücevher Tasarımcısı", "Jewelry Designer");
                default: return LocalizationManager.L("Occ_Customer", "Müşteri", "Customer");
            }
        }

        private static readonly string[] maleFirstNamesTr = new string[]
        {
            "Ahmet", "Mehmet", "Mustafa", "Ali", "Can", "Burak", "Emre", "Serkan", "Ömer", 
            "Kerem", "Hakan", "Murat", "Ogün", "Tolga", "Volkan", "Eren", "Batu", "Selim",
            "Kaan", "Cem", "Taha", "Gökhan", "Alperen", "Turgut", "Boran", "Hasan", "Onur"
        };
        private static readonly string[] maleFirstNamesEn = new string[]
        {
            "John", "James", "Robert", "Michael", "William", "David", "Richard", "Charles", "Joseph", "Thomas",
            "Christopher", "Daniel", "Matthew", "Anthony", "Mark", "Steven", "Andrew", "Paul", "Joshua", "Kenneth"
        };

        private static readonly string[] femaleFirstNamesTr = new string[]
        {
            "Ayşe", "Elif", "Zeynep", "Merve", "Büşra", "Selin", "Deniz", "Gizem", "Ebru", 
            "Ceren", "Aslı", "İrem", "Gamze", "Yasemin", "Defne", "Melisa", "Pelin", "Derya",
            "Fatma", "Sibel", "Tuğba", "Esra", "Cansu", "Beste", "Simge", "Hande", "Nur"
        };
        private static readonly string[] femaleFirstNamesEn = new string[]
        {
            "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Barbara", "Susan", "Jessica", "Sarah", "Karen",
            "Lisa", "Nancy", "Betty", "Margaret", "Sandra", "Ashley", "Kimberly", "Emily", "Donna", "Michelle"
        };

        private static readonly string[] lastNamesTr = new string[]
        {
            "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız", "Yıldırım", "Öztürk", 
            "Aydın", "Özdemir", "Arslan", "Doğan", "Kılıç", "Aslan", "Çetin", "Kara", 
            "Koç", "Kurt", "Özkan", "Şimşek", "Güneş", "Bulut", "Erdem", "Tekin", "Soylu"
        };
        private static readonly string[] lastNamesEn = new string[]
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
            "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin"
        };

        public static CustomerProfileData GenerateProfile(CustomerType type)
        {
            CustomerProfileData data = new CustomerProfileData();
            data.customerType = type;

            bool isFemale = IsCustomerFemale(type);
            data.isFemale = isFemale;
            data.genderText = isFemale ? "Kadın ♀" : "Erkek ♂";

            bool isEnglish = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            string[] femaleNames = isEnglish ? femaleFirstNamesEn : femaleFirstNamesTr;
            string[] maleNames = isEnglish ? maleFirstNamesEn : maleFirstNamesTr;
            string[] surnames = isEnglish ? lastNamesEn : lastNamesTr;

            string firstName = isFemale 
                ? femaleNames[Random.Range(0, femaleNames.Length)] 
                : maleNames[Random.Range(0, maleNames.Length)];
            string lastName = surnames[Random.Range(0, surnames.Length)];
            data.fullName = $"{firstName} {lastName}";

            GetDetailsForCustomerType(type, isFemale, out int minAge, out int maxAge, out string defaultOcc, out string emoji, out Color bgColor);

            data.age = Random.Range(minAge, maxAge + 1);
            data.occupation = defaultOcc;
            data.avatarEmoji = emoji;
            data.avatarBgColor = bgColor;

            return data;
        }

        private static bool IsCustomerFemale(CustomerType type)
        {
            switch (type)
            {
                case CustomerType.L1_GrandmaTeyze:
                case CustomerType.L1_StudentGirl:
                case CustomerType.L1_VillageGirl:
                case CustomerType.L1_NeighborhoodMom:
                case CustomerType.L2_FashionWoman:
                case CustomerType.L2_BusinessWoman:
                case CustomerType.L2_ArtistGirl:
                case CustomerType.L2_DoctorWoman:
                case CustomerType.L3_VIP_Influencer:
                case CustomerType.L3_BoutiqueLady:
                case CustomerType.L3_JewelryLady:
                    return true;
                default:
                    return false;
            }
        }

        private static void GetDetailsForCustomerType(CustomerType type, bool isFemale, out int minAge, out int maxAge, out string occ, out string emoji, out Color bgColor)
        {
            switch (type)
            {
                // Level 1 Müşteriler
                case CustomerType.L1_CasualBoy:
                    minAge = 20; maxAge = 26; occ = "Üniversite Öğrencisi"; emoji = "👦";
                    bgColor = new Color(0.20f, 0.50f, 0.80f); break;
                case CustomerType.L1_FarmerUncle:
                    minAge = 50; maxAge = 64; occ = "Çiftçi / Üretici"; emoji = "👨‍🌾";
                    bgColor = new Color(0.35f, 0.55f, 0.25f); break;
                case CustomerType.L1_GrandmaTeyze:
                    minAge = 60; maxAge = 74; occ = "Emekli Öğretmen"; emoji = "👵";
                    bgColor = new Color(0.70f, 0.40f, 0.50f); break;
                case CustomerType.L1_StudentGirl:
                    minAge = 19; maxAge = 24; occ = "Öğrenci"; emoji = "👩‍🎓";
                    bgColor = new Color(0.85f, 0.45f, 0.65f); break;
                case CustomerType.L1_BakeryCustomer:
                    minAge = 35; maxAge = 50; occ = "Fırın Ustası"; emoji = "👨‍🍳";
                    bgColor = new Color(0.80f, 0.55f, 0.20f); break;
                case CustomerType.L1_Workman:
                    minAge = 28; maxAge = 46; occ = "İnşaat Ustası"; emoji = "👷‍♂️";
                    bgColor = new Color(0.85f, 0.50f, 0.15f); break;
                case CustomerType.L1_GrandpaDede:
                    minAge = 65; maxAge = 78; occ = "Emekli Memur"; emoji = "👴";
                    bgColor = new Color(0.40f, 0.40f, 0.45f); break;
                case CustomerType.L1_VillageGirl:
                    minAge = 21; maxAge = 30; occ = "Tarım Girişimcisi"; emoji = "👩‍🌾";
                    bgColor = new Color(0.45f, 0.65f, 0.30f); break;
                case CustomerType.L1_SportsMan:
                    minAge = 23; maxAge = 32; occ = "Beden Eğitimi Öğretmeni"; emoji = "🏃‍♂️";
                    bgColor = new Color(0.15f, 0.60f, 0.75f); break;
                case CustomerType.L1_NeighborhoodMom:
                    minAge = 38; maxAge = 52; occ = "Ev Hanımı"; emoji = "👩";
                    bgColor = new Color(0.75f, 0.35f, 0.45f); break;

                // Level 2 Müşteriler
                case CustomerType.L2_OfficeWorker:
                    minAge = 28; maxAge = 44; occ = "Muhasebe Uzmanı"; emoji = "👨‍💼";
                    bgColor = new Color(0.25f, 0.35f, 0.65f); break;
                case CustomerType.L2_HipsterGuy:
                    minAge = 24; maxAge = 34; occ = "Grafik Tasarımcı"; emoji = "🧔";
                    bgColor = new Color(0.55f, 0.30f, 0.65f); break;
                case CustomerType.L2_FashionWoman:
                    minAge = 25; maxAge = 36; occ = "Moda Stilisti"; emoji = "👠";
                    bgColor = new Color(0.85f, 0.30f, 0.55f); break;
                case CustomerType.L2_DeliveryCourier:
                    minAge = 21; maxAge = 30; occ = "Lojistik Kurye"; emoji = "🛵";
                    bgColor = new Color(0.90f, 0.45f, 0.15f); break;
                case CustomerType.L2_BusinessWoman:
                    minAge = 32; maxAge = 46; occ = "Pazarlama Müdürü"; emoji = "👩‍💼";
                    bgColor = new Color(0.20f, 0.55f, 0.70f); break;
                case CustomerType.L2_GymBro:
                    minAge = 24; maxAge = 35; occ = "Kişisel Antrenör"; emoji = "🏋️‍♂️";
                    bgColor = new Color(0.75f, 0.20f, 0.20f); break;
                case CustomerType.L2_ArtistGirl:
                    minAge = 23; maxAge = 33; occ = "Ressam / Sanatçı"; emoji = "👩‍🎨";
                    bgColor = new Color(0.65f, 0.40f, 0.75f); break;
                case CustomerType.L2_TechNerd:
                    minAge = 23; maxAge = 36; occ = "Kıdemli Yazılımcı"; emoji = "👨‍💻";
                    bgColor = new Color(0.15f, 0.65f, 0.50f); break;
                case CustomerType.L2_TouristGuy:
                    minAge = 30; maxAge = 48; occ = "Gezi Fotoğrafçısı"; emoji = "📷";
                    bgColor = new Color(0.30f, 0.60f, 0.40f); break;
                case CustomerType.L2_DoctorWoman:
                    minAge = 34; maxAge = 50; occ = "Uzman Doktor"; emoji = "👩‍⚕️";
                    bgColor = new Color(0.20f, 0.65f, 0.85f); break;

                // Level 3 Müşteriler
                case CustomerType.L3_CEO_Executive:
                    minAge = 42; maxAge = 58; occ = "Şirket CEO'su"; emoji = "🕴️";
                    bgColor = new Color(0.15f, 0.20f, 0.35f); break;
                case CustomerType.L3_VIP_Influencer:
                    minAge = 22; maxAge = 31; occ = "Dijital İçerik Üreticisi"; emoji = "🌟";
                    bgColor = new Color(0.90f, 0.40f, 0.70f); break;
                case CustomerType.L3_RichGentleman:
                    minAge = 50; maxAge = 66; occ = "Yatırımcı / Sanayici"; emoji = "🎩";
                    bgColor = new Color(0.45f, 0.35f, 0.20f); break;
                case CustomerType.L3_BoutiqueLady:
                    minAge = 38; maxAge = 54; occ = "Galeri Sahibi"; emoji = "💃";
                    bgColor = new Color(0.75f, 0.25f, 0.45f); break;
                case CustomerType.L3_GamerPro:
                    minAge = 19; maxAge = 27; occ = "E-Spor Oyuncusu"; emoji = "🎮";
                    bgColor = new Color(0.50f, 0.20f, 0.80f); break;
                case CustomerType.L3_CelebrityActor:
                    minAge = 30; maxAge = 46; occ = "Sinema Oyuncusu"; emoji = "🎬";
                    bgColor = new Color(0.85f, 0.65f, 0.15f); break;
                case CustomerType.L3_PilotMan:
                    minAge = 36; maxAge = 52; occ = "Kaptan Pilot"; emoji = "👨‍✈️";
                    bgColor = new Color(0.15f, 0.40f, 0.75f); break;
                case CustomerType.L3_GoldChainRapper:
                    minAge = 23; maxAge = 32; occ = "Müzik Yapımcısı"; emoji = "🎤";
                    bgColor = new Color(0.80f, 0.50f, 0.10f); break;
                case CustomerType.L3_JewelryLady:
                    minAge = 34; maxAge = 50; occ = "Mücevher Tasarımcısı"; emoji = "💎";
                    bgColor = new Color(0.20f, 0.75f, 0.75f); break;
                case CustomerType.L3_BillionaireYacht:
                    minAge = 48; maxAge = 64; occ = "Armatör"; emoji = "⚓";
                    bgColor = new Color(0.10f, 0.35f, 0.60f); break;

                default:
                    minAge = 25; maxAge = 45; occ = "Müşteri"; emoji = isFemale ? "👩" : "👨";
                    bgColor = new Color(0.30f, 0.50f, 0.70f); break;
            }
        }
    }
}
