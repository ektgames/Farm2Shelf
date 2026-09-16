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
        private TutorialSpotlightOverlay spotlight;
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
            instance.RefreshSpotlight();
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
            RefreshSpotlight();
            if (!introDone) return;
            SpeakForStep(true);
        }

        private void HandleProgress()
        {
            RefreshSpotlight();
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
            dimCanvas.sortingOrder = 920;
            CanvasScaler dimScaler = dimRoot.AddComponent<CanvasScaler>();
            dimScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            dimScaler.referenceResolution = new Vector2(1920, 1080);
            dimScaler.matchWidthOrHeight = 0.5f;
            dimRoot.AddComponent<GraphicRaycaster>();

            GameObject talkRoot = new GameObject("Mentor_TalkCanvas");
            talkRoot.transform.SetParent(root.transform, false);
            Canvas canvas = talkRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 980;
            CanvasScaler scaler = talkRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            talkRoot.AddComponent<GraphicRaycaster>();

            Font font = UIStyleUtility.GetGlobalFont(20);

            GameObject ovObj = new GameObject("Spotlight");
            ovObj.transform.SetParent(dimRoot.transform, false);
            RectTransform ovRt = ovObj.AddComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero;
            ovRt.anchorMax = Vector2.one;
            ovRt.offsetMin = Vector2.zero;
            ovRt.offsetMax = Vector2.zero;
            ovObj.AddComponent<Image>();
            spotlight = ovObj.AddComponent<TutorialSpotlightOverlay>();
            Shader holeShader = Shader.Find("Farm2Shelf/UI/TutorialSpotlight");
            if (holeShader != null)
            {
                spotlight.Initialize(new Material(holeShader));
            }

            GameObject card = new GameObject("MentorCard");
            card.transform.SetParent(talkRoot.transform, false);
            RectTransform cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(1f, 0f);
            cardRt.anchorMax = new Vector2(1f, 0f);
            cardRt.pivot = new Vector2(1f, 0f);
            cardRt.anchoredPosition = new Vector2(-36f, 228f);
            cardRt.sizeDelta = new Vector2(620f, 268f);

            Image cardBg = card.AddComponent<Image>();
            cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(620, 268, 22, 3, new Color(0.95f, 0.78f, 0.32f), new Color(0.09f, 0.11f, 0.15f, 0.96f));
            cardBg.raycastTarget = false;

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
            if (focusTimer >= 0.08f)
            {
                focusTimer = 0f;
                RefreshSpotlight();
            }
        }

        private void RefreshSpotlight()
        {
            if (spotlight == null) return;

            focusBuf.Clear();
            bool world = false;
            CollectFocus(focusBuf, ref world);

            if (world || focusBuf.Count == 0)
            {
                spotlight.SetDimStrength(world ? 0.42f : 0.62f);
                spotlight.SetTarget(new Rect(0f, 0f, Screen.width, Screen.height), false);
                return;
            }

            Rect hole = Inflate(ScreenRectOf(focusBuf[0]), 10f);
            if (hole.width < 8f || hole.height < 8f)
            {
                spotlight.SetDimStrength(0.62f);
                spotlight.SetTarget(hole, false);
                return;
            }

            spotlight.SetDimStrength(0.86f);
            spotlight.SetTarget(hole, true);
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

        private static Rect Inflate(Rect r, float pad)
        {
            return Rect.MinMaxRect(r.xMin - pad, r.yMin - pad, r.xMax + pad, r.yMax + pad);
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
            if (into.Count > 0) return;
            RectTransform rt = TutorialFocusTarget.FindRect(id);
            if (rt != null) into.Add(rt);
        }

        private static bool TabletOpen => EKTPhoneManager.IsTabletOpen;

        private void FocusTabletExplore(TutorialManager tm, EKTPhoneManager phone, List<RectTransform> into)
        {
            if (!TabletOpen)
            {
                Add(into, "phone");
                return;
            }

            if (phone != null && phone.IsHomeScreenVisible)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (!tm.IsAppExplored(i))
                    {
                        Add(into, "app_" + i);
                        return;
                    }
                }
                if (tm.ExploredAppsCount >= 7 && !tm.HasAcceptedTownContract())
                {
                    Add(into, "app_6");
                }
                return;
            }

            if (phone != null && phone.IsOnlineMarketVisible)
            {
                if (phone.ActiveOnlineMarketTab != 1)
                {
                    Add(into, "om_tab_1");
                    return;
                }
                Add(into, "om_contracts_list");
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

            int cash = tm.GetStoreRoleCount(StaffRole.Kasiyer);
            int rest = tm.GetStoreRoleCount(StaffRole.Reyoncu);
            if (cash < 2 || rest < 2)
            {
                if (phone.ActiveStoreTab != 2)
                {
                    Add(into, "store_tab_2");
                    return;
                }
                Add(into, cash < 2 ? "hire_cashier" : "hire_restocker");
                return;
            }

            if (phone.ActiveStoreTab != 3)
            {
                Add(into, "store_tab_3");
                return;
            }
            Add(into, "call_early");
            if (into.Count == 0) Add(into, "store_shift");
        }

        private void FocusStoreShifts(EKTPhoneManager phone, List<RectTransform> into)
        {
            if (!TabletOpen)
            {
                Add(into, "phone");
                return;
            }
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
            if (phone.ActiveStoreTab != 3)
            {
                Add(into, "store_tab_3");
                return;
            }
            Add(into, "store_shift");
        }

        private void FocusShopping(EKTPhoneManager phone, List<RectTransform> into, int category)
        {
            if (!TabletOpen)
            {
                Add(into, "phone");
                return;
            }
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
            if (phone.ActiveShoppingCategory != category)
            {
                Add(into, "shop_cat_" + category);
                return;
            }
            Add(into, "shop_content");
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
                if (phone.ActiveFarmTab != 2)
                {
                    Add(into, "farm_tab_2");
                    return;
                }
                Add(into, "hire_farmer");
                return;
            }
            if (phone.ActiveFarmTab != 3)
            {
                Add(into, "farm_tab_3");
                return;
            }
            Add(into, "farm_shift");
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
                    tr = "Önce haritayı tanı. Kaydır, yakınlaştır, döndür. Tabeladaki sloganı okuyana kadar gez — başka bir şeye bakmana gerek yok.";
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
                        tr = "Vardiyalar sekmesine geç. Sabah vardiyalı bir reyoncuya Erken Çağır de. Vardiya da burada ayarlanır.";
                        en = "Open Shifts. Tap Call Early on a morning restocker. Shifts are set on this tab too.";
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
