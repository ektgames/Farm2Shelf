using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Farm2Shelf Gerçek Zamanlı Gece-Gündüz Döngüsü Yöneticisi (Day-Night Cycle).
    /// Oyun saatine (TimeManager.Instance.Hour & Minute) göre güneş açısını, ışık rengini,
    /// ortam aydınlatmasını (Ambient Light), sokak lambalarını, mağaza içi tüm odaları,
    /// çevre binaların camlarını ve araç farlarını dinamik olarak yönetir.
    /// </summary>
    public class DayNightCycleManager : MonoBehaviour
    {
        public static DayNightCycleManager Instance { get; private set; }

        [Header("Işık ve Nesne Kayıtları")]
        private Light directionalSunLight;
        private readonly List<Light> streetPointLights = new List<Light>();
        private readonly List<Renderer> streetLampBulbs = new List<Renderer>();
        private readonly List<Light> storeInteriorLights = new List<Light>();
        private readonly List<Light> playerInteriorLights = new List<Light>();
        private readonly List<Light> vehicleHeadlights = new List<Light>();
        private readonly List<Renderer> headlightRenderers = new List<Renderer>();
        private readonly List<Renderer> buildingWindows = new List<Renderer>();
        private readonly List<VehicleHeadlightController> vehicleHeadlightControllers = new List<VehicleHeadlightController>();

        [Header("Materyaller")]
        private Material bulbOnMat;
        private Material bulbOffMat;
        private Material windowGlowOnMat;
        private Material windowGlowOffMat;
        private Material headlightOnMat;
        private Material headlightOffMat;

        public static Material HeadlightOnMaterial => Instance != null ? Instance.headlightOnMat : null;
        public static Material HeadlightOffMaterial => Instance != null ? Instance.headlightOffMat : null;
        public static Material WindowGlowOnMaterial => Instance != null ? Instance.windowGlowOnMat : null;
        public static Material WindowGlowOffMaterial => Instance != null ? Instance.windowGlowOffMat : null;
        public bool IsNight => isNight;

        private bool isNight = false;
        private float nextLightingUpdateTime;
        private const float LIGHTING_UPDATE_INTERVAL = 0.25f;
        private readonly HashSet<int> configuredLightIds = new HashSet<int>();
        private readonly HashSet<int> playerInteriorIds = new HashSet<int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateMaterials();
        }

        private void Start()
        {
            FindOrCreateSun();
            ScanAndCollectSceneNightObjects();
            UpdateLightingImmediate();
        }

        private void FindOrCreateSun()
        {
            GameObject sunObj = GameObject.Find("Directional Light");
            if (sunObj == null)
            {
                sunObj = new GameObject("Directional Light");
                directionalSunLight = sunObj.AddComponent<Light>();
                directionalSunLight.type = LightType.Directional;
            }
            else
            {
                directionalSunLight = sunObj.GetComponent<Light>();
            }

            if (directionalSunLight != null)
            {
                directionalSunLight.shadows = LightShadows.Soft;
            }
        }

        private void CreateMaterials()
        {
            if (bulbOnMat != null && windowGlowOnMat != null) return;

            Shader litShader = ShaderHelper.GetLitShader() ?? Shader.Find("Standard");

            // 1. Sokak Lamba Ampulü — Unlit opak (mobilde magenta/bloom yok)
            Color bulbGlow = new Color(1.0f, 0.88f, 0.48f, 1.0f);
            bulbOnMat = ShaderHelper.CreateUnlitOpaqueMaterial(bulbGlow, "LampBulb_ON");

            bulbOffMat = ShaderHelper.CreateLitMaterial(new Color(0.35f, 0.35f, 0.38f, 1.0f), "LampBulb_OFF");
            if (bulbOffMat != null && bulbOffMat.HasProperty("_Smoothness")) bulbOffMat.SetFloat("_Smoothness", 0.35f);

            // 2. Çevre bina camları — sıcak amber Unlit. Emission yok (mobilde pembe hata shader'ı).
            Color windowGlow = new Color(1.0f, 0.78f, 0.32f, 1.0f);
            windowGlowOnMat = ShaderHelper.CreateUnlitOpaqueMaterial(windowGlow, "WindowGlass_ON");

            windowGlowOffMat = ShaderHelper.CreateLitMaterial(new Color(0.16f, 0.24f, 0.36f, 1.0f), "WindowGlass_OFF");
            if (windowGlowOffMat != null)
            {
                if (windowGlowOffMat.HasProperty("_Metallic")) windowGlowOffMat.SetFloat("_Metallic", 0.05f);
                if (windowGlowOffMat.HasProperty("_Smoothness")) windowGlowOffMat.SetFloat("_Smoothness", 0.45f);
            }

            // 3. Araba farları
            Color headlightGlow = new Color(1.0f, 0.96f, 0.86f, 1.0f);
            headlightOnMat = ShaderHelper.CreateUnlitOpaqueMaterial(headlightGlow, "Headlight_ON");

            headlightOffMat = ShaderHelper.CreateLitMaterial(new Color(0.85f, 0.85f, 0.88f, 1.0f), "Headlight_OFF");

            if (litShader == null)
            {
                Debug.LogWarning("[DayNightCycleManager] Lit shader bulunamadı; gece yüzeyleri Unlit ile devam ediyor.");
            }
        }

        private static void PrepareNightSurfaceRenderer(Renderer r)
        {
            if (r == null) return;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        public void RegisterStreetLamp(GameObject bulbObj, Light pLight)
        {
            ConfigureAndTrackLight(pLight);
            if (pLight != null && !streetPointLights.Contains(pLight))
            {
                streetPointLights.Add(pLight);
                pLight.enabled = isNight;
            }
            if (bulbObj != null)
            {
                Renderer r = bulbObj.GetComponent<Renderer>();
                if (r != null && !streetLampBulbs.Contains(r))
                {
                    PrepareNightSurfaceRenderer(r);
                    streetLampBulbs.Add(r);
                    if (isNight && bulbOnMat != null) r.sharedMaterial = bulbOnMat;
                }
            }
        }

        public void RegisterStoreInteriorLight(Light iLight)
        {
            ConfigureAndTrackLight(iLight);
            if (iLight != null && !storeInteriorLights.Contains(iLight))
            {
                storeInteriorLights.Add(iLight);
                iLight.enabled = isNight;
            }
        }

        public void RegisterPlayerInteriorLight(Light iLight)
        {
            if (iLight == null) return;
            LightingPipelineBinder.ConfigurePlayerInteriorLight(iLight);
            playerInteriorIds.Add(iLight.GetInstanceID());
            if (!playerInteriorLights.Contains(iLight))
            {
                playerInteriorLights.Add(iLight);
            }
            iLight.enabled = true;
        }

        public void RegisterVehicleHeadlightController(VehicleHeadlightController ctrl)
        {
            if (ctrl != null && !vehicleHeadlightControllers.Contains(ctrl))
            {
                vehicleHeadlightControllers.Add(ctrl);
                ctrl.UpdateHeadlights();
            }
        }

        public void RegisterVehicleHeadlight(Light sLight, GameObject hlObj = null)
        {
            ConfigureAndTrackLight(sLight);
            if (sLight != null && !vehicleHeadlights.Contains(sLight))
            {
                vehicleHeadlights.Add(sLight);
                sLight.enabled = isNight;
            }
            if (hlObj != null)
            {
                Renderer r = hlObj.GetComponent<Renderer>();
                if (r != null && !headlightRenderers.Contains(r))
                {
                    PrepareNightSurfaceRenderer(r);
                    headlightRenderers.Add(r);
                    if (isNight && headlightOnMat != null) r.sharedMaterial = headlightOnMat;
                }
            }
        }

        public void RegisterBuildingWindow(GameObject glassObj)
        {
            if (glassObj != null)
            {
                Renderer r = glassObj.GetComponent<Renderer>();
                if (r != null && !buildingWindows.Contains(r))
                {
                    PrepareNightSurfaceRenderer(r);
                    buildingWindows.Add(r);
                    if (isNight && windowGlowOnMat != null) r.sharedMaterial = windowGlowOnMat;
                }
            }
        }

        public void RegisterApartmentWindow(GameObject winObj, bool isLitTonight)
        {
            if (winObj == null) return;
            Renderer r = winObj.GetComponent<Renderer>();
            if (r == null) return;

            if (isLitTonight)
            {
                PrepareNightSurfaceRenderer(r);
                if (!buildingWindows.Contains(r))
                {
                    buildingWindows.Add(r);
                }
                if (isNight && windowGlowOnMat != null)
                {
                    r.sharedMaterial = windowGlowOnMat;
                }
            }
            else
            {
                if (windowGlowOffMat != null)
                {
                    r.sharedMaterial = windowGlowOffMat;
                }
            }
        }

        public void ClearStoreInteriorLights()
        {
            storeInteriorLights.Clear();
        }

        public void ClearPlayerInteriorLights()
        {
            for (int i = 0; i < playerInteriorLights.Count; i++)
            {
                if (playerInteriorLights[i] != null)
                {
                    playerInteriorIds.Remove(playerInteriorLights[i].GetInstanceID());
                }
            }
            playerInteriorLights.Clear();
        }

        private void ScanAndCollectSceneNightObjects()
        {
            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in allRenderers)
            {
                if (r == null || r.gameObject == null) continue;
                string n = r.gameObject.name;
                if ((n.Contains("Window_Glass_Pane") || n.Contains("Apartment_Window_Glass_Lit")) && !buildingWindows.Contains(r))
                {
                    if (n.Contains("Apartment_Window_Glass_Lit"))
                    {
                        PrepareNightSurfaceRenderer(r);
                        buildingWindows.Add(r);
                    }
                    else if (Random.value < 0.70f)
                    {
                        PrepareNightSurfaceRenderer(r);
                        buildingWindows.Add(r);
                    }
                }
            }

            Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < allLights.Length; i++)
            {
                Light l = allLights[i];
                if (l == null || l.type == LightType.Directional) continue;
                ConfigureAndTrackLight(l);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextLightingUpdateTime) return;
            nextLightingUpdateTime = Time.unscaledTime + LIGHTING_UPDATE_INTERVAL;
            UpdateLightingImmediate();
        }

        public void RefreshLightingNow()
        {
            UpdateLightingImmediate();
        }

        private void UpdateLightingImmediate()
        {
            if (TimeManager.Instance == null) return;

            float hour = TimeManager.Instance.Hour;
            float minute = TimeManager.Instance.Minute;
            float timeInHours = hour + (minute / 60.0f); // 0.0 - 24.0 arası saat

            // 1. Güneş Rotasyonu (06:00 Doğuş, 12:00 Tepe, 19:30 Batış)
            float sunAngleX;
            float sunAngleY = -30.0f;

            if (timeInHours >= 6.0f && timeInHours <= 19.5f)
            {
                float dayProgress = (timeInHours - 6.0f) / 13.5f; // 0.0 to 1.0
                sunAngleX = Mathf.Sin(dayProgress * Mathf.PI) * 55.0f + 15.0f;
                sunAngleY = Mathf.Lerp(-60.0f, 60.0f, dayProgress);
            }
            else
            {
                sunAngleX = -35.0f;
                sunAngleY = -30.0f;
            }

            if (directionalSunLight != null)
            {
                directionalSunLight.transform.rotation = Quaternion.Euler(sunAngleX, sunAngleY, 0f);
            }

            // 2. Güneş Işık Yoğunluğu & Rengi, Ortam Işığı (Ambient Light)
            Color sunColor;
            Color skyAmbientColor;
            float sunIntensity;

            if (timeInHours >= 6.0f && timeInHours < 8.0f)
            {
                // GÜNDOĞUMU (06:00 - 08:00)
                float t = (timeInHours - 6.0f) / 2.0f;
                sunColor = Color.Lerp(new Color(1.0f, 0.50f, 0.25f), new Color(1.0f, 0.95f, 0.82f), t);
                skyAmbientColor = Color.Lerp(new Color(0.25f, 0.20f, 0.35f), new Color(0.60f, 0.72f, 0.88f), t);
                sunIntensity = Mathf.Lerp(0.20f, 1.25f, t);
            }
            else if (timeInHours >= 8.0f && timeInHours < 18.0f)
            {
                // TAM GÜNDÜZ (08:00 - 18:00)
                sunColor = new Color(1.0f, 0.96f, 0.90f);
                skyAmbientColor = new Color(0.65f, 0.78f, 0.92f);
                sunIntensity = 1.30f;
            }
            else if (timeInHours >= 18.0f && timeInHours < 20.0f)
            {
                // GÜNBATIMI (18:00 - 20:00)
                float t = (timeInHours - 18.0f) / 2.0f;
                sunColor = Color.Lerp(new Color(1.0f, 0.90f, 0.70f), new Color(0.95f, 0.35f, 0.15f), t);
                skyAmbientColor = Color.Lerp(new Color(0.65f, 0.78f, 0.92f), new Color(0.22f, 0.16f, 0.35f), t);
                sunIntensity = Mathf.Lerp(1.30f, 0.15f, t);
            }
            else
            {
                // GECE (20:00 - 06:00)
                sunColor = new Color(0.28f, 0.36f, 0.58f);
                skyAmbientColor = LightingPipelineBinder.IsMobileLightingProfile()
                    ? new Color(0.16f, 0.18f, 0.30f)
                    : new Color(0.12f, 0.14f, 0.26f);
                sunIntensity = LightingPipelineBinder.IsMobileLightingProfile() ? 0.22f : 0.16f;
            }

            ApplyWeatherAtmosphere(timeInHours, ref sunColor, ref skyAmbientColor, ref sunIntensity);

            if (directionalSunLight != null)
            {
                directionalSunLight.color = sunColor;
                directionalSunLight.intensity = sunIntensity;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = skyAmbientColor;

            // 3. Gece Lambaları, Mağaza İçi Odalar, Cam Işıkları ve Araç Farlarının Açılıp Kapanması (19:30 - 06:30 arası Açık)
            bool shouldNightLightsBeOn = (timeInHours >= 19.5f || timeInHours < 6.5f);

            if (isNight != shouldNightLightsBeOn)
            {
                isNight = shouldNightLightsBeOn;
                ToggleNightLights(isNight);
            }
        }

        private static void ApplyWeatherAtmosphere(float timeInHours, ref Color sunColor, ref Color skyAmbientColor, ref float sunIntensity)
        {
            if (WeatherManager.Instance == null) return;

            bool isDay = timeInHours >= 6.5f && timeInHours < 19.5f;
            WeatherType weather = WeatherManager.Instance.CurrentWeather;

            if (weather == WeatherType.Rainy)
            {
                sunColor = Color.Lerp(sunColor, new Color(0.58f, 0.64f, 0.72f), isDay ? 0.70f : 0.35f);
                skyAmbientColor = Color.Lerp(skyAmbientColor, new Color(0.36f, 0.40f, 0.46f), isDay ? 0.62f : 0.28f);
                sunIntensity *= isDay ? 0.58f : 0.82f;
                sunIntensity = Mathf.Max(sunIntensity, isDay ? 0.42f : 0.10f);

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.40f, 0.44f, 0.50f);
                RenderSettings.fogDensity = 0.0115f;
            }
            else if (weather == WeatherType.Snowy)
            {
                sunColor = Color.Lerp(sunColor, new Color(0.86f, 0.90f, 0.96f), isDay ? 0.48f : 0.22f);
                skyAmbientColor = Color.Lerp(skyAmbientColor, new Color(0.70f, 0.76f, 0.84f), isDay ? 0.42f : 0.18f);
                sunIntensity *= isDay ? 0.80f : 0.90f;
                sunIntensity = Mathf.Max(sunIntensity, isDay ? 0.55f : 0.11f);

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.76f, 0.82f, 0.88f);
                RenderSettings.fogDensity = 0.0075f;
            }
            else
            {
                RenderSettings.fog = false;
            }
        }

        public void ApplyPlayerInteriorBrandTint(Color tint)
        {
            for (int i = playerInteriorLights.Count - 1; i >= 0; i--)
            {
                if (playerInteriorLights[i] == null)
                {
                    playerInteriorLights.RemoveAt(i);
                    continue;
                }

                playerInteriorLights[i].color = tint;
            }
        }

        private void ToggleNightLights(bool turnOn)
        {
            SetListLightsEnabled(streetPointLights, turnOn);
            SetListLightsEnabled(storeInteriorLights, turnOn);
            SetListLightsEnabled(vehicleHeadlights, turnOn);
            SetListLightsEnabled(playerInteriorLights, true);

            Material targetBulbMat = turnOn ? bulbOnMat : bulbOffMat;
            foreach (var r in streetLampBulbs)
            {
                if (r != null && targetBulbMat != null) r.sharedMaterial = targetBulbMat;
            }

            Material targetWinMat = turnOn ? windowGlowOnMat : windowGlowOffMat;
            foreach (var r in buildingWindows)
            {
                if (r != null && targetWinMat != null) r.sharedMaterial = targetWinMat;
            }

            vehicleHeadlightControllers.RemoveAll(c => c == null);
            foreach (var ctrl in vehicleHeadlightControllers)
            {
                if (ctrl != null) ctrl.UpdateHeadlights();
            }

            Material targetHlMat = turnOn ? headlightOnMat : headlightOffMat;
            foreach (var r in headlightRenderers)
            {
                if (r != null && targetHlMat != null) r.sharedMaterial = targetHlMat;
            }
        }

        private static void SetListLightsEnabled(List<Light> lights, bool enabled)
        {
            for (int i = lights.Count - 1; i >= 0; i--)
            {
                if (lights[i] == null)
                {
                    lights.RemoveAt(i);
                    continue;
                }
                lights[i].enabled = enabled;
            }
        }

        private void ConfigureAndTrackLight(Light light)
        {
            if (light == null) return;
            int id = light.GetInstanceID();
            if (playerInteriorIds.Contains(id)) return;
            if (configuredLightIds.Add(id))
            {
                LightingPipelineBinder.ConfigureRealtimeLight(light);
            }
        }
    }
}
