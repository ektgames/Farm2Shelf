using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    public static class ProceduralStaffModelBuilder
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

        public static GameObject CreateStaffModel(StaffRole role, bool isFemale, out List<Transform> leftLimbs, out List<Transform> rightLimbs)
        {
            leftLimbs = new List<Transform>();
            rightLimbs = new List<Transform>();

            string genderPrefix = isFemale ? "Female_" : "Male_";
            GameObject root = new GameObject($"Staff_{genderPrefix}{role}");
            Transform tRoot = root.transform;

            // Fiziksel Çarpışma ve Kapı Algılama Bileşenleri
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.35f;
            col.height = 1.8f;
            col.isTrigger = true;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Ten & Saç Materyalleri
            Material skinMat = isFemale ? GetMaterial("Mat_StaffSkinF", new Color(0.95f, 0.80f, 0.68f)) : GetMaterial("Mat_StaffSkinM", new Color(0.88f, 0.72f, 0.58f));
            Material hairMat = isFemale ? GetMaterial("Mat_StaffHairF", new Color(0.35f, 0.22f, 0.14f)) : GetMaterial("Mat_StaffHairM", new Color(0.12f, 0.12f, 0.14f));
            Material shoeMat = GetMaterial("Mat_StaffShoe", new Color(0.12f, 0.12f, 0.14f));

            // Role Özel Üniforma Renkleri
            Color uniformPrimary = GetRolePrimaryColor(role);
            Color uniformPants = GetRolePantsColor(role);

            Material torsoMat = GetMaterial($"Mat_StaffTorso_{role}", uniformPrimary);
            Material pantsMat = GetMaterial($"Mat_StaffPants_{role}", uniformPants);

            // Anatomi Boyutları (Erkek vs Kadın)
            Vector3 torsoSize = isFemale ? new Vector3(0.44f, 0.52f, 0.28f) : new Vector3(0.52f, 0.55f, 0.30f);
            Vector3 headSize = isFemale ? new Vector3(0.30f, 0.30f, 0.28f) : new Vector3(0.32f, 0.32f, 0.30f);

            // Torso (Gövde)
            CreateBlock(tRoot, "Torso", new Vector3(0f, 1.05f, 0f), torsoSize, torsoMat);

            // Baş (Head)
            CreateBlock(tRoot, "Head", new Vector3(0f, 1.50f, 0f), headSize, skinMat);

            // Sol & Sağ Bacak
            float legX = isFemale ? 0.13f : 0.15f;
            GameObject lLeg = CreateLimb(tRoot, "Leg_L", new Vector3(-legX, 0.75f, 0f), new Vector3(0.18f, 0.70f, 0.20f), pantsMat, shoeMat);
            GameObject rLeg = CreateLimb(tRoot, "Leg_R", new Vector3(legX, 0.75f, 0f), new Vector3(0.18f, 0.70f, 0.20f), pantsMat, shoeMat);
            leftLimbs.Add(lLeg.transform);
            rightLimbs.Add(rLeg.transform);

            // Sol & Sağ Kol
            float armX = isFemale ? 0.28f : 0.32f;
            GameObject lArm = CreateArm(tRoot, "Arm_L", new Vector3(-armX, 1.25f, 0f), new Vector3(0.15f, 0.55f, 0.16f), torsoMat, skinMat);
            GameObject rArm = CreateArm(tRoot, "Arm_R", new Vector3(armX, 1.25f, 0f), new Vector3(0.15f, 0.55f, 0.16f), torsoMat, skinMat);
            leftLimbs.Add(lArm.transform);
            rightLimbs.Add(rArm.transform);

            // Saç ve Cinsiyet Detayı
            BuildHairAndHeadwear(tRoot, isFemale, hairMat, role);

            // Role Özel Üniforma ve Aksesuar Detayları
            BuildRoleUniformAccessories(tRoot, role, isFemale, skinMat);

            return root;
        }

        private static Color GetRolePrimaryColor(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.Kasiyer: return new Color(0.15f, 0.45f, 0.85f); // Mağaza Mavi Tişört
                case StaffRole.Reyoncu: return new Color(0.18f, 0.65f, 0.35f); // Mağaza Yeşil Önlük
                case StaffRole.Temizlikçi: return new Color(0.20f, 0.60f, 0.85f); // Mavi Temizlik Tulumu
                case StaffRole.Güvenlik: return new Color(0.12f, 0.14f, 0.18f); // Siyah Özel Güvenlik Üniforması
                case StaffRole.MüşteriHizmetlisi: return new Color(0.96f, 0.96f, 0.98f); // Lüks Beyaz Takım Elbise / Beyaz Elbise
                case StaffRole.Maskot: return new Color(0.95f, 0.75f, 0.15f); // Sarı Maskot Kostümü
                case StaffRole.Çiftçi: return new Color(0.45f, 0.35f, 0.20f); // Kahverengi Tulum
                case StaffRole.DeneyimliÇiftçi: return new Color(0.25f, 0.45f, 0.25f); // Yeşil Çiftçi Tulumu
                case StaffRole.UstaÇiftlikSorumlusu: return new Color(0.80f, 0.40f, 0.15f); // Turuncu Tulum
                case StaffRole.TarımOtomasyonUzmanı: return new Color(0.20f, 0.30f, 0.50f); // Mavi Mühendis Ceketi
                default: return Color.blue;
            }
        }

        private static Color GetRolePantsColor(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.Kasiyer: return new Color(0.15f, 0.18f, 0.25f); // Siyah Kumaş Pantolon
                case StaffRole.Reyoncu: return new Color(0.20f, 0.35f, 0.60f); // Kot Pantolon
                case StaffRole.Güvenlik: return new Color(0.10f, 0.12f, 0.15f); // Siyah Güvenlik Pantolonu
                case StaffRole.MüşteriHizmetlisi: return new Color(0.96f, 0.96f, 0.98f); // Beyaz Kumaş Pantolon / Etek
                case StaffRole.Maskot: return new Color(0.95f, 0.75f, 0.15f);
                default: return new Color(0.18f, 0.20f, 0.25f);
            }
        }

        private static void BuildHairAndHeadwear(Transform parent, bool isFemale, Material hairMat, StaffRole role)
        {
            if (role == StaffRole.Maskot) return; // Maskot özel kafa kullanır

            if (isFemale)
            {
                // Kadın Saçı (At kuyruğu & Yan Saçlar)
                CreateBlock(parent, "Hair_Top", new Vector3(0f, 1.68f, -0.02f), new Vector3(0.32f, 0.08f, 0.30f), hairMat);
                CreateBlock(parent, "Ponytail", new Vector3(0f, 1.55f, -0.18f), new Vector3(0.12f, 0.30f, 0.12f), hairMat);
            }
            else
            {
                // Erkek Saçı (Kısa Saç)
                CreateBlock(parent, "Hair_Short", new Vector3(0f, 1.68f, -0.01f), new Vector3(0.33f, 0.08f, 0.31f), hairMat);
            }
        }

        private static void BuildRoleUniformAccessories(Transform parent, StaffRole role, bool isFemale, Material skinMat)
        {
            Material goldBadge = GetMaterial("Mat_GoldBadge", new Color(0.95f, 0.80f, 0.15f), 0.9f, 0.9f);
            Material nameTagMat = GetMaterial("Mat_NameTag", Color.white);
            Material blackAcc = GetMaterial("Mat_AccBlack", new Color(0.12f, 0.12f, 0.14f));
            Material yellowGloves = GetMaterial("Mat_YellowGloves", new Color(0.95f, 0.85f, 0.10f));

            switch (role)
            {
                case StaffRole.Kasiyer:
                    // Yaka İsim Kartı & Kulaklık Mikrofon
                    CreateBlock(parent, "NameTag", new Vector3(0.12f, 1.22f, 0.16f), new Vector3(0.10f, 0.08f, 0.02f), nameTagMat);
                    GameObject headset = CreateBlock(parent, "Headset", new Vector3(0f, 1.66f, 0f), new Vector3(0.34f, 0.04f, 0.20f), blackAcc);
                    CreateBlock(headset.transform, "Mic", new Vector3(-0.16f, -0.10f, 0.12f), new Vector3(0.04f, 0.04f, 0.15f), blackAcc);
                    break;

                case StaffRole.Reyoncu:
                    // Ön Lojistik Önlük & Taşıma Kemer Bel Çantası
                    CreateBlock(parent, "Apron", new Vector3(0f, 1.05f, 0.16f), new Vector3(0.42f, 0.45f, 0.02f), GetMaterial("Mat_ApronGreen", new Color(0.18f, 0.65f, 0.35f)));
                    CreateBlock(parent, "ToolBelt", new Vector3(0f, 0.82f, 0f), new Vector3(0.54f, 0.08f, 0.32f), blackAcc);
                    break;

                case StaffRole.Temizlikçi:
                    // Temizlik Eldivenleri & Paspas Mop Tutucu
                    CreateBlock(parent, "Glove_L", new Vector3(-0.25f, 0.92f, 0f), new Vector3(0.16f, 0.18f, 0.17f), yellowGloves);
                    CreateBlock(parent, "Glove_R", new Vector3(0.25f, 0.92f, 0f), new Vector3(0.16f, 0.18f, 0.17f), yellowGloves);
                    CreateBlock(parent, "MopHandle", new Vector3(0.30f, 0.90f, 0.15f), new Vector3(0.05f, 1.20f, 0.05f), GetMaterial("Mat_MopStick", new Color(0.70f, 0.70f, 0.75f)));
                    break;

                case StaffRole.Güvenlik:
                    // Özel Güvenlik Şapkası, Göğüs Arması & Telsiz
                    CreateBlock(parent, "Sec_Cap", new Vector3(0f, 1.68f, 0.04f), new Vector3(0.36f, 0.10f, 0.38f), blackAcc);
                    CreateBlock(parent, "Sec_Badge", new Vector3(-0.12f, 1.22f, 0.16f), new Vector3(0.08f, 0.10f, 0.02f), goldBadge);
                    CreateBlock(parent, "WalkieTalkie", new Vector3(0.24f, 1.08f, 0.16f), new Vector3(0.08f, 0.16f, 0.06f), blackAcc);
                    break;

                case StaffRole.MüşteriHizmetlisi:
                    // Beyaz Takım Elbise / Beyaz Elbise Aksesuarları
                    Material redSilk = GetMaterial("Mat_RedSilkTie", new Color(0.85f, 0.20f, 0.20f));
                    CreateBlock(parent, "GoldBadge", new Vector3(-0.12f, 1.22f, 0.16f), new Vector3(0.08f, 0.10f, 0.02f), goldBadge);

                    if (isFemale)
                    {
                        // Kadın: Beyaz Elbise Şık Kırmızı Fular
                        CreateBlock(parent, "SilkScarf", new Vector3(0f, 1.34f, 0.15f), new Vector3(0.14f, 0.12f, 0.04f), redSilk);
                    }
                    else
                    {
                        // Erkek: Beyaz Takım Elbise Kırmızı Kravatı
                        CreateBlock(parent, "SuitTie", new Vector3(0f, 1.15f, 0.16f), new Vector3(0.06f, 0.30f, 0.02f), redSilk);
                    }

                    // Şık Danışma Kulaklığı & Mikrofonu
                    GameObject custHeadset = CreateBlock(parent, "HeadsetCS", new Vector3(0f, 1.66f, 0f), new Vector3(0.34f, 0.04f, 0.20f), blackAcc);
                    CreateBlock(custHeadset.transform, "MicCS", new Vector3(-0.16f, -0.10f, 0.12f), new Vector3(0.04f, 0.04f, 0.15f), blackAcc);
                    break;

                case StaffRole.Maskot:
                    if (isFemale)
                    {
                        // 🐰 KADIN: SEVİMLİ PEMBE TAVŞAN MASKOT KOSTÜMÜ
                        Material rabbitBodyMat = GetMaterial("Mat_MascotRabbitPink", new Color(0.98f, 0.68f, 0.78f));
                        Material rabbitWhiteMat = GetMaterial("Mat_MascotRabbitWhite", Color.white);
                        Material rabbitInnerEarMat = GetMaterial("Mat_MascotRabbitInnerEar", new Color(0.98f, 0.85f, 0.90f));
                        Material redBowTieMat = GetMaterial("Mat_MascotRabbitBow", new Color(0.85f, 0.18f, 0.25f));
                        Material blackEyesMat = GetMaterial("Mat_MascotBlackEyes", new Color(0.10f, 0.10f, 0.12f));

                        // 1. Tavşan Maskot Kafası (Kocaman Sevimli Peluş Kafa)
                        GameObject rHead = CreateBlock(parent, "Rabbit_Head_Mascot", new Vector3(0f, 1.52f, 0.02f), new Vector3(0.44f, 0.42f, 0.42f), rabbitBodyMat);

                        // 2. Dik Duran Sevimli Tavşan Kulakları (Sol ve Sağ)
                        GameObject earL = CreateBlock(rHead.transform, "Rabbit_Ear_L", new Vector3(-0.14f, 0.32f, 0f), new Vector3(0.10f, 0.50f, 0.08f), rabbitBodyMat);
                        earL.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
                        CreateBlock(earL.transform, "Inner_L", new Vector3(0f, 0f, 0.03f), new Vector3(0.06f, 0.42f, 0.03f), rabbitInnerEarMat);

                        GameObject earR = CreateBlock(rHead.transform, "Rabbit_Ear_R", new Vector3(0.14f, 0.32f, 0f), new Vector3(0.10f, 0.50f, 0.08f), rabbitBodyMat);
                        earR.transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
                        CreateBlock(earR.transform, "Inner_R", new Vector3(0f, 0f, 0.03f), new Vector3(0.06f, 0.42f, 0.03f), rabbitInnerEarMat);

                        // 3. Ağız, Burun ve Gözler
                        CreateBlock(rHead.transform, "Rabbit_Snout", new Vector3(0f, -0.06f, 0.20f), new Vector3(0.20f, 0.14f, 0.10f), rabbitWhiteMat);
                        CreateBlock(rHead.transform, "Rabbit_Nose", new Vector3(0f, -0.02f, 0.24f), new Vector3(0.06f, 0.05f, 0.04f), rabbitInnerEarMat);
                        CreateBlock(rHead.transform, "Eye_L", new Vector3(-0.12f, 0.06f, 0.20f), new Vector3(0.06f, 0.08f, 0.04f), blackEyesMat);
                        CreateBlock(rHead.transform, "Eye_R", new Vector3(0.12f, 0.06f, 0.20f), new Vector3(0.06f, 0.08f, 0.04f), blackEyesMat);

                        // 4. Göğüs Beyaz Göbek Yaması & Kırmızı Fiyonk Kravat
                        CreateBlock(parent, "Rabbit_Belly", new Vector3(0f, 1.05f, 0.16f), new Vector3(0.34f, 0.42f, 0.03f), rabbitWhiteMat);
                        CreateBlock(parent, "Rabbit_Bow", new Vector3(0f, 1.28f, 0.18f), new Vector3(0.20f, 0.10f, 0.04f), redBowTieMat);

                        // 5. Arka Pofuduk Tavşan Kuyruğu
                        CreateBlock(parent, "Rabbit_Tail", new Vector3(0f, 0.78f, -0.18f), new Vector3(0.18f, 0.18f, 0.18f), rabbitWhiteMat);
                    }
                    else
                    {
                        // 🐻 ERKEK: SEVİMLİ KAHVERENGİ AYI MASKOT KOSTÜMÜ
                        Material bearBodyMat = GetMaterial("Mat_MascotBearBrown", new Color(0.58f, 0.36f, 0.18f));
                        Material bearSnoutMat = GetMaterial("Mat_MascotBearCream", new Color(0.95f, 0.85f, 0.68f));
                        Material blackNoseMat = GetMaterial("Mat_MascotBlackNose", new Color(0.10f, 0.10f, 0.12f));

                        // 1. Ayı Maskot Kafası (Büyük Sevimli Peluş Kafa)
                        GameObject bHead = CreateBlock(parent, "Bear_Head_Mascot", new Vector3(0f, 1.52f, 0.02f), new Vector3(0.48f, 0.44f, 0.44f), bearBodyMat);

                        // 2. Yuvarlak Pofuduk Ayı Kulakları (Sol ve Sağ)
                        GameObject bEarL = CreateBlock(bHead.transform, "Bear_Ear_L", new Vector3(-0.20f, 0.22f, 0f), new Vector3(0.14f, 0.14f, 0.08f), bearBodyMat);
                        CreateBlock(bEarL.transform, "Inner_L", new Vector3(0f, 0f, 0.03f), new Vector3(0.08f, 0.08f, 0.03f), bearSnoutMat);

                        GameObject bEarR = CreateBlock(bHead.transform, "Bear_Ear_R", new Vector3(0.20f, 0.22f, 0f), new Vector3(0.14f, 0.14f, 0.08f), bearBodyMat);
                        CreateBlock(bEarR.transform, "Inner_R", new Vector3(0f, 0f, 0.03f), new Vector3(0.08f, 0.08f, 0.03f), bearSnoutMat);

                        // 3. Çıkıntılı Ayı Ağız Ağızlığı & Siyah Burun & Siyah Gözler
                        CreateBlock(bHead.transform, "Bear_Snout", new Vector3(0f, -0.06f, 0.22f), new Vector3(0.24f, 0.16f, 0.12f), bearSnoutMat);
                        CreateBlock(bHead.transform, "Bear_Nose", new Vector3(0f, -0.01f, 0.27f), new Vector3(0.08f, 0.06f, 0.05f), blackNoseMat);
                        CreateBlock(bHead.transform, "Eye_L", new Vector3(-0.14f, 0.08f, 0.21f), new Vector3(0.07f, 0.07f, 0.04f), blackNoseMat);
                        CreateBlock(bHead.transform, "Eye_R", new Vector3(0.14f, 0.08f, 0.21f), new Vector3(0.07f, 0.07f, 0.04f), blackNoseMat);

                        // 4. Krem Rengi Ayı Göbek Yaması
                        CreateBlock(parent, "Bear_Belly", new Vector3(0f, 1.05f, 0.16f), new Vector3(0.38f, 0.44f, 0.03f), bearSnoutMat);

                        // 5. Arka Yuvarlak Ayı Kuyruğu
                        CreateBlock(parent, "Bear_Tail", new Vector3(0f, 0.78f, -0.18f), new Vector3(0.16f, 0.16f, 0.16f), bearBodyMat);
                    }
                    break;

                case StaffRole.Çiftçi:
                case StaffRole.DeneyimliÇiftçi:
                case StaffRole.UstaÇiftlikSorumlusu:
                    // Çiftçi Hasır Şapkası
                    CreateBlock(parent, "Farmer_Hat", new Vector3(0f, 1.68f, 0f), new Vector3(0.55f, 0.08f, 0.55f), GetMaterial("Mat_HatStraw", new Color(0.90f, 0.80f, 0.25f)));
                    break;

                case StaffRole.TarımOtomasyonUzmanı:
                    // Mühendis Baret & Akıllı Tablet
                    CreateBlock(parent, "Eng_Helmet", new Vector3(0f, 1.68f, 0f), new Vector3(0.36f, 0.12f, 0.36f), GetMaterial("Mat_HelmetWhite", Color.white));
                    CreateBlock(parent, "Tablet", new Vector3(0.25f, 1.0f, 0.15f), new Vector3(0.04f, 0.25f, 0.32f), blackAcc);
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
            shoe.transform.localPosition = new Vector3(0f, -0.45f, 0.12f);
            shoe.transform.localScale = new Vector3(1.05f, 0.18f, 1.3f);

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
