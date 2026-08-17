using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// iOS / TestFlight Intro ve UI Akışı Teşhis Ekranı (Diagnostic Overlay).
    /// IMGUI (OnGUI) kullanarak ekranda en üst katmanda bağımsız bir panelle
    /// intro aşamalarını, aktif canvas'ları, istisnaları (exception) ve zaman durumunu gösterir.
    /// </summary>
    public class iOSIntroDiagnosticOverlay : MonoBehaviour
    {
        public static iOSIntroDiagnosticOverlay Instance { get; private set; }

        private string currentStage = "Initializing";
        private string lastError = "None";
        private List<string> eventHistory = new List<string>();
        private const int MaxHistory = 8;

        private Texture2D backgroundTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("[iOS_Diagnostic_Overlay]");
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

            // Arka plan dokusunu bir kez oluştur
            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0.02f, 0.04f, 0.08f, 0.88f));
            backgroundTexture.Apply();

            Application.logMessageReceived += HandleLog;
            AddEvent("[DIAGNOSTIC] Overlay Initialized");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;

            if (backgroundTexture != null)
            {
                Destroy(backgroundTexture);
            }
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

        public void AddEvent(string evt)
        {
            if (eventHistory.Count >= MaxHistory)
            {
                eventHistory.RemoveAt(0);
            }
            eventHistory.Add($"{DateTime.Now:HH:mm:ss} - {evt}");
        }

        private void OnGUI()
        {
            // Retina ve yüksek çözünürlük ölçeklemesi
            float scale = Mathf.Max(1f, Screen.height / 600f);
            Matrix4x4 origMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            // Sol üst köşe stili
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            if (backgroundTexture != null)
            {
                boxStyle.normal.background = backgroundTexture;
            }

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 11;
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f, 1f);

            GUIStyle textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 9;
            textStyle.normal.textColor = Color.white;

            GUIStyle errStyle = new GUIStyle(GUI.skin.label);
            errStyle.fontSize = 9;
            errStyle.normal.textColor = new Color(1f, 0.35f, 0.35f, 1f);

            Rect panelRect = new Rect(8, 8, 380, 260);
            GUI.Box(panelRect, GUIContent.none, boxStyle);

            GUILayout.BeginArea(new Rect(12, 12, 372, 252));

            GUILayout.Label("=== FARM2SHELF iOS DIAGNOSTIC ===", titleStyle);
            GUILayout.Space(2);

            // Real-time Durum Değerleri
            bool hasIntroFin = EKTReklamIntroManager.HasIntroFinished;
            bool introInstOK = EKTReklamIntroManager.Instance != null;
            bool mainInstOK = MainMenuUI.Instance != null;
            
            GameObject introCanvasObj = GameObject.Find("EKT_Reklam_Intro_Canvas");
            GameObject blackCurtainObj = GameObject.Find("[EKT_Intro_BlackCurtain]");

            GUILayout.Label($"Stage: {currentStage}", textStyle);
            GUILayout.Label($"HasIntroFinished: {hasIntroFin} | Frame: {Time.frameCount}", textStyle);
            GUILayout.Label($"Intro Instance: {(introInstOK ? "OK" : "NULL")} | MainMenu Instance: {(mainInstOK ? "OK" : "NULL")}", textStyle);
            GUILayout.Label($"Intro Canvas: {(introCanvasObj != null ? "ALIVE" : "NULL")} | Black Curtain: {(blackCurtainObj != null ? "ALIVE" : "NULL")}", textStyle);
            GUILayout.Label($"Time.timeScale: {Time.timeScale:F1} | Unscaled Time: {Time.unscaledTime:F1}s", textStyle);

            // Aktif Canvas Tespiti
            Canvas[] activeCanvases = FindObjectsOfType<Canvas>();
            string canvasNames = "";
            int count = 0;
            if (activeCanvases != null)
            {
                foreach (var c in activeCanvases)
                {
                    if (c != null && c.gameObject.activeInHierarchy)
                    {
                        if (count < 4) canvasNames += $"{c.gameObject.name}(order:{c.sortingOrder}), ";
                        count++;
                    }
                }
            }
            GUILayout.Label($"Active Canvases ({count}): {(string.IsNullOrEmpty(canvasNames) ? "None" : canvasNames.TrimEnd(',', ' '))}", textStyle);

            // Son Hata/Exception
            if (lastError != "None")
            {
                GUILayout.Label($"Last Error: {lastError}", errStyle);
            }

            GUILayout.Space(2);
            GUILayout.Label("--- Event History (Last 8) ---", titleStyle);
            for (int i = eventHistory.Count - 1; i >= 0; i--)
            {
                GUILayout.Label(eventHistory[i], textStyle);
            }

            GUILayout.EndArea();

            GUI.matrix = origMatrix;
        }
    }
}
