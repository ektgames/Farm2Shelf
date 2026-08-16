using UnityEngine;
using Farm2Shelf.Environment;
using Farm2Shelf.CameraSystem;
using Farm2Shelf.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Farm2Shelf Başlatıcı (Bootstrapper).
    /// Kullanıcı Unity editöründe sadece Play'e bastığında 3D izometrik çevreyi,
    /// kamera kurulumunu, saat yöneticisini, ekonomi yöneticisini ve şeffaf HUD arayüzünü otomatik başlatır.
    /// </summary>
    public class Farm2ShelfBootstrapper : MonoBehaviour
    {
        [Header("Otomatik Kurulum")]
        [SerializeField] private bool autoBuildOnStart = true;

        private void Awake()
        {
            if (autoBuildOnStart)
            {
                InitializeGameWorld();
            }
        }

        public static void InitializeGameWorld()
        {
            // 1. Core Manager'ları Başlatma (Time, Economy, StoreStatus)
            GameObject managersObj = GameObject.Find("Core_Managers");
            if (managersObj == null)
            {
                managersObj = new GameObject("Core_Managers");
            }

            if (managersObj.GetComponent<TimeManager>() == null)
                managersObj.AddComponent<TimeManager>();

            if (managersObj.GetComponent<AudioManager>() == null)
                managersObj.AddComponent<AudioManager>();

            if (managersObj.GetComponent<EconomyManager>() == null)
                managersObj.AddComponent<EconomyManager>();

            if (managersObj.GetComponent<StoreStatusManager>() == null)
                managersObj.AddComponent<StoreStatusManager>();

            if (managersObj.GetComponent<StaffManager>() == null)
                managersObj.AddComponent<StaffManager>();

            if (managersObj.GetComponent<FinanceManager>() == null)
                managersObj.AddComponent<FinanceManager>();

            if (managersObj.GetComponent<BankLoanManager>() == null)
                managersObj.AddComponent<BankLoanManager>();

            if (managersObj.GetComponent<StockMarketManager>() == null)
                managersObj.AddComponent<StockMarketManager>();

            if (managersObj.GetComponent<EndOfDayReportModalUI>() == null)
                managersObj.AddComponent<EndOfDayReportModalUI>();

            if (managersObj.GetComponent<FurnitureDeliveryManager>() == null)
                managersObj.AddComponent<FurnitureDeliveryManager>();

            if (managersObj.GetComponent<FurniturePlacementManager>() == null)
                managersObj.AddComponent<FurniturePlacementManager>();

            if (managersObj.GetComponent<WholesaleTruckManager>() == null)
                managersObj.AddComponent<WholesaleTruckManager>();

            if (managersObj.GetComponent<CityTrafficManager>() == null)
                managersObj.AddComponent<CityTrafficManager>();

            if (managersObj.GetComponent<CustomerShoppingManager>() == null)
                managersObj.AddComponent<CustomerShoppingManager>();

            if (managersObj.GetComponent<StaffVisualManager>() == null)
                managersObj.AddComponent<StaffVisualManager>();

            if (managersObj.GetComponent<StoreCleanlinessManager>() == null)
                managersObj.AddComponent<StoreCleanlinessManager>();

            if (managersObj.GetComponent<StaffTaskController>() == null)
                managersObj.AddComponent<StaffTaskController>();

            if (managersObj.GetComponent<CityBusManager>() == null)
                managersObj.AddComponent<CityBusManager>();

            if (managersObj.GetComponent<DayNightCycleManager>() == null)
                managersObj.AddComponent<DayNightCycleManager>();

            if (managersObj.GetComponent<SaveSystemManager>() == null)
                managersObj.AddComponent<SaveSystemManager>();

            // 2. Çevre Oluşturucu
            GameObject envManager = GameObject.Find("EnvironmentManager");
            if (envManager == null)
            {
                envManager = new GameObject("EnvironmentManager");
            }

            EnvironmentBuilder builder = envManager.GetComponent<EnvironmentBuilder>();
            if (builder == null)
            {
                builder = envManager.AddComponent<EnvironmentBuilder>();
            }

            builder.BuildEnvironment();

            // 3. Kamera Kurulumu
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
            }

            IsometricCameraSetup camSetup = mainCam.GetComponent<IsometricCameraSetup>();
            if (camSetup == null)
            {
                camSetup = mainCam.gameObject.AddComponent<IsometricCameraSetup>();
            }

            camSetup.FocusOn(new Vector3(0f, 0f, 1f));

            // 4. Sol Üst HUD UI Arayüzü Başlatma
            GameObject uiManagerObj = GameObject.Find("UI_Manager");
            if (uiManagerObj == null)
            {
                uiManagerObj = new GameObject("UI_Manager");
            }

            if (uiManagerObj.GetComponent<GameHUDManager>() == null)
                uiManagerObj.AddComponent<GameHUDManager>();

            if (uiManagerObj.GetComponent<FurnitureInfoModalUI>() == null)
                uiManagerObj.AddComponent<FurnitureInfoModalUI>();

            if (uiManagerObj.GetComponent<StaffProfileModalUI>() == null)
                uiManagerObj.AddComponent<StaffProfileModalUI>();

            if (uiManagerObj.GetComponent<EKTReklamIntroManager>() == null)
                uiManagerObj.AddComponent<EKTReklamIntroManager>();

            if (uiManagerObj.GetComponent<MainMenuUI>() == null)
                uiManagerObj.AddComponent<MainMenuUI>();

            if (uiManagerObj.GetComponent<PauseMenuUI>() == null)
                uiManagerObj.AddComponent<PauseMenuUI>();

            if (uiManagerObj.GetComponent<SaveLoadSlotModalUI>() == null)
                uiManagerObj.AddComponent<SaveLoadSlotModalUI>();

            if (uiManagerObj.GetComponent<SettingsModalUI>() == null)
                uiManagerObj.AddComponent<SettingsModalUI>();

            if (uiManagerObj.GetComponent<HowToPlayModalUI>() == null)
                uiManagerObj.AddComponent<HowToPlayModalUI>();
        }

        #if UNITY_EDITOR
        [MenuItem("Farm2Shelf/Generate Environment & UI")]
        public static void GenerateEnvironmentFromMenu()
        {
            InitializeGameWorld();
            Debug.Log("[Farm2Shelf] Editör menüsünden harita ve HUD arayüz kurulumu tamamlandı!");
        }
        #endif
    }

    /// <summary>
    /// Sahne başlatıldığında otomatik olarak Bootstrapper bileşenini ekleyen runtime initializer.
    /// </summary>
    public static class AutoBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            if (Object.FindFirstObjectByType<Farm2ShelfBootstrapper>() == null)
            {
                GameObject bootstrapper = new GameObject("[Farm2ShelfBootstrapper]");
                bootstrapper.AddComponent<Farm2ShelfBootstrapper>();
            }
        }
    }
}
