using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Farm2Shelf.Utils
{
    /// <summary>
    /// URP ek ışık limitlerini ve kamera derinlik dokusunu Play başında sabitler.
    /// Mobil Forward'da nesne başı 4 ışık, kamera kayınca lambaların sönmesine / parlama patlamasına yol açar.
    /// </summary>
    public static class LightingPipelineBinder
    {
        private static bool applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyBeforeSceneLoad()
        {
            Apply();
        }

        public static void Apply()
        {
            QualitySettings.pixelLightCount = 8;
            applied = true;
        }

        public static void ConfigureMainCamera(Camera cam)
        {
            if (cam == null)
            {
                return;
            }

            if (!applied)
            {
                Apply();
            }

            cam.allowHDR = true;
            cam.allowMSAA = false;
            if (cam.farClipPlane < 320f)
            {
                cam.farClipPlane = 320f;
            }

            UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
            if (camData == null)
            {
                return;
            }

            camData.renderPostProcessing = true;
            camData.renderShadows = true;
            camData.requiresDepthOption = CameraOverrideOption.On;
            camData.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
        }

        public static void ConfigureRealtimeLight(Light light)
        {
            if (light == null || light.type == LightType.Directional)
            {
                return;
            }

            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            light.bounceIntensity = 0f;
            light.cullingMask = ~0;

            float mobileBoost = Application.isMobilePlatform ? 1.40f : 1.12f;

            if (light.type == LightType.Spot)
            {
                light.intensity = Mathf.Clamp(light.intensity * mobileBoost, 2.4f, 4.2f);
                light.range = Mathf.Clamp(light.range, 12f, 20f);
                if (light.spotAngle < 50f) light.spotAngle = 60f;
                if (light.innerSpotAngle < 20f) light.innerSpotAngle = 28f;
                return;
            }

            // Kısa menzilli dekor (bollard vb.): zemini görünsün ama patlamasın.
            if (light.range <= 6.5f)
            {
                light.intensity = Mathf.Clamp(Mathf.Max(light.intensity, 1.15f) * mobileBoost, 1.15f, 2.2f);
                light.range = Mathf.Clamp(Mathf.Max(light.range, 4.8f), 4.8f, 8.0f);
                return;
            }

            // İç mekân / sokak: örtüşen yüksek yoğunluk kamera hareketinde HDR patlaması yapar.
            light.intensity = Mathf.Clamp(light.intensity * mobileBoost, 1.8f, 3.4f);
            light.range = Mathf.Clamp(light.range, 10f, 16f);
        }

        public static void ConfigurePlayerInteriorLight(Light light)
        {
            if (light == null || light.type == LightType.Directional)
            {
                return;
            }

            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            light.bounceIntensity = 0f;
            light.cullingMask = ~0;
            light.color = new Color(1.0f, 0.96f, 0.88f);
            light.intensity = Application.isMobilePlatform ? 6.4f : 5.6f;
            light.range = 15.0f;
            light.enabled = true;
        }
    }
}
