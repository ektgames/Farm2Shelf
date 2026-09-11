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

        public static bool IsMobileLightingProfile()
        {
            if (Application.isMobilePlatform) return true;
            if (QualitySettings.names != null)
            {
                int qi = QualitySettings.GetQualityLevel();
                if (qi >= 0 && qi < QualitySettings.names.Length
                    && QualitySettings.names[qi].IndexOf("Mobile", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
#if UNITY_EDITOR
            UnityEditor.BuildTarget t = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            if (t == UnityEditor.BuildTarget.Android || t == UnityEditor.BuildTarget.iOS)
            {
                return true;
            }
#endif
            return false;
        }

        public static void Apply()
        {
            QualitySettings.pixelLightCount = 16;
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

            bool mobileLook = IsMobileLightingProfile();
            cam.allowHDR = !mobileLook;
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

            camData.renderPostProcessing = !mobileLook;
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
            light.renderMode = LightRenderMode.ForcePixel;
            light.bounceIntensity = 0f;
            light.cullingMask = ~0;

            // Menzili şişirme: örtüşen ışık HDR patlatır. Kotayı doldurmasın diye tavan koy.
            if (light.type == LightType.Spot)
            {
                if (light.intensity > 2.8f) light.intensity = 2.8f;
                if (light.range > 16f) light.range = 16f;
                if (light.spotAngle < 50f) light.spotAngle = 60f;
                if (light.innerSpotAngle < 20f) light.innerSpotAngle = 28f;
            }
            else
            {
                if (light.intensity > 2.2f) light.intensity = 2.2f;
                if (light.range > 12f) light.range = 12f;
            }

            UniversalAdditionalLightData extra = light.GetUniversalAdditionalLightData();
            if (extra != null)
            {
                extra.usePipelineSettings = true;
            }
        }

        public static void ConfigurePlayerInteriorLight(Light light)
        {
            if (light == null || light.type == LightType.Directional)
            {
                return;
            }

            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            light.bounceIntensity = 0f;
            light.cullingMask = ~0;
            light.color = new Color(1.0f, 0.96f, 0.88f);
            light.intensity = 3.4f;
            light.range = 12.0f;
            light.enabled = true;

            UniversalAdditionalLightData extra = light.GetUniversalAdditionalLightData();
            if (extra != null)
            {
                extra.usePipelineSettings = true;
            }
        }
    }
}
