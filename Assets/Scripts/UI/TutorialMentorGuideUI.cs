using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Rıfat Amca rehberi: hikâye, konuşma balonu, Geç butonu ve klasik spotlight.
    /// Mevcut 10 görev mantığına dokunmaz; yalnızca yönlendirir.
    /// </summary>
    public class TutorialMentorGuideUI : MonoBehaviour
    {
        public const string MentorNameTr = "Rıfat Amca";
        public const string MentorNameEn = "Uncle Rifat";

        private static TutorialMentorGuideUI instance;
        private GameObject root;
        private RectTransform holeGlow;
        private readonly Image[] dimPanels = new Image[4];
        private Image portraitImg;
        private RectTransform portraitRect;
        private Text nameText;
        private Text speechText;
        private Button skipBtn;
        private Text skipBtnText;
        private CanvasGroup speechGroup;

        private readonly List<string> queueTr = new List<string>();
        private readonly List<string> queueEn = new List<string>();
        private int lineIndex;
        private string typed;
        private float typeTimer;
        private bool typing;
        private bool introDone;
        private TutorialStep lastSpokenStep = TutorialStep.None;
        private string lastSpeechKey = "";
        private float focusTimer;
        private float bobTime;
        private readonly List<RectTransform> focusBuf = new List<RectTransform>();

        public static bool IsGuideOpen => instance != null && instance.root != null && instance.root.activeSelf;

        public static void ShowGuide(bool playIntro = true)
        {
            if (instance == null)
            {
                GameObject host = new GameObject("TutorialMentorGuide");
                instance = host.AddComponent<TutorialMentorGuideUI>();
                Object.DontDestroyOnLoad(host);
            }
            instance.BuildIfNeeded();
            instance.root.SetActive(true);
            if (playIntro) instance.BeginIntro();
            else
            {
                instance.introDone = true;
                instance.SpeakForStep(true);
            }
        }

        public static void HideGuide()
        {
            if (instance == null) return;
            if (instance.root != null) instance.root.SetActive(false);
        }

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLang;
                LocalizationManager.Instance.OnLanguageChanged += HandleLang;
            }
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnTutorialStepChanged -= HandleStep;
                TutorialManager.Instance.OnTutorialProgressUpdated -= HandleProgress;
                TutorialManager.Instance.OnTutorialStepChanged += HandleStep;
                TutorialManager.Instance.OnTutorialProgressUpdated += HandleProgress;
            }
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLang;
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnTutorialStepChanged -= HandleStep;
                TutorialManager.Instance.OnTutorialProgressUpdated -= HandleProgress;
            }
        }

        private void HandleLang(GameLanguage lang)
        {
            RefreshStaticLabels();
            ShowCurrentLine(true);
        }

        private void HandleStep(TutorialStep step)
        {
            if (!introDone) return;
            SpeakForStep(true);
        }

        private void HandleProgress()
        {
            if (!introDone) return;
            SpeakForStep(false);
        }

        private void BuildIfNeeded()
        {
            if (root != null) return;

            root = new GameObject("Mentor_Root");
            root.transform.SetParent(transform, false);

            GameObject dimRoot = new GameObject("Mentor_DimCanvas");
            dimRoot.transform.SetParent(root.transform, false);
            Canvas dimCanvas = dimRoot.AddComponent<Canvas>();
            dimCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dimCanvas.sortingOrder = 280;
            CanvasScaler dimScaler = dimRoot.AddComponent<CanvasScaler>();
            dimScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            dimScaler.referenceResolution = new Vector2(1920, 1080);
            dimScaler.matchWidthOrHeight = 0.5f;
            dimRoot.AddComponent<GraphicRaycaster>();

            GameObject talkRoot = new GameObject("Mentor_TalkCanvas");
            talkRoot.transform.SetParent(root.transform, false);
            Canvas canvas = talkRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 322;
            CanvasScaler scaler = talkRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            talkRoot.AddComponent<GraphicRaycaster>();

            Font font = UIStyleUtility.GetGlobalFont(20);

            for (int i = 0; i < 4; i++)
            {
                GameObject p = new GameObject("Dim_" + i);
                p.transform.SetParent(dimRoot.transform, false);
                RectTransform rt = p.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Image img = p.AddComponent<Image>();
                img.color = new Color(0.02f, 0.03f, 0.05f, 0.72f);
                img.raycastTarget = true;
                dimPanels[i] = img;
            }

            GameObject glowObj = new GameObject("HoleGlow");
            glowObj.transform.SetParent(dimRoot.transform, false);
            holeGlow = glowObj.AddComponent<RectTransform>();
            holeGlow.anchorMin = new Vector2(0.5f, 0.5f);
            holeGlow.anchorMax = new Vector2(0.5f, 0.5f);
            holeGlow.pivot = new Vector2(0.5f, 0.5f);
            Image glowImg = glowObj.AddComponent<Image>();
            glowImg.sprite = UIStyleUtility.CreateOutlinePillSprite(200, 80, 18, 4, new Color(1f, 0.86f, 0.35f, 0.95f), new Color(1f, 0.92f, 0.45f, 0.06f));
            glowImg.raycastTarget = false;

            GameObject card = new GameObject("MentorCard");
            card.transform.SetParent(talkRoot.transform, false);
            RectTransform cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(1f, 0f);
            cardRt.anchorMax = new Vector2(1f, 0f);
            cardRt.pivot = new Vector2(1f, 0f);
            cardRt.anchoredPosition = new Vector2(-36f, 118f);
            cardRt.sizeDelta = new Vector2(620f, 268f);

            Image cardBg = card.AddComponent<Image>();
            cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(620, 268, 22, 3, new Color(0.95f, 0.78f, 0.32f), new Color(0.09f, 0.11f, 0.15f, 0.96f));
            cardBg.raycastTarget = true;

            GameObject portObj = new GameObject("Portrait");
            portObj.transform.SetParent(card.transform, false);
            portraitRect = portObj.AddComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0.5f);
            portraitRect.anchorMax = new Vector2(0f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(108f, 8f);
            portraitRect.sizeDelta = new Vector2(188f, 236f);
            portraitImg = portObj.AddComponent<Image>();
            portraitImg.preserveAspect = true;
            portraitImg.raycastTarget = false;
            ApplyPortrait();

            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(card.transform, false);
            RectTransform nRt = nameObj.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0.5f, 1f);
            nRt.anchoredPosition = new Vector2(70f, -10f);
            nRt.sizeDelta = new Vector2(-160f, 36f);
            nameText = nameObj.AddComponent<Text>();
            nameText.font = font;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = new Color(1f, 0.86f, 0.42f);
            nameText.raycastTarget = false;
            Fit(nameText, 22, true);

            GameObject speechObj = new GameObject("Speech");
            speechObj.transform.SetParent(card.transform, false);
            speechGroup = speechObj.AddComponent<CanvasGroup>();
            RectTransform sRt = speechObj.AddComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(210f, 58f);
            sRt.offsetMax = new Vector2(-18f, -48f);
            speechText = speechObj.AddComponent<Text>();
            speechText.font = font;
            speechText.alignment = TextAnchor.UpperLeft;
            speechText.color = new Color(0.94f, 0.96f, 0.98f);
            speechText.supportRichText = true;
            speechText.raycastTarget = false;
            Fit(speechText, 18, true);

            GameObject skipObj = new GameObject("SkipSpeech");
            skipObj.transform.SetParent(card.transform, false);
            RectTransform skRt = skipObj.AddComponent<RectTransform>();
            skRt.anchorMin = new Vector2(1f, 0f);
            skRt.anchorMax = new Vector2(1f, 0f);
            skRt.pivot = new Vector2(1f, 0f);
            skRt.anchoredPosition = new Vector2(-16f, 12f);
            skRt.sizeDelta = new Vector2(168f, 40f);
            Image skBg = skipObj.AddComponent<Image>();
            skBg.sprite = UIStyleUtility.CreateRoundedPillSprite(168, 40, 14, new Color(0.28f, 0.34f, 0.42f));
            skipBtn = skipObj.AddComponent<Button>();
            skipBtn.targetGraphic = skBg;
            skipBtn.onClick.AddListener(OnSkipSpeech);
            GameObject skTxtObj = new GameObject("Txt");
            skTxtObj.transform.SetParent(skipObj.transform, false);
            RectTransform skt = skTxtObj.AddComponent<RectTransform>();
            skt.anchorMin = Vector2.zero;
            skt.anchorMax = Vector2.one;
            skt.offsetMin = new Vector2(8f, 2f);
            skt.offsetMax = new Vector2(-8f, -2f);
            skipBtnText = skTxtObj.AddComponent<Text>();
            skipBtnText.font = font;
            skipBtnText.fontStyle = FontStyle.Bold;
            skipBtnText.alignment = TextAnchor.MiddleCenter;
            skipBtnText.color = Color.white;
            skipBtnText.raycastTarget = false;
            Fit(skipBtnText, 17, true);

            RefreshStaticLabels();
        }

        private void ApplyPortrait()
        {
            Texture2D tex = Resources.Load<Texture2D>("Tutorial/rifat_amca_portrait");
            if (tex != null)
            {
                portraitImg.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                return;
            }
            portraitImg.sprite = UIStyleUtility.CreateRoundedPillSprite(188, 236, 28, new Color(0.72f, 0.58f, 0.42f));
        }

        private static void Fit(Text txt, int size, bool wrap)
        {
            txt.fontSize = size;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = Mathf.Max(13, size - 6);
            txt.resizeTextMaxSize = size;
            txt.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void RefreshStaticLabels()
        {
            if (nameText != null)
            {
                nameText.text = LocalizationManager.L("Mentor_NameTitle", "Rıfat Amca  •  Mahallenin Eski Bakkalı", "Uncle Rifat  •  The old neighborhood grocer");
            }
            if (skipBtnText != null)
            {
                skipBtnText.text = LocalizationManager.L("Mentor_SkipLine", "Geç ▶", "Skip ▶");
            }
        }

        private void BeginIntro()
        {
            introDone = false;
            lastSpokenStep = TutorialStep.None;
            lastSpeechKey = "";
            queueTr.Clear();
            queueEn.Clear();
            queueTr.Add("Hoş geldin evlat. Ben Rıfat… kırk yıl bu sokağın bakkalını açıp kapattım. İlk tezgâhımı tarladan gelen kasayla kurdum; o yüzden buraya hâlâ ‘Çiftlikten Rafa’ derim.");
            queueEn.Add("Welcome, kid. I'm Rifat… forty years I opened and closed this street's grocer. My first stall was a crate from the field; that's why I still call this place Farm-to-Shelf.");
            queueTr.Add("Markan tabelada duruyor. Ben emekli oldum ama gözüm hâlâ reyonlarda. On kısa görevle seni yüzüstü bırakmam: kamera, tablet, kadro, raf, pasaport, tarla.");
            queueEn.Add("Your brand sits on the sign. I retired, but my eyes are still on the shelves. Ten short quests — I won't leave you stranded: camera, tablet, staff, shelves, passports, fields.");
            queueTr.Add("Ekran kararacak; yalnızca yapman gereken yer parlayacak. Başka yere dokunamazsın. Konuşmamı bitirmek istersen balondaki Geç'e bas. Sol alttaki görev listen yerinde duruyor.");
            queueEn.Add("The screen will dim; only the next thing you must do stays bright. You can't tap the rest. To finish my line, tap Skip in the balloon. Your quest list stays bottom-left.");
            lineIndex = 0;
            StartLine();
        }

        private void SpeakForStep(bool force)
        {
            string key;
            string tr;
            string en;
            ResolveCoachLine(out key, out tr, out en);
            if (!force && key == lastSpeechKey) return;
            lastSpeechKey = key;
            lastSpokenStep = TutorialManager.Instance != null ? TutorialManager.Instance.CurrentStep : TutorialStep.None;
            queueTr.Clear();
            queueEn.Clear();
            queueTr.Add(tr);
            queueEn.Add(en);
            lineIndex = 0;
            StartLine();
        }

        private void StartLine()
        {
            typed = "";
            typeTimer = 0f;
            typing = true;
            ShowCurrentLine(false);
        }

        private void ShowCurrentLine(bool instant)
        {
            if (speechText == null) return;
            if (lineIndex < 0 || lineIndex >= queueTr.Count)
            {
                speechText.text = "";
                return;
            }
            string full = LocalizationManager.L("MentorLine_" + lastSpeechKey + "_" + lineIndex, queueTr[lineIndex], queueEn[lineIndex]);
            if (lineIndex < queueTr.Count && lastSpeechKey == "")
            {
                full = LocalizationManager.L("MentorIntro_" + lineIndex, queueTr[lineIndex], queueEn[lineIndex]);
            }
            if (instant)
            {
                typed = full;
                typing = false;
            }
            speechText.text = typing ? typed : full;
        }

        private void OnSkipSpeech()
        {
            if (typing)
            {
                typing = false;
                ShowCurrentLine(true);
                return;
            }

            if (!introDone)
            {
                lineIndex++;
                if (lineIndex >= queueTr.Count)
                {
                    introDone = true;
                    SpeakForStep(true);
                    return;
                }
                StartLine();
                return;
            }

            // Görev konuşmasını geç: bir sonraki alt hedef metnine zorla yenile
            lastSpeechKey = "";
            SpeakForStep(true);
        }

        private void Update()
        {
            if (root == null || !root.activeSelf) return;

            bobTime += Time.unscaledDeltaTime;
            if (portraitRect != null)
            {
                float bob = Mathf.Sin(bobTime * 2.1f) * 5f;
                float talk = typing ? Mathf.Sin(bobTime * 14f) * 2.2f : 0f;
                portraitRect.anchoredPosition = new Vector2(108f, 8f + bob + talk);
                portraitRect.localScale = Vector3.one * (1f + (typing ? 0.012f * Mathf.Sin(bobTime * 16f) : 0f));
            }

            if (typing && lineIndex >= 0 && lineIndex < queueTr.Count)
            {
                string full = introDone
                    ? LocalizationManager.L("MentorLine_" + lastSpeechKey + "_" + lineIndex, queueTr[lineIndex], queueEn[lineIndex])
                    : LocalizationManager.L("MentorIntro_" + lineIndex, queueTr[lineIndex], queueEn[lineIndex]);
                typeTimer += Time.unscaledDeltaTime;
                int chars = Mathf.Min(full.Length, Mathf.FloorToInt(typeTimer * 38f));
                typed = full.Substring(0, chars);
                speechText.text = typed;
                if (chars >= full.Length) typing = false;
            }

            focusTimer += Time.unscaledDeltaTime;
            if (focusTimer >= 0.2f)
            {
                focusTimer = 0f;
                RefreshSpotlight();
            }
        }

        private void RefreshSpotlight()
        {
            focusBuf.Clear();
            bool world = false;
            CollectFocus(focusBuf, ref world);

            if (world && focusBuf.Count == 0)
            {
                ApplyWorldHole();
                return;
            }
            if (focusBuf.Count == 0)
            {
                ApplyWorldHole();
                return;
            }

            Rect hole = ScreenRectOf(focusBuf[0]);
            for (int i = 1; i < focusBuf.Count; i++)
            {
                hole = Union(hole, ScreenRectOf(focusBuf[i]));
            }
            hole = Inflate(hole, 18f);
            if (world)
            {
                Rect play = new Rect(80f, 90f, Screen.width - 160f, Screen.height - 200f);
                hole = Union(hole, play);
            }
            ApplyHole(hole);
        }

        private static Rect ScreenRectOf(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            float xMin = Mathf.Min(c[0].x, c[2].x);
            float xMax = Mathf.Max(c[0].x, c[2].x);
            float yMin = Mathf.Min(c[0].y, c[2].y);
            float yMax = Mathf.Max(c[0].y, c[2].y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect Union(Rect a, Rect b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        private static Rect Inflate(Rect r, float pad)
        {
            return Rect.MinMaxRect(r.xMin - pad, r.yMin - pad, r.xMax + pad, r.yMax + pad);
        }

        private void ApplyWorldHole()
        {
            ApplyHole(new Rect(70f, 100f, Screen.width - 140f, Screen.height - 210f));
        }

        private void ApplyHole(Rect hole)
        {
            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            hole.xMin = Mathf.Clamp(hole.xMin, 0f, w);
            hole.xMax = Mathf.Clamp(hole.xMax, 0f, w);
            hole.yMin = Mathf.Clamp(hole.yMin, 0f, h);
            hole.yMax = Mathf.Clamp(hole.yMax, 0f, h);
            if (hole.width < 8f || hole.height < 8f)
            {
                ApplyWorldHole();
                return;
            }

            float x0 = hole.xMin / w;
            float x1 = hole.xMax / w;
            float y0 = hole.yMin / h;
            float y1 = hole.yMax / h;

            SetPanelAnchors(dimPanels[0], new Vector2(0f, y1), new Vector2(1f, 1f));
            SetPanelAnchors(dimPanels[1], new Vector2(0f, 0f), new Vector2(1f, y0));
            SetPanelAnchors(dimPanels[2], new Vector2(0f, y0), new Vector2(x0, y1));
            SetPanelAnchors(dimPanels[3], new Vector2(x1, y0), new Vector2(1f, y1));

            if (holeGlow != null)
            {
                holeGlow.anchorMin = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
                holeGlow.anchorMax = holeGlow.anchorMin;
                holeGlow.anchoredPosition = Vector2.zero;
                Canvas canvas = holeGlow.GetComponentInParent<Canvas>();
                float sf = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
                holeGlow.sizeDelta = new Vector2((hole.width + 14f) / sf, (hole.height + 14f) / sf);
            }
        }

        private static void SetPanelAnchors(Image img, Vector2 min, Vector2 max)
        {
            if (img == null) return;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            bool on = (max.x - min.x) > 0.004f && (max.y - min.y) > 0.004f;
            img.enabled = on;
            img.raycastTarget = on;
        }

        private void CollectFocus(List<RectTransform> into, ref bool world)
        {
            TutorialManager tm = TutorialManager.Instance;
            EKTPhoneManager phone = EKTPhoneManager.Instance;
            if (tm == null || !tm.IsTutorialActive)
            {
                world = true;
                return;
            }

            switch (tm.CurrentStep)
            {
                case TutorialStep.Step1_CameraControls:
                    world = true;
                    break;

                case TutorialStep.Step2_ExploreTabletApps:
                    FocusTabletExplore(tm, phone, into);
                    break;

                case TutorialStep.Step3_HireStoreStaffAndCallEarly:
                    FocusHireStore(tm, phone, into);
                    break;

                case TutorialStep.Step4_AssignStoreShifts:
                    FocusStoreShifts(phone, into);
                    break;

                case TutorialStep.Step5_BuyInitialFurniture:
                    FocusShopping(phone, into, 0);
                    break;

                case TutorialStep.Step6_UnpackAndPlaceFurniture:
                    world = true;
                    Add(into, "phone");
                    break;

                case TutorialStep.Step7_PlaceWholesaleBulkOrder:
                    FocusWholesale(tm, phone, into, ref world);
                    break;

                case TutorialStep.Step8_HireFarmStaffAndShifts:
                    FocusFarm(tm, phone, into);
                    break;

                case TutorialStep.Step9_BuyStartingSeeds:
                    FocusShopping(phone, into, 3);
                    break;

                case TutorialStep.Step10_PlantSeedsAndOpenStore:
                    if (tm.CropsPlantedInTutorial < 1 && (FieldPlotController.AllPlots == null || !FieldPlotController.AllPlots.Exists(p => p != null && p.State != PlotState.Empty)))
                    {
                        world = true;
                    }
                    else
                    {
                        Add(into, "hud_store_toggle");
                    }
                    break;
            }
        }

        private static void Add(List<RectTransform> into, string id)
        {
            TutorialFocusTarget.CollectActive(id, into);
        }

        private static bool TabletOpen => EKTPhoneManager.IsTabletOpen;

        private void FocusTabletExplore(TutorialManager tm, EKTPhoneManager phone, List<RectTransform> into)
        {
            if (!TabletOpen)
            {
                Add(into, "phone");
                return;
            }

            Add(into, "tablet_back");
            if (phone != null && phone.IsHomeScreenVisible)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (!tm.IsAppExplored(i)) Add(into, "app_" + i);
                }
                if (tm.ExploredAppsCount >= 7 && !tm.HasAcceptedTownContract())
                {
                    Add(into, "app_6");
                }
                return;
            }

            if (phone != null && phone.IsOnlineMarketVisible)
            {
                Add(into, "om_tab_1");
                if (phone.ActiveOnlineMarketTab == 1)
                {
                    Add(into, "om_queue");
                }
                return;
            }

            Add(into, "tablet_back");
        }

        private void FocusHireStore(TutorialManager tm, EKTPhoneManager phone, List<RectTransform> into)
        {
            if (!TabletOpen)
            {
                Add(into, "phone");
                return;
            }
            Add(into, "tablet_back");
            if (phone != null && phone.IsHomeScreenVisible)
            {
                Add(into, "app_0");
                return;
            }
            if (phone == null || !phone.IsStoreAppVisible)
            {
                Add(into, "app_0");
                return;
            }

            int cash = tm.GetStoreRoleCount(StaffRole.Kasiyer);
            int rest = tm.GetStoreRoleCount(StaffRole.Reyoncu);
            if (cash < 2 || rest < 2)
            {
                Add(into, "store_tab_2");
                if (phone.ActiveStoreTab == 2)
                {
                    if (cash < 2) Add(into, "hire_cashier");
                    if (rest < 2) Add(into, "hire_restocker");
                }
                return;
            }

            Add(into, "store_tab_1");
            if (phone.ActiveStoreTab == 1)
            {
                Add(into, "call_early");
            }
        }

        private void FocusStoreShifts(EKTPhoneManager phone, List<RectTransform> into)
        {
            if (!TabletOpen)
            {
                Add(into, "phone");
                return;
            }
            Add(into, "tablet_back");
            if (phone != null && phone.IsHomeScreenVisible)
            {
                Add(into, "app_0");
                return;
            }
            if (phone == null || !phone.IsStoreAppVisible)
            {
                Add(into, "tablet_back");
                return;
            }
            Add(into, "store_tab_3");
            if (phone.ActiveStoreTab == 3)
            {
                Add(into, "store_shift");
            }
        }

        private void FocusShopping(EKTPhoneManager phone, List<RectTransform> into, int category)
        {
            if (!TabletOpen)
            {
                Add(into, "phone");
                return;
            }
            Add(into, "tablet_back");
            if (phone != null && phone.IsHomeScreenVisible)
            {
                Add(into, "app_2");
                return;
            }
            if (phone == null || !phone.IsShoppingVisible)
            {
                Add(into, "tablet_back");
                return;
            }
            Add(into, "shop_cat_" + category);
            if (phone.ActiveShoppingCategory == category)
            {
                Add(into, "shop_content");
            }
        }

        private void FocusWholesale(TutorialManager tm, EKTPhoneManager phone, List<RectTransform> into, ref bool world)
        {
            if (!tm.DidPlaceBulkOrder)
            {
                if (!TabletOpen)
                {
                    Add(into, "phone");
                    return;
                }
                Add(into, "tablet_back");
                if (phone != null && phone.IsHomeScreenVisible)
                {
                    Add(into, "app_2");
                    return;
                }
                Add(into, "shop_bulk");
                return;
            }
            world = true;
        }

        private void FocusFarm(TutorialManager tm, EKTPhoneManager phone, List<RectTransform> into)
        {
            if (!TabletOpen)
            {
                Add(into, "phone");
                return;
            }
            Add(into, "tablet_back");
            if (phone != null && phone.IsHomeScreenVisible)
            {
                Add(into, "app_1");
                return;
            }
            if (phone == null || !phone.IsFarmAppVisible)
            {
                Add(into, "tablet_back");
                return;
            }

            int farmers = tm.GetFarmRoleCount(StaffRole.Çiftçi);
            if (farmers < 2)
            {
                Add(into, "farm_tab_2");
                if (phone.ActiveFarmTab == 2) Add(into, "hire_farmer");
                return;
            }
            Add(into, "farm_tab_3");
            if (phone.ActiveFarmTab == 3) Add(into, "farm_shift");
        }

        private void ResolveCoachLine(out string key, out string tr, out string en)
        {
            TutorialManager tm = TutorialManager.Instance;
            key = "idle";
            tr = "İşte böyle evlat. Sol alttaki listeye bak, parlayan yere dokun.";
            en = "That's it, kid. Watch the list on the bottom-left and tap the bright spot.";
            if (tm == null) return;

            switch (tm.CurrentStep)
            {
                case TutorialStep.Step1_CameraControls:
                    key = "s1";
                    tr = "Önce haritayı tanı. Kaydır, yakınlaştır, döndür. Tabelandaki sloganı okuyana kadar gez — başka bir şeye bakmana gerek yok.";
                    en = "Learn the map first. Pan, zoom, rotate. Move until you can read the slogan on your sign — nothing else matters yet.";
                    break;
                case TutorialStep.Step2_ExploreTabletApps:
                    if (!TabletOpen)
                    {
                        key = "s2_phone";
                        tr = "Sağ alttaki parlayan EKT TABLET'e dokun. İşletmenin beyni orada.";
                        en = "Tap the glowing EKT TABLET at the bottom-right. That's HQ.";
                    }
                    else if (tm.ExploredAppsCount < 7)
                    {
                        key = "s2_apps";
                        tr = "Yedi uygulamayı tek tek aç: Mağaza, Çiftlik, Alışveriş, Finans, Sosyal, Atölye, Online Market. Yalnızca parlayan ikonlar açık.";
                        en = "Open all seven apps: Store, Farm, Shopping, Finance, Social, Workshop, Online Market. Only the glowing icons work.";
                    }
                    else
                    {
                        key = "s2_ctr";
                        tr = "Online Market'te Kontratlar sekmesine gir. Bir teklife Sıraya Al de. Motor yoksa bile sıraya almak yeter.";
                        en = "In Online Market open Contracts. Tap Queue It on one offer. No bike yet is fine — queuing is the lesson.";
                    }
                    break;
                case TutorialStep.Step3_HireStoreStaffAndCallEarly:
                    if (tm.GetStoreRoleCount(StaffRole.Kasiyer) < 2)
                    {
                        key = "s3_cash";
                        tr = "Mağaza → İşe Alım. Önce 2 kasiyer al. Yalnızca kasiyer kartı parlıyor.";
                        en = "Store → Hire. Take 2 cashiers first. Only the cashier card is lit.";
                    }
                    else if (tm.GetStoreRoleCount(StaffRole.Reyoncu) < 2)
                    {
                        key = "s3_rest";
                        tr = "Şimdi 2 reyoncu al. Reyoncu koliyi ve pasaportlu hasadı rafa taşır.";
                        en = "Now hire 2 restockers. They move boxes and passport harvest onto shelves.";
                    }
                    else
                    {
                        key = "s3_early";
                        tr = "Kadro sekmesine geç. Sabah vardiyalı bir reyoncuya Erken Çağır de.";
                        en = "Open Roster. Tap Call Early on a morning restocker.";
                    }
                    break;
                case TutorialStep.Step4_AssignStoreShifts:
                    key = "s4";
                    tr = "Mağaza → Vardiyalar. Birini sabaha (08–16), birini akşama (16–24) koy. Gece defteri boş vardiyayı affetmez.";
                    en = "Store → Shifts. Put someone on morning (08–16) and evening (16–24). The night ledger won't forgive empty shifts.";
                    break;
                case TutorialStep.Step5_BuyInitialFurniture:
                    key = "s5";
                    tr = "Alışveriş → Mobilyalar. 3 reyon, 1 sepet, 3 depo rafı, 1 kasa, 2 buzdolabı. Sepete koy, öde.";
                    en = "Shopping → Furniture. 3 shelves, 1 cart stand, 3 storage racks, 1 register, 2 fridges. Add and checkout.";
                    break;
                case TutorialStep.Step6_UnpackAndPlaceFurniture:
                    key = "s6";
                    tr = "Mal kabul yanındaki teslimat paletine git. Kutuya dokun, hayaleti sürükle, Kur. En az 8 parça. Yollara dokunma.";
                    en = "Go to the delivery pallet by Goods Receipt. Tap boxes, drag the ghost, Assemble. At least 8 pieces. Don't change the streets.";
                    break;
                case TutorialStep.Step7_PlaceWholesaleBulkOrder:
                    if (!tm.DidPlaceBulkOrder)
                    {
                        key = "s7_bulk";
                        tr = "Alışveriş'teki yeşil Toplu Sipariş'e bas. Mavi kamyon toptan getirir.";
                        en = "Tap green Bulk Order in Shopping. The blue truck brings wholesale lots.";
                    }
                    else
                    {
                        key = "s7_rows";
                        tr = "Bir reyonun ve bir buzdolabının 4 sırasına ürün ata. Rafta nereden geldiği yazar.";
                        en = "Assign products to all 4 rows of one shelf and one fridge. The shelf shows where it came from.";
                    }
                    break;
                case TutorialStep.Step8_HireFarmStaffAndShifts:
                    if (tm.GetFarmRoleCount(StaffRole.Çiftçi) < 2)
                    {
                        key = "s8_hire";
                        tr = "Çiftlik → İşe Alım. 2 çiftçi al. Onlar eker, hasat eder, pasaportu tarlaya işler.";
                        en = "Farm → Hire. Take 2 farmers. They plant, harvest, and stamp the plot on the passport.";
                    }
                    else
                    {
                        key = "s8_sh";
                        tr = "Çiftlik → Vardiyalar. Birini sabaha, birini akşama koy.";
                        en = "Farm → Shifts. Morning for one, evening for the other.";
                    }
                    break;
                case TutorialStep.Step9_BuyStartingSeeds:
                    key = "s9";
                    tr = "Alışveriş → Tohumlar. Domates, salatalık, marul — birer paket. Mevsim dışı ekilmez.";
                    en = "Shopping → Seeds. Tomato, cucumber, lettuce — one pack each. Out-of-season plots refuse them.";
                    break;
                case TutorialStep.Step10_PlantSeedsAndOpenStore:
                    key = "s10";
                    tr = "Sağdaki boş tarlaya dokun, ek. Sonra HUD'daki Dükkan Kapalı ile kapıyı aç. Markan hazır.";
                    en = "Tap an empty plot on the right and plant. Then tap Store Closed on the HUD to open. Your brand is ready.";
                    break;
            }
        }
    }
}
