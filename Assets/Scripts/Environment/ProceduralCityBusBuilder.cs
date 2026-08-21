using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Şehir İçi Belediye Otobüsü (City Bus) modelini low-poly tarzında oluşturan üretici.
    /// </summary>
    public static class ProceduralCityBusBuilder
    {
        public static GameObject CreateCityBusModel(out List<Transform> wheels, out GameObject frontDoor, out GameObject rearDoor)
        {
            wheels = new List<Transform>();
            GameObject busRoot = new GameObject("Procedural_City_Bus");

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material busYellowMat = CreateMat(shader, new Color(0.96f, 0.78f, 0.12f), 0.2f, 0.7f); // Sarı Belediye Gövdesi
            Material navyStripeMat = CreateMat(shader, new Color(0.10f, 0.20f, 0.45f), 0.2f, 0.6f); // Lacivert Kuşak
            Material roofGreyMat = CreateMat(shader, new Color(0.82f, 0.84f, 0.88f), 0.3f, 0.7f);   // Tavan Rengi
            Material glassMat = CreateMat(shader, new Color(0.15f, 0.35f, 0.55f, 0.75f), 0.1f, 0.95f); // Camlar
            Material chromeMat = CreateMat(shader, new Color(0.90f, 0.92f, 0.95f), 0.8f, 0.9f);   // Tampon / Izgara
            Material blackMat = CreateMat(shader, new Color(0.12f, 0.12f, 0.14f), 0.1f, 0.5f);    // Tekerlekler
            Material ledScreenMat = CreateMat(shader, new Color(0.05f, 0.75f, 0.90f), 0.1f, 0.9f); // LED Tabela

            // 1. Ana Otobüs Gövdesi (7.6m uzunluk, 2.5m yükseklik, 2.3m genişlik)
            GameObject body = CreatePrimitive(busRoot, "Bus_Body", PrimitiveType.Cube, new Vector3(0f, 1.4f, 0f), new Vector3(2.3f, 2.2f, 7.6f), busYellowMat);

            // 2. Alt Lacivert Dekoratif Şerit
            CreatePrimitive(busRoot, "Stripe_L", PrimitiveType.Cube, new Vector3(-1.16f, 0.6f, 0f), new Vector3(0.04f, 0.35f, 7.55f), navyStripeMat);
            CreatePrimitive(busRoot, "Stripe_R", PrimitiveType.Cube, new Vector3(1.16f, 0.6f, 0f), new Vector3(0.04f, 0.35f, 7.55f), navyStripeMat);

            // 3. Tavan Klima Üniteleri
            CreatePrimitive(busRoot, "Roof_AC1", PrimitiveType.Cube, new Vector3(0f, 2.6f, 1.5f), new Vector3(1.4f, 0.25f, 1.8f), roofGreyMat);
            CreatePrimitive(busRoot, "Roof_AC2", PrimitiveType.Cube, new Vector3(0f, 2.6f, -1.8f), new Vector3(1.4f, 0.25f, 1.8f), roofGreyMat);

            // 4. Camlar (Ön Ön Cam + Yan Camlar)
            CreatePrimitive(busRoot, "Windshield_Front", PrimitiveType.Cube, new Vector3(0f, 1.7f, 3.81f), new Vector3(2.1f, 1.2f, 0.04f), glassMat);
            CreatePrimitive(busRoot, "Windshield_Back", PrimitiveType.Cube, new Vector3(0f, 1.7f, -3.81f), new Vector3(2.1f, 1.1f, 0.04f), glassMat);

            CreatePrimitive(busRoot, "SideWindows_L", PrimitiveType.Cube, new Vector3(-1.16f, 1.75f, 0f), new Vector3(0.04f, 1.0f, 7.2f), glassMat);
            CreatePrimitive(busRoot, "SideWindows_R", PrimitiveType.Cube, new Vector3(1.16f, 1.75f, 0.5f), new Vector3(0.04f, 1.0f, 5.0f), glassMat);

            // 5. Ön LED Hat Tabelası ("100 - FARM2SHELF EXPRES")
            CreatePrimitive(busRoot, "LED_Display", PrimitiveType.Cube, new Vector3(0f, 2.35f, 3.82f), new Vector3(1.8f, 0.28f, 0.05f), ledScreenMat);

            // 6. Yolcu Kapıları (Ön Kapı ve Orta Kapı - Sağ Tarafta)
            frontDoor = CreatePrimitive(busRoot, "Bus_Door_Front", PrimitiveType.Cube, new Vector3(1.16f, 1.3f, 2.6f), new Vector3(0.05f, 1.8f, 0.90f), chromeMat);
            rearDoor = CreatePrimitive(busRoot, "Bus_Door_Middle", PrimitiveType.Cube, new Vector3(1.16f, 1.3f, -1.2f), new Vector3(0.05f, 1.8f, 0.90f), chromeMat);

            // 7. Otobüs Tekerlekleri (4 Adet Ağır Vasıta Tekerleği)
            Vector3[] wheelPositions = new Vector3[]
            {
                new Vector3(-1.05f, 0.45f,  2.2f),
                new Vector3( 1.05f, 0.45f,  2.2f),
                new Vector3(-1.05f, 0.45f, -2.4f),
                new Vector3( 1.05f, 0.45f, -2.4f)
            };

            foreach (var wPos in wheelPositions)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Bus_Wheel";
                wheel.transform.SetParent(busRoot.transform, false);
                wheel.transform.localPosition = wPos;
                wheel.transform.localScale = new Vector3(0.85f, 0.18f, 0.85f);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.GetComponent<Renderer>().sharedMaterial = blackMat;
                Object.Destroy(wheel.GetComponent<Collider>());
                wheels.Add(wheel.transform);
            }

            return busRoot;
        }

        private static GameObject CreatePrimitive(GameObject parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent.transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(obj.GetComponent<Collider>());
            return obj;
        }

        private static readonly Dictionary<string, Material> busMatCache = new Dictionary<string, Material>();

        private static Material CreateMat(Shader shader, Color col, float metallic, float smoothness)
        {
            string key = $"BusMat_{col.r:F3}_{col.g:F3}_{col.b:F3}_{col.a:F3}_{metallic:F2}_{smoothness:F2}";
            if (busMatCache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material mat = new Material(shader) { color = col, name = key };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            busMatCache[key] = mat;
            return mat;
        }
    }
}
