using UnityEngine;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Teslimat scooter / kurye motosikleti (şasi, gidon, kutu, jant, far).
    /// CreateCourierMotorcycle imzası CourierManager ile uyumludur.
    /// </summary>
    public static class ProceduralMotorcycleBuilder
    {
        private static Material bodyMatRed;
        private static Material bodyMatTeal;
        private static Material bodyMatYellow;
        private static Material bodyMatDark;
        private static Material frameDarkMat;
        private static Material plasticMat;
        private static Material seatLeatherMat;
        private static Material tireRubberMat;
        private static Material rimSilverMat;
        private static Material chromeMat;
        private static Material steelMat;
        private static Material deliveryBoxMat;
        private static Material deliveryBoxDarkMat;
        private static Material headlightMat;
        private static Material headlightInnerMat;
        private static Material taillightMat;
        private static Material amberMat;
        private static Material windshieldMat;
        private static Material plateMat;
        private static Material brandMat;

        private static void InitMaterials()
        {
            if (bodyMatRed != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Lightweight Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            bodyMatRed = CreateMaterial(shader, "Moto_Red", new Color(0.86f, 0.12f, 0.14f), 0.28f, 0.78f);
            bodyMatTeal = CreateMaterial(shader, "Moto_Teal", new Color(0.08f, 0.62f, 0.68f), 0.28f, 0.78f);
            bodyMatYellow = CreateMaterial(shader, "Moto_Yellow", new Color(0.96f, 0.72f, 0.08f), 0.28f, 0.78f);
            bodyMatDark = CreateMaterial(shader, "Moto_BodyDark", new Color(0.18f, 0.20f, 0.24f), 0.35f, 0.62f);

            frameDarkMat = CreateMaterial(shader, "Moto_DarkFrame", new Color(0.10f, 0.11f, 0.13f), 0.45f, 0.55f);
            plasticMat = CreateMaterial(shader, "Moto_Plastic", new Color(0.08f, 0.08f, 0.09f), 0.08f, 0.32f);
            seatLeatherMat = CreateMaterial(shader, "Moto_Leather", new Color(0.12f, 0.11f, 0.12f), 0.06f, 0.28f);
            tireRubberMat = CreateMaterial(shader, "Moto_Tire", new Color(0.08f, 0.08f, 0.09f), 0.0f, 0.22f);
            rimSilverMat = CreateMaterial(shader, "Moto_Rim", new Color(0.78f, 0.80f, 0.84f), 0.82f, 0.82f);
            chromeMat = CreateMaterial(shader, "Moto_Chrome", new Color(0.88f, 0.90f, 0.94f), 0.92f, 0.90f);
            steelMat = CreateMaterial(shader, "Moto_Steel", new Color(0.38f, 0.40f, 0.44f), 0.70f, 0.55f);
            deliveryBoxMat = CreateMaterial(shader, "Moto_CargoBox", new Color(0.94f, 0.42f, 0.10f), 0.18f, 0.62f);
            deliveryBoxDarkMat = CreateMaterial(shader, "Moto_CargoDark", new Color(0.16f, 0.16f, 0.18f), 0.15f, 0.45f);

            headlightMat = CreateMaterial(shader, "Moto_Headlight", new Color(1.0f, 0.97f, 0.86f), 0.08f, 0.96f, true, new Color(1.0f, 0.95f, 0.78f));
            headlightInnerMat = CreateMaterial(shader, "Moto_HeadInner", new Color(1.0f, 0.99f, 0.94f), 0.02f, 0.98f);
            taillightMat = CreateMaterial(shader, "Moto_Taillight", new Color(0.88f, 0.10f, 0.10f), 0.08f, 0.90f, true, new Color(0.90f, 0.08f, 0.08f));
            amberMat = CreateMaterial(shader, "Moto_Amber", new Color(1.0f, 0.52f, 0.08f), 0.06f, 0.86f, true, new Color(1.0f, 0.40f, 0.04f));
            windshieldMat = CreateMaterial(shader, "Moto_Windshield", new Color(0.28f, 0.52f, 0.68f, 0.42f), 0.08f, 0.94f);
            plateMat = CreateMaterial(shader, "Moto_Plate", new Color(0.92f, 0.90f, 0.82f), 0.12f, 0.38f);
            brandMat = CreateMaterial(shader, "Moto_Brand", new Color(0.96f, 0.96f, 0.97f), 0.05f, 0.35f);
        }

        private static Material CreateMaterial(Shader shader, string name, Color color, float metallic = 0.0f, float smoothness = 0.5f, bool isEmissive = false, Color emissiveColor = default)
        {
            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (isEmissive)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissiveColor * 1.4f);
            }
            return mat;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
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

        public static GameObject CreateCourierMotorcycle(int colorIndex, out Transform[] wheels, out Light headlightComp, out Transform driverSeatMount)
        {
            InitMaterials();

            GameObject motoRoot = new GameObject("Courier_Motorcycle");
            Transform rootT = motoRoot.transform;

            Material body = (colorIndex % 3 == 0) ? bodyMatRed : ((colorIndex % 3 == 1) ? bodyMatTeal : bodyMatYellow);

            BuildChassis(rootT, body);
            BuildSeat(rootT, out driverSeatMount);
            BuildHandlebarsAndControls(rootT);
            wheels = new Transform[2];
            BuildWheelsAndSuspension(rootT, wheels);
            BuildPowertrain(rootT);
            BuildCargoBox(rootT);
            headlightComp = BuildLights(rootT);
            BindHeadlights(rootT, headlightComp);

            BoxCollider col = motoRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.62f, -0.04f);
            col.size = new Vector3(0.78f, 1.22f, 1.92f);

            return motoRoot;
        }

        private static void BuildChassis(Transform root, Material body)
        {
            Cube(root, "Moto_Spine", new Vector3(0f, 0.42f, -0.02f), new Vector3(0.10f, 0.10f, 1.18f), steelMat);
            Cube(root, "Moto_Underbone", new Vector3(0f, 0.28f, 0.04f), new Vector3(0.16f, 0.08f, 0.92f), frameDarkMat);

            Cube(root, "Moto_FloorStep", new Vector3(0f, 0.20f, 0.10f), new Vector3(0.54f, 0.05f, 0.62f), plasticMat);
            Cube(root, "Moto_FloorRib", new Vector3(0f, 0.23f, 0.10f), new Vector3(0.42f, 0.02f, 0.50f), frameDarkMat);
            Cube(root, "Peg_L", new Vector3(-0.28f, 0.21f, 0.08f), new Vector3(0.10f, 0.03f, 0.12f), steelMat);
            Cube(root, "Peg_R", new Vector3(0.28f, 0.21f, 0.08f), new Vector3(0.10f, 0.03f, 0.12f), steelMat);

            Cube(root, "Moto_SidePanel_L", new Vector3(-0.20f, 0.40f, 0.02f), new Vector3(0.08f, 0.28f, 0.72f), body);
            Cube(root, "Moto_SidePanel_R", new Vector3(0.20f, 0.40f, 0.02f), new Vector3(0.08f, 0.28f, 0.72f), body);
            Cube(root, "Moto_Belly", new Vector3(0f, 0.36f, 0.06f), new Vector3(0.36f, 0.16f, 0.78f), body);
            Cube(root, "Moto_TailCowl", new Vector3(0f, 0.48f, -0.38f), new Vector3(0.34f, 0.14f, 0.36f), body, new Vector3(-8f, 0f, 0f));

            Cube(root, "Moto_FrontShield", new Vector3(0f, 0.58f, 0.38f), new Vector3(0.44f, 0.38f, 0.16f), body, new Vector3(18f, 0f, 0f));
            Cube(root, "Moto_FrontBeak", new Vector3(0f, 0.46f, 0.52f), new Vector3(0.38f, 0.16f, 0.22f), body, new Vector3(12f, 0f, 0f));
            Cube(root, "Moto_DashHood", new Vector3(0f, 0.78f, 0.36f), new Vector3(0.36f, 0.10f, 0.18f), bodyMatDark);

            Cube(root, "Moto_Windshield", new Vector3(0f, 1.04f, 0.42f), new Vector3(0.34f, 0.30f, 0.018f), windshieldMat, new Vector3(28f, 0f, 0f));
            Cube(root, "Moto_Cluster", new Vector3(0f, 0.90f, 0.34f), new Vector3(0.18f, 0.06f, 0.08f), plasticMat, new Vector3(-18f, 0f, 0f));
            Cube(root, "Moto_ClusterGlass", new Vector3(0f, 0.905f, 0.375f), new Vector3(0.14f, 0.03f, 0.02f), headlightInnerMat, new Vector3(-18f, 0f, 0f));

            Cube(root, "Kickstand", new Vector3(-0.16f, 0.12f, -0.08f), new Vector3(0.03f, 0.18f, 0.03f), steelMat, new Vector3(0f, 0f, 28f));
        }

        private static void BuildSeat(Transform root, out Transform driverSeatMount)
        {
            Cube(root, "Moto_Seat_Base", new Vector3(0f, 0.58f, -0.10f), new Vector3(0.34f, 0.08f, 0.52f), frameDarkMat);
            Cube(root, "Moto_Seat", new Vector3(0f, 0.64f, -0.08f), new Vector3(0.32f, 0.09f, 0.50f), seatLeatherMat, new Vector3(-6f, 0f, 0f));
            Cube(root, "Moto_Seat_Nose", new Vector3(0f, 0.62f, 0.14f), new Vector3(0.22f, 0.07f, 0.16f), seatLeatherMat, new Vector3(-12f, 0f, 0f));
            Cube(root, "Moto_Seat_Tail", new Vector3(0f, 0.66f, -0.30f), new Vector3(0.28f, 0.07f, 0.14f), seatLeatherMat);

            GameObject seatMount = new GameObject("Driver_Seat_MountPoint");
            seatMount.transform.SetParent(root, false);
            seatMount.transform.localPosition = new Vector3(0f, 0.68f, -0.06f);
            driverSeatMount = seatMount.transform;
        }

        private static void BuildHandlebarsAndControls(Transform root)
        {
            Cyl(root, "Moto_SteerStem", new Vector3(0f, 0.78f, 0.40f), new Vector3(16f, 0f, 0f), new Vector3(0.045f, 0.16f, 0.045f), chromeMat);
            Cyl(root, "Moto_Handlebar", new Vector3(0f, 0.98f, 0.38f), new Vector3(0f, 0f, 90f), new Vector3(0.032f, 0.30f, 0.032f), chromeMat);

            for (int s = -1; s <= 1; s += 2)
            {
                Cyl(root, s < 0 ? "Grip_Sleeve_L" : "Grip_Sleeve_R",
                    new Vector3(s * 0.30f, 0.98f, 0.38f), new Vector3(0f, 0f, 90f), new Vector3(0.042f, 0.07f, 0.042f), plasticMat);
                Cube(root, s < 0 ? "Lever_L" : "Lever_R",
                    new Vector3(s * 0.26f, 0.96f, 0.46f), new Vector3(0.12f, 0.012f, 0.012f), chromeMat, new Vector3(0f, s * -18f, 8f));
                Cube(root, s < 0 ? "Mirror_Arm" : "Mirror_Arm_R",
                    new Vector3(s * 0.34f, 1.06f, 0.36f), new Vector3(0.018f, 0.12f, 0.018f), chromeMat, new Vector3(12f, 0f, s * -8f));
                Cube(root, s < 0 ? "Mirror_L" : "Mirror_R",
                    new Vector3(s * 0.38f, 1.14f, 0.34f), new Vector3(0.11f, 0.07f, 0.025f), chromeMat, new Vector3(12f, s * 18f, 0f));
                Cube(root, s < 0 ? "MirrorGlass_L" : "MirrorGlass_R",
                    new Vector3(s * 0.38f, 1.14f, 0.328f), new Vector3(0.09f, 0.055f, 0.01f), windshieldMat, new Vector3(12f, s * 18f, 0f));

                GameObject grip = new GameObject(s < 0 ? "Grip_L" : "Grip_R");
                grip.transform.SetParent(root, false);
                grip.transform.localPosition = new Vector3(s * 0.32f, 0.98f, 0.38f);
            }
        }

        private static void BuildWheelsAndSuspension(Transform root, Transform[] wheels)
        {
            GameObject frontHub = new GameObject("Front_Wheel_Hub");
            frontHub.transform.SetParent(root, false);
            frontHub.transform.localPosition = new Vector3(0f, 0.22f, 0.68f);
            wheels[0] = frontHub.transform;
            BuildWheelMesh(frontHub.transform, true);

            GameObject rearHub = new GameObject("Rear_Wheel_Hub");
            rearHub.transform.SetParent(root, false);
            rearHub.transform.localPosition = new Vector3(0f, 0.22f, -0.62f);
            wheels[1] = rearHub.transform;
            BuildWheelMesh(rearHub.transform, false);

            for (int f = -1; f <= 1; f += 2)
            {
                Cyl(root, "Fork_" + f, new Vector3(f * 0.11f, 0.52f, 0.56f), new Vector3(18f, 0f, 0f), new Vector3(0.036f, 0.34f, 0.036f), chromeMat);
                Cyl(root, "ForkLower_" + f, new Vector3(f * 0.11f, 0.30f, 0.64f), new Vector3(18f, 0f, 0f), new Vector3(0.042f, 0.14f, 0.042f), steelMat);
            }

            Cube(root, "Fender_F", new Vector3(0f, 0.42f, 0.70f), new Vector3(0.22f, 0.05f, 0.28f), bodyMatDark, new Vector3(18f, 0f, 0f));
            Cube(root, "Fender_R", new Vector3(0f, 0.40f, -0.66f), new Vector3(0.24f, 0.05f, 0.26f), bodyMatDark, new Vector3(-12f, 0f, 0f));

            Cube(root, "Swingarm", new Vector3(0.12f, 0.24f, -0.36f), new Vector3(0.04f, 0.05f, 0.42f), steelMat);
            Cube(root, "Shock", new Vector3(0.14f, 0.40f, -0.42f), new Vector3(0.04f, 0.22f, 0.04f), chromeMat, new Vector3(18f, 0f, 0f));
        }

        private static void BuildPowertrain(Transform root)
        {
            Cube(root, "Engine_Block", new Vector3(0.02f, 0.30f, -0.18f), new Vector3(0.22f, 0.18f, 0.28f), steelMat);
            Cube(root, "Engine_Head", new Vector3(0.02f, 0.42f, -0.14f), new Vector3(0.18f, 0.08f, 0.18f), frameDarkMat);
            Cyl(root, "Engine_Fin", new Vector3(0.14f, 0.34f, -0.16f), new Vector3(0f, 0f, 90f), new Vector3(0.16f, 0.04f, 0.16f), steelMat);
            Cyl(root, "Exhaust_Header", new Vector3(0.18f, 0.22f, -0.28f), new Vector3(78f, 12f, 0f), new Vector3(0.045f, 0.22f, 0.045f), chromeMat);
            Cyl(root, "Exhaust_Can", new Vector3(0.22f, 0.20f, -0.52f), new Vector3(88f, 0f, 0f), new Vector3(0.08f, 0.20f, 0.08f), chromeMat);
            Cube(root, "Exhaust_Heat", new Vector3(0.22f, 0.26f, -0.46f), new Vector3(0.06f, 0.03f, 0.16f), steelMat);

            Cube(root, "Chain_Guard", new Vector3(0.14f, 0.22f, -0.40f), new Vector3(0.03f, 0.08f, 0.28f), plasticMat);
            Cyl(root, "Sprocket", new Vector3(0.12f, 0.22f, -0.62f), new Vector3(0f, 0f, 90f), new Vector3(0.16f, 0.02f, 0.16f), steelMat);
        }

        private static void BuildCargoBox(Transform root)
        {
            Cube(root, "Moto_CargoRack", new Vector3(0f, 0.58f, -0.62f), new Vector3(0.50f, 0.05f, 0.46f), steelMat);
            Cube(root, "Moto_RackStay_L", new Vector3(-0.18f, 0.50f, -0.48f), new Vector3(0.03f, 0.16f, 0.03f), steelMat);
            Cube(root, "Moto_RackStay_R", new Vector3(0.18f, 0.50f, -0.48f), new Vector3(0.03f, 0.16f, 0.03f), steelMat);

            GameObject box = Cube(root, "Moto_Delivery_CargoBox", new Vector3(0f, 0.88f, -0.66f), new Vector3(0.54f, 0.54f, 0.50f), deliveryBoxMat);
            Cube(box.transform, "CargoBox_Lid", new Vector3(0f, 0.52f, 0f), new Vector3(1.04f, 0.08f, 1.04f), deliveryBoxDarkMat);
            Cube(box.transform, "CargoBox_Latch", new Vector3(0f, 0.28f, 0.52f), new Vector3(0.18f, 0.08f, 0.06f), chromeMat);
            Cube(box.transform, "CargoBox_Brand", new Vector3(0f, 0.06f, 0.51f), new Vector3(0.42f, 0.16f, 0.03f), brandMat);
            Cube(box.transform, "CargoBox_BrandBar", new Vector3(0f, -0.08f, 0.51f), new Vector3(0.42f, 0.04f, 0.03f), frameDarkMat);

            for (int s = -1; s <= 1; s += 2)
            {
                Cube(box.transform, "CargoReflect_" + s, new Vector3(s * 0.51f, 0f, 0f), new Vector3(0.03f, 0.14f, 0.72f), amberMat);
            }

            Cube(root, "GrabRail", new Vector3(0f, 0.72f, -0.40f), new Vector3(0.28f, 0.03f, 0.03f), chromeMat);
        }

        private static Light BuildLights(Transform root)
        {
            Cube(root, "Moto_HeadHousing", new Vector3(0f, 0.72f, 0.60f), new Vector3(0.26f, 0.16f, 0.10f), plasticMat);
            GameObject lens = Sph(root, "Moto_Headlight_Lens", new Vector3(0f, 0.72f, 0.66f), 0.16f, headlightMat);
            lens.name = "Moto_Headlight_Lens";
            Sph(root, "Moto_HeadInner", new Vector3(0f, 0.72f, 0.68f), 0.10f, headlightInnerMat);

            Cube(root, "Signal_FL", new Vector3(-0.20f, 0.68f, 0.56f), new Vector3(0.06f, 0.04f, 0.04f), amberMat);
            Cube(root, "Signal_FR", new Vector3(0.20f, 0.68f, 0.56f), new Vector3(0.06f, 0.04f, 0.04f), amberMat);

            Cube(root, "Moto_Taillight", new Vector3(0f, 0.62f, -0.90f), new Vector3(0.24f, 0.08f, 0.04f), taillightMat);
            Cube(root, "Signal_RL", new Vector3(-0.16f, 0.58f, -0.88f), new Vector3(0.06f, 0.04f, 0.03f), amberMat);
            Cube(root, "Signal_RR", new Vector3(0.16f, 0.58f, -0.88f), new Vector3(0.06f, 0.04f, 0.03f), amberMat);
            Cube(root, "Plate", new Vector3(0f, 0.48f, -0.90f), new Vector3(0.20f, 0.10f, 0.02f), plateMat);

            GameObject lightObj = new GameObject("Moto_Night_SpotLight");
            lightObj.transform.SetParent(root, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.72f, 0.70f);
            lightObj.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

            Light spot = lightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 18f;
            spot.spotAngle = 52f;
            spot.intensity = 3.1f;
            spot.color = new Color(1f, 0.96f, 0.84f);
            spot.enabled = false;
            return spot;
        }

        private static void BindHeadlights(Transform root, Light spot)
        {
            VehicleHeadlightController ctrl = root.gameObject.GetComponent<VehicleHeadlightController>();
            if (ctrl == null) ctrl = root.gameObject.AddComponent<VehicleHeadlightController>();
            ctrl.isEngineRunning = false;
            if (spot != null && !ctrl.spotLights.Contains(spot))
            {
                ctrl.spotLights.Add(spot);
            }

            Transform lens = root.Find("Moto_Headlight_Lens");
            if (lens != null)
            {
                Renderer r = lens.GetComponent<Renderer>();
                if (r != null && !ctrl.headlightRenderers.Contains(r))
                {
                    ctrl.headlightRenderers.Add(r);
                }
            }

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterVehicleHeadlightController(ctrl);
            }

            ctrl.UpdateHeadlights();
        }

        private static void BuildWheelMesh(Transform wheelParent, bool front)
        {
            float tireD = front ? 0.42f : 0.44f;
            Cyl(wheelParent, "Tire_Mesh", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(tireD, 0.11f, tireD), tireRubberMat);
            Cyl(wheelParent, "Tire_Shoulder", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(tireD * 0.92f, 0.125f, tireD * 0.92f), tireRubberMat);
            Cyl(wheelParent, "Rim_Mesh", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(tireD * 0.70f, 0.13f, tireD * 0.70f), rimSilverMat);
            Cyl(wheelParent, "Rim_Inner", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(tireD * 0.42f, 0.08f, tireD * 0.42f), plasticMat);
            Cyl(wheelParent, "Axle_Pin", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.08f, 0.15f, 0.08f), chromeMat);
            Cyl(wheelParent, "Disc", new Vector3(front ? -0.08f : 0.08f, 0f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.28f, 0.012f, 0.28f), steelMat);

            for (int i = 0; i < 8; i++)
            {
                float a = i * 22.5f;
                Cube(wheelParent, "Spoke_" + i, Vector3.zero, new Vector3(0.018f, tireD * 0.58f, 0.018f), rimSilverMat, new Vector3(a, 0f, 0f));
            }
        }
    }
}
