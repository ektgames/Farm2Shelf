using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Belediye Başkanı rehberi: hikâye, konuşma balonu, Geç butonu ve klasik spotlight.
    /// Mevcut 10 görev mantığına dokunmaz; yalnızca yönlendirir.
    /// </summary>
    public partial class TutorialMentorGuideUI : MonoBehaviour
    {
        public const string MentorNameTr = "Belediye Başkanı";
        public const string MentorNameEn = "Mayor";

        private static TutorialMentorGuideUI instance;
        private GameObject root;
        private TutorialSpotlightOverlay spotlight;
        private Image portraitImg;
        private RectTransform portraitRect;
        private RectTransform mentorCard;
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
        private bool palletCameraFocused;
        private readonly List<RectTransform> focusBuf = new List<RectTransform>();

        public static bool IsGuideOpen => instance != null && instance.root != null && instance.root.activeSelf;
        public static RectTransform CompanionCard => IsGuideOpen ? instance.mentorCard : null;
        public static Rect TutorialBodyArea
        {
            get
            {
                Rect safe = Screen.safeArea;
                float gap = Mathf.Max(6f, safe.height * .015f);
                return Rect.MinMaxRect(safe.xMin + gap, safe.yMin + gap,
                    safe.xMax - gap, safe.yMax - safe.height * .25f - gap * 2f);
            }
        }

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
            SpeakForStep(true);
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
            dimCanvas.sortingOrder = 1120; // Above tablet, pallet and ordinary gameplay windows.
            CanvasScaler dimScaler = dimRoot.AddComponent<CanvasScaler>();
            dimScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            dimScaler.referenceResolution = new Vector2(1920, 1080);
            dimScaler.matchWidthOrHeight = 0.5f;
            dimRoot.AddComponent<GraphicRaycaster>();

            GameObject talkRoot = new GameObject("Mentor_TalkCanvas");
            talkRoot.transform.SetParent(root.transform, false);
            Canvas canvas = talkRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1140; // Mentor/Skip remain usable above the spotlight.
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
            spotlight = ovObj.AddComponent<TutorialSpotlightOverlay>();
            spotlight.Initialize();

            GameObject card = new GameObject("MentorCard");
            card.transform.SetParent(talkRoot.transform, false);
            RectTransform cardRt = card.AddComponent<RectTransform>();
            mentorCard = cardRt;
            cardRt.anchorMin = new Vector2(.5f, 1f);
            cardRt.anchorMax = new Vector2(.5f, 1f);
            cardRt.pivot = new Vector2(.5f, 1f);
            cardRt.anchoredPosition = new Vector2(-36f, 190f);
            cardRt.sizeDelta = new Vector2(680f, 330f);

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
            portraitRect.sizeDelta = new Vector2(126f, 158f);
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
            nRt.anchoredPosition = new Vector2(-12f, -10f);
            nRt.sizeDelta = new Vector2(-320f, 36f);
            nameText = nameObj.AddComponent<Text>();
            nameText.font = font;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = new Color(1f, 0.86f, 0.42f);
            nameText.raycastTarget = false;
            Fit(nameText, 26, true);

            GameObject speechObj = new GameObject("Speech");
            speechObj.transform.SetParent(card.transform, false);
            speechGroup = speechObj.AddComponent<CanvasGroup>();
            RectTransform sRt = speechObj.AddComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(148f, 14f);
            sRt.offsetMax = new Vector2(-192f, -48f);
            speechText = speechObj.AddComponent<Text>();
            speechText.font = font;
            speechText.alignment = TextAnchor.UpperLeft;
            speechText.color = new Color(0.94f, 0.96f, 0.98f);
            speechText.supportRichText = true;
            speechText.raycastTarget = false;
            Fit(speechText, 24, true);
            speechText.resizeTextMinSize = 20;

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
            Texture2D tex = Resources.Load<Texture2D>("Tutorial/mayor_portrait");
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
                nameText.text = LocalizationManager.L("Mayor_NameTitle", "Belediye Başkanı", "Mayor");
            }
            if (skipBtnText != null)
            {
                skipBtnText.text = LocalizationManager.L("Mayor_ContinueLine", "Devam ▶", "Continue ▶");
            }
        }

        private void BeginIntro()
        {
            introDone = true; SpeakForStep(true);
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

            if (journeyPan != null) return;
            if (TutorialManager.Instance != null && TutorialManager.Instance.JourneyStep == 9)
                TutorialManager.Instance.AcknowledgeTourStop();
            // Advance the current explanation without bypassing task requirements.
            lastSpeechKey = "";
            SpeakForStep(true);
        }

        private void Update()
        {
            if (root == null || !root.activeSelf) return;
            if (mentorCard != null)
            {
                mentorCard.localScale = Vector3.one;
                Canvas canvas = mentorCard.GetComponentInParent<Canvas>();
                float scale = canvas != null ? Mathf.Max(.01f, canvas.scaleFactor) : 1f;
                Rect safe = Screen.safeArea;
                float gap = Mathf.Max(6f, safe.height * .015f);
                mentorCard.sizeDelta = new Vector2((safe.width - gap * 2f) / scale, safe.height * .25f / scale);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(mentorCard.parent as RectTransform,
                    new Vector2(safe.center.x, safe.yMax - gap), null, out Vector2 local);
                mentorCard.localPosition = new Vector3(local.x, local.y, 0f);
                portraitRect.sizeDelta = new Vector2(126f, Mathf.Min(158f, mentorCard.rect.height - 20f));
            }

            bobTime += Time.unscaledDeltaTime;
            if (portraitRect != null)
            {
                float bob = Mathf.Sin(bobTime * 2.1f) * 5f;
                float talk = typing ? Mathf.Sin(bobTime * 14f) * 2.2f : 0f;
                portraitRect.anchoredPosition = new Vector2(74f, bob + talk);
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
                if (introDone) SpeakForStep(false);
            }
        }

        private void RefreshSpotlight()
        {
            if (spotlight == null) return;

            TutorialManager tutorial = TutorialManager.Instance;
            bool placingStep = tutorial != null && tutorial.JourneyStep == 5 && tutorial.FurnitureBought && !tutorial.FurnitureInstalled;
            if (TryJourneyWorldFocus()) return;
            if (!placingStep) palletCameraFocused = false;
            if (placingStep && !TabletOpen && !PalletStorageInventoryModalUI.IsModalOpen && !ModalManager.IsModalOpen &&
                (FurniturePlacementManager.Instance == null || !FurniturePlacementManager.Instance.IsPlacing) &&
                TryFocusDeliveryPallet()) return;

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
            Canvas ownerCanvas = rt.GetComponentInParent<Canvas>();
            Camera eventCamera = ownerCanvas != null && ownerCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? ownerCanvas.worldCamera : null;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < c.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(eventCamera, c[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            float xMin = min.x, yMin = min.y, xMax = max.x, yMax = max.y;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private bool TryFocusDeliveryPallet()
        {
            Camera camera = Camera.main;
            FurnitureDeliveryManager delivery = FurnitureDeliveryManager.Instance;
            if (camera == null || delivery == null || !delivery.TryGetTutorialPalletBounds(out Bounds bounds)) return false;

            Rect visible = Screen.safeArea;
            Vector3 center = camera.WorldToScreenPoint(bounds.center);
            if (!palletCameraFocused)
            {
                palletCameraFocused = true;
                // Keep an already visible pallet in place; recover off-screen targets once.
                if ((center.z <= camera.nearClipPlane || !visible.Contains(new Vector2(center.x, center.y))) &&
                    Farm2Shelf.CameraSystem.IsometricCameraSetup.Instance != null)
                    Farm2Shelf.CameraSystem.IsometricCameraSetup.Instance.FocusOn(bounds.center, false);
            }

            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = camera.WorldToScreenPoint(new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z));
                if (point.z <= camera.nearClipPlane) return false;
                min = Vector2.Min(min, new Vector2(point.x, point.y));
                max = Vector2.Max(max, new Vector2(point.x, point.y));
            }
            float padding = Mathf.Clamp(Screen.height * .018f, 10f, 28f);
            Rect hole = Inflate(Rect.MinMaxRect(min.x, min.y, max.x, max.y), padding);
            if (!hole.Overlaps(new Rect(0, 0, Screen.width, Screen.height))) return false;
            spotlight.SetDimStrength(.86f);
            spotlight.SetTarget(hole, true, false);
            return true;
        }

        private static Rect Inflate(Rect r, float pad)
        {
            return Rect.MinMaxRect(r.xMin - pad, r.yMin - pad, r.xMax + pad, r.yMax + pad);
        }

        private void CollectFocus(List<RectTransform> into, ref bool world)
        {
            CollectJourneyFocus(into, ref world);
        }

        private static void Add(List<RectTransform> into, string id)
        {
            if (into.Count > 0) return;
            // Every guided control may be inside a scroll view, not only recruitment cards.
            FocusRecruitment(into, id);
        }

        private static bool TabletOpen => EKTPhoneManager.IsTabletOpen;

        private static void FocusRecruitment(List<RectTransform> into, string id)
        {
            RectTransform target = TutorialFocusTarget.FindRect(id);
            if (target == null) return;
            ScrollRect scroll = target.GetComponentInParent<ScrollRect>();
            if (scroll != null && scroll.content != null && scroll.vertical && target.IsChildOf(scroll.content))
            {
                Canvas.ForceUpdateCanvases();
                RectTransform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
                if (viewport != null)
                {
                    Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
                    Bounds contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, scroll.content);
                    float hidden = contentBounds.size.y - viewport.rect.height;
                    float delta = 0f;
                    if (bounds.min.y < viewport.rect.yMin + 12f)
                        delta = viewport.rect.yMin + 12f - bounds.min.y;
                    else if (bounds.max.y > viewport.rect.yMax - 12f)
                        delta = viewport.rect.yMax - 12f - bounds.max.y;
                    if (hidden > .1f && Mathf.Abs(delta) > .5f)
                    {
                        scroll.StopMovement();
                        float destination = Mathf.Clamp01(scroll.verticalNormalizedPosition - delta / hidden);
                        scroll.verticalNormalizedPosition = Mathf.MoveTowards(scroll.verticalNormalizedPosition, destination, .24f);
                    }
                    // While the target is still clipped, allow dragging the visible list.
                    bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
                    if (bounds.min.y < viewport.rect.yMin - .5f || bounds.max.y > viewport.rect.yMax + .5f)
                    {
                        into.Add(viewport);
                        return;
                    }
                }
            }
            into.Add(target);
        }

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
                if (cash + phone.GetPendingRecruitmentCount(0, StaffRole.Kasiyer) < 2)
                    FocusRecruitment(into, "hire_cashier");
                else if (rest + phone.GetPendingRecruitmentCount(0, StaffRole.Reyoncu) < 2)
                    FocusRecruitment(into, "hire_restocker");
                else
                    FocusRecruitment(into, "hire_confirm_0");
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
                FocusRecruitment(into, farmers + phone.GetPendingRecruitmentCount(1, StaffRole.Çiftçi) < 2
                    ? "hire_role_Çiftçi" : "hire_confirm_1");
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
            var tm = TutorialManager.Instance;
            key = tm != null ? "journey_" + tm.JourneyStep + "_" + tm.TourStop : "idle";
            if (tm != null && tm.JourneyStep == 7 && tm.SeedsPlanted && !tm.PlantingFinished) key += "_finish_planting";
            tr = en = tm != null ? (tm.JourneyStep == 9 ? TourSpeech(tm.TourStop) : tm.JourneyInstruction(tm.JourneyStep)) : "";
        }
    }
}
