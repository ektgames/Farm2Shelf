using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    public enum VehicleType
    {
        PoliceCar,
        FireTruck,
        Ambulance,
        SedanRed,
        SedanBlue,
        SedanSilver,
        SUVBlack,
        SUVWhite,
        SUVOrange,
        MinibusYellow,
        MinibusDarkBlue,
        CityBus,
        CoupeSportYellow,
        CoupeSportRed,
        ConvertibleCyan
    }

    public static partial class ProceduralCarModelBuilder
    {
        private static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        private static Material GetMaterial(string name, Color color, float metallic = 0.2f, float smoothness = 0.5f)
        {
            if (matCache.TryGetValue(name, out Material mat) && mat != null)
            {
                return mat;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material newMat = new Material(shader)
            {
                name = name,
                color = color
            };

            if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", color);
            if (newMat.HasProperty("_Color")) newMat.SetColor("_Color", color);
            if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", metallic);
            if (newMat.HasProperty("_Glossiness")) newMat.SetFloat("_Glossiness", smoothness);
            if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", smoothness);

            matCache[name] = newMat;
            return newMat;
        }

        public static GameObject CreateVehicleModel(VehicleType type, out List<Transform> wheels)
        {
            return CreateDetailedTrafficVehicle(type, out wheels);
        }
        #region Helper Component Creators

        private static GameObject CreateBlock(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = pos;
            cube.transform.localScale = scale;
            if (mat != null) cube.GetComponent<Renderer>().sharedMaterial = mat;

            Collider col = cube.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            return cube;
        }

        private static void AddLightsAndBumpers(Transform parent, float width, float length, Material hlMat, Material tlMat, float yCenter = 0.45f)
        {
            float halfW = width / 2f - 0.25f;
            float halfL = length / 2f + 0.02f;

            GameObject hlL = CreateBlock(parent, "Headlight_L", new Vector3(-halfW, yCenter, halfL), new Vector3(0.35f, 0.18f, 0.08f), hlMat);
            GameObject hlR = CreateBlock(parent, "Headlight_R", new Vector3(halfW, yCenter, halfL), new Vector3(0.35f, 0.18f, 0.08f), hlMat);

            CreateBlock(parent, "Taillight_L", new Vector3(-halfW, yCenter, -halfL), new Vector3(0.35f, 0.18f, 0.08f), tlMat);
            CreateBlock(parent, "Taillight_R", new Vector3(halfW, yCenter, -halfL), new Vector3(0.35f, 0.18f, 0.08f), tlMat);

            // Gece Yanan Ön Far Işık Hüzmesi (Front SpotLights)
            GameObject spotLObj = new GameObject("Car_SpotLight_L");
            spotLObj.transform.SetParent(parent, false);
            spotLObj.transform.localPosition = new Vector3(-halfW, yCenter, halfL + 0.1f);
            spotLObj.transform.localRotation = Quaternion.Euler(10f, -5f, 0f);

            Light spotL = spotLObj.AddComponent<Light>();
            spotL.type = LightType.Spot;
            spotL.color = new Color(1.0f, 0.96f, 0.82f);
            spotL.intensity = 3.5f;
            spotL.range = 14.0f;
            spotL.spotAngle = 55f;
            spotL.enabled = false;

            GameObject spotRObj = new GameObject("Car_SpotLight_R");
            spotRObj.transform.SetParent(parent, false);
            spotRObj.transform.localPosition = new Vector3(halfW, yCenter, halfL + 0.1f);
            spotRObj.transform.localRotation = Quaternion.Euler(10f, 5f, 0f);

            Light spotR = spotRObj.AddComponent<Light>();
            spotR.type = LightType.Spot;
            spotR.color = new Color(1.0f, 0.96f, 0.82f);
            spotR.intensity = 3.5f;
            spotR.range = 14.0f;
            spotR.spotAngle = 55f;
            spotR.enabled = false;

            VehicleHeadlightController ctrl = parent.GetComponentInParent<VehicleHeadlightController>();
            if (ctrl == null) ctrl = parent.GetComponent<VehicleHeadlightController>();
            if (ctrl == null) ctrl = parent.gameObject.AddComponent<VehicleHeadlightController>();

            if (spotL != null && !ctrl.spotLights.Contains(spotL)) ctrl.spotLights.Add(spotL);
            if (spotR != null && !ctrl.spotLights.Contains(spotR)) ctrl.spotLights.Add(spotR);

            Renderer rL = hlL.GetComponent<Renderer>();
            Renderer rR = hlR.GetComponent<Renderer>();
            if (rL != null && !ctrl.headlightRenderers.Contains(rL)) ctrl.headlightRenderers.Add(rL);
            if (rR != null && !ctrl.headlightRenderers.Contains(rR)) ctrl.headlightRenderers.Add(rR);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterVehicleHeadlightController(ctrl);
            }
        }

        #endregion
    }
}
