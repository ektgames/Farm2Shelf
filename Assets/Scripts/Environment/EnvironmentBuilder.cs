using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Farm2Shelf haritasını (Tüm Sahne Kök Nesnelerini Güvenle Temizleyip Haritayı Kusursuz Oluşturan,
    /// Dükkan Tabanına Taşan Ahşap Kapı Çerçevesinin Duvar İçine Tam Gömülmesi ve Sıfırlanması,
    /// Tüm Asfalt Yol Şebekesini Dıştan ve İçten 360 Derece Çevreleyen Çift Halkalı Kaldırım Mimarisi,
    /// Binaların Kaldırımlara Taşmasını Önleyen Temiz Açıklıklı Kasaba Mimarisi, Yola Taşmayan Kusursuz Hizalı Kaldırımlar,
    /// Yaya Geçitlerinde Kaldırılmış Sarı Yol Çizgileri, Çeşmeli Kasaba Meydanını Ana Yola Bağlayan Bağlantı Kaldırımı,
    /// Mevcut Tüm Yapıları 100% Koruyarak Güney Bölgesine Dikdörtgen Çevre Yolu) oluşturan çevre yöneticisi.
    /// </summary>
    public class EnvironmentBuilder : MonoBehaviour
    {
        public static EnvironmentBuilder Instance { get; private set; }

        [Header("Root Container")]
        private Transform environmentRoot;

        [Header("Geliştirme Seviyesi")]
        private int currentUpgradeLevel = 1; // 1: Başlangıç (12 Araç), 2: Seviye 2 (18 Araç), 3: Seviye 3 (26 Araç)

        public static event Action<int> OnStoreUpgraded;

        // Materyal Deposu
        private Material grassMat;
        private Material darkWallMat;
        private Material storeFloorMat;
        private Material storageFloorMat;
        private Material staffRoomFloorMat;
        private Material sidewalkMat;
        private Material mainRoadMat;
        private Material roadLineMat;
        private Material crosswalkMat;
        private Material loadingZoneMat;
        private Material parkingLineMat;
        private Material barrierHousingMat;
        private Material barrierArmMat;
        private Material doorFrameMat;
        private Material mainDoorGlassMat;
        private Material storageDoorMat;
        private Material goodsDoorMat;
        private Material staffDoorMat;
        private Material doorHandleMat;

        // Çiftlik Materyalleri
        private Material footpathMat;
        private Material farmhouseWallMat;
        private Material farmhouseRoofMat;
        private Material barnWallMat;
        private Material barnRoofMat;
        private Material soilPlotMat;
        private Material soilBorderMat;
        private Material pondWaterMat;
        private Material pondStoneMat;
        private Material fenceWoodMat;
        private Material treeFoliageMat;
        private Material treeTrunkMat;

        // Kasaba Materyalleri
        private Material townSquareMat;
        private Material bakeryWallMat;
        private Material cafeWallMat;
        private Material townHallWallMat;
        private Material resWallBlueMat;
        private Material resWallYellowMat;
        private Material roofRedMat;
        private Material roofBlueMat;
        private Material roofBrownMat;
        private Material flowerRedMat;
        private Material flowerYellowMat;
        private Material wheatCropMat;
        private Material windowGlassMat;
        private Material windowFrameMat;
        private Material windowSillMat;
        private Material chimneyBrickMat;
        private Material woodDoorMat;
        private Material awningRedWhiteMat;
        private Material awningGreenMat;
        private Material pillarStoneMat;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshAll3DWorldLabels;
                LocalizationManager.Instance.OnLanguageChanged += RefreshAll3DWorldLabels;
            }
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshAll3DWorldLabels;
            }
        }

        private void Start()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshAll3DWorldLabels;
                LocalizationManager.Instance.OnLanguageChanged += RefreshAll3DWorldLabels;
            }
            RefreshAll3DWorldLabels();
        }

        public void RefreshAll3DWorldLabels(GameLanguage lang = GameLanguage.Turkish)
        {
            activeWorldLabels.RemoveAll(info => info == null || info.mesh == null);
            foreach (var info in activeWorldLabels)
            {
                if (info.mesh != null)
                {
                    info.mesh.text = LocalizationManager.L("Label3D_" + info.textTr, info.textTr, info.textEn);
                }
            }
        }

        public void BuildEnvironment()
        {
            activeWorldLabels.Clear();

            // Sahnedeki eski harita kökünü temizle
            GameObject existing = GameObject.Find("Farm2Shelf_Environment");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
            }

            CleanStrayObjectsInStoreArea();

            environmentRoot = new GameObject("Farm2Shelf_Environment").transform;

            InitializeMaterials();
            CreateGrassTerrain();
            CreateRectangleRingRoadAndLanes();
            CreateSidewalk();
            CreateUnifiedBuilding();
            CreateSingleLaneDeliveryRoad();
            CreateCustomerParkingLotAndTurnstile();
            CreateCleanFarmComplex();
            CreateTownshipSystem();
            CreateLightingAndDecorations();
            CreateDenseTwoRowTreeBoundaryWall();

            RefreshAll3DWorldLabels();

            Debug.Log($"[Farm2Shelf] Seviye {currentUpgradeLevel} Harita Mimarisi ve Dükkan Alanı Başarıyla Yeniden Oluşturuldu!");
        }

        private void CleanStrayObjectsInStoreArea()
        {
            // 1. Sahnedeki tüm kök (root) nesneleri tara ve dükkan içi/eski sahipsiz çit/yapı nesnelerini temizle
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootGo in rootObjects)
            {
                if (rootGo == null) continue;
                string n = rootGo.name;
                
                // Ana sistem yöneticileri, mobilya konteyneri, kamera ve UI tuvalini KESİNLİKLE KORU
                if (n == "Farm2Shelf_Environment" || n == "Core_Managers" || n == "UI_Manager" ||
                    n == "EnvironmentManager" || n == "[Farm2ShelfBootstrapper]" ||
                    n == "Placed_Furniture_Container" || n.Contains("Placed_Furniture") || n.Contains("PlacedFurniture") ||
                    n == "Main Camera" || n == "Directional Light" || n == "EventSystem" || 
                    n == "Farm2Shelf_HUD_Canvas" || n.Contains("Canvas") || n.Contains("Camera"))
                {
                    continue;
                }

                Vector3 pos = rootGo.transform.position;
                bool isInsideStoreBounds = (pos.x >= -14.0f && pos.x <= 4.0f && pos.z >= -4.0f && pos.z <= 20.0f);

                string lowerName = n.ToLower();
                bool isStrayObject = lowerName.Contains("fence") || lowerName.Contains("wooden") || lowerName.Contains("wood") ||
                                     lowerName.Contains("table") || lowerName.Contains("bench") || lowerName.Contains("legacy") ||
                                     lowerName.Contains("prop") || lowerName.Contains("building") || lowerName.Contains("cube") ||
                                     lowerName.Contains("barrier") || lowerName.Contains("structure") || lowerName.Contains("obstacle") ||
                                     lowerName.Contains("road") || lowerName.Contains("asphalt") || lowerName.Contains("sidewalk");

                if (isInsideStoreBounds || isStrayObject)
                {
                    rootGo.SetActive(false);
                    if (Application.isPlaying) Destroy(rootGo);
                    else DestroyImmediate(rootGo);
                }
            }

            // 2. Sahnedeki sahipsiz (herhangi bir kök objesinin altındaki) tüm dükkan içi çit/tezgah nesnelerini derinlemesine tara
