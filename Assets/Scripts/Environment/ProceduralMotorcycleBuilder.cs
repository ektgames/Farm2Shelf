using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Şık, detaylı ve Low-Poly Kurye Motorsikleti (Scooter / Delivery Motorbike)
    /// üreten usulü 3D modelleme oluşturucu.
    /// Tekerlek döndürme, arka teslimat çantası/sepeti ve gece farlarını destekler.
    /// </summary>
    public static class ProceduralMotorcycleBuilder
    {
        private static Material bodyMatRed;
        private static Material bodyMatTeal;
        private static Material bodyMatYellow;
        private static Material frameDarkMat;
        private static Material seatLeatherMat;
        private static Material tireRubberMat;
        private static Material rimSilverMat;
        private static Material chromeMat;
        private static Material deliveryBoxMat;
        private static Material headlightMat;
        private static Material taillightMat;
        private static Material windshieldMat;

        private static void InitMaterials()
        {
            if (bodyMatRed != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Lightweight Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            bodyMatRed = CreateMaterial(shader, "Moto_Red", new Color(0.92f, 0.22f, 0.22f), 0.3f, 0.8f);
            bodyMatTeal = CreateMaterial(shader, "Moto_Teal", new Color(0.12f, 0.78f, 0.75f), 0.3f, 0.8f);
            bodyMatYellow = CreateMaterial(shader, "Moto_Yellow", new Color(0.98f, 0.78f, 0.12f), 0.3f, 0.8f);

            frameDarkMat = CreateMaterial(shader, "Moto_DarkFrame", new Color(0.12f, 0.14f, 0.18f), 0.5f, 0.7f);
            seatLeatherMat = CreateMaterial(shader, "Moto_Leather", new Color(0.15f, 0.15f, 0.17f), 0.1f, 0.4f);
            tireRubberMat = CreateMaterial(shader, "Moto_Tire", new Color(0.10f, 0.10f, 0.12f), 0.0f, 0.3f);
            rimSilverMat = CreateMaterial(shader, "Moto_Rim", new Color(0.78f, 0.82f, 0.86f), 0.8f, 0.85f);
            chromeMat = CreateMaterial(shader, "Moto_Chrome", new Color(0.90f, 0.92f, 0.95f), 0.9f, 0.9f);
            deliveryBoxMat = CreateMaterial(shader, "Moto_CargoBox", new Color(0.95f, 0.45f, 0.15f), 0.2f, 0.7f);

            headlightMat = CreateMaterial(shader, "Moto_Headlight", new Color(1.0f, 0.98f, 0.85f), 0.1f, 0.95f, true, new Color(1.0f, 0.95f, 0.80f));
            taillightMat = CreateMaterial(shader, "Moto_Taillight", new Color(0.90f, 0.15f, 0.15f), 0.1f, 0.9f, true, new Color(0.90f, 0.10f, 0.10f));
            windshieldMat = CreateMaterial(shader, "Moto_Windshield", new Color(0.35f, 0.75f, 0.95f, 0.45f), 0.1f, 0.95f);
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
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissiveColor * 1.5f);
            }
            return mat;
        }

        public static GameObject CreateCourierMotorcycle(int colorIndex, out Transform[] wheels, out Light headlightComp, out Transform driverSeatMount)
        {
            InitMaterials();

            GameObject motoRoot = new GameObject("Courier_Motorcycle");
            Transform rootT = motoRoot.transform;

            Material activeBodyMat = (colorIndex % 3 == 0) ? bodyMatRed : ((colorIndex % 3 == 1) ? bodyMatTeal : bodyMatYellow);

            // 1. ANA GÖVDE VE ŞASİ (Chassis & Main Body)
            GameObject bodyBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyBase.name = "Moto_Body_Base";
            bodyBase.transform.SetParent(rootT, false);
            bodyBase.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            bodyBase.transform.localScale = new Vector3(0.42f, 0.32f, 1.25f);
            bodyBase.GetComponent<Renderer>().sharedMaterial = activeBodyMat;
            Object.Destroy(bodyBase.GetComponent<Collider>());

            // Ön Grenaj & Çamurluk Eğimli Panel (Front Fairing)
            GameObject frontFairing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontFairing.name = "Moto_Front_Fairing";
            frontFairing.transform.SetParent(rootT, false);
            frontFairing.transform.localPosition = new Vector3(0f, 0.65f, 0.42f);
            frontFairing.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            frontFairing.transform.localScale = new Vector3(0.40f, 0.46f, 0.35f);
            frontFairing.GetComponent<Renderer>().sharedMaterial = activeBodyMat;
            Object.Destroy(frontFairing.GetComponent<Collider>());

            // Ayak Basma Platformu (Footrest Step Floor)
            GameObject footFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            footFloor.name = "Moto_FloorStep";
            footFloor.transform.SetParent(rootT, false);
            footFloor.transform.localPosition = new Vector3(0f, 0.22f, 0.05f);
            footFloor.transform.localScale = new Vector3(0.50f, 0.06f, 0.55f);
            footFloor.GetComponent<Renderer>().sharedMaterial = frameDarkMat;
            Object.Destroy(footFloor.GetComponent<Collider>());

            // 2. SELE / DERİ SÜRÜCÜ KOLTUĞU (Saddle / Seat)
            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Moto_Seat";
            seat.transform.SetParent(rootT, false);
            seat.transform.localPosition = new Vector3(0f, 0.54f, -0.15f);
            seat.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
            seat.transform.localScale = new Vector3(0.36f, 0.12f, 0.58f);
            seat.GetComponent<Renderer>().sharedMaterial = seatLeatherMat;
            Object.Destroy(seat.GetComponent<Collider>());

            // Sürücü Oturma Noktası (Driver Mount Point)
            GameObject seatMount = new GameObject("Driver_Seat_MountPoint");
            seatMount.transform.SetParent(rootT, false);
            seatMount.transform.localPosition = new Vector3(0f, 0.58f, -0.15f);
            driverSeatMount = seatMount.transform;

            // 3. DİREKSİYON & GİDON (Handlebars & Mirrors)
            GameObject handlebar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handlebar.name = "Moto_Handlebar";
            handlebar.transform.SetParent(rootT, false);
            handlebar.transform.localPosition = new Vector3(0f, 0.92f, 0.40f);
            handlebar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            handlebar.transform.localScale = new Vector3(0.04f, 0.32f, 0.04f);
            handlebar.GetComponent<Renderer>().sharedMaterial = chromeMat;
            Object.Destroy(handlebar.GetComponent<Collider>());

            // Sol ve Sağ Aynalar
            for (int m = -1; m <= 1; m += 2)
            {
                GameObject mirror = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mirror.name = (m < 0) ? "Mirror_L" : "Mirror_R";
                mirror.transform.SetParent(rootT, false);
                mirror.transform.localPosition = new Vector3(m * 0.32f, 1.05f, 0.42f);
                mirror.transform.localRotation = Quaternion.Euler(15f, m * 15f, 0f);
                mirror.transform.localScale = new Vector3(0.10f, 0.07f, 0.03f);
                mirror.GetComponent<Renderer>().sharedMaterial = chromeMat;
                Object.Destroy(mirror.GetComponent<Collider>());
            }

            // Ön Rüzgarlık / Siperlik (Windshield)
            GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windshield.name = "Moto_Windshield";
            windshield.transform.SetParent(rootT, false);
            windshield.transform.localPosition = new Vector3(0f, 1.02f, 0.44f);
            windshield.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            windshield.transform.localScale = new Vector3(0.32f, 0.28f, 0.02f);
            Renderer wsRend = windshield.GetComponent<Renderer>();
            wsRend.sharedMaterial = windshieldMat;
            wsRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(windshield.GetComponent<Collider>());

            // 4. ÖN VE ARKA TEKERLEKLER (Front & Rear Wheels)
            wheels = new Transform[2];

            // Ön Tekerlek (Front Wheel: Z = +0.65m)
            GameObject frontWheelHub = new GameObject("Front_Wheel_Hub");
            frontWheelHub.transform.SetParent(rootT, false);
            frontWheelHub.transform.localPosition = new Vector3(0f, 0.22f, 0.65f);
            wheels[0] = frontWheelHub.transform;
            BuildWheelMesh(frontWheelHub.transform);

            // Arka Tekerlek (Rear Wheel: Z = -0.58m)
            GameObject rearWheelHub = new GameObject("Rear_Wheel_Hub");
            rearWheelHub.transform.SetParent(rootT, false);
            rearWheelHub.transform.localPosition = new Vector3(0f, 0.22f, -0.58f);
            wheels[1] = rearWheelHub.transform;
            BuildWheelMesh(rearWheelHub.transform);

            // Ön Çatal / Maşa (Front Fork)
            for (int f = -1; f <= 1; f += 2)
            {
                GameObject fork = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fork.name = "Fork_" + f;
                fork.transform.SetParent(rootT, false);
                fork.transform.localPosition = new Vector3(f * 0.12f, 0.50f, 0.54f);
                fork.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
                fork.transform.localScale = new Vector3(0.04f, 0.35f, 0.04f);
                fork.GetComponent<Renderer>().sharedMaterial = chromeMat;
                Object.Destroy(fork.GetComponent<Collider>());
            }

            // Egzoz Borusu (Exhaust Pipe)
            GameObject exhaust = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            exhaust.name = "Exhaust_Pipe";
            exhaust.transform.SetParent(rootT, false);
            exhaust.transform.localPosition = new Vector3(0.24f, 0.20f, -0.45f);
            exhaust.transform.localRotation = Quaternion.Euler(85f, 0f, 0f);
            exhaust.transform.localScale = new Vector3(0.07f, 0.32f, 0.07f);
            exhaust.GetComponent<Renderer>().sharedMaterial = chromeMat;
            Object.Destroy(exhaust.GetComponent<Collider>());

            // 5. ARKA TERMAL KURYE ÇANTASI / SEPETİ (Courier Cargo Delivery Box)
            GameObject cargoRack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cargoRack.name = "Moto_CargoRack";
            cargoRack.transform.SetParent(rootT, false);
            cargoRack.transform.localPosition = new Vector3(0f, 0.52f, -0.58f);
            cargoRack.transform.localScale = new Vector3(0.46f, 0.06f, 0.46f);
            cargoRack.GetComponent<Renderer>().sharedMaterial = frameDarkMat;
            Object.Destroy(cargoRack.GetComponent<Collider>());

            GameObject cargoBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cargoBox.name = "Moto_Delivery_CargoBox";
            cargoBox.transform.SetParent(rootT, false);
            cargoBox.transform.localPosition = new Vector3(0f, 0.82f, -0.58f);
            cargoBox.transform.localScale = new Vector3(0.52f, 0.52f, 0.52f);
            cargoBox.GetComponent<Renderer>().sharedMaterial = deliveryBoxMat;
            Object.Destroy(cargoBox.GetComponent<Collider>());

            // Koli Kapağı ve Şeritler
            GameObject boxLid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxLid.name = "CargoBox_Lid";
            boxLid.transform.SetParent(cargoBox.transform, false);
            boxLid.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            boxLid.transform.localScale = new Vector3(1.04f, 0.08f, 1.04f);
            boxLid.GetComponent<Renderer>().sharedMaterial = frameDarkMat;
            Object.Destroy(boxLid.GetComponent<Collider>());

            // Reflektör Sarı / Gümüş Şeritler
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "CargoBox_Stripe_" + s;
                stripe.transform.SetParent(cargoBox.transform, false);
                stripe.transform.localPosition = new Vector3(s * 0.51f, 0f, 0f);
                stripe.transform.localScale = new Vector3(0.02f, 0.12f, 0.85f);
                stripe.GetComponent<Renderer>().sharedMaterial = headlightMat;
                Object.Destroy(stripe.GetComponent<Collider>());
            }

            // 6. ÖN FAR & ARKA STOP LAMBASI (Headlight & Taillight)
            GameObject headlightMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            headlightMesh.name = "Moto_Headlight_Lens";
            headlightMesh.transform.SetParent(rootT, false);
            headlightMesh.transform.localPosition = new Vector3(0f, 0.72f, 0.58f);
            headlightMesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            headlightMesh.transform.localScale = new Vector3(0.18f, 0.06f, 0.18f);
            headlightMesh.GetComponent<Renderer>().sharedMaterial = headlightMat;
            Object.Destroy(headlightMesh.GetComponent<Collider>());

            // Gece Aydınlatma Işığı (Real Light)
            GameObject lightObj = new GameObject("Moto_Night_SpotLight");
            lightObj.transform.SetParent(headlightMesh.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            lightObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Light spotLight = lightObj.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.range = 16.0f;
            spotLight.spotAngle = 65.0f;
            spotLight.intensity = 2.4f;
            spotLight.color = new Color(1.0f, 0.96f, 0.85f);
            spotLight.enabled = false; // Başlangıçta gündüzse kapalı
            headlightComp = spotLight;

            // Arka Kırmızı Stop Lambası
            GameObject taillight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            taillight.name = "Moto_Taillight";
            taillight.transform.SetParent(rootT, false);
            taillight.transform.localPosition = new Vector3(0f, 0.56f, -0.84f);
            taillight.transform.localScale = new Vector3(0.22f, 0.08f, 0.04f);
            taillight.GetComponent<Renderer>().sharedMaterial = taillightMat;
            Object.Destroy(taillight.GetComponent<Collider>());

            // Çarpışma Kutusu (Box Collider)
            BoxCollider col = motoRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.58f, 0f);
            col.size = new Vector3(0.70f, 1.15f, 1.85f);

            return motoRoot;
        }

        private static void BuildWheelMesh(Transform wheelParent)
        {
            // Dış Lastik (Tire)
            GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tire.name = "Tire_Mesh";
            tire.transform.SetParent(wheelParent, false);
            tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tire.transform.localScale = new Vector3(0.44f, 0.12f, 0.44f);
            tire.GetComponent<Renderer>().sharedMaterial = tireRubberMat;
            Object.Destroy(tire.GetComponent<Collider>());

            // İç Gümüş Jant (Rim)
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim_Mesh";
            rim.transform.SetParent(wheelParent, false);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rim.transform.localScale = new Vector3(0.32f, 0.13f, 0.32f);
            rim.GetComponent<Renderer>().sharedMaterial = rimSilverMat;
            Object.Destroy(rim.GetComponent<Collider>());

            // Jant Göbek Pimi (Axle Pin)
            GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pin.name = "Axle_Pin";
            pin.transform.SetParent(wheelParent, false);
            pin.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            pin.transform.localScale = new Vector3(0.10f, 0.15f, 0.10f);
            pin.GetComponent<Renderer>().sharedMaterial = chromeMat;
            Object.Destroy(pin.GetComponent<Collider>());
        }
    }
}
