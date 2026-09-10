using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Dükkan önü şirket tabelası. Seviye 1/2/3'te model değişir; yazı her seviyede okunaklı kalır.
    /// </summary>
    public class StorefrontSignboardController : MonoBehaviour
    {
        public static StorefrontSignboardController Instance { get; private set; }

        private static readonly Vector3 SIGN_ROOT_POS = new Vector3(4.5f, 0.0f, -3.08f);
        private static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();

        private GameObject currentSignModelObj;
        private Text companyNameText;
        private Text sloganText;
        private const int CompanyNameMaxChars = 18;
        private const int SloganMaxChars = 32;
        private int currentLevel = 1;
        private string cachedCompanyName = "";
        private float checkTimer;

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

        private void Update()
        {
            checkTimer += Time.deltaTime;
            if (checkTimer < 1.0f) return;
            checkTimer = 0f;

            if (StoreStatusManager.Instance != null
                && !string.IsNullOrEmpty(StoreStatusManager.Instance.CompanyName)
                && StoreStatusManager.Instance.CompanyName != cachedCompanyName)
            {
                cachedCompanyName = StoreStatusManager.Instance.CompanyName;
                UpdateTextLabels();
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

            companyNameText = null;
            sloganText = null;

            currentSignModelObj = new GameObject($"Storefront_Signboard_Lv{level}");
            currentSignModelObj.transform.SetParent(transform, false);
            currentSignModelObj.transform.localPosition = SIGN_ROOT_POS;
            currentSignModelObj.transform.localRotation = Quaternion.identity;

            switch (level)
            {
                case 1:
                    BuildLevel1BoutiqueSign(currentSignModelObj.transform);
                    break;
                case 2:
                    BuildLevel2SupermarketSign(currentSignModelObj.transform);
                    break;
                default:
                    BuildLevel3HypermarketSign(currentSignModelObj.transform);
                    break;
            }

            UpdateTextLabels();
        }

        // =========================================================================
        // SEVİYE 1 — Ahşap kaset fasya + pirinç çerçeve (butik doğal market)
        // =========================================================================
        private void BuildLevel1BoutiqueSign(Transform parent)
        {
            const float y = 4.18f;
            const float w = 5.35f;
            const float h = 1.28f;
            const float depth = 0.16f;

            CreateRaceway(parent, new Vector3(0f, y, 0.10f), w + 0.55f, 0.10f, Mat("RacewayDark", new Color(0.22f, 0.23f, 0.25f), 0.55f, 0.35f));
            CreateWallMount(parent, new Vector3(-2.05f, 3.42f, 0.08f), 0.82f, Mat("MountSteel", new Color(0.28f, 0.30f, 0.33f), 0.70f, 0.40f));
            CreateWallMount(parent, new Vector3(2.05f, 3.42f, 0.08f), 0.82f, Mat("MountSteel", new Color(0.28f, 0.30f, 0.33f), 0.70f, 0.40f));

            CreateWoodCabinet(parent, new Vector3(0f, y, 0.02f), w + 0.28f, h + 0.22f, depth + 0.04f);

            Material brass = Mat("BrassFrame", new Color(0.72f, 0.55f, 0.22f), 0.85f, 0.62f);
            CreateRectFrame(parent, "Brass", new Vector3(0f, y, -0.07f), w + 0.04f, h + 0.04f, 0.055f, 0.055f, brass);

            Material ivory = Mat("IvoryFace", new Color(0.93f, 0.88f, 0.76f), 0.02f, 0.38f);
            CreatePrim(PrimitiveType.Cube, "Letter_Face", parent, new Vector3(0f, y, -0.10f), new Vector3(w - 0.10f, h - 0.10f, 0.03f), ivory);

            CreateCornerRosette(parent, new Vector3(-(w * 0.5f) + 0.08f, y + (h * 0.5f) - 0.08f, -0.12f), brass);
            CreateCornerRosette(parent, new Vector3((w * 0.5f) - 0.08f, y + (h * 0.5f) - 0.08f, -0.12f), brass);
            CreateCornerRosette(parent, new Vector3(-(w * 0.5f) + 0.08f, y - (h * 0.5f) + 0.08f, -0.12f), brass);
            CreateCornerRosette(parent, new Vector3((w * 0.5f) - 0.08f, y - (h * 0.5f) + 0.08f, -0.12f), brass);

            CreateGooseneckLamp(parent, new Vector3(-1.55f, y + (h * 0.5f) + 0.18f, 0.02f), GetBrandWash(), Mat("HoodBronze", new Color(0.22f, 0.16f, 0.10f), 0.55f, 0.35f));
            CreateGooseneckLamp(parent, new Vector3(1.55f, y + (h * 0.5f) + 0.18f, 0.02f), GetBrandWash(), Mat("HoodBronze", new Color(0.22f, 0.16f, 0.10f), 0.55f, 0.35f));
            CreateSignWashLights(parent, y, w, h, GetBrandWash(), 2);

            CreateSignTypography(
                parent,
                new Vector3(0f, y, -0.18f),
                new Vector2(w - 0.22f, h - 0.18f),
                GetBrandTitleColor(true),
                GetBrandOutlineColor(true),
                GetBrandPlateColor(true));
        }

        // =========================================================================
        // SEVİYE 2 — Alüminyum kaset LED tabela (süpermarket)
        // =========================================================================
        private void BuildLevel2SupermarketSign(Transform parent)
        {
            const float y = 4.32f;
            const float w = 6.70f;
            const float h = 1.48f;
            const float depth = 0.20f;

            Material alum = Mat("BrushedAlu", new Color(0.62f, 0.65f, 0.70f), 0.88f, 0.48f);
            Material navy = Mat("NavyReturn", new Color(0.08f, 0.16f, 0.28f), 0.25f, 0.42f);
            Material face = Mat("EmeraldAcrylic", new Color(0.05f, 0.18f, 0.16f), 0.08f, 0.52f);
            Material led = Emissive("LedCyan", new Color(0.15f, 0.85f, 0.80f), new Color(0.20f, 1.1f, 1.05f));

            CreateRaceway(parent, new Vector3(0f, y, 0.14f), w + 0.70f, 0.12f, alum);
            CreateWallMount(parent, new Vector3(-2.55f, 3.38f, 0.10f), 0.98f, alum);
            CreateWallMount(parent, new Vector3(0f, 3.38f, 0.10f), 0.98f, alum);
            CreateWallMount(parent, new Vector3(2.55f, 3.38f, 0.10f), 0.98f, alum);

            CreateLightbox(parent, new Vector3(0f, y, 0f), w, h, depth, navy, face);
            CreateRectFrame(parent, "AluFrame", new Vector3(0f, y, -0.11f), w + 0.06f, h + 0.06f, 0.06f, 0.07f, alum);

            CreatePrim(PrimitiveType.Cube, "LED_Top_Channel", parent, new Vector3(0f, y + (h * 0.5f) - 0.045f, -0.125f), new Vector3(w - 0.18f, 0.045f, 0.03f), led);
            CreatePrim(PrimitiveType.Cube, "LED_Bot_Channel", parent, new Vector3(0f, y - (h * 0.5f) + 0.045f, -0.125f), new Vector3(w - 0.18f, 0.045f, 0.03f), led);

            CreateFloodLightBar(parent, new Vector3(-2.15f, y + (h * 0.5f) + 0.16f, -0.02f), alum, GetBrandWash());
            CreateFloodLightBar(parent, new Vector3(0f, y + (h * 0.5f) + 0.16f, -0.02f), alum, GetBrandWash());
            CreateFloodLightBar(parent, new Vector3(2.15f, y + (h * 0.5f) + 0.16f, -0.02f), alum, GetBrandWash());
            CreateSignWashLights(parent, y, w, h, GetBrandWash(), 2);

            CreateSignTypography(
                parent,
                new Vector3(0f, y, -0.20f),
                new Vector2(w - 0.28f, h - 0.18f),
                GetBrandTitleColor(false),
                GetBrandOutlineColor(false),
                GetBrandPlateColor(false));
        }

        // =========================================================================
        // SEVİYE 3 — Altın eloksal + akrilik hipermarket fasya
        // =========================================================================
        private void BuildLevel3HypermarketSign(Transform parent)
        {
            const float y = 4.50f;
            const float w = 8.20f;
            const float h = 1.72f;
            const float depth = 0.26f;

            Material chrome = Mat("Chrome", new Color(0.78f, 0.80f, 0.84f), 0.95f, 0.78f);
            Material gold = Mat("GoldAnodized", new Color(0.83f, 0.66f, 0.22f), 0.92f, 0.72f);
            Material piano = Mat("PianoBlack", new Color(0.04f, 0.04f, 0.05f), 0.35f, 0.72f);
            Material whiteFace = Mat("WhiteAcrylic", new Color(0.10f, 0.10f, 0.12f), 0.06f, 0.55f);
            Material goldGlow = Emissive("GoldEdge", new Color(0.90f, 0.72f, 0.28f), new Color(0.55f, 0.40f, 0.12f));

            CreateRaceway(parent, new Vector3(0f, y, 0.18f), w + 0.90f, 0.14f, chrome);
            CreateWallMount(parent, new Vector3(-3.35f, 3.32f, 0.12f), 1.18f, chrome);
            CreateWallMount(parent, new Vector3(-1.15f, 3.32f, 0.12f), 1.18f, chrome);
            CreateWallMount(parent, new Vector3(1.15f, 3.32f, 0.12f), 1.18f, chrome);
            CreateWallMount(parent, new Vector3(3.35f, 3.32f, 0.12f), 1.18f, chrome);

            CreateLightbox(parent, new Vector3(0f, y, 0.02f), w + 0.12f, h + 0.12f, depth, piano, whiteFace);

            CreateRectFrame(parent, "GoldOuter", new Vector3(0f, y, -0.12f), w + 0.16f, h + 0.16f, 0.07f, 0.085f, gold);
            CreateRectFrame(parent, "BlackReveal", new Vector3(0f, y, -0.14f), w - 0.02f, h - 0.02f, 0.03f, 0.028f, piano);

            CreateSteppedCrown(parent, new Vector3(0f, y + (h * 0.5f) + 0.22f, 0.0f), w + 0.55f, gold, chrome);
            CreatePrim(PrimitiveType.Cube, "Gold_Pinstripe", parent, new Vector3(0f, y - 0.18f, -0.155f), new Vector3(w - 0.90f, 0.025f, 0.02f), goldGlow);

            CreateFloodLightBar(parent, new Vector3(-3.05f, y + (h * 0.5f) + 0.20f, -0.04f), chrome, GetBrandWash());
            CreateFloodLightBar(parent, new Vector3(-1.05f, y + (h * 0.5f) + 0.20f, -0.04f), chrome, GetBrandWash());
            CreateFloodLightBar(parent, new Vector3(1.05f, y + (h * 0.5f) + 0.20f, -0.04f), chrome, GetBrandWash());
            CreateFloodLightBar(parent, new Vector3(3.05f, y + (h * 0.5f) + 0.20f, -0.04f), chrome, GetBrandWash());
            CreateSignWashLights(parent, y, w, h, GetBrandWash(), 2);

            CreateSignTypography(
                parent,
                new Vector3(0f, y, -0.22f),
                new Vector2(w - 0.36f, h - 0.20f),
                GetBrandTitleColor(false),
                GetBrandOutlineColor(false),
                GetBrandPlateColor(false));
        }

        // =========================================================================
        // TİPOGRAFİ — otobüs durağı cephesi, World Space UI (görünür, düz, taşmaz)
        // =========================================================================
        private void CreateSignTypography(
            Transform parent,
            Vector3 streetPos,
            Vector2 worldSize,
            Color titleColor,
            Color outlineColor,
            Color plateColor)
        {
            Font font = UIStyleUtility.GetGlobalFont(64);
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            GameObject canvasObj = new GameObject("Signboard_Street_Canvas");
            canvasObj.layer = 0;
            canvasObj.transform.SetParent(parent, false);
            canvasObj.transform.localPosition = streetPos;
            canvasObj.transform.localRotation = Quaternion.identity;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = false;
            if (Camera.main != null) canvas.worldCamera = Camera.main;

            const float pixel = 0.0025f;
            RectTransform cRect = canvasObj.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(worldSize.x / pixel, worldSize.y / pixel);
            cRect.localScale = new Vector3(pixel, pixel, pixel);

            canvasObj.AddComponent<RectMask2D>();

            GameObject plateObj = new GameObject("Name_Plate");
            plateObj.transform.SetParent(canvasObj.transform, false);
            RectTransform plateRect = plateObj.AddComponent<RectTransform>();
            plateRect.anchorMin = Vector2.zero;
            plateRect.anchorMax = Vector2.one;
            plateRect.offsetMin = Vector2.zero;
            plateRect.offsetMax = Vector2.zero;
            Image plate = plateObj.AddComponent<Image>();
            plate.color = plateColor;
            plate.raycastTarget = false;

            GameObject textObj = new GameObject("Company_Name");
            textObj.transform.SetParent(canvasObj.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            if (tRect == null) tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0.38f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.offsetMin = new Vector2(36f, 8f);
            tRect.offsetMax = new Vector2(-36f, -18f);

            companyNameText = textObj.AddComponent<Text>();
            companyNameText.font = font;
            companyNameText.fontSize = 120;
            companyNameText.resizeTextForBestFit = true;
            companyNameText.resizeTextMinSize = 28;
            companyNameText.resizeTextMaxSize = 140;
            companyNameText.fontStyle = FontStyle.Bold;
            companyNameText.alignment = TextAnchor.MiddleCenter;
            companyNameText.color = titleColor;
            companyNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            companyNameText.verticalOverflow = VerticalWrapMode.Truncate;
            companyNameText.supportRichText = false;
            companyNameText.raycastTarget = false;

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(4f, -4f);

            GameObject sloganObj = new GameObject("Company_Slogan");
            sloganObj.transform.SetParent(canvasObj.transform, false);
            RectTransform sRect = sloganObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0f);
            sRect.anchorMax = new Vector2(1f, 0.40f);
            sRect.offsetMin = new Vector2(48f, 16f);
            sRect.offsetMax = new Vector2(-48f, -4f);

            sloganText = sloganObj.AddComponent<Text>();
            sloganText.font = font;
            sloganText.fontSize = 42;
            sloganText.resizeTextForBestFit = true;
            sloganText.resizeTextMinSize = 18;
            sloganText.resizeTextMaxSize = 48;
            sloganText.fontStyle = FontStyle.Italic;
            sloganText.alignment = TextAnchor.UpperCenter;
            sloganText.color = Color.Lerp(titleColor, plateColor, 0.18f);
            sloganText.horizontalOverflow = HorizontalWrapMode.Wrap;
            sloganText.verticalOverflow = VerticalWrapMode.Truncate;
            sloganText.supportRichText = false;
            sloganText.raycastTarget = false;
        }

        private static Color GetBrandColor()
        {
            return StoreStatusManager.Instance != null
                ? StoreStatusManager.Instance.BrandColor
                : StoreStatusManager.GetDefaultBrandColor(BrandIdentity.LocalProducer);
        }

        private static Color GetBrandWash()
        {
            return StoreStatusManager.Instance != null
                ? StoreStatusManager.Instance.GetBrandWashColor()
                : Color.Lerp(GetBrandColor(), Color.white, 0.38f);
        }

        private static float GetBrandLuminance(Color c)
        {
            return (0.299f * c.r) + (0.587f * c.g) + (0.114f * c.b);
        }

        private static Color GetBrandPlateColor(bool ivoryBoutique)
        {
            Color brand = GetBrandColor();
            if (ivoryBoutique)
            {
                return Color.Lerp(new Color(0.96f, 0.90f, 0.76f), brand, 0.42f);
            }

            return Color.Lerp(new Color(0.06f, 0.08f, 0.10f), brand, 0.72f);
        }

        private static Color GetBrandTitleColor(bool ivoryBoutique)
        {
            Color plate = GetBrandPlateColor(ivoryBoutique);
            return GetBrandLuminance(plate) > 0.48f
                ? new Color(0.12f, 0.08f, 0.05f)
                : Color.white;
        }

        private static Color GetBrandOutlineColor(bool ivoryBoutique)
        {
            Color title = GetBrandTitleColor(ivoryBoutique);
            return GetBrandLuminance(title) > 0.5f
                ? new Color(0.05f, 0.05f, 0.06f, 0.85f)
                : new Color(0.95f, 0.93f, 0.88f, 0.70f);
        }

        private void UpdateTextLabels()
        {
            if (companyNameText == null) return;

            string nameToDisplay = !string.IsNullOrWhiteSpace(cachedCompanyName) ? cachedCompanyName.Trim() : "Farm2Shelf Market";
            nameToDisplay = nameToDisplay.ToUpper();
            if (nameToDisplay.Length > CompanyNameMaxChars)
            {
                nameToDisplay = nameToDisplay.Substring(0, CompanyNameMaxChars);
            }

            companyNameText.text = nameToDisplay;
            if (nameToDisplay.Length <= 8)
            {
                companyNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
                companyNameText.resizeTextMinSize = 48;
            }
            else
            {
                companyNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                companyNameText.resizeTextMinSize = 28;
            }

            if (sloganText != null)
            {
                string slogan = StoreStatusManager.Instance != null
                    ? StoreStatusManager.Instance.GetResolvedSlogan()
                    : "";
                if (slogan.Length > SloganMaxChars)
                {
                    slogan = slogan.Substring(0, SloganMaxChars);
                }

                sloganText.text = slogan;
            }
        }

        // =========================================================================
        // 3D PARÇALAR
        // =========================================================================
        private void CreateWoodCabinet(Transform parent, Vector3 center, float w, float h, float depth)
        {
            Material woodDark = Mat("WoodDark", new Color(0.32f, 0.20f, 0.10f), 0.04f, 0.22f);
            Material wood = Mat("WoodSlat", new Color(0.52f, 0.34f, 0.16f), 0.02f, 0.28f);

            CreatePrim(PrimitiveType.Cube, "Wood_Cabinet_Core", parent, center, new Vector3(w, h, depth), woodDark);

            int slats = 6;
            float slatH = (h - 0.08f) / slats;
            float startY = center.y - (h * 0.5f) + 0.04f + slatH * 0.5f;
            for (int i = 0; i < slats; i++)
            {
                float yy = startY + i * slatH;
                Color tone = Color.Lerp(new Color(0.46f, 0.29f, 0.14f), new Color(0.58f, 0.38f, 0.18f), (i % 2) * 0.35f);
                Material slatMat = Mat($"WoodSlat_{i % 2}", tone, 0.02f, 0.26f);
                CreatePrim(PrimitiveType.Cube, "Wood_Slat", parent, new Vector3(center.x, yy, center.z - depth * 0.42f), new Vector3(w - 0.06f, slatH - 0.012f, 0.025f), slatMat);
            }
        }

        private void CreateLightbox(Transform parent, Vector3 center, float w, float h, float depth, Material returnMat, Material faceMat)
        {
            CreatePrim(PrimitiveType.Cube, "Box_Back", parent, center + new Vector3(0f, 0f, depth * 0.35f), new Vector3(w, h, depth * 0.28f), returnMat);
            CreatePrim(PrimitiveType.Cube, "Box_TopReturn", parent, center + new Vector3(0f, h * 0.5f - 0.03f, 0f), new Vector3(w, 0.06f, depth), returnMat);
            CreatePrim(PrimitiveType.Cube, "Box_BotReturn", parent, center + new Vector3(0f, -h * 0.5f + 0.03f, 0f), new Vector3(w, 0.06f, depth), returnMat);
            CreatePrim(PrimitiveType.Cube, "Box_LeftReturn", parent, center + new Vector3(-w * 0.5f + 0.03f, 0f, 0f), new Vector3(0.06f, h, depth), returnMat);
            CreatePrim(PrimitiveType.Cube, "Box_RightReturn", parent, center + new Vector3(w * 0.5f - 0.03f, 0f, 0f), new Vector3(0.06f, h, depth), returnMat);
            CreatePrim(PrimitiveType.Cube, "Box_Face", parent, center + new Vector3(0f, 0f, -depth * 0.42f), new Vector3(w - 0.08f, h - 0.08f, 0.035f), faceMat);
        }

        private void CreateRectFrame(Transform parent, string prefix, Vector3 center, float w, float h, float depth, float thick, Material mat)
        {
            CreatePrim(PrimitiveType.Cube, prefix + "_Top", parent, center + new Vector3(0f, h * 0.5f, 0f), new Vector3(w + thick, thick, depth), mat);
            CreatePrim(PrimitiveType.Cube, prefix + "_Bot", parent, center + new Vector3(0f, -h * 0.5f, 0f), new Vector3(w + thick, thick, depth), mat);
            CreatePrim(PrimitiveType.Cube, prefix + "_L", parent, center + new Vector3(-w * 0.5f, 0f, 0f), new Vector3(thick, h, depth), mat);
            CreatePrim(PrimitiveType.Cube, prefix + "_R", parent, center + new Vector3(w * 0.5f, 0f, 0f), new Vector3(thick, h, depth), mat);
        }

        private void CreateRaceway(Transform parent, Vector3 pos, float width, float height, Material mat)
        {
            CreatePrim(PrimitiveType.Cube, "Wall_Raceway", parent, pos, new Vector3(width, height, 0.08f), mat);
        }

        private void CreateWallMount(Transform parent, Vector3 pos, float height, Material mat)
        {
            Material boltMat = Mat("BoltSteel", new Color(0.35f, 0.36f, 0.38f), 0.80f, 0.50f);
            CreatePrim(PrimitiveType.Cube, "Mount_Plate", parent, pos + new Vector3(0f, 0.08f, 0.06f), new Vector3(0.22f, 0.28f, 0.04f), mat);
            CreatePrim(PrimitiveType.Cube, "Mount_Post", parent, pos + new Vector3(0f, height * 0.35f, 0.02f), new Vector3(0.09f, height, 0.09f), mat);
            CreatePrim(PrimitiveType.Cylinder, "Mount_Bolt_TL", parent, pos + new Vector3(-0.06f, 0.16f, 0.035f), new Vector3(0.035f, 0.012f, 0.035f), boltMat, new Vector3(90f, 0f, 0f));
            CreatePrim(PrimitiveType.Cylinder, "Mount_Bolt_TR", parent, pos + new Vector3(0.06f, 0.16f, 0.035f), new Vector3(0.035f, 0.012f, 0.035f), boltMat, new Vector3(90f, 0f, 0f));
            CreatePrim(PrimitiveType.Cylinder, "Mount_Bolt_BL", parent, pos + new Vector3(-0.06f, 0.00f, 0.035f), new Vector3(0.035f, 0.012f, 0.035f), boltMat, new Vector3(90f, 0f, 0f));
            CreatePrim(PrimitiveType.Cylinder, "Mount_Bolt_BR", parent, pos + new Vector3(0.06f, 0.00f, 0.035f), new Vector3(0.035f, 0.012f, 0.035f), boltMat, new Vector3(90f, 0f, 0f));
        }

        private void CreateCornerRosette(Transform parent, Vector3 pos, Material mat)
        {
            CreatePrim(PrimitiveType.Cylinder, "Corner_Rosette", parent, pos, new Vector3(0.10f, 0.012f, 0.10f), mat, new Vector3(90f, 0f, 0f));
        }

        private void CreateGooseneckLamp(Transform parent, Vector3 wallPos, Color glow, Material hoodMat)
        {
            GameObject root = new GameObject("Gooseneck_Lamp");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = wallPos;

            Material armMat = Mat("GooseneckArm", new Color(0.18f, 0.18f, 0.20f), 0.70f, 0.40f);
            CreatePrim(PrimitiveType.Cylinder, "Arm_Rise", root.transform, new Vector3(0f, 0.08f, 0.02f), new Vector3(0.045f, 0.10f, 0.045f), armMat);
            CreatePrim(PrimitiveType.Cylinder, "Arm_Reach", root.transform, new Vector3(0f, 0.16f, -0.18f), new Vector3(0.04f, 0.16f, 0.04f), armMat, new Vector3(90f, 0f, 0f));
            CreatePrim(PrimitiveType.Cylinder, "Hood", root.transform, new Vector3(0f, 0.05f, -0.34f), new Vector3(0.22f, 0.08f, 0.22f), hoodMat, new Vector3(28f, 0f, 0f));
            CreatePrim(PrimitiveType.Sphere, "Lamp_Lens", root.transform, new Vector3(0f, -0.02f, -0.36f), new Vector3(0.11f, 0.11f, 0.11f), Emissive("GooseLens", glow, glow * 1.35f));
            CreateSignSpot(root.transform, new Vector3(0f, -0.04f, -0.38f), new Vector3(55f, 180f, 0f), glow, 2.6f, 7.5f);
        }

        private void CreateFloodLightBar(Transform parent, Vector3 pos, Material housingMat, Color glow)
        {
            GameObject root = new GameObject("Flood_Fixture");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;

            CreatePrim(PrimitiveType.Cube, "Housing", root.transform, new Vector3(0f, 0.02f, -0.12f), new Vector3(0.38f, 0.10f, 0.16f), housingMat);
            CreatePrim(PrimitiveType.Cube, "Bracket", root.transform, new Vector3(0f, 0.06f, 0.02f), new Vector3(0.08f, 0.06f, 0.18f), housingMat);
            CreatePrim(PrimitiveType.Cube, "Lens", root.transform, new Vector3(0f, -0.01f, -0.20f), new Vector3(0.32f, 0.05f, 0.03f), Emissive("FloodLens", glow, glow * 1.2f));
            CreateSignSpot(root.transform, new Vector3(0f, -0.04f, -0.22f), new Vector3(50f, 180f, 0f), glow, 2.8f, 8.0f);
        }

        private void CreateSignWashLights(Transform parent, float y, float w, float h, Color color, int count)
        {
            count = Mathf.Clamp(count, 2, 4);
            float span = w * 0.62f;
            float startX = count == 1 ? 0f : -span * 0.5f;
            float step = count <= 1 ? 0f : span / (count - 1);
            for (int i = 0; i < count; i++)
            {
                float x = startX + step * i;
                CreateSignSpot(parent, new Vector3(x, y + (h * 0.5f) + 0.08f, -0.22f), new Vector3(42f, 180f, 0f), color, 2.4f, 8.5f);
            }
        }

        private static void CreateSignSpot(Transform parent, Vector3 localPos, Vector3 euler, Color color, float intensity, float range)
        {
            GameObject go = new GameObject("Sign_SpotLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity * 1.35f;
            light.range = range + 1.5f;
            light.spotAngle = 80f;
            light.innerSpotAngle = 35f;
            light.shadows = LightShadows.None;
            light.bounceIntensity = 0f;
            light.enabled = true;
        }

        private void CreateSteppedCrown(Transform parent, Vector3 pos, float width, Material gold, Material chrome)
        {
            CreatePrim(PrimitiveType.Cube, "Crown_Step1", parent, pos + new Vector3(0f, 0.00f, 0.02f), new Vector3(width, 0.08f, 0.22f), gold);
            CreatePrim(PrimitiveType.Cube, "Crown_Step2", parent, pos + new Vector3(0f, 0.08f, 0.00f), new Vector3(width - 0.22f, 0.07f, 0.18f), chrome);
            CreatePrim(PrimitiveType.Cube, "Crown_Step3", parent, pos + new Vector3(0f, 0.15f, -0.02f), new Vector3(width - 0.42f, 0.06f, 0.14f), gold);
        }

        private static GameObject CreatePrim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, Vector3 euler = default)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = localScale;
            if (mat != null)
            {
                go.GetComponent<Renderer>().sharedMaterial = mat;
            }

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }

            return go;
        }

        private static Material Mat(string name, Color color, float metallic, float smoothness)
        {
            if (MatCache.TryGetValue(name, out Material cached) && cached != null) return cached;

            Material mat = new Material(ShaderHelper.GetLitShader()) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            MatCache[name] = mat;
            return mat;
        }

        private static Material Emissive(string name, Color baseColor, Color emission)
        {
            if (MatCache.TryGetValue(name, out Material cached) && cached != null) return cached;

            Material mat = Mat(name, baseColor, 0.15f, 0.45f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emission);
                mat.EnableKeyword("_EMISSION");
            }

            return mat;
        }
    }
}