#if UNITY_2023_1_OR_NEWER
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
#endif
            foreach (GameObject go in allObjects)
            {
                if (go == null) continue;
                
                string lowerName = go.name.ToLower();
                bool isFence = lowerName.Contains("fence") || lowerName.Contains("wooden_fence");

                Transform topParent = go.transform.root;
                string topName = topParent != null ? topParent.name : "";

                bool isProtectedParent = (topName == "Farm2Shelf_Environment" || topName == "Core_Managers" || topName == "UI_Manager" ||
                    topName == "EnvironmentManager" || topName == "[Farm2ShelfBootstrapper]" ||
                    topName == "Placed_Furniture_Container" || topName.Contains("Placed_Furniture") || topName.Contains("PlacedFurniture") ||
                    topName == "Main Camera" || topName == "Directional Light" || topName == "EventSystem" || 
                    topName == "Farm2Shelf_HUD_Canvas" || topName.Contains("Canvas") || topName.Contains("Camera"));

                // Çitler KESİNLİKLE hiçbir yerde kullanılmadığı için korumalı parent altındakiler dahil TÜM ÇİTLERİ SİL!
                if (isFence || (!isProtectedParent && (go.transform.position.x >= -14f && go.transform.position.x <= 4f && go.transform.position.z >= -4f && go.transform.position.z <= 25f)))
                {
                    go.SetActive(false);
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }
        }

        public bool TryUpgradeStore(int targetLevel, int cost)
        {
            if (currentUpgradeLevel >= targetLevel) return false;
            if (targetLevel != currentUpgradeLevel + 1) return false;

            if (Farm2Shelf.Core.EconomyManager.Instance != null)
            {
                if (Farm2Shelf.Core.EconomyManager.Instance.TrySpendCredits(cost))
                {
                    currentUpgradeLevel = targetLevel;

                    if (Farm2Shelf.Core.FinanceManager.Instance != null)
                    {
                        Farm2Shelf.Core.FinanceManager.Instance.RecordExpense("Geliştirme", $"Market Seviye {targetLevel} Genişletme", cost);
                    }

                    BuildEnvironment();
                    OnStoreUpgraded?.Invoke(currentUpgradeLevel);

                    Debug.Log($"[Farm2Shelf] MARKET GELİŞTİRİLDİ! Yeni Seviye: {currentUpgradeLevel} (Harcama: {cost:N0} Credit)");
                    return true;
                }
            }
            return false;
        }

        public void UpgradeStoreToLevel(int targetLevel)
        {
            if (targetLevel < 1 || targetLevel > 3) return;
            currentUpgradeLevel = targetLevel;
            BuildEnvironment();
            OnStoreUpgraded?.Invoke(currentUpgradeLevel);
            Debug.Log($"[Farm2Shelf] Market Seviyesi Yüklendi: {currentUpgradeLevel}");
        }

        public int CurrentUpgradeLevel => currentUpgradeLevel;

        private void InitializeMaterials()
        {
            // Çevre & Yol
            grassMat = CreateSolidMaterial("GrassMat", new Color(0.28f, 0.62f, 0.28f));
            darkWallMat = CreateSolidMaterial("DarkWallMat", new Color(0.12f, 0.14f, 0.17f));
            storeFloorMat = CreateSolidMaterial("StoreFloorMat", new Color(0.85f, 0.72f, 0.53f));
            storageFloorMat = CreateSolidMaterial("StorageFloorMat", new Color(0.45f, 0.50f, 0.55f));
            staffRoomFloorMat = CreateSolidMaterial("StaffRoomFloorMat", new Color(0.35f, 0.45f, 0.58f));
            sidewalkMat = CreateSolidMaterial("SidewalkMat", new Color(0.70f, 0.72f, 0.75f));
            mainRoadMat = CreateSolidMaterial("MainRoadMat", new Color(0.18f, 0.20f, 0.22f));
            roadLineMat = CreateSolidMaterial("RoadLineMat", new Color(0.95f, 0.80f, 0.15f));
            crosswalkMat = CreateSolidMaterial("CrosswalkMat", new Color(0.96f, 0.96f, 0.98f));
            loadingZoneMat = CreateSolidMaterial("LoadingZoneMat", new Color(0.15f, 0.16f, 0.18f));
            parkingLineMat = CreateSolidMaterial("ParkingLineMat", new Color(0.96f, 0.96f, 0.98f));
            barrierHousingMat = CreateSolidMaterial("BarrierHousingMat", new Color(0.95f, 0.55f, 0.05f));
            barrierArmMat = CreateSolidMaterial("BarrierArmMat", new Color(0.85f, 0.15f, 0.15f));

            // Gerçekçi Kapı Materyalleri
            doorFrameMat = CreateSolidMaterial("DoorFrameMat", new Color(0.18f, 0.20f, 0.24f), 0.0f, 0.2f);
            mainDoorGlassMat = CreateSolidMaterial("MainDoorGlassMat", new Color(0.23f, 0.75f, 0.96f, 1.0f), 0.0f, 0.25f);
            storageDoorMat = CreateSolidMaterial("StorageDoorMat", new Color(0.18f, 0.20f, 0.24f), 0.0f, 0.2f);
            goodsDoorMat = CreateSolidMaterial("GoodsDoorMat", new Color(0.20f, 0.65f, 0.38f), 0.0f, 0.2f);
            staffDoorMat = CreateSolidMaterial("StaffDoorMat", new Color(0.42f, 0.48f, 0.58f, 1.0f), 0.0f, 0.2f);
            doorHandleMat = CreateSolidMaterial("DoorHandleMat", new Color(0.90f, 0.92f, 0.95f), 0.2f, 0.3f);

            // Çiftlik Materyalleri
            footpathMat = CreateSolidMaterial("FootpathMat", new Color(0.75f, 0.68f, 0.55f));
            farmhouseWallMat = CreateSolidMaterial("FarmhouseWallMat", new Color(0.88f, 0.78f, 0.62f));
            farmhouseRoofMat = CreateSolidMaterial("FarmhouseRoofMat", new Color(0.82f, 0.25f, 0.18f));
            barnWallMat = CreateSolidMaterial("BarnWallMat", new Color(0.72f, 0.18f, 0.15f));
            barnRoofMat = CreateSolidMaterial("BarnRoofMat", new Color(0.28f, 0.30f, 0.35f));
            soilPlotMat = CreateSolidMaterial("SoilPlotMat", new Color(0.28f, 0.18f, 0.10f));
            soilBorderMat = CreateSolidMaterial("SoilBorderMat", new Color(0.20f, 0.12f, 0.06f));
            pondWaterMat = CreateSolidMaterial("PondWaterMat", new Color(0.18f, 0.65f, 0.88f));
            pondStoneMat = CreateSolidMaterial("PondStoneMat", new Color(0.55f, 0.58f, 0.60f));
            fenceWoodMat = CreateSolidMaterial("FenceWoodMat", new Color(0.58f, 0.38f, 0.20f));
            treeFoliageMat = CreateSolidMaterial("TreeFoliageMat", new Color(0.18f, 0.55f, 0.22f));
            treeTrunkMat = CreateSolidMaterial("TreeTrunkMat", new Color(0.40f, 0.26f, 0.14f));

            // Kasaba Materyalleri
            townSquareMat = CreateSolidMaterial("TownSquareMat", new Color(0.65f, 0.68f, 0.72f));
            bakeryWallMat = CreateSolidMaterial("BakeryWallMat", new Color(0.85f, 0.52f, 0.32f));
            cafeWallMat = CreateSolidMaterial("CafeWallMat", new Color(0.92f, 0.88f, 0.78f));
            townHallWallMat = CreateSolidMaterial("TownHallWallMat", new Color(0.35f, 0.45f, 0.55f));
            resWallBlueMat = CreateSolidMaterial("ResWallBlueMat", new Color(0.45f, 0.65f, 0.82f));
            resWallYellowMat = CreateSolidMaterial("ResWallYellowMat", new Color(0.92f, 0.82f, 0.45f));
            roofRedMat = CreateSolidMaterial("RoofRedMat", new Color(0.78f, 0.22f, 0.18f));
            roofBlueMat = CreateSolidMaterial("RoofBlueMat", new Color(0.20f, 0.38f, 0.60f));
            roofBrownMat = CreateSolidMaterial("RoofBrownMat", new Color(0.42f, 0.28f, 0.18f));
            flowerRedMat = CreateSolidMaterial("FlowerRedMat", new Color(0.95f, 0.25f, 0.35f));
            flowerYellowMat = CreateSolidMaterial("FlowerYellowMat", new Color(0.98f, 0.85f, 0.15f));
            wheatCropMat = CreateSolidMaterial("WheatCropMat", new Color(0.92f, 0.78f, 0.25f));

            // Mimari Bina Detay Materyalleri
            windowGlassMat = CreateSolidMaterial("WindowGlassMat", new Color(0.35f, 0.70f, 0.90f, 1.0f), 0.0f, 0.20f);
            windowFrameMat = CreateSolidMaterial("WindowFrameMat", new Color(0.95f, 0.95f, 0.95f));
            windowSillMat = CreateSolidMaterial("WindowSillMat", new Color(0.80f, 0.82f, 0.85f));
            chimneyBrickMat = CreateSolidMaterial("ChimneyBrickMat", new Color(0.65f, 0.28f, 0.20f));
            woodDoorMat = CreateSolidMaterial("WoodDoorMat", new Color(0.48f, 0.30f, 0.16f));
            awningRedWhiteMat = CreateSolidMaterial("AwningRedWhiteMat", new Color(0.85f, 0.20f, 0.20f));
            awningGreenMat = CreateSolidMaterial("AwningGreenMat", new Color(0.20f, 0.65f, 0.30f));
            pillarStoneMat = CreateSolidMaterial("PillarStoneMat", new Color(0.90f, 0.90f, 0.92f));
        }

        private Material CreateSolidMaterial(string name, Color color, float metallic = 0.0f, float smoothness = 0.5f)
        {
            Shader shader = ShaderHelper.GetLitShader();
            if (shader == null)
            {
                Debug.LogError($"[EnvironmentBuilder] HATA: '{name}' materyali için 3D URP shader null döndü! Çökme önlendi.");
                return null;
            }

            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (color.a < 1.0f)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            return mat;
        }

        private void CreateGrassTerrain()
        {
            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grass.name = "Grass_Terrain_Ground";
            grass.transform.SetParent(environmentRoot);
            grass.transform.position = new Vector3(0f, -0.15f, -20f);
            grass.transform.localScale = new Vector3(220f, 0.1f, 260f);
            grass.GetComponent<Renderer>().sharedMaterial = grassMat;
        }

        private void CreateRectangleRingRoadAndLanes()
        {
            Transform roadGroup = new GameObject("Rectangle_Ring_Road_System").transform;
            roadGroup.SetParent(environmentRoot);

            // 1. GÜNEY ANA ASFALT YOL (KASABA ALT YOLU)
            GameObject southRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            southRoad.name = "South_Asphalt_Road";
            southRoad.transform.SetParent(roadGroup);
            southRoad.transform.position = new Vector3(0f, -0.05f, -55f);
            southRoad.transform.localScale = new Vector3(156f, 0.1f, 6f);
            southRoad.GetComponent<Renderer>().sharedMaterial = mainRoadMat;

            for (float x = -75f; x <= 75f; x += 3f)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Road_Center_Line";
                line.transform.SetParent(roadGroup);
                line.transform.position = new Vector3(x, 0.01f, -55f);
                line.transform.localScale = new Vector3(1.8f, 0.02f, 0.25f);
                line.GetComponent<Renderer>().sharedMaterial = roadLineMat;
            }

            // 2. ORTA BÖLGE ANA OTOYOLU (KASABA DIŞINA, UFKA VE SINIR UZAKLIĞINA KADAR UZANAN UZUN ANA BAĞLANTI OTOYOLU - 400m)
            GameObject midRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            midRoad.name = "Mid_Asphalt_Road";
            midRoad.transform.SetParent(roadGroup);
            midRoad.transform.position = new Vector3(0f, -0.05f, -9f);
            midRoad.transform.localScale = new Vector3(400f, 0.1f, 6f);
            midRoad.GetComponent<Renderer>().sharedMaterial = mainRoadMat;

            for (float x = -195f; x <= 195f; x += 3f)
            {
                if (Mathf.Abs(x - (-5.0f)) <= 2.5f || Mathf.Abs(x - 0.0f) <= 3.2f || Mathf.Abs(x - 13.0f) <= 2.5f || 
                    (x >= -21.0f && x <= -10.0f) || Mathf.Abs(x - (-79.5f)) <= 2.0f || Mathf.Abs(x - (79.5f)) <= 2.0f)
                {
                    continue;
                }

                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Road_Center_Line";
                line.transform.SetParent(roadGroup);
                line.transform.position = new Vector3(x, 0.01f, -9f);
                line.transform.localScale = new Vector3(1.8f, 0.02f, 0.25f);
                line.GetComponent<Renderer>().sharedMaterial = roadLineMat;
            }

            // 3. UZATILMIŞ KUZEY ANA ASFALT YOL (DÜKKAN, OTOPARK VE ÇİFTLİK ÜSTÜ SINIR YOLU - FERAH Z=50.0m)
            GameObject northRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            northRoad.name = "North_Asphalt_Road_Extended";
            northRoad.transform.SetParent(roadGroup);
            northRoad.transform.position = new Vector3(0f, -0.05f, 50f);
            northRoad.transform.localScale = new Vector3(156f, 0.1f, 6f);
            northRoad.GetComponent<Renderer>().sharedMaterial = mainRoadMat;

            for (float x = -75f; x <= 75f; x += 3f)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Road_Center_Line";
                line.transform.SetParent(roadGroup);
                line.transform.position = new Vector3(x, 0.01f, 50f);
                line.transform.localScale = new Vector3(1.8f, 0.02f, 0.25f);
                line.GetComponent<Renderer>().sharedMaterial = roadLineMat;
            }

            // 4. BATI ASFALT YOLU (TÜM YOL ŞEBEKESİNİ GÜNEYDEN KUZEYE 360 DERECE BİRLEŞTİREN SOL DİKEY YOL)
            GameObject westRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westRoad.name = "West_Asphalt_Road_Full";
            westRoad.transform.SetParent(roadGroup);
            westRoad.transform.position = new Vector3(-75f, -0.05f, -2.5f);
            westRoad.transform.localScale = new Vector3(6f, 0.1f, 114f);
            westRoad.GetComponent<Renderer>().sharedMaterial = mainRoadMat;

            for (float z = -52f; z <= 47f; z += 3f)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Road_Center_Line";
                line.transform.SetParent(roadGroup);
                line.transform.position = new Vector3(-75f, 0.01f, z);
                line.transform.localScale = new Vector3(0.25f, 0.02f, 1.8f);
                line.GetComponent<Renderer>().sharedMaterial = roadLineMat;
            }

            // 5. DOĞU ASFALT YOLU (TÜM YOL ŞEBEKESİNİ GÜNEYDEN KUZEYE 360 DERECE BİRLEŞTİREN SAĞ DİKEY YOL)
            GameObject eastRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastRoad.name = "East_Asphalt_Road_Full";
            eastRoad.transform.SetParent(roadGroup);
            eastRoad.transform.position = new Vector3(75f, -0.05f, -2.5f);
            eastRoad.transform.localScale = new Vector3(6f, 0.1f, 114f);
            eastRoad.GetComponent<Renderer>().sharedMaterial = mainRoadMat;

            for (float z = -52f; z <= 47f; z += 3f)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Road_Center_Line";
                line.transform.SetParent(roadGroup);
                line.transform.position = new Vector3(75f, 0.01f, z);
                line.transform.localScale = new Vector3(0.25f, 0.02f, 1.8f);
                line.GetComponent<Renderer>().sharedMaterial = roadLineMat;
            }
        }

        private void CreateSidewalk()
        {
            Transform sidewalkGroup = new GameObject("Sidewalk_System").transform;
            sidewalkGroup.SetParent(environmentRoot);

            // DIŞ KALDIRIMLAR (EN DIŞ SINIR - ORTA OTOYOL KAVŞAĞINDA KESİLEREK DIŞ YAYA GEÇİTLERİ EKLENDİ)
            GameObject northOuterSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            northOuterSidewalk.name = "North_Outer_Sidewalk_TopBoundary";
            northOuterSidewalk.transform.SetParent(sidewalkGroup);
            northOuterSidewalk.transform.position = new Vector3(0f, 0.05f, 54.5f);
            northOuterSidewalk.transform.localScale = new Vector3(162f, 0.2f, 3f);
            northOuterSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject southOuterSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            southOuterSidewalk.name = "South_Outer_Sidewalk";
            southOuterSidewalk.transform.SetParent(sidewalkGroup);
            southOuterSidewalk.transform.position = new Vector3(0f, 0.05f, -59.5f);
            southOuterSidewalk.transform.localScale = new Vector3(162f, 0.2f, 3f);
            southOuterSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            // Batı Dış Kaldırımı (Orta otoyol kavşağında Z: -12m ile -6m arası kesildi, tam hizalandı)
            GameObject westOuterSidewalkTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westOuterSidewalkTop.name = "West_Outer_Sidewalk_Top";
            westOuterSidewalkTop.transform.SetParent(sidewalkGroup);
            westOuterSidewalkTop.transform.position = new Vector3(-79.5f, 0.05f, 25.0f);
            westOuterSidewalkTop.transform.localScale = new Vector3(3f, 0.2f, 62.0f);
            westOuterSidewalkTop.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject westOuterSidewalkBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westOuterSidewalkBottom.name = "West_Outer_Sidewalk_Bottom";
            westOuterSidewalkBottom.transform.SetParent(sidewalkGroup);
            westOuterSidewalkBottom.transform.position = new Vector3(-79.5f, 0.05f, -36.5f);
            westOuterSidewalkBottom.transform.localScale = new Vector3(3f, 0.2f, 49.0f);
            westOuterSidewalkBottom.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            // Doğu Dış Kaldırımı (Orta otoyol kavşağında Z: -12m ile -6m arası kesildi, tam hizalandı)
            GameObject eastOuterSidewalkTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastOuterSidewalkTop.name = "East_Outer_Sidewalk_Top";
            eastOuterSidewalkTop.transform.SetParent(sidewalkGroup);
            eastOuterSidewalkTop.transform.position = new Vector3(79.5f, 0.05f, 25.0f);
            eastOuterSidewalkTop.transform.localScale = new Vector3(3f, 0.2f, 62.0f);
            eastOuterSidewalkTop.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject eastOuterSidewalkBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastOuterSidewalkBottom.name = "East_Outer_Sidewalk_Bottom";
            eastOuterSidewalkBottom.transform.SetParent(sidewalkGroup);
            eastOuterSidewalkBottom.transform.position = new Vector3(79.5f, 0.05f, -36.5f);
            eastOuterSidewalkBottom.transform.localScale = new Vector3(3f, 0.2f, 49.0f);
            eastOuterSidewalkBottom.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            // DIŞ KALDIRIM YAYA GEÇİTLERİ (KASABA DIŞINA UZANAN ANA OTOYOL ÜZERİNDE YAYALAR İÇİN ZEBRA GEÇİTLER)
            Transform westOuterCrosswalk = new GameObject("Pedestrian_Crosswalk_Outer_West").transform;
            westOuterCrosswalk.SetParent(sidewalkGroup);
            for (float z = -11.5f; z <= -6.5f; z += 0.75f)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Outer_Zebra_Stripe_West";
                stripe.transform.SetParent(westOuterCrosswalk);
                stripe.transform.position = new Vector3(-79.5f, 0.02f, z);
                stripe.transform.localScale = new Vector3(3.0f, 0.01f, 0.40f);
                stripe.GetComponent<Renderer>().sharedMaterial = crosswalkMat;
            }

            Transform eastOuterCrosswalk = new GameObject("Pedestrian_Crosswalk_Outer_East").transform;
            eastOuterCrosswalk.SetParent(sidewalkGroup);
            for (float z = -11.5f; z <= -6.5f; z += 0.75f)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Outer_Zebra_Stripe_East";
                stripe.transform.SetParent(eastOuterCrosswalk);
                stripe.transform.position = new Vector3(79.5f, 0.02f, z);
                stripe.transform.localScale = new Vector3(3.0f, 0.01f, 0.40f);
                stripe.GetComponent<Renderer>().sharedMaterial = crosswalkMat;
            }

            // ORTA OTOYOL DIŞ UZANTI KALDIRIMLARI (SOL VE SAĞ UFKA UZANAN KALDIRIMLAR)
            // Batı Otoyolu Kuzey ve Güney Kaldırımları (X: -200m ile -81m arası)
            GameObject westHighwayNorthSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westHighwayNorthSidewalk.name = "West_Highway_North_Sidewalk";
            westHighwayNorthSidewalk.transform.SetParent(sidewalkGroup);
            westHighwayNorthSidewalk.transform.position = new Vector3(-140.5f, 0.05f, -4.5f);
            westHighwayNorthSidewalk.transform.localScale = new Vector3(119.0f, 0.2f, 3.0f);
            westHighwayNorthSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject westHighwaySouthSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westHighwaySouthSidewalk.name = "West_Highway_South_Sidewalk";
            westHighwaySouthSidewalk.transform.SetParent(sidewalkGroup);
            westHighwaySouthSidewalk.transform.position = new Vector3(-140.5f, 0.05f, -13.5f);
            westHighwaySouthSidewalk.transform.localScale = new Vector3(119.0f, 0.2f, 3.0f);
            westHighwaySouthSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            // Doğu Otoyolu Kuzey ve Güney Kaldırımları (X: +81m ile +200m arası)
            GameObject eastHighwayNorthSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastHighwayNorthSidewalk.name = "East_Highway_North_Sidewalk";
            eastHighwayNorthSidewalk.transform.SetParent(sidewalkGroup);
            eastHighwayNorthSidewalk.transform.position = new Vector3(140.5f, 0.05f, -4.5f);
            eastHighwayNorthSidewalk.transform.localScale = new Vector3(119.0f, 0.2f, 3.0f);
            eastHighwayNorthSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject eastHighwaySouthSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastHighwaySouthSidewalk.name = "East_Highway_South_Sidewalk";
            eastHighwaySouthSidewalk.transform.SetParent(sidewalkGroup);
            eastHighwaySouthSidewalk.transform.position = new Vector3(140.5f, 0.05f, -13.5f);
            eastHighwaySouthSidewalk.transform.localScale = new Vector3(119.0f, 0.2f, 3.0f);
            eastHighwaySouthSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            // İÇ KALDIRIMLAR (KASABA HALKASI & KUZEY HALKASI)
            // KUZEY HALKA İÇ KALDIRIMI (Teslimat yolu bağlantısında X: 11.0f ile 15.0f arası kesilerek asfalt açıldı)
            GameObject northRingInnerSidewalkLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            northRingInnerSidewalkLeft.name = "North_Ring_Inner_Sidewalk_Left";
            northRingInnerSidewalkLeft.transform.SetParent(sidewalkGroup);
            northRingInnerSidewalkLeft.transform.position = new Vector3(-30.5f, 0.05f, 45.5f);
            northRingInnerSidewalkLeft.transform.localScale = new Vector3(83.0f, 0.2f, 3f);
            northRingInnerSidewalkLeft.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject northRingInnerSidewalkRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            northRingInnerSidewalkRight.name = "North_Ring_Inner_Sidewalk_Right";
            northRingInnerSidewalkRight.transform.SetParent(sidewalkGroup);
            northRingInnerSidewalkRight.transform.position = new Vector3(43.5f, 0.05f, 45.5f);
            northRingInnerSidewalkRight.transform.localScale = new Vector3(57.0f, 0.2f, 3f);
            northRingInnerSidewalkRight.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            // ORTA ORTASI DÜKKAN TARAFI KALDIRIMI (Batı yolu, Çift turnike ve Teslimat yolu kavşaklarında TAM KESİLEREK pürüzsüz asfalt açıldı)
            // 1. Sol segment: Batı Yolu iç kenarı (X=-72) ile Çıkış Turnikesi (X=-20) arası
            GameObject midOuterSidewalkShopSideLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            midOuterSidewalkShopSideLeft.name = "Mid_Outer_Sidewalk_ShopSide_Left";
            midOuterSidewalkShopSideLeft.transform.SetParent(sidewalkGroup);
            midOuterSidewalkShopSideLeft.transform.position = new Vector3(-46.0f, 0.05f, -4.5f);
            midOuterSidewalkShopSideLeft.transform.localScale = new Vector3(52.0f, 0.2f, 3f);
            midOuterSidewalkShopSideLeft.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            // 2. Orta segment: Giriş Turnikesi (X=-14) ile Teslimat Yolu sol kenarı (X=11) arası
            GameObject midOuterSidewalkShopSideCenter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            midOuterSidewalkShopSideCenter.name = "Mid_Outer_Sidewalk_ShopSide_Center";
            midOuterSidewalkShopSideCenter.transform.SetParent(sidewalkGroup);
            midOuterSidewalkShopSideCenter.transform.position = new Vector3(-1.5f, 0.05f, -4.5f);
            midOuterSidewalkShopSideCenter.transform.localScale = new Vector3(25.0f, 0.2f, 3f);
            midOuterSidewalkShopSideCenter.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            // 3. Sağ segment: Teslimat Yolu sağ kenarı (X=15) ile Doğu Yolu iç kenarı (X=72) arası
            GameObject midOuterSidewalkShopSideRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            midOuterSidewalkShopSideRight.name = "Mid_Outer_Sidewalk_ShopSide_Right";
            midOuterSidewalkShopSideRight.transform.SetParent(sidewalkGroup);
            midOuterSidewalkShopSideRight.transform.position = new Vector3(43.5f, 0.05f, -4.5f);
            midOuterSidewalkShopSideRight.transform.localScale = new Vector3(57.0f, 0.2f, 3f);
            midOuterSidewalkShopSideRight.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject northInnerSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            northInnerSidewalk.name = "North_Inner_Sidewalk_TownSide";
            northInnerSidewalk.transform.SetParent(sidewalkGroup);
            northInnerSidewalk.transform.position = new Vector3(0f, 0.05f, -13.5f);
            northInnerSidewalk.transform.localScale = new Vector3(144f, 0.2f, 3f);
            northInnerSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject southInnerSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            southInnerSidewalk.name = "South_Inner_Sidewalk";
            southInnerSidewalk.transform.SetParent(sidewalkGroup);
            southInnerSidewalk.transform.position = new Vector3(0f, 0.05f, -50.5f);
            southInnerSidewalk.transform.localScale = new Vector3(144f, 0.2f, 3f);
            southInnerSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject westInnerSidewalkTown = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westInnerSidewalkTown.name = "West_Inner_Sidewalk_Town";
            westInnerSidewalkTown.transform.SetParent(sidewalkGroup);
            westInnerSidewalkTown.transform.position = new Vector3(-70.5f, 0.05f, -32.0f);
            westInnerSidewalkTown.transform.localScale = new Vector3(3f, 0.2f, 40f);
            westInnerSidewalkTown.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject eastInnerSidewalkTown = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastInnerSidewalkTown.name = "East_Inner_Sidewalk_Town";
            eastInnerSidewalkTown.transform.SetParent(sidewalkGroup);
            eastInnerSidewalkTown.transform.position = new Vector3(70.5f, 0.05f, -32.0f);
            eastInnerSidewalkTown.transform.localScale = new Vector3(3f, 0.2f, 40f);
            eastInnerSidewalkTown.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject westInnerSidewalkNorthRing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westInnerSidewalkNorthRing.name = "West_Inner_Sidewalk_NorthRing";
            westInnerSidewalkNorthRing.transform.SetParent(sidewalkGroup);
            westInnerSidewalkNorthRing.transform.position = new Vector3(-70.5f, 0.05f, 20.5f);
            westInnerSidewalkNorthRing.transform.localScale = new Vector3(3f, 0.2f, 53f);
            westInnerSidewalkNorthRing.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject eastInnerSidewalkNorthRing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastInnerSidewalkNorthRing.name = "East_Inner_Sidewalk_NorthRing";
            eastInnerSidewalkNorthRing.transform.SetParent(sidewalkGroup);
            eastInnerSidewalkNorthRing.transform.position = new Vector3(70.5f, 0.05f, 20.5f);
            eastInnerSidewalkNorthRing.transform.localScale = new Vector3(3f, 0.2f, 53f);
            eastInnerSidewalkNorthRing.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject fountainToMainSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fountainToMainSidewalk.name = "Fountain_Square_To_MainSidewalk_Connector";
            fountainToMainSidewalk.transform.SetParent(sidewalkGroup);
            fountainToMainSidewalk.transform.position = new Vector3(0f, 0.05f, -17.5f);
            fountainToMainSidewalk.transform.localScale = new Vector3(4.5f, 0.2f, 5.0f);
            fountainToMainSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            Transform centerCrosswalk = new GameObject("Center_Main_Crosswalk").transform;
            centerCrosswalk.SetParent(sidewalkGroup);

            for (float x = -2.5f; x <= 2.5f; x += 0.7f)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Center_Zebra_Stripe";
                stripe.transform.SetParent(centerCrosswalk);
                stripe.transform.position = new Vector3(x, 0.02f, -9.0f);
                stripe.transform.localScale = new Vector3(0.40f, 0.01f, 6.0f);
                stripe.GetComponent<Renderer>().sharedMaterial = crosswalkMat;
            }

            // Yaya Geçidinin Sağ Çaprazına 3D Otobüs Durağı Tabelası, Kabin ve Yol Çizgilerini Kur
            BuildBusStopSign(sidewalkGroup);
        }

        private void BuildBusStopSign(Transform parent)
        {
            Transform busStopGroup = new GameObject("Bus_Stop_Sign_Group").transform;
            busStopGroup.SetParent(parent);
            busStopGroup.position = new Vector3(4.5f, 0.05f, -5.8f); // Yaya geçidinin sağ çaprazındaki kaldırım

            // 1. Ana Mavi Metalik Direk
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "BusStop_Pole";
            pole.transform.SetParent(busStopGroup, false);
            pole.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            pole.transform.localScale = new Vector3(0.08f, 1.2f, 0.08f);
            pole.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("BusStopPoleMat", new Color(0.12f, 0.25f, 0.55f), 0.7f, 0.8f);

            // 2. Dairesel Mavi/Beyaz "D / BUS" Tabelası Header
            GameObject headerSign = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            headerSign.name = "BusStop_HeaderSign";
            headerSign.transform.SetParent(busStopGroup, false);
            headerSign.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            headerSign.transform.localScale = new Vector3(0.65f, 0.04f, 0.65f);
            headerSign.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            headerSign.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("BusStopHeaderMat", new Color(0.10f, 0.35f, 0.85f), 0.2f, 0.7f);

            // Tabela İç Harf "D" Simgesi (Koyu Sarı D Plakası)
            GameObject dPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dPlate.name = "BusStop_D_Plate";
            dPlate.transform.SetParent(headerSign.transform, false);
            dPlate.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            dPlate.transform.localScale = new Vector3(0.50f, 0.02f, 0.50f);
            dPlate.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("BusStopDMat", new Color(0.95f, 0.82f, 0.15f), 0.1f, 0.6f);

            // 3. Camlı Otobüs Durağı Kabini & Bank (Bus Shelter)
            GameObject shelter = new GameObject("BusStop_Shelter");
            shelter.transform.SetParent(busStopGroup, false);
            shelter.transform.localPosition = new Vector3(0f, 0f, 0.60f); // Kaldırım içi

            // Kabin Çatı
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Shelter_Roof";
            roof.transform.SetParent(shelter.transform, false);
            roof.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            roof.transform.localScale = new Vector3(2.6f, 0.08f, 1.2f);
            roof.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("ShelterRoofMat", new Color(0.20f, 0.22f, 0.28f), 0.8f, 0.9f);

            // Kabin Arka Camı
            GameObject backGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backGlass.name = "Shelter_BackGlass";
            backGlass.transform.SetParent(shelter.transform, false);
            backGlass.transform.localPosition = new Vector3(0f, 1.1f, 0.55f);
            backGlass.transform.localScale = new Vector3(2.5f, 2.0f, 0.04f);
            backGlass.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("ShelterGlassMat", new Color(0.25f, 0.65f, 0.85f, 0.40f), 0.1f, 0.95f);

            // Kabin Oturma Bankı
            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Shelter_Bench";
            bench.transform.SetParent(shelter.transform, false);
            bench.transform.localPosition = new Vector3(0f, 0.45f, 0.35f);
            bench.transform.localScale = new Vector3(2.0f, 0.06f, 0.38f);
            bench.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("ShelterBenchMat", new Color(0.65f, 0.42f, 0.22f), 0.0f, 0.4f);

            // 4. Yolda Sarı Otobüs Durağı Çizgileri & Sarı Zemin Sınırı
            GameObject roadBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadBox.name = "Road_BusStop_Marking";
            roadBox.transform.SetParent(busStopGroup, false);
            roadBox.transform.localPosition = new Vector3(0f, -0.03f, -1.6f); // Yola sıfır
            roadBox.transform.localScale = new Vector3(8.0f, 0.01f, 0.35f);
            roadBox.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("BusStopLineMat", new Color(0.95f, 0.85f, 0.15f), 0.1f, 0.5f);
        }

        private void CreateUnifiedBuilding()
        {
            Transform buildingGroup = new GameObject("Building_Complex").transform;
            buildingGroup.SetParent(environmentRoot);

            float wallH = 3.5f;
            float wallT = 0.4f;

            // Seviye 1: 18m depth (Z: -3 to 15), Seviye 2: 27m depth (Z: -3 to 24), Seviye 3: 36m depth (Z: -3 to 33 -> Otopark ile TAM AYNI SINIR!)
            float frontWallZ = -3.0f;
            float storeDepth = (currentUpgradeLevel == 1) ? 18.0f : ((currentUpgradeLevel == 2) ? 27.0f : 36.0f);
            float backWallZ = frontWallZ + storeDepth;

            float storageDepth = (currentUpgradeLevel == 1) ? 9.5f : ((currentUpgradeLevel == 2) ? 14.5f : 19.5f);
            float storageBackZ = frontWallZ + storageDepth;
            float staffDepth = backWallZ - storageBackZ;

            // TABAN ZEMİNLERİ
            GameObject storeFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            storeFloor.name = "Store_Floor_FullDepth";
            storeFloor.transform.SetParent(buildingGroup);
            storeFloor.transform.position = new Vector3(-5f, 0.01f, frontWallZ + (storeDepth / 2f));
            storeFloor.transform.localScale = new Vector3(16f, 0.02f, storeDepth);
            storeFloor.GetComponent<Renderer>().sharedMaterial = storeFloorMat;

            GameObject storageFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            storageFloor.name = "Storage_Floor";
            storageFloor.transform.SetParent(buildingGroup);
            storageFloor.transform.position = new Vector3(7f, 0.01f, frontWallZ + (storageDepth / 2f));
            storageFloor.transform.localScale = new Vector3(8f, 0.02f, storageDepth);
            storageFloor.GetComponent<Renderer>().sharedMaterial = storageFloorMat;

            GameObject staffFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            staffFloor.name = "Staff_BreakRoom_Floor";
            staffFloor.transform.SetParent(buildingGroup);
            staffFloor.transform.position = new Vector3(7f, 0.01f, storageBackZ + (staffDepth / 2f));
            staffFloor.transform.localScale = new Vector3(8f, 0.02f, staffDepth);
            staffFloor.GetComponent<Renderer>().sharedMaterial = staffRoomFloorMat;

            // DUVARLAR
            CreateWall("Building_Wall_Back_Unified", buildingGroup, new Vector3(-1f, wallH / 2f, backWallZ), new Vector3(24.4f, wallH, wallT));
            CreateWall("Store_Wall_Left", buildingGroup, new Vector3(-13f, wallH / 2f, frontWallZ + (storeDepth / 2f)), new Vector3(wallT, wallH, storeDepth + wallT));

            float rightWallLength1 = 3.5f;
            CreateWall("Storage_Wall_Right_Front", buildingGroup, new Vector3(11f, wallH / 2f, -1.25f), new Vector3(wallT, wallH, rightWallLength1));
            CreateWall("Storage_Wall_Right_Over_Door", buildingGroup, new Vector3(11f, wallH - 0.4f, 2f), new Vector3(wallT, 0.8f, 3.0f));

            float rightWallLength3 = backWallZ - 3.5f;
            float rightWallCenter3 = 3.5f + (rightWallLength3 / 2f);
            CreateWall("Storage_Wall_Right_Back", buildingGroup, new Vector3(11f, wallH / 2f, rightWallCenter3), new Vector3(wallT, wallH, rightWallLength3 + wallT));

            // ZEMİNE HİÇBİR ŞEY TAŞIRMAYAN DUVAR İÇİNE GÖMÜLÜ BÖLME DUVARLAR
            float partitionLength1 = 3.0f;
            CreateWall("Partition_Store_Storage_Front", buildingGroup, new Vector3(3f, wallH / 2f, -1.5f), new Vector3(wallT, wallH, partitionLength1));
            CreateWall("Partition_Store_Storage_Top", buildingGroup, new Vector3(3f, wallH - 0.4f, 2f), new Vector3(wallT, 0.8f, 4.0f));

            float partitionLength3 = backWallZ - 4.0f;
            float partitionCenter3 = 4.0f + (partitionLength3 / 2f);
            CreateWall("Partition_Store_Storage_Back", buildingGroup, new Vector3(3f, wallH / 2f, partitionCenter3), new Vector3(wallT, wallH, partitionLength3 + wallT));

            CreateWall("Staff_Partition_Left", buildingGroup, new Vector3(4.25f, wallH / 2f, storageBackZ), new Vector3(2.5f, wallH, wallT));
            CreateWall("Staff_Partition_Right", buildingGroup, new Vector3(9.75f, wallH / 2f, storageBackZ), new Vector3(2.5f, wallH, wallT));
            CreateWall("Staff_Partition_Top", buildingGroup, new Vector3(7f, wallH - 0.4f, storageBackZ), new Vector3(3.0f, 0.8f, wallT));

            CreateWall("Front_Wall_Left_Expanded", buildingGroup, new Vector3(-9.75f, wallH / 2f, -3f), new Vector3(6.5f, wallH, wallT));
            CreateWall("Front_Wall_Over_MainDoor", buildingGroup, new Vector3(-5f, wallH - 0.4f, -3f), new Vector3(3.0f, 0.8f, wallT));
            CreateWall("Front_Wall_Center_Unified", buildingGroup, new Vector3(3.75f, wallH / 2f, -3f), new Vector3(14.5f, wallH, wallT));

            // KAPILAR (DUVAR DOKUSUYLA SIFIRLANMIŞ, ZEMİNE VEYA ODAYA KUTU/ÇİT ŞEKLİNDE ÇIKINTI YAPMAYAN ŞIK KAPILAR)
            CreateFlushDoubleDoor("Main_Entrance_DoubleDoor", buildingGroup, new Vector3(-5f, 0f, -3f), mainDoorGlassMat, true, "ANA GİRİŞ (CAM KAPISI)", "MAIN ENTRANCE (GLASS DOOR)", Color.cyan, 0f, true, 3.0f);
            CreateFlushDoubleDoor("Storage_DoubleDoor", buildingGroup, new Vector3(3f, 0f, 2f), darkWallMat, false, "DEPO GİRİŞİ", "STORAGE ENTRANCE", Color.yellow, 90f, false, 4.0f);
            CreateFlushDoubleDoor("Goods_Receipt_DoubleDoor_RightWall", buildingGroup, new Vector3(11f, 0f, 2f), goodsDoorMat, false, "MAL KABUL (YÜKLEME)", "GOODS RECEIPT (LOADING)", Color.green, 90f, false, 3.0f);
            CreateFlushDoubleDoor("StaffRoom_DoubleDoor", buildingGroup, new Vector3(7f, 0f, storageBackZ), staffDoorMat, true, "PERSONEL ODASI", "STAFF ROOM", Color.magenta, 0f, false, 3.0f);

            // SEVİYEYE BAĞLI ODA BÜYÜKLÜĞÜ İLE %100 UYUMLU DETAYLI VE MOBİLYALI PERSONEL DİNLENME ODASI
            BuildStaffRoomFurnishings(buildingGroup, currentUpgradeLevel, storageBackZ, backWallZ, staffDepth);

            // DÜKKAN ÖNÜ (OTOBÜS DURAĞI ARKASI) IŞIKLI VE OKUNAKLI ŞİRKET TABELASI (SEVİYE 1/2/3 MODELLERİ)
            GameObject signHost = new GameObject("Storefront_Signboard_Host");
            signHost.transform.SetParent(buildingGroup, false);
            signHost.transform.localPosition = Vector3.zero;
            StorefrontSignboardController signCtrl = signHost.AddComponent<StorefrontSignboardController>();
            signCtrl.RefreshSignboard();

            // TÜM ODALAR (MAĞAZA, DEPO, PERSONEL) İÇİN DÜKKAN BÜYÜTME UYUMLU VE PARLAK TAVAN AYDINLATMALARI
            BuildDynamicStoreInteriorLighting(buildingGroup, storeDepth, storageDepth, staffDepth, frontWallZ, backWallZ, storageBackZ);
        }

        private void BuildDynamicStoreInteriorLighting(Transform parent, float storeDepth, float storageDepth, float staffDepth, float frontWallZ, float backWallZ, float storageBackZ)
        {
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.ClearStoreInteriorLights();
            }

            Transform lightGroup = new GameObject("Store_Interior_Ceiling_Lights_Group").transform;
            lightGroup.SetParent(parent);

            Material fixtureFrameMat = CreateSolidMaterial("CeilingFixtureMat", new Color(0.95f, 0.98f, 1.0f, 0.12f), 0.1f, 0.9f);

            // 1. MAĞAZA REYON ALANI CIKIS - GİRİŞ CEILING LIGHTS (Store Floor: X = -11.0 to 1.0)
            for (float z = frontWallZ + 3.0f; z <= backWallZ - 2.0f; z += 5.5f)
            {
                for (float x = -11.0f; x <= 1.0f; x += 5.5f)
                {
                    Vector3 lPos = new Vector3(x, 3.25f, z);
                    CreateStoreCeilingLightFixture(lightGroup, lPos, "StoreFloor_CeilingLight", fixtureFrameMat);
                }
            }

            // 2. DEPO ODASI CEILING LIGHTS (Storage Room: X = 7.0)
            for (float z = frontWallZ + 3.0f; z <= storageBackZ - 1.5f; z += 5.0f)
            {
                Vector3 lPos = new Vector3(7.0f, 3.25f, z);
                CreateStoreCeilingLightFixture(lightGroup, lPos, "Storage_CeilingLight", fixtureFrameMat);
            }

            // 3. PERSONEL DİNLENME ODASI CEILING LIGHTS (Staff Room: X = 7.0)
            for (float z = storageBackZ + 3.0f; z <= backWallZ - 1.5f; z += 5.0f)
            {
                Vector3 lPos = new Vector3(7.0f, 3.25f, z);
                CreateStoreCeilingLightFixture(lightGroup, lPos, "StaffRoom_CeilingLight", fixtureFrameMat);
            }

            // Sahnedeki mevcut tüm Panel_Fixture objelerini temizle
