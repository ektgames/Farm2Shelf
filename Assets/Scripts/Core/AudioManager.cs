using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Farm2Shelf Ses ve Müzik Yöneticisi (AudioManager).
    /// 10 adet tatlı telifsiz low-poly arka plan müziğini (BGM) otomatik sırayla çalar,
    /// buton tıklama, tablet dokunma, para ve mahsul biçme ses efektlerini (SFX) yayınlar.
    /// Kaynak dosyaları eksik olsa dahi çalışma zamanında (Runtime Procedural Audio) 10 adet 
    /// farklı beste üreterek müziğin %100 kesintisiz ve çeşitlilikle çalmasını sağlar.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        private AudioSource bgmSource;
        private AudioSource sfxSource;

        [Header("Audio Collections")]
        private List<AudioClip> bgmTracks = new List<AudioClip>();
        private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();

        [Header("Track Names (Türkçe Başlıklar)")]
        private readonly string[] trackTitles = new string[]
        {
            "İlham Veren Akustik Folk 🌾",
            "Tarım & Çiftlik Melodisi 🚜",
            "Nehir Boyu Huzur 🌊",
            "Sakin & Şükran Günü ☀️",
            "Neşeli Kırsal Yaşam 🌻",
            "Bereketli Tarla Esintisi 🌱",
            "Pazar Yeri Coşkusu 🛒",
            "Organik Tarım Ruhu 🥦",
            "Çiftlikte Hasat Şenliği 🌽",
            "Bahçeni Büyüt 🌸",
            "Kırsal Country Ritimleri 🪕",
            "Gece Pazarı Melodileri 🌙",
            "Neşeli Çiftlik Ritimleri 🌾",
            "Enerjik Kasaba Funky 🎺",
            "Ritmik Mağaza Yürüyüşü 🛒",
            "Modern Market Melodisi 🛍️",
            "Büyük Hasat Coşkusu 🎉"
        };

        private readonly string[] trackTitlesEn = new string[]
        {
            "Inspiring Acoustic Folk 🌾",
            "Agriculture & Farm Song 🚜",
            "Peaceful River Stroll 🌊",
            "Calm & Gratitude ☀️",
            "Joyful Rural Life 🌻",
            "Bountiful Field Breeze 🌱",
            "Marketplace Energy 🛒",
            "Organic Farming Spirit 🥦",
            "Farm Harvest Festival 🌽",
            "Grow Your Garden 🌸",
            "Rural Country Beats 🪕",
            "Night Market Melodies 🌙",
            "Joyful Farm Beats 🌾",
            "Upbeat Town Funk 🎺",
            "Rhythmic Store Walk 🛒",
            "Modern Market Melody 🛍️",
            "Grand Harvest Celebration 🎉"
        };

        [Header("Audio State")]
        private int currentTrackIndex = 0;
        private float bgmVolume = 0.6f;
        private float sfxVolume = 0.8f;
        private bool isBgmMuted = false;
        private bool isSfxMuted = false;
        private bool isChangingTrack = false;

        private const string PREF_BGM_VOL = "Farm2Shelf_BGM_Vol";
        private const string PREF_SFX_VOL = "Farm2Shelf_SFX_Vol";
        private const string PREF_BGM_MUTE = "Farm2Shelf_BGM_Mute";
        private const string PREF_SFX_MUTE = "Farm2Shelf_SFX_Mute";

        public event System.Action<int, string> OnTrackChanged;
        public event System.Action<float> OnBGMVolumeChanged;
        public event System.Action<float> OnSFXVolumeChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSystem();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudioSystem()
        {
            // 1. AudioSource Bileşenleri
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = false;
            bgmSource.playOnAwake = false;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;

            // 2. PlayerPrefs Ayarlarını Yükle
            bgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOL, 0.6f);
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.8f);
            isBgmMuted = PlayerPrefs.GetInt(PREF_BGM_MUTE, 0) == 1;
            isSfxMuted = PlayerPrefs.GetInt(PREF_SFX_MUTE, 0) == 1;

            UpdateSourceVolumes();

            // 3. Masaüstünden Yüklenen 12 Gerçek MP3 Müzik Parçasını Yükle
            LoadAudioAssets();

            // 4. Ana Menüden İtibaren Müziği Başlat
            if (bgmTracks.Count > 0)
            {
                PlayCurrentBGMTrack();
            }
        }

        private void LoadAudioAssets()
        {
            bgmTracks.Clear();
            sfxClips.Clear();

            // Masaüstünden entegre edilen 17 Gerçek MP3 Müzik Parçasını Yükle
            for (int i = 1; i <= 17; i++)
            {
                string path = $"Audio/BGM/track_{i:D2}";
                AudioClip clip = Resources.Load<AudioClip>(path);
                if (clip != null)
                {
                    bgmTracks.Add(clip);
                    Debug.Log($"[AudioManager] Masaüstü MP3 Müziği Yüklendi: {path}");
                }
                else
                {
                    // Fallback (Yedek Prosedürel Beste)
                    clip = GenerateProceduralBGMTrack(i - 1);
                    bgmTracks.Add(clip);
                }
            }

            // SFX Seslerini Yükle
            string[] sfxNames = new string[] { "button_click", "tablet_tap", "coins_purchase", "harvest_crop", "modal_open", "modal_close", "barcode_beep", "cash_register" };
            foreach (string name in sfxNames)
            {
                AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{name}");
                if (clip == null)
                {
                    clip = GenerateProceduralSFX(name);
                }
                sfxClips[name] = clip;
            }
        }

        private void Update()
        {
            // Fon müziği bittiğinde otomatik bir sonraki parçaya geç
            if (bgmSource != null && bgmTracks.Count > 0 && !isBgmMuted && !isChangingTrack && bgmSource.clip != null)
            {
                // Parça çalıyordu ve sonuna ulaştıysa sonraki parçaya geç
                if (bgmSource.isPlaying && bgmSource.clip.length > 0f && bgmSource.time >= bgmSource.clip.length - 0.25f)
                {
                    StartCoroutine(AutoNextTrackRoutine());
                }
            }
        }

        private IEnumerator AutoNextTrackRoutine()
        {
            isChangingTrack = true;
            NextTrack();
            yield return new WaitForSeconds(1.0f);
            isChangingTrack = false;
        }

        private void PlayCurrentBGMTrack()
        {
            if (bgmTracks.Count == 0) return;
            currentTrackIndex = Mathf.Clamp(currentTrackIndex, 0, bgmTracks.Count - 1);

            AudioClip clip = bgmTracks[currentTrackIndex];
            if (clip != null && bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = clip;
                bgmSource.time = 0f;
                if (!isBgmMuted)
                {
                    bgmSource.Play();
                }
                string title = GetCurrentTrackTitle();
                OnTrackChanged?.Invoke(currentTrackIndex + 1, title);
                Debug.Log($"[AudioManager] Çalan Parça ({currentTrackIndex + 1}/{bgmTracks.Count}): {title}");
            }
        }

        public void NextTrack()
        {
            if (bgmTracks.Count == 0) return;
            currentTrackIndex = (currentTrackIndex + 1) % bgmTracks.Count;
            PlayCurrentBGMTrack();
        }

        public void PreviousTrack()
        {
            if (bgmTracks.Count == 0) return;
            currentTrackIndex = (currentTrackIndex - 1 + bgmTracks.Count) % bgmTracks.Count;
            PlayCurrentBGMTrack();
        }

        // ==================== PROSEDÜREL 10 ADET FARKLI BESTE ÜRETİCİSİ ====================

        private AudioClip GenerateProceduralBGMTrack(int trackIndex)
        {
            int sampleRate = 44100;
            float noteDuration = 0.22f;

            // 10 Farklı Beste Dizilimi (C Major, F Major, G Major, A Minor Pentatonik Gamlar)
            string[][] melodies = new string[][]
            {
                // 1. Çiftlikte Sabah Güneşi 🌾
                new string[] { "C4", "E4", "G4", "C5", "G4", "E4", "C4", "REST", "D4", "F4", "A4", "D5", "A4", "F4", "D4", "REST", "G4", "B4", "D5", "G5", "D5", "B4", "G4", "REST", "C5", "G4", "E4", "C4" },
                // 2. Yeşil Tarlalar & Neşe 🌱
                new string[] { "E4", "G4", "C5", "E5", "D5", "C5", "G4", "E4", "F4", "A4", "C5", "F5", "E5", "C5", "A4", "F4", "G4", "B4", "D5", "F5", "E5", "D5", "C5", "G4", "C5", "E5", "G5" },
                // 3. Tatlı Pazar Alışverişi 🛒
                new string[] { "C5", "B4", "A4", "G4", "E4", "G4", "C5", "REST", "D5", "C5", "B4", "A4", "F4", "A4", "D5", "REST", "E5", "D5", "C5", "B4", "G4", "B4", "E5", "REST", "C5", "E5", "G5", "C6" },
                // 4. Ahırda Hasat Zamanı 🚜
                new string[] { "G4", "C5", "E5", "G5", "F5", "E5", "D5", "C5", "A4", "C5", "F5", "A5", "G5", "F5", "E5", "D5", "B4", "D5", "G5", "B5", "A5", "G5", "F5", "D5", "C5", "E5", "G5" },
                // 5. Şehir Parkı Gezintisi 🌳
                new string[] { "E5", "D5", "C5", "G4", "A4", "C5", "E5", "REST", "F5", "E5", "D5", "A4", "B4", "D5", "F5", "REST", "G5", "F5", "E5", "B4", "C5", "E5", "G5", "REST", "C6", "G5", "E5" },
                // 6. Reyonlar Arasında 📦
                new string[] { "C4", "G4", "C5", "E5", "D5", "B4", "G4", "REST", "D4", "A4", "D5", "F5", "E5", "C5", "A4", "REST", "E4", "B4", "E5", "G5", "F5", "D5", "B4", "REST", "C5", "G4", "E4" },
                // 7. Akşam Üstü Esintisi 🌅
                new string[] { "G4", "A4", "C5", "D5", "E5", "D5", "C5", "A4", "G4", "A4", "C5", "E5", "G5", "E5", "C5", "REST", "F4", "A4", "C5", "F5", "E5", "C5", "A4", "F4", "C5", "E5", "G5" },
                // 8. Kahve Arası & Mola ☕
                new string[] { "C5", "C5", "E5", "G5", "A5", "G5", "E5", "C5", "D5", "D5", "F5", "A5", "B5", "A5", "F5", "D5", "E5", "E5", "G5", "B5", "C6", "B5", "G5", "E5", "C5", "E5", "G5" },
                // 9. Yıldızlı Çiftlik Gecesi 🌙
                new string[] { "E4", "G4", "B4", "E5", "D5", "B4", "G4", "E4", "A4", "C5", "E5", "A5", "G5", "E5", "C5", "A4", "B4", "D5", "F5", "B5", "A5", "F5", "D5", "B4", "C5", "E5", "G5" },
                // 10. Süpermarket Coşkusu 🏬
                new string[] { "C5", "E5", "G5", "C6", "B5", "G5", "E5", "C5", "F5", "A5", "C6", "F6", "E6", "C6", "A5", "F5", "G5", "B5", "D6", "G6", "F6", "D6", "B5", "G5", "C6", "G5", "E5" }
            };

            string[] seq = melodies[trackIndex % melodies.Length];
            int totalSamples = (int)(seq.Length * noteDuration * sampleRate);
            float[] data = new float[totalSamples];

            int sampleIndex = 0;
            float timbreFreqMult = 1.0f + (trackIndex * 0.04f); // Her şarkıya özel oktav & tını farkı

            foreach (string n in seq)
            {
                int noteSamplesCount = (int)(noteDuration * sampleRate);
                if (n != "REST")
                {
                    float freq = GetNoteFrequency(n) * timbreFreqMult;
                    for (int i = 0; i < noteSamplesCount; i++)
                    {
                        if (sampleIndex >= totalSamples) break;
                        float t = (float)i / sampleRate;
                        float env = 1.0f;
                        int attack = (int)(0.01f * sampleRate);
                        int release = (int)(0.04f * sampleRate);
                        if (i < attack) env = (float)i / attack;
                        else if (i > noteSamplesCount - release) env = (float)(noteSamplesCount - i) / release;

                        // Low-Poly Marimba/Pluck Tınısı (3 Harmonikli Yumuşak Sentez)
                        float wave1 = Mathf.Sin(2f * Mathf.PI * freq * t);
                        float wave2 = Mathf.Sin(2f * Mathf.PI * freq * 2.01f) * 0.25f;
                        float wave3 = Mathf.Sin(2f * Mathf.PI * freq * 3.02f) * 0.10f;

                        data[sampleIndex] = (wave1 + wave2 + wave3) * 0.25f * env;
                        sampleIndex++;
                    }
                }
                else
                {
                    for (int i = 0; i < noteSamplesCount; i++)
                    {
                        if (sampleIndex >= totalSamples) break;
                        data[sampleIndex] = 0f;
                        sampleIndex++;
                    }
                }
            }

            AudioClip clip = AudioClip.Create($"procedural_track_{trackIndex + 1}", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateProceduralSFX(string sfxName)
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            if (sfxName == "coins_purchase") duration = 0.30f;
            else if (sfxName == "barcode_beep") duration = 0.075f;
            else if (sfxName == "cash_register") duration = 0.32f;

            int totalSamples = (int)(duration * sampleRate);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = (float)(totalSamples - i) / totalSamples;
                float freq = 800f;

                if (sfxName == "button_click") freq = 1200f - (t / duration) * 600f;
                else if (sfxName == "tablet_tap") freq = 500f - (t / duration) * 200f;
                else if (sfxName == "coins_purchase") freq = (t < 0.12f) ? 1046f : 1318f;
                else if (sfxName == "harvest_crop") freq = 600f + (t / duration) * 400f;
                else if (sfxName == "barcode_beep")
                {
                    // Gerçekçi Süpermarket Barkod Okuyucu Bip Sesi (2700 Hz Net ve Parlak Bip)
                    freq = 2700f;
                    float attack = Mathf.Clamp01(t / 0.006f);
                    float decay = Mathf.Pow(env, 0.5f);
                    data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.8f + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.2f) * 0.45f * attack * decay;
                    continue;
                }
                else if (sfxName == "cash_register")
                {
                    freq = (t < 0.14f) ? 1568f : 2093f; // G6 -> C7 neşeli kasa çanı
                }

                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f * env;
            }

            AudioClip clip = AudioClip.Create($"procedural_sfx_{sfxName}", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private float GetNoteFrequency(string note)
        {
            switch (note)
            {
                case "C4": return 261.63f;
                case "D4": return 293.66f;
                case "E4": return 329.63f;
                case "F4": return 349.23f;
                case "G4": return 392.00f;
                case "A4": return 440.00f;
                case "B4": return 493.88f;
                case "C5": return 523.25f;
                case "D5": return 587.33f;
                case "E5": return 659.25f;
                case "F5": return 698.46f;
                case "G5": return 783.99f;
                case "A5": return 880.00f;
                case "B5": return 987.77f;
                case "C6": return 1046.50f;
                case "D6": return 1174.66f;
                case "E6": return 1318.51f;
                case "F6": return 1396.91f;
                case "G6": return 1567.98f;
                default: return 440.00f;
            }
        }

        // ==================== SES DÜZEYİ & MUTE AYARLARI ====================

        public void SetBGMVolume(float vol)
        {
            bgmVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(PREF_BGM_VOL, bgmVolume);
            PlayerPrefs.Save();
            UpdateSourceVolumes();
            OnBGMVolumeChanged?.Invoke(bgmVolume);
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(PREF_SFX_VOL, sfxVolume);
            PlayerPrefs.Save();
            UpdateSourceVolumes();
            OnSFXVolumeChanged?.Invoke(sfxVolume);
        }

        public void ToggleBGMMute()
        {
            isBgmMuted = !isBgmMuted;
            PlayerPrefs.SetInt(PREF_BGM_MUTE, isBgmMuted ? 1 : 0);
            PlayerPrefs.Save();

            if (isBgmMuted)
            {
                bgmSource.Pause();
            }
            else
            {
                if (!bgmSource.isPlaying)
                {
                    PlayCurrentBGMTrack();
                }
            }
            UpdateSourceVolumes();
        }

        public void ToggleSFXMute()
        {
            isSfxMuted = !isSfxMuted;
            PlayerPrefs.SetInt(PREF_SFX_MUTE, isSfxMuted ? 1 : 0);
            PlayerPrefs.Save();
            UpdateSourceVolumes();
        }

        private void UpdateSourceVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = isBgmMuted ? 0f : bgmVolume;
            }
            if (sfxSource != null)
            {
                sfxSource.volume = isSfxMuted ? 0f : sfxVolume;
            }
        }

        // ==================== SFX ÇALMA YARDIMCILARI ====================

        public void PlaySFX(string name)
        {
            if (isSfxMuted || sfxSource == null) return;
            if (sfxClips.TryGetValue(name, out AudioClip clip))
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        public void PlayButtonClick() => PlaySFX("button_click");
        public void PlayTabletTap() => PlaySFX("tablet_tap");
        public void PlayCoins() => PlaySFX("coins_purchase");
        public void PlayHarvest() => PlaySFX("harvest_crop");
        public void PlayModalOpen() => PlaySFX("modal_open");
        public void PlayModalClose() => PlaySFX("modal_close");

        public void PlayBarcodeBeep(float pitch = 1.0f)
        {
            if (isSfxMuted || sfxSource == null) return;
            if (sfxClips.TryGetValue("barcode_beep", out AudioClip clip))
            {
                sfxSource.pitch = Mathf.Clamp(pitch, 0.7f, 1.4f);
                sfxSource.PlayOneShot(clip, sfxVolume * 0.90f);
                sfxSource.pitch = 1.0f;
            }
        }

        public void PlayCashRegister() => PlaySFX("cash_register");

        // ==================== PROPERTY GETTERS ====================

        public float BGMVolume => bgmVolume;
        public float SFXVolume => sfxVolume;
        public bool IsBGMMuted => isBgmMuted;
        public bool IsSFXMuted => isSfxMuted;
        public int CurrentTrackIndex => currentTrackIndex + 1;
        public int TotalTracks => bgmTracks.Count > 0 ? bgmTracks.Count : 17;

        public string GetCurrentTrackTitle()
        {
            if (currentTrackIndex >= 0 && currentTrackIndex < trackTitles.Length)
            {
                return LocalizationManager.L("Track_" + currentTrackIndex, trackTitles[currentTrackIndex], trackTitlesEn[currentTrackIndex]);
            }
            string defaultFmt = LocalizationManager.L("Track_DefaultFmt", "Parça {0}", "Track {0}");
            return string.Format(defaultFmt, currentTrackIndex + 1);
        }
    }
}
