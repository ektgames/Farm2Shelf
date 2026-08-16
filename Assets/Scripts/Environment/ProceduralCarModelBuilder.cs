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

    public static class ProceduralCarModelBuilder
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
            wheels = new List<Transform>();
            GameObject carRoot = new GameObject("Vehicle_" + type.ToString());

            // Ortak Materyaller
            Material wheelMat = GetMaterial("Mat_CarWheel", new Color(0.12f, 0.12f, 0.12f), 0.1f, 0.2f);
            Material rimMat = GetMaterial("Mat_CarRim", new Color(0.85f, 0.85f, 0.88f), 0.8f, 0.8f);
            Material glassMat = GetMaterial("Mat_CarGlass", new Color(0.20f, 0.35f, 0.45f, 0.80f), 0.9f, 0.9f);
            Material headLightMat = GetMaterial("Mat_Headlight", new Color(1.0f, 0.96f, 0.80f), 0.0f, 0.9f);
            Material tailLightMat = GetMaterial("Mat_Taillight", new Color(0.95f, 0.15f, 0.10f), 0.0f, 0.9f);

            switch (type)
            {
                case VehicleType.PoliceCar:
                    BuildPoliceCar(carRoot.transform, wheels, wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.FireTruck:
                    BuildFireTruck(carRoot.transform, wheels, wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.Ambulance:
                    BuildAmbulance(carRoot.transform, wheels, wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.SedanRed:
                    BuildSedan(carRoot.transform, wheels, GetMaterial("Mat_RedBody", new Color(0.85f, 0.15f, 0.15f)), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.SedanBlue:
                    BuildSedan(carRoot.transform, wheels, GetMaterial("Mat_BlueBody", new Color(0.15f, 0.40f, 0.85f)), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.SedanSilver:
                    BuildSedan(carRoot.transform, wheels, GetMaterial("Mat_SilverBody", new Color(0.75f, 0.78f, 0.82f), 0.7f, 0.7f), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.SUVBlack:
                    BuildSUV(carRoot.transform, wheels, GetMaterial("Mat_BlackBody", new Color(0.12f, 0.14f, 0.16f)), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.SUVWhite:
                    BuildSUV(carRoot.transform, wheels, GetMaterial("Mat_WhiteBody", new Color(0.92f, 0.94f, 0.96f)), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.SUVOrange:
                    BuildSUV(carRoot.transform, wheels, GetMaterial("Mat_OrangeBody", new Color(0.95f, 0.45f, 0.10f)), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.MinibusYellow:
                    BuildMinibus(carRoot.transform, wheels, GetMaterial("Mat_YellowBody", new Color(0.95f, 0.75f, 0.10f)), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.MinibusDarkBlue:
                    BuildMinibus(carRoot.transform, wheels, GetMaterial("Mat_DarkBlueBody", new Color(0.10f, 0.20f, 0.45f)), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.CityBus:
                    BuildCityBus(carRoot.transform, wheels, wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.CoupeSportYellow:
                    BuildCoupeSport(carRoot.transform, wheels, GetMaterial("Mat_SportYellow", new Color(0.98f, 0.85f, 0.05f)), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.CoupeSportRed:
                    BuildCoupeSport(carRoot.transform, wheels, GetMaterial("Mat_SportRed", new Color(0.92f, 0.10f, 0.15f)), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
                case VehicleType.ConvertibleCyan:
                    BuildConvertible(carRoot.transform, wheels, GetMaterial("Mat_CyanBody", new Color(0.10f, 0.75f, 0.85f)), wheelMat, rimMat, glassMat, headLightMat, tailLightMat);
                    break;
            }

            return carRoot;
        }

        #region Vehicle Builders

        private static void BuildPoliceCar(Transform parent, List<Transform> wheels, Material wMat, Material rMat, Material gMat, Material hlMat, Material tlMat)
        {
            Material blackMat = GetMaterial("Mat_PoliceBlack", new Color(0.10f, 0.10f, 0.12f));
            Material whiteMat = GetMaterial("Mat_PoliceWhite", new Color(0.95f, 0.95f, 0.95f));

            CreateBlock(parent, "Body_Base", new Vector3(0f, 0.5f, 0f), new Vector3(1.55f, 0.5f, 3.8f), blackMat);
            CreateBlock(parent, "Body_Middle", new Vector3(0f, 0.68f, 0.1f), new Vector3(1.50f, 0.4f, 1.9f), whiteMat);
            CreateBlock(parent, "Cabin_Glass", new Vector3(0f, 1.02f, -0.1f), new Vector3(1.42f, 0.48f, 1.7f), gMat);

            Material blueSiren = GetMaterial("Mat_SirenBlue", new Color(0.10f, 0.35f, 0.95f));
            Material redSiren = GetMaterial("Mat_SirenRed", new Color(0.95f, 0.15f, 0.10f));

            GameObject sirenBar = CreateBlock(parent, "Siren_Bar", new Vector3(0f, 1.32f, -0.1f), new Vector3(0.95f, 0.10f, 0.25f), GetMaterial("Mat_SirenBase", Color.black));
            CreateBlock(sirenBar.transform, "Siren_Left", new Vector3(-0.3f, 0.07f, 0f), new Vector3(0.25f, 0.10f, 0.2f), blueSiren);
            CreateBlock(sirenBar.transform, "Siren_Right", new Vector3(0.3f, 0.07f, 0f), new Vector3(0.25f, 0.10f, 0.2f), redSiren);

            AddLightsAndBumpers(parent, 1.55f, 3.8f, hlMat, tlMat);
            AddStandardWheels(parent, wheels, 1.55f, 3.8f, wMat, rMat);
        }

        private static void BuildFireTruck(Transform parent, List<Transform> wheels, Material wMat, Material rMat, Material gMat, Material hlMat, Material tlMat)
        {
            Material fireRed = GetMaterial("Mat_FireRed", new Color(0.85f, 0.12f, 0.10f));
            Material whiteMat = GetMaterial("Mat_TruckWhite", new Color(0.92f, 0.92f, 0.95f));
            Material silverMat = GetMaterial("Mat_TruckSilver", new Color(0.75f, 0.75f, 0.78f));

            CreateBlock(parent, "Body_Base", new Vector3(0f, 0.75f, 0f), new Vector3(1.85f, 1.1f, 5.4f), fireRed);
            CreateBlock(parent, "Cabin", new Vector3(0f, 1.2f, 1.7f), new Vector3(1.80f, 0.95f, 1.6f), fireRed);
            CreateBlock(parent, "Cabin_Glass", new Vector3(0f, 1.48f, 1.8f), new Vector3(1.70f, 0.5f, 1.4f), gMat);

            GameObject ladder = CreateBlock(parent, "Ladder_Base", new Vector3(0f, 1.98f, -0.6f), new Vector3(0.9f, 0.12f, 3.4f), whiteMat);
            CreateBlock(ladder.transform, "Ladder_Rung1", new Vector3(0f, 0.08f, -0.9f), new Vector3(0.75f, 0.07f, 0.12f), silverMat);
            CreateBlock(ladder.transform, "Ladder_Rung2", new Vector3(0f, 0.08f, 0f), new Vector3(0.75f, 0.07f, 0.12f), silverMat);
            CreateBlock(ladder.transform, "Ladder_Rung3", new Vector3(0f, 0.08f, 0.9f), new Vector3(0.75f, 0.07f, 0.12f), silverMat);

            Material redSiren = GetMaterial("Mat_SirenRed", new Color(0.95f, 0.15f, 0.10f));
            CreateBlock(parent, "Siren_Left", new Vector3(-0.6f, 1.95f, 2.2f), new Vector3(0.22f, 0.12f, 0.22f), redSiren);
            CreateBlock(parent, "Siren_Right", new Vector3(0.6f, 1.95f, 2.2f), new Vector3(0.22f, 0.12f, 0.22f), redSiren);

            AddLightsAndBumpers(parent, 1.85f, 5.4f, hlMat, tlMat, 0.65f);
            AddLargeWheels(parent, wheels, 1.85f, 5.4f, wMat, rMat);
        }

        private static void BuildAmbulance(Transform parent, List<Transform> wheels, Material wMat, Material rMat, Material gMat, Material hlMat, Material tlMat)
        {
            Material whiteMat = GetMaterial("Mat_AmbWhite", new Color(0.95f, 0.95f, 0.95f));
            Material redMat = GetMaterial("Mat_AmbRed", new Color(0.90f, 0.15f, 0.12f));

            CreateBlock(parent, "Body_Base", new Vector3(0f, 0.85f, -0.2f), new Vector3(1.75f, 1.15f, 4.2f), whiteMat);
            CreateBlock(parent, "Red_Stripe", new Vector3(0f, 0.78f, -0.2f), new Vector3(1.77f, 0.2f, 4.22f), redMat);

            CreateBlock(parent, "Cabin_Glass", new Vector3(0f, 1.18f, 1.2f), new Vector3(1.65f, 0.45f, 1.2f), gMat);

            CreateBlock(parent, "Cross_Left", new Vector3(-0.89f, 1.1f, -0.4f), new Vector3(0.04f, 0.4f, 0.4f), redMat);
            CreateBlock(parent, "Cross_Right", new Vector3(0.89f, 1.1f, -0.4f), new Vector3(0.04f, 0.4f, 0.4f), redMat);

            CreateBlock(parent, "Siren_Top", new Vector3(0f, 1.7f, 1.3f), new Vector3(0.6f, 0.12f, 0.25f), redMat);

            AddLightsAndBumpers(parent, 1.75f, 4.2f, hlMat, tlMat, 0.5f);
            AddStandardWheels(parent, wheels, 1.75f, 4.2f, wMat, rMat, 0.38f);
        }

        private static void BuildSedan(Transform parent, List<Transform> wheels, Material bodyMat, Material wMat, Material rMat, Material gMat, Material hlMat, Material tlMat)
        {
            CreateBlock(parent, "Body_Base", new Vector3(0f, 0.45f, 0f), new Vector3(1.55f, 0.45f, 3.7f), bodyMat);
            CreateBlock(parent, "Cabin_Glass", new Vector3(0f, 0.88f, -0.1f), new Vector3(1.45f, 0.42f, 1.8f), gMat);
            CreateBlock(parent, "Roof", new Vector3(0f, 1.12f, -0.1f), new Vector3(1.4f, 0.07f, 1.5f), bodyMat);

            AddLightsAndBumpers(parent, 1.55f, 3.7f, hlMat, tlMat);
            AddStandardWheels(parent, wheels, 1.55f, 3.7f, wMat, rMat);
        }

        private static void BuildSUV(Transform parent, List<Transform> wheels, Material bodyMat, Material wMat, Material rMat, Material gMat, Material hlMat, Material tlMat)
        {
            CreateBlock(parent, "Body_Base", new Vector3(0f, 0.55f, 0f), new Vector3(1.68f, 0.6f, 4.0f), bodyMat);
            CreateBlock(parent, "Cabin_Glass", new Vector3(0f, 1.08f, -0.15f), new Vector3(1.58f, 0.5f, 2.2f), gMat);
            CreateBlock(parent, "Roof", new Vector3(0f, 1.36f, -0.15f), new Vector3(1.52f, 0.07f, 2.1f), bodyMat);

            Material rackMat = GetMaterial("Mat_SUVRack", new Color(0.2f, 0.2f, 0.2f));
            CreateBlock(parent, "Rack_Left", new Vector3(-0.7f, 1.43f, -0.15f), new Vector3(0.07f, 0.07f, 1.9f), rackMat);
            CreateBlock(parent, "Rack_Right", new Vector3(0.7f, 1.43f, -0.15f), new Vector3(0.07f, 0.07f, 1.9f), rackMat);

            AddLightsAndBumpers(parent, 1.68f, 4.0f, hlMat, tlMat, 0.45f);
            AddLargeWheels(parent, wheels, 1.68f, 4.0f, wMat, rMat, 0.38f);
        }

        private static void BuildMinibus(Transform parent, List<Transform> wheels, Material bodyMat, Material wMat, Material rMat, Material gMat, Material hlMat, Material tlMat)
        {
            CreateBlock(parent, "Body_Base", new Vector3(0f, 0.72f, 0f), new Vector3(1.68f, 1.05f, 4.2f), bodyMat);
            CreateBlock(parent, "Cabin_Glass", new Vector3(0f, 1.08f, 1.0f), new Vector3(1.60f, 0.45f, 1.3f), gMat);
            CreateBlock(parent, "Side_Glass", new Vector3(0f, 1.08f, -0.6f), new Vector3(1.60f, 0.42f, 1.8f), gMat);

            AddLightsAndBumpers(parent, 1.68f, 4.2f, hlMat, tlMat, 0.48f);
            AddStandardWheels(parent, wheels, 1.68f, 4.2f, wMat, rMat, 0.38f);
        }

        private static void BuildCityBus(Transform parent, List<Transform> wheels, Material wMat, Material rMat, Material gMat, Material hlMat, Material tlMat)
        {
            Material greenMat = GetMaterial("Mat_BusGreen", new Color(0.15f, 0.65f, 0.35f));
            Material whiteMat = GetMaterial("Mat_BusWhite", new Color(0.95f, 0.95f, 0.95f));

            CreateBlock(parent, "Body_Lower", new Vector3(0f, 0.6f, 0f), new Vector3(1.85f, 0.7f, 6.4f), greenMat);
            CreateBlock(parent, "Body_Upper", new Vector3(0f, 1.38f, 0f), new Vector3(1.85f, 0.85f, 6.4f), whiteMat);
            CreateBlock(parent, "Side_Windows", new Vector3(0f, 1.42f, 0.15f), new Vector3(1.87f, 0.55f, 5.6f), gMat);
            CreateBlock(parent, "Front_Window", new Vector3(0f, 1.42f, 3.15f), new Vector3(1.78f, 0.65f, 0.1f), gMat);

            AddLightsAndBumpers(parent, 1.85f, 6.4f, hlMat, tlMat, 0.6f);
            AddLargeWheels(parent, wheels, 1.85f, 6.4f, wMat, rMat, 0.45f);
        }

        private static void BuildCoupeSport(Transform parent, List<Transform> wheels, Material bodyMat, Material wMat, Material rMat, Material gMat, Material hlMat, Material tlMat)
        {
            CreateBlock(parent, "Body_Base", new Vector3(0f, 0.4f, 0f), new Vector3(1.55f, 0.38f, 3.7f), bodyMat);
            CreateBlock(parent, "Cabin_Glass", new Vector3(0f, 0.76f, -0.15f), new Vector3(1.42f, 0.38f, 1.6f), gMat);
            CreateBlock(parent, "Roof", new Vector3(0f, 0.96f, -0.2f), new Vector3(1.36f, 0.05f, 1.1f), bodyMat);

            Material spoilerMat = GetMaterial("Mat_SpoilerBlack", new Color(0.10f, 0.10f, 0.12f));
            GameObject spoiler = CreateBlock(parent, "Spoiler_Base", new Vector3(0f, 0.68f, -1.65f), new Vector3(1.3f, 0.07f, 0.3f), spoilerMat);
            CreateBlock(spoiler.transform, "Leg_Left", new Vector3(-0.5f, -0.08f, 0f), new Vector3(0.07f, 0.18f, 0.12f), spoilerMat);
            CreateBlock(spoiler.transform, "Leg_Right", new Vector3(0.5f, -0.08f, 0f), new Vector3(0.07f, 0.18f, 0.12f), spoilerMat);

            AddLightsAndBumpers(parent, 1.55f, 3.7f, hlMat, tlMat, 0.3f);
            AddStandardWheels(parent, wheels, 1.55f, 3.7f, wMat, rMat, 0.32f);
        }

        private static void BuildConvertible(Transform parent, List<Transform> wheels, Material bodyMat, Material wMat, Material rMat, Material gMat, Material hlMat, Material tlMat)
        {
            CreateBlock(parent, "Body_Base", new Vector3(0f, 0.4f, 0f), new Vector3(1.55f, 0.4f, 3.7f), bodyMat);
            CreateBlock(parent, "Windshield", new Vector3(0f, 0.78f, 0.35f), new Vector3(1.45f, 0.38f, 0.1f), gMat);

            Material seatMat = GetMaterial("Mat_LeatherSeat", new Color(0.25f, 0.15f, 0.10f));
            CreateBlock(parent, "Seat_Driver", new Vector3(-0.38f, 0.58f, -0.1f), new Vector3(0.48f, 0.3f, 0.48f), seatMat);
            CreateBlock(parent, "Seat_Passenger", new Vector3(0.38f, 0.58f, -0.1f), new Vector3(0.48f, 0.3f, 0.48f), seatMat);

            AddLightsAndBumpers(parent, 1.55f, 3.7f, hlMat, tlMat, 0.3f);
            AddStandardWheels(parent, wheels, 1.55f, 3.7f, wMat, rMat, 0.32f);
        }

        #endregion

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

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterVehicleHeadlight(spotL, hlL);
                DayNightCycleManager.Instance.RegisterVehicleHeadlight(spotR, hlR);
            }
        }

        private static void AddStandardWheels(Transform parent, List<Transform> wheels, float width, float length, Material wMat, Material rMat, float wheelRadius = 0.35f)
        {
            float xOffset = width / 2f + 0.02f;
            float zFront = length / 2f - 0.9f;
            float zRear = -length / 2f + 0.9f;

            CreateWheel(parent, wheels, new Vector3(-xOffset, wheelRadius, zFront), wheelRadius, wMat, rMat);
            CreateWheel(parent, wheels, new Vector3(xOffset, wheelRadius, zFront), wheelRadius, wMat, rMat);
            CreateWheel(parent, wheels, new Vector3(-xOffset, wheelRadius, zRear), wheelRadius, wMat, rMat);
            CreateWheel(parent, wheels, new Vector3(xOffset, wheelRadius, zRear), wheelRadius, wMat, rMat);
        }

        private static void AddLargeWheels(Transform parent, List<Transform> wheels, float width, float length, Material wMat, Material rMat, float wheelRadius = 0.50f)
        {
            float xOffset = width / 2f + 0.04f;
            float zFront = length / 2f - 1.1f;
            float zRear = -length / 2f + 1.1f;

            CreateWheel(parent, wheels, new Vector3(-xOffset, wheelRadius, zFront), wheelRadius, wMat, rMat);
            CreateWheel(parent, wheels, new Vector3(xOffset, wheelRadius, zFront), wheelRadius, wMat, rMat);
            CreateWheel(parent, wheels, new Vector3(-xOffset, wheelRadius, zRear), wheelRadius, wMat, rMat);
            CreateWheel(parent, wheels, new Vector3(xOffset, wheelRadius, zRear), wheelRadius, wMat, rMat);
        }

        private static void CreateWheel(Transform parent, List<Transform> wheels, Vector3 pos, float radius, Material wMat, Material rMat)
        {
            GameObject wheelObj = new GameObject("Wheel");
            wheelObj.transform.SetParent(parent, false);
            wheelObj.transform.localPosition = pos;

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Wheel_Tire";
            cylinder.transform.SetParent(wheelObj.transform, false);
            cylinder.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            cylinder.transform.localScale = new Vector3(radius * 2f, 0.14f, radius * 2f);
            if (wMat != null) cylinder.GetComponent<Renderer>().sharedMaterial = wMat;

            Collider col = cylinder.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Wheel_Rim";
            rim.transform.SetParent(wheelObj.transform, false);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rim.transform.localScale = new Vector3(radius * 1.1f, 0.15f, radius * 1.1f);
            if (rMat != null) rim.GetComponent<Renderer>().sharedMaterial = rMat;

            Collider rCol = rim.GetComponent<Collider>();
            if (rCol != null) Object.Destroy(rCol);

            wheels.Add(wheelObj.transform);
        }

        #endregion
    }
}