#if UNITY_2023_1_OR_NEWER
            GameObject[] allSceneObjs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
            GameObject[] allSceneObjs = UnityEngine.Object.FindObjectsOfType<GameObject>();
#endif
            foreach (GameObject sceneObj in allSceneObjs)
            {
                if (sceneObj != null && sceneObj.name == "Panel_Fixture")
                {
                    if (Application.isPlaying) Destroy(sceneObj);
                    else DestroyImmediate(sceneObj);
                }
            }
        }

        private void CreateStoreCeilingLightFixture(Transform parent, Vector3 worldPos, string fixtureName, Material frameMat)
        {
            GameObject fixtureObj = new GameObject(fixtureName);
            fixtureObj.transform.SetParent(parent, false);
            fixtureObj.transform.position = worldPos;

            // Işık Kaynağı (Görsel kirlilik yaratmayan saf ışık kaynağı - PointLight)
            Light ceilingLight = fixtureObj.AddComponent<Light>();
            ceilingLight.type = LightType.Point;
            ceilingLight.color = new Color(1.0f, 0.96f, 0.88f); // Canlı Sıcak Beyaz
            ceilingLight.intensity = 3.8f;
            ceilingLight.range = 18.0f;
            ceilingLight.shadows = LightShadows.None;
            ceilingLight.enabled = false; // Gündüz kapalı, gece DayNightCycleManager ile açılır

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(ceilingLight);
            }
        }

        private void BuildStaffRoomFurnishings(Transform parent, int level, float storageBackZ, float backWallZ, float staffDepth)
        {
            Transform roomGroup = new GameObject($"StaffRoom_Furnishings_Level_{level}").transform;
            roomGroup.SetParent(parent, false);

            Material woodMat = CreateSolidMaterial("Staff_WoodMat", new Color(0.78f, 0.58f, 0.38f));
            Material darkWoodMat = CreateSolidMaterial("Staff_DarkWoodMat", new Color(0.38f, 0.24f, 0.14f));
            Material metalMat = CreateSolidMaterial("Staff_MetalMat", new Color(0.52f, 0.56f, 0.62f));
            Material darkMetalMat = CreateSolidMaterial("Staff_DarkMetalMat", new Color(0.20f, 0.22f, 0.26f));
            Material fabricBlueMat = CreateSolidMaterial("Staff_FabricBlueMat", new Color(0.20f, 0.45f, 0.72f));
            Material cushionWhiteMat = CreateSolidMaterial("Staff_CushionWhiteMat", new Color(0.90f, 0.92f, 0.95f));
            Material waterBodyMat = CreateSolidMaterial("Staff_WaterBodyMat", new Color(0.92f, 0.95f, 0.98f));
            Material waterBottleMat = CreateSolidMaterial("Staff_WaterBottleMat", new Color(0.20f, 0.68f, 0.92f, 0.50f));
            Material tvMat = CreateSolidMaterial("Staff_TVMat", new Color(0.08f, 0.08f, 0.10f));
            Material tvScreenMat = CreateSolidMaterial("Staff_TVScreenMat", new Color(0.15f, 0.22f, 0.32f));
            Material plantPotMat = CreateSolidMaterial("Staff_PlantPotMat", new Color(0.78f, 0.42f, 0.26f));
            Material leafMat = CreateSolidMaterial("Staff_LeafMat", new Color(0.18f, 0.62f, 0.25f));
            Material rugMat = CreateSolidMaterial("Staff_RugMat", new Color(0.26f, 0.38f, 0.54f));
            Material redMat = CreateSolidMaterial("Staff_RedMat", new Color(0.85f, 0.20f, 0.20f));

            float startZ = storageBackZ;
            float centerZ = startZ + (staffDepth / 2f);

            if (level == 1)
            {
                // ---------------- LEVEL 1: BASİT PERSONEL DİNLENME ODASI ----------------
                // 1. Ahşap Mola Masası & 3 Sandalye (Sol Ön)
                CreateBreakTableWithChairs(roomGroup, new Vector3(4.8f, 0f, startZ + 2.5f), 3, woodMat, darkMetalMat);

                // 2. 5'li Metal Soyunma Dolapları (Sağ Duvara Sıfır Yaslanmış, Kapakları Odaya Bakıyor)
                CreateMetalLockerCabinet(roomGroup, new Vector3(10.65f, 0f, centerZ), 5, metalMat, darkMetalMat, 90f);

                // 3. Duvara Yaslanmış 3 Kişilik Konforlu Koltuk (Gövdesi Arka Duvara Tam Yaslı, Ön Yüzü Kapıya Bakıyor)
                CreateThreeSeaterSofa(roomGroup, new Vector3(7.0f, 0f, backWallZ - 0.60f), 0f, fabricBlueMat, cushionWhiteMat);

                // 4. Su Sebili (Sol Arka Köşe)
                CreateWaterDispenser(roomGroup, new Vector3(3.5f, 0f, backWallZ - 1.0f), waterBodyMat, waterBottleMat);

                // 5. Çöp Kovası & Duyuru Panosu & Saati
                CreateTrashBin(roomGroup, new Vector3(9.8f, 0f, startZ + 1.2f), darkMetalMat);
                CreateNoticeBoard(roomGroup, new Vector3(3.15f, 1.8f, startZ + 2.5f), woodMat);
                CreateWallClock(roomGroup, new Vector3(7.0f, 2.3f, backWallZ - 0.2f), metalMat);
            }
            else if (level == 2)
            {
                // ---------------- LEVEL 2: GELİŞMİŞ PERSONEL DİNLENME ODASI ----------------
                // 1. Karşılıklı Koltuk Takımı & Ortadaki Masa (Sol Arka Lounge Alanı - Personel Geçiş Yolu İçin Genişletilmiş Mesafe)
                // Halı (Z-Fighting önleyici Y: 0.025f yüksekliği ile zeminin tam üstünde oturur)
                GameObject rug = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rug.transform.SetParent(roomGroup, false);
                rug.transform.position = new Vector3(5.0f, 0.025f, backWallZ - 2.8f);
                rug.transform.localScale = new Vector3(2.8f, 0.008f, 5.2f);
                rug.GetComponent<Renderer>().sharedMaterial = rugMat;
                Destroy(rug.GetComponent<Collider>());

                // 3'lü Koltuk (Arka duvara tam yaslı, ön yüzü kapıya bakıyor)
                CreateThreeSeaterSofa(roomGroup, new Vector3(5.0f, 0f, backWallZ - 0.60f), 0f, fabricBlueMat, cushionWhiteMat);

                // Ortadaki Sehpa / Masa (Koltuk Önünde Personel Rahatça Yürüyebilecek Şekilde 2.2 Metre İleriye Kaydırıldı)
                CreateCoffeeTable(roomGroup, new Vector3(5.0f, 0f, backWallZ - 2.8f), woodMat);

                // 2'li Koltuk (Sehpanın Tam Karşısında Eşit Mesafede Geride, Yüzü Masaya/3'lü Koltuğa Bakıyor)
                CreateTwoSeaterSofa(roomGroup, new Vector3(5.0f, 0f, backWallZ - 5.0f), 180f, fabricBlueMat, cushionWhiteMat);

                // 2. Genişletilmiş Mola Masası & 4 Sandalye (Sol Ön)
                CreateBreakTableWithChairs(roomGroup, new Vector3(4.8f, 0f, startZ + 2.5f), 4, woodMat, darkMetalMat);

                // 3. 8'li Metal Soyunma Dolapları (Sağ Duvara Sıfır Yaslanmış, Kapaklar Odaya Bakıyor)
                CreateMetalLockerCabinet(roomGroup, new Vector3(10.65f, 0f, backWallZ - 2.8f), 8, metalMat, darkMetalMat, 90f);

                // 4. Mutfak Tezgahı & Mini Buzdolabı & Mikrodalga (Sağ Ön Duvar)
                CreateKitchenetteCounter(roomGroup, new Vector3(10.2f, 0f, startZ + 3.0f), woodMat, metalMat, darkMetalMat);

                // 5. Atıştırmalık / İçecek Otomatı (Sağ Orta Duvar)
                CreateVendingMachine(roomGroup, new Vector3(10.2f, 0f, startZ + 5.5f), redMat, metalMat);

                // 6. Duvar TV & Su Sebili & Saksı Bitkisi & Duyuru Panosu
                CreateWallTV(roomGroup, new Vector3(3.15f, 1.9f, backWallZ - 2.8f), tvMat, tvScreenMat);
                CreateWaterDispenser(roomGroup, new Vector3(3.5f, 0f, startZ + 1.2f), waterBodyMat, waterBottleMat);
                CreatePottedPlant(roomGroup, new Vector3(3.4f, 0f, backWallZ - 6.2f), plantPotMat, leafMat);
                CreateNoticeBoard(roomGroup, new Vector3(3.15f, 1.8f, startZ + 2.8f), woodMat);
                CreateWallClock(roomGroup, new Vector3(7.0f, 2.3f, backWallZ - 0.2f), metalMat);
            }
            else
            {
                // ---------------- LEVEL 3: LÜKS VIP PERSONEL LOUNGE & EĞLENCE ODASI ----------------
                // Zümrüt Yeşili & Fildişi VIP Kumaş ve Deri Materyalleri
                Material vipEmeraldMat = CreateSolidMaterial("Staff_VipEmeraldMat", new Color(0.12f, 0.38f, 0.30f));
                Material vipIvoryCushionMat = CreateSolidMaterial("Staff_VipIvoryCushionMat", new Color(0.94f, 0.94f, 0.90f));

                // 1. VIP 3+2+1 Koltuk Takımı & Cam Sehpa & Halı (Sol Arka VIP Lounge)
                // Halı (Z-Fighting önleyici Y: 0.025f yüksekliği ile lüks alanı tam kaplar)
                GameObject rug = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rug.transform.SetParent(roomGroup, false);
                rug.transform.position = new Vector3(5.0f, 0.025f, backWallZ - 2.8f);
                rug.transform.localScale = new Vector3(3.2f, 0.008f, 5.8f);
                rug.GetComponent<Renderer>().sharedMaterial = rugMat;
                Destroy(rug.GetComponent<Collider>());

                // 3'lü VIP Koltuk (Arka duvara yaslı, kapıya bakıyor)
                CreateThreeSeaterSofa(roomGroup, new Vector3(5.0f, 0f, backWallZ - 0.60f), 0f, vipEmeraldMat, vipIvoryCushionMat);

                // Ortadaki Ahşap / Cam Lüks Sehpa (Personellerin yürümesi için 2.2m koridor mesafesi)
                CreateCoffeeTable(roomGroup, new Vector3(5.0f, 0f, backWallZ - 2.8f), darkWoodMat);

                // 2'li VIP Koltuk (Sehpanın tam karşısında, 3'lü koltuğa bakıyor)
                CreateTwoSeaterSofa(roomGroup, new Vector3(5.0f, 0f, backWallZ - 5.0f), 180f, vipEmeraldMat, vipIvoryCushionMat);

                // Ekstra Tekli VIP Koltuk (Sehpanın solunda, masaya ve lounge alanına bakıyor)
                CreateSingleArmchair(roomGroup, new Vector3(3.3f, 0f, backWallZ - 2.8f), 90f, vipEmeraldMat, vipIvoryCushionMat);

                // 2. VIP 6 Kişilik Yemek Masası (Sol Ön)
                CreateVIPDiningTable(roomGroup, new Vector3(4.8f, 0f, startZ + 3.8f), darkWoodMat, metalMat);

                // 3. Full Mutfak Adası & Kahve Barı (Sağ Ön Duvar)
                CreateFullKitchenIsland(roomGroup, new Vector3(10.2f, 0f, startZ + 3.5f), darkWoodMat, metalMat);

                // 4. Atari / Arcade Kabini & Kırmızı Otomat Yanında Su Sebili (Sağ Duvar)
                CreateArcadeCabinet(roomGroup, new Vector3(10.2f, 0f, startZ + 7.8f), redMat, tvScreenMat);
                CreateWaterDispenser(roomGroup, new Vector3(10.2f, 0f, startZ + 6.4f), waterBodyMat, waterBottleMat);

                // 5. Lüks 10'lu Ahşap Desenli Soyunma Dolapları (Sağ Arka Duvar - Önü Tamamen Açık)
                CreateMetalLockerCabinet(roomGroup, new Vector3(10.65f, 0f, backWallZ - 3.5f), 10, darkWoodMat, metalMat, 90f);

                // 6. Dev 65-inch Duvar TV & Oyun Konsolu Ünitesi (Sol Duvar)
                CreateGiantWallTV(roomGroup, new Vector3(3.15f, 2.0f, backWallZ - 2.8f), tvMat, tvScreenMat, darkWoodMat);

                // 7. Ferah Köşe Lambaderi (Abajur) & Giriş Köşesi Saksı Bitkileri & Tablo
                CreateFloorLamp(roomGroup, new Vector3(3.3f, 0f, backWallZ - 0.60f), metalMat, cushionWhiteMat);
                CreatePottedPlant(roomGroup, new Vector3(3.4f, 0f, startZ + 1.2f), plantPotMat, leafMat);
                CreatePottedPlant(roomGroup, new Vector3(10.2f, 0f, startZ + 1.2f), plantPotMat, leafMat);
                CreateNoticeBoard(roomGroup, new Vector3(3.15f, 1.8f, startZ + 3.8f), darkWoodMat);
                CreateWallClock(roomGroup, new Vector3(7.0f, 2.3f, backWallZ - 0.2f), metalMat);
            }
        }

        private void AddPhysicalObstacleAndCollider(GameObject obj, Vector3 center, Vector3 size)
        {
            if (obj == null) return;

            // 1. Karakterlerin takılmadan rahatça koltuğa/sandalyeye oturabilmesi için Trigger BoxCollider
            BoxCollider col = obj.AddComponent<BoxCollider>();
            col.center = center;
            col.size = size;
            col.isTrigger = true;

            // 2. NavMesh Dinamik Yürüyüş Engeli (Oturmayı ve serbest geçişi engellemez)
            NavMeshObstacle obstacle = obj.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = center;
            obstacle.size = size;
            obstacle.carving = false;
        }

        private void CreateBreakTableWithChairs(Transform parent, Vector3 centerPos, int chairCount, Material tableMat, Material chairMat)
        {
            GameObject group = new GameObject("BreakTable_Set");
            group.transform.SetParent(parent, false);
            group.transform.position = centerPos;

            // Masa Üst Tablası
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(group.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            top.transform.localScale = new Vector3(1.6f, 0.08f, 1.0f);
            top.GetComponent<Renderer>().sharedMaterial = tableMat;
            Destroy(top.GetComponent<Collider>());

            // Masa Ayakları
            Vector3[] legOffsets = new Vector3[] {
                new Vector3(-0.7f, 0.37f, -0.4f), new Vector3(0.7f, 0.37f, -0.4f),
                new Vector3(-0.7f, 0.37f, 0.4f),  new Vector3(0.7f, 0.37f, 0.4f)
            };
            foreach (Vector3 legPos in legOffsets)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.transform.SetParent(group.transform, false);
                leg.transform.localPosition = legPos;
                leg.transform.localScale = new Vector3(0.08f, 0.74f, 0.08f);
                leg.GetComponent<Renderer>().sharedMaterial = chairMat;
                Destroy(leg.GetComponent<Collider>());
            }

            // Sandalyeler
            Vector3[] chairPositions = new Vector3[] {
                new Vector3(-1.0f, 0f, 0f),
                new Vector3(1.0f, 0f, 0f),
                new Vector3(0f, 0f, -0.75f),
                new Vector3(0f, 0f, 0.75f)
            };
            for (int i = 0; i < Mathf.Min(chairCount, 4); i++)
            {
                GameObject chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chair.name = "Chair_" + i;
                chair.transform.SetParent(group.transform, false);
                chair.transform.localPosition = chairPositions[i] + new Vector3(0f, 0.22f, 0f);
                chair.transform.localScale = new Vector3(0.42f, 0.44f, 0.42f);
                chair.GetComponent<Renderer>().sharedMaterial = tableMat;
                Destroy(chair.GetComponent<Collider>());

                // Sandalye Sırtlığı
                GameObject backrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backrest.transform.SetParent(chair.transform, false);
                backrest.transform.localPosition = new Vector3(0f, 0.55f, -0.4f);
                backrest.transform.localScale = new Vector3(0.9f, 0.9f, 0.15f);
                backrest.GetComponent<Renderer>().sharedMaterial = tableMat;
                Destroy(backrest.GetComponent<Collider>());
            }

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.9f, 1.6f));
        }

        private void CreateThreeSeaterSofa(Transform parent, Vector3 pos, float rotationY, Material sofaMat, Material cushionMat)
        {
            GameObject group = new GameObject("Staff_3Seater_Sofa");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;
            group.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            // Koltuk Gövdesi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(group.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            body.transform.localScale = new Vector3(2.4f, 0.50f, 0.85f);
            body.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(body.GetComponent<Collider>());

            // Duvara Dayanmış Koltuk Arkalığı
            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.transform.SetParent(group.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.70f, 0.35f);
            back.transform.localScale = new Vector3(2.4f, 0.65f, 0.22f);
            back.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(back.GetComponent<Collider>());

            // Yan Kolçaklar
            GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armL.transform.SetParent(group.transform, false);
            armL.transform.localPosition = new Vector3(-1.25f, 0.55f, 0.05f);
            armL.transform.localScale = new Vector3(0.22f, 0.50f, 0.95f);
            armL.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(armL.GetComponent<Collider>());

            GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armR.transform.SetParent(group.transform, false);
            armR.transform.localPosition = new Vector3(1.25f, 0.55f, 0.05f);
            armR.transform.localScale = new Vector3(0.22f, 0.50f, 0.95f);
            armR.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(armR.GetComponent<Collider>());

            // 3 Adet Oturma Minderi
            float startX = -0.70f;
            for (int i = 0; i < 3; i++)
            {
                GameObject cushion = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cushion.transform.SetParent(group.transform, false);
                cushion.transform.localPosition = new Vector3(startX + (i * 0.70f), 0.42f, -0.05f);
                cushion.transform.localScale = new Vector3(0.66f, 0.16f, 0.65f);
                cushion.GetComponent<Renderer>().sharedMaterial = cushionMat;
                Destroy(cushion.GetComponent<Collider>());
            }

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.50f, 0.10f), new Vector3(2.5f, 1.0f, 1.0f));
        }

        private void CreateTwoSeaterSofa(Transform parent, Vector3 pos, float rotationY, Material sofaMat, Material cushionMat)
        {
            GameObject group = new GameObject("Staff_2Seater_Sofa");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;
            group.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            // Koltuk Gövdesi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(group.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            body.transform.localScale = new Vector3(1.8f, 0.50f, 0.85f);
            body.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(body.GetComponent<Collider>());

            // Koltuk Arkalığı
            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.transform.SetParent(group.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.70f, 0.35f);
            back.transform.localScale = new Vector3(1.8f, 0.65f, 0.22f);
            back.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(back.GetComponent<Collider>());

            // Yan Kolçaklar
            GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armL.transform.SetParent(group.transform, false);
            armL.transform.localPosition = new Vector3(-0.95f, 0.55f, 0.05f);
            armL.transform.localScale = new Vector3(0.22f, 0.50f, 0.95f);
            armL.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(armL.GetComponent<Collider>());

            GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armR.transform.SetParent(group.transform, false);
            armR.transform.localPosition = new Vector3(0.95f, 0.55f, 0.05f);
            armR.transform.localScale = new Vector3(0.22f, 0.50f, 0.95f);
            armR.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(armR.GetComponent<Collider>());

            // 2 Adet Oturma Minderi
            float startX = -0.42f;
            for (int i = 0; i < 2; i++)
            {
                GameObject cushion = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cushion.transform.SetParent(group.transform, false);
                cushion.transform.localPosition = new Vector3(startX + (i * 0.84f), 0.42f, -0.05f);
                cushion.transform.localScale = new Vector3(0.78f, 0.16f, 0.65f);
                cushion.GetComponent<Renderer>().sharedMaterial = cushionMat;
                Destroy(cushion.GetComponent<Collider>());
            }

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.50f, 0.10f), new Vector3(1.9f, 1.0f, 1.0f));
        }

        private void CreateSingleArmchair(Transform parent, Vector3 pos, float rotationY, Material sofaMat, Material cushionMat)
        {
            GameObject group = new GameObject("Staff_Single_Armchair");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;
            group.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            // Koltuk Gövdesi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(group.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            body.transform.localScale = new Vector3(0.95f, 0.50f, 0.85f);
            body.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(body.GetComponent<Collider>());

            // Koltuk Arkalığı
            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.transform.SetParent(group.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.70f, 0.35f);
            back.transform.localScale = new Vector3(0.95f, 0.65f, 0.22f);
            back.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(back.GetComponent<Collider>());

            // Yan Kolçaklar
            GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armL.transform.SetParent(group.transform, false);
            armL.transform.localPosition = new Vector3(-0.52f, 0.55f, 0.05f);
            armL.transform.localScale = new Vector3(0.18f, 0.50f, 0.95f);
            armL.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(armL.GetComponent<Collider>());

            GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armR.transform.SetParent(group.transform, false);
            armR.transform.localPosition = new Vector3(0.52f, 0.55f, 0.05f);
            armR.transform.localScale = new Vector3(0.18f, 0.50f, 0.95f);
            armR.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(armR.GetComponent<Collider>());

            // Oturma Minderi
            GameObject cushion = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cushion.transform.SetParent(group.transform, false);
            cushion.transform.localPosition = new Vector3(0f, 0.42f, -0.05f);
            cushion.transform.localScale = new Vector3(0.78f, 0.16f, 0.65f);
            cushion.GetComponent<Renderer>().sharedMaterial = cushionMat;
            Destroy(cushion.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.50f, 0.10f), new Vector3(1.1f, 1.0f, 1.0f));
        }

        private void CreateCoffeeTable(Transform parent, Vector3 pos, Material tableMat)
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Staff_Coffee_Table";
            table.transform.SetParent(parent, false);
            table.transform.position = pos + new Vector3(0f, 0.22f, 0f);
            table.transform.localScale = new Vector3(1.4f, 0.44f, 0.75f);
            table.GetComponent<Renderer>().sharedMaterial = tableMat;
            Destroy(table.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(table, Vector3.zero, new Vector3(1.5f, 0.50f, 0.8f));
        }

        private void CreateMetalLockerCabinet(Transform parent, Vector3 centerPos, int doorCount, Material cabinetMat, Material detailMat, float rotationY = 0f)
        {
            GameObject group = new GameObject("Staff_Metal_Lockers");
            group.transform.SetParent(parent, false);
            group.transform.position = centerPos;
            group.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            float width = 0.55f * doorCount;
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(group.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localScale = new Vector3(width, 1.9f, 0.55f);
            body.GetComponent<Renderer>().sharedMaterial = cabinetMat;
            Destroy(body.GetComponent<Collider>());

            // Dolap Kapak Çizgileri & Kulpları
            float startX = -width / 2f + 0.275f;
            for (int i = 0; i < doorCount; i++)
            {
                float posX = startX + (i * 0.55f);

                GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.transform.SetParent(group.transform, false);
                handle.transform.localPosition = new Vector3(posX - 0.15f, 1.0f, -0.29f);
                handle.transform.localScale = new Vector3(0.04f, 0.18f, 0.04f);
                handle.GetComponent<Renderer>().sharedMaterial = detailMat;
                Destroy(handle.GetComponent<Collider>());

                // Havalandırma Izgarası (Vent Plate)
                GameObject vent = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vent.transform.SetParent(group.transform, false);
                vent.transform.localPosition = new Vector3(posX, 1.65f, -0.285f);
                vent.transform.localScale = new Vector3(0.35f, 0.12f, 0.02f);
                vent.GetComponent<Renderer>().sharedMaterial = detailMat;
                Destroy(vent.GetComponent<Collider>());
            }

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.95f, 0f), new Vector3(width, 1.9f, 0.6f));
        }

        private void CreateWaterDispenser(Transform parent, Vector3 pos, Material bodyMat, Material bottleMat)
        {
            GameObject group = new GameObject("Staff_Water_Dispenser");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Ana Gövde
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(group.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.50f, 0f);
            body.transform.localScale = new Vector3(0.42f, 1.0f, 0.42f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Destroy(body.GetComponent<Collider>());

            // Su Şişesi (Damacana Tank)
            GameObject bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottle.transform.SetParent(group.transform, false);
            bottle.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            bottle.transform.localScale = new Vector3(0.36f, 0.25f, 0.36f);
            Renderer bRend = bottle.GetComponent<Renderer>();
            bRend.sharedMaterial = bottleMat;
            bRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(bottle.GetComponent<Collider>());

            // Sıcak / Soğuk Musluklar
            GameObject tap1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tap1.transform.SetParent(group.transform, false);
            tap1.transform.localPosition = new Vector3(-0.08f, 0.72f, -0.22f);
            tap1.transform.localScale = new Vector3(0.04f, 0.06f, 0.06f);
            tap1.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("BlueTapMat", new Color(0.20f, 0.60f, 0.95f));
            Destroy(tap1.GetComponent<Collider>());

            GameObject tap2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tap2.transform.SetParent(group.transform, false);
            tap2.transform.localPosition = new Vector3(0.08f, 0.72f, -0.22f);
            tap2.transform.localScale = new Vector3(0.04f, 0.06f, 0.06f);
            tap2.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("RedTapMat", new Color(0.90f, 0.20f, 0.20f));
            Destroy(tap2.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.75f, 0f), new Vector3(0.5f, 1.5f, 0.5f));
        }

        private void CreateSofaSet(Transform parent, Vector3 pos, Material sofaMat, Material cushionMat, Material tableMat, Material rugMat)
        {
            GameObject group = new GameObject("Staff_Sofa_Set");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Halı
            GameObject rug = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rug.transform.SetParent(group.transform, false);
            rug.transform.localPosition = new Vector3(0f, 0.015f, 0.5f);
            rug.transform.localScale = new Vector3(2.2f, 0.01f, 2.4f);
            rug.GetComponent<Renderer>().sharedMaterial = rugMat;
            Destroy(rug.GetComponent<Collider>());

            // Koltuk Gövdesi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(group.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            body.transform.localScale = new Vector3(1.8f, 0.50f, 0.85f);
            body.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(body.GetComponent<Collider>());

            // Koltuk Arkalığı
            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.transform.SetParent(group.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.70f, 0.35f);
            back.transform.localScale = new Vector3(1.8f, 0.65f, 0.22f);
            back.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(back.GetComponent<Collider>());

            // Yan Kolçaklar
            GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armL.transform.SetParent(group.transform, false);
            armL.transform.localPosition = new Vector3(-0.95f, 0.55f, 0.05f);
            armL.transform.localScale = new Vector3(0.22f, 0.50f, 0.95f);
            armL.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(armL.GetComponent<Collider>());

            GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armR.transform.SetParent(group.transform, false);
            armR.transform.localPosition = new Vector3(0.95f, 0.55f, 0.05f);
            armR.transform.localScale = new Vector3(0.22f, 0.50f, 0.95f);
            armR.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(armR.GetComponent<Collider>());

            // Oturma Minderleri
            GameObject cushion = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cushion.transform.SetParent(group.transform, false);
            cushion.transform.localPosition = new Vector3(0f, 0.42f, -0.05f);
            cushion.transform.localScale = new Vector3(1.6f, 0.16f, 0.65f);
            cushion.GetComponent<Renderer>().sharedMaterial = cushionMat;
            Destroy(cushion.GetComponent<Collider>());

            // Sehpa (Coffee Table)
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.transform.SetParent(group.transform, false);
            table.transform.localPosition = new Vector3(0f, 0.25f, 1.1f);
            table.transform.localScale = new Vector3(1.2f, 0.45f, 0.65f);
            table.GetComponent<Renderer>().sharedMaterial = tableMat;
            Destroy(table.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.50f, 0.5f), new Vector3(2.0f, 1.0f, 2.2f));
        }

        private void CreateLShapedSofaSet(Transform parent, Vector3 pos, Material sofaMat, Material cushionMat, Material tableMat, Material rugMat)
        {
            GameObject group = new GameObject("Staff_LShaped_VIP_Sofa");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Halı
            GameObject rug = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rug.transform.SetParent(group.transform, false);
            rug.transform.localPosition = new Vector3(0.3f, 0.015f, 0.6f);
            rug.transform.localScale = new Vector3(2.8f, 0.01f, 2.8f);
            rug.GetComponent<Renderer>().sharedMaterial = rugMat;
            Destroy(rug.GetComponent<Collider>());

            // Ana Koltuk Bloku
            GameObject mainBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainBody.transform.SetParent(group.transform, false);
            mainBody.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            mainBody.transform.localScale = new Vector3(2.2f, 0.50f, 0.85f);
            mainBody.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(mainBody.GetComponent<Collider>());

            // L-Uzantı Bloku
            GameObject lExtension = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lExtension.transform.SetParent(group.transform, false);
            lExtension.transform.localPosition = new Vector3(0.85f, 0.35f, 0.85f);
            lExtension.transform.localScale = new Vector3(0.85f, 0.50f, 1.2f);
            lExtension.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(lExtension.GetComponent<Collider>());

            // Arka Minderler
            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.transform.SetParent(group.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.70f, 0.35f);
            back.transform.localScale = new Vector3(2.2f, 0.65f, 0.22f);
            back.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Destroy(back.GetComponent<Collider>());

            // Cam Sehpa
            Material glassMat = CreateSolidMaterial("GlassTableMat", new Color(0.70f, 0.90f, 1.0f, 0.40f));
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.transform.SetParent(group.transform, false);
            table.transform.localPosition = new Vector3(-0.3f, 0.25f, 1.0f);
            table.transform.localScale = new Vector3(1.3f, 0.42f, 0.75f);
            table.GetComponent<Renderer>().sharedMaterial = glassMat;
            Destroy(table.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0.3f, 0.50f, 0.5f), new Vector3(2.6f, 1.0f, 2.6f));
        }

        private void CreateKitchenetteCounter(Transform parent, Vector3 pos, Material woodMat, Material metalMat, Material darkMat)
        {
            GameObject group = new GameObject("Staff_Kitchenette");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Tezgah Gövdesi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(group.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            body.transform.localScale = new Vector3(0.75f, 0.90f, 2.2f);
            body.GetComponent<Renderer>().sharedMaterial = woodMat;
            Destroy(body.GetComponent<Collider>());

            // Evye / Lavabo (Sink)
            GameObject sink = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sink.transform.SetParent(group.transform, false);
            sink.transform.localPosition = new Vector3(0f, 0.91f, 0.60f);
            sink.transform.localScale = new Vector3(0.55f, 0.04f, 0.65f);
            sink.GetComponent<Renderer>().sharedMaterial = metalMat;
            Destroy(sink.GetComponent<Collider>());

            // Mikrodalga Fırın (Microwave)
            GameObject microwave = GameObject.CreatePrimitive(PrimitiveType.Cube);
            microwave.transform.SetParent(group.transform, false);
            microwave.transform.localPosition = new Vector3(0f, 1.10f, -0.60f);
            microwave.transform.localScale = new Vector3(0.48f, 0.32f, 0.55f);
            microwave.GetComponent<Renderer>().sharedMaterial = darkMat;
            Destroy(microwave.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.60f, 0f), new Vector3(0.8f, 1.2f, 2.3f));
        }

        private void CreateFullKitchenIsland(Transform parent, Vector3 pos, Material woodMat, Material metalMat)
        {
            GameObject group = new GameObject("Staff_Full_Kitchen_Island");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Mutfak Adası Gövdesi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(group.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            body.transform.localScale = new Vector3(0.85f, 0.95f, 3.2f);
            body.GetComponent<Renderer>().sharedMaterial = woodMat;
            Destroy(body.GetComponent<Collider>());

            // Çiftli Evye
            GameObject sink = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sink.transform.SetParent(group.transform, false);
            sink.transform.localPosition = new Vector3(0f, 0.96f, 0.90f);
            sink.transform.localScale = new Vector3(0.60f, 0.04f, 0.85f);
            sink.GetComponent<Renderer>().sharedMaterial = metalMat;
            Destroy(sink.GetComponent<Collider>());

            // Kahve Makinesi & Espresso Barları
            GameObject espresso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            espresso.transform.SetParent(group.transform, false);
            espresso.transform.localPosition = new Vector3(0f, 1.18f, -0.90f);
            espresso.transform.localScale = new Vector3(0.45f, 0.42f, 0.45f);
            espresso.GetComponent<Renderer>().sharedMaterial = metalMat;
            Destroy(espresso.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.60f, 0f), new Vector3(0.9f, 1.2f, 3.3f));
        }

        private void CreateVendingMachine(Transform parent, Vector3 pos, Material bodyMat, Material detailMat)
        {
            GameObject group = new GameObject("Staff_Vending_Machine");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Otomat Gövdesi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(group.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localScale = new Vector3(0.75f, 1.9f, 0.95f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Destroy(body.GetComponent<Collider>());

            // Ön Cam Vitrin Paneli
            Material glassMat = CreateSolidMaterial("VendingGlassMat", new Color(0.35f, 0.80f, 0.95f, 0.45f));
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.transform.SetParent(group.transform, false);
            glass.transform.localPosition = new Vector3(-0.38f, 1.15f, 0f);
            glass.transform.localScale = new Vector3(0.04f, 1.1f, 0.80f);
            Renderer gRend = glass.GetComponent<Renderer>();
            gRend.sharedMaterial = glassMat;
            gRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(glass.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 1.0f, 0f), new Vector3(0.8f, 2.0f, 1.0f));
        }

        private void CreateArcadeCabinet(Transform parent, Vector3 pos, Material bodyMat, Material screenMat)
        {
            GameObject group = new GameObject("Staff_Arcade_Cabinet");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Kabin Ana Gövdesi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(group.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.90f, 0f);
            body.transform.localScale = new Vector3(0.75f, 1.8f, 0.75f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Destroy(body.GetComponent<Collider>());

            // Parlak Oyun Ekranı
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.transform.SetParent(group.transform, false);
            screen.transform.localPosition = new Vector3(-0.38f, 1.25f, 0f);
            screen.transform.localScale = new Vector3(0.04f, 0.55f, 0.65f);
            screen.GetComponent<Renderer>().sharedMaterial = screenMat;
            Destroy(screen.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.95f, 0f), new Vector3(0.8f, 1.9f, 0.8f));
        }

        private void CreateWallTV(Transform parent, Vector3 pos, Material frameMat, Material screenMat)
        {
            GameObject group = new GameObject("Staff_Wall_TV");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Ekran Çerçevesi
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.transform.SetParent(group.transform, false);
            screen.transform.localPosition = Vector3.zero;
            screen.transform.localScale = new Vector3(0.04f, 0.75f, 1.3f);
            screen.GetComponent<Renderer>().sharedMaterial = screenMat;
            Destroy(screen.GetComponent<Collider>());
        }

        private void CreateGiantWallTV(Transform parent, Vector3 pos, Material frameMat, Material screenMat, Material consoleMat)
        {
            GameObject group = new GameObject("Staff_Giant_VIP_TV");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // 65-inch Dev Ekran
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.transform.SetParent(group.transform, false);
            screen.transform.localPosition = Vector3.zero;
            screen.transform.localScale = new Vector3(0.04f, 1.05f, 1.8f);
            screen.GetComponent<Renderer>().sharedMaterial = screenMat;
            Destroy(screen.GetComponent<Collider>());

            // Oyun Konsolu Konsol Masası
            GameObject console = GameObject.CreatePrimitive(PrimitiveType.Cube);
            console.transform.SetParent(group.transform, false);
            console.transform.localPosition = new Vector3(0.25f, -0.90f, 0f);
            console.transform.localScale = new Vector3(0.45f, 0.40f, 1.6f);
            console.GetComponent<Renderer>().sharedMaterial = consoleMat;
            Destroy(console.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma (Konsol Masası İçin)
            AddPhysicalObstacleAndCollider(console, Vector3.zero, new Vector3(0.45f, 0.40f, 1.6f));
        }

        private void CreateVIPDiningTable(Transform parent, Vector3 centerPos, Material tableMat, Material chairMat)
        {
            GameObject group = new GameObject("Staff_VIP_Dining_Table");
            group.transform.SetParent(parent, false);
            group.transform.position = centerPos;

            // Masa Üst Tablası
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(group.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.76f, 0f);
            top.transform.localScale = new Vector3(1.8f, 0.08f, 1.2f);
            top.GetComponent<Renderer>().sharedMaterial = tableMat;
            Destroy(top.GetComponent<Collider>());

            // 6 Sandalye
            Vector3[] chairPositions = new Vector3[] {
                new Vector3(-1.1f, 0f, -0.35f), new Vector3(-1.1f, 0f, 0.35f),
                new Vector3(1.1f, 0f, -0.35f),  new Vector3(1.1f, 0f, 0.35f),
                new Vector3(0f, 0f, -0.85f),    new Vector3(0f, 0f, 0.85f)
            };
            for (int i = 0; i < 6; i++)
            {
                GameObject chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chair.transform.SetParent(group.transform, false);
                chair.transform.localPosition = chairPositions[i] + new Vector3(0f, 0.22f, 0f);
                chair.transform.localScale = new Vector3(0.42f, 0.44f, 0.42f);
                chair.GetComponent<Renderer>().sharedMaterial = tableMat;
                Destroy(chair.GetComponent<Collider>());
            }

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.45f, 0f), new Vector3(2.4f, 0.90f, 1.8f));
        }

        private void CreateNoticeBoard(Transform parent, Vector3 pos, Material frameMat)
        {
            GameObject group = new GameObject("Staff_Notice_Board");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.SetParent(group.transform, false);
            board.transform.localPosition = Vector3.zero;
            board.transform.localScale = new Vector3(0.04f, 0.85f, 1.3f);
            board.GetComponent<Renderer>().sharedMaterial = frameMat;
            Destroy(board.GetComponent<Collider>());
        }

        private void CreateWallClock(Transform parent, Vector3 pos, Material frameMat)
        {
            GameObject group = new GameObject("Staff_Wall_Clock");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            GameObject clock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            clock.transform.SetParent(group.transform, false);
            clock.transform.localPosition = Vector3.zero;
            clock.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            clock.transform.localScale = new Vector3(0.42f, 0.04f, 0.42f);
            clock.GetComponent<Renderer>().sharedMaterial = frameMat;
            Destroy(clock.GetComponent<Collider>());
        }

        private void CreateTrashBin(Transform parent, Vector3 pos, Material binMat)
        {
            GameObject bin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bin.name = "Staff_Trash_Bin";
            bin.transform.SetParent(parent, false);
            bin.transform.position = pos + new Vector3(0f, 0.25f, 0f);
            bin.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
            bin.GetComponent<Renderer>().sharedMaterial = binMat;
            Destroy(bin.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(bin, Vector3.zero, new Vector3(0.4f, 0.5f, 0.4f));
        }

        private void CreatePottedPlant(Transform parent, Vector3 pos, Material potMat, Material leafMat)
        {
            GameObject group = new GameObject("Staff_Potted_Plant");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Saksı
            GameObject pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.transform.SetParent(group.transform, false);
            pot.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            pot.transform.localScale = new Vector3(0.45f, 0.25f, 0.45f);
            pot.GetComponent<Renderer>().sharedMaterial = potMat;
            Destroy(pot.GetComponent<Collider>());

            // Yeşil Yapraklar
            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.transform.SetParent(group.transform, false);
            foliage.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            foliage.transform.localScale = new Vector3(0.70f, 0.75f, 0.70f);
            foliage.GetComponent<Renderer>().sharedMaterial = leafMat;
            Destroy(foliage.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.60f, 0f), new Vector3(0.7f, 1.2f, 0.7f));
        }

        private void CreateFloorLamp(Transform parent, Vector3 pos, Material metalMat, Material shadeMat)
        {
            GameObject group = new GameObject("Staff_Floor_Lamp");
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Direk
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(group.transform, false);
            pole.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            pole.transform.localScale = new Vector3(0.06f, 0.85f, 0.06f);
            pole.GetComponent<Renderer>().sharedMaterial = metalMat;
            Destroy(pole.GetComponent<Collider>());

            // Şapka / Abajur
            GameObject shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shade.transform.SetParent(group.transform, false);
            shade.transform.localPosition = new Vector3(0f, 1.70f, 0f);
            shade.transform.localScale = new Vector3(0.48f, 0.22f, 0.48f);
            shade.GetComponent<Renderer>().sharedMaterial = shadeMat;
            Destroy(shade.GetComponent<Collider>());

            // Fiziksel Çarpışma ve NavMesh Engel Oyma
            AddPhysicalObstacleAndCollider(group, new Vector3(0f, 0.90f, 0f), new Vector3(0.5f, 1.8f, 0.5f));
        }

        private void CreateFlushDoubleDoor(string doorName, Transform parent, Vector3 pos, Material leafMat, bool slideAlongX, string labelTextTr, string labelTextEn, Color labelColor, float labelRotation = 0f, bool isGlassDoor = false, float doorwayWidth = 3.0f)
        {
            GameObject doorRoot = new GameObject(doorName);
            doorRoot.transform.SetParent(parent);
            doorRoot.transform.position = pos;

            float wallThickness = 0.38f;
            float leafW = slideAlongX ? (doorwayWidth / 2f) : wallThickness;
            float leafD = slideAlongX ? wallThickness : (doorwayWidth / 2f);
            float openDistance = (doorwayWidth / 2f);

            GameObject leftLeaf = new GameObject("Left_Leaf_Pocket");
            leftLeaf.transform.SetParent(doorRoot.transform);
            leftLeaf.transform.localPosition = slideAlongX ? new Vector3(-leafW / 2f, 1.25f, 0f) : new Vector3(0f, 1.25f, -leafD / 2f);

            GameObject leftBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftBody.name = "Panel_Body";
            leftBody.transform.SetParent(leftLeaf.transform);
            leftBody.transform.localPosition = Vector3.zero;
            leftBody.transform.localScale = new Vector3(leafW, 2.5f, leafD);
            leftBody.GetComponent<Renderer>().sharedMaterial = leafMat;
            DestroyImmediate(leftBody.GetComponent<Collider>());

            GameObject rightLeaf = new GameObject("Right_Leaf_Pocket");
            rightLeaf.transform.SetParent(doorRoot.transform);
            rightLeaf.transform.localPosition = slideAlongX ? new Vector3(leafW / 2f, 1.25f, 0f) : new Vector3(0f, 1.25f, leafD / 2f);

            GameObject rightBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightBody.name = "Panel_Body";
            rightBody.transform.SetParent(rightLeaf.transform);
            rightBody.transform.localPosition = Vector3.zero;
            rightBody.transform.localScale = new Vector3(leafW, 2.5f, leafD);
            rightBody.GetComponent<Renderer>().sharedMaterial = leafMat;
            DestroyImmediate(rightBody.GetComponent<Collider>());

            InteractiveDoubleDoor doorScript = doorRoot.AddComponent<InteractiveDoubleDoor>();
            doorScript.SetupDoors(leftLeaf.transform, rightLeaf.transform, slideAlongX, openDistance);

            Vector3 labelPos = slideAlongX ? new Vector3(0f, 2.85f, -0.3f) : new Vector3(-0.3f, 2.85f, 0f);
            CreateLabel(labelTextTr, labelTextEn, doorRoot.transform, labelPos, labelColor, labelRotation);
        }

        private void CreateSingleLaneDeliveryRoad()
        {
            Transform deliveryGroup = new GameObject("Delivery_Road_System").transform;
            deliveryGroup.SetParent(environmentRoot);

            // Mid Road (-9f) ile North Road (50f) arasında uzanan dükkana ve tesise hizmet eden teslimat yolu
            GameObject deliveryRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deliveryRoad.name = "Flush_Single_Lane_Delivery_Road";
            deliveryRoad.transform.SetParent(deliveryGroup);
            deliveryRoad.transform.position = new Vector3(13.0f, -0.04f, 20.5f);
            deliveryRoad.transform.localScale = new Vector3(4f, 0.08f, 59f);
            deliveryRoad.GetComponent<Renderer>().sharedMaterial = mainRoadMat;

            GameObject loadingZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            loadingZone.name = "Truck_Loading_Unloading_Zone";
            loadingZone.transform.SetParent(deliveryGroup);
            loadingZone.transform.position = new Vector3(13.0f, 0.01f, 2f);
            loadingZone.transform.localScale = new Vector3(3.8f, 0.01f, 6.0f);
            loadingZone.GetComponent<Renderer>().sharedMaterial = loadingZoneMat;

            CreateZoneBorderLine(deliveryGroup, new Vector3(13.0f, 0.02f, 5f), new Vector3(3.8f, 0.01f, 0.15f));
            CreateZoneBorderLine(deliveryGroup, new Vector3(13.0f, 0.02f, -1f), new Vector3(3.8f, 0.01f, 0.15f));
            CreateZoneBorderLine(deliveryGroup, new Vector3(11.1f, 0.02f, 2f), new Vector3(0.15f, 0.01f, 6.0f));
            CreateZoneBorderLine(deliveryGroup, new Vector3(14.9f, 0.02f, 2f), new Vector3(0.15f, 0.01f, 6.0f));

            CreateLabel("KAMYON YANAŞMA ALANI", "TRUCK DOCK AREA", deliveryGroup, new Vector3(13.0f, 0.05f, 2f), Color.yellow, 90f);

            Transform crosswalkGroup = new GameObject("Pedestrian_Crosswalk_Right").transform;
            crosswalkGroup.SetParent(deliveryGroup);

            for (float x = 11.5f; x <= 14.5f; x += 0.6f)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Zebra_Stripe";
                stripe.transform.SetParent(crosswalkGroup);
                stripe.transform.position = new Vector3(x, 0.02f, -4.5f);
                stripe.transform.localScale = new Vector3(0.35f, 0.01f, 2.4f);
                stripe.GetComponent<Renderer>().sharedMaterial = crosswalkMat;
            }
        }

        private void CreateCustomerParkingLotAndTurnstile()
        {
            Transform parkingGroup = new GameObject("Customer_Parking_System").transform;
            parkingGroup.SetParent(environmentRoot);

            // Asfalt yol genişletmesi (Seviye 1: 20m, Seviye 2: 29m, Seviye 3: 38m)
            float totalZLength = (currentUpgradeLevel == 1) ? 20.0f : ((currentUpgradeLevel == 2) ? 29.0f : 38.0f);
            float parkingCenterZ = -3.0f + (totalZLength / 2f);

            GameObject parkingFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            parkingFloor.name = "Customer_Parking_Asphalt_Floor";
            parkingFloor.transform.SetParent(parkingGroup);
            parkingFloor.transform.position = new Vector3(-27.0f, -0.04f, parkingCenterZ);
            parkingFloor.transform.localScale = new Vector3(26f, 0.08f, totalZLength);
            parkingFloor.GetComponent<Renderer>().sharedMaterial = mainRoadMat;

            CreateOutlineBox(parkingGroup, new Vector3(-27.0f, 0.01f, parkingCenterZ), 26f, totalZLength);

            int totalSlotsCount = (currentUpgradeLevel == 1) ? 10 : ((currentUpgradeLevel == 2) ? 16 : 22);
            int slotsPerRow = totalSlotsCount / 2;

            CreateLabel($"MÜŞTERİ OTOPARKI ({totalSlotsCount} ARAÇ KAPASİTESİ)", $"CUSTOMER PARKING ({totalSlotsCount} VEHICLES CAPACITY)", parkingGroup, new Vector3(-27.0f, 3.5f, -3.0f + totalZLength - 1.5f), Color.white);

            float slotWidthX = 5.2f;
            float slotDepthZ = 2.6f;
            float zPitch = 2.8f;

            // Sol sütun (Sol alt köşeden sınırlanır)
            for (int i = 0; i < slotsPerRow; i++)
            {
                float pz = 1.0f + (i * zPitch);
                CreateAttachedParkingSlot(parkingGroup, new Vector3(-39.0f + (slotWidthX / 2f), 0.01f, pz), slotWidthX, slotDepthZ, "P" + (i + 1), true);
            }

            // Sağ sütun (Yukarıdaki sınır çizgisinden aşağı doğru sınırlanır, turnike girişinde geniş manevra alanı bırakır)
            float topPZ = (-3.0f + totalZLength) - 2.8f;
            for (int i = 0; i < slotsPerRow; i++)
            {
                float pz = topPZ - ((slotsPerRow - 1 - i) * zPitch);
                CreateAttachedParkingSlot(parkingGroup, new Vector3(-15.0f - (slotWidthX / 2f), 0.01f, pz), slotWidthX, slotDepthZ, "P" + (i + 1 + slotsPerRow), false);
            }

            for (float pz = 3.0f; pz < -3.0f + totalZLength - 2.0f; pz += 7.0f)
            {
                CreateLabel("▲", "▲", parkingGroup, new Vector3(-27.0f, 0.02f, pz), new Color(1.0f, 0.90f, 0.30f, 0.85f));
            }

            GameObject driveway = GameObject.CreatePrimitive(PrimitiveType.Cube);
            driveway.name = "Parking_Driveway_TwoWay";
            driveway.transform.SetParent(parkingGroup);
            driveway.transform.position = new Vector3(-17.0f, -0.04f, -6f);
            driveway.transform.localScale = new Vector3(6f, 0.08f, 6f);
            driveway.GetComponent<Renderer>().sharedMaterial = mainRoadMat;

            for (float z = -8.5f; z <= -3.5f; z += 1.8f)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Driveway_Divider_Line";
                line.transform.SetParent(parkingGroup);
                line.transform.position = new Vector3(-17.0f, 0.01f, z);
                line.transform.localScale = new Vector3(0.2f, 0.02f, 1.0f);
                line.GetComponent<Renderer>().sharedMaterial = roadLineMat;
            }

            // DİFT TURNİKE KAPISI (SAĞDA GİRİŞ TURNİKESİ OTOPARK İÇ KÖŞESİNDE X=-14.0m, SOLDA ÇIKIŞ TURNİKESİ X=-20.0m)
            GameObject turnstileRightRoot = new GameObject("Parking_Barrier_Turnstile_Entrance");
            turnstileRightRoot.transform.SetParent(parkingGroup);
            turnstileRightRoot.transform.position = new Vector3(-14.0f, 0.0f, -3.0f);

            ParkingBarrierTurnstile turnstileRightScript = turnstileRightRoot.AddComponent<ParkingBarrierTurnstile>();

            GameObject housingRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housingRight.name = "Turnstile_Housing_Right";
            housingRight.transform.SetParent(turnstileRightRoot.transform);
            housingRight.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            housingRight.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
            housingRight.GetComponent<Renderer>().sharedMaterial = barrierHousingMat;

            GameObject armPivotRight = new GameObject("Turnstile_Arm_Pivot_Right");
            armPivotRight.transform.SetParent(turnstileRightRoot.transform);
            armPivotRight.transform.localPosition = new Vector3(0f, 1.05f, 0f);

            GameObject armRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armRight.name = "Turnstile_Barrier_Arm_Right";
            armRight.transform.SetParent(armPivotRight.transform);
            armRight.transform.localPosition = new Vector3(-1.5f, 0f, 0f);
            armRight.transform.localScale = new Vector3(3.0f, 0.12f, 0.12f);
            armRight.GetComponent<Renderer>().sharedMaterial = barrierArmMat;

            turnstileRightScript.SetupTurnstile(armPivotRight.transform);

            CreateLabel("TURNİKE (GİRİŞ)", "TURNSTILE (ENTRY)", turnstileRightRoot.transform, new Vector3(0f, 1.6f, 0f), Color.yellow);

            // ÇIKIŞ TURNİKESİ (TAM KARŞISINDA / SOL KANATTA X=-20.0m)
            GameObject turnstileLeftRoot = new GameObject("Parking_Barrier_Turnstile_Exit");
            turnstileLeftRoot.transform.SetParent(parkingGroup);
            turnstileLeftRoot.transform.position = new Vector3(-20.0f, 0.0f, -3.0f);

            ParkingBarrierTurnstile turnstileLeftScript = turnstileLeftRoot.AddComponent<ParkingBarrierTurnstile>();

            GameObject housingLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housingLeft.name = "Turnstile_Housing_Left";
            housingLeft.transform.SetParent(turnstileLeftRoot.transform);
            housingLeft.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            housingLeft.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
            housingLeft.GetComponent<Renderer>().sharedMaterial = barrierHousingMat;

            GameObject armPivotLeft = new GameObject("Turnstile_Arm_Pivot_Left");
            armPivotLeft.transform.SetParent(turnstileLeftRoot.transform);
            armPivotLeft.transform.localPosition = new Vector3(0f, 1.05f, 0f);

            GameObject armLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armLeft.name = "Turnstile_Barrier_Arm_Left";
            armLeft.transform.SetParent(armPivotLeft.transform);
            armLeft.transform.localPosition = new Vector3(1.5f, 0f, 0f);
            armLeft.transform.localScale = new Vector3(3.0f, 0.12f, 0.12f);
            armLeft.GetComponent<Renderer>().sharedMaterial = barrierArmMat;

            turnstileLeftScript.SetupTurnstile(armPivotLeft.transform);

            CreateLabel("TURNİKE (ÇIKIŞ)", "TURNSTILE (EXIT)", turnstileLeftRoot.transform, new Vector3(0f, 1.6f, 0f), Color.cyan);

            // Turnike Yanı Güvenlik Duvarları (Karakterlerin/Müşterilerin dükkan sol duvarı ile turnikeler arasındaki çıkmaz ara boşluğa sapmasını %100 önler)
            CreateWall("Store_Turnstile_Barrier_Wall", parkingGroup, new Vector3(-13.5f, 1.0f, -3.0f), new Vector3(1.1f, 2.0f, 0.4f));
            CreateWall("Parking_Exit_Turnstile_West_Barrier_Wall", parkingGroup, new Vector3(-23.0f, 1.0f, -3.0f), new Vector3(6.0f, 2.0f, 0.4f));

            // KUSURSUZ ÇİFT TURNİKE YAYA GEÇİDİ (KALDIRIM İLE TAM HİZALI Z: -6.0m ile -3.0m ARASI 3m DERİNLİK)
            Transform turnstileCrosswalkGroup = new GameObject("Pedestrian_Crosswalk_Turnstile_Full").transform;
            turnstileCrosswalkGroup.SetParent(parkingGroup);

            for (float x = -19.5f; x <= -14.5f; x += 0.80f)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Turnstile_Zebra_Stripe";
                stripe.transform.SetParent(turnstileCrosswalkGroup);
                stripe.transform.position = new Vector3(x, 0.02f, -4.5f);
                stripe.transform.localScale = new Vector3(0.45f, 0.01f, 3.0f);
                stripe.GetComponent<Renderer>().sharedMaterial = crosswalkMat;
            }
        }

        private void CreateTownshipSystem()
        {
            Transform townGroup = new GameObject("Township_Complex").transform;
            townGroup.SetParent(environmentRoot);

            GameObject squareFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            squareFloor.name = "Town_Center_Square_Paving";
            squareFloor.transform.SetParent(townGroup);
            squareFloor.transform.position = new Vector3(0f, 0.01f, -31.5f);
            squareFloor.transform.localScale = new Vector3(26f, 0.02f, 18f);
            squareFloor.GetComponent<Renderer>().sharedMaterial = townSquareMat;

            CreateLabel("KASABA MEYDANI", "TOWN SQUARE", townGroup, new Vector3(0f, 3.5f, -31.5f), Color.yellow);

            GameObject fountain = new GameObject("Town_Decorative_Fountain");
            fountain.transform.SetParent(townGroup);
            fountain.transform.position = new Vector3(0f, 0f, -31.5f);

            GameObject basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basin.name = "Fountain_Basin";
            basin.transform.SetParent(fountain.transform);
            basin.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            basin.transform.localScale = new Vector3(4.5f, 0.25f, 4.5f);
            basin.GetComponent<Renderer>().sharedMaterial = pondStoneMat;

            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Water_Surface";
            water.transform.SetParent(fountain.transform);
            water.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            water.transform.localScale = new Vector3(4.0f, 0.02f, 4.0f);
            water.GetComponent<Renderer>().sharedMaterial = pondWaterMat;

            GameObject jet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            jet.name = "Water_Jet_Column";
            jet.transform.SetParent(fountain.transform);
            jet.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            jet.transform.localScale = new Vector3(0.6f, 0.75f, 0.6f);
            jet.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            CreateTownBuildingDetailed(townGroup, "Town_Hall_Building", new Vector3(0f, 0f, -43.5f), new Vector3(11f, 5.5f, 5.5f), townHallWallMat, roofBlueMat, "KASABA BELEDİYESİ", "TOWN HALL", Color.cyan, true, "TownHall");
            CreateTownBuildingDetailed(townGroup, "Town_Bakery", new Vector3(-22f, 0f, -20f), new Vector3(8f, 3.5f, 5.0f), bakeryWallMat, roofBrownMat, "TAŞ FIRIN", "BAKERY", new Color(1.0f, 0.60f, 0.20f), false, "Bakery");
            CreateTownBuildingDetailed(townGroup, "Town_GreenGrocer", new Vector3(22f, 0f, -20f), new Vector3(8f, 3.5f, 5.0f), cafeWallMat, roofRedMat, "KASABA MANAVI", "GREENGROCER", Color.green, false, "GreenGrocer");
            CreateTownBuildingDetailed(townGroup, "Town_Cafe", new Vector3(-45f, 0f, -31.5f), new Vector3(8.5f, 5.2f, 5.5f), cafeWallMat, roofRedMat, "KASABA KAFESİ", "TOWN CAFE", new Color(0.95f, 0.45f, 0.75f), true, "Cafe");

            CreateTownBuildingDetailed(townGroup, "Residential_House_1", new Vector3(45f, 0f, -31.5f), new Vector3(8.5f, 5.2f, 5.5f), resWallBlueMat, roofRedMat, "MÜSTAKİL EV", "HOUSE", Color.white, true, "Residential");
            CreateTownBuildingDetailed(townGroup, "Residential_House_2", new Vector3(-22f, 0f, -43.5f), new Vector3(7.5f, 3.8f, 5.2f), resWallYellowMat, roofBrownMat, "KASABA EVİ", "HOUSE", Color.white, false, "Residential");
            CreateTownBuildingDetailed(townGroup, "Residential_House_3", new Vector3(22f, 0f, -43.5f), new Vector3(8.0f, 5.0f, 5.2f), farmhouseWallMat, roofBlueMat, "MÜSTAKİL EV", "HOUSE", Color.white, true, "Residential");

            CreateTownGardenField(townGroup, "Town_Flower_Garden_West", new Vector3(-48f, 0.02f, -19.5f), new Vector3(7f, 0.04f, 5f), flowerRedMat, "ÇİÇEK BAHÇESİ", "FLOWER GARDEN");
            CreateTownGardenField(townGroup, "Town_Vegetable_Garden_East", new Vector3(48f, 0.02f, -19.5f), new Vector3(7f, 0.04f, 5f), flowerYellowMat, "SEBZE BAHÇESİ", "VEGETABLE GARDEN");
            CreateTownGardenField(townGroup, "Town_Mini_Wheat_Field", new Vector3(45f, 0.02f, -43.5f), new Vector3(7.5f, 0.04f, 5f), wheatCropMat, "UFAK BUĞDAY TARLASI", "WHEAT FIELD");

            CreateTownFootpath(townGroup, new Vector3(0f, 0.01f, -38.5f), new Vector3(3.5f, 0.02f, 4f));
            CreateTownFootpath(townGroup, new Vector3(-20f, 0.01f, -31.5f), new Vector3(14f, 0.02f, 3.0f));
            CreateTownFootpath(townGroup, new Vector3(20f, 0.01f, -31.5f), new Vector3(14f, 0.02f, 3.0f));

            Vector3[] townTreePos = new Vector3[] {
                new Vector3(-12f, 0f, -20f), new Vector3(12f, 0f, -20f),
                new Vector3(-12f, 0f, -41f), new Vector3(12f, 0f, -41f),
                new Vector3(-32f, 0f, -31.5f), new Vector3(32f, 0f, -31.5f),
                new Vector3(-58f, 0f, -31.5f), new Vector3(58f, 0f, -31.5f),
                new Vector3(-35f, 0f, -43.5f), new Vector3(35f, 0f, -43.5f)
            };

            foreach (Vector3 pos in townTreePos)
            {
                GameObject tree = new GameObject("Town_Tree");
                tree.transform.SetParent(townGroup);
                tree.transform.position = pos;

                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trunk.name = "Trunk";
                trunk.transform.SetParent(tree.transform);
                trunk.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                trunk.transform.localScale = new Vector3(0.45f, 2.0f, 0.45f);
                trunk.GetComponent<Renderer>().sharedMaterial = treeTrunkMat;

                GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foliage.name = "Foliage";
                foliage.transform.SetParent(tree.transform);
                foliage.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                foliage.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
                foliage.GetComponent<Renderer>().sharedMaterial = treeFoliageMat;
            }
        }

        private void CreateTownBuildingDetailed(Transform parent, string name, Vector3 pos, Vector3 size, Material wallMat, Material roofMat, string labelText, string labelEn, Color labelColor, bool isTwoStory, string buildingType = "Generic")
        {
            GameObject buildingRoot = new GameObject(name);
            buildingRoot.transform.SetParent(parent);
            buildingRoot.transform.position = pos;

            // 1. TEMEL ETEK SU BASMAN TAŞI (BASEBOARD FOUNDATION PLINTH)
            GameObject baseboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseboard.name = "Foundation_Plinth";
            baseboard.transform.SetParent(buildingRoot.transform);
            baseboard.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            baseboard.transform.localScale = new Vector3(size.x + 0.2f, 0.3f, size.z + 0.2f);
            baseboard.GetComponent<Renderer>().sharedMaterial = windowSillMat;

            // 2. ANA BİNA GÖVDESİ (MAIN BUILDING BODY)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Building_Body";
            body.transform.SetParent(buildingRoot.transform);
            body.transform.localPosition = new Vector3(0f, (size.y / 2f) + 0.3f, 0f);
            body.transform.localScale = size;
            body.GetComponent<Renderer>().sharedMaterial = wallMat;

            // 3. 4 DIŞ KÖŞE KOLON ÇITALARI (CORNER PILLAR TRIMS)
            float halfX = size.x / 2f;
            float halfZ = size.z / 2f;
            float bodyYCenter = (size.y / 2f) + 0.3f;
            Vector3[] cornerOffsets = new Vector3[] {
                new Vector3(-halfX, bodyYCenter, -halfZ),
                new Vector3(halfX, bodyYCenter, -halfZ),
                new Vector3(-halfX, bodyYCenter, halfZ),
                new Vector3(halfX, bodyYCenter, halfZ)
            };
            foreach (Vector3 offset in cornerOffsets)
            {
                GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                corner.name = "Corner_Trim_Pillar";
                corner.transform.SetParent(buildingRoot.transform);
                corner.transform.localPosition = offset;
                corner.transform.localScale = new Vector3(0.25f, size.y + 0.05f, 0.25f);
                corner.GetComponent<Renderer>().sharedMaterial = windowFrameMat;
            }

            // 4. ÇATI VE SAÇAKLAR (GABLED PITCHED ROOF & EAVES & CHIMNEY)
            float roofBaseY = size.y + 0.3f;
            GameObject roofEaveTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofEaveTrim.name = "Roof_Eave_Trim";
            roofEaveTrim.transform.SetParent(buildingRoot.transform);
            roofEaveTrim.transform.localPosition = new Vector3(0f, roofBaseY + 0.15f, 0f);
            roofEaveTrim.transform.localScale = new Vector3(size.x + 0.6f, 0.3f, size.z + 0.6f);
            roofEaveTrim.GetComponent<Renderer>().sharedMaterial = darkWallMat;

            // Eğimli Ana Çatı
            GameObject roofCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofCap.name = "Pitched_Roof_Cap";
            roofCap.transform.SetParent(buildingRoot.transform);
            roofCap.transform.localPosition = new Vector3(0f, roofBaseY + 0.85f, 0f);
            roofCap.transform.localScale = new Vector3(size.x + 0.4f, 1.1f, size.z + 0.4f);
            roofCap.GetComponent<Renderer>().sharedMaterial = roofMat;

            // Çatı Üstü Baca (Chimney)
            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.name = "Roof_Chimney";
            chimney.transform.SetParent(buildingRoot.transform);
            chimney.transform.localPosition = new Vector3(size.x * 0.25f, roofBaseY + 1.6f, size.z * 0.15f);
            chimney.transform.localScale = new Vector3(0.7f, 1.2f, 0.7f);
            chimney.GetComponent<Renderer>().sharedMaterial = chimneyBrickMat;

            GameObject chimneyCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimneyCap.name = "Chimney_Cap";
            chimneyCap.transform.SetParent(buildingRoot.transform);
            chimneyCap.transform.localPosition = new Vector3(size.x * 0.25f, roofBaseY + 2.25f, size.z * 0.15f);
            chimneyCap.transform.localScale = new Vector3(0.85f, 0.15f, 0.85f);
            chimneyCap.GetComponent<Renderer>().sharedMaterial = darkWallMat;

            // 5. İKİNCİ KAT AYIRMA ŞERİDİ (2-STORY FLOOR SEPARATOR BAND)
            if (isTwoStory)
            {
                GameObject floorBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorBand.name = "Floor_Separator_Band";
                floorBand.transform.SetParent(buildingRoot.transform);
                floorBand.transform.localPosition = new Vector3(0f, (size.y * 0.5f) + 0.3f, 0f);
                floorBand.transform.localScale = new Vector3(size.x + 0.25f, 0.25f, size.z + 0.25f);
                floorBand.GetComponent<Renderer>().sharedMaterial = windowFrameMat;
            }

            // 6. ÖN ANA KAPI, ÇERÇEVE, KULP VE GİRİŞ BASAMAĞI (FRONT MAIN DOOR)
            float doorZ = halfZ + 0.05f;
            float doorY = 1.0f;
            float doorW = 1.3f;
            float doorH = 2.0f;

            // Kapı Çerçevesi
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "Main_Door_Frame";
            doorFrame.transform.SetParent(buildingRoot.transform);
            doorFrame.transform.localPosition = new Vector3(0f, doorY, doorZ);
            doorFrame.transform.localScale = new Vector3(doorW + 0.2f, doorH + 0.2f, 0.12f);
            doorFrame.GetComponent<Renderer>().sharedMaterial = doorFrameMat;

            // Kapı Paneli
            GameObject doorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorPanel.name = "Main_Door_Panel";
            doorPanel.transform.SetParent(buildingRoot.transform);
            doorPanel.transform.localPosition = new Vector3(0f, doorY, doorZ + 0.02f);
            doorPanel.transform.localScale = new Vector3(doorW, doorH, 0.1f);
            doorPanel.GetComponent<Renderer>().sharedMaterial = woodDoorMat;

            // Kapı Kulpu
            GameObject doorHandle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            doorHandle.name = "Main_Door_Handle";
            doorHandle.transform.SetParent(buildingRoot.transform);
            doorHandle.transform.localPosition = new Vector3(0.45f, doorY, doorZ + 0.08f);
            doorHandle.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            doorHandle.GetComponent<Renderer>().sharedMaterial = doorHandleMat;

            // Giriş Basamağı
            GameObject doorStep = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorStep.name = "Main_Door_Step";
            doorStep.transform.SetParent(buildingRoot.transform);
            doorStep.transform.localPosition = new Vector3(0f, 0.15f, doorZ + 0.35f);
            doorStep.transform.localScale = new Vector3(doorW + 0.6f, 0.25f, 0.6f);
            doorStep.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            // 7. DETAYLI PENCERELER (CAM, ÇERÇEVE, MERYEM/DENİZLİK VE IZGARA)
            float windowYGround = isTwoStory ? (size.y * 0.28f) + 0.3f : (size.y * 0.50f) + 0.3f;
            float windowYUpper = (size.y * 0.75f) + 0.3f;

            // Ön Cephe Pencereleri (Sol & Sağ)
            float winOffsetX = size.x * 0.28f;
            CreateWindowWithFrame(buildingRoot.transform, new Vector3(-winOffsetX, windowYGround, doorZ), new Vector3(1.1f, 1.2f, 0.1f));
            CreateWindowWithFrame(buildingRoot.transform, new Vector3(winOffsetX, windowYGround, doorZ), new Vector3(1.1f, 1.2f, 0.1f));

            if (isTwoStory)
            {
                CreateWindowWithFrame(buildingRoot.transform, new Vector3(-winOffsetX, windowYUpper, doorZ), new Vector3(1.1f, 1.2f, 0.1f));
                CreateWindowWithFrame(buildingRoot.transform, new Vector3(0f, windowYUpper, doorZ), new Vector3(1.1f, 1.2f, 0.1f));
                CreateWindowWithFrame(buildingRoot.transform, new Vector3(winOffsetX, windowYUpper, doorZ), new Vector3(1.1f, 1.2f, 0.1f));
            }

            // Yan Cephe Pencereleri (Sol & Sağ Yan Duvarlar)
            float sideXLeft = -halfX - 0.05f;
            float sideXRight = halfX + 0.05f;
            CreateWindowWithFrameSide(buildingRoot.transform, new Vector3(sideXLeft, windowYGround, 0f), new Vector3(0.1f, 1.2f, 1.1f));
            CreateWindowWithFrameSide(buildingRoot.transform, new Vector3(sideXRight, windowYGround, 0f), new Vector3(0.1f, 1.2f, 1.1f));

            if (isTwoStory)
            {
                CreateWindowWithFrameSide(buildingRoot.transform, new Vector3(sideXLeft, windowYUpper, 0f), new Vector3(0.1f, 1.2f, 1.1f));
                CreateWindowWithFrameSide(buildingRoot.transform, new Vector3(sideXRight, windowYUpper, 0f), new Vector3(0.1f, 1.2f, 1.1f));
            }

            // Arka Cephe Pencereleri (Sol, Sağ ve Üst Kat Arka Duvarlar)
            float backZ = -halfZ - 0.05f;
            CreateWindowWithFrameBack(buildingRoot.transform, new Vector3(-winOffsetX, windowYGround, backZ), new Vector3(1.1f, 1.2f, 0.1f));
            CreateWindowWithFrameBack(buildingRoot.transform, new Vector3(winOffsetX, windowYGround, backZ), new Vector3(1.1f, 1.2f, 0.1f));

            if (isTwoStory)
            {
                CreateWindowWithFrameBack(buildingRoot.transform, new Vector3(-winOffsetX, windowYUpper, backZ), new Vector3(1.1f, 1.2f, 0.1f));
                CreateWindowWithFrameBack(buildingRoot.transform, new Vector3(0f, windowYUpper, backZ), new Vector3(1.1f, 1.2f, 0.1f));
                CreateWindowWithFrameBack(buildingRoot.transform, new Vector3(winOffsetX, windowYUpper, backZ), new Vector3(1.1f, 1.2f, 0.1f));
            }

            // 8. BİNAYA ÖZEL MİMARİ EKSTRALAR (CANOPY, AWNING, COLUMNS)
            if (buildingType == "Bakery")
            {
                GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
                awning.name = "Bakery_Awning";
                awning.transform.SetParent(buildingRoot.transform);
                awning.transform.localPosition = new Vector3(0f, 2.2f, doorZ + 0.5f);
                awning.transform.localScale = new Vector3(size.x * 0.85f, 0.15f, 1.1f);
                awning.GetComponent<Renderer>().sharedMaterial = awningRedWhiteMat;
            }
            else if (buildingType == "GreenGrocer")
            {
                GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
                awning.name = "Grocer_Awning";
                awning.transform.SetParent(buildingRoot.transform);
                awning.transform.localPosition = new Vector3(0f, 2.2f, doorZ + 0.5f);
                awning.transform.localScale = new Vector3(size.x * 0.85f, 0.15f, 1.1f);
                awning.GetComponent<Renderer>().sharedMaterial = awningGreenMat;

                for (float cx = -2.0f; cx <= 2.0f; cx += 1.3f)
                {
                    if (Mathf.Abs(cx) < 0.8f) continue;
                    GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crate.name = "Grocer_Produce_Crate";
                    crate.transform.SetParent(buildingRoot.transform);
                    crate.transform.localPosition = new Vector3(cx, 0.35f, doorZ + 0.7f);
                    crate.transform.localScale = new Vector3(0.9f, 0.5f, 0.7f);
                    crate.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;
                }
            }
            else if (buildingType == "Cafe")
            {
                GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
                awning.name = "Cafe_Awning";
                awning.transform.SetParent(buildingRoot.transform);
                awning.transform.localPosition = new Vector3(0f, 2.2f, doorZ + 0.5f);
                awning.transform.localScale = new Vector3(size.x * 0.85f, 0.15f, 1.2f);
                awning.GetComponent<Renderer>().sharedMaterial = awningRedWhiteMat;
            }
            else if (buildingType == "TownHall")
            {
                float pillarX = size.x * 0.35f;
                GameObject pillarL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillarL.name = "TownHall_Column_Left";
                pillarL.transform.SetParent(buildingRoot.transform);
                pillarL.transform.localPosition = new Vector3(-pillarX, size.y / 2f, doorZ + 0.6f);
                pillarL.transform.localScale = new Vector3(0.5f, size.y / 2f, 0.5f);
                pillarL.GetComponent<Renderer>().sharedMaterial = pillarStoneMat;

                GameObject pillarR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillarR.name = "TownHall_Column_Right";
                pillarR.transform.SetParent(buildingRoot.transform);
                pillarR.transform.localPosition = new Vector3(pillarX, size.y / 2f, doorZ + 0.6f);
                pillarR.transform.localScale = new Vector3(0.5f, size.y / 2f, 0.5f);
                pillarR.GetComponent<Renderer>().sharedMaterial = pillarStoneMat;

                GameObject pediment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pediment.name = "TownHall_Pediment";
                pediment.transform.SetParent(buildingRoot.transform);
                pediment.transform.localPosition = new Vector3(0f, size.y + 0.8f, doorZ + 0.6f);
                pediment.transform.localScale = new Vector3(size.x + 0.2f, 0.5f, 0.8f);
                pediment.GetComponent<Renderer>().sharedMaterial = pillarStoneMat;
            }

            // 9. BİNA İSİM VE TABELA PANOSU (SIGNBOARD PANEL & BOLD LABEL)
            GameObject signboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signboard.name = "Building_Signboard_Panel";
            signboard.transform.SetParent(buildingRoot.transform);
            signboard.transform.localPosition = new Vector3(0f, size.y + 0.05f, doorZ + 0.1f);
            signboard.transform.localScale = new Vector3(size.x * 0.75f, 0.65f, 0.15f);
            signboard.GetComponent<Renderer>().sharedMaterial = darkWallMat;

            CreateLabel(labelText, labelEn, buildingRoot.transform, new Vector3(0f, size.y + 0.05f, doorZ + 0.20f), labelColor);
        }

        private void CreateWindowWithFrame(Transform parent, Vector3 localPos, Vector3 winSize)
        {
            GameObject winGroup = new GameObject("Window_Complex");
            winGroup.transform.SetParent(parent);
            winGroup.transform.localPosition = localPos;

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Window_Frame";
            frame.transform.SetParent(winGroup.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(winSize.x + 0.18f, winSize.y + 0.18f, 0.10f);
            frame.GetComponent<Renderer>().sharedMaterial = windowFrameMat;

            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Window_Glass_Pane";
            glass.transform.SetParent(winGroup.transform);
            glass.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            glass.transform.localScale = winSize;
            glass.GetComponent<Renderer>().sharedMaterial = windowGlassMat;

            GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "Window_Sill";
            sill.transform.SetParent(winGroup.transform);
            sill.transform.localPosition = new Vector3(0f, -winSize.y / 2f - 0.08f, 0.10f);
            sill.transform.localScale = new Vector3(winSize.x + 0.35f, 0.15f, 0.25f);
            sill.GetComponent<Renderer>().sharedMaterial = windowSillMat;

            GameObject dividerV = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dividerV.name = "Window_Divider_V";
            dividerV.transform.SetParent(winGroup.transform);
            dividerV.transform.localPosition = new Vector3(0f, 0f, 0.03f);
            dividerV.transform.localScale = new Vector3(0.06f, winSize.y, 0.08f);
            dividerV.GetComponent<Renderer>().sharedMaterial = windowFrameMat;

            GameObject dividerH = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dividerH.name = "Window_Divider_H";
            dividerH.transform.SetParent(winGroup.transform);
            dividerH.transform.localPosition = new Vector3(0f, 0f, 0.03f);
            dividerH.transform.localScale = new Vector3(winSize.x, 0.06f, 0.08f);
            dividerH.GetComponent<Renderer>().sharedMaterial = windowFrameMat;
        }

        private void CreateWindowWithFrameSide(Transform parent, Vector3 localPos, Vector3 winSize)
        {
            GameObject winGroup = new GameObject("Window_Complex_Side");
            winGroup.transform.SetParent(parent);
            winGroup.transform.localPosition = localPos;

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Window_Frame_Side";
            frame.transform.SetParent(winGroup.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(0.10f, winSize.y + 0.18f, winSize.z + 0.18f);
            frame.GetComponent<Renderer>().sharedMaterial = windowFrameMat;

            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Window_Glass_Pane_Side";
            glass.transform.SetParent(winGroup.transform);
            glass.transform.localPosition = Vector3.zero;
            glass.transform.localScale = winSize;
            glass.GetComponent<Renderer>().sharedMaterial = windowGlassMat;

            GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "Window_Sill_Side";
            sill.transform.SetParent(winGroup.transform);
            sill.transform.localPosition = new Vector3(0f, -winSize.y / 2f - 0.08f, 0f);
            sill.transform.localScale = new Vector3(0.25f, 0.15f, winSize.z + 0.35f);
            sill.GetComponent<Renderer>().sharedMaterial = windowSillMat;
        }

        private void CreateWindowWithFrameBack(Transform parent, Vector3 localPos, Vector3 winSize)
        {
            GameObject winGroup = new GameObject("Window_Complex_Back");
            winGroup.transform.SetParent(parent);
            winGroup.transform.localPosition = localPos;

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Window_Frame_Back";
            frame.transform.SetParent(winGroup.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(winSize.x + 0.18f, winSize.y + 0.18f, 0.10f);
            frame.GetComponent<Renderer>().sharedMaterial = windowFrameMat;

            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Window_Glass_Pane_Back";
            glass.transform.SetParent(winGroup.transform);
            glass.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            glass.transform.localScale = winSize;
            glass.GetComponent<Renderer>().sharedMaterial = windowGlassMat;

            GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "Window_Sill_Back";
            sill.transform.SetParent(winGroup.transform);
            sill.transform.localPosition = new Vector3(0f, -winSize.y / 2f - 0.08f, -0.10f);
            sill.transform.localScale = new Vector3(winSize.x + 0.35f, 0.15f, 0.25f);
            sill.GetComponent<Renderer>().sharedMaterial = windowSillMat;

            GameObject dividerV = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dividerV.name = "Window_Divider_V_Back";
            dividerV.transform.SetParent(winGroup.transform);
            dividerV.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            dividerV.transform.localScale = new Vector3(0.06f, winSize.y, 0.08f);
            dividerV.GetComponent<Renderer>().sharedMaterial = windowFrameMat;

            GameObject dividerH = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dividerH.name = "Window_Divider_H_Back";
            dividerH.transform.SetParent(winGroup.transform);
            dividerH.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            dividerH.transform.localScale = new Vector3(winSize.x, 0.06f, 0.08f);
            dividerH.GetComponent<Renderer>().sharedMaterial = windowFrameMat;
        }

        private void CreateTownGardenField(Transform parent, string name, Vector3 pos, Vector3 size, Material cropMat, string labelText, string labelEn)
        {
            GameObject gardenRoot = new GameObject(name);
            gardenRoot.transform.SetParent(parent);
            gardenRoot.transform.position = pos;

            GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.name = "Garden_Soil_Bed";
            soil.transform.SetParent(gardenRoot.transform);
            soil.transform.localPosition = Vector3.zero;
            soil.transform.localScale = size;
            soil.GetComponent<Renderer>().sharedMaterial = cropMat;

            CreateLabel(labelText, labelEn, gardenRoot.transform, new Vector3(0f, 1.2f, 0f), Color.white);
        }

        private void CreateTownFootpath(Transform parent, Vector3 pos, Vector3 scale)
        {
            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = "Town_Pedestrian_Path";
            path.transform.SetParent(parent);
            path.transform.position = pos;
            path.transform.localScale = scale;
            path.GetComponent<Renderer>().sharedMaterial = footpathMat;
        }

        private void CreateOutlineBox(Transform parent, Vector3 center, float width, float depth)
        {
            GameObject boxObj = new GameObject("Parking_Outer_Outline_Box");
            boxObj.transform.SetParent(parent);
            boxObj.transform.position = center;

            float halfW = width / 2f;
            float halfD = depth / 2f;
            float lineT = 0.15f;

            CreateLineSegment(boxObj.transform, new Vector3(-halfW, 0.01f, 0f), new Vector3(lineT, 0.01f, depth));
            CreateLineSegment(boxObj.transform, new Vector3(halfW, 0.01f, 0f), new Vector3(lineT, 0.01f, depth));
            CreateLineSegment(boxObj.transform, new Vector3(0f, 0.01f, halfD), new Vector3(width, 0.01f, lineT));
            CreateLineSegment(boxObj.transform, new Vector3(0f, 0.01f, -halfD), new Vector3(width, 0.01f, lineT));
        }

        private void CreateAttachedParkingSlot(Transform parent, Vector3 center, float sizeX, float sizeZ, string slotName, bool isLeftEdge)
        {
            GameObject slotObj = new GameObject("Parking_Slot_" + slotName);
            slotObj.transform.SetParent(parent);
            slotObj.transform.position = center;

            float halfX = sizeX / 2f;
            float halfZ = sizeZ / 2f;
            float lineThickness = 0.10f;

            CreateLineSegment(slotObj.transform, new Vector3(0f, 0.01f, halfZ), new Vector3(sizeX, 0.01f, lineThickness));
            CreateLineSegment(slotObj.transform, new Vector3(0f, 0.01f, -halfZ), new Vector3(sizeX, 0.01f, lineThickness));

            float backLineX = isLeftEdge ? -halfX : halfX;
            CreateLineSegment(slotObj.transform, new Vector3(backLineX, 0.01f, 0f), new Vector3(lineThickness, 0.01f, sizeZ));

            CreateLabel(slotName, slotName, slotObj.transform, new Vector3(0f, 0.03f, 0f), Color.white);
        }

        private void CreateCleanFarmComplex()
        {
            Transform farmGroup = new GameObject("Clean_Farm_Complex").transform;
            farmGroup.SetParent(environmentRoot);

            GameObject farmFootpath = GameObject.CreatePrimitive(PrimitiveType.Cube);
            farmFootpath.name = "Pedestrian_Farm_Footpath";
            farmFootpath.transform.SetParent(farmGroup);
            farmFootpath.transform.position = new Vector3(17.5f, 0.01f, 2f);
            farmFootpath.transform.localScale = new Vector3(5.0f, 0.04f, 2.4f);
            farmFootpath.GetComponent<Renderer>().sharedMaterial = footpathMat;

            CreateLabel("ÇİFTLİK YAYA YOLU", "FARM PEDESTRIAN WALKWAY", farmGroup, new Vector3(17.5f, 0.05f, 2f), Color.white);

            // BİNALARI YUKARIDAKİ KALDIRIM VE AĞAÇ HATTI ÖNÜNE SINIRLA (Z = 36.5m)
            CreateUltraDetailedFarmhouse(farmGroup, new Vector3(25.0f, 0f, 36.5f));
            CreateUltraDetailedBarn(farmGroup, new Vector3(37.5f, 0f, 36.5f));

            // AĞAÇ VE ÇİT İLE TÜM ÇİFTLİK ÇEVRESİNİ KUŞAT (YUKARIDAN AŞAĞIYA KADAR)
            CreateFarmFenceAndTreeEnclosure(farmGroup);

            // GÖLÜ TAM ORTAYA KOY (Z = 15.0m, X = 33.0m)
            GameObject pond = new GameObject("Farm_Water_Pond");
            pond.transform.SetParent(farmGroup);
            pond.transform.position = new Vector3(33.0f, 0f, 15.0f);

            GameObject pondWater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pondWater.name = "Water_Surface";
            pondWater.transform.SetParent(pond.transform);
            pondWater.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            pondWater.transform.localScale = new Vector3(4.2f, 0.02f, 4.2f);
            pondWater.GetComponent<Renderer>().sharedMaterial = pondWaterMat;

            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * 2f / 10f;
                float sx = Mathf.Cos(angle) * 2.1f;
                float sz = Mathf.Sin(angle) * 2.1f;

                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "Pond_Rock";
                rock.transform.SetParent(pond.transform);
                rock.transform.localPosition = new Vector3(sx, 0.15f, sz);
                rock.transform.localScale = new Vector3(0.55f, 0.35f, 0.55f);
                rock.GetComponent<Renderer>().sharedMaterial = pondStoneMat;
            }

            CreateLabel("ÇİFTLİK GÖLÜ", "FARM POND", pond.transform, new Vector3(0f, 1.6f, 0f), Color.cyan);

            // GÖLÜN ETRAFINA ALABİLDİĞİNCE EKİLEBİLİR TARLA YAP (ÇİFTLİK YAYA YOLU ÇAKIŞMASIZ)
            Transform soilGroup = new GameObject("Farmland_Soil_Plots_Grid").transform;
            soilGroup.SetParent(farmGroup);

            float plotSize = 2.2f;
            int count = 1;

            // Yaya yolu (X: 15-20, Z: 0.8-3.2) ile çakışmayı önlemek için tarlalar X=21.8m ve Z=4.2m'den başlar
            float[] colX = new float[] { 21.8f, 26.2f, 30.6f, 36.2f, 40.6f, 45.0f };
            float[] rowZ = new float[] { 4.2f, 8.0f, 11.8f, 15.6f, 19.4f, 23.2f, 27.0f };

            foreach (float pz in rowZ)
            {
                foreach (float px in colX)
                {
                    // Gölün bulunduğu orta alanı atla
                    if (Mathf.Abs(px - 33.0f) < 4.0f && Mathf.Abs(pz - 15.0f) < 3.5f) continue;

                    GameObject plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plot.name = $"Soil_Plot_{count}";
                    plot.transform.SetParent(soilGroup);
                    plot.transform.position = new Vector3(px, 0.02f, pz);
                    plot.transform.localScale = new Vector3(plotSize, 0.04f, plotSize);
                    plot.GetComponent<Renderer>().sharedMaterial = soilPlotMat;

                    BoxCollider pCol = plot.GetComponent<BoxCollider>();
                    if (pCol != null)
                    {
                        pCol.center = new Vector3(0f, 20.0f, 0f);
                        pCol.size = new Vector3(1.0f, 40.0f, 1.0f);
                    }

                    plot.AddComponent<FieldPlotController>();

                    GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    border.name = "Soil_Border";
                    border.transform.SetParent(plot.transform);
                    border.transform.localPosition = new Vector3(0f, -0.01f, 0f);
                    border.transform.localScale = new Vector3(1.1f, 0.5f, 1.1f);
                    border.GetComponent<Renderer>().sharedMaterial = soilBorderMat;

                    Collider bCol = border.GetComponent<Collider>();
                    if (bCol != null) UnityEngine.Object.DestroyImmediate(bCol);

                    count++;
                }
            }

            CreateLabel($"EKİLEBİLİR TARLA ({count - 1} TİLE)", $"PLANTABLE FIELD ({count - 1} TILES)", soilGroup, new Vector3(33.0f, 1.2f, 3.5f), new Color(1.0f, 0.75f, 0.30f));
        }

        private void CreateFenceLine(Transform parent, Vector3 pos, Vector3 scale)
        {
            GameObject fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fence.name = "Wooden_Fence";
            fence.transform.SetParent(parent);
            fence.transform.position = pos;
            fence.transform.localScale = scale;
            fence.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;
        }

        private void CreateLineSegment(Transform parent, Vector3 localPos, Vector3 scale)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "Slot_Line";
            line.transform.SetParent(parent);
            line.transform.localPosition = localPos;
            line.transform.localScale = scale;
            line.GetComponent<Renderer>().sharedMaterial = parkingLineMat;
        }

        private void CreateZoneBorderLine(Transform parent, Vector3 pos, Vector3 scale)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "Zone_Border_Line";
            line.transform.SetParent(parent);
            line.transform.position = pos;
            line.transform.localScale = scale;
            line.GetComponent<Renderer>().sharedMaterial = roadLineMat;
        }

        private void CreateWall(string wallName, Transform parent, Vector3 pos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = darkWallMat;
        }

        private class World3DLabelInfo
        {
            public TextMesh mesh;
            public string textTr;
            public string textEn;
        }

        private List<World3DLabelInfo> activeWorldLabels = new List<World3DLabelInfo>();

        private void CreateLabel(string textTr, string textEn, Transform parent, Vector3 pos, Color color, float yRotation = 0f)
        {
            GameObject labelObj = new GameObject("Label_" + textTr);
            labelObj.transform.SetParent(parent);
            labelObj.transform.localPosition = pos;
            labelObj.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = LocalizationManager.L("Label3D_" + textTr, textTr, textEn);
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.15f;
            textMesh.color = color;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.fontStyle = FontStyle.Bold;

            activeWorldLabels.Add(new World3DLabelInfo { mesh = textMesh, textTr = textTr, textEn = textEn });
        }

        private void CreateLabel(string text, Transform parent, Vector3 pos, Color color, float yRotation = 0f)
        {
            CreateLabel(text, text, parent, pos, color, yRotation);
        }

        private void CreateLightingAndDecorations()
        {
            GameObject lightObj = GameObject.Find("Directional Light");
            if (lightObj == null)
            {
                lightObj = new GameObject("Directional Light");
                Light lightComp = lightObj.AddComponent<Light>();
                lightComp.type = LightType.Directional;
            }

            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light l = lightObj.GetComponent<Light>();
            l.intensity = 1.2f;
            l.color = new Color(1f, 0.96f, 0.90f);
            l.shadows = LightShadows.Soft;

            // 3D Sokak Lambaları ve Mağaza İçi Tavan Aydınlatmalarını Kur
            BuildStreetLampsAndStoreLighting();
        }

        private void BuildStreetLampsAndStoreLighting()
        {
            Transform lightingGroup = new GameObject("Street_And_Store_Lighting_Group").transform;
            lightingGroup.SetParent(environmentRoot);

            Material poleMat = CreateSolidMaterial("StreetLampPoleMat", new Color(0.18f, 0.20f, 0.24f), 0.7f, 0.8f);
            Material bulbDefaultMat = CreateSolidMaterial("StreetLampBulbMat", new Color(0.35f, 0.35f, 0.38f), 0.1f, 0.5f);

            // 1. ANA KALDIRIM VE OTOPARK SOKAK LAMBALARI (Street Lamps)
            Vector3[] lampPositions = new Vector3[]
            {
                new Vector3(-25.0f, 0.05f, -5.8f),
                new Vector3(-12.0f, 0.05f, -5.8f),
                new Vector3(  8.0f, 0.05f, -5.8f),
                new Vector3( 22.0f, 0.05f, -5.8f),

                // Otopark Çevresi Lambaları
                new Vector3(-20.0f, 0.05f, -18.5f),
                new Vector3( -5.0f, 0.05f, -18.5f),
                new Vector3( 10.0f, 0.05f, -18.5f)
            };

            foreach (var lPos in lampPositions)
            {
                GameObject lampObj = new GameObject("StreetLamp_Post");
                lampObj.transform.SetParent(lightingGroup, false);
                lampObj.transform.position = lPos;

                // Metalik Direk (3.4m yükseklik)
                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Lamp_Pole";
                pole.transform.SetParent(lampObj.transform, false);
                pole.transform.localPosition = new Vector3(0f, 1.7f, 0f);
                pole.transform.localScale = new Vector3(0.10f, 1.7f, 0.10f);
                pole.GetComponent<Renderer>().sharedMaterial = poleMat;
                Destroy(pole.GetComponent<Collider>());

                // Üst Kavis Başlık
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = "Lamp_Arm";
                arm.transform.SetParent(lampObj.transform, false);
                arm.transform.localPosition = new Vector3(0.25f, 3.35f, 0f);
                arm.transform.localScale = new Vector3(0.6f, 0.08f, 0.10f);
                arm.GetComponent<Renderer>().sharedMaterial = poleMat;
                Destroy(arm.GetComponent<Collider>());

                // Ampul (Bulb Object)
                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = "Lamp_Bulb";
                bulb.transform.SetParent(lampObj.transform, false);
                bulb.transform.localPosition = new Vector3(0.50f, 3.25f, 0f);
                bulb.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
                bulb.GetComponent<Renderer>().sharedMaterial = bulbDefaultMat;
                Destroy(bulb.GetComponent<Collider>());

                // Gece Yanan Point Light (Sıcak Sarı Sokak Işığı)
                GameObject lightChild = new GameObject("StreetLamp_Light");
                lightChild.transform.SetParent(lampObj.transform, false);
                lightChild.transform.localPosition = new Vector3(0.50f, 3.1f, 0f);

                Light pointLight = lightChild.AddComponent<Light>();
                pointLight.type = LightType.Point;
                pointLight.color = new Color(1.0f, 0.88f, 0.55f); // Sıcak Sarı
                pointLight.intensity = 2.5f;
                pointLight.range = 14.0f;
                pointLight.shadows = LightShadows.None;
                pointLight.enabled = false; // Gündüz Kapalı!

                if (DayNightCycleManager.Instance != null)
                {
                    DayNightCycleManager.Instance.RegisterStreetLamp(bulb, pointLight);
                }
            }

        }

        private void CreateDenseTwoRowTreeBoundaryWall()
        {
            Transform borderGroup = new GameObject("Map_Boundary_Tree_Forest_Wall").transform;
            borderGroup.SetParent(environmentRoot);

            // 1. ÇİM TABANINI AĞAÇ SINIRINA GÖRE TAM DÜZENLE
            GameObject grass = GameObject.Find("Grass_Terrain_Ground");
            if (grass != null)
            {
                grass.transform.position = new Vector3(0f, -0.15f, -2.5f);
                grass.transform.localScale = new Vector3(178.0f, 0.1f, 133.0f);
            }

            // OTOYOL ALTINA UZAK UFUK ÇİM TABAN UZANTILARI (SOL VE SAĞ UZAK YOL VE AĞAÇ BULVARI ALTI)
            GameObject westExtendedGrass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westExtendedGrass.name = "West_Extended_Highway_Grass";
            westExtendedGrass.transform.SetParent(borderGroup);
            westExtendedGrass.transform.position = new Vector3(-144f, -0.15f, -9f);
            westExtendedGrass.transform.localScale = new Vector3(112f, 0.1f, 28f);
            westExtendedGrass.GetComponent<Renderer>().sharedMaterial = grassMat;

            GameObject eastExtendedGrass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastExtendedGrass.name = "East_Extended_Highway_Grass";
            eastExtendedGrass.transform.SetParent(borderGroup);
            eastExtendedGrass.transform.position = new Vector3(144f, -0.15f, -9f);
            eastExtendedGrass.transform.localScale = new Vector3(112f, 0.1f, 28f);
            eastExtendedGrass.GetComponent<Renderer>().sharedMaterial = grassMat;

            float treePitch = 3.8f; // Yan yana iki ağaç arasındaki mesafe

            // BATI SINIRI (Sol taraf: Row 1 at X = -83.5m, Row 2 at X = -87.0m)
            for (float z = -66.0f; z <= 61.0f; z += treePitch)
            {
                // OTOYOL GEÇİDİNDE (Z: -14.5m ile -3.5m arası) KESİNLİKLE AĞAÇ KOYMA! YOLU TEMİZ VE AÇIK BIRAK!
                if (z >= -14.5f && z <= -3.5f)
                {
                    continue;
                }

                CreateBoundaryTree(borderGroup, new Vector3(-83.5f, 0f, z));
                CreateBoundaryTree(borderGroup, new Vector3(-87.0f, 0f, z + (treePitch / 2f)));
            }

            // DOĞU SINIRI (Sağ taraf: Row 1 at X = +83.5m, Row 2 at X = +87.0m)
            for (float z = -66.0f; z <= 61.0f; z += treePitch)
            {
                // OTOYOL GEÇİDİNDE (Z: -14.5m ile -3.5m arası) KESİNLİKLE AĞAÇ KOYMA! YOLU TEMİZ VE AÇIK BIRAK!
                if (z >= -14.5f && z <= -3.5f)
                {
                    continue;
                }

                CreateBoundaryTree(borderGroup, new Vector3(83.5f, 0f, z));
                CreateBoundaryTree(borderGroup, new Vector3(87.0f, 0f, z + (treePitch / 2f)));
            }

            // KUZEY SINIRI (Üst taraf: Row 1 at Z = +58.5m, Row 2 at Z = +62.0m)
            for (float x = -87.0f; x <= 87.0f; x += treePitch)
            {
                CreateBoundaryTree(borderGroup, new Vector3(x, 0f, 58.5f));
                CreateBoundaryTree(borderGroup, new Vector3(x + (treePitch / 2f), 0f, 62.0f));
            }

            // GÜNEY SINIRI (Alt taraf: Row 1 at Z = -63.5m, Row 2 at Z = -67.0m)
            for (float x = -87.0f; x <= 87.0f; x += treePitch)
            {
                CreateBoundaryTree(borderGroup, new Vector3(x, 0f, -63.5f));
                CreateBoundaryTree(borderGroup, new Vector3(x + (treePitch / 2f), 0f, -67.0f));
            }

            // BATI UZATILMIŞ OTOYOL AĞAÇ BULVARI (Sol taraf X: -200m ile -83.5m arası, kaldırımların arkasında 2 sıra ağaç)
            for (float x = -200.0f; x <= -83.5f; x += treePitch)
            {
                // Kuzey kaldırım arkası 2 sıra ağaç
                CreateBoundaryTree(borderGroup, new Vector3(x, 0f, -1.5f));
                CreateBoundaryTree(borderGroup, new Vector3(x + (treePitch / 2f), 0f, 2.0f));

                // Güney kaldırım arkası 2 sıra ağaç
                CreateBoundaryTree(borderGroup, new Vector3(x, 0f, -16.5f));
                CreateBoundaryTree(borderGroup, new Vector3(x + (treePitch / 2f), 0f, -20.0f));
            }

            // DOĞU UZATILMIŞ OTOYOL AĞAÇ BULVARI (Sağ taraf X: +83.5m ile +200m arası, kaldırımların arkasında 2 sıra ağaç)
            for (float x = 83.5f; x <= 200.0f; x += treePitch)
            {
                // Kuzey kaldırım arkası 2 sıra ağaç
                CreateBoundaryTree(borderGroup, new Vector3(x, 0f, -1.5f));
                CreateBoundaryTree(borderGroup, new Vector3(x + (treePitch / 2f), 0f, 2.0f));

                // Güney kaldırım arkası 2 sıra ağaç
                CreateBoundaryTree(borderGroup, new Vector3(x, 0f, -16.5f));
                CreateBoundaryTree(borderGroup, new Vector3(x + (treePitch / 2f), 0f, -20.0f));
            }

            // OYUNCUNUN HARİTA DIŞINA ÇIKMASINI ENGELLEYEN FİZİK ENGELLERİ (GÖRÜNMEZ DUVARLAR)
            // Batı Fizik Duvarları (Otoyol kapısı açık bırakılarak sol/sağ orman sınırı kapatıldı)
            CreateWorldPhysicsBoundaryCollider(borderGroup, "Boundary_Physics_Wall_West_Top", new Vector3(-83.5f, 2.5f, 28.5f), new Vector3(1.0f, 6.0f, 65.0f));
            CreateWorldPhysicsBoundaryCollider(borderGroup, "Boundary_Physics_Wall_West_Bottom", new Vector3(-83.5f, 2.5f, -40.0f), new Vector3(1.0f, 6.0f, 52.0f));
            CreateWorldPhysicsBoundaryCollider(borderGroup, "Boundary_Physics_Wall_West_Highway_End", new Vector3(-92.0f, 2.5f, -9.0f), new Vector3(1.0f, 6.0f, 12.0f));

            // Doğu Fizik Duvarları (Otoyol kapısı açık bırakılarak sol/sağ orman sınırı kapatıldı)
            CreateWorldPhysicsBoundaryCollider(borderGroup, "Boundary_Physics_Wall_East_Top", new Vector3(83.5f, 2.5f, 28.5f), new Vector3(1.0f, 6.0f, 65.0f));
            CreateWorldPhysicsBoundaryCollider(borderGroup, "Boundary_Physics_Wall_East_Bottom", new Vector3(83.5f, 2.5f, -40.0f), new Vector3(1.0f, 6.0f, 52.0f));
            CreateWorldPhysicsBoundaryCollider(borderGroup, "Boundary_Physics_Wall_East_Highway_End", new Vector3(92.0f, 2.5f, -9.0f), new Vector3(1.0f, 6.0f, 12.0f));

            CreateWorldPhysicsBoundaryCollider(borderGroup, "Boundary_Physics_Wall_North", new Vector3(0f, 2.5f, 58.5f), new Vector3(175.0f, 6.0f, 1.0f));
            CreateWorldPhysicsBoundaryCollider(borderGroup, "Boundary_Physics_Wall_South", new Vector3(0f, 2.5f, -63.5f), new Vector3(175.0f, 6.0f, 1.0f));
        }

        private void CreateBoundaryTree(Transform parent, Vector3 pos)
        {
            GameObject tree = new GameObject("Map_Boundary_Tree");
            tree.transform.SetParent(parent);
            tree.transform.position = pos;

            // Gövde
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            trunk.transform.localScale = new Vector3(0.6f, 1.75f, 0.6f);
            trunk.GetComponent<Renderer>().sharedMaterial = treeTrunkMat;

            // Alt Çam Katmanı
            GameObject foliage1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage1.name = "Foliage_Layer_1";
            foliage1.transform.SetParent(tree.transform);
            foliage1.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            foliage1.transform.localScale = new Vector3(3.2f, 2.2f, 3.2f);
            foliage1.GetComponent<Renderer>().sharedMaterial = treeFoliageMat;

            // Üst Çam Katmanı
            GameObject foliage2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage2.name = "Foliage_Layer_2";
            foliage2.transform.SetParent(tree.transform);
            foliage2.transform.localPosition = new Vector3(0f, 4.8f, 0f);
            foliage2.transform.localScale = new Vector3(2.4f, 2.0f, 2.4f);
            foliage2.GetComponent<Renderer>().sharedMaterial = treeFoliageMat;

            // Ağaç Gövde Fiziksel Engel (Fizik Kolayderı)
            CapsuleCollider col = tree.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 2.5f, 0f);
            col.radius = 1.2f;
            col.height = 5.0f;
        }

        private void CreateWorldPhysicsBoundaryCollider(Transform parent, string wallName, Vector3 pos, Vector3 size)
        {
            GameObject wall = new GameObject(wallName);
            wall.transform.SetParent(parent);
            wall.transform.position = pos;

            BoxCollider col = wall.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = false;
        }

        private void CreateFarmFenceAndTreeEnclosure(Transform parent)
        {
            GameObject enclosureGroup = new GameObject("Farm_Fence_And_Tree_Enclosure");
            enclosureGroup.transform.SetParent(parent);

            float minX = 15.0f;
            float maxX = 48.5f;
            float minZ = 0.5f; // Çit iç sınırı Z=0.5m
            float maxZ = 42.0f; // Çit iç sınırı Z=42.0m
            float stepSize = 3.0f;

            // 1. KUZEY (AĞAÇLAR ÇİTİN DIŞINDA Z=43.1m'DE, TOP KALDIRIMDAN 0.9m ÖNCE ÇİMDE DURUR)
            for (float x = minX; x < maxX; x += stepSize)
            {
                float railLen = Mathf.Min(stepSize, maxX - x);
                CreateFencePostAndRail(enclosureGroup.transform, new Vector3(x, 0f, maxZ), true, railLen);
                CreateFarmPerimeterTree(enclosureGroup.transform, new Vector3(x + (railLen / 2f), 0f, maxZ + 1.1f));
            }
            CreateFencePostOnly(enclosureGroup.transform, new Vector3(maxX, 0f, maxZ));

            // 2. DOĞU (AĞAÇLAR ÇİTİN DIŞINDA SAĞ KENAR X=49.7m'DE DURUR)
            for (float z = minZ; z < maxZ; z += stepSize)
            {
                float railLen = Mathf.Min(stepSize, maxZ - z);
                CreateFencePostAndRail(enclosureGroup.transform, new Vector3(maxX, 0f, z), false, railLen);
                CreateFarmPerimeterTree(enclosureGroup.transform, new Vector3(maxX + 1.2f, 0f, z + (railLen / 2f)));
            }
            CreateFencePostOnly(enclosureGroup.transform, new Vector3(maxX, 0f, maxZ));

            // 3. GÜNEY (AĞAÇLAR ÇİTİN DIŞINDA Z=-1.2m'DE DURUR, KALDIRIMA 3.3m UZAKTADIR)
            for (float x = minX; x < maxX; x += stepSize)
            {
                float railLen = Mathf.Min(stepSize, maxX - x);
                CreateFencePostAndRail(enclosureGroup.transform, new Vector3(x, 0f, minZ), true, railLen);
                CreateFarmPerimeterTree(enclosureGroup.transform, new Vector3(x + (railLen / 2f), 0f, -1.2f));
            }
            CreateFencePostOnly(enclosureGroup.transform, new Vector3(maxX, 0f, minZ));

            // 4. BATI (SOL KENAR X=15.0m) (KAMYON YOLUNDA AĞAÇ OLMAMASI İÇİN SADECE ÇİT YER ALIR, YAYA YOLU Z: 0.0m - 4.0m AÇIK BIRAKILIR)
            for (float z = minZ; z < maxZ; z += stepSize)
            {
                if (z >= -0.5f && z <= 4.2f) continue;
                float railLen = Mathf.Min(stepSize, maxZ - z);
                CreateFencePostAndRail(enclosureGroup.transform, new Vector3(minX, 0f, z), false, railLen);
            }

            // 5. YAYA YOLU GİRİŞİNDE ŞIK KEMERLİ ÇİFTLİK KAPISI (FARM ENTRANCE ARCHWAY GATE)
            CreateFarmEntranceArchwayGate(enclosureGroup.transform, new Vector3(15.0f, 0f, 2.0f));
        }

        private void CreateFencePostOnly(Transform parent, Vector3 pos)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Fence_Corner_Post";
            post.transform.SetParent(parent);
            post.transform.position = new Vector3(pos.x, 0.5f, pos.z);
            post.transform.localScale = new Vector3(0.18f, 1.0f, 0.18f);
            post.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;
        }

        private void CreateFarmEntranceArchwayGate(Transform parent, Vector3 centerPos)
        {
            GameObject gateRoot = new GameObject("Farm_Entrance_Archway_Gate");
            gateRoot.transform.SetParent(parent);
            gateRoot.transform.position = centerPos;

            // Sol Kapı Direği
            GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postL.name = "Arch_Post_Left";
            postL.transform.SetParent(gateRoot.transform);
            postL.transform.localPosition = new Vector3(0f, 1.3f, -1.4f);
            postL.transform.localScale = new Vector3(0.35f, 1.3f, 0.35f);
            postL.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            GameObject topBallL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topBallL.name = "Post_Cap_Ball_Left";
            topBallL.transform.SetParent(postL.transform);
            topBallL.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            topBallL.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            topBallL.GetComponent<Renderer>().sharedMaterial = barrierHousingMat;

            // Sağ Kapı Direği
            GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postR.name = "Arch_Post_Right";
            postR.transform.SetParent(gateRoot.transform);
            postR.transform.localPosition = new Vector3(0f, 1.3f, 1.4f);
            postR.transform.localScale = new Vector3(0.35f, 1.3f, 0.35f);
            postR.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            GameObject topBallR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topBallR.name = "Post_Cap_Ball_Right";
            topBallR.transform.SetParent(postR.transform);
            topBallR.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            topBallR.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            topBallR.GetComponent<Renderer>().sharedMaterial = barrierHousingMat;

            // Kemer Üst Header Kirişi (Arch Header Beam)
            GameObject header = GameObject.CreatePrimitive(PrimitiveType.Cube);
            header.name = "Arch_Header_Beam";
            header.transform.SetParent(gateRoot.transform);
            header.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            header.transform.localScale = new Vector3(0.30f, 0.28f, 3.2f);
            header.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            // Kemer Tabela Panosu
            GameObject signBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBoard.name = "Arch_Welcome_Signboard";
            signBoard.transform.SetParent(gateRoot.transform);
            signBoard.transform.localPosition = new Vector3(-0.05f, 2.7f, 0f);
            signBoard.transform.localScale = new Vector3(0.12f, 0.65f, 2.2f);
            signBoard.GetComponent<Renderer>().sharedMaterial = darkWallMat;

            CreateLabel("ÇİFTLİK GİRİŞİ", "FARM GATE", gateRoot.transform, new Vector3(-0.15f, 2.7f, 0f), Color.yellow, 90f);
        }

        private void CreateFarmPerimeterTree(Transform parent, Vector3 pos)
        {
            GameObject tree = new GameObject("Farm_Perimeter_Tree");
            tree.transform.SetParent(parent);
            tree.transform.position = pos;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            trunk.transform.localScale = new Vector3(0.45f, 2.0f, 0.45f);
            trunk.GetComponent<Renderer>().sharedMaterial = treeTrunkMat;

            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Foliage";
            foliage.transform.SetParent(tree.transform);
            foliage.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            foliage.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            foliage.GetComponent<Renderer>().sharedMaterial = treeFoliageMat;
        }

        private void CreateFencePostAndRail(Transform parent, Vector3 pos, bool alignHorizontalX, float railLength)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Fence_Post";
            post.transform.SetParent(parent);
            post.transform.position = new Vector3(pos.x, 0.5f, pos.z);
            post.transform.localScale = new Vector3(0.18f, 1.0f, 0.18f);
            post.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            GameObject rail1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail1.name = "Fence_Rail_Top";
            rail1.transform.SetParent(parent);
            rail1.transform.position = new Vector3(pos.x + (alignHorizontalX ? railLength / 2f : 0f), 0.75f, pos.z + (alignHorizontalX ? 0f : railLength / 2f));
            rail1.transform.localScale = alignHorizontalX ? new Vector3(railLength, 0.10f, 0.08f) : new Vector3(0.08f, 0.10f, railLength);
            rail1.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            GameObject rail2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail2.name = "Fence_Rail_Bottom";
            rail2.transform.SetParent(parent);
            rail2.transform.position = new Vector3(pos.x + (alignHorizontalX ? railLength / 2f : 0f), 0.35f, pos.z + (alignHorizontalX ? 0f : railLength / 2f));
            rail2.transform.localScale = alignHorizontalX ? new Vector3(railLength, 0.10f, 0.08f) : new Vector3(0.08f, 0.10f, railLength);
            rail2.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;
        }

        private void CreateUltraDetailedFarmhouse(Transform parent, Vector3 pos)
        {
            GameObject farmhouse = new GameObject("Farmhouse");
            farmhouse.transform.SetParent(parent);
            farmhouse.transform.position = pos;

            Vector3 size = new Vector3(6.5f, 3.8f, 5.2f);
            float halfX = size.x / 2f;
            float halfZ = size.z / 2f;

            // 1. TEMEL SU BASMAN TAŞI
            GameObject baseboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseboard.name = "Foundation_Plinth";
            baseboard.transform.SetParent(farmhouse.transform);
            baseboard.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            baseboard.transform.localScale = new Vector3(size.x + 0.2f, 0.3f, size.z + 0.2f);
            baseboard.GetComponent<Renderer>().sharedMaterial = windowSillMat;

            // 2. ANA EV DUVARLARI
            GameObject houseBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            houseBody.name = "House_Walls";
            houseBody.transform.SetParent(farmhouse.transform);
            houseBody.transform.localPosition = new Vector3(0f, (size.y / 2f) + 0.3f, 0f);
            houseBody.transform.localScale = size;
            houseBody.GetComponent<Renderer>().sharedMaterial = farmhouseWallMat;

            // 3. KÖŞE AHŞAP DİKME KİRİŞLERİ
            Vector3[] cornerOffsets = new Vector3[] {
                new Vector3(-halfX, (size.y / 2f) + 0.3f, -halfZ),
                new Vector3(halfX, (size.y / 2f) + 0.3f, -halfZ),
                new Vector3(-halfX, (size.y / 2f) + 0.3f, halfZ),
                new Vector3(halfX, (size.y / 2f) + 0.3f, halfZ)
            };
            foreach (Vector3 offset in cornerOffsets)
            {
                GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                corner.name = "Corner_Timber_Post";
                corner.transform.SetParent(farmhouse.transform);
                corner.transform.localPosition = offset;
                corner.transform.localScale = new Vector3(0.25f, size.y + 0.05f, 0.25f);
                corner.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;
            }

            // 4. ÇATI VE BACA
            float roofBaseY = size.y + 0.3f;
            GameObject houseRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            houseRoof.name = "House_Pitched_Roof";
            houseRoof.transform.SetParent(farmhouse.transform);
            houseRoof.transform.localPosition = new Vector3(0f, roofBaseY + 0.95f, 0f);
            houseRoof.transform.localScale = new Vector3(size.x + 0.6f, 1.3f, size.z + 0.6f);
            houseRoof.GetComponent<Renderer>().sharedMaterial = farmhouseRoofMat;

            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.name = "Chimney";
            chimney.transform.SetParent(farmhouse.transform);
            chimney.transform.localPosition = new Vector3(size.x * 0.28f, roofBaseY + 1.7f, size.z * 0.15f);
            chimney.transform.localScale = new Vector3(0.8f, 1.5f, 0.8f);
            chimney.GetComponent<Renderer>().sharedMaterial = chimneyBrickMat;

            GameObject chimneyCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimneyCap.name = "Chimney_Cap";
            chimneyCap.transform.SetParent(farmhouse.transform);
            chimneyCap.transform.localPosition = new Vector3(size.x * 0.28f, roofBaseY + 2.5f, size.z * 0.15f);
            chimneyCap.transform.localScale = new Vector3(0.95f, 0.15f, 0.95f);
            chimneyCap.GetComponent<Renderer>().sharedMaterial = darkWallMat;

            // 5. GİRİŞ VERANDASI / AHŞAP SUNDURMA VE TAŞ YOL
            float doorZ = -halfZ - 0.05f;
            
            // Ahşap Sundurma Direkleri
            GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postL.name = "Porch_Post_Left";
            postL.transform.SetParent(farmhouse.transform);
            postL.transform.localPosition = new Vector3(-1.2f, 1.2f, doorZ - 0.7f);
            postL.transform.localScale = new Vector3(0.18f, 1.2f, 0.18f);
            postL.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postR.name = "Porch_Post_Right";
            postR.transform.SetParent(farmhouse.transform);
            postR.transform.localPosition = new Vector3(1.2f, 1.2f, doorZ - 0.7f);
            postR.transform.localScale = new Vector3(0.18f, 1.2f, 0.18f);
            postR.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            // Sundurma Çatısı
            GameObject porchRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            porchRoof.name = "Porch_Roof_Canopy";
            porchRoof.transform.SetParent(farmhouse.transform);
            porchRoof.transform.localPosition = new Vector3(0f, 2.45f, doorZ - 0.35f);
            porchRoof.transform.localScale = new Vector3(2.8f, 0.15f, 1.0f);
            porchRoof.GetComponent<Renderer>().sharedMaterial = farmhouseRoofMat;

            // Giriş Kapısı Panel & Çerçeve
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "Door_Frame";
            doorFrame.transform.SetParent(farmhouse.transform);
            doorFrame.transform.localPosition = new Vector3(0f, 1.0f, doorZ);
            doorFrame.transform.localScale = new Vector3(1.3f, 2.0f, 0.12f);
            doorFrame.GetComponent<Renderer>().sharedMaterial = doorFrameMat;

            GameObject doorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorPanel.name = "Door_Panel";
            doorPanel.transform.SetParent(farmhouse.transform);
            doorPanel.transform.localPosition = new Vector3(0f, 1.0f, doorZ - 0.02f);
            doorPanel.transform.localScale = new Vector3(1.1f, 1.8f, 0.1f);
            doorPanel.GetComponent<Renderer>().sharedMaterial = woodDoorMat;

            // UFAK TAŞ YOL (STONE PATH AT DOOR ENTRANCE)
            for (int step = 1; step <= 5; step++)
            {
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stone.name = "Entrance_Stepping_Stone_" + step;
                stone.transform.SetParent(farmhouse.transform);
                float xOffset = (step % 2 == 0) ? 0.12f : -0.12f;
                stone.transform.localPosition = new Vector3(xOffset, 0.02f, doorZ - 0.4f - (step * 0.65f));
                stone.transform.localScale = new Vector3(0.65f, 0.03f, 0.65f);
                stone.GetComponent<Renderer>().sharedMaterial = pondStoneMat;
            }

            // 6. ÇİÇEK SAKSILI PENCERELER (WINDOWS WITH FLOWER BOXES & SHUTTERS)
            float winY = (size.y * 0.5f) + 0.3f;
            float winOffsetX = size.x * 0.28f;

            // Ön Pencere Sol & Sağ (Çiçek Saksılı & Panjurlu)
            CreateFarmWindowWithFlowerBox(farmhouse.transform, new Vector3(-winOffsetX, winY, doorZ), new Vector3(1.0f, 1.1f, 0.1f), true);
            CreateFarmWindowWithFlowerBox(farmhouse.transform, new Vector3(winOffsetX, winY, doorZ), new Vector3(1.0f, 1.1f, 0.1f), true);

            // Arka Pencere Sol & Sağ (Çiçek Saksılı)
            float backZ = halfZ + 0.05f;
            CreateFarmWindowWithFlowerBox(farmhouse.transform, new Vector3(-winOffsetX, winY, backZ), new Vector3(1.0f, 1.1f, 0.1f), false);
            CreateFarmWindowWithFlowerBox(farmhouse.transform, new Vector3(winOffsetX, winY, backZ), new Vector3(1.0f, 1.1f, 0.1f), false);

            CreateLabel("ÇİFTLİK EVİ", "FARMHOUSE", farmhouse.transform, new Vector3(0f, size.y + 2.2f, 0f), Color.yellow);
        }

        private void CreateUltraDetailedBarn(Transform parent, Vector3 pos)
        {
            GameObject barn = new GameObject("Farm_Barn");
            barn.transform.SetParent(parent);
            barn.transform.position = pos;

            Vector3 size = new Vector3(7.5f, 4.2f, 5.8f);
            float halfX = size.x / 2f;
            float halfZ = size.z / 2f;

            // 1. AHIR TAŞ TEMELİ
            GameObject baseboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseboard.name = "Barn_Stone_Base";
            baseboard.transform.SetParent(barn.transform);
            baseboard.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            baseboard.transform.localScale = new Vector3(size.x + 0.2f, 0.4f, size.z + 0.2f);
            baseboard.GetComponent<Renderer>().sharedMaterial = pondStoneMat;

            // 2. AHIR KIRMIZI AHŞAP DUVARLARI
            GameObject barnBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barnBody.name = "Barn_Walls";
            barnBody.transform.SetParent(barn.transform);
            barnBody.transform.localPosition = new Vector3(0f, (size.y / 2f) + 0.4f, 0f);
            barnBody.transform.localScale = size;
            barnBody.GetComponent<Renderer>().sharedMaterial = barnWallMat;

            // 3. KÖŞE AHŞAP KİRİŞLERİ
            Vector3[] cornerOffsets = new Vector3[] {
                new Vector3(-halfX, (size.y / 2f) + 0.4f, -halfZ),
                new Vector3(halfX, (size.y / 2f) + 0.4f, -halfZ),
                new Vector3(-halfX, (size.y / 2f) + 0.4f, halfZ),
                new Vector3(halfX, (size.y / 2f) + 0.4f, halfZ)
            };
            foreach (Vector3 offset in cornerOffsets)
            {
                GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                corner.name = "Corner_Timber_Post";
                corner.transform.SetParent(barn.transform);
                corner.transform.localPosition = offset;
                corner.transform.localScale = new Vector3(0.28f, size.y + 0.05f, 0.28f);
                corner.GetComponent<Renderer>().sharedMaterial = windowFrameMat;
            }

            // 4. AHIR ÇATISI VE OT VİNÇ KİRİŞİ (HAYLOFT CRANE BEAM)
            float roofBaseY = size.y + 0.4f;
            GameObject barnRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barnRoof.name = "Barn_Pitched_Roof";
            barnRoof.transform.SetParent(barn.transform);
            barnRoof.transform.localPosition = new Vector3(0f, roofBaseY + 1.05f, 0f);
            barnRoof.transform.localScale = new Vector3(size.x + 0.7f, 1.5f, size.z + 0.7f);
            barnRoof.GetComponent<Renderer>().sharedMaterial = barnRoofMat;

            float doorZ = -halfZ - 0.05f;

            // Ot Vinç Kirişi (Crane Boom)
            GameObject craneBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            craneBeam.name = "Hayloft_Crane_Beam";
            craneBeam.transform.SetParent(barn.transform);
            craneBeam.transform.localPosition = new Vector3(0f, roofBaseY + 1.2f, doorZ - 0.5f);
            craneBeam.transform.localScale = new Vector3(0.18f, 0.18f, 1.2f);
            craneBeam.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            // 5. AHIR ÇİFT KANAT SÜRGÜLÜ KAPISI (LARGE SLIDING BARN DOORS WITH X-BRACE)
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "Barn_Door_Frame";
            doorFrame.transform.SetParent(barn.transform);
            doorFrame.transform.localPosition = new Vector3(0f, 1.4f, doorZ);
            doorFrame.transform.localScale = new Vector3(3.2f, 2.6f, 0.12f);
            doorFrame.GetComponent<Renderer>().sharedMaterial = windowFrameMat;

            GameObject doorL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorL.name = "Barn_Door_Left";
            doorL.transform.SetParent(barn.transform);
            doorL.transform.localPosition = new Vector3(-0.75f, 1.4f, doorZ - 0.02f);
            doorL.transform.localScale = new Vector3(1.4f, 2.4f, 0.1f);
            doorL.GetComponent<Renderer>().sharedMaterial = woodDoorMat;

            GameObject doorR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorR.name = "Barn_Door_Right";
            doorR.transform.SetParent(barn.transform);
            doorR.transform.localPosition = new Vector3(0.75f, 1.4f, doorZ - 0.02f);
            doorR.transform.localScale = new Vector3(1.4f, 2.4f, 0.1f);
            doorR.GetComponent<Renderer>().sharedMaterial = woodDoorMat;

            // Ahır Kapısı X Çapraz Ahşap Çıtaları (X-Braces)
            GameObject braceL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            braceL.name = "X_Brace_Left";
            braceL.transform.SetParent(doorL.transform);
            braceL.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            braceL.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            braceL.transform.localScale = new Vector3(0.12f, 2.5f, 0.05f);
            braceL.GetComponent<Renderer>().sharedMaterial = windowFrameMat;

            GameObject braceR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            braceR.name = "X_Brace_Right";
            braceR.transform.SetParent(doorR.transform);
            braceR.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            braceR.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            braceR.transform.localScale = new Vector3(0.12f, 2.5f, 0.05f);
            braceR.GetComponent<Renderer>().sharedMaterial = windowFrameMat;

            // AHIR GİRİŞ TAŞ YOLU (STONE PATH AT BARN ENTRANCE)
            for (int step = 1; step <= 5; step++)
            {
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stone.name = "Barn_Stepping_Stone_" + step;
                stone.transform.SetParent(barn.transform);
                float xOffset = (step % 2 == 0) ? 0.2f : -0.2f;
                stone.transform.localPosition = new Vector3(xOffset, 0.02f, doorZ - 0.4f - (step * 0.65f));
                stone.transform.localScale = new Vector3(0.85f, 0.03f, 0.85f);
                stone.GetComponent<Renderer>().sharedMaterial = pondStoneMat;
            }

            // 6. GÜMÜŞ/TAŞ SILO KULESİ VE KUPOLA ŞAPKASI (GRAIN SILO TOWER)
            GameObject silo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            silo.name = "Silo_Tower";
            silo.transform.SetParent(barn.transform);
            silo.transform.localPosition = new Vector3(4.8f, 3.5f, -0.8f);
            silo.transform.localScale = new Vector3(2.4f, 3.5f, 2.4f);
            silo.GetComponent<Renderer>().sharedMaterial = sidewalkMat;

            GameObject siloDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            siloDome.name = "Silo_Dome_Cap";
            siloDome.transform.SetParent(silo.transform);
            siloDome.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            siloDome.transform.localScale = new Vector3(1.05f, 0.8f, 1.05f);
            siloDome.GetComponent<Renderer>().sharedMaterial = doorFrameMat;

            // 7. AHIR YAN VE ARKA PENCERELERİ (ÇİÇEK SAKSILARI İLE)
            float winY = (size.y * 0.55f) + 0.4f;
            float winOffsetX = size.x * 0.30f;
            CreateFarmWindowWithFlowerBox(barn.transform, new Vector3(-winOffsetX, winY, doorZ), new Vector3(0.9f, 0.9f, 0.1f), true);
            CreateFarmWindowWithFlowerBox(barn.transform, new Vector3(winOffsetX, winY, doorZ), new Vector3(0.9f, 0.9f, 0.1f), true);

            float backZ = halfZ + 0.05f;
            CreateFarmWindowWithFlowerBox(barn.transform, new Vector3(-winOffsetX, winY, backZ), new Vector3(0.9f, 0.9f, 0.1f), false);
            CreateFarmWindowWithFlowerBox(barn.transform, new Vector3(winOffsetX, winY, backZ), new Vector3(0.9f, 0.9f, 0.1f), false);

            // GİRİŞ KAPI YANINDA SAMAN BALYALARI VE AHŞAP FISTIK FIRÇALARI (HAY BALES & BARRELS)
            GameObject hayBale1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hayBale1.name = "Golden_Hay_Bale_1";
            hayBale1.transform.SetParent(barn.transform);
            hayBale1.transform.localPosition = new Vector3(-2.4f, 0.35f, doorZ - 0.6f);
            hayBale1.transform.localScale = new Vector3(0.9f, 0.6f, 0.6f);
            hayBale1.GetComponent<Renderer>().sharedMaterial = wheatCropMat;

            GameObject hayBale2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hayBale2.name = "Golden_Hay_Bale_2";
            hayBale2.transform.SetParent(barn.transform);
            hayBale2.transform.localPosition = new Vector3(-2.2f, 0.9f, doorZ - 0.6f);
            hayBale2.transform.localScale = new Vector3(0.85f, 0.55f, 0.55f);
            hayBale2.GetComponent<Renderer>().sharedMaterial = wheatCropMat;

            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Wooden_Storage_Barrel";
            barrel.transform.SetParent(barn.transform);
            barrel.transform.localPosition = new Vector3(2.4f, 0.45f, doorZ - 0.6f);
            barrel.transform.localScale = new Vector3(0.6f, 0.45f, 0.6f);
            barrel.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            barn.AddComponent<BarnController>();
            CreateLabel("AHIR", "BARN", barn.transform, new Vector3(0f, size.y + 2.4f, 0f), Color.red);
        }

        private void CreateFarmWindowWithFlowerBox(Transform parent, Vector3 localPos, Vector3 winSize, bool faceNegativeZ)
        {
            GameObject winGroup = new GameObject("Farm_Window_With_FlowerBox");
            winGroup.transform.SetParent(parent);
            winGroup.transform.localPosition = localPos;

            float zSign = faceNegativeZ ? -1.0f : 1.0f;

            // Çerçeve
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Window_Frame";
            frame.transform.SetParent(winGroup.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(winSize.x + 0.15f, winSize.y + 0.15f, 0.10f);
            frame.GetComponent<Renderer>().sharedMaterial = windowFrameMat;

            // Cam Pane
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Window_Glass";
            glass.transform.SetParent(winGroup.transform);
            glass.transform.localPosition = new Vector3(0f, 0f, zSign * 0.02f);
            glass.transform.localScale = winSize;
            glass.GetComponent<Renderer>().sharedMaterial = windowGlassMat;

            // Denizlik
            GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "Window_Sill";
            sill.transform.SetParent(winGroup.transform);
            sill.transform.localPosition = new Vector3(0f, -winSize.y / 2f - 0.08f, zSign * 0.10f);
            sill.transform.localScale = new Vector3(winSize.x + 0.3f, 0.12f, 0.22f);
            sill.GetComponent<Renderer>().sharedMaterial = windowSillMat;

            // AHŞAP ÇİÇEK SAKSI KUTUSU (FLOWER POT BOX UNDER WINDOW)
            GameObject flowerBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flowerBox.name = "Window_Flower_Box";
            flowerBox.transform.SetParent(winGroup.transform);
            flowerBox.transform.localPosition = new Vector3(0f, -winSize.y / 2f - 0.22f, zSign * 0.15f);
            flowerBox.transform.localScale = new Vector3(winSize.x + 0.1f, 0.25f, 0.28f);
            flowerBox.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            // Saksı İçi Kırmızı & Sarı Çiçek Açan Bitkiler
            for (float fx = -winSize.x / 2.5f; fx <= winSize.x / 2.5f; fx += 0.25f)
            {
                GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flower.name = "Flower_Blossom";
                flower.transform.SetParent(flowerBox.transform);
                flower.transform.localPosition = new Vector3(fx / (winSize.x + 0.1f), 0.6f, 0f);
                flower.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                flower.GetComponent<Renderer>().sharedMaterial = (Mathf.Abs(fx) < 0.2f) ? flowerYellowMat : flowerRedMat;
            }

            // Ahşap Panjurlar (Shutters)
            GameObject shutterL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shutterL.name = "Shutter_Left";
            shutterL.transform.SetParent(winGroup.transform);
            shutterL.transform.localPosition = new Vector3(-winSize.x / 2f - 0.20f, 0f, zSign * 0.04f);
            shutterL.transform.localScale = new Vector3(0.28f, winSize.y + 0.1f, 0.08f);
            shutterL.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;

            GameObject shutterR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shutterR.name = "Shutter_Right";
            shutterR.transform.SetParent(winGroup.transform);
            shutterR.transform.localPosition = new Vector3(winSize.x / 2f + 0.20f, 0f, zSign * 0.04f);
            shutterR.transform.localScale = new Vector3(0.28f, winSize.y + 0.1f, 0.08f);
            shutterR.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;
        }

        public Color CurrentWallColor { get; private set; } = new Color(0.12f, 0.14f, 0.17f);
        public Color CurrentFloorColor { get; private set; } = new Color(0.85f, 0.72f, 0.53f);

        public void ApplyWallColor(Color c)
        {
            CurrentWallColor = c;
            if (darkWallMat != null)
            {
                darkWallMat.color = c;
                if (darkWallMat.HasProperty("_BaseColor")) darkWallMat.SetColor("_BaseColor", c);
            }
            GameObject bObj = GameObject.Find("Building_Complex");
            if (bObj != null)
            {
                Renderer[] renderers = bObj.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r == null || r.gameObject == null) continue;
                    string n = r.gameObject.name;
                    if (n.Contains("Wall") || n.Contains("Partition"))
                    {
                        if (r.material != null)
                        {
                            r.material.color = c;
                            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c);
                        }
                    }
                }
            }
        }

        public void ApplyFloorStyle(Color c)
        {
            CurrentFloorColor = c;
            if (storeFloorMat != null)
            {
                storeFloorMat.color = c;
                if (storeFloorMat.HasProperty("_BaseColor")) storeFloorMat.SetColor("_BaseColor", c);
            }
            GameObject bObj = GameObject.Find("Building_Complex");
            if (bObj != null)
            {
                Renderer[] renderers = bObj.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r == null || r.gameObject == null) continue;
                    string n = r.gameObject.name;
                    if (n.Contains("Floor") || n.Contains("Store_Floor"))
                    {
                        if (r.material != null)
                        {
                            r.material.color = c;
                            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c);
                        }
                    }
                }
            }
        }
    }
}
