using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Farm2Shelf Dükkan Önü (Otobüs Durağı Arkası) Seviyeye Göre Gelişen Işıklı Tabela Sistemi.
    /// - Sabit Konum: Otobüs durağının hemen arkasında, ön bina cephesinde (X: 4.5, Z: -3.08).
    /// - Dinamik Şirket İsmi: Oyuncunun belirlediği şirket ismini 7/24 büyük, tam ortalı ve okunaklı yansıtır.
    /// - Seviyeye Göre Gelişen 3D Mimari Modeller (Seviye 1, 2, 3).
    /// - Sabit Emissive Işıma & Mat Materyaller: Kamera hareket ettiğinde ışık kayması, parlama veya titreme yaşanmaz.
    /// </summary>
    public class StorefrontSignboardController : MonoBehaviour
    {
        public static StorefrontSignboardController Instance { get; private set; }

        [Header("Konumlandırma")]
        private static readonly Vector3 SIGN_ROOT_POS = new Vector3(4.5f, 0.0f, -3.08f);

        [Header("Aktif Tabela Referansları")]
        private GameObject currentSignModelObj;
        private Text companyNameTextComponent;
        private List<Light> signboardLights = new List<Light>();
        private List<Renderer> emissiveRenderers = new List<Renderer>();

        private int currentLevel = 1;
        private string cachedCompanyName = "";

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            SubscribeEvents();
            if (currentSignModelObj == null)
            {
                RefreshSignboard();
            }
        }

        private float checkTimer = 0f;

        private void Update()
        {
            checkTimer += Time.deltaTime;
            if (checkTimer >= 1.0f)
            {
                checkTimer = 0f;
                if (StoreStatusManager.Instance != null && !string.IsNullOrEmpty(StoreStatusManager.Instance.CompanyName) && StoreStatusManager.Instance.CompanyName != cachedCompanyName)
                {
                    cachedCompanyName = StoreStatusManager.Instance.CompanyName;
                    UpdateTextLabels();
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (Instance == this) Instance = null;
        }

        private void SubscribeEvents()
        {
            if (StoreStatusManager.Instance != null)
            {
                StoreStatusManager.Instance.OnCompanyNameChanged -= HandleCompanyNameChanged;
                StoreStatusManager.Instance.OnCompanyNameChanged += HandleCompanyNameChanged;
            }

            EnvironmentBuilder.OnStoreUpgraded -= HandleStoreUpgraded;
            EnvironmentBuilder.OnStoreUpgraded += HandleStoreUpgraded;
        }

        private void UnsubscribeEvents()
        {
            if (StoreStatusManager.Instance != null)
            {
                StoreStatusManager.Instance.OnCompanyNameChanged -= HandleCompanyNameChanged;
            }

            EnvironmentBuilder.OnStoreUpgraded -= HandleStoreUpgraded;
        }

        private void HandleCompanyNameChanged(string newName)
        {
            cachedCompanyName = newName;
            UpdateTextLabels();
        }

        private void HandleStoreUpgraded(int newLevel)
        {
            currentLevel = newLevel;
            RefreshSignboard();
        }

        /// <summary>
        /// Tabelayı mevcut dükkan seviyesine ve şirket ismine göre baştan inşa eder.
        /// </summary>
        public void RefreshSignboard()
        {
            if (EnvironmentBuilder.Instance != null)
            {
                currentLevel = Mathf.Clamp(EnvironmentBuilder.Instance.CurrentUpgradeLevel, 1, 3);
            }

            if (StoreStatusManager.Instance != null && !string.IsNullOrWhiteSpace(StoreStatusManager.Instance.CompanyName))
            {
                cachedCompanyName = StoreStatusManager.Instance.CompanyName;
            }
            else if (string.IsNullOrEmpty(cachedCompanyName))
            {
                cachedCompanyName = "Farm2Shelf Market";
            }

            BuildSignboardModel(currentLevel);
        }

        private void BuildSignboardModel(int level)
        {
            // Eski modeli temizle
            if (currentSignModelObj != null)
            {
                if (Application.isPlaying) Destroy(currentSignModelObj);
                else DestroyImmediate(currentSignModelObj);
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name.StartsWith("Storefront_Signboard"))
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }

            signboardLights.Clear();
            emissiveRenderers.Clear();

            currentSignModelObj = new GameObject($"Storefront_Signboard_Lv{level}");
            currentSignModelObj.transform.SetParent(transform, false);
            currentSignModelObj.transform.localPosition = SIGN_ROOT_POS;
            currentSignModelObj.transform.localRotation = Quaternion.identity;

            Font globalFont = UIStyleUtility.GetGlobalFont();

            switch (level)
            {
                case 1:
                    BuildLevel1BoutiqueSign(currentSignModelObj.transform, globalFont);
                    break;
                case 2:
                    BuildLevel2SupermarketSign(currentSignModelObj.transform, globalFont);
                    break;
                case 3:
                default:
                    BuildLevel3HypermarketSign(currentSignModelObj.transform, globalFont);
                    break;
            }

            UpdateTextLabels();
        }

        // =========================================================================
        // SEVİYE 1: AHŞAP & ANTRASİT KOMPOZİT LED TABELA (BUTİK DOĞAL MARKET)
        // =========================================================================
        private void BuildLevel1BoutiqueSign(Transform parent, Font font)
        {
            float baseY = 4.20f;
            float signW = 5.20f;
            float signH = 1.35f;

            // 1. Çelik Montaj Ayakları
            CreateSteelBracket(parent, new Vector3(-2.0f, 3.35f, 0.04f), 0.90f);
            CreateSteelBracket(parent, new Vector3(2.0f, 3.35f, 0.04f), 0.90f);

            // 2. Arka Ahşap Lambrili Çerçeve
            GameObject woodBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            woodBack.name = "Wood_Back_Louver";
            woodBack.transform.SetParent(parent, false);
            woodBack.transform.localPosition = new Vector3(0f, baseY, 0.02f);
            woodBack.transform.localScale = new Vector3(signW + 0.30f, signH + 0.20f, 0.12f);
            woodBack.GetComponent<Renderer>().sharedMaterial = CreateMaterial("SignWoodMat", new Color(0.55f, 0.36f, 0.18f), 0f, 0.15f);

            // 3. Ön Mat Antrasit Kompozit Panel
            GameObject mainPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainPanel.name = "Antracite_Front_Panel";
            mainPanel.transform.SetParent(parent, false);
            mainPanel.transform.localPosition = new Vector3(0f, baseY, -0.04f);
            mainPanel.transform.localScale = new Vector3(signW, signH, 0.08f);
            mainPanel.GetComponent<Renderer>().sharedMaterial = CreateMaterial("SignDarkPanelMat", new Color(0.12f, 0.14f, 0.18f), 0f, 0.15f);

            // 4. Pirinç Altın Kenarlık Çerçevesi (Emissive Işıma)
            GameObject goldTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goldTrim.name = "Gold_Brass_Trim";
            goldTrim.transform.SetParent(parent, false);
            goldTrim.transform.localPosition = new Vector3(0f, baseY, -0.06f);
            goldTrim.transform.localScale = new Vector3(signW + 0.06f, signH + 0.06f, 0.02f);
            goldTrim.GetComponent<Renderer>().sharedMaterial = CreateEmissiveMaterial("SignGoldTrimMat", new Color(0.95f, 0.78f, 0.25f), new Color(0.95f, 0.78f, 0.25f) * 1.5f);
            emissiveRenderers.Add(goldTrim.GetComponent<Renderer>());

            // 5. WorldSpace UI Canvas (Tam Ortalanmış & Gölgeli Şirket İsmi)
            CreateSignCanvas(parent, new Vector3(0f, baseY, -0.12f), new Vector2(signW - 0.5f, signH - 0.2f), font, 94,
                new Color(1.0f, 0.96f, 0.85f));

            // 6. Üst Gooseneck Siyah Metal Lambalar (Emissive Ampullü)
            CreateGooseneckFixture(parent, new Vector3(-1.5f, baseY + (signH / 2f) + 0.25f, -0.38f), new Color(1.0f, 0.90f, 0.65f));
            CreateGooseneckFixture(parent, new Vector3(1.5f, baseY + (signH / 2f) + 0.25f, -0.38f), new Color(1.0f, 0.90f, 0.65f));

            // 7. Yumuşak Difüz Işıma (Kaymayan Soft Fill Light)
            CreateSoftFillLight(parent, new Vector3(0f, baseY, -0.6f), new Color(1.0f, 0.90f, 0.75f), 6.5f, 1.2f);
        }

        // =========================================================================
        // SEVİYE 2: FIRÇALANMIŞ KOBALT & ZÜMRÜT ALÜMİNYUM LED TABELA (SÜPERMARKET)
        // =========================================================================
        private void BuildLevel2SupermarketSign(Transform parent, Font font)
        {
            float baseY = 4.35f;
            float signW = 6.60f;
            float signH = 1.60f;

            // 1. Üçlü Güçlendirilmiş Montaj Kolonu
            CreateSteelBracket(parent, new Vector3(-2.6f, 3.35f, 0.04f), 1.05f);
            CreateSteelBracket(parent, new Vector3(0.0f, 3.35f, 0.04f), 1.05f);
            CreateSteelBracket(parent, new Vector3(2.6f, 3.35f, 0.04f), 1.05f);

            // 2. Kobalt Mavi Arka Panel
            GameObject cobaltBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cobaltBack.name = "Cobalt_Back_Plate";
            cobaltBack.transform.SetParent(parent, false);
            cobaltBack.transform.localPosition = new Vector3(0f, baseY, 0.03f);
            cobaltBack.transform.localScale = new Vector3(signW + 0.35f, signH + 0.25f, 0.14f);
            cobaltBack.GetComponent<Renderer>().sharedMaterial = CreateMaterial("SignCobaltMat", new Color(0.10f, 0.22f, 0.45f), 0f, 0.20f);

            // 3. Ön Zümrüt & Gece Siyahı Alüminyum Kompozit Yüzey
            GameObject frontPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontPanel.name = "Emerald_Front_Panel";
            frontPanel.transform.SetParent(parent, false);
            frontPanel.transform.localPosition = new Vector3(0f, baseY, -0.04f);
            frontPanel.transform.localScale = new Vector3(signW, signH, 0.08f);
            frontPanel.GetComponent<Renderer>().sharedMaterial = CreateMaterial("SignEmeraldPanelMat", new Color(0.06f, 0.16f, 0.14f), 0f, 0.20f);

            // 4. Üst ve Alt Çift Neon LED Şeridi (Glow Strips)
            GameObject topLed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topLed.name = "Top_Neon_LED_Bar";
            topLed.transform.SetParent(parent, false);
            topLed.transform.localPosition = new Vector3(0f, baseY + (signH / 2f) + 0.03f, -0.07f);
            topLed.transform.localScale = new Vector3(signW + 0.10f, 0.06f, 0.06f);
            topLed.GetComponent<Renderer>().sharedMaterial = CreateEmissiveMaterial("NeonCyanMat", new Color(0.00f, 0.95f, 0.85f), new Color(0.00f, 0.95f, 0.85f) * 2.5f);
            emissiveRenderers.Add(topLed.GetComponent<Renderer>());

            GameObject bottomLed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomLed.name = "Bottom_Neon_LED_Bar";
            bottomLed.transform.SetParent(parent, false);
            bottomLed.transform.localPosition = new Vector3(0f, baseY - (signH / 2f) - 0.03f, -0.07f);
            bottomLed.transform.localScale = new Vector3(signW + 0.10f, 0.06f, 0.06f);
            bottomLed.GetComponent<Renderer>().sharedMaterial = CreateEmissiveMaterial("NeonCyanMat", new Color(0.00f, 0.95f, 0.85f), new Color(0.00f, 0.95f, 0.85f) * 2.5f);
            emissiveRenderers.Add(bottomLed.GetComponent<Renderer>());

            // 5. WorldSpace UI Canvas (Tam Ortalanmış Şirket İsmi)
            CreateSignCanvas(parent, new Vector3(0f, baseY, -0.13f), new Vector2(signW - 0.6f, signH - 0.2f), font, 114,
                new Color(0.95f, 1.0f, 1.0f));

            // 6. Üçlü Lambalar (Emissive Ampullü)
            CreateGooseneckFixture(parent, new Vector3(-2.2f, baseY + (signH / 2f) + 0.32f, -0.45f), new Color(0.90f, 0.96f, 1.0f));
            CreateGooseneckFixture(parent, new Vector3(0.0f, baseY + (signH / 2f) + 0.32f, -0.45f), new Color(0.90f, 0.96f, 1.0f));
            CreateGooseneckFixture(parent, new Vector3(2.2f, baseY + (signH / 2f) + 0.32f, -0.45f), new Color(0.90f, 0.96f, 1.0f));

            // 7. Yumuşak Difüz Işıma
            CreateSoftFillLight(parent, new Vector3(0f, baseY, -0.7f), new Color(0.85f, 0.95f, 1.0f), 7.5f, 1.4f);
        }

        // =========================================================================
        // SEVİYE 3: ALTIN VARAKLI & KRİSTAL AKRİLİK ÇİFT TAÇLI HİPERMARKET TOTEMİ
        // =========================================================================
        private void BuildLevel3HypermarketSign(Transform parent, Font font)
        {
            float baseY = 4.55f;
            float signW = 8.40f;
            float signH = 1.95f;

            // 1. Dörtlü Sanayi Destek Sütunları
            CreateSteelBracket(parent, new Vector3(-3.5f, 3.35f, 0.04f), 1.25f);
            CreateSteelBracket(parent, new Vector3(-1.2f, 3.35f, 0.04f), 1.25f);
            CreateSteelBracket(parent, new Vector3(1.2f, 3.35f, 0.04f), 1.25f);
            CreateSteelBracket(parent, new Vector3(3.5f, 3.35f, 0.04f), 1.25f);

            // 2. Üst Mimari Taç Profil
            GameObject crownTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crownTop.name = "Hypermarket_Crown_Top";
            crownTop.transform.SetParent(parent, false);
            crownTop.transform.localPosition = new Vector3(0f, baseY + (signH / 2f) + 0.22f, 0.02f);
            crownTop.transform.localScale = new Vector3(signW + 0.60f, 0.28f, 0.25f);
            crownTop.GetComponent<Renderer>().sharedMaterial = CreateEmissiveMaterial("SignGoldCrownMat", new Color(0.98f, 0.82f, 0.22f), new Color(0.98f, 0.82f, 0.22f) * 2.2f);
            emissiveRenderers.Add(crownTop.GetComponent<Renderer>());

            // 3. Parlak Siyah Arka Panel
            GameObject glossBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glossBack.name = "Piano_Gloss_Black_Panel";
            glossBack.transform.SetParent(parent, false);
            glossBack.transform.localPosition = new Vector3(0f, baseY, 0.04f);
            glossBack.transform.localScale = new Vector3(signW + 0.40f, signH + 0.25f, 0.18f);
            glossBack.GetComponent<Renderer>().sharedMaterial = CreateMaterial("SignPianoBlackMat", new Color(0.06f, 0.06f, 0.08f), 0f, 0.20f);

            // 4. Altın Varaklı Kenar Çerçevesi
            GameObject goldBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goldBorder.name = "Gold_Diamond_Border";
            goldBorder.transform.SetParent(parent, false);
            goldBorder.transform.localPosition = new Vector3(0f, baseY, -0.05f);
            goldBorder.transform.localScale = new Vector3(signW + 0.08f, signH + 0.08f, 0.04f);
            goldBorder.GetComponent<Renderer>().sharedMaterial = CreateEmissiveMaterial("GoldVarakMat", new Color(1.0f, 0.85f, 0.28f), new Color(1.0f, 0.85f, 0.28f) * 2.5f);
            emissiveRenderers.Add(goldBorder.GetComponent<Renderer>());

            // 5. WorldSpace UI Canvas (Ultra-Geniş, Tam Ortalanmış Parlak Tipografi)
            CreateSignCanvas(parent, new Vector3(0f, baseY, -0.14f), new Vector2(signW - 0.8f, signH - 0.2f), font, 138,
                new Color(1.0f, 0.98f, 0.90f));

            // 6. Dörtlü Lamba Armatürleri
            CreateGooseneckFixture(parent, new Vector3(-3.0f, baseY + (signH / 2f) + 0.40f, -0.55f), new Color(1.0f, 0.92f, 0.70f));
            CreateGooseneckFixture(parent, new Vector3(-1.0f, baseY + (signH / 2f) + 0.40f, -0.55f), new Color(1.0f, 0.92f, 0.70f));
            CreateGooseneckFixture(parent, new Vector3(1.0f, baseY + (signH / 2f) + 0.40f, -0.55f), new Color(1.0f, 0.92f, 0.70f));
            CreateGooseneckFixture(parent, new Vector3(3.0f, baseY + (signH / 2f) + 0.40f, -0.55f), new Color(1.0f, 0.92f, 0.70f));

            // 7. Yumuşak Difüz Işıma
            CreateSoftFillLight(parent, new Vector3(0f, baseY, -0.8f), new Color(1.0f, 0.92f, 0.75f), 9.0f, 1.6f);
        }

        // =========================================================================
        // YARDIMCI 3D MİMARİ VE IŞIK OLUŞTURUCULARI
        // =========================================================================

        private void CreateSteelBracket(Transform parent, Vector3 pos, float height)
        {
            GameObject bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bracket.name = "Steel_Mount_Bracket";
            bracket.transform.SetParent(parent, false);
            bracket.transform.localPosition = pos;
            bracket.transform.localScale = new Vector3(0.12f, height, 0.12f);
            bracket.GetComponent<Renderer>().sharedMaterial = CreateMaterial("SignBracketMat", new Color(0.18f, 0.20f, 0.24f), 0f, 0.2f);
        }

        private void CreateGooseneckFixture(Transform parent, Vector3 lampPos, Color lightColor)
        {
            GameObject lampFixture = new GameObject("Gooseneck_Fixture");
            lampFixture.transform.SetParent(parent, false);
            lampFixture.transform.localPosition = lampPos;

            // Lamba Başlığı
            GameObject hood = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hood.name = "Lamp_Hood";
            hood.transform.SetParent(lampFixture.transform, false);
            hood.transform.localPosition = Vector3.zero;
            hood.transform.localScale = new Vector3(0.24f, 0.12f, 0.24f);
            hood.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            hood.GetComponent<Renderer>().sharedMaterial = CreateMaterial("LampHoodMat", new Color(0.12f, 0.14f, 0.16f), 0f, 0.2f);

            // Parlayan Ampul Camı (Emissive - Işık kayması yapmaz, 7/24 pürüzsüz parlar)
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb_Glow";
            bulb.transform.SetParent(hood.transform, false);
            bulb.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            bulb.transform.localScale = new Vector3(0.80f, 0.80f, 0.80f);
            bulb.GetComponent<Renderer>().sharedMaterial = CreateEmissiveMaterial("BulbEmissiveMat", lightColor, lightColor * 3.0f);
            emissiveRenderers.Add(bulb.GetComponent<Renderer>());
        }

        private void CreateSoftFillLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
        {
            GameObject fillLightObj = new GameObject("Sign_Soft_Fill_Light");
            fillLightObj.transform.SetParent(parent, false);
            fillLightObj.transform.localPosition = pos;

            Light pLight = fillLightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = color;
            pLight.range = range;
            pLight.intensity = intensity;
            pLight.shadows = LightShadows.None;
            signboardLights.Add(pLight);
        }

        // =========================================================================
        // WORLDSPACE CANVAS & TİPOGRAFİ OLUŞTURUCUSU
        // =========================================================================

        private void CreateSignCanvas(Transform parent, Vector3 localPos, Vector2 canvasSize, Font font, int titleSize, Color titleColor)
        {
            GameObject canvasObj = new GameObject("Signboard_World_Canvas");
            canvasObj.transform.SetParent(parent, false);
            canvasObj.transform.localPosition = localPos;
            canvasObj.transform.localRotation = Quaternion.identity;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform cRect = canvasObj.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(canvasSize.x * 250f, canvasSize.y * 250f);
            cRect.localScale = new Vector3(0.004f, 0.004f, 0.004f);

            // Ana Şirket İsmi Metni (Company Name)
            GameObject titleObj = new GameObject("Title_Text");
            titleObj.transform.SetParent(canvasObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            companyNameTextComponent = titleObj.AddComponent<Text>();
            companyNameTextComponent.font = font;
            companyNameTextComponent.fontSize = titleSize;
            companyNameTextComponent.resizeTextForBestFit = true;
            companyNameTextComponent.resizeTextMinSize = 44;
            companyNameTextComponent.resizeTextMaxSize = titleSize;
            companyNameTextComponent.fontStyle = FontStyle.Bold;
            companyNameTextComponent.alignment = TextAnchor.MiddleCenter;
            companyNameTextComponent.color = titleColor;
            companyNameTextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            companyNameTextComponent.verticalOverflow = VerticalWrapMode.Truncate;
            companyNameTextComponent.supportRichText = true;

            // Kontrast & Okunabilirlik için Güçlü Siyah Çerçeve ve Gölge
            Outline tOutline = titleObj.AddComponent<Outline>();
            tOutline.effectColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            tOutline.effectDistance = new Vector2(3.5f, -3.5f);

            Shadow tShadow = titleObj.AddComponent<Shadow>();
            tShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            tShadow.effectDistance = new Vector2(4.5f, -4.5f);
        }

        private void UpdateTextLabels()
        {
            if (companyNameTextComponent != null)
            {
                string nameToDisplay = !string.IsNullOrWhiteSpace(cachedCompanyName) ? cachedCompanyName : "Farm2Shelf Market";
                companyNameTextComponent.text = $"<b>{nameToDisplay.ToUpper()}</b>";
            }
        }

        // =========================================================================
        // MATERYAL ÜRETİCİLERİ
        // =========================================================================

        private Material CreateMaterial(string name, Color color, float metallic = 0.0f, float smoothness = 0.15f)
        {
            Shader shader = ShaderHelper.GetLitShader();
            Material mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;

            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            return mat;
        }

        private Material CreateEmissiveMaterial(string name, Color baseColor, Color emissionColor)
        {
            Shader shader = ShaderHelper.GetLitShader();
            Material mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            else mat.color = baseColor;

            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
            }

            return mat;
        }
    }
}
