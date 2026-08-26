using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// 6 adet endüstriyel atölye makinesi için prosedürel 3D modeller inşa eden yardımcı sınıf.
    /// </summary>
    public static class WorkshopMachineModelBuilder
    {
        private static Material industrialMat;
        private static Material copperMat;
        private static Material stainlessMat;
        private static Material glassMat;
        private static Material woodMat;
        private static Material accentMat;

        private static void InitMaterials()
        {
            if (industrialMat == null)
            {
                Shader shader = ShaderHelper.GetLitShader();
                industrialMat = new Material(shader) { color = new Color(0.20f, 0.25f, 0.32f) };
                copperMat = new Material(shader) { color = new Color(0.85f, 0.45f, 0.15f) };
                stainlessMat = new Material(shader) { color = new Color(0.78f, 0.82f, 0.88f) };
                glassMat = new Material(shader) { color = new Color(0.40f, 0.85f, 0.95f, 0.65f) };
                woodMat = new Material(shader) { color = new Color(0.45f, 0.30f, 0.18f) };
                accentMat = new Material(shader) { color = new Color(0.95f, 0.65f, 0.10f) };
            }
        }

        public static GameObject BuildMachineModel(WorkshopMachineType machineType)
        {
            InitMaterials();

            GameObject root = new GameObject("Machine_Model_" + machineType);

            switch (machineType)
            {
                case WorkshopMachineType.JamMaker:
                    BuildJamMakerModel(root);
                    break;
                case WorkshopMachineType.JuiceExtractor:
                    BuildJuiceExtractorModel(root);
                    break;
                case WorkshopMachineType.Cannery:
                    BuildCanneryModel(root);
                    break;
                case WorkshopMachineType.Dehydrator:
                    BuildDehydratorModel(root);
                    break;
                case WorkshopMachineType.OilPress:
                    BuildOilPressModel(root);
                    break;
                case WorkshopMachineType.SaladStation:
                    BuildSaladStationModel(root);
                    break;
            }

            return root;
        }

        private static void BuildJamMakerModel(GameObject parent)
        {
            // 1. Ağır Döküm Kaide
            CreateCube(parent, new Vector3(0f, 0.25f, 0f), new Vector3(2.2f, 0.5f, 1.8f), industrialMat);

            // 2. Büyük Bakır Kazan
            CreateCylinder(parent, new Vector3(0f, 0.95f, 0f), new Vector3(1.5f, 0.65f, 1.5f), copperMat);

            // 3. Kazan Üst Kapağı ve Karıştırıcı Motoru
            CreateCylinder(parent, new Vector3(0f, 1.62f, 0f), new Vector3(1.6f, 0.08f, 1.6f), stainlessMat);
            CreateCube(parent, new Vector3(0f, 1.85f, 0f), new Vector3(0.45f, 0.4f, 0.45f), accentMat);

            // 4. Buhar Tahliye Bacası
            CreateCylinder(parent, new Vector3(0.55f, 1.9f, 0.4f), new Vector3(0.18f, 0.5f, 0.18f), copperMat);

            // 5. Basınç Göstergesi / Manometre
            CreateCylinder(parent, new Vector3(-0.65f, 1.25f, 0.85f), new Vector3(0.28f, 0.06f, 0.28f), stainlessMat);
        }

        private static void BuildJuiceExtractorModel(GameObject parent)
        {
            // 1. Paslanmaz Gövde Kaidesi
            CreateCube(parent, new Vector3(0f, 0.35f, 0f), new Vector3(2.0f, 0.7f, 1.6f), stainlessMat);

            // 2. Şeffaf Cam Meyve Suyu Haznesi
            CreateCylinder(parent, new Vector3(-0.45f, 1.15f, 0f), new Vector3(0.9f, 0.65f, 0.9f), glassMat);

            // 3. Hidrolik Sıkma Pres Kulesi
            CreateCube(parent, new Vector3(0.55f, 1.15f, 0f), new Vector3(0.8f, 0.9f, 0.8f), industrialMat);
            CreateCylinder(parent, new Vector3(0.55f, 1.8f, 0f), new Vector3(0.35f, 0.5f, 0.35f), stainlessMat);

            // 4. Üst Huni Girişi
            CreateCylinder(parent, new Vector3(0.55f, 2.15f, 0f), new Vector3(0.7f, 0.25f, 0.7f), accentMat);

            // 5. Musluk / Çıkış Ağzı
            CreateCube(parent, new Vector3(-0.45f, 0.75f, 0.55f), new Vector3(0.12f, 0.12f, 0.3f), copperMat);
        }

        private static void BuildCanneryModel(GameObject parent)
        {
            // 1. Uzun Çelik Konveyör Tezgahı
            CreateCube(parent, new Vector3(0f, 0.4f, 0f), new Vector3(2.6f, 0.8f, 1.5f), stainlessMat);

            // 2. Dolum & Vakumlama Kabini
            CreateCube(parent, new Vector3(0f, 1.2f, 0f), new Vector3(1.2f, 0.8f, 1.3f), industrialMat);
            CreateCube(parent, new Vector3(0f, 1.2f, 0.66f), new Vector3(0.9f, 0.6f, 0.05f), glassMat);

            // 3. Dolum Hunisi
            CreateCylinder(parent, new Vector3(0f, 1.85f, 0f), new Vector3(0.75f, 0.45f, 0.75f), stainlessMat);

            // 4. Taşıyıcı Bant Şeritleri
            CreateCube(parent, new Vector3(-0.95f, 0.82f, 0f), new Vector3(0.65f, 0.04f, 0.8f), industrialMat);
            CreateCube(parent, new Vector3(0.95f, 0.82f, 0f), new Vector3(0.65f, 0.04f, 0.8f), industrialMat);

            // 5. Kontrol Paneli
            CreateCube(parent, new Vector3(0.85f, 1.15f, 0.55f), new Vector3(0.35f, 0.35f, 0.12f), accentMat);
        }

        private static void BuildDehydratorModel(GameObject parent)
        {
            // 1. Ağır Fırın Gövdesi
            CreateCube(parent, new Vector3(0f, 0.85f, 0f), new Vector3(2.0f, 1.7f, 1.6f), industrialMat);

            // 2. Çift Camlı Isı İzolasyonlu Fırın Kapağı
            CreateCube(parent, new Vector3(0f, 0.85f, 0.81f), new Vector3(1.6f, 1.3f, 0.05f), glassMat);

            // 3. İç Çelik Raflar / Tepsiler (Görsel çizgi olarak)
            CreateCube(parent, new Vector3(0f, 0.6f, 0.78f), new Vector3(1.5f, 0.03f, 0.03f), stainlessMat);
            CreateCube(parent, new Vector3(0f, 0.9f, 0.78f), new Vector3(1.5f, 0.03f, 0.03f), stainlessMat);
            CreateCube(parent, new Vector3(0f, 1.2f, 0.78f), new Vector3(1.5f, 0.03f, 0.03f), stainlessMat);

            // 4. Üst Havalandırma & Sirkülasyon Fanı
            CreateCylinder(parent, new Vector3(0f, 1.8f, 0f), new Vector3(0.8f, 0.2f, 0.8f), stainlessMat);

            // 5. Dijital Isı Kontrol Ekranı
            CreateCube(parent, new Vector3(0.65f, 1.55f, 0.82f), new Vector3(0.4f, 0.2f, 0.04f), accentMat);
        }

        private static void BuildOilPressModel(GameObject parent)
        {
            // 1. Ağır Çelik Kaide
            CreateCube(parent, new Vector3(0f, 0.3f, 0f), new Vector3(2.2f, 0.6f, 1.6f), industrialMat);

            // 2. Yatay Burgulu Pres Silindiri
            GameObject cylinder = CreateCylinder(parent, new Vector3(0f, 0.85f, 0f), new Vector3(0.65f, 1.4f, 0.65f), stainlessMat);
            cylinder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // 3. Üst Besleme Hunisi (Tohum Girişi)
            CreateCylinder(parent, new Vector3(0f, 1.45f, -0.45f), new Vector3(0.7f, 0.5f, 0.7f), copperMat);

            // 4. Saf Yağ Toplama Tankı & Musluğu
            CreateCylinder(parent, new Vector3(0f, 0.8f, 0.55f), new Vector3(0.55f, 0.45f, 0.55f), glassMat);

            // 5. Basınç Hidrolik Motoru
            CreateCube(parent, new Vector3(0.75f, 0.85f, 0f), new Vector3(0.5f, 0.6f, 0.6f), accentMat);
        }

        private static void BuildSaladStationModel(GameObject parent)
        {
            // 1. Paslanmaz Meze Hazırlık Tezgahı
            CreateCube(parent, new Vector3(0f, 0.45f, 0f), new Vector3(2.4f, 0.9f, 1.6f), stainlessMat);

            // 2. Üst Soğutmalı Cam Vitrin
            CreateCube(parent, new Vector3(0f, 1.15f, -0.25f), new Vector3(2.2f, 0.5f, 0.7f), glassMat);

            // 3. Gastronorm Meze Küvetleri (3 Bölme)
            CreateCube(parent, new Vector3(-0.65f, 0.92f, -0.25f), new Vector3(0.55f, 0.08f, 0.55f), stainlessMat);
            CreateCube(parent, new Vector3(0f, 0.92f, -0.25f), new Vector3(0.55f, 0.08f, 0.55f), stainlessMat);
            CreateCube(parent, new Vector3(0.65f, 0.92f, -0.25f), new Vector3(0.55f, 0.08f, 0.55f), stainlessMat);

            // 4. Ahşap Kesme & Hazırlık Tablası
            CreateCube(parent, new Vector3(0f, 0.92f, 0.4f), new Vector3(2.2f, 0.06f, 0.6f), woodMat);
        }

        private static GameObject CreateCube(GameObject parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject CreateCylinder(GameObject parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
