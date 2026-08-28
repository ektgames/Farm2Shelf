using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

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

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Sokak Lamba Ampulü (Gece Yanan Sıcak Sarı)
            bulbOnMat = new Material(shader) { name = "LampBulb_ON", color = new Color(1.0f, 0.88f, 0.45f) };
            if (bulbOnMat.HasProperty("_BaseColor")) bulbOnMat.SetColor("_BaseColor", new Color(1.0f, 0.88f, 0.45f));
            if (bulbOnMat.HasProperty("_Color")) bulbOnMat.SetColor("_Color", new Color(1.0f, 0.88f, 0.45f));
            if (bulbOnMat.HasProperty("_EmissionColor"))
            {
                bulbOnMat.SetColor("_EmissionColor", new Color(1.0f, 0.88f, 0.45f) * 3.5f);
                bulbOnMat.EnableKeyword("_EMISSION");
            }
            bulbOnMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            bulbOffMat = new Material(shader) { name = "LampBulb_OFF", color = new Color(0.35f, 0.35f, 0.38f) };
            if (bulbOffMat.HasProperty("_BaseColor")) bulbOffMat.SetColor("_BaseColor", new Color(0.35f, 0.35f, 0.38f));
            if (bulbOffMat.HasProperty("_Color")) bulbOffMat.SetColor("_Color", new Color(0.35f, 0.35f, 0.38f));

            // 2. Çevre Binaların Camları (Gece İçi Aydınlatmalı Işıyan Sıcak Sarı Cam)
            windowGlowOnMat = new Material(shader) { name = "WindowGlass_ON", color = new Color(1.0f, 0.88f, 0.35f, 1.0f) };
            if (windowGlowOnMat.HasProperty("_BaseColor")) windowGlowOnMat.SetColor("_BaseColor", new Color(1.0f, 0.88f, 0.35f, 1.0f));
            if (windowGlowOnMat.HasProperty("_Color")) windowGlowOnMat.SetColor("_Color", new Color(1.0f, 0.88f, 0.35f, 1.0f));
            if (windowGlowOnMat.HasProperty("_EmissionColor"))
            {
                windowGlowOnMat.SetColor("_EmissionColor", new Color(1.0f, 0.85f, 0.30f) * 1.20f);
                windowGlowOnMat.EnableKeyword("_EMISSION");
            }
            windowGlowOnMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            windowGlowOffMat = new Material(shader) { name = "WindowGlass_OFF", color = new Color(0.20f, 0.35f, 0.50f, 0.90f) };
            if (windowGlowOffMat.HasProperty("_BaseColor")) windowGlowOffMat.SetColor("_BaseColor", new Color(0.20f, 0.35f, 0.50f, 0.90f));
            if (windowGlowOffMat.HasProperty("_Color")) windowGlowOffMat.SetColor("_Color", new Color(0.20f, 0.35f, 0.50f, 0.90f));

            // 3. Araba Farları (Gece Yanan Parlak Beyaz-Sarı)
            headlightOnMat = new Material(shader) { name = "Headlight_ON", color = new Color(1.0f, 0.98f, 0.85f) };
            if (headlightOnMat.HasProperty("_BaseColor")) headlightOnMat.SetColor("_BaseColor", new Color(1.0f, 0.98f, 0.85f));
            if (headlightOnMat.HasProperty("_Color")) headlightOnMat.SetColor("_Color", new Color(1.0f, 0.98f, 0.85f));
            if (headlightOnMat.HasProperty("_EmissionColor"))
            {
                headlightOnMat.SetColor("_EmissionColor", new Color(1.0f, 0.98f, 0.85f) * 3.5f);
                headlightOnMat.EnableKeyword("_EMISSION");
            }
            headlightOnMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            headlightOffMat = new Material(shader) { name = "Headlight_OFF", color = new Color(0.85f, 0.85f, 0.88f) };
            if (headlightOffMat.HasProperty("_BaseColor")) headlightOffMat.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.88f));
            if (headlightOffMat.HasProperty("_Color")) headlightOffMat.SetColor("_Color", new Color(0.85f, 0.85f, 0.88f));
        }

        public void RegisterStreetLamp(GameObject bulbObj, Light pLight)
        {
            if (pLight != null && !streetPointLights.Contains(pLight)) streetPointLights.Add(pLight);
            if (bulbObj != null)
            {
                Renderer r = bulbObj.GetComponent<Renderer>();
                if (r != null && !streetLampBulbs.Contains(r)) streetLampBulbs.Add(r);
            }
        }

        public void RegisterStoreInteriorLight(Light iLight)
        {
            if (iLight != null && !storeInteriorLights.Contains(iLight))
            {
                storeInteriorLights.Add(iLight);
                iLight.enabled = isNight;
            }
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
                        buildingWindows.Add(r);
                    }
                    else if (Random.value < 0.70f)
                    {
                        buildingWindows.Add(r);
                    }
                }
            }
        }

        private void Update()
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
                sunColor = new Color(0.22f, 0.30f, 0.55f);
                skyAmbientColor = new Color(0.08f, 0.10f, 0.22f);
                sunIntensity = 0.12f;
            }

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

        private void ToggleNightLights(bool turnOn)
        {
            // A) Sokak Lambaları Işıkları ve Ampul Materyalleri
            foreach (var pLight in streetPointLights)
            {
                if (pLight != null) pLight.enabled = turnOn;
            }

            Material targetBulbMat = turnOn ? bulbOnMat : bulbOffMat;
            foreach (var r in streetLampBulbs)
            {
                if (r != null && targetBulbMat != null) r.sharedMaterial = targetBulbMat;
            }

            // B) Mağaza İçi Tüm Oda Tavan Işıkları (Canlı ve Parlak Aydınlatma)
            foreach (var iLight in storeInteriorLights)
            {
                if (iLight != null) iLight.enabled = turnOn;
            }

            // C) Binaların Cam Işıkları (Gece Işıldayan Camlar)
            Material targetWinMat = turnOn ? windowGlowOnMat : windowGlowOffMat;
            foreach (var r in buildingWindows)
            {
                if (r != null && targetWinMat != null) r.sharedMaterial = targetWinMat;
            }

            // D) Araç Farları ve Ön Işık Huzmeleri
            vehicleHeadlightControllers.RemoveAll(c => c == null);
            foreach (var ctrl in vehicleHeadlightControllers)
            {
                if (ctrl != null) ctrl.UpdateHeadlights();
            }

            foreach (var vLight in vehicleHeadlights)
            {
                if (vLight != null) vLight.enabled = turnOn;
            }

            Material targetHlMat = turnOn ? headlightOnMat : headlightOffMat;
            foreach (var r in headlightRenderers)
            {
                if (r != null && targetHlMat != null) r.sharedMaterial = targetHlMat;
            }
        }
    }
}
