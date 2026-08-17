using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// UGUI tabanlı en üst katman (sortingOrder = 32000) iOS / TestFlight teşhis paneli.
    /// OnGUI yerine yerel UGUI Canvas kullanarak iPhone ekranında en üst sırada
    /// intro akışını, timeScale durumunu, exception hatalarını ve event geçmişini basar.
    /// Touch ve Raycast girdilerini kesinlikle engellemez (GraphicRaycaster yoktur, blocksRaycasts = false).
    /// </summary>
    public class iOSIntroDiagnosticOverlay : MonoBehaviour
    {
        public static iOSIntroDiagnosticOverlay Instance { get; private set; }

        private string currentStage = "[DIAGNOSTIC] Runtime overlay created";
        private string lastError = "None";
        private List<string> eventHistory = new List<string>();
        private const int MaxHistory = 7;

        private Text diagnosticText;
        private Canvas overlayCanvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("[iOS_UGUI_Diagnostic_Overlay]");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<iOSIntroDiagnosticOverlay>();
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Application.logMessageReceived += HandleLog;
            CreateUGUIPanel();
            Debug.LogError("[DIAGNOSTIC] UGUI OVERLAY CREATED");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                lastError = $"[{type}] {logString}";
            }

            if (logString.StartsWith("[INTRO]") || logString.StartsWith("[MAIN_MENU]") || logString.StartsWith("[DIAGNOSTIC]"))
            {
                currentStage = logString;
                AddEvent(logString);
            }
        }

        private void AddEvent(string evt)
        {
            if (eventHistory.Count >= MaxHistory)
            {
                eventHistory.RemoveAt(0);
            }
            eventHistory.Add($"{DateTime.Now:HH:mm:ss} - {evt}");
        }

        private void CreateUGUIPanel()
        {
            // En Üst Katman UGUI Canvas (sortingOrder = 32000)
            overlayCanvas = gameObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 32000;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Dokunmaları kesinlikle engellemesin!
            CanvasGroup cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Siyah Yarı Şeffaf Arka Plan Paneli (Sol Üst)
            GameObject panelObj = new GameObject("DiagnosticPanel");
            panelObj.transform.SetParent(transform, false);

            RectTransform rtPanel = panelObj.AddComponent<RectTransform>();
            rtPanel.anchorMin = new Vector2(0f, 1f);
            rtPanel.anchorMax = new Vector2(0f, 1f);
            rtPanel.pivot = new Vector2(0f, 1f);
            rtPanel.anchoredPosition = new Vector2(10f, -10f);
            rtPanel.sizeDelta = new Vector2(550f, 380f);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.02f, 0.04f, 0.08f, 0.88f);
            panelBg.raycastTarget = false;

            // Metin Alanı
            GameObject textObj = new GameObject("DiagnosticText");
            textObj.transform.SetParent(panelObj.transform, false);

            RectTransform rtText = textObj.AddComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.sizeDelta = new Vector2(-16f, -16f);
            rtText.anchoredPosition = Vector2.zero;

            diagnosticText = textObj.AddComponent<Text>();
            diagnosticText.font = GetSafeFont();
            diagnosticText.fontSize = 13;
            diagnosticText.color = Color.white;
            diagnosticText.alignment = TextAnchor.UpperLeft;
            diagnosticText.horizontalOverflow = HorizontalWrapMode.Wrap;
            diagnosticText.verticalOverflow = VerticalWrapMode.Truncate;
            diagnosticText.raycastTarget = false;

            UpdateDisplayText();
        }

        private void Update()
        {
            UpdateDisplayText();
        }

        private void UpdateDisplayText()
        {
            if (diagnosticText == null) return;

            bool hasIntroFin = EKTReklamIntroManager.HasIntroFinished;
            bool introInstOK = EKTReklamIntroManager.Instance != null;
            bool mainInstOK = MainMenuUI.Instance != null;

            GameObject introCanvasObj = GameObject.Find("EKT_Reklam_Intro_Canvas");
            GameObject blackCurtainObj = GameObject.Find("[EKT_Intro_BlackCurtain]");

            Canvas[] activeCanvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
            string canvasListStr = "";
            int activeCanvasCount = 0;
            if (activeCanvases != null)
            {
                foreach (var c in activeCanvases)
                {
                    if (c != null && c.gameObject.activeInHierarchy)
                    {
                        if (activeCanvasCount < 4)
                        {
                            canvasListStr += $"{c.gameObject.name}(so:{c.sortingOrder}), ";
                        }
                        activeCanvasCount++;
                    }
                }
            }

            string historyStr = "";
            for (int i = eventHistory.Count - 1; i >= 0; i--)
            {
                historyStr += $"\n • {eventHistory[i]}";
            }

            diagnosticText.text =
                $"<b><color=#FFD700>=== FARM2SHELF DIAGNOSTIC ACTIVE ===</color></b>\n" +
                $"Stage: <color=#00FFFF>{currentStage}</color>\n" +
                $"Frame: {Time.frameCount} | Time.timeScale: {Time.timeScale:F1} | Unscaled: {Time.unscaledTime:F1}s\n" +
                $"HasIntroFinished: <b>{(hasIntroFin ? "<color=#00FF00>TRUE</color>" : "<color=#FF4500>FALSE</color>")}</b>\n" +
                $"Intro Inst: {(introInstOK ? "OK" : "NULL")} | MainMenu Inst: {(mainInstOK ? "OK" : "NULL")}\n" +
                $"Intro Canvas: {(introCanvasObj != null ? "<color=#FF4500>ALIVE</color>" : "NULL")} | Black Curtain: {(blackCurtainObj != null ? "<color=#FF4500>ALIVE</color>" : "NULL")}\n" +
                $"Canvases ({activeCanvasCount}): {(string.IsNullOrEmpty(canvasListStr) ? "None" : canvasListStr.TrimEnd(',', ' '))}\n" +
                $"<color=#FF6347>Last Error: {lastError}</color>\n" +
                $"<b>History (Last {eventHistory.Count}):</b>{historyStr}";
        }

        private Font GetSafeFont()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (font != null) return font;

            try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            if (font != null) return font;

            try { font = Font.CreateDynamicFontFromOSFont("Arial", 14); } catch { }
            if (font != null) return font;

            try
            {
                Text[] sceneTexts = UnityEngine.Object.FindObjectsOfType<Text>(true);
                if (sceneTexts != null && sceneTexts.Length > 0)
                {
                    foreach (var st in sceneTexts)
                    {
                        if (st != null && st.font != null) return st.font;
                    }
                }
            }
            catch { }

            return font;
        }
    }
}
