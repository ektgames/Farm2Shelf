using System;
using UnityEngine;

namespace Farm2Shelf.Core
{
    public enum GameLanguage
    {
        Turkish,
        English
    }

    /// <summary>
    /// Farm2Shelf Küresel Dil Ve Yerelleştirme Yöneticisi (LocalizationManager).
    /// Türkçe ve İngilizce dil seçeneklerini yönetir, PlayerPrefs üzerinde saklar
    /// ve dil değiştiğinde OnLanguageChanged olayını tetikleyerek tüm UI'ların anında güncellenmesini sağlar.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        private static LocalizationManager instance;
        private static bool isQuitting = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            isQuitting = false;
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            isQuitting = false;
            if (instance == null)
            {
                instance = UnityEngine.Object.FindFirstObjectByType<LocalizationManager>();
                if (instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("[LocalizationManager]");
                    instance = go.AddComponent<LocalizationManager>();
                    DontDestroyOnLoad(go);
                }
            }
        }

        public static LocalizationManager Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = UnityEngine.Object.FindFirstObjectByType<LocalizationManager>();
                }
                return instance;
            }
        }

        private const string PREF_KEY_LANGUAGE = "Farm2Shelf_GameLanguage";

        public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.Turkish;

        public event Action<GameLanguage> OnLanguageChanged;

        private void Awake()
        {
            if (instance == null || instance == this)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSavedLanguage();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void LoadSavedLanguage()
        {
            string savedLang = PlayerPrefs.GetString(PREF_KEY_LANGUAGE, "Turkish");
            if (Enum.TryParse(savedLang, out GameLanguage lang))
            {
                CurrentLanguage = lang;
            }
            else
            {
                CurrentLanguage = GameLanguage.Turkish;
            }
        }

        public void SetLanguage(GameLanguage language)
        {
            CurrentLanguage = language;
            PlayerPrefs.SetString(PREF_KEY_LANGUAGE, language.ToString());
            PlayerPrefs.Save();

            Debug.Log($"[LocalizationManager] Dil değiştirildi -> {CurrentLanguage}");

            if (OnLanguageChanged != null)
            {
                Delegate[] invocationList = OnLanguageChanged.GetInvocationList();
                foreach (var del in invocationList)
                {
                    try
                    {
                        ((Action<GameLanguage>)del).Invoke(CurrentLanguage);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LocalizationManager] Dil güncelleme bildirimi uyarısı: {ex.Message}");
                    }
                }
            }
        }

        public void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == GameLanguage.Turkish ? GameLanguage.English : GameLanguage.Turkish);
        }

        /// <summary>
        /// Küresel Metin Yerelleştirme Yardımcısı.
        /// Mevcut dil Türkçe ise turkishText, İngilizce ise englishText döner.
        /// Sahne kapanışlarında nesne üretmez.
        /// </summary>
        public static string L(string key, string turkishText, string englishText)
        {
            if (instance != null && instance.CurrentLanguage == GameLanguage.English)
            {
                return englishText;
            }
            return turkishText;
        }

        public bool IsEnglish => CurrentLanguage == GameLanguage.English;
        public bool IsTurkish => CurrentLanguage == GameLanguage.Turkish;
    }
}
