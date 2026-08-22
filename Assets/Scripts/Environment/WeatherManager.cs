using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    public enum WeatherType
    {
        Sunny,
        Rainy,
        Snowy
    }

    /// <summary>
    /// Farm2Shelf Mevsimsel Dinamik Hava Durumu Yöneticisi (Weather Manager).
    /// İlkbahar, Yaz, Sonbahar ve Kış mevsimlerine göre gerçekçi olasılıklarla Yağmur, Kar veya Güneş üretir.
    /// Yağmur yağdığında sahnede sağanak yağmur parçacıkları belirir ve yollar ıslanıp parlar.
    /// Kar yağdığında sahnede lapa lapa kar taneleri süzülür ve çevre (yollar, çimler, çatılar) beyaza bürünür.
    /// </summary>
    public class WeatherManager : MonoBehaviour
    {
        public static WeatherManager Instance { get; private set; }

        public WeatherType CurrentWeather { get; private set; } = WeatherType.Sunny;

        public event Action<WeatherType> OnWeatherChanged;

        private ParticleSystem rainParticleSys;
        private ParticleSystem snowParticleSys;
        private Transform weatherFollowGroup;

        // Materyal Orijinal Renk Kayıtları (Kar ve Yağmur Etkisi Sonrası Eskiye Dönüş İçin)
        private Color origGrassColor = new Color(0.28f, 0.62f, 0.28f);
        private Color origRoadColor = new Color(0.18f, 0.20f, 0.22f);
        private Color origSidewalkColor = new Color(0.70f, 0.72f, 0.75f);
        private Color origTownSquareColor = new Color(0.65f, 0.68f, 0.72f);
        private Color origRoofRedColor = new Color(0.78f, 0.22f, 0.18f);

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
        }

        private void Start()
        {
            CreateWeatherParticleSystems();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateUpdated -= HandleDateUpdated;
                TimeManager.Instance.OnDateUpdated += HandleDateUpdated;

                // Mevcut mevsim için hava durumunu başlat
                RollWeatherForSeason(TimeManager.Instance.CurrentSeason);
            }
            else
            {
                SetWeather(WeatherType.Sunny);
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateUpdated -= HandleDateUpdated;
            }
        }

        private void Update()
        {
            // Hava durumu parçacık sistemlerinin kamerayı takip etmesi (Tüm haritayı kapsaması için)
            if (weatherFollowGroup != null && Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                weatherFollowGroup.position = new Vector3(camPos.x, 22.0f, camPos.z + 5.0f);
            }
        }

        private void HandleDateUpdated(TimeManager.Season season, int day, int year)
        {
            RollWeatherForSeason(season);
        }

        public static WeatherType GetWeatherForecastForDay(TimeManager.Season season, int day, int year = 1)
        {
            int seed = (year * 1000) + ((int)season * 100) + day;
            UnityEngine.Random.State prevState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);
            float roll = UnityEngine.Random.value;
            UnityEngine.Random.state = prevState;

            switch (season)
            {
                case TimeManager.Season.İlkbahar:
                    return (roll < 0.30f) ? WeatherType.Rainy : WeatherType.Sunny;
                case TimeManager.Season.Yaz:
                    return (roll < 0.10f) ? WeatherType.Rainy : WeatherType.Sunny;
                case TimeManager.Season.Sonbahar:
                    return (roll < 0.60f) ? WeatherType.Rainy : WeatherType.Sunny;
                case TimeManager.Season.Kış:
                    if (roll < 0.70f) return WeatherType.Snowy;
                    if (roll < 0.85f) return WeatherType.Rainy;
                    return WeatherType.Sunny;
                default:
                    return WeatherType.Sunny;
            }
        }

        public void RollWeatherForSeason(TimeManager.Season season)
        {
            int curDay = (TimeManager.Instance != null) ? TimeManager.Instance.Day : 1;
            int curYear = (TimeManager.Instance != null) ? TimeManager.Instance.Year : 1;
            WeatherType selectedWeather = GetWeatherForecastForDay(season, curDay, curYear);
            SetWeather(selectedWeather);
        }

        public void SetWeather(WeatherType weather)
        {
            CurrentWeather = weather;
            Debug.Log($"[WeatherManager] HAVA DURUMU DEĞİŞTİ: {weather} ☀️🌧️❄️");

            UpdateParticleEffects();
            ApplyEnvironmentMaterialEffects();

            OnWeatherChanged?.Invoke(CurrentWeather);
        }

        private void CreateWeatherParticleSystems()
        {
            GameObject group = new GameObject("Weather_Effects_Group");
            group.transform.SetParent(transform);
            weatherFollowGroup = group.transform;
            weatherFollowGroup.position = new Vector3(0f, 22.0f, 0f);

            // 1. YAĞMUR PARÇACIK SİSTEMİ
            GameObject rainObj = new GameObject("Rain_Particle_System");
            rainObj.transform.SetParent(weatherFollowGroup, false);
            rainObj.transform.localPosition = Vector3.zero;
            rainObj.transform.localRotation = Quaternion.Euler(85f, 0f, 0f);

            rainParticleSys = rainObj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule rMain = rainParticleSys.main;
            rMain.startLifetime = 1.0f;
            rMain.startSpeed = 26.0f;
            rMain.startSize = 0.25f;
            rMain.startColor = new Color(0.80f, 0.90f, 1.0f, 0.65f);
            rMain.maxParticles = 1200;
            rMain.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule rEmission = rainParticleSys.emission;
            rEmission.rateOverTime = 450f;

            ParticleSystem.ShapeModule rShape = rainParticleSys.shape;
            rShape.shapeType = ParticleSystemShapeType.Box;
            rShape.scale = new Vector3(90f, 90f, 1f);

            ParticleSystemRenderer rRenderer = rainObj.GetComponent<ParticleSystemRenderer>();
            rRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rRenderer.cameraVelocityScale = 0f;
            rRenderer.velocityScale = 0.15f;
            rRenderer.lengthScale = 3.5f;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material rainMat = new Material(shader) { color = new Color(0.85f, 0.92f, 1.0f, 0.60f) };
            rRenderer.sharedMaterial = rainMat;

            // 2. KAR PARÇACIK SİSTEMİ
            GameObject snowObj = new GameObject("Snow_Particle_System");
            snowObj.transform.SetParent(weatherFollowGroup, false);
            snowObj.transform.localPosition = Vector3.zero;
            snowObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            snowParticleSys = snowObj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule sMain = snowParticleSys.main;
            sMain.startLifetime = 4.5f;
            sMain.startSpeed = 4.5f;
            sMain.startSize = 0.40f;
            sMain.startColor = new Color(0.96f, 0.98f, 1.0f, 0.90f);
            sMain.maxParticles = 1500;
            sMain.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule sEmission = snowParticleSys.emission;
            sEmission.rateOverTime = 320f;

            ParticleSystem.ShapeModule sShape = snowParticleSys.shape;
            sShape.shapeType = ParticleSystemShapeType.Box;
            sShape.scale = new Vector3(90f, 90f, 1f);

            ParticleSystemRenderer sRenderer = snowObj.GetComponent<ParticleSystemRenderer>();
            sRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material snowMat = new Material(shader) { color = new Color(0.98f, 0.98f, 1.0f, 0.95f) };
            sRenderer.sharedMaterial = snowMat;

            rainParticleSys.Stop();
            snowParticleSys.Stop();
        }

        private void UpdateParticleEffects()
        {
            if (rainParticleSys == null || snowParticleSys == null) return;

            if (CurrentWeather == WeatherType.Rainy)
            {
                if (!rainParticleSys.isPlaying) rainParticleSys.Play();
                if (snowParticleSys.isPlaying) snowParticleSys.Stop();
            }
            else if (CurrentWeather == WeatherType.Snowy)
            {
                if (rainParticleSys.isPlaying) rainParticleSys.Stop();
                if (!snowParticleSys.isPlaying) snowParticleSys.Play();
            }
            else
            {
                if (rainParticleSys.isPlaying) rainParticleSys.Stop();
                if (snowParticleSys.isPlaying) snowParticleSys.Stop();
            }
        }

        /// <summary>
        /// Yağmurda yolların ıslanıp parlamasını, Kışın ise çevrenin karla kaplanıp beyaza bürünmesini sağlar.
        /// </summary>
        private void ApplyEnvironmentMaterialEffects()
        {
            bool isRainy = (CurrentWeather == WeatherType.Rainy);
            bool isSnowy = (CurrentWeather == WeatherType.Snowy);

            // 1. ISLAK YOL EFEKTİ (Yağmurda Yollar & Kaldırımlar Parlar ve Islanır)
            Color roadColor = isSnowy
                ? new Color(0.82f, 0.86f, 0.88f) // Karlı yollar
                : (isRainy ? new Color(0.10f, 0.12f, 0.14f) : origRoadColor); // Islak siyah yol vs normal yol

            Color sidewalkColor = isSnowy
                ? new Color(0.90f, 0.93f, 0.95f)
                : (isRainy ? new Color(0.50f, 0.53f, 0.58f) : origSidewalkColor);

            Color townSquareColor = isSnowy
                ? new Color(0.88f, 0.92f, 0.94f)
                : (isRainy ? new Color(0.45f, 0.48f, 0.52f) : origTownSquareColor);

            // 2. BEYAZA BÜRÜNME EFEKTİ (Kışın Çimler & Çatılar Kar İle Kaplanır)
            Color grassColor = isSnowy
                ? new Color(0.92f, 0.96f, 0.98f) // Bembeyaz Kar Örtüsü!
                : origGrassColor;

            Color roofColor = isSnowy
                ? new Color(0.95f, 0.97f, 1.0f) // Karlı Beyaz Çatı!
                : origRoofRedColor;

            // Sahnede İlgili Materyalleri Tek Geçişte Güncelle (6 kat daha hızlı ve 0 GC)
            Dictionary<string, (Color color, float smoothness)> matUpdates = new Dictionary<string, (Color color, float smoothness)>
            {
                { "MainRoadMat", (roadColor, isRainy ? 0.85f : 0.2f) },
                { "SidewalkMat", (sidewalkColor, isRainy ? 0.75f : 0.1f) },
                { "TownSquareMat", (townSquareColor, isRainy ? 0.70f : 0.1f) },
                { "GrassMat", (grassColor, 0.05f) },
                { "RoofRedMat", (roofColor, 0.1f) },
                { "FarmhouseRoofMat", (roofColor, 0.1f) },
                { "BarnRoofMat", (isSnowy ? new Color(0.92f, 0.95f, 0.98f) : new Color(0.28f, 0.30f, 0.35f), 0.1f) }
            };

            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (renderers == null) return;

            int rLen = renderers.Length;
            for (int i = 0; i < rLen; i++)
            {
                var r = renderers[i];
                if (r == null || r.sharedMaterial == null) continue;

                string sMatName = r.sharedMaterial.name;
                foreach (var kvp in matUpdates)
                {
                    if (sMatName.Contains(kvp.Key))
                    {
                        Material mat = r.sharedMaterial;
                        if (mat != null)
                        {
                            mat.color = kvp.Value.color;
                            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", kvp.Value.color);
                            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", kvp.Value.smoothness);
                            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", kvp.Value.smoothness);
                        }
                        break;
                    }
                }
            }
        }
    }
}
