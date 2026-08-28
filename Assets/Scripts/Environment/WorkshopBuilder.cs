using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Bağımsız Atölye İnşa Modülü (Workshop Builder).
    /// Otoparkın sol tarafındaki parselde (X: -68..-42, Z: -3..+40) 3 aşamalı büyütülebilen,
    /// önünde otomatik kayar kapılı, etrafı modern pencerelerle çevrili, içi boş ve endüstriyel
    /// aydınlatmalı Atölye Binasını oluşturur.
    /// </summary>
    public class WorkshopBuilder : MonoBehaviour
    {
        public static WorkshopBuilder Instance { get; private set; }

        [Header("Root Container")]
        private Transform workshopRoot;

        // Materyaller
        private Material wallMat;
        private Material wallAccentMat;
        private Material floorMat;
        private Material windowGlassMat;
        private Material windowFrameMat;
        private Material windowSillMat;
        private Material doorFrameMat;
        private Material doorGlassMat;
        private Material hazardStripeMat;
        private Material trussMat;

        private TextMesh worldLabelMesh;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            Refresh3DLabel();
        }

        public void Refresh3DLabel()
        {
            if (worldLabelMesh != null)
            {
                worldLabelMesh.text = LocalizationManager.L("Label3D_Workshop", "ATÖLYE", "WORKSHOP");
            }
        }

        private void InitializeMaterials()
        {
            if (wallMat != null) return;

            // Koyu Modern Endüstriyel Duvar
            wallMat = CreateSolidMaterial("WorkshopWallMat", new Color(0.14f, 0.17f, 0.22f), 0.2f, 0.8f);
            
            // Turuncu/Kehribar Endüstriyel Vurgu Kirişleri
            wallAccentMat = CreateSolidMaterial("WorkshopAccentMat", new Color(0.92f, 0.55f, 0.15f), 0.3f, 0.7f);

            // Cilalı Endüstriyel Epoksi Zemin
            floorMat = CreateSolidMaterial("WorkshopFloorMat", new Color(0.20f, 0.24f, 0.30f), 0.5f, 0.9f);

            // Şeffaf Yansımalı Cam
            windowGlassMat = CreateSolidMaterial("WorkshopGlassMat", new Color(0.40f, 0.80f, 0.95f, 0.35f), 0.1f, 0.95f);

            // Antrasit Metal Çerçeve
            windowFrameMat = CreateSolidMaterial("WorkshopFrameMat", new Color(0.10f, 0.12f, 0.16f), 0.7f, 0.8f);

            // Pencere Denizliği (Açık Gri Taş)
            windowSillMat = CreateSolidMaterial("WorkshopSillMat", new Color(0.68f, 0.70f, 0.75f), 0.1f, 0.9f);

            // Çelik Kapı Kasası
            doorFrameMat = CreateSolidMaterial("WorkshopDoorFrameMat", new Color(0.35f, 0.38f, 0.45f), 0.8f, 0.7f);

            // Kayar Kapı Camı
            doorGlassMat = CreateSolidMaterial("WorkshopDoorGlassMat", new Color(0.30f, 0.85f, 0.95f, 0.45f), 0.1f, 0.95f);

            // Sarı/Siyah Güvenlik Şeridi
            hazardStripeMat = CreateSolidMaterial("WorkshopHazardMat", new Color(0.95f, 0.80f, 0.15f), 0.1f, 0.6f);

            // Çatı Çelik Makas Materyali
            trussMat = CreateSolidMaterial("WorkshopTrussMat", new Color(0.28f, 0.32f, 0.38f), 0.8f, 0.6f);
        }

        private Material CreateSolidMaterial(string matName, Color color, float metallic = 0.0f, float smoothness = 0.5f)
        {
            Shader shader = ShaderHelper.GetLitShader();
            if (shader == null)
            {
                Debug.LogError($"[WorkshopBuilder] HATA: '{matName}' materyali için 3D URP shader null döndü! Çökme önlendi.");
                return null;
            }

            Material mat = new Material(shader);
            mat.name = matName;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (color.a < 1.0f)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            return mat;
        }

        /// <summary>
        /// Belirtilen seviyede Atölye Binasını baştan oluşturur.
        /// </summary>
        public void BuildWorkshop(int level = 1)
        {
            InitializeMaterials();

            // Eski atölye yapısını temizle
            GameObject existing = GameObject.Find("Workshop_Complex");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
            }

            workshopRoot = new GameObject("Workshop_Complex").transform;

            // Parametreler
            float wallH = 3.8f;
            float wallT = 0.4f;
            float frontWallZ = -3.0f;
            float width = 25.0f; // X: -67.5 ile -42.5 arası
            float centerX = -55.0f;

            // Seviye Derinlikleri: Seviye 1: 18m, Seviye 2: 27m, Seviye 3: 43m (Kuzey yoluna kadar)
            float depth = (level == 1) ? 18.0f : ((level == 2) ? 27.0f : 43.0f);
            float backWallZ = frontWallZ + depth;
            float centerZ = frontWallZ + (depth / 2f);

            // 1. ZEMİN (Epoksi & Güvenlik Çerçevesi)
            GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorObj.name = "Workshop_Floor";
            floorObj.transform.SetParent(workshopRoot, false);
            floorObj.transform.position = new Vector3(centerX, 0.01f, centerZ);
            floorObj.transform.localScale = new Vector3(width, 0.02f, depth);
            floorObj.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Giriş Önü Sarı Güvenlik Çizgisi
            GameObject hazardLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hazardLine.name = "Entrance_Hazard_Stripe";
            hazardLine.transform.SetParent(workshopRoot, false);
            hazardLine.transform.position = new Vector3(centerX, 0.025f, frontWallZ + 0.35f);
            hazardLine.transform.localScale = new Vector3(5.0f, 0.01f, 0.30f);
            hazardLine.GetComponent<Renderer>().sharedMaterial = hazardStripeMat;
            Destroy(hazardLine.GetComponent<Collider>());

            // 2. ÖN CEPHE (KAYAR KAPI + 2 GENİŞ ÖN CAM PENCERE + ÜST TABELA)
            BuildFrontFacade(centerX, frontWallZ, width, wallH, wallT);

            // 3. SOL CEPHE DUVARI VE PENCERELERİ (Batı Manzaralı)
            BuildSideWallWithWindows(centerX - (width / 2f), frontWallZ, depth, wallH, wallT, isLeftWall: true);

            // 4. SAĞ CEPHE DUVARI VE PENCERELERİ (Otoparka Bakan)
            BuildSideWallWithWindows(centerX + (width / 2f), frontWallZ, depth, wallH, wallT, isLeftWall: false);

            // 5. ARKA DUVAR VE ÜST PENCERELERİ
            BuildBackWall(centerX, backWallZ, width, wallH, wallT);

            // 6. ÇATI MAKASLARI VE TAVAN ENDÜSTRİYEL AYDINLATMALARI
            BuildCeilingLightingAndTrusses(centerX, frontWallZ, backWallZ, width, wallH);

            // 7. ATÖLYE HAMMADDE PALET RAFI (3D MODEL VE ETKİLEŞİM)
            BuildWorkshopPalletRack(centerX, frontWallZ);

            Refresh3DLabel();
            Debug.Log($"[WorkshopBuilder] Seviye {level} Atölye Binası (Derinlik: {depth}m) Başarıyla İnşa Edildi!");
        }

        private void BuildWorkshopPalletRack(float centerX, float frontZ)
        {
            Vector3 rackPos = new Vector3(centerX - 7.5f, 0.02f, frontZ + 5.5f);

            GameObject palletStorage = new GameObject("Workshop_Pallet_Storage");
            palletStorage.transform.SetParent(workshopRoot, false);
            palletStorage.transform.position = rackPos;

            // Dokunmatik / Fare Tıklama Alanı
            BoxCollider col = palletStorage.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.25f, 0f);
            col.size = new Vector3(3.6f, 2.6f, 2.8f);

            palletStorage.AddComponent<WorkshopPalletClickable>();

            Material steelBlueMat = CreateSolidMaterial("WorkshopPalletSteelMat", new Color(0.18f, 0.35f, 0.65f), 0.7f, 0.7f);
            Material beamOrangeMat = CreateSolidMaterial("WorkshopPalletBeamMat", new Color(0.95f, 0.55f, 0.12f), 0.5f, 0.8f);
            Material woodMat = CreateSolidMaterial("WorkshopPalletWoodMat", new Color(0.62f, 0.44f, 0.24f), 0.1f, 0.6f);

            float rackW = 2.6f;
            float rackD = 2.0f;
            float rackH = 2.4f;

            // 4 Adet Dikey Çelik Dikme Kolon
            float halfW = rackW / 2f;
            float halfD = rackD / 2f;
            Vector3[] postOffsets = new Vector3[] {
                new Vector3(-halfW, rackH / 2f, -halfD),
                new Vector3( halfW, rackH / 2f, -halfD),
                new Vector3(-halfW, rackH / 2f,  halfD),
                new Vector3( halfW, rackH / 2f,  halfD)
            };

            foreach (var offset in postOffsets)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "Rack_Post";
                post.transform.SetParent(palletStorage.transform, false);
                post.transform.localPosition = offset;
                post.transform.localScale = new Vector3(0.12f, rackH, 0.12f);
                post.GetComponent<Renderer>().sharedMaterial = steelBlueMat;
                Destroy(post.GetComponent<Collider>());
            }

            // Yatay Taşıyıcı Kirişler (Alt Kat, Orta Kat, Üst Kat)
            float[] beamHeights = new float[] { 0.12f, 1.20f, 2.35f };
            foreach (float bh in beamHeights)
            {
                // Ön Kiriş
                GameObject frontBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frontBeam.name = "Beam_Front";
                frontBeam.transform.SetParent(palletStorage.transform, false);
                frontBeam.transform.localPosition = new Vector3(0f, bh, -halfD);
                frontBeam.transform.localScale = new Vector3(rackW, 0.10f, 0.08f);
                frontBeam.GetComponent<Renderer>().sharedMaterial = beamOrangeMat;
                Destroy(frontBeam.GetComponent<Collider>());

                // Arka Kiriş
                GameObject backBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backBeam.name = "Beam_Back";
                backBeam.transform.SetParent(palletStorage.transform, false);
                backBeam.transform.localPosition = new Vector3(0f, bh, halfD);
                backBeam.transform.localScale = new Vector3(rackW, 0.10f, 0.08f);
                backBeam.GetComponent<Renderer>().sharedMaterial = beamOrangeMat;
                Destroy(backBeam.GetComponent<Collider>());

                // Yan Kirişler
                GameObject leftBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftBeam.name = "Beam_Left";
                leftBeam.transform.SetParent(palletStorage.transform, false);
                leftBeam.transform.localPosition = new Vector3(-halfW, bh, 0f);
                leftBeam.transform.localScale = new Vector3(0.08f, 0.10f, rackD);
                leftBeam.GetComponent<Renderer>().sharedMaterial = beamOrangeMat;
                Destroy(leftBeam.GetComponent<Collider>());

                GameObject rightBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightBeam.name = "Beam_Right";
                rightBeam.transform.SetParent(palletStorage.transform, false);
                rightBeam.transform.localPosition = new Vector3(halfW, bh, 0f);
                rightBeam.transform.localScale = new Vector3(0.08f, 0.10f, rackD);
                rightBeam.GetComponent<Renderer>().sharedMaterial = beamOrangeMat;
                Destroy(rightBeam.GetComponent<Collider>());
            }

            // Ahşap Palet Tabanı (Alt Zemin)
            GameObject bottomPallet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomPallet.name = "EuroPallet_Base";
            bottomPallet.transform.SetParent(palletStorage.transform, false);
            bottomPallet.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            bottomPallet.transform.localScale = new Vector3(rackW - 0.2f, 0.12f, rackD - 0.2f);
            bottomPallet.GetComponent<Renderer>().sharedMaterial = woodMat;
            Destroy(bottomPallet.GetComponent<Collider>());

            // Ahşap Palet Çıtaları
            for (float px = -(halfW - 0.3f); px <= (halfW - 0.3f); px += 0.45f)
            {
                GameObject slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = "Pallet_Slat";
                slat.transform.SetParent(palletStorage.transform, false);
                slat.transform.localPosition = new Vector3(px, 0.13f, 0f);
                slat.transform.localScale = new Vector3(0.12f, 0.03f, rackD - 0.2f);
                slat.GetComponent<Renderer>().sharedMaterial = woodMat;
                Destroy(slat.GetComponent<Collider>());
            }

            // 3D Başlık Tabelası
            GameObject signBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBoard.name = "Pallet_Header_Board";
            signBoard.transform.SetParent(palletStorage.transform, false);
            signBoard.transform.localPosition = new Vector3(0f, rackH + 0.25f, -halfD);
            signBoard.transform.localScale = new Vector3(2.4f, 0.40f, 0.08f);
            signBoard.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("WorkshopSignBgMat", new Color(0.12f, 0.16f, 0.22f), 0.2f, 0.8f);
            Destroy(signBoard.GetComponent<Collider>());

            GameObject textObj = new GameObject("LabelText");
            textObj.transform.SetParent(signBoard.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.55f);

            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = LocalizationManager.L("Label3D_RawPallet", "📦 HAMMADDE PALETİ", "📦 RAW MATERIAL PALLET");
            tm.fontSize = 32;
            tm.characterSize = 0.05f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(0.95f, 0.65f, 0.20f);
            tm.fontStyle = FontStyle.Bold;

            // Koli Yığını Container'ı
            Transform boxContainer = new GameObject("Workshop_Boxes_Container").transform;
            boxContainer.SetParent(palletStorage.transform, false);
            boxContainer.localPosition = Vector3.zero;

            if (WorkshopPalletManager.Instance != null)
            {
                WorkshopPalletManager.Instance.RegisterBoxContainer(boxContainer);
            }
        }

        private void BuildFrontFacade(float centerX, float frontZ, float width, float wallH, float wallT)
        {
            Transform frontGroup = new GameObject("Front_Facade").transform;
            frontGroup.SetParent(workshopRoot, false);

            float doorW = 3.6f;
            float doorH = 2.7f;
            float leftPillarW = (width - doorW) / 2f; // ~10.7m

            // A. Sol Ön Duvar Segmenti (Pencereli)
            // Sol duvar parçası (X: centerX - width/2f ile centerX - doorW/2f arası)
            float leftPillarCenter = centerX - (doorW / 2f) - (leftPillarW / 2f);
            CreateWallSegmentWithWindow(frontGroup, new Vector3(leftPillarCenter, 0f, frontZ), leftPillarW, wallH, wallT, 4.2f, 1.8f, 1.9f, true);

            // B. Sağ Ön Duvar Segmenti (Pencereli)
            float rightPillarCenter = centerX + (doorW / 2f) + (leftPillarW / 2f);
            CreateWallSegmentWithWindow(frontGroup, new Vector3(rightPillarCenter, 0f, frontZ), leftPillarW, wallH, wallT, 4.2f, 1.8f, 1.9f, true);

            // C. Kapı Üstü Duvar Kirişi
            float overDoorH = wallH - doorH;
            GameObject overDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            overDoor.name = "Front_Wall_Over_Door";
            overDoor.transform.SetParent(frontGroup, false);
            overDoor.transform.position = new Vector3(centerX, doorH + (overDoorH / 2f), frontZ);
            overDoor.transform.localScale = new Vector3(doorW, overDoorH, wallT);
            overDoor.GetComponent<Renderer>().sharedMaterial = wallAccentMat;

            // D. Otomatik Çift Kanatlı Kayar Kapı Mekanizması
            CreateSlidingDoor(frontGroup, new Vector3(centerX, 0f, frontZ), doorW, doorH, wallT);

            // E. 3D Atölye Başlık Tabelası
            Create3DHeaderSign(frontGroup, new Vector3(centerX, wallH + 0.35f, frontZ - 0.25f));
        }

        private void CreateSlidingDoor(Transform parent, Vector3 pos, float doorwayW, float doorH, float wallT)
        {
            GameObject doorRoot = new GameObject("Workshop_Sliding_DoubleDoor");
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.position = pos;

            float leafW = doorwayW / 2f;
            float leafT = 0.12f;

            // Sol Kanat
            GameObject leftLeaf = new GameObject("Left_Leaf");
            leftLeaf.transform.SetParent(doorRoot.transform, false);
            leftLeaf.transform.localPosition = new Vector3(-leafW / 2f, doorH / 2f, 0f);

            GameObject leftBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftBody.name = "Panel_Body";
            leftBody.transform.SetParent(leftLeaf.transform, false);
            leftBody.transform.localPosition = Vector3.zero;
            leftBody.transform.localScale = new Vector3(leafW, doorH, leafT);
            leftBody.GetComponent<Renderer>().sharedMaterial = doorGlassMat;
            Destroy(leftBody.GetComponent<Collider>());

            // Sağ Kanat
            GameObject rightLeaf = new GameObject("Right_Leaf");
            rightLeaf.transform.SetParent(doorRoot.transform, false);
            rightLeaf.transform.localPosition = new Vector3(leafW / 2f, doorH / 2f, 0f);

            GameObject rightBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightBody.name = "Panel_Body";
            rightBody.transform.SetParent(rightLeaf.transform, false);
            rightBody.transform.localPosition = Vector3.zero;
            rightBody.transform.localScale = new Vector3(leafW, doorH, leafT);
            rightBody.GetComponent<Renderer>().sharedMaterial = doorGlassMat;
            Destroy(rightBody.GetComponent<Collider>());

            // Otomatik Kayar Kapı Kontrolcüsü
            InteractiveDoubleDoor doorScript = doorRoot.AddComponent<InteractiveDoubleDoor>();
            doorScript.SetupDoors(leftLeaf.transform, rightLeaf.transform, true, leafW * 0.95f);
        }

        private void BuildSideWallWithWindows(float wallX, float startZ, float depth, float wallH, float wallT, bool isLeftWall)
        {
            Transform sideGroup = new GameObject(isLeftWall ? "Left_Wall_System" : "Right_Wall_System").transform;
            sideGroup.SetParent(workshopRoot, false);

            // Pencereler her 6 metrede bir yerleştirilir
            float windowSpacing = 6.0f;
            float windowW = 2.8f;
            float windowH = 1.8f;
            float windowCenterY = 2.0f;

            float curZ = startZ + 3.0f;
            float endZ = startZ + depth;

            List<float> windowPositions = new List<float>();
            while (curZ < endZ - 2.5f)
            {
                windowPositions.Add(curZ);
                curZ += windowSpacing;
            }

            // Duvarı pencereler arasına bölerek sağlam şekilde ör
            float lastZ = startZ;
            for (int i = 0; i < windowPositions.Count; i++)
            {
                float winZ = windowPositions[i];
                float winStart = winZ - (windowW / 2f);
                float winEnd = winZ + (windowW / 2f);

                // Pencere öncesi dolu duvar parçası
                float solidLength = winStart - lastZ;
                if (solidLength > 0.01f)
                {
                    float solidCenter = lastZ + (solidLength / 2f);
                    CreateWallCube(sideGroup, new Vector3(wallX, wallH / 2f, solidCenter), new Vector3(wallT, wallH, solidLength), wallMat);
                }

                // Pencere Altı Duvar
                float underWinH = windowCenterY - (windowH / 2f);
                CreateWallCube(sideGroup, new Vector3(wallX, underWinH / 2f, winZ), new Vector3(wallT, underWinH, windowW), wallMat);

                // Pencere Üstü Duvar
                float overWinH = wallH - (windowCenterY + (windowH / 2f));
                float overWinCenterY = (windowCenterY + (windowH / 2f)) + (overWinH / 2f);
                CreateWallCube(sideGroup, new Vector3(wallX, overWinCenterY, winZ), new Vector3(wallT, overWinH, windowW), wallAccentMat);

                // Cam Panel
                GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glass.name = "Window_Glass";
                glass.transform.SetParent(sideGroup, false);
                glass.transform.position = new Vector3(wallX, windowCenterY, winZ);
                glass.transform.localScale = new Vector3(0.06f, windowH, windowW);
                glass.GetComponent<Renderer>().sharedMaterial = windowGlassMat;
                Destroy(glass.GetComponent<Collider>());

                // Pencere Çerçevesi & Denizlik
                CreateSideWindowFrame(sideGroup, new Vector3(wallX, windowCenterY, winZ), windowW, windowH, wallT);

                lastZ = winEnd;
            }

            // Son pencereden arka duvara kadar olan parça
            float finalSolidLength = endZ - lastZ;
            if (finalSolidLength > 0.01f)
            {
                float finalSolidCenter = lastZ + (finalSolidLength / 2f);
                CreateWallCube(sideGroup, new Vector3(wallX, wallH / 2f, finalSolidCenter), new Vector3(wallT, wallH, finalSolidLength), wallMat);
            }
        }

        private void BuildBackWall(float centerX, float backZ, float width, float wallH, float wallT)
        {
            Transform backGroup = new GameObject("Back_Wall_System").transform;
            backGroup.SetParent(workshopRoot, false);

            // Arka duvarda 2 adet üst aydınlatma penceresi
            float winW = 5.0f;
            float winH = 1.2f;
            float winY = 2.6f;

            float leftWinX = centerX - 6.0f;
            float rightWinX = centerX + 6.0f;

            // Tam arka duvar gövdesi
            CreateWallCube(backGroup, new Vector3(centerX, wallH / 2f, backZ), new Vector3(width, wallH, wallT), wallMat);

            // Pencereleri görsel girinti olarak duvara monte et
            CreateWindowInset(backGroup, new Vector3(leftWinX, winY, backZ), winW, winH, wallT);
            CreateWindowInset(backGroup, new Vector3(rightWinX, winY, backZ), winW, winH, wallT);
        }

        private void CreateWindowInset(Transform parent, Vector3 pos, float winW, float winH, float wallT)
        {
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Clerestory_Glass";
            glass.transform.SetParent(parent, false);
            glass.transform.position = pos;
            glass.transform.localScale = new Vector3(winW, winH, wallT + 0.04f);
            glass.GetComponent<Renderer>().sharedMaterial = windowGlassMat;
            Destroy(glass.GetComponent<Collider>());

            CreateFrontWindowFrame(parent, pos, winW, winH, wallT);
        }

        private void CreateWallSegmentWithWindow(Transform parent, Vector3 basePos, float segmentW, float wallH, float wallT, float winW, float winH, float winCenterY, bool isFront)
        {
            // Segmentin sol, sağ, alt ve üstünü örer
            float sidePillarW = (segmentW - winW) / 2f;
            float underWinH = winCenterY - (winH / 2f);
            float overWinH = wallH - (winCenterY + (winH / 2f));
            float overWinCenterY = (winCenterY + (winH / 2f)) + (overWinH / 2f);

            // Sol Dolu Parça
            CreateWallCube(parent, new Vector3(basePos.x - (winW / 2f) - (sidePillarW / 2f), wallH / 2f, basePos.z), new Vector3(sidePillarW, wallH, wallT), wallMat);

            // Sağ Dolu Parça
            CreateWallCube(parent, new Vector3(basePos.x + (winW / 2f) + (sidePillarW / 2f), wallH / 2f, basePos.z), new Vector3(sidePillarW, wallH, wallT), wallMat);

            // Pencere Altı
            CreateWallCube(parent, new Vector3(basePos.x, underWinH / 2f, basePos.z), new Vector3(winW, underWinH, wallT), wallMat);

            // Pencere Üstü
            CreateWallCube(parent, new Vector3(basePos.x, overWinCenterY, basePos.z), new Vector3(winW, overWinH, wallT), wallAccentMat);

            // Cam Panel
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Facade_Window_Glass";
            glass.transform.SetParent(parent, false);
            glass.transform.position = new Vector3(basePos.x, winCenterY, basePos.z);
            glass.transform.localScale = new Vector3(winW, winH, 0.06f);
            glass.GetComponent<Renderer>().sharedMaterial = windowGlassMat;
            Destroy(glass.GetComponent<Collider>());

            CreateFrontWindowFrame(parent, new Vector3(basePos.x, winCenterY, basePos.z), winW, winH, wallT);
        }

        private void CreateFrontWindowFrame(Transform parent, Vector3 pos, float winW, float winH, float wallT)
        {
            // Dış Çerçeve
            CreateFrameBox(parent, pos, winW, winH, wallT + 0.06f, 0.10f, windowFrameMat);

            // Alt Taş Denizlik
            GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "Window_Sill";
            sill.transform.SetParent(parent, false);
            sill.transform.position = new Vector3(pos.x, pos.y - (winH / 2f) - 0.04f, pos.z);
            sill.transform.localScale = new Vector3(winW + 0.30f, 0.08f, wallT + 0.22f);
            sill.GetComponent<Renderer>().sharedMaterial = windowSillMat;
            Destroy(sill.GetComponent<Collider>());
        }

        private void CreateSideWindowFrame(Transform parent, Vector3 pos, float winW, float winH, float wallT)
        {
            // Z doğrultusunda çerçeve
            float frameT = 0.08f;
            GameObject fGroup = new GameObject("Side_Window_Frame");
            fGroup.transform.SetParent(parent, false);
            fGroup.transform.position = pos;

            // Üst & Alt
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(fGroup.transform, false);
            top.transform.localPosition = new Vector3(0f, (winH / 2f) - (frameT / 2f), 0f);
            top.transform.localScale = new Vector3(wallT + 0.06f, frameT, winW);
            top.GetComponent<Renderer>().sharedMaterial = windowFrameMat;
            Destroy(top.GetComponent<Collider>());

            GameObject bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottom.transform.SetParent(fGroup.transform, false);
            bottom.transform.localPosition = new Vector3(0f, -(winH / 2f) + (frameT / 2f), 0f);
            bottom.transform.localScale = new Vector3(wallT + 0.06f, frameT, winW);
            bottom.GetComponent<Renderer>().sharedMaterial = windowFrameMat;
            Destroy(bottom.GetComponent<Collider>());

            // Sol & Sağ
            GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.transform.SetParent(fGroup.transform, false);
            left.transform.localPosition = new Vector3(0f, 0f, -(winW / 2f) + (frameT / 2f));
            left.transform.localScale = new Vector3(wallT + 0.06f, winH, frameT);
            left.GetComponent<Renderer>().sharedMaterial = windowFrameMat;
            Destroy(left.GetComponent<Collider>());

            GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.transform.SetParent(fGroup.transform, false);
            right.transform.localPosition = new Vector3(0f, 0f, (winW / 2f) - (frameT / 2f));
            right.transform.localScale = new Vector3(wallT + 0.06f, winH, frameT);
            right.GetComponent<Renderer>().sharedMaterial = windowFrameMat;
            Destroy(right.GetComponent<Collider>());

            // Denizlik
            GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "Side_Window_Sill";
            sill.transform.SetParent(fGroup.transform, false);
            sill.transform.localPosition = new Vector3(0f, -(winH / 2f) - 0.04f, 0f);
            sill.transform.localScale = new Vector3(wallT + 0.22f, 0.08f, winW + 0.30f);
            sill.GetComponent<Renderer>().sharedMaterial = windowSillMat;
            Destroy(sill.GetComponent<Collider>());
        }

        private void CreateFrameBox(Transform parent, Vector3 center, float w, float h, float d, float thickness, Material frameM)
        {
            GameObject group = new GameObject("FrameBox");
            group.transform.SetParent(parent, false);
            group.transform.position = center;

            // Üst
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(group.transform, false);
            top.transform.localPosition = new Vector3(0f, (h / 2f) - (thickness / 2f), 0f);
            top.transform.localScale = new Vector3(w, thickness, d);
            top.GetComponent<Renderer>().sharedMaterial = frameM;
            Destroy(top.GetComponent<Collider>());

            // Alt
            GameObject bot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bot.transform.SetParent(group.transform, false);
            bot.transform.localPosition = new Vector3(0f, -(h / 2f) + (thickness / 2f), 0f);
            bot.transform.localScale = new Vector3(w, thickness, d);
            bot.GetComponent<Renderer>().sharedMaterial = frameM;
            Destroy(bot.GetComponent<Collider>());

            // Sol
            GameObject l = GameObject.CreatePrimitive(PrimitiveType.Cube);
            l.transform.SetParent(group.transform, false);
            l.transform.localPosition = new Vector3(-(w / 2f) + (thickness / 2f), 0f, 0f);
            l.transform.localScale = new Vector3(thickness, h, d);
            l.GetComponent<Renderer>().sharedMaterial = frameM;
            Destroy(l.GetComponent<Collider>());

            // Sağ
            GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.transform.SetParent(group.transform, false);
            r.transform.localPosition = new Vector3((w / 2f) - (thickness / 2f), 0f, 0f);
            r.transform.localScale = new Vector3(thickness, h, d);
            r.GetComponent<Renderer>().sharedMaterial = frameM;
            Destroy(r.GetComponent<Collider>());
        }

        private void CreateWallCube(Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall_Block";
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = m;
        }

        private void Create3DHeaderSign(Transform parent, Vector3 pos)
        {
            GameObject signObj = new GameObject("Workshop_3D_Header_Sign");
            signObj.transform.SetParent(parent, false);
            signObj.transform.position = pos;

            // Dış Tabela Çerçevesi (Turuncu Endüstriyel Vurgu)
            GameObject bgOuter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bgOuter.name = "Sign_Plate_Outer";
            bgOuter.transform.SetParent(signObj.transform, false);
            bgOuter.transform.localPosition = Vector3.zero;
            bgOuter.transform.localScale = new Vector3(9.2f, 1.50f, 0.16f);
            bgOuter.GetComponent<Renderer>().sharedMaterial = wallAccentMat;
            Destroy(bgOuter.GetComponent<Collider>());

            // İç Kontrast Plaka (Koyu Metalik Panel)
            GameObject bgInner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bgInner.name = "Sign_Plate_Inner";
            bgInner.transform.SetParent(signObj.transform, false);
            bgInner.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            bgInner.transform.localScale = new Vector3(8.7f, 1.25f, 0.18f);
            bgInner.GetComponent<Renderer>().sharedMaterial = windowFrameMat;
            Destroy(bgInner.GetComponent<Collider>());

            // 3D TextMesh (Büyük, Okunaklı & Çift Dilli)
            GameObject textObj = new GameObject("Label_Text");
            textObj.transform.SetParent(signObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.16f);

            worldLabelMesh = textObj.AddComponent<TextMesh>();
            worldLabelMesh.text = LocalizationManager.L("Label3D_Workshop", "ATÖLYE", "WORKSHOP");
            worldLabelMesh.fontSize = 62;
            worldLabelMesh.characterSize = 0.088f;
            worldLabelMesh.alignment = TextAlignment.Center;
            worldLabelMesh.anchor = TextAnchor.MiddleCenter;
            worldLabelMesh.color = new Color(1.0f, 0.96f, 0.88f);
            worldLabelMesh.fontStyle = FontStyle.Bold;
        }

        private void BuildCeilingLightingAndTrusses(float centerX, float frontZ, float backZ, float width, float wallH)
        {
            Transform lightGroup = new GameObject("Workshop_Ceiling_Lighting_Group").transform;
            lightGroup.SetParent(workshopRoot, false);

            float trussSpacing = 5.5f;
            for (float z = frontZ + 3.0f; z <= backZ - 1.5f; z += trussSpacing)
            {
                // Çatı Çelik Makası
                GameObject truss = GameObject.CreatePrimitive(PrimitiveType.Cube);
                truss.name = "Ceiling_Truss";
                truss.transform.SetParent(lightGroup, false);
                truss.transform.position = new Vector3(centerX, wallH - 0.15f, z);
                truss.transform.localScale = new Vector3(width - 0.8f, 0.25f, 0.25f);
                truss.GetComponent<Renderer>().sharedMaterial = trussMat;
                Destroy(truss.GetComponent<Collider>());

                // 3 Adet Endüstriyel Tavan Işığı (Sol, Orta, Sağ)
                CreateIndustrialPendantLight(lightGroup, new Vector3(centerX - 7.0f, wallH - 0.40f, z));
                CreateIndustrialPendantLight(lightGroup, new Vector3(centerX, wallH - 0.40f, z));
                CreateIndustrialPendantLight(lightGroup, new Vector3(centerX + 7.0f, wallH - 0.40f, z));
            }

            // Genel İç Aydınlatma Dolgu Işıkları (Gündüz ve gece atölye içinin aydınlık ve ferah görünmesi için)
            float depth = backZ - frontZ;
            CreateInteriorAmbientFillLight(lightGroup, new Vector3(centerX, 2.2f, frontZ + depth * 0.33f));
            CreateInteriorAmbientFillLight(lightGroup, new Vector3(centerX, 2.2f, frontZ + depth * 0.67f));
        }

        private void CreateIndustrialPendantLight(Transform parent, Vector3 pos)
        {
            GameObject lampObj = new GameObject("Workshop_Pendant_Light");
            lampObj.transform.SetParent(parent, false);
            lampObj.transform.position = pos;

            // Armatür Gövdesi
            GameObject shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shade.transform.SetParent(lampObj.transform, false);
            shade.transform.localPosition = Vector3.zero;
            shade.transform.localScale = new Vector3(0.75f, 0.16f, 0.75f);
            shade.GetComponent<Renderer>().sharedMaterial = doorFrameMat;
            Destroy(shade.GetComponent<Collider>());

            // Parlayan Ampul Görseli
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.transform.SetParent(lampObj.transform, false);
            bulb.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            bulb.transform.localScale = new Vector3(0.32f, 0.20f, 0.32f);
            bulb.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial("WorkshopBulbMat", new Color(1.0f, 0.98f, 0.85f), 0.1f, 0.95f);
            Destroy(bulb.GetComponent<Collider>());

            // Işık Kaynağı (Atölye içi daima aydınlık ve net)
            Light pLight = lampObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.96f, 0.88f);
            pLight.intensity = 5.2f;
            pLight.range = 22.0f;
            pLight.shadows = LightShadows.None;
            pLight.enabled = true;

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(pLight);
            }
        }

        private void CreateInteriorAmbientFillLight(Transform parent, Vector3 pos)
        {
            GameObject fillObj = new GameObject("Workshop_Ambient_Fill_Light");
            fillObj.transform.SetParent(parent, false);
            fillObj.transform.position = pos;

            Light fLight = fillObj.AddComponent<Light>();
            fLight.type = LightType.Point;
            fLight.color = new Color(1.0f, 0.95f, 0.88f);
            fLight.intensity = 4.0f;
            fLight.range = 28.0f;
            fLight.shadows = LightShadows.None;
            fLight.enabled = true;

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(fLight);
            }
        }
    }

    /// <summary>
    /// Atölye içindeki hammadde paletine tıklandığında Atölye Palet Arayüzünü açan etkileşim bileşeni.
    /// </summary>
    public class WorkshopPalletClickable : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler, UnityEngine.EventSystems.IPointerDownHandler
    {
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            HandleClick();
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            HandleClick();
        }

        private void OnMouseDown()
        {
            HandleClick();
        }

        private void HandleClick()
        {
            if (Farm2Shelf.UI.ModalManager.IsModalOpen || Farm2Shelf.UI.EKTPhoneManager.IsTabletOpen || (Farm2Shelf.UI.PauseMenuUI.Instance != null && Farm2Shelf.UI.PauseMenuUI.Instance.IsPauseMenuOpen)) return;

            Farm2Shelf.UI.PalletStorageInventoryModalUI.ShowModal(isWorkshopMode: true);
        }
    }
}
