using UnityEngine;
using System;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Farm2Shelf Oyun Saati ve Takvim Yöneticisi.
    /// Gece yarısı (00:00) OnMidnightRollover olayını tetikler.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Zaman Ayarları")]
        [SerializeField] private float realSecondsPerGameMinute = 0.5f;

        [Header("Mevcut Zaman State")]
        [SerializeField] private int currentHour = 6;
        [SerializeField] private int currentMinute = 0;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int currentYear = 1;
        [SerializeField] private Season currentSeason = Season.İlkbahar;

        private float timer = 0f;

        public enum Season { İlkbahar, Yaz, Sonbahar, Kış }

        public event Action<int, int> OnTimeUpdated; // (Hour, Minute)
        public event Action<Season, int, int> OnDateUpdated; // (Season, Day, Year) - Takvim UI & Mevsim rozetleri
        public event Action<Season, int, int> OnNewDayStarted; // (Season, Day, Year) - Yalnızca gün atlandığında tetiklenir (Tarla/mahsul simülasyonu için)
        [Header("Duraklatma State")]
        [SerializeField] private bool isTimePaused = true;
        [SerializeField] private bool isDayActive = false;

        public bool IsTimePaused => isTimePaused;
        public bool IsDayActive => isDayActive;

        public event Action OnMidnightRollover; // Gece yarısı 00:00 tetikleyicisi!
        public event Action OnHourPassed;       // Saat başı tetikleyici!

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartDayTimeFlow()
        {
            if (currentHour >= 24)
            {
                Debug.LogWarning("[TimeManager] Saat 24:00 olduğu için gün akışı başlatılamaz. Ertesi güne geçilmelidir.");
                return;
            }
            timer = 0f;
            isDayActive = true;
            isTimePaused = false;
            Debug.Log("[TimeManager] GÜN ZAMAN AKIŞI BAŞLATILDI (Gece 12'ye kadar kesintisiz akacak)");
        }

        public void SetTime(int day, int hour, int minute)
        {
            currentDay = Mathf.Max(1, day);
            currentHour = Mathf.Clamp(hour, 0, 24);
            currentMinute = Mathf.Clamp(minute, 0, 59);
            if (currentHour >= 24)
            {
                currentHour = 24;
                currentMinute = 0;
                isTimePaused = true;
                isDayActive = false;
            }
            OnTimeUpdated?.Invoke(currentHour, currentMinute);
            OnDateUpdated?.Invoke(currentSeason, currentDay, currentYear);
        }

        public void SetTimeAndSeason(int day, int hour, int minute, Season season, int year)
        {
            currentDay = Mathf.Max(1, day);
            currentHour = Mathf.Clamp(hour, 0, 24);
            currentMinute = Mathf.Clamp(minute, 0, 59);
            currentSeason = season;
            currentYear = Mathf.Max(1, year);
            if (currentHour >= 24)
            {
                currentHour = 24;
                currentMinute = 0;
                isTimePaused = true;
                isDayActive = false;
            }
            OnTimeUpdated?.Invoke(currentHour, currentMinute);
            OnDateUpdated?.Invoke(currentSeason, currentDay, currentYear);
        }

        public void SetTimePaused(bool paused)
        {
            isTimePaused = paused;
            Debug.Log($"[TimeManager] Zaman {(isTimePaused ? "DURAKLATILDI" : "BAŞLATILDI")}");
        }

        public void SkipToNextDay06AM()
        {
            currentHour = 6;
            currentMinute = 0;
            timer = 0f;
            isDayActive = false;
            isTimePaused = true; // Oyuncu dükkanı açana kadar saat durur!

            if (StoreStatusManager.Instance != null && StoreStatusManager.Instance.IsOpen)
            {
                StoreStatusManager.Instance.CloseStore();
            }

            AdvanceDay();
            OnTimeUpdated?.Invoke(currentHour, currentMinute);
            Debug.Log($"[TimeManager] Ertesi Güne Atlandı: Sabah 06:00 (Zaman Duraklatıldı)");
        }

        private void Update()
        {
            // Gün başlatılmamışsa (Sabah 06:00 beklemesi), zaman duraklatılmışsa, Gece 24:00 olmuşsa veya dükkan kapalıysa oyun saati KESİNLİKLE ilerlemez!
            if (isTimePaused || !isDayActive || currentHour >= 24 || (StoreStatusManager.Instance != null && !StoreStatusManager.Instance.IsOpen)) return;

            AdvanceTime();
        }

        private void AdvanceTime()
        {
            // Saat 24:00 (Gece 12:00) veya üstüyse zaman KESİNTİSİZ DONAR
            if (currentHour >= 24)
            {
                currentHour = 24;
                currentMinute = 0;
                isTimePaused = true;
                isDayActive = false;
                return;
            }

            timer += Time.deltaTime;
            if (timer >= realSecondsPerGameMinute)
            {
                timer -= realSecondsPerGameMinute;
                currentMinute++;

                if (currentMinute >= 60)
                {
                    currentMinute = 0;
                    currentHour++;
                    OnHourPassed?.Invoke();

                    if (currentHour >= 24)
                    {
                        // GECE YARISI (24:00 / GECE 12) AKIŞI:
                        // 1. Saat 24:00'da sabitlenir ve zaman akışı KESİNTİSİZ DURDURULUR
                        // 2. Dükkan Kapalı Duruma Getirilir (Yeni müşteri girmez, içeridekiler tahliye edilir)
                        // 3. Gece yarısı devir teslim ve maaş olaylarını tetikle
                        currentHour = 24;
                        currentMinute = 0;
                        isTimePaused = true;
                        isDayActive = false;

                        if (StoreStatusManager.Instance != null)
                        {
                            StoreStatusManager.Instance.SetStoreStatus(false);
                        }

                        OnTimeUpdated?.Invoke(currentHour, currentMinute);
                        OnMidnightRollover?.Invoke();
                        return;
                    }
                }

                OnTimeUpdated?.Invoke(currentHour, currentMinute);
            }
        }

        private void AdvanceDay()
        {
            currentDay++;
            if (currentDay > 30)
            {
                currentDay = 1;
                AdvanceSeason();
            }

            OnDateUpdated?.Invoke(currentSeason, currentDay, currentYear);
            OnNewDayStarted?.Invoke(currentSeason, currentDay, currentYear);
        }

        private void AdvanceSeason()
        {
            int nextSeasonIndex = ((int)currentSeason + 1) % 4;
            currentSeason = (Season)nextSeasonIndex;

            if (currentSeason == Season.İlkbahar)
            {
                currentYear++;
            }
        }

        public string GetFormattedTime()
        {
            return $"{currentHour:D2}:{currentMinute:D2}";
        }

        public string GetFormattedDate()
        {
            string seasonName = GetLocalizedSeasonName(currentSeason).ToUpper();
            string dayLabel = LocalizationManager.L("Label_DayUpper", "GÜN", "DAY");
            return $"{seasonName} • {dayLabel} {currentDay}";
        }

        public int Hour => currentHour;
        public int Minute => currentMinute;
        public int CurrentHour => currentHour;
        public int CurrentMinute => currentMinute;
        public int Day => currentDay;
        public Season CurrentSeason => currentSeason;
        public int Year => currentYear;

        public string GetLocalizedSeasonName()
        {
            return GetLocalizedSeasonName(currentSeason);
        }

        public string GetLocalizedSeasonName(Season season)
        {
            switch (season)
            {
                case Season.İlkbahar: return LocalizationManager.L("Season_Spring", "İlkbahar", "Spring");
                case Season.Yaz: return LocalizationManager.L("Season_Summer", "Yaz", "Summer");
                case Season.Sonbahar: return LocalizationManager.L("Season_Autumn", "Sonbahar", "Autumn");
                case Season.Kış: return LocalizationManager.L("Season_Winter", "Kış", "Winter");
                default: return season.ToString();
            }
        }
    }
}
