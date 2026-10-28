using System;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    // Own script asset and native UGUI canvas: available before world generation,
    // without relying on IMGUI or on the main menu being constructed successfully.
    public sealed class StartupStatus : MonoBehaviour
    {
        private static StartupStatus instance;
        private static string stage = "Starting";
        private Text title, status;
        private RectTransform safe;
        private GameObject panel;
        private string failure, details;
        private bool complete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Begin() { EnsureVisible(); }

        public static void EnsureVisible()
        {
            if (instance != null) return;
            var host = new GameObject("Startup_Status_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(host);
            instance = host.AddComponent<StartupStatus>();
            instance.CreateUI();
            Application.logMessageReceived += instance.Observe;
        }

        private void CreateUI()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32700;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            panel = new GameObject("Loading", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.045f, .065f, .09f, 1);
            var content = new GameObject("SafeArea", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            safe = (RectTransform)content.transform;
            title = Label("FARM2SHELF", new Vector2(.06f, .68f), new Vector2(.94f, .92f), 34);
            status = Label("Yükleniyor / Loading", new Vector2(.06f, .23f), new Vector2(.94f, .68f), 22);
            var copy = new GameObject("CopyError", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            copy.transform.SetParent(safe, false);
            var copyRect = (RectTransform)copy.transform;
            copyRect.anchorMin = new Vector2(.1f, .05f); copyRect.anchorMax = new Vector2(.9f, .2f);
            copyRect.offsetMin = copyRect.offsetMax = Vector2.zero;
            copy.GetComponent<Image>().color = new Color(.13f, .32f, .38f);
            copy.GetComponent<Button>().onClick.AddListener(() => GUIUtility.systemCopyBuffer = details ?? stage);
            var caption = Label("Hata bilgisini kopyala / Copy error", Vector2.zero, Vector2.one, 20);
            caption.transform.SetParent(copy.transform, false);
            copy.SetActive(false);
            UpdateDisplay();
        }

        private Text Label(string text, Vector2 min, Vector2 max, int size)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(safe, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.fontSize = size; label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 14; label.resizeTextMaxSize = size;
            return label;
        }

        public static void SetStage(string value)
        {
            stage = value;
            Debug.Log("[STARTUP] " + value);
            if (instance != null && !instance.complete) instance.UpdateDisplay();
        }

        public static void Fail(Exception exception)
        {
            Debug.LogException(exception);
            if (instance != null) instance.SaveFailure(exception.Message, exception.ToString());
            GameSimulationTime.SetPaused(GameSimulationTime.PauseReason.Intro, true);
        }

        private void Observe(string message, string trace, LogType type)
        {
            if (!complete && type == LogType.Exception) SaveFailure(message, trace);
        }

        private void SaveFailure(string message, string trace)
        {
            if (failure != null) return;
            failure = stage + "\n" + message;
            details = "Farm2Shelf " + Application.version + " | " + Application.buildGUID + "\n" +
                SystemInfo.operatingSystem + " | " + SystemInfo.deviceModel + "\n" + failure + "\n" + trace;
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "startup-error.txt"), details); }
            catch (Exception) { }
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (status == null) return;
            title.text = failure == null ? "FARM2SHELF" : "Başlatma tamamlanamadı / Startup incomplete";
            status.text = failure ?? ("Yükleniyor / Loading\n" + stage);
            safe.Find("CopyError").gameObject.SetActive(failure != null);
        }

        private void Update()
        {
            if (complete) return;
            Rect area = Screen.safeArea;
            if (Screen.width > 0 && Screen.height > 0 && area.width > 0 && area.height > 0)
            {
                safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
                safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
                safe.offsetMin = safe.offsetMax = Vector2.zero;
            }
            // Let the existing intro take over only after successful world setup.
            if (failure == null && EKTReklamIntroManager.IsIntroVisible) panel.SetActive(false);
            if (failure != null) panel.SetActive(true);
            if (MainMenuUI.IsMenuVisible && EKTReklamIntroManager.HasIntroFinished && failure == null)
            {
                complete = true;
                Application.logMessageReceived -= Observe;
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy() { Application.logMessageReceived -= Observe; if (instance == this) instance = null; }
    }
}
