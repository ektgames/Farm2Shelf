using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.Environment;
using Farm2Shelf.CameraSystem;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

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
            try
            {
                StartupStatus.SetStage("Render pipeline");
                LightingPipelineBinder.Apply();
                ConfigureMobilePerformance();
                ShaderHelper.GetLitShader();
                ShaderHelper.GetUnlitShader();
                Farm2Shelf.Utils.PrimitiveFactory.Warmup();
                if (autoBuildOnStart)
                {
                    InitializeGameWorld();
                }
            }
            catch (System.Exception exception)
            {
                StartupStatus.Fail(exception);
            }
        }

        private static void ConfigureMobilePerformance()
        {
#if UNITY_IOS || UNITY_ANDROID
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
#endif
        }

        public static void InitializeGameWorld()
        {
            StartupStatus.SetStage("Input and core managers");
            EnsureEventSystem();

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

            if (managersObj.GetComponent<StoreAnnouncementManager>() == null)
                managersObj.AddComponent<StoreAnnouncementManager>();

            if (managersObj.GetComponent<EconomyManager>() == null)
                managersObj.AddComponent<EconomyManager>();

            if (managersObj.GetComponent<StoreStatusManager>() == null)
                managersObj.AddComponent<StoreStatusManager>();

            if (managersObj.GetComponent<StaffManager>() == null)
                managersObj.AddComponent<StaffManager>();

            if (managersObj.GetComponent<CourierManager>() == null && FindFirstObjectByType<CourierManager>() == null)
            {
                managersObj.AddComponent<CourierManager>();
            }

            if (managersObj.GetComponent<TaxiFleetManager>() == null && FindFirstObjectByType<TaxiFleetManager>() == null)
            {
                managersObj.AddComponent<TaxiFleetManager>();
            }

            if (FindFirstObjectByType<TownContractManager>() == null)
            {
                GameObject contractGo = new GameObject("TownContractManager");
                contractGo.AddComponent<TownContractManager>();
            }

            if (managersObj.GetComponent<SeasonalInspectorManager>() == null)
                managersObj.AddComponent<SeasonalInspectorManager>();

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

            if (managersObj.GetComponent<LivestockManager>() == null)
                managersObj.AddComponent<LivestockManager>();

            if (managersObj.GetComponent<DailySpinWheelManager>() == null)
                managersObj.AddComponent<DailySpinWheelManager>();

            if (managersObj.GetComponent<TutorialManager>() == null)
                managersObj.AddComponent<TutorialManager>();

            if (managersObj.GetComponent<WorkshopManager>() == null)
                managersObj.AddComponent<WorkshopManager>();

            if (managersObj.GetComponent<Farm2Shelf.Environment.WorkshopBuilder>() == null)
                managersObj.AddComponent<Farm2Shelf.Environment.WorkshopBuilder>();

            if (managersObj.GetComponent<WorkshopPalletManager>() == null)
                managersObj.AddComponent<WorkshopPalletManager>();

            if (managersObj.GetComponent<Farm2Shelf.UI.WorkshopPalletModalUI>() == null)
                managersObj.AddComponent<Farm2Shelf.UI.WorkshopPalletModalUI>();

            if (managersObj.GetComponent<Farm2Shelf.UI.LivestockCoopModalUI>() == null)
                managersObj.AddComponent<Farm2Shelf.UI.LivestockCoopModalUI>();

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

            StartupStatus.SetStage("World generation");
            builder.BuildEnvironment();

            // 3. Kamera Kurulumu
            StartupStatus.SetStage("Isometric camera");
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
            LightingPipelineBinder.ConfigureMainCamera(mainCam);

            if (StoreQualityManager.Instance == null)
            {
                GameObject sqObj = new GameObject("StoreQualityManager");
                sqObj.AddComponent<StoreQualityManager>();
            }

            if (Farm2Shelf.Environment.WeatherManager.Instance == null)
            {
                GameObject wmObj = new GameObject("WeatherManager");
                wmObj.AddComponent<Farm2Shelf.Environment.WeatherManager>();
            }

            // 4. Sol Üst HUD UI Arayüzü Başlatma
            StartupStatus.SetStage("Menu and HUD");
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

            if (uiManagerObj.GetComponent<GameOverUI>() == null)
                uiManagerObj.AddComponent<GameOverUI>();

            if (uiManagerObj.GetComponent<SaveLoadSlotModalUI>() == null)
                uiManagerObj.AddComponent<SaveLoadSlotModalUI>();

            if (uiManagerObj.GetComponent<SettingsModalUI>() == null)
                uiManagerObj.AddComponent<SettingsModalUI>();

            if (uiManagerObj.GetComponent<HowToPlayModalUI>() == null)
                uiManagerObj.AddComponent<HowToPlayModalUI>();

            if (uiManagerObj.GetComponent<DailySpinWheelUI>() == null)
                uiManagerObj.AddComponent<DailySpinWheelUI>();
            StartupStatus.SetStage("Intro and menu activation");
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
#if ENABLE_INPUT_SYSTEM
                if (EventSystem.current.GetComponent<InputSystemUIInputModule>() == null)
                {
                    EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
                }
#endif
                Farm2Shelf.Utils.UiReliableClick.ConfigureSharedEventSystem();
                return;
            }

            GameObject esObj = new GameObject("EventSystem");
            Object.DontDestroyOnLoad(esObj);
            esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<InputSystemUIInputModule>();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
            Farm2Shelf.Utils.UiReliableClick.ConfigureSharedEventSystem();
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
    // A startup failure must never leave a player staring at the default camera
    // with no indication of what failed. This stops observing after the menu opens.
    public sealed class StartupStatus : MonoBehaviour
    {
        private static StartupStatus instance;
        private static string stage = "Before scene load";
        private string failure;
        private string details;
        private float began;
        private bool complete;
        private Vector2 scroll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Begin()
        {
            stage = "Before scene load";
            var host = new GameObject("Startup_Status_Canvas");
            instance = host.AddComponent<StartupStatus>();
            DontDestroyOnLoad(host);
            instance.began = Time.realtimeSinceStartup;
            Application.logMessageReceived += instance.Observe;
        }

        public static void SetStage(string value)
        {
            stage = value;
            Debug.Log("[STARTUP] " + value);
        }

        public static void Fail(System.Exception exception)
        {
            Debug.LogException(exception);
            if (instance != null) instance.SaveFailure(exception.GetType().Name + ": " + exception.Message, exception.ToString());
            GameSimulationTime.SetPaused(GameSimulationTime.PauseReason.MainMenu, true);
        }

        private void Observe(string message, string trace, LogType type)
        {
            if (!complete && type == LogType.Exception && failure == null) SaveFailure(message, trace);
        }

        private void SaveFailure(string message, string trace)
        {
            if (failure != null) return;
            failure = stage + "\n" + message;
            details = "Farm2Shelf " + Application.version + " | " + Application.buildGUID + "\n" +
                SystemInfo.operatingSystem + " | " + SystemInfo.deviceModel + "\n" + failure + "\n" + trace;
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "startup-error.txt"), details); }
            catch (System.Exception) { /* The on-screen report still works without disk access. */ }
        }

        private void Update()
        {
            if (complete) return;
            if (MainMenuUI.IsMenuVisible && EKTReklamIntroManager.HasIntroFinished)
            {
                complete = true;
                Application.logMessageReceived -= Observe;
            }
        }

        private void OnDestroy() { Application.logMessageReceived -= Observe; }

        private void OnGUI()
        {
            if (complete || (failure == null && Time.realtimeSinceStartup - began < 20f)) return;
            Rect safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
            float scale = Mathf.Max(.5f, Mathf.Min(safe.width / 860f, safe.height / 420f));
            Matrix4x4 saved = GUI.matrix;
            int savedDepth = GUI.depth;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0), Quaternion.identity, Vector3.one * scale);
            float width = safe.width / scale, height = safe.height / scale;
            var label = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
            label.normal.textColor = Color.white;
            GUI.depth = -32000;
            GUI.Box(new Rect(8, 8, width - 16, height - 16), GUIContent.none);
            GUI.Label(new Rect(28, 20, width - 56, 60), "Başlatma tamamlanamadı / Startup incomplete", label);
            scroll = GUI.BeginScrollView(new Rect(28, 80, width - 56, height - 165), scroll,
                new Rect(0, 0, width - 84, Mathf.Max(height - 165, label.CalcHeight(new GUIContent(failure ?? stage), width - 84))));
            GUI.Label(new Rect(0, 0, width - 84, Mathf.Max(500, label.CalcHeight(new GUIContent(failure ?? stage), width - 84))), failure ?? stage, label);
            GUI.EndScrollView();
            var button = new GUIStyle(GUI.skin.button) { fontSize = 20 };
            if (GUI.Button(new Rect(28, height - 68, width - 56, 48), "Hata bilgisini kopyala / Copy error", button))
                GUIUtility.systemCopyBuffer = details ?? ("Farm2Shelf " + Application.version + " | " + stage);
            GUI.matrix = saved;
            GUI.depth = savedDepth;
        }
    }

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
