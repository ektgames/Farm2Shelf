using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Kod ile 10 farklı mobilya türü için gerçekçi, yüksek detaylı ve 3D prosedürel modeller üreten yardımcı sınıf.
    /// Ghost (Önizleme) modu için yeşil/kırmızı şeffaf materyal atamasını destekler.
    /// </summary>
    public static class FurnitureModelBuilder
    {
        // Standart materyal önbelleği
        private static Material woodMat;
        private static Material darkWoodMat;
        private static Material metalMat;
        private static Material orangeMetalMat;
        private static Material steelMat;
        private static Material glassMat;
        private static Material whiteGlossMat;
        private static Material cyanLedMat;
        private static Material pinkLedMat;
        private static Material blackMat;
        private static Material redAccentMat;
        private static Material greenProduceMat;
        private static Material goldMat;
        private static Material blackMetalMat;
        private static Material silverMetalMat;

        public static Material ValidGhostMaterial { get; private set; }
        public static Material InvalidGhostMaterial { get; private set; }
        public static Material CardboardBoxMaterial { get; private set; }
        public static Material RedAccentMaterial => redAccentMat;
        public static Material BlackMaterial => blackMat;
        public static Material GoldMaterial => goldMat;

        private static Shader GetSmartShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Lightweight Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Unlit/Color");
            return s;
        }

        private static Material CreateSmartMaterial(string name, Color color, bool isTransparent = false)
        {
            Shader shader = GetSmartShader();
            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (isTransparent || color.a < 1.0f)
            {
                mat.SetFloat("_Surface", 1); // URP Transparent
                mat.SetFloat("_Blend", 0);   // URP Alpha
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            return mat;
        }

        private static Material CreateGhostMaterial(string name, Color color)
        {
            // URP Unlit veya Transparent shader'ı öncelikli olarak seç
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            mat.SetFloat("_Surface", 1); // URP Transparent
            mat.SetFloat("_Blend", 0);   // URP Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;

            return mat;
        }

        private static void InitMaterials()
        {
            if (woodMat != null) return;

            // Ahşap
            woodMat = CreateSmartMaterial("Furniture_Wood", new Color(0.65f, 0.42f, 0.22f));
            darkWoodMat = CreateSmartMaterial("Furniture_DarkWood", new Color(0.35f, 0.20f, 0.10f));

            // Metal & Paslanmaz Çelik
            metalMat = CreateSmartMaterial("Furniture_Metal", new Color(0.22f, 0.25f, 0.30f));
            metalMat.SetFloat("_Metallic", 0.7f);
            metalMat.SetFloat("_Smoothness", 0.5f);

            orangeMetalMat = CreateSmartMaterial("Furniture_OrangeMetal", new Color(0.90f, 0.40f, 0.05f));

            steelMat = CreateSmartMaterial("Furniture_Steel", new Color(0.82f, 0.85f, 0.88f));
            steelMat.SetFloat("_Metallic", 0.9f);
            steelMat.SetFloat("_Smoothness", 0.8f);

            // Cam (Şeffaf)
            glassMat = CreateSmartMaterial("Furniture_Glass", new Color(0.60f, 0.85f, 0.95f, 0.35f), isTransparent: true);

            // Beyaz Parlak
            whiteGlossMat = CreateSmartMaterial("Furniture_WhiteGloss", new Color(0.95f, 0.95f, 0.98f));
            whiteGlossMat.SetFloat("_Smoothness", 0.85f);

            // Siyah
            blackMat = CreateSmartMaterial("Furniture_Black", new Color(0.10f, 0.12f, 0.15f));

            // Kırmızı Accent
            redAccentMat = CreateSmartMaterial("Furniture_RedAccent", new Color(0.85f, 0.15f, 0.20f));

            // Yeşil Manav
            greenProduceMat = CreateSmartMaterial("Furniture_GreenProduce", new Color(0.20f, 0.65f, 0.25f));

            // Altın & Metaller
            goldMat = CreateSmartMaterial("Furniture_Gold", new Color(0.95f, 0.80f, 0.15f));
            goldMat.SetFloat("_Metallic", 0.9f);
            goldMat.SetFloat("_Smoothness", 0.9f);

            blackMetalMat = CreateSmartMaterial("Furniture_BlackMetal", new Color(0.12f, 0.12f, 0.14f));
            blackMetalMat.SetFloat("_Metallic", 0.8f);

            silverMetalMat = CreateSmartMaterial("Furniture_SilverMetal", new Color(0.75f, 0.78f, 0.82f));
            silverMetalMat.SetFloat("_Metallic", 0.85f);

            // LED Işıkları
            cyanLedMat = CreateSmartMaterial("Furniture_CyanLed", new Color(0.10f, 0.85f, 0.95f));
            cyanLedMat.EnableKeyword("_EMISSION");
            cyanLedMat.SetColor("_EmissionColor", new Color(0.05f, 0.60f, 0.80f));

            pinkLedMat = CreateSmartMaterial("Furniture_PinkLed", new Color(0.95f, 0.25f, 0.65f));
            pinkLedMat.EnableKeyword("_EMISSION");
            pinkLedMat.SetColor("_EmissionColor", new Color(0.80f, 0.15f, 0.50f));

            // Ghost (Önizleme) ve Koli Materyalleri - Canlı ve Yüksek Görünürlükte Neon Renkler
            ValidGhostMaterial = CreateGhostMaterial("Ghost_Valid", new Color(0.15f, 1.0f, 0.35f, 0.70f));
            InvalidGhostMaterial = CreateGhostMaterial("Ghost_Invalid", new Color(1.0f, 0.15f, 0.20f, 0.70f));
            CardboardBoxMaterial = CreateSmartMaterial("Furniture_CardboardBox", new Color(0.72f, 0.52f, 0.32f));
        }

        public static GameObject CreateFurnitureModel(FurnitureType type, bool isGhost = false)
        {
            InitMaterials();

            GameObject root = new GameObject("Furniture_" + type.ToString());

            switch (type)
            {
                case FurnitureType.Shelf:
                    BuildStoreShelf(root);
                    break;
                case FurnitureType.ShoppingCart:
                    BuildShoppingCartStand(root);
                    break;
                case FurnitureType.StorageShelf:
                    BuildStorageShelf(root);
                    break;
                case FurnitureType.Cashier:
                    BuildCashierCounter(root);
                    break;
                case FurnitureType.CustomerServiceDesk:
                    BuildCustomerServiceDesk(root);
                    break;
                case FurnitureType.Fridge:
                    BuildCommercialFridge(root);
                    break;
                case FurnitureType.Freezer:
                    BuildChestFreezer(root);
                    break;
                case FurnitureType.CosmeticShelf:
                    BuildCosmeticsShelf(root);
                    break;
                case FurnitureType.BakeryCounter:
                    BuildBakeryCounter(root);
                    break;
                case FurnitureType.ProduceShelf:
                    BuildProduceShelf(root);
                    break;
                case FurnitureType.ButcherCounter:
                    BuildButcherCounter(root);
                    break;
                case FurnitureType.ElectronicsShelf:
                    BuildElectronicsShelf(root);
                    break;

                // --- SEVİYE 1 DEKORASYONLAR (10 Adet) ---
                case FurnitureType.PlantPot: BuildPlantPot(root); break;
                case FurnitureType.PottedPalm: BuildPottedPalm(root); break;
                case FurnitureType.TrashCan: BuildTrashCan(root); break;
                case FurnitureType.BenchWood: BuildBenchWood(root); break;
                case FurnitureType.WelcomeMat: BuildWelcomeMat(root); break;
                case FurnitureType.WallClock: BuildWallClock(root); break;
                case FurnitureType.AdBanner: BuildAdBanner(root); break;
                case FurnitureType.CeilingSpotlight: BuildCeilingSpotlight(root); break;
                case FurnitureType.DividerFence: BuildDividerFence(root); break;
                case FurnitureType.WaterDispenser: BuildWaterDispenser(root); break;

                // --- SEVİYE 2 DEKORASYONLAR (10 Adet) ---
                case FurnitureType.CoffeeMachine: BuildCoffeeMachine(root); break;
                case FurnitureType.NeonSign: BuildNeonSign(root); break;
                case FurnitureType.FountainSmall: BuildFountainSmall(root); break;
                case FurnitureType.GumballMachine: BuildGumballMachine(root); break;
                case FurnitureType.VendingSnack: BuildVendingSnack(root); break;
                case FurnitureType.IceCreamCart: BuildIceCreamCart(root); break;
                case FurnitureType.RedCarpet: BuildRedCarpet(root); break;
                case FurnitureType.DigitalMenuBoard: BuildDigitalMenuBoard(root); break;
                case FurnitureType.BonsaiTree: BuildBonsaiTree(root); break;
                case FurnitureType.AtmMachine: BuildAtmMachine(root); break;

                // --- SEVİYE 3 DEKORASYONLAR (10 Adet) ---
                case FurnitureType.ArcadeMachine: BuildArcadeMachine(root); break;
                case FurnitureType.AquariumGrand: BuildAquariumGrand(root); break;
                case FurnitureType.Jukebox: BuildJukebox(root); break;
                case FurnitureType.GoldenStatue: BuildGoldenStatue(root); break;
                case FurnitureType.ChandelierCrystal: BuildChandelierCrystal(root); break;
                case FurnitureType.SlushieMachine: BuildSlushieMachine(root); break;
                case FurnitureType.MassageChair: BuildMassageChair(root); break;
                case FurnitureType.DonutDispenser: BuildDonutDispenser(root); break;
                case FurnitureType.HologramProjector: BuildHologramProjector(root); break;
                case FurnitureType.FlowerArch: BuildFlowerArch(root); break;
            }

            if (isGhost)
            {
                ApplyGhostMaterial(root, ValidGhostMaterial);
            }

            return root;
        }

        public static void ApplyGhostMaterial(GameObject root, Material ghostMat)
        {
            if (root == null || ghostMat == null) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                if (r.sharedMaterial != ghostMat)
                {
                    r.sharedMaterial = ghostMat;
                }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                if (c != null) Object.Destroy(c);
            }
        }

        // --- MODEL İNŞA FONKSİYONLARI ---

        // 1. Raf (Store Shelf)
        private static void BuildStoreShelf(GameObject parent)
        {
            // Boyutlar: 1.8m genişlik, 2.0m yükseklik, 0.6m derinlik
            float w = 1.8f, h = 2.0f, d = 0.6f;

            // Yan Dikmeler (Metal)
            CreatePrimitive(parent, "Pole_Left", PrimitiveType.Cube, new Vector3(-w / 2f + 0.05f, h / 2f, 0f), new Vector3(0.08f, h, d), metalMat);
            CreatePrimitive(parent, "Pole_Right", PrimitiveType.Cube, new Vector3(w / 2f - 0.05f, h / 2f, 0f), new Vector3(0.08f, h, d), metalMat);
            CreatePrimitive(parent, "BackPanel", PrimitiveType.Cube, new Vector3(0f, h / 2f, d / 2f - 0.02f), new Vector3(w, h, 0.04f), metalMat);

            // 3 Kat Ahşap Raf
            float[] shelfY = new float[] { 0.3f, 0.9f, 1.5f };
            foreach (float y in shelfY)
            {
                CreatePrimitive(parent, "ShelfBoard", PrimitiveType.Cube, new Vector3(0f, y, 0f), new Vector3(w - 0.1f, 0.06f, d - 0.05f), woodMat);
                // Ön Fiyat Şeridi
                CreatePrimitive(parent, "PriceTagStrip", PrimitiveType.Cube, new Vector3(0f, y, -d / 2f + 0.02f), new Vector3(w - 0.1f, 0.04f, 0.02f), whiteGlossMat);
            }

            // Ön Yön Ok Göstergesi
            BuildDirectionalArrowIndicator(parent, -d / 2f);
        }

        // 2. Depo Rafı (Storage Heavy Rack)
        private static void BuildStorageShelf(GameObject parent)
        {
            float w = 2.2f, h = 2.2f, d = 0.9f;

            // 4 Adet Turuncu Çelik Ayak
            float[] xOffsets = new float[] { -w / 2f + 0.06f, w / 2f - 0.06f };
            float[] zOffsets = new float[] { -d / 2f + 0.06f, d / 2f - 0.06f };

            foreach (float x in xOffsets)
            {
                foreach (float z in zOffsets)
                {
                    CreatePrimitive(parent, "HeavyLeg", PrimitiveType.Cube, new Vector3(x, h / 2f, z), new Vector3(0.12f, h, 0.12f), orangeMetalMat);
                }
            }

            // 3 Kat Kalın Ahşap Palet Tablası ve Gri Çelik Destekler
            float[] shelfY = new float[] { 0.2f, 1.1f, 2.0f };
            foreach (float y in shelfY)
            {
                CreatePrimitive(parent, "Beam_Front", PrimitiveType.Cube, new Vector3(0f, y, -d / 2f + 0.06f), new Vector3(w, 0.1f, 0.08f), metalMat);
                CreatePrimitive(parent, "Beam_Back", PrimitiveType.Cube, new Vector3(0f, y, d / 2f - 0.06f), new Vector3(w, 0.1f, 0.08f), metalMat);
                CreatePrimitive(parent, "HeavyBoard", PrimitiveType.Cube, new Vector3(0f, y + 0.04f, 0f), new Vector3(w - 0.15f, 0.08f, d - 0.1f), darkWoodMat);
            }

            // Ön Yön Ok Göstergesi (Depo Dizim Yönü - Turuncu)
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.50f, 0.10f));
        }

        // 3. Kasa (Checkout Counter)
        private static void BuildCashierCounter(GameObject parent)
        {
            float w = 2.4f, h = 0.95f, d = 1.1f;

            // Ana Masa Gövdesi (Ahşap & Beyaz)
            CreatePrimitive(parent, "DeskBase", PrimitiveType.Cube, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), darkWoodMat);
            CreatePrimitive(parent, "DeskTop", PrimitiveType.Cube, new Vector3(0f, h + 0.02f, 0f), new Vector3(w + 0.05f, 0.04f, d + 0.05f), whiteGlossMat);

            // Siyah Konveyör Bant Şeridi
            CreatePrimitive(parent, "ConveyorBelt", PrimitiveType.Cube, new Vector3(-0.3f, h + 0.05f, -0.1f), new Vector3(1.4f, 0.02f, 0.5f), blackMat);

            // Barkod Okuyucu Paneli
            CreatePrimitive(parent, "ScannerPanel", PrimitiveType.Cube, new Vector3(0.5f, h + 0.06f, -0.1f), new Vector3(0.35f, 0.03f, 0.35f), steelMat);
            CreatePrimitive(parent, "ScannerGlass", PrimitiveType.Cube, new Vector3(0.5f, h + 0.08f, -0.1f), new Vector3(0.2f, 0.01f, 0.2f), glassMat);

            // Kasa Ekranı & POS Terminali
            GameObject monitor = CreatePrimitive(parent, "MonitorStand", PrimitiveType.Cube, new Vector3(0.8f, h + 0.25f, 0.3f), new Vector3(0.08f, 0.4f, 0.08f), metalMat);
            CreatePrimitive(parent, "MonitorScreen", PrimitiveType.Cube, new Vector3(0.8f, h + 0.45f, 0.3f), new Vector3(0.35f, 0.25f, 0.05f), blackMat);
            CreatePrimitive(parent, "ScreenGlass", PrimitiveType.Cube, new Vector3(0.8f, h + 0.45f, 0.27f), new Vector3(0.32f, 0.22f, 0.01f), cyanLedMat);

            // Seperatör Ayırıcı Çubuk
            CreatePrimitive(parent, "DividerBar", PrimitiveType.Cube, new Vector3(-0.9f, h + 0.08f, 0.25f), new Vector3(0.06f, 0.06f, 0.5f), redAccentMat);

            // Kasa Ön L-Şeklinde Kuyruk Yön Oku (Neon Turkuaz)
            BuildLShapedQueueArrowIndicator(parent, -d / 2f);
        }

        // Zemin Kasa L-Şeklinde Kuyruk Ok Göstergesi (L-Shaped Cashier Queue Arrow Decal)
        private static void BuildLShapedQueueArrowIndicator(GameObject parent, float frontOffsetZ)
        {
            GameObject arrowGroup = new GameObject("Cashier_LQueue_Arrow");
            arrowGroup.transform.SetParent(parent.transform, false);

            Color arrowColor = new Color(0.10f, 0.90f, 0.95f); // Neon turkuaz
            Material arrowMat = CreateSmartMaterial("LQueue_Arrow_Mat", arrowColor);
            arrowMat.EnableKeyword("_EMISSION");
            arrowMat.SetColor("_EmissionColor", arrowColor * 0.7f);

            // 1. Ödemedeki Müşteri Noktası (Tezgah Önü - Slot 0)
            GameObject slot0Point = CreatePrimitive(arrowGroup, "Slot0_Stand", PrimitiveType.Cube, new Vector3(0f, 0.015f, frontOffsetZ - 0.20f), new Vector3(0.30f, 0.015f, 0.30f), arrowMat);
            slot0Point.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Object.Destroy(slot0Point.GetComponent<Collider>());

            // 2. L-Köşe Dönüş Şeridi (Sağ Taraf - Slot 1)
            GameObject cornerShaft = CreatePrimitive(arrowGroup, "LCorner_Shaft", PrimitiveType.Cube, new Vector3(0.50f, 0.015f, frontOffsetZ - 0.20f), new Vector3(0.95f, 0.015f, 0.12f), arrowMat);
            Object.Destroy(cornerShaft.GetComponent<Collider>());

            // 3. Ön Koridor Kuyruk Uzantı Şeridi (Masa Yanından Ön Koridora Uzanır - Slot 2, 3, 4)
            GameObject backShaft = CreatePrimitive(arrowGroup, "LBack_Shaft", PrimitiveType.Cube, new Vector3(0.95f, 0.015f, frontOffsetZ - 1.10f), new Vector3(0.12f, 0.015f, 1.80f), arrowMat);
            Object.Destroy(backShaft.GetComponent<Collider>());

            // 4. Kuyruk Giriş Ok Başı (Koridordan kuyruğa girişi gösterir)
            GameObject head = CreatePrimitive(arrowGroup, "LQueue_Head", PrimitiveType.Cube, new Vector3(0.95f, 0.015f, frontOffsetZ - 2.05f), new Vector3(0.24f, 0.015f, 0.24f), arrowMat);
            head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Object.Destroy(head.GetComponent<Collider>());
        }

        // 4. Buzdolabı (Commercial Refrigerator)
        private static void BuildCommercialFridge(GameObject parent)
        {
            float w = 1.4f, h = 2.2f, d = 0.85f;

            // Çelik Gövde
            CreatePrimitive(parent, "Body", PrimitiveType.Cube, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), steelMat);
            // İç Boşluk / Siyah Arka
            CreatePrimitive(parent, "InnerCave", PrimitiveType.Cube, new Vector3(0f, h / 2f + 0.1f, -0.05f), new Vector3(w - 0.15f, h - 0.4f, d - 0.15f), blackMat);

            // 3 Adet Şeffaf Cam Kapak
            float doorW = (w - 0.2f) / 2f;
            CreatePrimitive(parent, "GlassDoor_L", PrimitiveType.Cube, new Vector3(-doorW / 2f - 0.02f, h / 2f + 0.1f, -d / 2f), new Vector3(doorW, h - 0.45f, 0.05f), glassMat);
            CreatePrimitive(parent, "GlassDoor_R", PrimitiveType.Cube, new Vector3(doorW / 2f + 0.02f, h / 2f + 0.1f, -d / 2f), new Vector3(doorW, h - 0.45f, 0.05f), glassMat);

            // Kapak Kulpları
            CreatePrimitive(parent, "Handle_L", PrimitiveType.Cube, new Vector3(-0.05f, h / 2f, -d / 2f - 0.03f), new Vector3(0.03f, 0.4f, 0.03f), metalMat);
            CreatePrimitive(parent, "Handle_R", PrimitiveType.Cube, new Vector3(0.05f, h / 2f, -d / 2f - 0.03f), new Vector3(0.03f, 0.4f, 0.03f), metalMat);

            // Üst Soğutucu Izgara ve İç Mavi LED Işık
            CreatePrimitive(parent, "TopVent", PrimitiveType.Cube, new Vector3(0f, h - 0.12f, -d / 2f + 0.02f), new Vector3(w - 0.1f, 0.2f, 0.06f), metalMat);
            CreatePrimitive(parent, "InteriorLed", PrimitiveType.Cube, new Vector3(0f, h - 0.25f, -0.1f), new Vector3(w - 0.2f, 0.04f, 0.04f), cyanLedMat);

            // Ön Yön Ok Göstergesi
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.40f, 0.85f, 0.95f));
        }

        // 5. Dondurucu (Chest Freezer)
        private static void BuildChestFreezer(GameObject parent)
        {
            float w = 2.0f, h = 0.9f, d = 1.0f;

            // Beyaz Dış Gövde
            CreatePrimitive(parent, "FreezerBody", PrimitiveType.Cube, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), whiteGlossMat);

            // İç Dondurucu Çukuru
            CreatePrimitive(parent, "InnerCave", PrimitiveType.Cube, new Vector3(0f, h / 2f + 0.05f, 0f), new Vector3(w - 0.2f, h - 0.15f, d - 0.2f), blackMat);

            // Üst Sürgülü Çift Cam Panel
            float paneW = (w - 0.1f) / 2f;
            CreatePrimitive(parent, "GlassPane_L", PrimitiveType.Cube, new Vector3(-paneW / 2f + 0.02f, h + 0.01f, 0f), new Vector3(paneW, 0.03f, d - 0.1f), glassMat);
            CreatePrimitive(parent, "GlassPane_R", PrimitiveType.Cube, new Vector3(paneW / 2f - 0.02f, h + 0.02f, 0f), new Vector3(paneW, 0.03f, d - 0.1f), glassMat);

            // Sürgü Kulpları
            CreatePrimitive(parent, "Handle_L", PrimitiveType.Cube, new Vector3(-0.1f, h + 0.04f, -d / 3f), new Vector3(0.15f, 0.03f, 0.04f), metalMat);
            CreatePrimitive(parent, "Handle_R", PrimitiveType.Cube, new Vector3(0.1f, h + 0.05f, d / 3f), new Vector3(0.15f, 0.03f, 0.04f), metalMat);

            // Ön Sıcaklık LED Göstergesi
            CreatePrimitive(parent, "TempDisplay", PrimitiveType.Cube, new Vector3(0f, h - 0.2f, -d / 2f - 0.01f), new Vector3(0.3f, 0.12f, 0.02f), cyanLedMat);

            // Ön Yön Ok Göstergesi
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.60f, 0.90f, 1.00f));
        }

        // 6. Kozmetik Ürün Rafı (Cosmetics Display Shelf)
        private static void BuildCosmeticsShelf(GameObject parent)
        {
            float w = 1.6f, h = 2.1f, d = 0.6f;

            // Parlak Beyaz Dolap Gövdesi
            CreatePrimitive(parent, "OuterCabinet", PrimitiveType.Cube, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), whiteGlossMat);

            // Aynalı Arka Panel (Steel Gloss)
            CreatePrimitive(parent, "MirrorBacking", PrimitiveType.Cube, new Vector3(0f, h / 2f, d / 2f - 0.03f), new Vector3(w - 0.1f, h - 0.2f, 0.02f), steelMat);

            // 4 Kat Şeffaf Cam Raf
            float[] yPos = new float[] { 0.4f, 0.85f, 1.3f, 1.75f };
            foreach (float y in yPos)
            {
                CreatePrimitive(parent, "GlassShelf", PrimitiveType.Cube, new Vector3(0f, y, 0f), new Vector3(w - 0.12f, 0.03f, d - 0.08f), glassMat);
                // Raf Altı Pembe/Cyan LED Işık Şeridi
                CreatePrimitive(parent, "LedStrip", PrimitiveType.Cube, new Vector3(0f, y - 0.02f, -d / 2f + 0.05f), new Vector3(w - 0.15f, 0.02f, 0.03f), pinkLedMat);
            }

            // Ön Yön Ok Göstergesi
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.40f, 0.75f));
        }

        // 7. Fırın Tezgahı (Bakery Counter)
        private static void BuildBakeryCounter(GameObject parent)
        {
            float w = 2.0f, h = 1.2f, d = 0.9f;

            // Koyu Ahşap Taban Dolabı
            CreatePrimitive(parent, "WoodBase", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(w, 0.7f, d), darkWoodMat);

            // Kavisli Cam Fanus Teşhir Üstü
            CreatePrimitive(parent, "GlassTop", PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f), new Vector3(w - 0.08f, 0.5f, d - 0.08f), glassMat);

            // İç Ahşap Tepsi Rafları (2 Kat)
            CreatePrimitive(parent, "Tray_1", PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(w - 0.2f, 0.04f, d - 0.2f), woodMat);
            CreatePrimitive(parent, "Tray_2", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), new Vector3(w - 0.2f, 0.04f, d - 0.3f), woodMat);

            // Pirinç Çerçeve Detayı
            CreatePrimitive(parent, "BrassTrim", PrimitiveType.Cube, new Vector3(0f, 0.71f, -d / 2f - 0.01f), new Vector3(w, 0.04f, 0.03f), orangeMetalMat);

            // Ön Yön Ok Göstergesi
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.70f, 0.25f));
        }

        // 8. Manav Rafı (Produce Shelf)
        private static void BuildProduceShelf(GameObject parent)
        {
            float w = 1.9f, h = 1.4f, d = 1.0f;

            // Eğimli Ahşap İskelet
            CreatePrimitive(parent, "SideFrame_L", PrimitiveType.Cube, new Vector3(-w / 2f + 0.05f, h / 2f, 0f), new Vector3(0.08f, h, d), woodMat);
            CreatePrimitive(parent, "SideFrame_R", PrimitiveType.Cube, new Vector3(w / 2f - 0.05f, h / 2f, 0f), new Vector3(0.08f, h, d), woodMat);

            // 3 Kademeli Eğimli Ahşap Kasa Bölmeleri
            float[] yHeights = new float[] { 0.3f, 0.75f, 1.15f };
            float[] zDepth = new float[] { -0.1f, 0.0f, 0.1f };

            for (int i = 0; i < 3; i++)
            {
                GameObject crate = CreatePrimitive(parent, "ProduceCrate_" + i, PrimitiveType.Cube, new Vector3(0f, yHeights[i], zDepth[i]), new Vector3(w - 0.15f, 0.25f, 0.4f), woodMat);
                crate.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

                // İç Yeşil Çim / Sebze Tabanı
                GameObject mat = CreatePrimitive(parent, "ProduceMat_" + i, PrimitiveType.Cube, new Vector3(0f, yHeights[i] + 0.08f, zDepth[i] - 0.02f), new Vector3(w - 0.25f, 0.1f, 0.32f), greenProduceMat);
                mat.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            }

            // Karatahta Fiyat Panosu Header
            CreatePrimitive(parent, "ChalkBoard", PrimitiveType.Cube, new Vector3(0f, h + 0.15f, 0.2f), new Vector3(1.2f, 0.25f, 0.04f), blackMat);
            CreatePrimitive(parent, "BoardFrame", PrimitiveType.Cube, new Vector3(0f, h + 0.15f, 0.22f), new Vector3(1.25f, 0.28f, 0.02f), woodMat);

            // Ön Yön Ok Göstergesi
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.30f, 0.85f, 0.40f));
        }

        // 9. Kasap Reyonu (Butcher Counter)
        private static void BuildButcherCounter(GameObject parent)
        {
            float w = 2.2f, h = 1.15f, d = 0.95f;

            // Paslanmaz Çelik Ana Gövde
            CreatePrimitive(parent, "SteelBody", PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(w, 0.8f, d), steelMat);

            // Soğutmalı Ön Cam Vitrin
            CreatePrimitive(parent, "GlassCase", PrimitiveType.Cube, new Vector3(0f, 0.9f, -0.1f), new Vector3(w - 0.1f, 0.45f, d - 0.2f), glassMat);

            // Kırmızı Çizgi Accent Strip
            CreatePrimitive(parent, "RedStrip", PrimitiveType.Cube, new Vector3(0f, 0.78f, -d / 2f - 0.01f), new Vector3(w, 0.05f, 0.02f), redAccentMat);

            // Yan Et Kesim Kütüğü (Wood Block)
            CreatePrimitive(parent, "ButcherBlock", PrimitiveType.Cube, new Vector3(w / 2f - 0.3f, 0.85f, 0.2f), new Vector3(0.5f, 0.15f, 0.45f), darkWoodMat);

            // Ön Yön Ok Göstergesi
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.25f, 0.30f));
        }

        // 10. Elektronik Rafı (Electronics Shelf)
        private static void BuildElectronicsShelf(GameObject parent)
        {
            float w = 1.7f, h = 2.1f, d = 0.65f;

            // Modern Koyu Titanyum Gövde
            CreatePrimitive(parent, "TitaniumFrame", PrimitiveType.Cube, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d), metalMat);

            // Mavi LED Çerçeve Işıkları
            CreatePrimitive(parent, "LedBorder_L", PrimitiveType.Cube, new Vector3(-w / 2f + 0.03f, h / 2f, -d / 2f + 0.01f), new Vector3(0.04f, h - 0.1f, 0.03f), cyanLedMat);
            CreatePrimitive(parent, "LedBorder_R", PrimitiveType.Cube, new Vector3(w / 2f - 0.03f, h / 2f, -d / 2f + 0.01f), new Vector3(0.04f, h - 0.1f, 0.03f), cyanLedMat);
            CreatePrimitive(parent, "LedBorder_T", PrimitiveType.Cube, new Vector3(0f, h - 0.05f, -d / 2f + 0.01f), new Vector3(w, 0.04f, 0.03f), cyanLedMat);

            // 3 Kat Cam Teşhir Paneli ve Kilitli Askı Çubukları
            float[] shelfY = new float[] { 0.5f, 1.05f, 1.6f };
            foreach (float y in shelfY)
            {
                CreatePrimitive(parent, "TechShelf", PrimitiveType.Cube, new Vector3(0f, y, 0f), new Vector3(w - 0.12f, 0.03f, d - 0.08f), glassMat);
                // Güvenlik Askı Barı
                CreatePrimitive(parent, "SecurityBar", PrimitiveType.Cube, new Vector3(0f, y + 0.25f, -d / 2f + 0.1f), new Vector3(w - 0.2f, 0.02f, 0.02f), steelMat);
            }

            BuildDirectionalArrowIndicator(parent, -d / 2f);
        }

        // 11. Alışveriş Sepeti Stantı (Shopping Cart Stand)
        private static void BuildShoppingCartStand(GameObject parent)
        {
            float w = 1.2f, h = 0.9f, d = 0.8f;

            // Stant Tabanı
            CreatePrimitive(parent, "Stand_Base", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(w, 0.16f, d), steelMat);

            // Yan Korkuluk Çubukları
            for (float x = -0.52f; x <= 0.52f; x += 1.04f)
            {
                CreatePrimitive(parent, "Guide_Post", PrimitiveType.Cylinder, new Vector3(x, 0.5f, 0f), new Vector3(0.06f, 0.4f, 0.06f), steelMat);
            }

            // 3 Adet İç İçe İstifli Kırmızı Alışveriş Sepeti
            for (int i = 0; i < 3; i++)
            {
                float offsetZ = (i - 1) * 0.18f;
                float offsetY = 0.25f + (i * 0.08f);

                GameObject basket = CreatePrimitive(parent, $"Basket_{i+1}", PrimitiveType.Cube, new Vector3(0f, offsetY, offsetZ), new Vector3(0.55f, 0.35f, 0.42f), redAccentMat);
                CreatePrimitive(parent, $"Handle_{i+1}", PrimitiveType.Cube, new Vector3(0f, offsetY + 0.18f, offsetZ - 0.18f), new Vector3(0.5f, 0.04f, 0.05f), blackMat);
            }

            // Sepet Alma Yön Oku (Kırmızı/Mercan)
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.30f, 0.35f));
        }

        // Zemin Ön Yön Ok Göstergesi (Directional Arrow Decal)
        private static void BuildDirectionalArrowIndicator(GameObject parent, float frontOffsetZ, Color? customColor = null)
        {
            GameObject arrowGroup = new GameObject("Front_Direction_Arrow");
            arrowGroup.transform.SetParent(parent.transform, false);

            Color arrowColor = customColor ?? new Color(0.10f, 0.90f, 0.85f); // Neon turkuaz/yeşil
            Material arrowMat = CreateSmartMaterial("Arrow_Indicator_Mat", arrowColor);
            arrowMat.EnableKeyword("_EMISSION");
            arrowMat.SetColor("_EmissionColor", arrowColor * 0.6f);

            // Ok Gövdesi (Çubuk)
            GameObject shaft = CreatePrimitive(arrowGroup, "Arrow_Shaft", PrimitiveType.Cube, new Vector3(0f, 0.015f, frontOffsetZ - 0.25f), new Vector3(0.14f, 0.015f, 0.35f), arrowMat);
            Object.Destroy(shaft.GetComponent<Collider>());

            // Ok Başı (45° döndürülmüş küp)
            GameObject head = CreatePrimitive(arrowGroup, "Arrow_Head", PrimitiveType.Cube, new Vector3(0f, 0.015f, frontOffsetZ - 0.45f), new Vector3(0.24f, 0.015f, 0.24f), arrowMat);
            head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Object.Destroy(head.GetComponent<Collider>());

            // Zemin Yan Sınır Şeritleri
            GameObject leftDot = CreatePrimitive(arrowGroup, "Arrow_LeftBorder", PrimitiveType.Cube, new Vector3(-0.35f, 0.015f, frontOffsetZ - 0.30f), new Vector3(0.04f, 0.015f, 0.25f), arrowMat);
            Object.Destroy(leftDot.GetComponent<Collider>());

            GameObject rightDot = CreatePrimitive(arrowGroup, "Arrow_RightBorder", PrimitiveType.Cube, new Vector3(0.35f, 0.015f, frontOffsetZ - 0.30f), new Vector3(0.04f, 0.015f, 0.25f), arrowMat);
            Object.Destroy(rightDot.GetComponent<Collider>());
        }

        // Yardımcı ilkel obje üretici
        private static GameObject CreatePrimitive(GameObject parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent.transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            if (mat != null && obj.GetComponent<Renderer>() != null)
            {
                obj.GetComponent<Renderer>().sharedMaterial = mat;
            }

            return obj;
        }

        // ==================== DEKORASYON MODEL İNŞA METOTLARI (30 ADET) ====================

        // --- SEVİYE 1 DEKORASYONLAR ---
        private static void BuildPlantPot(GameObject parent)
        {
            CreatePrimitive(parent, "Pot", PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(0.45f, 0.25f, 0.45f), whiteGlossMat);
            CreatePrimitive(parent, "Soil", PrimitiveType.Cylinder, new Vector3(0f, 0.48f, 0f), new Vector3(0.42f, 0.02f, 0.42f), darkWoodMat);
            CreatePrimitive(parent, "Leaves", PrimitiveType.Sphere, new Vector3(0f, 0.75f, 0f), new Vector3(0.6f, 0.5f, 0.6f), greenProduceMat);
        }

        private static void BuildPottedPalm(GameObject parent)
        {
            CreatePrimitive(parent, "Pot", PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(0.55f, 0.35f, 0.55f), darkWoodMat);
            CreatePrimitive(parent, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(0.12f, 0.6f, 0.12f), woodMat);
            CreatePrimitive(parent, "Fronds", PrimitiveType.Sphere, new Vector3(0f, 1.7f, 0f), new Vector3(1.1f, 0.6f, 1.1f), greenProduceMat);
        }

        private static void BuildTrashCan(GameObject parent)
        {
            float d = 0.5f;
            CreatePrimitive(parent, "CanBody", PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(0.45f, 0.4f, 0.45f), steelMat);
            CreatePrimitive(parent, "CanLid", PrimitiveType.Cylinder, new Vector3(0f, 0.82f, 0f), new Vector3(0.47f, 0.04f, 0.47f), blackMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.20f, 0.85f, 0.40f));
        }

        private static void BuildBenchWood(GameObject parent)
        {
            float w = 1.6f, h = 0.8f, d = 0.6f;
            CreatePrimitive(parent, "Seat", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(w, 0.08f, d - 0.1f), woodMat);
            CreatePrimitive(parent, "Back", PrimitiveType.Cube, new Vector3(0f, 0.75f, d / 2f - 0.05f), new Vector3(w, 0.35f, 0.06f), woodMat);
            CreatePrimitive(parent, "Leg_L", PrimitiveType.Cube, new Vector3(-w / 2f + 0.1f, 0.22f, 0f), new Vector3(0.08f, 0.44f, d - 0.1f), metalMat);
            CreatePrimitive(parent, "Leg_R", PrimitiveType.Cube, new Vector3(w / 2f - 0.1f, 0.22f, 0f), new Vector3(0.08f, 0.44f, d - 0.1f), metalMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.70f, 0.25f));
        }

        private static void BuildWelcomeMat(GameObject parent)
        {
            CreatePrimitive(parent, "MatBase", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0f), new Vector3(1.2f, 0.015f, 0.8f), blackMat);
            CreatePrimitive(parent, "MatCenter", PrimitiveType.Cube, new Vector3(0f, 0.015f, 0f), new Vector3(1.0f, 0.015f, 0.6f), redAccentMat);
        }

        private static void BuildWallClock(GameObject parent)
        {
            CreatePrimitive(parent, "ClockFrame", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.6f, 0.04f, 0.6f), blackMat);
            CreatePrimitive(parent, "ClockFace", PrimitiveType.Cylinder, new Vector3(0f, 1.81f, 0f), new Vector3(0.52f, 0.04f, 0.52f), whiteGlossMat);
            CreatePrimitive(parent, "NeonBorder", PrimitiveType.Cylinder, new Vector3(0f, 1.82f, 0f), new Vector3(0.58f, 0.03f, 0.58f), cyanLedMat);
        }

        private static void BuildAdBanner(GameObject parent)
        {
            float w = 0.9f, h = 1.4f;
            CreatePrimitive(parent, "Leg_L", PrimitiveType.Cube, new Vector3(-0.4f, h / 2f, 0f), new Vector3(0.05f, h, 0.5f), woodMat);
            CreatePrimitive(parent, "Leg_R", PrimitiveType.Cube, new Vector3(0.4f, h / 2f, 0f), new Vector3(0.05f, h, 0.5f), woodMat);
            CreatePrimitive(parent, "Board", PrimitiveType.Cube, new Vector3(0f, h / 2f + 0.1f, 0f), new Vector3(w, h - 0.2f, 0.06f), blackMat);
        }

        private static void BuildCeilingSpotlight(GameObject parent)
        {
            CreatePrimitive(parent, "Bar", PrimitiveType.Cube, new Vector3(0f, 2.4f, 0f), new Vector3(1.8f, 0.06f, 0.06f), steelMat);
            for (float x = -0.6f; x <= 0.6f; x += 0.6f)
            {
                CreatePrimitive(parent, $"Spot_{x}", PrimitiveType.Cylinder, new Vector3(x, 2.25f, 0f), new Vector3(0.15f, 0.15f, 0.15f), blackMat);
                CreatePrimitive(parent, $"Lens_{x}", PrimitiveType.Cylinder, new Vector3(x, 2.16f, 0f), new Vector3(0.12f, 0.02f, 0.12f), cyanLedMat);
            }
        }

        private static void BuildDividerFence(GameObject parent)
        {
            float w = 1.8f, h = 1.0f;
            CreatePrimitive(parent, "Post_L", PrimitiveType.Cube, new Vector3(-w / 2f, h / 2f, 0f), new Vector3(0.08f, h, 0.08f), darkWoodMat);
            CreatePrimitive(parent, "Post_R", PrimitiveType.Cube, new Vector3(w / 2f, h / 2f, 0f), new Vector3(0.08f, h, 0.08f), darkWoodMat);
            for (float y = 0.3f; y <= 0.8f; y += 0.25f)
            {
                CreatePrimitive(parent, $"Rail_{y}", PrimitiveType.Cube, new Vector3(0f, y, 0f), new Vector3(w, 0.06f, 0.04f), woodMat);
            }
        }

        private static void BuildWaterDispenser(GameObject parent)
        {
            float d = 0.6f;
            CreatePrimitive(parent, "Body", PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(0.45f, 1.2f, d), whiteGlossMat);
            CreatePrimitive(parent, "DispenserNiche", PrimitiveType.Cube, new Vector3(0f, 0.75f, -d / 2f + 0.08f), new Vector3(0.35f, 0.35f, 0.15f), blackMat);
            CreatePrimitive(parent, "Jug", PrimitiveType.Cylinder, new Vector3(0f, 1.45f, 0f), new Vector3(0.32f, 0.25f, 0.32f), glassMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.10f, 0.85f, 0.95f));
        }

        // --- SEVİYE 2 DEKORASYONLAR ---
        private static void BuildCoffeeMachine(GameObject parent)
        {
            float d = 0.7f;
            CreatePrimitive(parent, "Cabinet", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.9f, d), darkWoodMat);
            CreatePrimitive(parent, "MachineBody", PrimitiveType.Cube, new Vector3(0f, 1.25f, 0f), new Vector3(0.85f, 0.7f, 0.5f), blackMat);
            CreatePrimitive(parent, "Screen", PrimitiveType.Cube, new Vector3(0f, 1.35f, -0.26f), new Vector3(0.4f, 0.3f, 0.02f), cyanLedMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.65f, 0.20f));
        }

        private static void BuildNeonSign(GameObject parent)
        {
            CreatePrimitive(parent, "Backing", PrimitiveType.Cube, new Vector3(0f, 1.8f, 0f), new Vector3(1.6f, 0.6f, 0.06f), blackMat);
            CreatePrimitive(parent, "NeonBorder", PrimitiveType.Cube, new Vector3(0f, 1.8f, -0.04f), new Vector3(1.5f, 0.5f, 0.02f), pinkLedMat);
        }

        private static void BuildFountainSmall(GameObject parent)
        {
            CreatePrimitive(parent, "Basin", PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(1.4f, 0.25f, 1.4f), whiteGlossMat);
            CreatePrimitive(parent, "Water", PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(1.25f, 0.04f, 1.25f), cyanLedMat);
            CreatePrimitive(parent, "Pillar", PrimitiveType.Cylinder, new Vector3(0f, 0.75f, 0f), new Vector3(0.3f, 0.3f, 0.3f), whiteGlossMat);
        }

        private static void BuildGumballMachine(GameObject parent)
        {
            float d = 0.5f;
            CreatePrimitive(parent, "StandBase", PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.35f, 0.3f, 0.35f), redAccentMat);
            CreatePrimitive(parent, "Globe", PrimitiveType.Sphere, new Vector3(0f, 0.85f, 0f), new Vector3(0.45f, 0.45f, 0.45f), glassMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.30f, 0.35f));
        }

        private static void BuildVendingSnack(GameObject parent)
        {
            float d = 0.8f;
            CreatePrimitive(parent, "Body", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0f), new Vector3(1.0f, 2.0f, d), blackMat);
            CreatePrimitive(parent, "GlassFront", PrimitiveType.Cube, new Vector3(-0.1f, 1.1f, -d / 2f + 0.02f), new Vector3(0.7f, 1.4f, 0.04f), glassMat);
            CreatePrimitive(parent, "Keypad", PrimitiveType.Cube, new Vector3(0.38f, 1.1f, -d / 2f + 0.02f), new Vector3(0.18f, 0.5f, 0.04f), cyanLedMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.10f, 0.85f, 0.95f));
        }

        private static void BuildIceCreamCart(GameObject parent)
        {
            float d = 0.9f;
            CreatePrimitive(parent, "CartBody", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.6f, d), whiteGlossMat);
            CreatePrimitive(parent, "Canopy", PrimitiveType.Cube, new Vector3(0f, 1.4f, 0f), new Vector3(1.5f, 0.08f, d + 0.1f), redAccentMat);
            CreatePrimitive(parent, "Wheel_L", PrimitiveType.Cylinder, new Vector3(-0.5f, 0.2f, d / 2f), new Vector3(0.35f, 0.05f, 0.35f), blackMat);
            CreatePrimitive(parent, "Wheel_R", PrimitiveType.Cylinder, new Vector3(0.5f, 0.2f, d / 2f), new Vector3(0.35f, 0.05f, 0.35f), blackMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.40f, 0.55f));
        }

        private static void BuildRedCarpet(GameObject parent)
        {
            CreatePrimitive(parent, "Carpet", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0f), new Vector3(1.2f, 0.015f, 2.4f), redAccentMat);
            for (float z = -1.0f; z <= 1.0f; z += 2.0f)
            {
                CreatePrimitive(parent, $"Post_L_{z}", PrimitiveType.Cylinder, new Vector3(-0.65f, 0.45f, z), new Vector3(0.06f, 0.45f, 0.06f), orangeMetalMat);
                CreatePrimitive(parent, $"Post_R_{z}", PrimitiveType.Cylinder, new Vector3(0.65f, 0.45f, z), new Vector3(0.06f, 0.45f, 0.06f), orangeMetalMat);
            }
        }

        private static void BuildDigitalMenuBoard(GameObject parent)
        {
            CreatePrimitive(parent, "MountBar", PrimitiveType.Cube, new Vector3(0f, 2.3f, 0f), new Vector3(2.2f, 0.06f, 0.06f), metalMat);
            for (float x = -0.7f; x <= 0.7f; x += 0.7f)
            {
                CreatePrimitive(parent, $"Screen_{x}", PrimitiveType.Cube, new Vector3(x, 1.9f, 0f), new Vector3(0.6f, 0.45f, 0.04f), cyanLedMat);
            }
        }

        private static void BuildBonsaiTree(GameObject parent)
        {
            CreatePrimitive(parent, "Pedestal", PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.8f, 0.6f), darkWoodMat);
            CreatePrimitive(parent, "Tray", PrimitiveType.Cube, new Vector3(0f, 0.83f, 0f), new Vector3(0.5f, 0.04f, 0.4f), whiteGlossMat);
            CreatePrimitive(parent, "BonsaiFoliage", PrimitiveType.Sphere, new Vector3(0f, 1.15f, 0f), new Vector3(0.45f, 0.35f, 0.45f), greenProduceMat);
        }

        private static void BuildAtmMachine(GameObject parent)
        {
            float d = 0.75f;
            CreatePrimitive(parent, "KioskBody", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0f), new Vector3(0.9f, 2.0f, d), metalMat);
            CreatePrimitive(parent, "Screen", PrimitiveType.Cube, new Vector3(0f, 1.35f, -d / 2f + 0.02f), new Vector3(0.55f, 0.35f, 0.02f), cyanLedMat);
            CreatePrimitive(parent, "Keypad", PrimitiveType.Cube, new Vector3(0f, 1.0f, -d / 2f + 0.05f), new Vector3(0.4f, 0.15f, 0.04f), steelMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.15f, 0.85f, 0.95f));
        }

        // --- SEVİYE 3 DEKORASYONLAR ---
        private static void BuildArcadeMachine(GameObject parent)
        {
            float d = 0.85f;
            CreatePrimitive(parent, "Cabinet", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0f), new Vector3(0.85f, 2.0f, d), blackMat);
            CreatePrimitive(parent, "Screen", PrimitiveType.Cube, new Vector3(0f, 1.4f, -0.15f), new Vector3(0.6f, 0.45f, 0.02f), pinkLedMat);
            CreatePrimitive(parent, "ControlDeck", PrimitiveType.Cube, new Vector3(0f, 0.95f, -d / 2f + 0.1f), new Vector3(0.75f, 0.12f, 0.3f), redAccentMat);
            CreatePrimitive(parent, "Marquee", PrimitiveType.Cube, new Vector3(0f, 1.88f, -d / 2f + 0.04f), new Vector3(0.75f, 0.18f, 0.06f), cyanLedMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.25f, 0.65f));
        }

        private static void BuildAquariumGrand(GameObject parent)
        {
            CreatePrimitive(parent, "BaseStand", PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(2.2f, 0.8f, 0.8f), darkWoodMat);
            CreatePrimitive(parent, "TankGlass", PrimitiveType.Cube, new Vector3(0f, 1.3f, 0f), new Vector3(2.1f, 1.0f, 0.7f), glassMat);
            CreatePrimitive(parent, "WaterVolume", PrimitiveType.Cube, new Vector3(0f, 1.28f, 0f), new Vector3(2.05f, 0.92f, 0.65f), cyanLedMat);
        }

        private static void BuildJukebox(GameObject parent)
        {
            float d = 0.7f;
            CreatePrimitive(parent, "JukeboxBody", PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(1.0f, 1.6f, d), darkWoodMat);
            CreatePrimitive(parent, "NeonArch", PrimitiveType.Cylinder, new Vector3(0f, 1.45f, -d / 2f + 0.04f), new Vector3(0.8f, 0.04f, 0.8f), pinkLedMat);
            CreatePrimitive(parent, "Grill", PrimitiveType.Cube, new Vector3(0f, 0.5f, -d / 2f + 0.02f), new Vector3(0.7f, 0.6f, 0.02f), steelMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.80f, 0.20f));
        }

        private static void BuildGoldenStatue(GameObject parent)
        {
            CreatePrimitive(parent, "MarbleBase", PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(0.7f, 0.8f, 0.7f), blackMat);
            CreatePrimitive(parent, "GoldTrophy", PrimitiveType.Cylinder, new Vector3(0f, 1.3f, 0f), new Vector3(0.35f, 1.0f, 0.35f), orangeMetalMat);
        }

        private static void BuildChandelierCrystal(GameObject parent)
        {
            CreatePrimitive(parent, "Chain", PrimitiveType.Cylinder, new Vector3(0f, 2.6f, 0f), new Vector3(0.04f, 0.4f, 0.04f), steelMat);
            CreatePrimitive(parent, "Ring", PrimitiveType.Cylinder, new Vector3(0f, 2.3f, 0f), new Vector3(1.2f, 0.08f, 1.2f), orangeMetalMat);
            CreatePrimitive(parent, "Crystals", PrimitiveType.Sphere, new Vector3(0f, 2.1f, 0f), new Vector3(1.0f, 0.4f, 1.0f), glassMat);
        }

        private static void BuildSlushieMachine(GameObject parent)
        {
            float d = 0.65f;
            CreatePrimitive(parent, "MachineBase", PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(0.85f, 0.8f, d), steelMat);
            CreatePrimitive(parent, "Bowl_L", PrimitiveType.Cylinder, new Vector3(-0.2f, 1.05f, 0f), new Vector3(0.32f, 0.4f, 0.32f), redAccentMat);
            CreatePrimitive(parent, "Bowl_R", PrimitiveType.Cylinder, new Vector3(0.2f, 1.05f, 0f), new Vector3(0.32f, 0.4f, 0.32f), cyanLedMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.10f, 0.85f, 0.95f));
        }

        private static void BuildMassageChair(GameObject parent)
        {
            float d = 1.1f;
            CreatePrimitive(parent, "SeatBase", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.7f, d), blackMat);
            CreatePrimitive(parent, "BackRest", PrimitiveType.Cube, new Vector3(0f, 0.85f, d / 3f), new Vector3(0.85f, 0.9f, 0.25f), blackMat);
            CreatePrimitive(parent, "HeadRest", PrimitiveType.Cube, new Vector3(0f, 1.35f, d / 3f), new Vector3(0.45f, 0.25f, 0.2f), blackMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.30f, 0.85f, 0.45f));
        }

        private static void BuildDonutDispenser(GameObject parent)
        {
            float d = 0.75f;
            CreatePrimitive(parent, "Cabinet", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, d), whiteGlossMat);
            CreatePrimitive(parent, "GlassDome", PrimitiveType.Cylinder, new Vector3(0f, 1.3f, 0f), new Vector3(0.75f, 0.8f, 0.75f), glassMat);
            BuildDirectionalArrowIndicator(parent, -d / 2f, new Color(0.95f, 0.40f, 0.65f));
        }

        private static void BuildHologramProjector(GameObject parent)
        {
            CreatePrimitive(parent, "BaseUnit", PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.8f, 0.15f, 0.8f), blackMat);
            CreatePrimitive(parent, "HoloSphere", PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), new Vector3(0.7f, 0.7f, 0.7f), cyanLedMat);
        }

        private static void BuildFlowerArch(GameObject parent)
        {
            float w = 2.4f, h = 2.4f;
            CreatePrimitive(parent, "Pillar_L", PrimitiveType.Cylinder, new Vector3(-w / 2f, h / 2f, 0f), new Vector3(0.3f, h, 0.3f), greenProduceMat);
            CreatePrimitive(parent, "Pillar_R", PrimitiveType.Cylinder, new Vector3(w / 2f, h / 2f, 0f), new Vector3(0.3f, h, 0.3f), greenProduceMat);
            CreatePrimitive(parent, "ArchTop", PrimitiveType.Cube, new Vector3(0f, h + 0.1f, 0f), new Vector3(w + 0.3f, 0.35f, 0.35f), redAccentMat);
        }

        // 9. Müşteri Hizmetleri Masası (Customer Service Desk)
        private static void BuildCustomerServiceDesk(GameObject parent)
        {
            float w = 1.8f;
            float h = 1.05f;
            float d = 1.0f;

            // Beyaz & Ahşap Lüks Danışma Masası Gövdesi
            CreatePrimitive(parent, "DeskBase", PrimitiveType.Cube, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d * 0.7f), whiteGlossMat);
            CreatePrimitive(parent, "WoodFrontPanel", PrimitiveType.Cube, new Vector3(0f, h / 2f + 0.05f, -d * 0.35f - 0.01f), new Vector3(w - 0.10f, h - 0.15f, 0.04f), woodMat);
            CreatePrimitive(parent, "CounterTop", PrimitiveType.Cube, new Vector3(0f, h, 0f), new Vector3(w + 0.10f, 0.06f, d * 0.85f), blackMat);

            // Altın Plaka "MÜŞTERİ HİZMETLERİ"
            CreatePrimitive(parent, "GoldPlaque", PrimitiveType.Cube, new Vector3(0f, h * 0.75f, -d * 0.35f - 0.03f), new Vector3(0.80f, 0.18f, 0.02f), goldMat);

            // Masa Üstü Ekipmanlar: Bilgisayar Monitörü, Klavye, Telefon & Evraklar
            CreatePrimitive(parent, "MonitorScreen", PrimitiveType.Cube, new Vector3(-0.35f, h + 0.28f, 0.05f), new Vector3(0.55f, 0.35f, 0.04f), blackMetalMat);
            CreatePrimitive(parent, "MonitorStand", PrimitiveType.Cylinder, new Vector3(-0.35f, h + 0.08f, 0.05f), new Vector3(0.08f, 0.15f, 0.08f), silverMetalMat);
            CreatePrimitive(parent, "Keyboard", PrimitiveType.Cube, new Vector3(-0.35f, h + 0.04f, -0.15f), new Vector3(0.40f, 0.02f, 0.15f), blackMat);
            CreatePrimitive(parent, "DesktopPhone", PrimitiveType.Cube, new Vector3(0.35f, h + 0.06f, -0.10f), new Vector3(0.20f, 0.08f, 0.22f), blackMetalMat);
            CreatePrimitive(parent, "DocumentFolder", PrimitiveType.Cube, new Vector3(0.55f, h + 0.03f, 0.10f), new Vector3(0.25f, 0.04f, 0.35f), redAccentMat);

            // Personel İçin Ergonomik Deri Ofis Koltuğu (Arka Tarafta)
            GameObject chairObj = new GameObject("Executive_Desk_Chair");
            chairObj.transform.SetParent(parent.transform, false);
            chairObj.transform.localPosition = new Vector3(0f, 0f, 0.45f);

            CreatePrimitive(chairObj, "ChairBase", PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.50f, 0.10f, 0.50f), blackMetalMat);
            CreatePrimitive(chairObj, "ChairPillar", PrimitiveType.Cylinder, new Vector3(0f, 0.32f, 0f), new Vector3(0.10f, 0.25f, 0.10f), silverMetalMat);
            CreatePrimitive(chairObj, "ChairSeat", PrimitiveType.Cube, new Vector3(0f, 0.48f, 0f), new Vector3(0.48f, 0.10f, 0.46f), blackMat);
            CreatePrimitive(chairObj, "ChairBack", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0.20f), new Vector3(0.46f, 0.65f, 0.10f), blackMat);

            // Müşterinin Duracağı Ön Hizalama Ok Göstergesi
            BuildDirectionalArrowIndicator(parent, -d / 2f - 0.2f, new Color(0.95f, 0.85f, 0.20f));
        }
    }
}
