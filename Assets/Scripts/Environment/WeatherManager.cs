using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    public enum WeatherType
    {
        Sunny,
        Rainy,
        Snowy
    }

    /// <summary>
    /// Mevsimsel hava: kasvetli ama okunabilir yağmur, ıslak zemin, şeffaf yağmur perdesi;
    /// kar örtüsü ve yollarda tekerlek izleri; ara ara görünen kar taneleri.
    /// </summary>
    public class WeatherManager : MonoBehaviour
    {
        public static WeatherManager Instance { get; private set; }

        public WeatherType CurrentWeather { get; private set; } = WeatherType.Sunny;

        public event Action<WeatherType> OnWeatherChanged;

        private ParticleSystem worldRainSys;
        private ParticleSystem screenRainSys;
        private ParticleSystem worldSnowSys;
        private ParticleSystem screenSnowSys;
        private Transform worldFxAnchor;
        private Transform screenFxAnchor;

        private Material rainParticleMat;
        private Material snowParticleMat;
        private Material snowRoadOverlayMat;
        private Texture2D rainStreakTex;
        private Texture2D snowFlakeTex;
        private Texture2D snowRoadTex;

        private readonly Dictionary<int, MaterialSnapshot> materialBackup = new Dictionary<int, MaterialSnapshot>();
        private readonly List<GameObject> snowRoadOverlays = new List<GameObject>();
        private bool originalsCaptured;

        private struct MaterialSnapshot
        {
            public Color Color;
            public float Smoothness;
            public float Metallic;
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
            }
        }

        private void Start()
        {
            BuildFxAssets();
            CreateParticleSystems();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateUpdated -= HandleDateUpdated;
                TimeManager.Instance.OnDateUpdated += HandleDateUpdated;
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
            ClearSnowRoadOverlays();
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;
            Vector3 fwd = cam.transform.forward;
            Vector3 right = cam.transform.right;

            if (worldFxAnchor != null)
            {
                Vector3 look = new Vector3(fwd.x, 0f, fwd.z);
                if (look.sqrMagnitude < 0.01f) look = Vector3.forward;
                look.Normalize();
                worldFxAnchor.position = camPos + look * 14f + Vector3.up * 16f;
            }

            if (screenFxAnchor != null)
            {
                screenFxAnchor.position = camPos + fwd * 6.5f + Vector3.up * 3.2f + right * 0.4f;
                screenFxAnchor.rotation = Quaternion.identity;
            }

            AnimateSnowFlurries();
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
            int curDay = TimeManager.Instance != null ? TimeManager.Instance.Day : 1;
            int curYear = TimeManager.Instance != null ? TimeManager.Instance.Year : 1;
            SetWeather(GetWeatherForecastForDay(season, curDay, curYear));
        }

        public void SetWeather(WeatherType weather)
        {
            CurrentWeather = weather;
            UpdateParticlePlayback();
            CaptureOriginalMaterialsIfNeeded();
            ApplyEnvironmentSurfaces();
            RebuildSnowRoadOverlays(weather == WeatherType.Snowy);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RefreshLightingNow();
            }

            OnWeatherChanged?.Invoke(CurrentWeather);
        }

        private void BuildFxAssets()
        {
            rainStreakTex = BuildRainStreakTexture();
            snowFlakeTex = BuildSnowflakeTexture();
            snowRoadTex = BuildSnowRoadTexture();

            Shader particleShader = FindParticleShader();
            rainParticleMat = CreateTransparentParticleMaterial(particleShader, "Weather_RainMat", rainStreakTex);
            snowParticleMat = CreateTransparentParticleMaterial(particleShader, "Weather_SnowMat", snowFlakeTex);

            Shader lit = ShaderHelper.GetLitShader() ?? Shader.Find("Standard");
            snowRoadOverlayMat = new Material(lit != null ? lit : particleShader);
            snowRoadOverlayMat.name = "Weather_SnowRoadOverlayMat";
            snowRoadOverlayMat.mainTexture = snowRoadTex;
            Color snowTint = Color.white;
            snowRoadOverlayMat.color = snowTint;
            if (snowRoadOverlayMat.HasProperty("_BaseMap")) snowRoadOverlayMat.SetTexture("_BaseMap", snowRoadTex);
            if (snowRoadOverlayMat.HasProperty("_BaseColor")) snowRoadOverlayMat.SetColor("_BaseColor", snowTint);
            if (snowRoadOverlayMat.HasProperty("_Smoothness")) snowRoadOverlayMat.SetFloat("_Smoothness", 0.22f);
            if (snowRoadOverlayMat.HasProperty("_Metallic")) snowRoadOverlayMat.SetFloat("_Metallic", 0.02f);
            snowRoadOverlayMat.renderQueue = 2450;
        }

        private static Shader FindParticleShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Particles/Standard Unlit");
            if (s == null) s = Shader.Find("Unlit/Transparent");
            if (s == null) s = ShaderHelper.GetUnlitShader();
            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }

        private static Material CreateTransparentParticleMaterial(Shader shader, string name, Texture2D tex)
        {
            Material mat = new Material(shader) { name = name };
            mat.mainTexture = tex;
            Color c = new Color(1f, 1f, 1f, 1f);
            mat.color = c;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3200;
            return mat;
        }

        private void CreateParticleSystems()
        {
            GameObject worldRoot = new GameObject("Weather_WorldFx");
            worldRoot.transform.SetParent(transform, false);
            worldFxAnchor = worldRoot.transform;

            GameObject screenRoot = new GameObject("Weather_ScreenFx");
            screenRoot.transform.SetParent(transform, false);
            screenFxAnchor = screenRoot.transform;

            worldRainSys = BuildRainSystem(worldRoot.transform, "WorldRain", new Vector3(48f, 36f, 1.2f), 720f, 22f, 1.15f, 0.028f, 4.2f, 1100);
            screenRainSys = BuildRainSystem(screenRoot.transform, "ScreenRain", new Vector3(11f, 9f, 5f), 220f, 16f, 0.55f, 0.022f, 2.6f, 420);

            worldSnowSys = BuildSnowSystem(worldRoot.transform, "WorldSnow", new Vector3(42f, 42f, 1.5f), 55f, 1.7f, 9.5f, 0.11f, 0.22f, 480);
            screenSnowSys = BuildSnowSystem(screenRoot.transform, "ScreenSnow", new Vector3(9f, 7f, 4f), 9f, 1.15f, 4.2f, 0.09f, 0.20f, 70);

            StopAllFx();
        }

        private ParticleSystem BuildRainSystem(Transform parent, string name, Vector3 box, float rate, float speed, float life, float width, float stretch, int maxParticles)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(78f, 14f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.75f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.82f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(width * 0.7f, width);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.78f, 0.86f, 0.95f, 0.18f),
                new Color(0.88f, 0.93f, 1.0f, 0.38f));
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.65f;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.4f;
            noise.octaveCount = 1;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.0f, 0f),
                    new GradientAlphaKey(1.0f, 0.12f),
                    new GradientAlphaKey(0.85f, 0.7f),
                    new GradientAlphaKey(0.0f, 1f)
                });
            col.color = g;

            ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Stretch;
            rend.velocityScale = 0.08f;
            rend.lengthScale = stretch;
            rend.cameraVelocityScale = 0f;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.sharedMaterial = rainParticleMat;
            rend.maxParticleSize = 0.35f;
            return ps;
        }

        private ParticleSystem BuildSnowSystem(Transform parent, string name, Vector3 box, float rate, float speed, float life, float sizeMin, float sizeMax, int maxParticles)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(88f, 0f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.96f, 0.98f, 1f, 0.55f),
                new Color(1f, 1f, 1f, 0.92f));
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.08f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.55f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.55f;
            noise.frequency = 0.22f;
            noise.scrollSpeed = 0.12f;
            noise.octaveCount = 2;
            noise.damping = true;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.15f, 1f),
                new Keyframe(0.85f, 1f),
                new Keyframe(1f, 0.2f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.18f),
                    new GradientAlphaKey(0.9f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = g;

            ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.sharedMaterial = snowParticleMat;
            rend.maxParticleSize = 0.22f;
            return ps;
        }

        private void AnimateSnowFlurries()
        {
            if (CurrentWeather != WeatherType.Snowy || worldSnowSys == null) return;

            float pulse = Mathf.PerlinNoise(Time.time * 0.07f, 1.7f);
            float screenPulse = Mathf.PerlinNoise(Time.time * 0.11f, 4.2f);
            var worldEm = worldSnowSys.emission;
            worldEm.rateOverTime = Mathf.Lerp(18f, 95f, pulse);
            if (screenSnowSys != null)
            {
                var screenEm = screenSnowSys.emission;
                screenEm.rateOverTime = Mathf.Lerp(3f, 16f, screenPulse);
            }
        }

        private void UpdateParticlePlayback()
        {
            bool rain = CurrentWeather == WeatherType.Rainy;
            bool snow = CurrentWeather == WeatherType.Snowy;
            SetPlaying(worldRainSys, rain);
            SetPlaying(screenRainSys, rain);
            SetPlaying(worldSnowSys, snow);
            SetPlaying(screenSnowSys, snow);
        }

        private static void SetPlaying(ParticleSystem ps, bool play)
        {
            if (ps == null) return;
            if (play)
            {
                if (!ps.isPlaying) ps.Play();
            }
            else if (ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void StopAllFx()
        {
            SetPlaying(worldRainSys, false);
            SetPlaying(screenRainSys, false);
            SetPlaying(worldSnowSys, false);
            SetPlaying(screenSnowSys, false);
        }

        private void CaptureOriginalMaterialsIfNeeded()
        {
            if (originalsCaptured) return;
            originalsCaptured = true;

            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material mat = renderers[i] != null ? renderers[i].sharedMaterial : null;
                if (mat == null) continue;
                int id = mat.GetInstanceID();
                if (materialBackup.ContainsKey(id)) continue;
                if (!IsOutdoorSurface(mat.name)) continue;

                MaterialSnapshot snap = new MaterialSnapshot
                {
                    Color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color,
                    Smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.2f,
                    Metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f
                };
                materialBackup[id] = snap;
            }
        }

        private void ApplyEnvironmentSurfaces()
        {
            bool rain = CurrentWeather == WeatherType.Rainy;
            bool snow = CurrentWeather == WeatherType.Snowy;

            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r.sharedMaterial == null) continue;
                Material mat = r.sharedMaterial;
                string n = mat.name;

                if (!materialBackup.TryGetValue(mat.GetInstanceID(), out MaterialSnapshot orig))
                {
                    if (!IsOutdoorSurface(n)) continue;
                    orig = new MaterialSnapshot
                    {
                        Color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color,
                        Smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.2f,
                        Metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f
                    };
                    materialBackup[mat.GetInstanceID()] = orig;
                }

                Color color = orig.Color;
                float smooth = orig.Smoothness;
                float metal = orig.Metallic;

                if (snow)
                {
                    ApplySnowColor(n, orig.Color, out color, out smooth, out metal);
                }
                else if (rain)
                {
                    ApplyRainColor(n, orig.Color, orig.Smoothness, orig.Metallic, out color, out smooth, out metal);
                }

                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smooth);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metal);
            }
        }

        private static bool IsOutdoorSurface(string n)
        {
            return n.Contains("MainRoadMat") || n.Contains("SidewalkMat") || n.Contains("TownSquareMat")
                || n.Contains("GrassMat") || n.Contains("Roof") || n.Contains("FootpathMat")
                || n.Contains("LoadingZoneMat") || n.Contains("TreeFoliageMat")
                || n.Contains("SoilPlotMat") || n.Contains("PondStoneMat") || n.Contains("FenceWoodMat")
                || n.Contains("RoadLineMat") || n.Contains("CrosswalkMat") || n.Contains("ParkingLineMat");
        }

        private static void ApplyRainColor(string n, Color orig, float origSmooth, float origMetal, out Color color, out float smooth, out float metal)
        {
            color = orig;
            smooth = origSmooth;
            metal = origMetal;

            if (n.Contains("MainRoadMat") || n.Contains("LoadingZoneMat"))
            {
                color = Color.Lerp(orig, new Color(0.09f, 0.11f, 0.13f), 0.72f);
                smooth = 0.88f;
                metal = 0.18f;
            }
            else if (n.Contains("SidewalkMat") || n.Contains("TownSquareMat") || n.Contains("FootpathMat") || n.Contains("PondStoneMat"))
            {
                color = Color.Lerp(orig, new Color(0.38f, 0.42f, 0.46f), 0.55f);
                smooth = 0.72f;
                metal = 0.08f;
            }
            else if (n.Contains("GrassMat"))
            {
                color = Color.Lerp(orig, new Color(0.14f, 0.32f, 0.18f), 0.55f);
                smooth = 0.28f;
            }
            else if (n.Contains("SoilPlotMat"))
            {
                color = Color.Lerp(orig, new Color(0.16f, 0.11f, 0.07f), 0.45f);
                smooth = 0.35f;
            }
            else if (n.Contains("TreeFoliageMat"))
            {
                color = Color.Lerp(orig, new Color(0.10f, 0.32f, 0.14f), 0.4f);
                smooth = 0.22f;
            }
            else if (n.Contains("Roof"))
            {
                color = Color.Lerp(orig, orig * 0.72f, 0.5f);
                smooth = 0.62f;
                metal = 0.12f;
            }
        }

        private static void ApplySnowColor(string n, Color orig, out Color color, out float smooth, out float metal)
        {
            color = orig;
            smooth = 0.18f;
            metal = 0.02f;

            if (n.Contains("MainRoadMat") || n.Contains("LoadingZoneMat"))
            {
                color = new Color(0.72f, 0.76f, 0.80f);
                smooth = 0.16f;
            }
            else if (n.Contains("RoadLineMat") || n.Contains("CrosswalkMat") || n.Contains("ParkingLineMat"))
            {
                color = new Color(0.90f, 0.93f, 0.96f);
                smooth = 0.12f;
            }
            else if (n.Contains("GrassMat") || n.Contains("SoilPlotMat"))
            {
                color = new Color(0.91f, 0.94f, 0.97f);
                smooth = 0.12f;
            }
            else if (n.Contains("SidewalkMat") || n.Contains("TownSquareMat") || n.Contains("FootpathMat") || n.Contains("PondStoneMat"))
            {
                color = new Color(0.90f, 0.93f, 0.96f);
                smooth = 0.20f;
            }
            else if (n.Contains("Roof"))
            {
                color = new Color(0.94f, 0.96f, 0.99f);
                smooth = 0.24f;
            }
            else if (n.Contains("TreeFoliageMat"))
            {
                color = new Color(0.82f, 0.88f, 0.90f);
                smooth = 0.14f;
            }
            else if (n.Contains("FenceWoodMat"))
            {
                color = Color.Lerp(orig, new Color(0.86f, 0.90f, 0.93f), 0.65f);
            }
        }

        private void RebuildSnowRoadOverlays(bool enable)
        {
            ClearSnowRoadOverlays();
            if (!enable || snowRoadOverlayMat == null) return;

            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r.sharedMaterial == null) continue;
                if (!r.sharedMaterial.name.Contains("MainRoadMat")) continue;
                if (!IsDriveLaneRenderer(r)) continue;

                GameObject overlay = CreateSnowTrackOverlay(r.transform);
                if (overlay != null) snowRoadOverlays.Add(overlay);
            }
        }

        private static bool IsDriveLaneRenderer(Renderer r)
        {
            string n = r.gameObject.name;
            if (n.IndexOf("Parking", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (n.IndexOf("Stall", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (n.IndexOf("Line", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (n.IndexOf("Marking", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (n.IndexOf("Crosswalk", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            Vector3 s = r.transform.lossyScale;
            float min = Mathf.Min(s.x, s.z);
            float max = Mathf.Max(s.x, s.z);
            return min >= 3.5f && max >= 8f;
        }

        private GameObject CreateSnowTrackOverlay(Transform road)
        {
            Vector3 lossy = road.lossyScale;
            bool alongX = lossy.x >= lossy.z;

            GameObject go = new GameObject("Snow_Road_TireTracks");
            go.transform.SetParent(road, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            Mesh mesh = new Mesh { name = "SnowRoadOverlayMesh" };
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0.82f, -0.5f),
                new Vector3(0.5f, 0.82f, -0.5f),
                new Vector3(0.5f, 0.82f, 0.5f),
                new Vector3(-0.5f, 0.82f, 0.5f)
            };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };

            if (alongX)
            {
                mesh.uv = new Vector2[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, Mathf.Max(1f, lossy.x / 8f)),
                    new Vector2(1f, Mathf.Max(1f, lossy.x / 8f)),
                    new Vector2(1f, 0f)
                };
            }
            else
            {
                mesh.uv = new Vector2[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, Mathf.Max(1f, lossy.z / 8f)),
                    new Vector2(0f, Mathf.Max(1f, lossy.z / 8f))
                };
            }

            mesh.RecalculateBounds();

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = snowRoadOverlayMat;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            return go;
        }

        private void ClearSnowRoadOverlays()
        {
            for (int i = 0; i < snowRoadOverlays.Count; i++)
            {
                if (snowRoadOverlays[i] != null) Destroy(snowRoadOverlays[i]);
            }
            snowRoadOverlays.Clear();
        }

        private static Texture2D BuildRainStreakTexture()
        {
            const int w = 8;
            const int h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);
                float shaft = Mathf.Exp(-Mathf.Pow((v - 0.55f) * 3.2f, 2f));
                float fade = Mathf.SmoothStep(0f, 1f, v) * (1f - Mathf.SmoothStep(0.75f, 1f, v));
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    float radial = 1f - Mathf.Abs(u - 0.5f) * 2.4f;
                    radial = Mathf.Clamp01(radial);
                    float a = shaft * fade * radial * 0.85f;
                    px[y * w + x] = new Color(0.86f, 0.92f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.name = "Weather_RainStreakTex";
            return tex;
        }

        private static Texture2D BuildSnowflakeTexture()
        {
            const int s = 48;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[s * s];
            float cx = (s - 1) * 0.5f;
            float cy = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (s * 0.48f);
                    float blob = Mathf.Clamp01(1f - dist);
                    blob = blob * blob;
                    float arms = 0f;
                    float ang = Mathf.Atan2(dy, dx);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * Mathf.PI / 3f;
                        float along = Mathf.Abs(Mathf.Cos(ang - a));
                        arms = Mathf.Max(arms, (1f - dist) * Mathf.Pow(along, 8f));
                    }
                    float aOut = Mathf.Clamp01(blob * 0.9f + arms * 0.55f);
                    px[y * s + x] = new Color(1f, 1f, 1f, aOut);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.name = "Weather_SnowflakeTex";
            return tex;
        }

        private static Texture2D BuildSnowRoadTexture()
        {
            const int w = 128;
            const int h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[w * h];
            Color snow = new Color(0.93f, 0.96f, 0.99f, 1f);
            Color slush = new Color(0.70f, 0.74f, 0.78f, 1f);
            Color asphalt = new Color(0.16f, 0.17f, 0.19f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    float v = y / (float)(h - 1);
                    float n = Mathf.PerlinNoise(u * 18f, v * 6f);
                    float rutL = RutMask(u, 0.33f, 0.07f);
                    float rutR = RutMask(u, 0.67f, 0.07f);
                    float rut = Mathf.Max(rutL, rutR);
                    rut *= 0.88f + n * 0.14f;
                    Color c = Color.Lerp(snow, slush, n * 0.28f);
                    c = Color.Lerp(c, asphalt, rut);
                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.name = "Weather_SnowRoadTex";
            return tex;
        }

        private static float RutMask(float u, float center, float halfWidth)
        {
            float d = Mathf.Abs(u - center);
            return 1f - Mathf.SmoothStep(halfWidth * 0.35f, halfWidth, d);
        }
    }
}
