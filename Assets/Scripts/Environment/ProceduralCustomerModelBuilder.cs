using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    public enum CustomerType
    {
        // ==================== SEVİYE 1 MÜŞTERİLER (10 ADET) ====================
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

        // ==================== SEVİYE 2 MÜŞTERİLER (10 ADET) ====================
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

        // ==================== SEVİYE 3 MÜŞTERİLER (10 ADET) ====================
        L3_CEO_Executive,      // Lüks takım elbiseli CEO
        L3_VIP_Influencer,     // Şapkalı stil sahibi influencer
        L3_RichGentleman,      // Silindir şapkalı zengin bey
        L3_BoutiqueLady,       // Kürk yakalı şık leydi
        L3_GamerPro,           // Işıklı kulaklıklı oyuncu
        L3_CelebrityActor,     // Şık ceketli ünlü aktör
        L3_PilotMan,           // Kaptan pilot üniformalı adam
        L3_GoldChainRapper,    // Altın kolyeli tarz müzisyen
        L3_JewelryLady,        // Gece elbiseli mücevherli hanımefendi
        L3_BillionaireYacht    // Yat kaptanı milyarder
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
                case CustomerType.L2_OfficeWorker: return new Color(0.15f, 0.18f, 0.25f); // Siyah Kumaş Pantolon
                case CustomerType.L2_GymBro: return new Color(0.85f, 0.20f, 0.20f); // Spor Şort
                case CustomerType.L3_CEO_Executive: return new Color(0.12f, 0.14f, 0.18f);
                case CustomerType.L3_GoldChainRapper: return new Color(0.12f, 0.12f, 0.14f);
                default: return new Color(0.20f, 0.22f, 0.28f);
            }
        }

        private static void BuildCustomerAccessories(CustomerType type, Transform parent, Material skin, Material hairBlack, Material hairBrown, Material hairBlonde, Material hairGray)
        {
            Material hatRed = GetMaterial("Mat_HatRed", new Color(0.85f, 0.15f, 0.15f));
            Material hatYellow = GetMaterial("Mat_HatYellow", new Color(0.95f, 0.85f, 0.10f));
            Material hatBlack = GetMaterial("Mat_HatBlack", new Color(0.12f, 0.12f, 0.14f));
            Material goldMat = GetMaterial("Mat_CustGold", new Color(0.95f, 0.80f, 0.15f), 0.9f, 0.9f);
            Material glassLensMat = GetMaterial("Mat_CustLens", new Color(0.2f, 0.2f, 0.25f, 0.8f), 0.9f, 0.9f);

            switch (type)
            {
                case CustomerType.L1_FarmerUncle:
                    // Hasır Şapka
                    CreateBlock(parent, "Farmer_Hat", new Vector3(0f, 1.70f, 0f), new Vector3(0.55f, 0.08f, 0.55f), hatYellow);
                    break;

                case CustomerType.L1_StudentGirl:
                    // Sırt Çantası
                    CreateBlock(parent, "Backpack", new Vector3(0f, 1.05f, -0.22f), new Vector3(0.35f, 0.40f, 0.16f), hatRed);
                    break;

                case CustomerType.L1_Workman:
                    // Sarı Baret
                    CreateBlock(parent, "Safety_Helmet", new Vector3(0f, 1.72f, 0f), new Vector3(0.38f, 0.15f, 0.38f), hatYellow);
                    break;

                case CustomerType.L1_GrandpaDede:
                    // Kasket & Baston
                    CreateBlock(parent, "Cap", new Vector3(0f, 1.70f, 0.05f), new Vector3(0.35f, 0.10f, 0.40f), hairGray);
                    CreateBlock(parent, "Cane", new Vector3(0.35f, 0.50f, 0.15f), new Vector3(0.05f, 0.90f, 0.05f), hairBrown);
                    break;

                case CustomerType.L2_OfficeWorker:
                    // Kravat & Çanta
                    CreateBlock(parent, "Tie", new Vector3(0f, 1.15f, 0.16f), new Vector3(0.08f, 0.35f, 0.02f), hatRed);
                    CreateBlock(parent, "Briefcase", new Vector3(0.35f, 0.60f, 0f), new Vector3(0.08f, 0.30f, 0.40f), hatBlack);
                    break;

                case CustomerType.L2_HipsterGuy:
                    // Bere & Gözlük
                    CreateBlock(parent, "Beanie", new Vector3(0f, 1.72f, 0f), new Vector3(0.34f, 0.18f, 0.32f), hatRed);
                    CreateBlock(parent, "Glasses", new Vector3(0f, 1.55f, 0.16f), new Vector3(0.28f, 0.08f, 0.04f), hatBlack);
                    break;

                case CustomerType.L2_DeliveryCourier:
                    // Kask
                    CreateBlock(parent, "Courier_Helmet", new Vector3(0f, 1.72f, 0f), new Vector3(0.38f, 0.22f, 0.38f), hatRed);
                    break;

                case CustomerType.L2_TouristGuy:
                    // Kamera
                    CreateBlock(parent, "Camera", new Vector3(0f, 1.10f, 0.18f), new Vector3(0.20f, 0.14f, 0.12f), hatBlack);
                    break;

                case CustomerType.L2_DoctorWoman:
                    // Steteskop
                    CreateBlock(parent, "Stethoscope", new Vector3(0f, 1.20f, 0.16f), new Vector3(0.25f, 0.25f, 0.04f), hatBlack);
                    break;

                case CustomerType.L3_RichGentleman:
                    // Silindir Şapka & Baston
                    CreateBlock(parent, "TopHat_Base", new Vector3(0f, 1.70f, 0f), new Vector3(0.48f, 0.05f, 0.48f), hatBlack);
                    CreateBlock(parent, "TopHat_Crown", new Vector3(0f, 1.90f, 0f), new Vector3(0.32f, 0.35f, 0.32f), hatBlack);
                    CreateBlock(parent, "Gold_Cane", new Vector3(0.35f, 0.50f, 0.15f), new Vector3(0.05f, 0.90f, 0.05f), goldMat);
                    break;

                case CustomerType.L3_GamerPro:
                    // Işıklı Kulaklık
                    GameObject headset = CreateBlock(parent, "Headset_Band", new Vector3(0f, 1.72f, 0f), new Vector3(0.36f, 0.06f, 0.20f), hatBlack);
                    CreateBlock(headset.transform, "Ear_L", new Vector3(-0.18f, -0.15f, 0f), new Vector3(0.08f, 0.15f, 0.15f), hatRed);
                    CreateBlock(headset.transform, "Ear_R", new Vector3(0.18f, -0.15f, 0f), new Vector3(0.08f, 0.15f, 0.15f), hatRed);
                    break;

                case CustomerType.L3_PilotMan:
                    // Pilot Kasketi
                    CreateBlock(parent, "Pilot_Cap", new Vector3(0f, 1.70f, 0.04f), new Vector3(0.36f, 0.12f, 0.38f), hatBlack);
                    CreateBlock(parent, "Gold_Badge", new Vector3(0f, 1.72f, 0.20f), new Vector3(0.10f, 0.06f, 0.02f), goldMat);
                    break;

                case CustomerType.L3_GoldChainRapper:
                    // Altın Kolye & Güneş Gözlüğü
                    CreateBlock(parent, "Gold_Chain", new Vector3(0f, 1.25f, 0.16f), new Vector3(0.26f, 0.08f, 0.04f), goldMat);
                    CreateBlock(parent, "Sunglasses", new Vector3(0f, 1.55f, 0.16f), new Vector3(0.28f, 0.07f, 0.04f), glassLensMat);
                    break;

                case CustomerType.L3_BillionaireYacht:
                    // Kaptan Şapkası
                    CreateBlock(parent, "Yacht_Cap", new Vector3(0f, 1.70f, 0.04f), new Vector3(0.36f, 0.12f, 0.38f), GetMaterial("Mat_CapWhite", Color.white));
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
