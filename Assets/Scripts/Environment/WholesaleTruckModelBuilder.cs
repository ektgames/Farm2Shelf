using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Şerit genişliğine (3.0m) tam uyan low-poly kapalı kasa toptancı kamyonu 3D model üreticisi.
    /// Kamyon genişliği 1.75m olarak ayarlanarak şeridin içine tam sığması sağlanmıştır.
    /// </summary>
    public static class WholesaleTruckModelBuilder
    {
        public static GameObject CreateTruckModel(out Transform[] rotatingWheels, out Transform rearDoors, Color? customCabColor = null, Color? customBoxStripeColor = null)
        {
            GameObject truckRoot = new GameObject("Wholesale_Box_Truck");
            truckRoot.transform.localScale = Vector3.one;

            // Materyaller
            Color cabColor = customCabColor ?? new Color(0.12f, 0.55f, 0.85f); // Varsayılan Mavi veya Özel Yeşil
            Color stripeColor = customBoxStripeColor ?? new Color(0.95f, 0.45f, 0.10f);

            Material cabMat = CreateMaterial("Truck_Cab_Mat", cabColor);
            Material boxMat = CreateMaterial("Truck_Box_Mat", new Color(0.92f, 0.94f, 0.96f)); // Krem Beyaz Kasa
            Material boxStripeMat = CreateMaterial("Truck_Stripe_Mat", stripeColor);
            Material chassisMat = CreateMaterial("Truck_Chassis_Mat", new Color(0.12f, 0.14f, 0.16f)); // Siyah Şasi
            Material chromeMat = CreateMaterial("Truck_Chrome_Mat", new Color(0.85f, 0.88f, 0.92f), metallic: 0.9f, smoothness: 0.85f);
            Material doorMetalMat = CreateMaterial("Truck_DoorMetal_Mat", new Color(0.35f, 0.38f, 0.42f));
            Material glassMat = CreateMaterial("Truck_Glass_Mat", new Color(0.20f, 0.35f, 0.45f, 0.75f), transparent: true);
            Material tireMat = CreateMaterial("Truck_Tire_Mat", new Color(0.15f, 0.15f, 0.15f));
            Material rimMat = CreateMaterial("Truck_Rim_Mat", new Color(0.75f, 0.75f, 0.78f), metallic: 0.8f);

            Material headlightMat = CreateMaterial("Truck_Headlight_Mat", new Color(1.0f, 0.98f, 0.70f));
            headlightMat.EnableKeyword("_EMISSION");
            headlightMat.SetColor("_EmissionColor", new Color(1.0f, 0.95f, 0.50f) * 4.0f);

            Material taillightMat = CreateMaterial("Truck_Taillight_Mat", new Color(0.90f, 0.10f, 0.10f));
            taillightMat.EnableKeyword("_EMISSION");
            taillightMat.SetColor("_EmissionColor", new Color(0.85f, 0.10f, 0.10f) * 2.5f);

            // 1. ŞASİ & TABAN (Chassis Frame: 1.6m G x 0.22m Y x 4.6m D)
            CreatePrimitive(truckRoot, "Chassis", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(1.6f, 0.22f, 4.6f), chassisMat);

            // 2. KAPALI KASA (Cargo Box: 1.75m G x 1.8m Y x 3.4m D - 3.0m Yol şeridine tam ölçekli!)
            CreatePrimitive(truckRoot, "CargoBox", PrimitiveType.Cube, new Vector3(0f, 1.45f, -0.5f), new Vector3(1.75f, 1.8f, 3.4f), boxMat);
            
            // Kasa Yan Dekoratif Turuncu Şeritleri (Kasa boyuyla hizada)
            CreatePrimitive(truckRoot, "Stripe_L", PrimitiveType.Cube, new Vector3(-0.88f, 1.45f, -0.5f), new Vector3(0.03f, 0.28f, 3.35f), boxStripeMat);
            CreatePrimitive(truckRoot, "Stripe_R", PrimitiveType.Cube, new Vector3(0.88f, 1.45f, -0.5f), new Vector3(0.03f, 0.28f, 3.35f), boxStripeMat);

            // Arka Çift Kapı Grubu (Rear Doors)
            GameObject rearDoorGroup = new GameObject("Rear_Door_Group");
            rearDoorGroup.transform.SetParent(truckRoot.transform, false);
            rearDoorGroup.transform.localPosition = new Vector3(0f, 1.45f, -2.21f);
            rearDoors = rearDoorGroup.transform;

            CreatePrimitive(rearDoorGroup, "Door_L", PrimitiveType.Cube, new Vector3(-0.41f, 0f, 0f), new Vector3(0.80f, 1.68f, 0.04f), doorMetalMat);
            CreatePrimitive(rearDoorGroup, "Door_R", PrimitiveType.Cube, new Vector3(0.41f, 0f, 0f), new Vector3(0.80f, 1.68f, 0.04f), doorMetalMat);
            CreatePrimitive(rearDoorGroup, "LockHandle", PrimitiveType.Cube, new Vector3(0f, 0f, -0.03f), new Vector3(0.06f, 0.5f, 0.03f), chromeMat);

            // 3. LOW-POLY KABİN (Driver Cab: 1.65m G x 1.3m Y x 1.2m D)
            CreatePrimitive(truckRoot, "CabMain", PrimitiveType.Cube, new Vector3(0f, 1.25f, 1.6f), new Vector3(1.65f, 1.3f, 1.2f), cabMat);
            CreatePrimitive(truckRoot, "CabNose", PrimitiveType.Cube, new Vector3(0f, 0.78f, 2.22f), new Vector3(1.60f, 0.65f, 0.45f), cabMat);

            // Camlar (Windows)
            CreatePrimitive(truckRoot, "Windshield", PrimitiveType.Cube, new Vector3(0f, 1.48f, 2.16f), new Vector3(1.48f, 0.55f, 0.05f), glassMat);
            CreatePrimitive(truckRoot, "SideWin_L", PrimitiveType.Cube, new Vector3(-0.83f, 1.48f, 1.7f), new Vector3(0.05f, 0.48f, 0.65f), glassMat);
            CreatePrimitive(truckRoot, "SideWin_R", PrimitiveType.Cube, new Vector3(0.83f, 1.48f, 1.7f), new Vector3(0.05f, 0.48f, 0.65f), glassMat);

            // Izgara & Tampon (Grille & Bumper)
            CreatePrimitive(truckRoot, "Grille", PrimitiveType.Cube, new Vector3(0f, 0.78f, 2.45f), new Vector3(1.1f, 0.38f, 0.03f), chromeMat);
            CreatePrimitive(truckRoot, "Bumper", PrimitiveType.Cube, new Vector3(0f, 0.38f, 2.41f), new Vector3(1.68f, 0.22f, 0.12f), chromeMat);

            // Farlar & Stoplar (Standart Araç Far Yapısı - Diğer Araçlarla %100 Aynı)
            GameObject hlL = CreatePrimitive(truckRoot, "Headlight_L", PrimitiveType.Cube, new Vector3(-0.65f, 0.65f, 2.44f), new Vector3(0.26f, 0.20f, 0.05f), headlightMat);
            GameObject hlR = CreatePrimitive(truckRoot, "Headlight_R", PrimitiveType.Cube, new Vector3(0.65f, 0.65f, 2.44f), new Vector3(0.26f, 0.20f, 0.05f), headlightMat);

            CreatePrimitive(truckRoot, "Taillight_L", PrimitiveType.Cube, new Vector3(-0.72f, 0.52f, -2.22f), new Vector3(0.20f, 0.12f, 0.04f), taillightMat);
            CreatePrimitive(truckRoot, "Taillight_R", PrimitiveType.Cube, new Vector3(0.72f, 0.52f, -2.22f), new Vector3(0.20f, 0.12f, 0.04f), taillightMat);

            // Unity Spotlight Işık Kaynakları (Diğer Araçlar Gibi Gece Aydınlatması)
            GameObject spotLObj = new GameObject("Truck_SpotLight_L");
            spotLObj.transform.SetParent(truckRoot.transform, false);
            spotLObj.transform.localPosition = new Vector3(-0.65f, 0.65f, 2.46f);
            spotLObj.transform.localRotation = Quaternion.Euler(10f, -4f, 0f);

            Light spotL = spotLObj.AddComponent<Light>();
            spotL.type = LightType.Spot;
            spotL.color = new Color(1.0f, 0.96f, 0.70f);
            spotL.intensity = 3.5f;
            spotL.range = 14.0f;
            spotL.spotAngle = 50f;
            spotL.enabled = false;

            GameObject spotRObj = new GameObject("Truck_SpotLight_R");
            spotRObj.transform.SetParent(truckRoot.transform, false);
            spotRObj.transform.localPosition = new Vector3(0.65f, 0.65f, 2.46f);
            spotRObj.transform.localRotation = Quaternion.Euler(10f, 4f, 0f);

            Light spotR = spotRObj.AddComponent<Light>();
            spotR.type = LightType.Spot;
            spotR.color = new Color(1.0f, 0.96f, 0.70f);
            spotR.intensity = 3.5f;
            spotR.range = 14.0f;
            spotR.spotAngle = 50f;
            spotR.enabled = false;

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterVehicleHeadlight(spotL, hlL);
                DayNightCycleManager.Instance.RegisterVehicleHeadlight(spotR, hlR);
            }

            // 4. DÖNEN TEKERLEKLER (6 Wheel Assemblies)
            rotatingWheels = new Transform[6];
            Vector3[] wheelPositions = new Vector3[]
            {
                new Vector3(-0.82f, 0.35f, 1.6f),   // Ön Sol
                new Vector3(0.82f, 0.35f, 1.6f),    // Ön Sağ
                new Vector3(-0.82f, 0.35f, -0.9f),  // Arka Sol 1
                new Vector3(0.82f, 0.35f, -0.9f),   // Arka Sağ 1
                new Vector3(-0.82f, 0.35f, -1.6f),  // Arka Sol 2
                new Vector3(0.82f, 0.35f, -1.6f)    // Arka Sağ 2
            };

            for (int i = 0; i < wheelPositions.Length; i++)
            {
                GameObject wheelObj = new GameObject($"Wheel_{i}");
                wheelObj.transform.SetParent(truckRoot.transform, false);
                wheelObj.transform.localPosition = wheelPositions[i];
                rotatingWheels[i] = wheelObj.transform;

                // Lastik (Tire)
                GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tire.name = "Tire";
                tire.transform.SetParent(wheelObj.transform, false);
                tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                tire.transform.localScale = new Vector3(0.62f, 0.10f, 0.62f);
                tire.GetComponent<Renderer>().sharedMaterial = tireMat;
                Object.Destroy(tire.GetComponent<Collider>());

                // Jant (Rim)
                GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rim.name = "Rim";
                rim.transform.SetParent(wheelObj.transform, false);
                rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                rim.transform.localScale = new Vector3(0.40f, 0.11f, 0.40f);
                rim.GetComponent<Renderer>().sharedMaterial = rimMat;
                Object.Destroy(rim.GetComponent<Collider>());
            }

            return truckRoot;
        }

        private static GameObject CreatePrimitive(GameObject parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent.transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            Collider col = obj.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            if (mat != null && obj.GetComponent<Renderer>() != null)
            {
                obj.GetComponent<Renderer>().sharedMaterial = mat;
            }

            return obj;
        }

        private static readonly Dictionary<string, Material> truckMatCache = new Dictionary<string, Material>();

        private static Material CreateMaterial(string name, Color color, float metallic = 0f, float smoothness = 0.5f, bool transparent = false)
        {
            string key = $"TruckMat_{name}_{color.r:F3}_{color.g:F3}_{color.b:F3}_{color.a:F3}_{metallic:F2}_{smoothness:F2}_{transparent}";
            if (truckMatCache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            if (transparent)
            {
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
            }

            truckMatCache[key] = mat;
            return mat;
        }
    }
}
