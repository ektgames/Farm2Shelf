using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Ekranın sol alt köşesinde beliren, tatlı low-poly tarzında
    /// 10 adımlı interaktif Eğitim Görev Takip Paneli (Tutorial Quest Tracker).
    /// Yapılan her görev ve alt hedef için parlak yeşil TİK (✅ [✓]) işaretleri gösterir.
    /// Mobil & PC hibrit uyumlu, katlanabilir (Minimize/Expand) ve tam çift dillidir.
    /// </summary>
    public class TutorialQuestTrackerUI : MonoBehaviour
    {
        private static GameObject trackerInstance;
        private static bool isMinimized = false;
        private static bool showAllQuestsModal = false;

        public static void ShowTracker()
        {
            if (trackerInstance != null) Destroy(trackerInstance);
            lastRefreshKey = null;

            trackerInstance = new GameObject("Farm2Shelf_Tutorial_Tracker_Canvas");
            trackerInstance.AddComponent<TutorialQuestTrackerUI>();

            Canvas canvas = trackerInstance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300; // HUD üstünde, modal altında

            CanvasScaler scaler = trackerInstance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            trackerInstance.AddComponent<GraphicRaycaster>();

            BuildTrackerBox(trackerInstance.transform);
        }

        public static void HideTracker()
        {
            if (trackerInstance != null)
            {
                Destroy(trackerInstance);
                trackerInstance = null;
                lastRefreshKey = null;
            }
        }

        private static string lastRefreshKey;
        private static bool builtMinimized, builtComplete;

        public static void RefreshDisplay()
        {
            if (trackerInstance == null) return;

            TutorialManager tm = TutorialManager.Instance;
            string key = tm != null
                ? ((int)tm.CurrentStep) + "|" + GetStepLiveChecklist(tm.CurrentStep) + "|" + GetStepInstruction(tm.CurrentStep) + "|" + isMinimized
                : "";
            if (key == lastRefreshKey) return;
            lastRefreshKey = key;

            Transform oldCard = trackerInstance.transform.Find("TrackerCard");
            if (oldCard != null && tm != null && builtMinimized == isMinimized && builtComplete == tm.IsCurrentStepComplete())
            {
                oldCard.Find("Header/Txt").GetComponent<Text>().text =
                    $"<b>{LocalizationManager.L("Tut_QuestPrefix", "GÖREV", "QUEST")} {(int)tm.CurrentStep}/10:</b> <color=#00FFA3>{GetStepShortTitle(tm.CurrentStep)}</color>";
                if (!isMinimized)
                {
                    oldCard.Find("RoadmapStrip").GetComponent<Text>().text = Get10StepRoadmapString((int)tm.CurrentStep);
                    oldCard.Find("Desc/Content").GetComponent<Text>().text = GetStepInstruction(tm.CurrentStep);
                    oldCard.Find("ProgressBox/Content").GetComponent<Text>().text = GetStepLiveChecklist(tm.CurrentStep);
                    LayoutChecklist(oldCard, (int)tm.CurrentStep == 5);
                }
                return;
            }
            if (oldCard != null) { oldCard.gameObject.SetActive(false); Destroy(oldCard.gameObject); }

            Transform oldModal = trackerInstance.transform.Find("AllQuestsRoadmapModal");
            if (oldModal != null) Destroy(oldModal.gameObject);

            BuildTrackerBox(trackerInstance.transform);
        }

        private static void FitLabel(Text txt, int size, bool wrap = false)
        {
            if (txt == null) return;
            txt.fontSize = size;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = Mathf.Max(13, size - 6);
            txt.resizeTextMaxSize = size;
            txt.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static float GetSafeLeftMargin()
        {
            float safeLeft = 120f; // iPhone kamera çentiği / Dynamic Island sol boşluğu
            if (Screen.safeArea.x > 0 && Screen.width > 0)
            {
                float canvasScale = 1920f / Screen.width;
                safeLeft = Mathf.Max(120f, Screen.safeArea.x * canvasScale + 25f);
            }
            return safeLeft;
        }

        private void Update()
        {
            if (trackerInstance != null)
            {
                float targetLeft = GetSafeLeftMargin();
                Transform card = trackerInstance.transform.Find("TrackerCard");
                if (card != null)
                {
                    RectTransform rt = card.GetComponent<RectTransform>();
                    if (rt != null && Mathf.Abs(rt.anchoredPosition.x - targetLeft) > 1f)
                    {
                        rt.anchoredPosition = new Vector2(targetLeft, rt.anchoredPosition.y);
                    }
                }

                Transform modal = trackerInstance.transform.Find("AllQuestsRoadmapModal");
                if (modal != null)
                {
                    RectTransform mrt = modal.GetComponent<RectTransform>();
                    if (mrt != null && Mathf.Abs(mrt.anchoredPosition.x - targetLeft) > 1f)
                    {
                        mrt.anchoredPosition = new Vector2(targetLeft, mrt.anchoredPosition.y);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (trackerInstance == null || !TutorialMentorGuideUI.IsGuideOpen) return;
            var card = trackerInstance.transform.Find("TrackerCard") as RectTransform;
            if (card == null) return;
            Rect area = TutorialMentorGuideUI.TutorialBodyArea;
            float canvasScale = trackerInstance.GetComponent<Canvas>().scaleFactor;
            float unit = Mathf.Min(canvasScale, area.width * .37f / 640f, area.height / 520f);
            card.localScale = Vector3.one * (unit / Mathf.Max(.01f, canvasScale));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(card.parent as RectTransform,
                new Vector2(area.xMin, area.yMax - card.rect.height * unit), null, out Vector2 local);
            card.localPosition = new Vector3(local.x, local.y, 0f);
        }

        private static void BuildTrackerBox(Transform parent)
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive) return;
            builtMinimized = isMinimized;
            builtComplete = TutorialManager.Instance.IsCurrentStepComplete();

            TutorialStep step = TutorialManager.Instance.CurrentStep;
            int stepNum = (int)step;

            Font font = UIStyleUtility.GetGlobalFont(22);

            float cardW = 640f;
            float cardH = isMinimized ? 70f : 520f;
            float safeLeft = GetSafeLeftMargin();

            GameObject cardObj = new GameObject("TrackerCard");
            cardObj.transform.SetParent(parent, false);

            RectTransform cRect = cardObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 0f);
            cRect.anchorMax = new Vector2(0f, 0f);
            cRect.pivot = new Vector2(0f, 0f);
            cRect.anchoredPosition = new Vector2(safeLeft, 22f);
            cRect.sizeDelta = new Vector2(cardW, cardH);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(Mathf.RoundToInt(cardW), Mathf.RoundToInt(cardH), 22, 3, new Color(0.20f, 0.85f, 0.55f, 0.95f), new Color(0.10f, 0.13f, 0.18f, 0.96f));

            // 1. Üst Başlık Şeridi
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = new Vector2(0f, -8f);
            hRect.sizeDelta = new Vector2(-16f, 50f);

            Image hBg = headerObj.AddComponent<Image>();
            hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(Mathf.RoundToInt(cardW - 16f), 50, 12, new Color(0.14f, 0.20f, 0.28f, 0.95f));

            // Başlık Yazısı
            GameObject hTxtObj = new GameObject("Txt");
            hTxtObj.transform.SetParent(headerObj.transform, false);
            RectTransform htRect = hTxtObj.AddComponent<RectTransform>();
            htRect.anchorMin = Vector2.zero;
            htRect.anchorMax = Vector2.one;
            htRect.offsetMin = new Vector2(12f, 2f);
            htRect.offsetMax = new Vector2(-52f, -2f);

            Text hTxt = hTxtObj.AddComponent<Text>();
            hTxt.font = font;
            string stepTitle = GetStepShortTitle(step);
            hTxt.text = $"🎓 <b>{LocalizationManager.L("Tut_QuestPrefix", "GÖREV", "QUEST")} {stepNum}/10:</b> <color=#00FFA3>{stepTitle}</color>";
            FitLabel(hTxt, 26, true);
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleLeft;
            hTxt.color = Color.white;

            // Küçültme / Büyütme Butonu (Minimize/Expand)
            GameObject minBtnObj = new GameObject("MinBtn");
            minBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform mbRect = minBtnObj.AddComponent<RectTransform>();
            mbRect.anchorMin = new Vector2(1f, 0.5f);
            mbRect.anchorMax = new Vector2(1f, 0.5f);
            mbRect.pivot = new Vector2(1f, 0.5f);
            mbRect.anchoredPosition = new Vector2(-6f, 0f);
            mbRect.sizeDelta = new Vector2(42f, 36f);

            Image mbBg = minBtnObj.AddComponent<Image>();
            mbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(42, 36, 8, new Color(0.25f, 0.32f, 0.42f, 0.90f));

            Button minBtn = minBtnObj.AddComponent<Button>();
            minBtn.targetGraphic = mbBg;
            minBtn.onClick.AddListener(() => {
                isMinimized = !isMinimized;
                RefreshDisplay();
            });

            GameObject mbTxtObj = new GameObject("Txt");
            mbTxtObj.transform.SetParent(minBtnObj.transform, false);
            RectTransform mbtRect = mbTxtObj.AddComponent<RectTransform>();
            mbtRect.anchorMin = Vector2.zero;
            mbtRect.anchorMax = Vector2.one;

            Text mbTxt = mbTxtObj.AddComponent<Text>();
            mbTxt.font = font;
            mbTxt.text = isMinimized ? "▲" : "▼";
            FitLabel(mbTxt, 20);
            mbTxt.fontStyle = FontStyle.Bold;
            mbTxt.alignment = TextAnchor.MiddleCenter;
            mbTxt.color = Color.white;

            if (isMinimized) return;

            // 1.5. 10 Görev İlerleme Şeridi (Roadmap Strip)
            GameObject stripObj = new GameObject("RoadmapStrip");
            stripObj.transform.SetParent(cardObj.transform, false);
            RectTransform sRect = stripObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 1f);
            sRect.anchorMax = new Vector2(1f, 1f);
            sRect.pivot = new Vector2(0.5f, 1f);
            sRect.anchoredPosition = new Vector2(0f, -64f);
            sRect.sizeDelta = new Vector2(-20f, 34f);

            Text sTxt = stripObj.AddComponent<Text>();
            sTxt.font = font;
            sTxt.text = Get10StepRoadmapString(stepNum);
            FitLabel(sTxt, 21);
            sTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            sTxt.verticalOverflow = VerticalWrapMode.Truncate;
            sTxt.fontStyle = FontStyle.Bold;
            sTxt.alignment = TextAnchor.MiddleCenter;
            sTxt.color = Color.white;

            // 2. Açıklama & Talimat Metni
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(cardObj.transform, false);
            RectTransform dRect = descObj.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0f, 1f);
            dRect.anchorMax = new Vector2(1f, 1f);
            dRect.pivot = new Vector2(0.5f, 1f);
            dRect.anchoredPosition = new Vector2(0f, -104f);
            dRect.sizeDelta = new Vector2(-24f, 158f);

            Text dTxt = CreateScrollableText(dRect, font, 25);
            dTxt.font = font;
            dTxt.text = GetStepInstruction(step);
            dTxt.fontSize = 25;
            dTxt.lineSpacing = 1.12f;
            dTxt.alignment = TextAnchor.UpperLeft;
            dTxt.color = new Color(0.92f, 0.94f, 0.98f);
            dTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            dTxt.verticalOverflow = VerticalWrapMode.Truncate;
            dTxt.supportRichText = true;

            // 3. Canlı İlerleme & Kontrol Kutusu (Live Progress Checklist with Tikler)
            GameObject progObj = new GameObject("ProgressBox");
            progObj.transform.SetParent(cardObj.transform, false);
            RectTransform pRect = progObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0f, 1f);
            pRect.anchorMax = new Vector2(1f, 1f);
            pRect.pivot = new Vector2(0.5f, 1f);
            pRect.anchoredPosition = new Vector2(0f, -272f);
            pRect.sizeDelta = new Vector2(-24f, 158f);

            Image pBg = progObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(Mathf.RoundToInt(cardW - 24f), 158, 12, 1, new Color(0.30f, 0.40f, 0.52f, 0.6f), new Color(0.12f, 0.16f, 0.22f, 0.90f));

            Text pTxt = CreateScrollableText(pRect, font, 24);
            pTxt.font = font;
            pTxt.text = GetStepLiveChecklist(step);
            pTxt.fontSize = 24;
            pTxt.lineSpacing = 1.14f;
            pTxt.alignment = TextAnchor.UpperLeft;
            pTxt.color = new Color(0.96f, 0.96f, 0.96f);
            pTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            pTxt.verticalOverflow = VerticalWrapMode.Truncate;
            pTxt.supportRichText = true;
            LayoutChecklist(cardObj.transform, stepNum == 5);

            // 4. Alt Butonlar (Devam Et & Eğitimi Geç)
            bool isStepDone = (TutorialManager.Instance != null && TutorialManager.Instance.IsCurrentStepComplete());

            if (isStepDone)
            {
                CreateActionButton(cardObj.transform, new Vector2(-118f, 30f), new Vector2(220f, 50f),
                    LocalizationManager.L("Tut_BtnComplete", "Görevi Tamamla ▶", "Complete Quest ▶"),
                    LocalizationManager.L("Tut_BtnComplete", "Görevi Tamamla ▶", "Complete Quest ▶"),
                    new Color(0.12f, 0.85f, 0.45f), font, 18, () => {
                    TutorialManager.Instance.AdvanceToNextStep();
                });
            }

            CreateActionButton(cardObj.transform, new Vector2(isStepDone ? 118f : 0f, 30f), new Vector2(220f, 50f),
                LocalizationManager.L("Tut_BtnSkip", "Eğitimi Atla ⏭️", "Skip Tutorial ⏭️"),
                LocalizationManager.L("Tut_BtnSkip", "Eğitimi Atla ⏭️", "Skip Tutorial ⏭️"),
                new Color(0.35f, 0.40f, 0.48f), font, 18, () => {
                TutorialManager.Instance.RequestSkipTutorial();
            });

            // 5. Eğer "Tüm Görevler" açık ise detaylı liste kartını göster
            if (showAllQuestsModal)
            {
                BuildAllQuestsRoadmapModal(parent, stepNum, font);
            }
        }

        private static void LayoutChecklist(Transform card, bool furniture)
        {
            var description = card.Find("Desc") as RectTransform;
            var progress = card.Find("ProgressBox") as RectTransform;
            if (description == null || progress == null) return;
            description.sizeDelta = new Vector2(-24f, furniture ? 80f : 158f);
            progress.anchoredPosition = new Vector2(0f, furniture ? -194f : -272f);
            progress.sizeDelta = new Vector2(-24f, furniture ? 258f : 158f);
            var label = progress.Find("Content").GetComponent<Text>();
            label.fontSize = furniture ? 21 : 24;
            label.lineSpacing = furniture ? 1f : 1.14f;
        }

        private static Text CreateScrollableText(RectTransform viewport, Font font, int size)
        {
            if (viewport.GetComponent<Image>() == null)
                viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .001f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f; scroll.viewport = viewport;
            var content = new GameObject("Content", typeof(RectTransform), typeof(Text));
            var rect = content.GetComponent<RectTransform>(); rect.SetParent(viewport, false);
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(12, 0); rect.offsetMax = new Vector2(-12, -6);
            var text = content.GetComponent<Text>(); text.font = font; text.fontSize = size;
            text.resizeTextForBestFit = false; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = rect;
            return text;
        }

        private static string Get10StepRoadmapString(int currentStepNum)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i <= 10; i++)
            {
                if (i < currentStepNum)
                {
                    sb.Append($"<color=#00FFA3>G{i}</color>");
                }
                else if (i == currentStepNum)
                {
                    sb.Append($"<color=#FFD700><b>G{i}</b></color>");
                }
                else
                {
                    sb.Append($"<color=#707E8E>G{i}</color>");
                }

                if (i < 10) sb.Append("  ");
            }
            return sb.ToString();
        }

        private static void BuildAllQuestsRoadmapModal(Transform parent, int currentStepNum, Font font)
        {
            GameObject modalObj = new GameObject("AllQuestsRoadmapModal");
            modalObj.transform.SetParent(parent, false);

            float safeLeft = GetSafeLeftMargin();
            RectTransform mRect = modalObj.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0f, 0f);
            mRect.anchorMax = new Vector2(0f, 0f);
            mRect.pivot = new Vector2(0f, 0f);
            mRect.anchoredPosition = new Vector2(safeLeft, 460f);
            mRect.sizeDelta = new Vector2(620f, 430f);

            Image mBg = modalObj.AddComponent<Image>();
            mBg.sprite = UIStyleUtility.CreateOutlinePillSprite(620, 430, 16, 2, new Color(0.25f, 0.85f, 0.55f), new Color(0.08f, 0.11f, 0.15f, 0.98f));

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -8f);
            tRect.sizeDelta = new Vector2(-20f, 44f);

            Text tTxt = titleObj.AddComponent<Text>();
            tTxt.font = font;
            tTxt.text = "📋 " + LocalizationManager.L("Tut_AllQuestsTitle", "TÜM EĞİTİM GÖREVLERİ & İLERLEME", "ALL TUTORIAL QUESTS & PROGRESS");
            FitLabel(tTxt, 20, true);
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = new Color(0.30f, 0.95f, 0.65f);

            // 10 Görev Listesi
            GameObject listObj = new GameObject("List");
            listObj.transform.SetParent(modalObj.transform, false);
            RectTransform lRect = listObj.AddComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = new Vector2(16f, 12f);
            lRect.offsetMax = new Vector2(-16f, -52f);

            Text lTxt = listObj.AddComponent<Text>();
            lTxt.font = font;
            FitLabel(lTxt, 17, true);
            lTxt.lineSpacing = 1.22f;

            StringBuilder sb = new StringBuilder();
            for (int i = 1; i <= 10; i++)
            {
                TutorialStep st = (TutorialStep)i;
                string title = GetStepShortTitle(st);

                if (i < currentStepNum)
                {
                    sb.AppendLine($"<color=#00FFA3>✅ <b>Görev {i}:</b> {title} (Tamamlandı)</color>");
                }
                else if (i == currentStepNum)
                {
                    sb.AppendLine($"<color=#FFD700>⏳ <b>Görev {i}:</b> {title} (Şu Anki Görev)</color>");
                }
                else
                {
                    sb.AppendLine($"<color=#7E8C9C>○ <b>Görev {i}:</b> {title}</color>");
                }
            }
            lTxt.text = sb.ToString();
        }

        private static void CreateActionButton(Transform parent, Vector2 pos, Vector2 size, string textTr, string textEn, Color color, Font font, int fontSize, Action onClick)
        {
            GameObject btnObj = new GameObject("Btn_" + textTr);
            btnObj.transform.SetParent(parent, false);
            RectTransform bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0f);
            bRect.anchorMax = new Vector2(0.5f, 0f);
            bRect.pivot = new Vector2(0.5f, 0.5f);
            bRect.anchoredPosition = pos;
            bRect.sizeDelta = size;

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), 12, color);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(8f, 4f);
            tRect.offsetMax = new Vector2(-8f, -4f);

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = LocalizationManager.L("TutBtn_" + textTr, textTr, textEn);
            FitLabel(txt, fontSize, true);
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        private static string Tick(bool done, string key, string tr, string en)
        {
            string label = LocalizationManager.L(key, tr, en);
            return done
                ? $"<color=#00FFA3>✅ [✓] {label}</color>"
                : $"<color=#FFD700>⏳ [ ] {label}</color>";
        }

        private static string GetStepShortTitle(TutorialStep step)
        {
            return TutorialManager.Instance != null ? TutorialManager.Instance.JourneyTitle((int)step) : "";
        }

        private static string GetStepInstruction(TutorialStep step)
        {
            return TutorialManager.Instance != null ? TutorialManager.Instance.JourneyInstruction((int)step) : "";
        }

        private static string GetStepLiveChecklist(TutorialStep step)
        {
            return TutorialManager.Instance != null ? TutorialManager.Instance.JourneyChecklist((int)step) : "";
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
            RefreshDisplay();
        }
    }
}
