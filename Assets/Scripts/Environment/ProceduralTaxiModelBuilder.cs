using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Detaylı sarı sedan taksi (Crown Vic / NYC tipi). Tekerlekler ayrı transform.
    /// </summary>
    public static class ProceduralTaxiModelBuilder
    {
        private static Material yellowMat;
        private static Material yellowDarkMat;
        private static Material blackMat;
        private static Material checkerWhiteMat;
        private static Material glassMat;
        private static Material chromeMat;
        private static Material steelMat;
        private static Material wheelMat;
        private static Material rimMat;
        private static Material hubMat;
        private static Material lightMat;
        private static Material lightInnerMat;
        private static Material amberMat;
        private static Material tailMat;
        private static Material tailInnerMat;
        private static Material signMat;
        private static Material signFaceMat;
        private static Material leatherMat;
        private static Material plateMat;
        private static Material rubberMat;

        private static void EnsureMaterials()
        {
            if (yellowMat != null) return;
            yellowMat = MakeMat("Taxi_Yellow", new Color(0.97f, 0.80f, 0.08f), 0.22f, 0.62f);
            yellowDarkMat = MakeMat("Taxi_YellowDark", new Color(0.78f, 0.58f, 0.05f), 0.18f, 0.48f);
            blackMat = MakeMat("Taxi_Black", new Color(0.07f, 0.07f, 0.08f), 0.25f, 0.42f);
            checkerWhiteMat = MakeMat("Taxi_CheckerW", new Color(0.96f, 0.96f, 0.97f), 0.08f, 0.35f);
            glassMat = MakeMat("Taxi_Glass", new Color(0.16f, 0.28f, 0.38f, 0.72f), 0.05f, 0.92f);
            chromeMat = MakeMat("Taxi_Chrome", new Color(0.86f, 0.88f, 0.92f), 0.88f, 0.88f);
            steelMat = MakeMat("Taxi_Steel", new Color(0.42f, 0.44f, 0.48f), 0.65f, 0.55f);
            wheelMat = MakeMat("Taxi_Tire", new Color(0.08f, 0.08f, 0.09f), 0.04f, 0.18f);
            rimMat = MakeMat("Taxi_Rim", new Color(0.78f, 0.80f, 0.84f), 0.82f, 0.80f);
            hubMat = MakeMat("Taxi_Hub", new Color(0.18f, 0.18f, 0.20f), 0.4f, 0.5f);
            lightMat = MakeMat("Taxi_Head", new Color(1f, 0.97f, 0.86f), 0.05f, 0.95f);
            lightInnerMat = MakeMat("Taxi_HeadInner", new Color(1f, 0.99f, 0.94f), 0f, 0.98f);
            amberMat = MakeMat("Taxi_Amber", new Color(1f, 0.55f, 0.08f), 0.05f, 0.88f);
            tailMat = MakeMat("Taxi_Tail", new Color(0.88f, 0.08f, 0.08f), 0.05f, 0.86f);
            tailInnerMat = MakeMat("Taxi_TailInner", new Color(1f, 0.22f, 0.16f), 0f, 0.92f);
            signMat = MakeMat("Taxi_RoofSign", new Color(1f, 0.90f, 0.12f), 0.08f, 0.55f);
            signFaceMat = MakeMat("Taxi_SignFace", new Color(0.08f, 0.08f, 0.10f), 0.05f, 0.3f);
            leatherMat = MakeMat("Taxi_Leather", new Color(0.18f, 0.12f, 0.10f), 0.08f, 0.38f);
            plateMat = MakeMat("Taxi_Plate", new Color(0.92f, 0.90f, 0.82f), 0.15f, 0.4f);
            rubberMat = MakeMat("Taxi_Rubber", new Color(0.12f, 0.12f, 0.13f), 0.05f, 0.22f);
        }

        private static Material MakeMat(string name, Color color, float metallic, float smoothness)
        {
            Shader s = ShaderHelper.GetLitShader();
            if (s == null) s = Shader.Find("Standard");
            Material m = new Material(s) { name = name };
            ShaderHelper.BindOpaqueColorMaps(m, color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        private static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            return go;
        }

        private static GameObject Cyl(Transform parent, string name, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            return go;
        }

        private static GameObject Sph(Transform parent, string name, Vector3 pos, float diameter, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * diameter;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            return go;
        }

        public static GameObject CreateTaxi(out List<Transform> wheels)
        {
            EnsureMaterials();
            wheels = new List<Transform>(4);

            GameObject root = new GameObject("Yellow_Taxi");

            BuildLowerBody(root.transform);
            BuildCabin(root.transform);
            BuildNoseAndTail(root.transform);
            BuildLights(root.transform);
            BuildTrimAndDoors(root.transform);
            BuildInterior(root.transform);
            BuildRoofSign(root.transform);
            BuildMirrorsAndAntenna(root.transform);
            BuildWheels(root.transform, wheels);
            BindHeadlights(root.transform);

            return root;
        }

        private static void BuildLowerBody(Transform root)
        {
            Block(root, "Rocker", new Vector3(0f, 0.30f, 0f), new Vector3(1.68f, 0.18f, 3.72f), yellowDarkMat);
            Block(root, "Body", new Vector3(0f, 0.50f, 0.02f), new Vector3(1.70f, 0.34f, 3.78f), yellowMat);
            Block(root, "Belt", new Vector3(0f, 0.68f, 0.02f), new Vector3(1.72f, 0.07f, 3.70f), yellowMat);

            Block(root, "Hood", new Vector3(0f, 0.66f, 1.18f), new Vector3(1.62f, 0.10f, 1.18f), yellowMat);
            Block(root, "Hood_Peak", new Vector3(0f, 0.72f, 1.05f), new Vector3(1.48f, 0.05f, 0.72f), yellowMat);
            Block(root, "Hood_Scoop", new Vector3(0f, 0.75f, 1.22f), new Vector3(0.42f, 0.04f, 0.55f), yellowDarkMat);

            Block(root, "Trunk", new Vector3(0f, 0.64f, -1.28f), new Vector3(1.60f, 0.12f, 1.08f), yellowMat);
            Block(root, "Trunk_Lid", new Vector3(0f, 0.72f, -1.22f), new Vector3(1.48f, 0.05f, 0.82f), yellowMat);

            Block(root, "Skirt_L", new Vector3(-0.86f, 0.26f, 0f), new Vector3(0.06f, 0.10f, 2.35f), blackMat);
            Block(root, "Skirt_R", new Vector3(0.86f, 0.26f, 0f), new Vector3(0.06f, 0.10f, 2.35f), blackMat);

            Block(root, "Arch_FL", new Vector3(-0.86f, 0.38f, 1.18f), new Vector3(0.10f, 0.28f, 0.72f), yellowDarkMat);
            Block(root, "Arch_FR", new Vector3(0.86f, 0.38f, 1.18f), new Vector3(0.10f, 0.28f, 0.72f), yellowDarkMat);
            Block(root, "Arch_RL", new Vector3(-0.86f, 0.38f, -1.18f), new Vector3(0.10f, 0.28f, 0.72f), yellowDarkMat);
            Block(root, "Arch_RR", new Vector3(0.86f, 0.38f, -1.18f), new Vector3(0.10f, 0.28f, 0.72f), yellowDarkMat);

            AddCheckerStripe(root, -0.875f);
            AddCheckerStripe(root, 0.875f);
        }

        private static void AddCheckerStripe(Transform root, float x)
        {
            for (int i = 0; i < 14; i++)
            {
                float z = -1.55f + i * 0.24f;
                Material mat = (i % 2 == 0) ? blackMat : checkerWhiteMat;
                Block(root, "Check_" + x + "_" + i, new Vector3(x, 0.54f, z), new Vector3(0.035f, 0.13f, 0.22f), mat);
            }
        }

        private static void BuildCabin(Transform root)
        {
            Block(root, "Cabin_Shell", new Vector3(0f, 0.92f, -0.12f), new Vector3(1.58f, 0.42f, 1.88f), yellowMat);
            Block(root, "Roof", new Vector3(0f, 1.18f, -0.14f), new Vector3(1.46f, 0.08f, 1.52f), yellowMat);
            Block(root, "Roof_Rail_L", new Vector3(-0.70f, 1.23f, -0.14f), new Vector3(0.05f, 0.04f, 1.40f), chromeMat);
            Block(root, "Roof_Rail_R", new Vector3(0.70f, 1.23f, -0.14f), new Vector3(0.05f, 0.04f, 1.40f), chromeMat);

            Block(root, "Windshield", new Vector3(0f, 0.98f, 0.78f), new Vector3(1.40f, 0.38f, 0.07f), glassMat);
            Block(root, "RearGlass", new Vector3(0f, 0.98f, -1.04f), new Vector3(1.38f, 0.36f, 0.07f), glassMat);
            Block(root, "Glass_FL", new Vector3(-0.80f, 0.96f, 0.22f), new Vector3(0.05f, 0.30f, 0.62f), glassMat);
            Block(root, "Glass_FR", new Vector3(0.80f, 0.96f, 0.22f), new Vector3(0.05f, 0.30f, 0.62f), glassMat);
            Block(root, "Glass_RL", new Vector3(-0.80f, 0.96f, -0.52f), new Vector3(0.05f, 0.30f, 0.70f), glassMat);
            Block(root, "Glass_RR", new Vector3(0.80f, 0.96f, -0.52f), new Vector3(0.05f, 0.30f, 0.70f), glassMat);

            Block(root, "Pillar_A_L", new Vector3(-0.76f, 0.98f, 0.62f), new Vector3(0.07f, 0.40f, 0.08f), blackMat);
            Block(root, "Pillar_A_R", new Vector3(0.76f, 0.98f, 0.62f), new Vector3(0.07f, 0.40f, 0.08f), blackMat);
            Block(root, "Pillar_B_L", new Vector3(-0.78f, 0.96f, -0.12f), new Vector3(0.08f, 0.38f, 0.10f), blackMat);
            Block(root, "Pillar_B_R", new Vector3(0.78f, 0.96f, -0.12f), new Vector3(0.08f, 0.38f, 0.10f), blackMat);
            Block(root, "Pillar_C_L", new Vector3(-0.76f, 0.96f, -0.92f), new Vector3(0.07f, 0.38f, 0.10f), blackMat);
            Block(root, "Pillar_C_R", new Vector3(0.76f, 0.96f, -0.92f), new Vector3(0.07f, 0.38f, 0.10f), blackMat);

            Block(root, "WindowTrim_F", new Vector3(0f, 1.16f, 0.78f), new Vector3(1.42f, 0.03f, 0.04f), chromeMat);
            Block(root, "WindowTrim_R", new Vector3(0f, 1.16f, -1.04f), new Vector3(1.40f, 0.03f, 0.04f), chromeMat);
        }

        private static void BuildNoseAndTail(Transform root)
        {
            Block(root, "Nose", new Vector3(0f, 0.48f, 1.88f), new Vector3(1.62f, 0.28f, 0.18f), yellowMat);
            Block(root, "Grill_Frame", new Vector3(0f, 0.46f, 1.98f), new Vector3(0.92f, 0.20f, 0.06f), chromeMat);
            for (int i = 0; i < 5; i++)
            {
                Block(root, "GrillBar_" + i, new Vector3(0f, 0.38f + i * 0.04f, 2.00f), new Vector3(0.84f, 0.018f, 0.03f), blackMat);
            }

            Block(root, "Bumper_F", new Vector3(0f, 0.24f, 1.99f), new Vector3(1.68f, 0.14f, 0.16f), chromeMat);
            Block(root, "Bumper_F_Rub", new Vector3(0f, 0.22f, 2.06f), new Vector3(1.50f, 0.07f, 0.05f), rubberMat);
            Block(root, "Fog_L", new Vector3(-0.58f, 0.26f, 2.04f), new Vector3(0.16f, 0.08f, 0.05f), lightMat);
            Block(root, "Fog_R", new Vector3(0.58f, 0.26f, 2.04f), new Vector3(0.16f, 0.08f, 0.05f), lightMat);

            Block(root, "Tail_Panel", new Vector3(0f, 0.50f, -1.90f), new Vector3(1.60f, 0.26f, 0.14f), yellowMat);
            Block(root, "Bumper_R", new Vector3(0f, 0.24f, -1.99f), new Vector3(1.68f, 0.14f, 0.16f), chromeMat);
            Block(root, "Bumper_R_Rub", new Vector3(0f, 0.22f, -2.06f), new Vector3(1.50f, 0.07f, 0.05f), rubberMat);

            Block(root, "Plate_F", new Vector3(0f, 0.34f, 2.08f), new Vector3(0.38f, 0.10f, 0.03f), plateMat);
            Block(root, "Plate_R", new Vector3(0f, 0.34f, -2.08f), new Vector3(0.38f, 0.10f, 0.03f), plateMat);

            Cyl(root, "Exhaust", new Vector3(0.42f, 0.16f, -2.04f), new Vector3(90f, 0f, 0f), new Vector3(0.07f, 0.10f, 0.07f), steelMat);
        }

        private static void BuildLights(Transform root)
        {
            Block(root, "Head_L", new Vector3(-0.52f, 0.46f, 1.97f), new Vector3(0.38f, 0.16f, 0.08f), lightMat);
            Block(root, "Head_R", new Vector3(0.52f, 0.46f, 1.97f), new Vector3(0.38f, 0.16f, 0.08f), lightMat);
            Sph(root, "HeadLens_L", new Vector3(-0.52f, 0.46f, 2.01f), 0.13f, lightInnerMat);
            Sph(root, "HeadLens_R", new Vector3(0.52f, 0.46f, 2.01f), 0.13f, lightInnerMat);
            Block(root, "Ind_L", new Vector3(-0.78f, 0.46f, 1.96f), new Vector3(0.12f, 0.10f, 0.06f), amberMat);
            Block(root, "Ind_R", new Vector3(0.78f, 0.46f, 1.96f), new Vector3(0.12f, 0.10f, 0.06f), amberMat);

            Block(root, "Tail_L", new Vector3(-0.54f, 0.48f, -1.97f), new Vector3(0.42f, 0.14f, 0.07f), tailMat);
            Block(root, "Tail_R", new Vector3(0.54f, 0.48f, -1.97f), new Vector3(0.42f, 0.14f, 0.07f), tailMat);
            Block(root, "TailSeg_L", new Vector3(-0.42f, 0.48f, -2.00f), new Vector3(0.14f, 0.08f, 0.04f), tailInnerMat);
            Block(root, "TailSeg_R", new Vector3(0.42f, 0.48f, -2.00f), new Vector3(0.14f, 0.08f, 0.04f), tailInnerMat);
            Block(root, "Rev_L", new Vector3(-0.66f, 0.42f, -1.98f), new Vector3(0.10f, 0.06f, 0.04f), lightMat);
            Block(root, "Rev_R", new Vector3(0.66f, 0.42f, -1.98f), new Vector3(0.10f, 0.06f, 0.04f), lightMat);
        }

        private static void BuildTrimAndDoors(Transform root)
        {
            Block(root, "ChromeLine_L", new Vector3(-0.86f, 0.66f, 0.02f), new Vector3(0.03f, 0.03f, 3.20f), chromeMat);
            Block(root, "ChromeLine_R", new Vector3(0.86f, 0.66f, 0.02f), new Vector3(0.03f, 0.03f, 3.20f), chromeMat);

            Block(root, "DoorGap_L", new Vector3(-0.86f, 0.58f, -0.12f), new Vector3(0.02f, 0.38f, 0.02f), blackMat);
            Block(root, "DoorGap_R", new Vector3(0.86f, 0.58f, -0.12f), new Vector3(0.02f, 0.38f, 0.02f), blackMat);

            Block(root, "Handle_FL", new Vector3(-0.88f, 0.62f, 0.28f), new Vector3(0.04f, 0.05f, 0.14f), chromeMat);
            Block(root, "Handle_FR", new Vector3(0.88f, 0.62f, 0.28f), new Vector3(0.04f, 0.05f, 0.14f), chromeMat);
            Block(root, "Handle_RL", new Vector3(-0.88f, 0.62f, -0.42f), new Vector3(0.04f, 0.05f, 0.14f), chromeMat);
            Block(root, "Handle_RR", new Vector3(0.88f, 0.62f, -0.42f), new Vector3(0.04f, 0.05f, 0.14f), chromeMat);

            Block(root, "FuelCap", new Vector3(0.86f, 0.58f, -1.42f), new Vector3(0.04f, 0.08f, 0.08f), chromeMat);
        }

        private static void BuildInterior(Transform root)
        {
            Block(root, "Dash", new Vector3(0f, 0.78f, 0.58f), new Vector3(1.28f, 0.16f, 0.22f), blackMat);
            Block(root, "Seat_D", new Vector3(-0.34f, 0.70f, 0.08f), new Vector3(0.46f, 0.22f, 0.42f), leatherMat);
            Block(root, "Seat_P", new Vector3(0.34f, 0.70f, 0.08f), new Vector3(0.46f, 0.22f, 0.42f), leatherMat);
            Block(root, "SeatBack_D", new Vector3(-0.34f, 0.92f, -0.10f), new Vector3(0.46f, 0.28f, 0.10f), leatherMat);
            Block(root, "SeatBack_P", new Vector3(0.34f, 0.92f, -0.10f), new Vector3(0.46f, 0.28f, 0.10f), leatherMat);
            Block(root, "RearBench", new Vector3(0f, 0.70f, -0.62f), new Vector3(1.22f, 0.20f, 0.38f), leatherMat);
            Cyl(root, "Steer", new Vector3(-0.32f, 0.86f, 0.42f), new Vector3(72f, 0f, 0f), new Vector3(0.22f, 0.02f, 0.22f), blackMat);
        }

        private static void BuildRoofSign(Transform root)
        {
            GameObject sign = Block(root, "TaxiSign", new Vector3(0f, 1.38f, -0.06f), new Vector3(0.72f, 0.26f, 0.20f), signMat);
            Block(sign.transform, "SignCap", new Vector3(0f, 0.14f, 0f), new Vector3(0.78f, 0.06f, 0.24f), yellowDarkMat);
            Block(sign.transform, "Face_F", new Vector3(0f, 0f, 0.12f), new Vector3(0.66f, 0.18f, 0.03f), signFaceMat);
            Block(sign.transform, "Face_B", new Vector3(0f, 0f, -0.12f), new Vector3(0.66f, 0.18f, 0.03f), signFaceMat);

            float[] xs = { -0.22f, -0.11f, 0f, 0.11f, 0.22f };
            for (int i = 0; i < xs.Length; i++)
            {
                Block(sign.transform, "Glyph_" + i, new Vector3(xs[i], 0.01f, 0.135f), new Vector3(0.06f, 0.10f, 0.02f), signMat);
            }

            Block(root, "SignMount", new Vector3(0f, 1.26f, -0.06f), new Vector3(0.18f, 0.08f, 0.12f), blackMat);
        }

        private static void BuildMirrorsAndAntenna(Transform root)
        {
            Block(root, "Mirror_L", new Vector3(-0.92f, 0.82f, 0.62f), new Vector3(0.16f, 0.10f, 0.08f), blackMat);
            Block(root, "MirrorGlass_L", new Vector3(-0.96f, 0.82f, 0.62f), new Vector3(0.03f, 0.08f, 0.07f), chromeMat);
            Block(root, "Mirror_R", new Vector3(0.92f, 0.82f, 0.62f), new Vector3(0.16f, 0.10f, 0.08f), blackMat);
            Block(root, "MirrorGlass_R", new Vector3(0.96f, 0.82f, 0.62f), new Vector3(0.03f, 0.08f, 0.07f), chromeMat);

            Cyl(root, "Antenna", new Vector3(0.52f, 1.48f, -0.72f), new Vector3(0f, 0f, 8f), new Vector3(0.018f, 0.28f, 0.018f), steelMat);
            Sph(root, "AntennaTip", new Vector3(0.56f, 1.76f, -0.72f), 0.04f, blackMat);

            Block(root, "Wiper_L", new Vector3(-0.22f, 0.80f, 0.82f), new Vector3(0.36f, 0.02f, 0.02f), blackMat);
            Block(root, "Wiper_R", new Vector3(0.22f, 0.80f, 0.82f), new Vector3(0.36f, 0.02f, 0.02f), blackMat);
        }

        private static void BuildWheels(Transform root, List<Transform> wheels)
        {
            AddWheel(root, wheels, new Vector3(-0.86f, 0.30f, 1.18f));
            AddWheel(root, wheels, new Vector3(0.86f, 0.30f, 1.18f));
            AddWheel(root, wheels, new Vector3(-0.86f, 0.30f, -1.18f));
            AddWheel(root, wheels, new Vector3(0.86f, 0.30f, -1.18f));
        }

        private static void AddWheel(Transform parent, List<Transform> wheels, Vector3 pos)
        {
            GameObject hub = new GameObject("Wheel");
            hub.transform.SetParent(parent, false);
            hub.transform.localPosition = pos;

            Cyl(hub.transform, "Tire", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.62f, 0.12f, 0.62f), wheelMat);
            Cyl(hub.transform, "TireWall", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.52f, 0.135f, 0.52f), rubberMat);
            Cyl(hub.transform, "Rim", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.38f, 0.08f, 0.38f), rimMat);
            Cyl(hub.transform, "HubCap", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.16f, 0.09f, 0.16f), hubMat);

            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                GameObject spoke = Block(hub.transform, "Spoke_" + i, Vector3.zero, new Vector3(0.05f, 0.04f, 0.30f), chromeMat);
                spoke.transform.localRotation = Quaternion.Euler(a, 0f, 90f);
            }

            wheels.Add(hub.transform);
        }

        private static void BindHeadlights(Transform root)
        {
            VehicleHeadlightController ctrl = root.gameObject.GetComponent<VehicleHeadlightController>();
            if (ctrl == null) ctrl = root.gameObject.AddComponent<VehicleHeadlightController>();

            AddSpot(root, ctrl, "Spot_L", new Vector3(-0.52f, 0.46f, 2.08f), new Vector3(8f, -4f, 0f));
            AddSpot(root, ctrl, "Spot_R", new Vector3(0.52f, 0.46f, 2.08f), new Vector3(8f, 4f, 0f));

            Transform hl = root.Find("Head_L");
            Transform hr = root.Find("Head_R");
            if (hl != null)
            {
                Renderer r = hl.GetComponent<Renderer>();
                if (r != null && !ctrl.headlightRenderers.Contains(r)) ctrl.headlightRenderers.Add(r);
            }
            if (hr != null)
            {
                Renderer r = hr.GetComponent<Renderer>();
                if (r != null && !ctrl.headlightRenderers.Contains(r)) ctrl.headlightRenderers.Add(r);
            }

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterVehicleHeadlightController(ctrl);
            }
        }

        private static void AddSpot(Transform root, VehicleHeadlightController ctrl, string name, Vector3 pos, Vector3 euler)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(root, false);
            obj.transform.localPosition = pos;
            obj.transform.localRotation = Quaternion.Euler(euler);
            Light spot = obj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.96f, 0.82f);
            spot.intensity = 3.2f;
            spot.range = 12f;
            spot.spotAngle = 50f;
            spot.enabled = false;
            if (!ctrl.spotLights.Contains(spot)) ctrl.spotLights.Add(spot);
        }
    }
}
