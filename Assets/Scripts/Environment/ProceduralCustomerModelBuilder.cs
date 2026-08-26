using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    public enum CustomerType
    {
        // ==================== SEVİYE 1 MÜŞTERİLER (15 ADET) ====================
        L1_CasualBoy,          // Kot tişörtlü genç çocuk
        L1_FarmerUncle,        // Şapkalı tulumlu çiftçi amca
        L1_GrandmaTeyze,       // Eşarplı hırkalı teyze
        L1_StudentGirl,        // Sırt çantalı öğrenci kız
        L1_BakeryCustomer,     // Önlüklü usta uşağı
        L1_Workman,            // Baretli tulumlu usta
        L1_GrandpaDede,        // Kasketli bastonlu dede
        L1_VillageGirl,        // Çiçekli elbiseli köylü kızı
        L1_SportsMan,          // Eşofmanlı koşan genç
        L1_NeighborhoodMom,    // Alışveriş çantalı mahalle annesi
        L1_GardenerGrandpa,    // Hasır şapkalı tulumlu bahçıvan dede
        L1_BookwormGirl,       // Yuvarlak gözlüklü örgü kazaklı kitap kurdu kız
        L1_MusicianGuy,        // Kot ceketli sırtında gitar taşıyan müzisyen genç
        L1_PostmanUncle,       // Mavi üniformalı postacı amca
        L1_Fisherman,          // Sarı yağmurluklu denizci kasketli balıkçı

        // ==================== SEVİYE 2 MÜŞTERİLER (15 ADET) ====================
        L2_OfficeWorker,       // Kravatlı gömlekli ofis çalışanı
        L2_HipsterGuy,         // Bereli gözlüklü hipster genç
        L2_FashionWoman,       // Güneş gözlüklü elbiseli şık kadın
        L2_DeliveryCourier,    // Kasklı yelekli kurye
        L2_BusinessWoman,      // Ceketli etekli iş kadını
        L2_GymBro,             // Atletli kaslı sporcu
        L2_ArtistGirl,         // Bereli ressam kız
        L2_TechNerd,           // Kapüşonlu kulaklıklı yazılımcı
        L2_TouristGuy,         // Kameralı şortlu turist
        L2_DoctorWoman,        // Steteskoplu doktor kadın
        L2_ChefMaster,         // Beyaz önlüklü ve şapkalı restoran şefi
        L2_YogaInstructor,     // Spor giyimli mat taşıyan yoga eğitmeni
        L2_ArchitectGuy,       // Boğazlı kazaklı çizim tüplü mimar
        L2_BaristaGirl,        // Deri önlüklü bandanalı kahve baristası
        L2_Veterinarian,       // Medikal önlüklü steteskoplu veteriner

        // ==================== SEVİYE 3 MÜŞTERİLER (15 ADET) ====================
        L3_CEO_Executive,      // Lüks takım elbiseli CEO
        L3_VIP_Influencer,     // Şapkalı stil sahibi influencer
        L3_RichGentleman,      // Silindir şapkalı zengin bey
        L3_BoutiqueLady,       // Kürk yakalı şık leydi
        L3_GamerPro,           // Işıklı kulaklıklı oyuncu
        L3_CelebrityActor,     // Şık ceketli ünlü aktör
        L3_PilotMan,           // Kaptan pilot üniformalı adam
        L3_GoldChainRapper,    // Altın kolyeli tarz müzisyen
        L3_JewelryLady,        // Gece elbiseli mücevherli hanımefendi
        L3_BillionaireYacht,   // Yat kaptanı milyarder
        L3_GourmetCritic,      // İpek fularlı tüvit ceketli gurme eleştirmeni
        L3_Supermodel,         // Siyah podyum elbiseli pırlanta küpeli süpermodel
        L3_TechInvestor,       // Minimalist blazer ceketli melek yatırımcı
        L3_OperaSinger,        // Papyonlu smokinli fraklı opera sanatçısı
        L3_LuxuryCollector     // Bordo kadife ceketli inci kolyeli sanat koleksiyoneri
    }

    public static class ProceduralCustomerModelBuilder
    {
        private static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        private static Material GetMaterial(string name, Color color, float metallic = 0.1f, float smoothness = 0.4f)
        {
            if (matCache.TryGetValue(name, out Material mat) && mat != null)
            {
                return mat;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            Material newMat = new Material(shader)
            {
                name = name,
                color = color
            };

            if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", color);
            if (newMat.HasProperty("_Color")) newMat.SetColor("_Color", color);
            if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", metallic);
            if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", smoothness);

            matCache[name] = newMat;
            return newMat;
        }

        public static GameObject CreateCustomerModel(CustomerType type, out List<Transform> leftLimbs, out List<Transform> rightLimbs)
        {
            leftLimbs = new List<Transform>();
            rightLimbs = new List<Transform>();

            GameObject root = new GameObject("Customer_" + type.ToString());
            Transform tRoot = root.transform;

            // Fiziksel Çarpışma ve Kapı Algılama Bileşenleri
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.35f;
            col.height = 1.8f;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Ten Rengi Materyalleri
            Material skinMat1 = GetMaterial("Mat_SkinLight", new Color(0.95f, 0.80f, 0.68f));
            Material skinMat2 = GetMaterial("Mat_SkinMedium", new Color(0.85f, 0.65f, 0.50f));
            Material skinMat3 = GetMaterial("Mat_SkinDark", new Color(0.55f, 0.38f, 0.28f));
            Material hairBlack = GetMaterial("Mat_HairBlack", new Color(0.12f, 0.12f, 0.14f));
            Material hairBrown = GetMaterial("Mat_HairBrown", new Color(0.40f, 0.25f, 0.15f));
            Material hairBlonde = GetMaterial("Mat_HairBlonde", new Color(0.92f, 0.80f, 0.35f));
            Material hairGray = GetMaterial("Mat_HairGray", new Color(0.75f, 0.75f, 0.78f));
            Material shoeDark = GetMaterial("Mat_ShoeDark", new Color(0.15f, 0.15f, 0.18f));

            Material selectedSkin = skinMat1;
            int typeIndex = (int)type;
            if (typeIndex % 3 == 1) selectedSkin = skinMat2;
            else if (typeIndex % 3 == 2) selectedSkin = skinMat3;

            // Temel İnsan Gövdesi Yapısı (Pelvis, Torso, Baş, Bacaklar, Kollar)
            Color bodyColor = GetTypePrimaryColor(type);
            Color pantsColor = GetTypeSecondaryColor(type);

            Material bodyMat = GetMaterial("Mat_CustBody_" + type.ToString(), bodyColor);
            Material pantsMat = GetMaterial("Mat_CustPants_" + type.ToString(), pantsColor);

            // Torso (Gövde)
            CreateBlock(tRoot, "Torso", new Vector3(0f, 1.05f, 0f), new Vector3(0.50f, 0.55f, 0.30f), bodyMat);

            // Baş (Head)
            CreateBlock(tRoot, "Head", new Vector3(0f, 1.52f, 0f), new Vector3(0.32f, 0.32f, 0.30f), selectedSkin);

            // Sol ve Sağ Bacak (Left/Right Legs)
            GameObject lLeg = CreateLimb(tRoot, "Leg_L", new Vector3(-0.14f, 0.75f, 0f), new Vector3(0.20f, 0.70f, 0.22f), pantsMat, shoeDark);
            GameObject rLeg = CreateLimb(tRoot, "Leg_R", new Vector3(0.14f, 0.75f, 0f), new Vector3(0.20f, 0.70f, 0.22f), pantsMat, shoeDark);
            leftLimbs.Add(lLeg.transform);
            rightLimbs.Add(rLeg.transform);

            // Sol ve Sağ Kol (Left/Right Arms)
            GameObject lArm = CreateArm(tRoot, "Arm_L", new Vector3(-0.33f, 1.25f, 0f), new Vector3(0.16f, 0.55f, 0.18f), bodyMat, selectedSkin);
            GameObject rArm = CreateArm(tRoot, "Arm_R", new Vector3(0.33f, 1.25f, 0f), new Vector3(0.16f, 0.55f, 0.18f), bodyMat, selectedSkin);
            leftLimbs.Add(lArm.transform);
            rightLimbs.Add(rArm.transform);

            // 30 Müşteri Tipine Özel Aksesuar ve Kıyafet Detayları
            BuildCustomerAccessories(type, tRoot, selectedSkin, hairBlack, hairBrown, hairBlonde, hairGray);

            return root;
        }

        private static Color GetTypePrimaryColor(CustomerType type)
        {
            switch (type)
            {
                // Level 1
                case CustomerType.L1_CasualBoy: return new Color(0.85f, 0.20f, 0.20f); // Kırmızı Tişört
                case CustomerType.L1_FarmerUncle: return new Color(0.25f, 0.45f, 0.25f); // Yeşil Tulum
                case CustomerType.L1_GrandmaTeyze: return new Color(0.65f, 0.35f, 0.55f); // Pembe Hırka
                case CustomerType.L1_StudentGirl: return new Color(0.90f, 0.70f, 0.20f); // Sarı Sweat
                case CustomerType.L1_BakeryCustomer: return new Color(0.85f, 0.85f, 0.85f); // Beyaz Önlük
                case CustomerType.L1_Workman: return new Color(0.95f, 0.50f, 0.10f); // Turuncu Yelek
                case CustomerType.L1_GrandpaDede: return new Color(0.45f, 0.38f, 0.28f); // Kahve Ceket
                case CustomerType.L1_VillageGirl: return new Color(0.95f, 0.40f, 0.60f); // Çiçekli Elbise
                case CustomerType.L1_SportsMan: return new Color(0.15f, 0.50f, 0.85f); // Mavi Eşofman
                case CustomerType.L1_NeighborhoodMom: return new Color(0.50f, 0.25f, 0.60f); // Mor Elbise
                case CustomerType.L1_GardenerGrandpa: return new Color(0.22f, 0.48f, 0.24f); // Yeşil Bahçe Önlüğü
                case CustomerType.L1_BookwormGirl: return new Color(0.88f, 0.68f, 0.18f); // Hardal Örgü Kazak
                case CustomerType.L1_MusicianGuy: return new Color(0.35f, 0.55f, 0.80f); // Kot Ceket
                case CustomerType.L1_PostmanUncle: return new Color(0.18f, 0.40f, 0.75f); // Postacı Mavisi Üniforma
                case CustomerType.L1_Fisherman: return new Color(0.95f, 0.78f, 0.10f); // Sarı Yağmurluk

                // Level 2
                case CustomerType.L2_OfficeWorker: return new Color(0.95f, 0.95f, 0.95f); // Beyaz Gömlek
                case CustomerType.L2_HipsterGuy: return new Color(0.80f, 0.35f, 0.15f); // Hardal Ceket
                case CustomerType.L2_FashionWoman: return new Color(0.95f, 0.25f, 0.45f); // Kırmızı Elbise
                case CustomerType.L2_DeliveryCourier: return new Color(0.10f, 0.65f, 0.35f); // Yeşil Kurye Yeleği
                case CustomerType.L2_BusinessWoman: return new Color(0.18f, 0.22f, 0.35f); // Lacivert Ceket
                case CustomerType.L2_GymBro: return new Color(0.12f, 0.12f, 0.14f); // Siyah Atlet
                case CustomerType.L2_ArtistGirl: return new Color(0.35f, 0.65f, 0.75f); // Mavi Önlük
                case CustomerType.L2_TechNerd: return new Color(0.25f, 0.28f, 0.35f); // Gri Kapüşonlu
                case CustomerType.L2_TouristGuy: return new Color(0.95f, 0.85f, 0.25f); // Çiçekli Desenli Gömlek
                case CustomerType.L2_DoctorWoman: return new Color(0.98f, 0.98f, 1.0f); // Beyaz Doktor Önlüğü
                case CustomerType.L2_ChefMaster: return new Color(0.98f, 0.98f, 0.98f); // Beyaz Şef Ceketi
                case CustomerType.L2_YogaInstructor: return new Color(0.72f, 0.60f, 0.85f); // Lila Spor Üst
                case CustomerType.L2_ArchitectGuy: return new Color(0.10f, 0.10f, 0.12f); // Siyah Boğazlı Kazak
                case CustomerType.L2_BaristaGirl: return new Color(0.30f, 0.45f, 0.35f); // Zeytin Yeşili Gömlek
                case CustomerType.L2_Veterinarian: return new Color(0.40f, 0.70f, 0.55f); // Mint Yeşili Medikal Önlük

                // Level 3
                case CustomerType.L3_CEO_Executive: return new Color(0.12f, 0.14f, 0.18f); // Siyah Lüks Takım
                case CustomerType.L3_VIP_Influencer: return new Color(0.95f, 0.75f, 0.85f); // Stil Pembe Ceket
                case CustomerType.L3_RichGentleman: return new Color(0.20f, 0.15f, 0.25f); // Koyu Mor Takım
                case CustomerType.L3_BoutiqueLady: return new Color(0.85f, 0.75f, 0.55f); // Krem Kürk Ceket
                case CustomerType.L3_GamerPro: return new Color(0.15f, 0.85f, 0.95f); // Işıklı Turkuaz Sweat
                case CustomerType.L3_CelebrityActor: return new Color(0.85f, 0.20f, 0.25f); // Kırmızı Halı Ceketi
                case CustomerType.L3_PilotMan: return new Color(0.10f, 0.15f, 0.30f); // Kaptan Pilot Üniforması
                case CustomerType.L3_GoldChainRapper: return new Color(0.95f, 0.80f, 0.15f); // Altın Sarısı Ceket
                case CustomerType.L3_JewelryLady: return new Color(0.10f, 0.45f, 0.75f); // Safir Gece Elbisesi
                case CustomerType.L3_BillionaireYacht: return new Color(0.95f, 0.95f, 0.98f); // Yat Kaptanı Kıyafeti
                case CustomerType.L3_GourmetCritic: return new Color(0.42f, 0.28f, 0.18f); // Kahverengi Tüvit Ceket
                case CustomerType.L3_Supermodel: return new Color(0.08f, 0.08f, 0.10f); // Siyah Saten Gece Elbisesi
                case CustomerType.L3_TechInvestor: return new Color(0.12f, 0.25f, 0.50f); // İtalyan Lacivert Blazer
                case CustomerType.L3_OperaSinger: return new Color(0.06f, 0.06f, 0.08f); // Siyah Fraklı Smokin
                case CustomerType.L3_LuxuryCollector: return new Color(0.48f, 0.10f, 0.20f); // Bordo Kadife Ceket
                default: return Color.blue;
            }
        }

        private static Color GetTypeSecondaryColor(CustomerType type)
        {
            switch (type)
            {
                case CustomerType.L1_CasualBoy: return new Color(0.20f, 0.35f, 0.65f); // Kot Pantolon
                case CustomerType.L1_FarmerUncle: return new Color(0.25f, 0.45f, 0.25f);
                case CustomerType.L1_StudentGirl: return new Color(0.18f, 0.20f, 0.25f);
                case CustomerType.L1_GardenerGrandpa: return new Color(0.38f, 0.28f, 0.18f); // Toprak Rengi Pantolon
                case CustomerType.L1_BookwormGirl: return new Color(0.18f, 0.24f, 0.40f); // Lacivert Etek
                case CustomerType.L1_MusicianGuy: return new Color(0.18f, 0.18f, 0.22f); // Siyah Kot
                case CustomerType.L1_PostmanUncle: return new Color(0.14f, 0.18f, 0.28f); // Koyu Lacivert Pantolon
                case CustomerType.L1_Fisherman: return new Color(0.12f, 0.22f, 0.38f); // Koyu Mavi Tulum
                case CustomerType.L2_OfficeWorker: return new Color(0.15f, 0.18f, 0.25f); // Siyah Kumaş Pantolon
                case CustomerType.L2_GymBro: return new Color(0.85f, 0.20f, 0.20f); // Spor Şort
                case CustomerType.L2_ChefMaster: return new Color(0.15f, 0.15f, 0.18f); // Siyah Şef Pantolonu
                case CustomerType.L2_YogaInstructor: return new Color(0.28f, 0.18f, 0.35f); // Koyu Mürdüm Tayt
                case CustomerType.L2_ArchitectGuy: return new Color(0.22f, 0.24f, 0.28f); // Kömür Grisi Pantolon
                case CustomerType.L2_BaristaGirl: return new Color(0.15f, 0.22f, 0.35f); // Koyu Kot Pantolon
                case CustomerType.L2_Veterinarian: return new Color(0.35f, 0.65f, 0.50f); // Mint Scrub Pantolonu
                case CustomerType.L3_CEO_Executive: return new Color(0.12f, 0.14f, 0.18f);
                case CustomerType.L3_GoldChainRapper: return new Color(0.12f, 0.12f, 0.14f);
                case CustomerType.L3_GourmetCritic: return new Color(0.85f, 0.80f, 0.70f); // Krem Pantolon
                case CustomerType.L3_Supermodel: return new Color(0.08f, 0.08f, 0.10f); // Saten Etek
                case CustomerType.L3_TechInvestor: return new Color(0.20f, 0.22f, 0.26f); // Şık Slim Kumaş
                case CustomerType.L3_OperaSinger: return new Color(0.08f, 0.08f, 0.10f); // Smokin Pantolonu
                case CustomerType.L3_LuxuryCollector: return new Color(0.12f, 0.12f, 0.14f); // Siyah Saten Pantolon
                default: return new Color(0.20f, 0.22f, 0.28f);
            }
        }

        private static void BuildCustomerAccessories(CustomerType type, Transform parent, Material skin, Material hairBlack, Material hairBrown, Material hairBlonde, Material hairGray)
        {
            Material hatRed = GetMaterial("Mat_HatRed", new Color(0.85f, 0.15f, 0.15f));
            Material hatYellow = GetMaterial("Mat_HatYellow", new Color(0.95f, 0.85f, 0.10f));
            Material hatBlack = GetMaterial("Mat_HatBlack", new Color(0.12f, 0.12f, 0.14f));
            Material hatWhite = GetMaterial("Mat_CapWhite", Color.white);
            Material goldMat = GetMaterial("Mat_CustGold", new Color(0.95f, 0.80f, 0.15f), 0.9f, 0.9f);
            Material pearlMat = GetMaterial("Mat_CustPearl", new Color(0.95f, 0.95f, 0.95f), 0.3f, 0.95f);
            Material glassLensMat = GetMaterial("Mat_CustLens", new Color(0.2f, 0.2f, 0.25f, 0.8f), 0.9f, 0.9f);
            Material leatherBrown = GetMaterial("Mat_LeatherBrown", new Color(0.42f, 0.24f, 0.12f), 0.3f, 0.3f);
            Material turquoiseMat = GetMaterial("Mat_Turquoise", new Color(0.15f, 0.75f, 0.80f));
            Material rubyMat = GetMaterial("Mat_RubyRed", new Color(0.85f, 0.10f, 0.25f), 0.8f, 0.9f);

            switch (type)
            {
                // ==================== LEVEL 1 AKSESUARLARI ====================
                case CustomerType.L1_FarmerUncle:
                    CreateBlock(parent, "Farmer_Hat", new Vector3(0f, 1.70f, 0f), new Vector3(0.55f, 0.08f, 0.55f), hatYellow);
                    break;

                case CustomerType.L1_StudentGirl:
                    CreateBlock(parent, "Backpack", new Vector3(0f, 1.05f, -0.22f), new Vector3(0.35f, 0.40f, 0.16f), hatRed);
                    break;

                case CustomerType.L1_Workman:
                    CreateBlock(parent, "Safety_Helmet", new Vector3(0f, 1.72f, 0f), new Vector3(0.38f, 0.15f, 0.38f), hatYellow);
                    break;

                case CustomerType.L1_GrandpaDede:
                    CreateBlock(parent, "Cap", new Vector3(0f, 1.70f, 0.05f), new Vector3(0.35f, 0.10f, 0.40f), hairGray);
                    CreateBlock(parent, "Cane", new Vector3(0.35f, 0.50f, 0.15f), new Vector3(0.05f, 0.90f, 0.05f), hairBrown);
                    break;

                case CustomerType.L1_GardenerGrandpa:
                    // Hasır Şapka & Bahçe Çantası
                    CreateBlock(parent, "Straw_Hat_Brim", new Vector3(0f, 1.68f, 0f), new Vector3(0.58f, 0.06f, 0.58f), hatYellow);
                    CreateBlock(parent, "Straw_Hat_Crown", new Vector3(0f, 1.76f, 0f), new Vector3(0.34f, 0.14f, 0.34f), hatYellow);
                    CreateBlock(parent, "Beard_Gray", new Vector3(0f, 1.44f, 0.14f), new Vector3(0.24f, 0.14f, 0.12f), hairGray);
                    break;

                case CustomerType.L1_BookwormGirl:
                    // Yuvarlak Gözlük & Kitap Destesi
                    CreateBlock(parent, "Glasses", new Vector3(0f, 1.55f, 0.16f), new Vector3(0.28f, 0.08f, 0.04f), hatBlack);
                    CreateBlock(parent, "Book_Stack", new Vector3(-0.35f, 0.95f, 0.10f), new Vector3(0.14f, 0.22f, 0.28f), hatRed);
                    break;

                case CustomerType.L1_MusicianGuy:
                    // Sırtında Gitar Çantası & Havalı Saç
                    CreateBlock(parent, "Guitar_Body", new Vector3(0f, 1.15f, -0.24f), new Vector3(0.32f, 0.45f, 0.14f), leatherBrown);
                    CreateBlock(parent, "Guitar_Neck", new Vector3(0.08f, 1.50f, -0.22f), new Vector3(0.08f, 0.38f, 0.06f), leatherBrown);
                    CreateBlock(parent, "Hair_Wavy", new Vector3(0f, 1.70f, 0f), new Vector3(0.36f, 0.14f, 0.36f), hairBrown);
                    break;

                case CustomerType.L1_PostmanUncle:
                    // Postacı Şapkası & Omuz Çantası
                    CreateBlock(parent, "Mail_Cap", new Vector3(0f, 1.70f, 0.04f), new Vector3(0.36f, 0.12f, 0.40f), GetMaterial("Mat_PostNavy", new Color(0.12f, 0.20f, 0.45f)));
                    CreateBlock(parent, "Mail_Badge", new Vector3(0f, 1.72f, 0.21f), new Vector3(0.08f, 0.06f, 0.02f), hatYellow);
                    CreateBlock(parent, "Mail_Bag", new Vector3(0.35f, 0.85f, 0f), new Vector3(0.12f, 0.32f, 0.38f), leatherBrown);
                    break;

                case CustomerType.L1_Fisherman:
                    // Balıkçı Beresi & Sakal
                    CreateBlock(parent, "Sailor_Beanie", new Vector3(0f, 1.72f, 0f), new Vector3(0.34f, 0.16f, 0.34f), GetMaterial("Mat_NavyBeanie", new Color(0.10f, 0.18f, 0.35f)));
                    CreateBlock(parent, "Fisher_Beard", new Vector3(0f, 1.42f, 0.14f), new Vector3(0.26f, 0.18f, 0.14f), hairGray);
                    break;

                // ==================== LEVEL 2 AKSESUARLARI ====================
                case CustomerType.L2_OfficeWorker:
                    CreateBlock(parent, "Tie", new Vector3(0f, 1.15f, 0.16f), new Vector3(0.08f, 0.35f, 0.02f), hatRed);
                    CreateBlock(parent, "Briefcase", new Vector3(0.35f, 0.60f, 0f), new Vector3(0.08f, 0.30f, 0.40f), hatBlack);
                    break;

                case CustomerType.L2_HipsterGuy:
                    CreateBlock(parent, "Beanie", new Vector3(0f, 1.72f, 0f), new Vector3(0.34f, 0.18f, 0.32f), hatRed);
                    CreateBlock(parent, "Glasses", new Vector3(0f, 1.55f, 0.16f), new Vector3(0.28f, 0.08f, 0.04f), hatBlack);
                    break;

                case CustomerType.L2_DeliveryCourier:
                    CreateBlock(parent, "Courier_Helmet", new Vector3(0f, 1.72f, 0f), new Vector3(0.38f, 0.22f, 0.38f), hatRed);
                    break;

                case CustomerType.L2_TouristGuy:
                    CreateBlock(parent, "Camera", new Vector3(0f, 1.10f, 0.18f), new Vector3(0.20f, 0.14f, 0.12f), hatBlack);
                    break;

                case CustomerType.L2_DoctorWoman:
                    CreateBlock(parent, "Stethoscope", new Vector3(0f, 1.20f, 0.16f), new Vector3(0.25f, 0.25f, 0.04f), hatBlack);
                    break;

                case CustomerType.L2_ChefMaster:
                    // Uzun Aşçı Şapkası (Toque) & Kırmızı Fular
                    CreateBlock(parent, "Chef_Hat_Base", new Vector3(0f, 1.70f, 0f), new Vector3(0.34f, 0.08f, 0.34f), hatWhite);
                    CreateBlock(parent, "Chef_Hat_Toque", new Vector3(0f, 1.95f, 0f), new Vector3(0.38f, 0.42f, 0.38f), hatWhite);
                    CreateBlock(parent, "Chef_Scarf", new Vector3(0f, 1.30f, 0.16f), new Vector3(0.16f, 0.12f, 0.04f), hatRed);
                    break;

                case CustomerType.L2_YogaInstructor:
                    // Rulo Yoga Matı & Atkuyruğu Saç
                    CreateBlock(parent, "Yoga_Mat", new Vector3(-0.35f, 0.95f, 0f), new Vector3(0.14f, 0.55f, 0.14f), turquoiseMat);
                    CreateBlock(parent, "Ponytail", new Vector3(0f, 1.62f, -0.20f), new Vector3(0.12f, 0.28f, 0.12f), hairBlonde);
                    break;

                case CustomerType.L2_ArchitectGuy:
                    // Modern Tasarım Gözlük & Çizim Tüpü
                    CreateBlock(parent, "Arch_Glasses", new Vector3(0f, 1.55f, 0.16f), new Vector3(0.28f, 0.07f, 0.04f), hatBlack);
                    CreateBlock(parent, "Blueprint_Tube", new Vector3(0.10f, 1.15f, -0.22f), new Vector3(0.10f, 0.65f, 0.10f), hatBlack);
                    CreateBlock(parent, "Tube_Cap", new Vector3(0.10f, 1.48f, -0.22f), new Vector3(0.11f, 0.06f, 0.11f), hatRed);
                    break;

                case CustomerType.L2_BaristaGirl:
                    // Deri Önlük & Bandana
                    CreateBlock(parent, "Bandana", new Vector3(0f, 1.68f, 0f), new Vector3(0.35f, 0.08f, 0.35f), rubyMat);
                    CreateBlock(parent, "Leather_Apron", new Vector3(0f, 1.05f, 0.16f), new Vector3(0.38f, 0.45f, 0.02f), leatherBrown);
                    break;

                case CustomerType.L2_Veterinarian:
                    // Steteskop & Medikal Dosya
                    CreateBlock(parent, "Vet_Stethoscope", new Vector3(0f, 1.20f, 0.16f), new Vector3(0.25f, 0.25f, 0.04f), hatBlack);
                    CreateBlock(parent, "Clipboard", new Vector3(0.35f, 0.90f, 0.10f), new Vector3(0.06f, 0.28f, 0.22f), hatWhite);
                    break;

                // ==================== LEVEL 3 AKSESUARLARI ====================
                case CustomerType.L3_RichGentleman:
                    CreateBlock(parent, "TopHat_Base", new Vector3(0f, 1.70f, 0f), new Vector3(0.48f, 0.05f, 0.48f), hatBlack);
                    CreateBlock(parent, "TopHat_Crown", new Vector3(0f, 1.90f, 0f), new Vector3(0.32f, 0.35f, 0.32f), hatBlack);
                    CreateBlock(parent, "Gold_Cane", new Vector3(0.35f, 0.50f, 0.15f), new Vector3(0.05f, 0.90f, 0.05f), goldMat);
                    break;

                case CustomerType.L3_GamerPro:
                    GameObject headset = CreateBlock(parent, "Headset_Band", new Vector3(0f, 1.72f, 0f), new Vector3(0.36f, 0.06f, 0.20f), hatBlack);
                    CreateBlock(headset.transform, "Ear_L", new Vector3(-0.18f, -0.15f, 0f), new Vector3(0.08f, 0.15f, 0.15f), hatRed);
                    CreateBlock(headset.transform, "Ear_R", new Vector3(0.18f, -0.15f, 0f), new Vector3(0.08f, 0.15f, 0.15f), hatRed);
                    break;

                case CustomerType.L3_PilotMan:
                    CreateBlock(parent, "Pilot_Cap", new Vector3(0f, 1.70f, 0.04f), new Vector3(0.36f, 0.12f, 0.38f), hatBlack);
                    CreateBlock(parent, "Gold_Badge", new Vector3(0f, 1.72f, 0.20f), new Vector3(0.10f, 0.06f, 0.02f), goldMat);
                    break;

                case CustomerType.L3_GoldChainRapper:
                    CreateBlock(parent, "Gold_Chain", new Vector3(0f, 1.25f, 0.16f), new Vector3(0.26f, 0.08f, 0.04f), goldMat);
                    CreateBlock(parent, "Sunglasses", new Vector3(0f, 1.55f, 0.16f), new Vector3(0.28f, 0.07f, 0.04f), glassLensMat);
                    break;

                case CustomerType.L3_BillionaireYacht:
                    CreateBlock(parent, "Yacht_Cap", new Vector3(0f, 1.70f, 0.04f), new Vector3(0.36f, 0.12f, 0.38f), hatWhite);
                    break;

                case CustomerType.L3_GourmetCritic:
                    // İpek Fular & Gurme Not Defteri
                    CreateBlock(parent, "Silk_Ascot", new Vector3(0f, 1.30f, 0.16f), new Vector3(0.18f, 0.18f, 0.04f), rubyMat);
                    CreateBlock(parent, "Critic_Notebook", new Vector3(0.35f, 0.90f, 0.10f), new Vector3(0.06f, 0.22f, 0.16f), leatherBrown);
                    CreateBlock(parent, "Critic_Hair", new Vector3(0f, 1.70f, 0f), new Vector3(0.35f, 0.12f, 0.35f), hairGray);
                    break;

                case CustomerType.L3_Supermodel:
                    // Pırlanta Küpeler & Lüks Çanta
                    CreateBlock(parent, "Earring_L", new Vector3(-0.18f, 1.48f, 0f), new Vector3(0.04f, 0.12f, 0.04f), pearlMat);
                    CreateBlock(parent, "Earring_R", new Vector3(0.18f, 1.48f, 0f), new Vector3(0.04f, 0.12f, 0.04f), pearlMat);
                    CreateBlock(parent, "Designer_Clutch", new Vector3(0.35f, 0.75f, 0f), new Vector3(0.08f, 0.16f, 0.24f), goldMat);
                    CreateBlock(parent, "Model_Hair", new Vector3(0f, 1.68f, -0.05f), new Vector3(0.34f, 0.22f, 0.32f), hairBlack);
                    break;

                case CustomerType.L3_TechInvestor:
                    // Titanyum Akıllı Saat & İnce Çerçeveli Gözlük
                    CreateBlock(parent, "Smart_Glasses", new Vector3(0f, 1.55f, 0.16f), new Vector3(0.28f, 0.06f, 0.03f), glassLensMat);
                    CreateBlock(parent, "Smart_Watch", new Vector3(-0.35f, 0.85f, 0f), new Vector3(0.18f, 0.06f, 0.18f), hatBlack);
                    break;

                case CustomerType.L3_OperaSinger:
                    // Papyon & Smokin Yaka Detayı
                    CreateBlock(parent, "BowTie", new Vector3(0f, 1.34f, 0.16f), new Vector3(0.18f, 0.08f, 0.04f), hatBlack);
                    CreateBlock(parent, "Shirt_Pleats", new Vector3(0f, 1.15f, 0.16f), new Vector3(0.20f, 0.30f, 0.02f), hatWhite);
                    CreateBlock(parent, "Rose_Boutonniere", new Vector3(0.14f, 1.25f, 0.16f), new Vector3(0.06f, 0.06f, 0.04f), hatRed);
                    break;

                case CustomerType.L3_LuxuryCollector:
                    // Üç Sıra İnci Kolye & Yakutlu Altın Baston
                    CreateBlock(parent, "Pearl_Necklace", new Vector3(0f, 1.28f, 0.16f), new Vector3(0.24f, 0.10f, 0.04f), pearlMat);
                    CreateBlock(parent, "Ruby_Cane_Shaft", new Vector3(0.35f, 0.50f, 0.15f), new Vector3(0.05f, 0.90f, 0.05f), hatBlack);
                    CreateBlock(parent, "Ruby_Cane_Head", new Vector3(0.35f, 0.96f, 0.15f), new Vector3(0.09f, 0.09f, 0.09f), rubyMat);
                    break;

                default:
                    // Standart Saç
                    CreateBlock(parent, "Hair", new Vector3(0f, 1.70f, -0.02f), new Vector3(0.34f, 0.10f, 0.32f), hairBrown);
                    break;
            }
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = localScale;

            if (mat != null)
            {
                cube.GetComponent<Renderer>().sharedMaterial = mat;
            }

            Collider col = cube.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            return cube;
        }

        private static GameObject CreateLimb(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material pantsMat, Material shoeMat)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPos + new Vector3(0f, localScale.y * 0.5f, 0f);

            GameObject legMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legMesh.name = name + "_Mesh";
            legMesh.transform.SetParent(pivot.transform, false);
            legMesh.transform.localPosition = new Vector3(0f, -localScale.y * 0.5f, 0f);
            legMesh.transform.localScale = localScale;

            if (pantsMat != null) legMesh.GetComponent<Renderer>().sharedMaterial = pantsMat;

            Collider col = legMesh.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            // Ayakkabı
            GameObject shoe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shoe.name = "Shoe";
            shoe.transform.SetParent(legMesh.transform, false);
            shoe.transform.localPosition = new Vector3(0f, -0.45f, 0.15f);
            shoe.transform.localScale = new Vector3(1.05f, 0.18f, 1.4f);

            if (shoeMat != null) shoe.GetComponent<Renderer>().sharedMaterial = shoeMat;

            Collider sCol = shoe.GetComponent<Collider>();
            if (sCol != null) Object.Destroy(sCol);

            return pivot;
        }

        private static GameObject CreateArm(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material sleeveMat, Material skinMat)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPos + new Vector3(0f, localScale.y * 0.5f, 0f);

            GameObject armMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armMesh.name = name + "_Mesh";
            armMesh.transform.SetParent(pivot.transform, false);
            armMesh.transform.localPosition = new Vector3(0f, -localScale.y * 0.5f, 0f);
            armMesh.transform.localScale = localScale;

            if (sleeveMat != null) armMesh.GetComponent<Renderer>().sharedMaterial = sleeveMat;

            Collider col = armMesh.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            // El
            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hand.name = "Hand";
            hand.transform.SetParent(armMesh.transform, false);
            hand.transform.localPosition = new Vector3(0f, -0.48f, 0f);
            hand.transform.localScale = new Vector3(0.9f, 0.20f, 0.9f);

            if (skinMat != null) hand.GetComponent<Renderer>().sharedMaterial = skinMat;

            Collider hCol = hand.GetComponent<Collider>();
            if (hCol != null) Object.Destroy(hCol);

            return pivot;
        }
    }
}
