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
            }
        }

        public static void RefreshDisplay()
        {
            if (trackerInstance != null)
            {
                // Mevcut içeriği yeniden oluştur
                Transform oldCard = trackerInstance.transform.Find("TrackerCard");
                if (oldCard != null) Destroy(oldCard.gameObject);

                Transform oldModal = trackerInstance.transform.Find("AllQuestsRoadmapModal");
                if (oldModal != null) Destroy(oldModal.gameObject);

                BuildTrackerBox(trackerInstance.transform);
            }
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

        private static void BuildTrackerBox(Transform parent)
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive) return;

            TutorialStep step = TutorialManager.Instance.CurrentStep;
            int stepNum = (int)step;

            Font font = UIStyleUtility.GetGlobalFont(18);

            float cardW = 580f;
            float cardH = isMinimized ? 58f : 430f;
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
            hRect.sizeDelta = new Vector2(-16f, 42f);

            Image hBg = headerObj.AddComponent<Image>();
            hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(Mathf.RoundToInt(cardW - 16f), 42, 12, new Color(0.14f, 0.20f, 0.28f, 0.95f));

            // Başlık Yazısı
            GameObject hTxtObj = new GameObject("Txt");
            hTxtObj.transform.SetParent(headerObj.transform, false);
            RectTransform htRect = hTxtObj.AddComponent<RectTransform>();
            htRect.anchorMin = Vector2.zero;
            htRect.anchorMax = Vector2.one;
            htRect.offsetMin = new Vector2(14f, 0f);
            htRect.offsetMax = new Vector2(-48f, 0f);

            Text hTxt = hTxtObj.AddComponent<Text>();
            hTxt.font = font;
            string stepTitle = GetStepShortTitle(step);
            hTxt.text = $"🎓 <b>{LocalizationManager.L("Tut_QuestPrefix", "GÖREV", "QUEST")} {stepNum}/10:</b> <color=#00FFA3>{stepTitle}</color>";
            hTxt.fontSize = 17;
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
            mbRect.sizeDelta = new Vector2(36f, 32f);

            Image mbBg = minBtnObj.AddComponent<Image>();
            mbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(36, 32, 8, new Color(0.25f, 0.32f, 0.42f, 0.90f));

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
            mbTxt.fontSize = 14;
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
            sRect.anchoredPosition = new Vector2(0f, -54f);
            sRect.sizeDelta = new Vector2(-20f, 24f);

            Text sTxt = stripObj.AddComponent<Text>();
            sTxt.font = font;
            sTxt.text = Get10StepRoadmapString(stepNum);
            sTxt.fontSize = 13;
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
            dRect.anchoredPosition = new Vector2(0f, -82f);
            dRect.sizeDelta = new Vector2(-24f, 128f);

            Text dTxt = descObj.AddComponent<Text>();
            dTxt.font = font;
            dTxt.text = GetStepInstruction(step);
            dTxt.fontSize = 13;
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
            pRect.anchoredPosition = new Vector2(0f, -216f);
            pRect.sizeDelta = new Vector2(-24f, 128f);

            Image pBg = progObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(Mathf.RoundToInt(cardW - 24f), 128, 12, 1, new Color(0.30f, 0.40f, 0.52f, 0.6f), new Color(0.12f, 0.16f, 0.22f, 0.90f));

            GameObject pTxtObj = new GameObject("Txt");
            pTxtObj.transform.SetParent(progObj.transform, false);
            RectTransform ptRect = pTxtObj.AddComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero;
            ptRect.anchorMax = Vector2.one;
            ptRect.offsetMin = new Vector2(14f, 6f);
            ptRect.offsetMax = new Vector2(-14f, -6f);

            Text pTxt = pTxtObj.AddComponent<Text>();
            pTxt.font = font;
            pTxt.text = GetStepLiveChecklist(step);
            pTxt.fontSize = 13;
            pTxt.lineSpacing = 1.12f;
            pTxt.alignment = TextAnchor.UpperLeft;
            pTxt.color = new Color(0.96f, 0.96f, 0.96f);
            pTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            pTxt.verticalOverflow = VerticalWrapMode.Truncate;
            pTxt.supportRichText = true;

            // 4. Alt Butonlar (Devam Et & Eğitimi Geç)
            bool isStepDone = (TutorialManager.Instance != null && TutorialManager.Instance.IsCurrentStepComplete());

            if (isStepDone)
            {
                CreateActionButton(cardObj.transform, new Vector2(-95f, 25f), new Vector2(175f, 40f),
                    LocalizationManager.L("Tut_BtnComplete", "Görevi Tamamla ▶", "Complete Quest ▶"),
                    LocalizationManager.L("Tut_BtnComplete", "Görevi Tamamla ▶", "Complete Quest ▶"),
                    new Color(0.12f, 0.85f, 0.45f), font, 15, () => {
                    TutorialManager.Instance.AdvanceToNextStep();
                });
            }

            CreateActionButton(cardObj.transform, new Vector2(isStepDone ? 100f : 0f, 25f), new Vector2(175f, 40f),
                LocalizationManager.L("Tut_BtnSkip", "Eğitimi Atla ⏭️", "Skip Tutorial ⏭️"),
                LocalizationManager.L("Tut_BtnSkip", "Eğitimi Atla ⏭️", "Skip Tutorial ⏭️"),
                new Color(0.35f, 0.40f, 0.48f), font, 14, () => {
                TutorialManager.Instance.RequestSkipTutorial();
            });

            // 5. Eğer "Tüm Görevler" açık ise detaylı liste kartını göster
            if (showAllQuestsModal)
            {
                BuildAllQuestsRoadmapModal(parent, stepNum, font);
            }
        }

        private static string Get10StepRoadmapString(int currentStepNum)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i <= 10; i++)
            {
                if (i < currentStepNum)
                {
                    sb.Append($"<color=#00FFA3>✅ G{i}</color>");
                }
                else if (i == currentStepNum)
                {
                    sb.Append($"<color=#FFD700>▶ G{i}</color>");
                }
                else
                {
                    sb.Append($"<color=#707E8E>○ G{i}</color>");
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
            mRect.anchoredPosition = new Vector2(safeLeft, 380f);
            mRect.sizeDelta = new Vector2(560f, 380f);

            Image mBg = modalObj.AddComponent<Image>();
            mBg.sprite = UIStyleUtility.CreateOutlinePillSprite(560, 380, 16, 2, new Color(0.25f, 0.85f, 0.55f), new Color(0.08f, 0.11f, 0.15f, 0.98f));

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -8f);
            tRect.sizeDelta = new Vector2(-20f, 36f);

            Text tTxt = titleObj.AddComponent<Text>();
            tTxt.font = font;
            tTxt.text = "📋 " + LocalizationManager.L("Tut_AllQuestsTitle", "TÜM EĞİTİM GÖREVLERİ & İLERLEME", "ALL TUTORIAL QUESTS & PROGRESS");
            tTxt.fontSize = 16;
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
            lRect.offsetMax = new Vector2(-16f, -44f);

            Text lTxt = listObj.AddComponent<Text>();
            lTxt.font = font;
            lTxt.fontSize = 14;
            lTxt.lineSpacing = 1.18f;

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

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = LocalizationManager.L("TutBtn_" + textTr, textTr, textEn);
            txt.fontSize = fontSize;
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
            switch (step)
            {
                case TutorialStep.Step1_CameraControls:
                    return LocalizationManager.L("Tut_S1_Title", "Kamera ve tabelanı gör", "Camera and your sign");
                case TutorialStep.Step2_ExploreTabletApps:
                    return LocalizationManager.L("Tut_S2_Title", "EKT Tablet ve ilk kontrat", "EKT Tablet and first contract");
                case TutorialStep.Step3_HireStoreStaffAndCallEarly:
                    return LocalizationManager.L("Tut_S3_Title", "Personel al, reyoncuyu çağır", "Hire staff, call a restocker");
                case TutorialStep.Step4_AssignStoreShifts:
                    return LocalizationManager.L("Tut_S4_Title", "Vardiya ve gece defteri", "Shifts and the night ledger");
                case TutorialStep.Step5_BuyInitialFurniture:
                    return LocalizationManager.L("Tut_S5_Title", "İlk mobilyaları satın al", "Buy starting furniture");
                case TutorialStep.Step6_UnpackAndPlaceFurniture:
                    return LocalizationManager.L("Tut_S6_Title", "Reyonları kur, vitrini kur", "Place shelves, set the floor");
                case TutorialStep.Step7_PlaceWholesaleBulkOrder:
                    return LocalizationManager.L("Tut_S7_Title", "Toptan sipariş ve pasaport", "Wholesale and passports");
                case TutorialStep.Step8_HireFarmStaffAndShifts:
                    return LocalizationManager.L("Tut_S8_Title", "Çiftçi ve tarla vardiyası", "Farmers and field shifts");
                case TutorialStep.Step9_BuyStartingSeeds:
                    return LocalizationManager.L("Tut_S9_Title", "Tohum al, hasadı planla", "Buy seeds, plan harvest");
                case TutorialStep.Step10_PlantSeedsAndOpenStore:
                    return LocalizationManager.L("Tut_S10_Title", "Ek, aç, markanı yaşat", "Plant, open, run your brand");
                default:
                    return "";
            }
        }

        private static string GetStepInstruction(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.Step1_CameraControls:
                    return LocalizationManager.L(
                        "Tut_S1_Inst",
                        "Haritayı tanı: dükkan, otopark ve cadde tabelan yerinde duruyor. Yeni oyunda seçtiğin <b>marka rengi ve slogan</b> tabelada yazılı.\n" +
                        "• Kaydır (parmak / sol sürükle / WASD)\n" +
                        "• Yakınlaştır (kıstır veya tekerlek)\n" +
                        "• Döndür (iki parmak veya sağ sürükle)\n" +
                        "Tabelandaki sloganı bir kez net görene kadar gez.",
                        "Learn the map: store, parking, and your street sign stay put. The <b>brand color and slogan</b> you picked sit on the fascia.\n" +
                        "• Pan (finger / left-drag / WASD)\n" +
                        "• Zoom (pinch or wheel)\n" +
                        "• Rotate (two fingers or right-drag)\n" +
                        "Move until you can read the slogan on your sign."
                    );

                case TutorialStep.Step2_ExploreTabletApps:
                    return LocalizationManager.L(
                        "Tut_S2_Inst",
                        "Sağ alttaki <b>EKT TABLET</b> işletmenin beyni. Yedi uygulamayı tek tek aç:\n" +
                        "Mağaza • Çiftlik • Alışveriş • Finans • Sosyal • Atölye • Online Market.\n" +
                        "Online Market içinde <b>Kontratlar</b> sekmesine gir. Kasaba her gün 5 teklif sunar; birine <b>Sıraya Al</b> de. Motor yoksa teslimat bekler, sıraya almak yeter. Sosyal akışta markanın sloganı da görünür.",
                        "The <b>EKT TABLET</b> is HQ. Open all seven apps:\n" +
                        "Store • Farm • Shopping • Finance • Social • Workshop • Online Market.\n" +
                        "In Online Market open <b>Contracts</b>. Town posts 5 offers a day; tap <b>Queue It</b> on one. No bike yet is fine — queuing is the lesson. Your slogan also shows on the social profile."
                    );

                case TutorialStep.Step3_HireStoreStaffAndCallEarly:
                    return LocalizationManager.L(
                        "Tut_S3_Inst",
                        "Tablet ➔ <b>Mağaza Yönetimi ➔ İşe Alım</b>. <b>2 kasiyer</b> ve <b>2 reyoncu</b> al. Kasiyer kuyruğu eritir; reyoncu toptan kolisini ve <b>pasaportlu</b> hasadı rafa taşır.\n" +
                        "Sonra <b>Kadro</b>da sabah vardiyalı bir reyoncuya <b>Erken Çağır</b>. Kontrat kuryesi sonra Online Market'ten eklenir; şimdi dükkan ekibi yeterli.",
                        "Tablet ➔ <b>Store Mgmt ➔ Hire</b>. Hire <b>2 cashiers</b> and <b>2 restockers</b>. Cashiers clear the line; restockers move wholesale boxes and <b>passport</b> harvest onto shelves.\n" +
                        "Then in <b>Roster</b> tap <b>Call Early</b> on a morning restocker. Contract couriers come later in Online Market; store staff is enough now."
                    );

                case TutorialStep.Step4_AssignStoreShifts:
                    return LocalizationManager.L(
                        "Tut_S4_Inst",
                        "Tablet ➔ <b>Mağaza ➔ Vardiyalar</b>. Personeli <b>Sabah 08:00–16:00</b> ve <b>Akşam 16:00–24:00</b> diye böl. Saat 24:00'te dükkan kapanır; müşteri çıkınca <b>gün sonu defteri</b> açılır ve yalnızca Ertesi güne atla 06:00'ya götürür. Vardiya boşsa kasa ve raf gece yarısına dayanmaz.",
                        "Tablet ➔ <b>Store ➔ Shifts</b>. Split staff across <b>Morning 08:00–16:00</b> and <b>Evening 16:00–24:00</b>. At 24:00 the store locks; after customers leave the <b>end-of-day ledger</b> opens and only Skip to next day moves you to 06:00. Empty shifts will not last until midnight."
                    );

                case TutorialStep.Step5_BuyInitialFurniture:
                    return LocalizationManager.L(
                        "Tut_S5_Inst",
                        "Tablet ➔ <b>Alışveriş ➔ Mobilyalar</b>. Sepete koy ve öde:\n" +
                        "3 standart reyon, 1 sepet standı, 3 depo rafı, 1 kasa, 2 buzdolabı.\n" +
                        "Reyonlar pasaport etiketinin görüneceği yer; kasa fişte yerel/toptan yazar. Depo rafları yalnızca depoya kurulur.",
                        "Tablet ➔ <b>Shopping ➔ Furniture</b>. Add and checkout:\n" +
                        "3 display shelves, 1 cart stand, 3 storage racks, 1 register, 2 fridges.\n" +
                        "Shelves are where passport labels show; the receipt marks local vs wholesale. Storage racks belong in the warehouse only."
                    );

                case TutorialStep.Step6_UnpackAndPlaceFurniture:
                    return LocalizationManager.L(
                        "Tut_S6_Inst",
                        "Mal kabul yanındaki <b>teslimat paletine</b> git. Kutulara dokun, hayaleti sürükle, <b>Kur</b>. En az 8 parça: reyon ve kasa dükkana, metal raflar depoya. Harita yollarına dokunma. Cadde tabelanın rengi markan; vitrin ışığı da aynı tona çekilir.",
                        "Go to the <b>delivery pallet</b> by Goods Receipt. Tap boxes, drag the ghost, tap <b>Assemble</b>. Place at least 8 pieces: shelves and register in the store, metal racks in storage. Do not change the streets. Your sign color is your brand; interior light tints to match."
                    );

                case TutorialStep.Step7_PlaceWholesaleBulkOrder:
                    return LocalizationManager.L(
                        "Tut_S7_Inst",
                        "1. Alışveriş'te yeşil <b>Toplu Sipariş</b> ver. Mavi kamyon toptan getirir; bu lotların pasaportu zayıf kalır, fiyatı markana göre değişir.\n" +
                        "2. Bir standart reyonun ve bir buzdolabının <b>4 sırasına</b> ürün ata. Rafta <b>nereden geldiği</b> yazar. Taze hasat daha sonra prim yapar; bayat ürün şikayet tweet'i üretir.",
                        "1. In Shopping tap green <b>Bulk Order</b>. The blue truck brings wholesale lots with a weaker passport; your brand still changes their price.\n" +
                        "2. Assign products to all <b>4 rows</b> of one display shelf and one fridge. The shelf shows <b>where it came from</b>. Fresh harvest later earns a premium; stale lots spark complaint tweets."
                    );

                case TutorialStep.Step8_HireFarmStaffAndShifts:
                    return LocalizationManager.L(
                        "Tut_S8_Inst",
                        "Tablet ➔ <b>Çiftlik ➔ İşe Alım</b>: <b>2 çiftçi</b>. <b>Vardiyalar</b>da birini sabah, birini akşama koy. Çiftçi eker ve hasat eder; hasat lotuna tarla, gün, saat ve hava işlenir. Yerel üretici kimliği bu mahsule prim verir.",
                        "Tablet ➔ <b>Farm ➔ Hire</b>: <b>2 farmers</b>. In <b>Shifts</b> put one on morning, one on evening. Farmers plant and harvest; each lot stores plot, day, hour, and weather. A local-producer brand pays a premium on that crop."
                    );

                case TutorialStep.Step9_BuyStartingSeeds:
                    return LocalizationManager.L(
                        "Tut_S9_Inst",
                        "Alışveriş ➔ <b>Tohumlar</b>. Birer paket al: domates, salatalık, marul. Mevsim dışı ekilmez. Hasat ahıra gider; oradan markete sevk, anında sat veya atölye hammaddesi. Sevkte pasaport korunur.",
                        "Shopping ➔ <b>Seeds</b>. Buy one pack each: tomato, cucumber, lettuce. Out-of-season plots refuse them. Harvest lands in the barn; from there ship to store, sell now, or send to the workshop. Shipping keeps the passport."
                    );

                case TutorialStep.Step10_PlantSeedsAndOpenStore:
                    return LocalizationManager.L(
                        "Tut_S10_Inst",
                        "Sağdaki boş tarlaya dokun, tohum ek. Sonra HUD'daki <b>Dükkan Kapalı</b> ile aç. Müşteri pasaportlu fiyatı görür; gece defteri bugünü yazar. Kontrat motoru dükkan açıkken sıradaki işe çıkar. Markan hazır — kapıyı aç.",
                        "Tap an empty plot on the right and plant. Then tap HUD <b>Store Closed</b> to open. Shoppers see passport pricing; the night ledger writes today's story. Contract bikes only leave while you are open. Your brand is ready — open the door."
                    );

                default:
                    return "";
            }
        }

        private static string GetStepLiveChecklist(TutorialStep step)
        {
            var tm = TutorialManager.Instance;
            if (tm == null) return "";

            switch (step)
            {
                case TutorialStep.Step1_CameraControls:
                    return "• " + Tick(tm.DidPanCamera, "Tut_C1_Pan", "Haritayı kaydır", "Pan the map") +
                           "\n• " + Tick(tm.DidZoomCamera, "Tut_C1_Zoom", "Yakınlaştır / uzaklaştır", "Zoom in / out") +
                           "\n• " + Tick(tm.DidRotateCamera, "Tut_C1_Rot", "Kamerayı döndür", "Rotate the camera");

                case TutorialStep.Step2_ExploreTabletApps:
                    string apps = Tick(tm.IsAppExplored(0), "Tut_C2_A0", "🛒 Mağaza", "🛒 Store") + "  " +
                                  Tick(tm.IsAppExplored(1), "Tut_C2_A1", "🌾 Çiftlik", "🌾 Farm") + "  " +
                                  Tick(tm.IsAppExplored(2), "Tut_C2_A2", "🛍️ Alışveriş", "🛍️ Shop") + "\n" +
                                  Tick(tm.IsAppExplored(3), "Tut_C2_A3", "💳 Finans", "💳 Finance") + "  " +
                                  Tick(tm.IsAppExplored(4), "Tut_C2_A4", "𝕏 Sosyal", "𝕏 Social") + "  " +
                                  Tick(tm.IsAppExplored(5), "Tut_C2_A5", "🏭 Atölye", "🏭 Workshop") + "\n" +
                                  Tick(tm.IsAppExplored(6), "Tut_C2_A6", "🌐 Online Market", "🌐 Online Market");
                    string ctr = Tick(tm.HasAcceptedTownContract(), "Tut_C2_Ctr", "Bir kasaba kontratını sıraya al", "Queue one town contract");
                    return $"{apps}\n{ctr}";

                case TutorialStep.Step3_HireStoreStaffAndCallEarly:
                    int cash = tm.GetStoreRoleCount(StaffRole.Kasiyer);
                    int rest = tm.GetStoreRoleCount(StaffRole.Reyoncu);
                    return "• " + Tick(cash >= 2, "Tut_C3_Cash", $"2 kasiyer ({cash}/2)", $"2 cashiers ({cash}/2)") +
                           "\n• " + Tick(rest >= 2, "Tut_C3_Rest", $"2 reyoncu ({rest}/2)", $"2 restockers ({rest}/2)") +
                           "\n• " + Tick(tm.DidCallRestockerEarly, "Tut_C3_Early", "Reyoncuyu erken çağır", "Call a restocker early");

                case TutorialStep.Step4_AssignStoreShifts:
                    bool shMorn = tm.HasStoreShift("Sabah") || tm.HasStoreShift("Gündüz") || tm.HasStoreShift("08:00") || tm.HasStoreShift("Morning");
                    bool shEve = tm.HasStoreShift("Akşam") || tm.HasStoreShift("16:00 - 24:00") || tm.HasStoreShift("24:00") || tm.HasStoreShift("Gece") || tm.HasStoreShift("Evening");
                    return "• " + Tick(shMorn, "Tut_C4_M", "Sabah vardiyası 08:00–16:00", "Morning shift 08:00–16:00") +
                           "\n• " + Tick(shEve, "Tut_C4_E", "Akşam vardiyası 16:00–24:00", "Evening shift 16:00–24:00");

                case TutorialStep.Step5_BuyInitialFurniture:
                    int sh = tm.GetBoughtCount(FurnitureType.Shelf);
                    int cs = tm.GetBoughtCount(FurnitureType.ShoppingCart);
                    int st = tm.GetBoughtCount(FurnitureType.StorageShelf);
                    int ca = tm.GetBoughtCount(FurnitureType.Cashier);
                    int fr = tm.GetBoughtCount(FurnitureType.Fridge);
                    return "• " + Tick(sh >= 3, "Tut_C5_Sh", $"3 reyon ({sh}/3)", $"3 shelves ({sh}/3)") + "  " +
                           Tick(cs >= 1, "Tut_C5_Cs", $"1 sepet ({cs}/1)", $"1 cart stand ({cs}/1)") +
                           "\n• " + Tick(st >= 3, "Tut_C5_St", $"3 depo rafı ({st}/3)", $"3 storage racks ({st}/3)") + "  " +
                           Tick(ca >= 1, "Tut_C5_Ca", $"1 kasa ({ca}/1)", $"1 register ({ca}/1)") +
                           "\n• " + Tick(fr >= 2, "Tut_C5_Fr", $"2 buzdolabı ({fr}/2)", $"2 fridges ({fr}/2)");

                case TutorialStep.Step6_UnpackAndPlaceFurniture:
                    int placedFurnitureCount = PlacedFurnitureController.AllPlacedFurniture != null ? PlacedFurnitureController.AllPlacedFurniture.Count : 0;
                    int placed = Mathf.Max(tm.TotalFurniturePlacedInTutorial, placedFurnitureCount);
                    return "• " + Tick(placed >= 8, "Tut_C6_Pl", $"Mobilya kur ({placed}/8)", $"Place furniture ({placed}/8)");

                case TutorialStep.Step7_PlaceWholesaleBulkOrder:
                    int sRows = tm.GetMaxAssignedRowsOnAnyShelf();
                    int fRows = tm.GetMaxAssignedRowsOnAnyFridge();
                    return "• " + Tick(tm.DidPlaceBulkOrder, "Tut_C7_Bo", "Toplu sipariş ver", "Place a bulk order") +
                           "\n• " + Tick(sRows >= 4, "Tut_C7_Sh", $"Reyona 4 ürün ({sRows}/4)", $"4 shelf rows ({sRows}/4)") +
                           "\n• " + Tick(fRows >= 4, "Tut_C7_Fr", $"Dolaba 4 ürün ({fRows}/4)", $"4 fridge rows ({fRows}/4)");

                case TutorialStep.Step8_HireFarmStaffAndShifts:
                    int farm = tm.GetFarmRoleCount(StaffRole.Çiftçi);
                    bool fMorn = tm.HasFarmShift("Sabah") || tm.HasFarmShift("Gündüz") || tm.HasFarmShift("08:00") || tm.HasFarmShift("Morning");
                    bool fEve = tm.HasFarmShift("Akşam") || tm.HasFarmShift("16:00 - 24:00") || tm.HasFarmShift("24:00") || tm.HasFarmShift("Gece") || tm.HasFarmShift("Evening");
                    return "• " + Tick(farm >= 2, "Tut_C8_F", $"2 çiftçi ({farm}/2)", $"2 farmers ({farm}/2)") +
                           "\n• " + Tick(fMorn && fEve, "Tut_C8_Sh", "Sabah ve akşam vardiyası", "Morning and evening shifts");

                case TutorialStep.Step9_BuyStartingSeeds:
                    bool hasTomato = tm.DidBuyTomatoSeed || (GardenSeedInventoryManager.Instance != null && GardenSeedInventoryManager.Instance.GetSeedCount("spring_tomato") > 0);
                    bool hasCucumber = tm.DidBuyCucumberSeed || (GardenSeedInventoryManager.Instance != null && GardenSeedInventoryManager.Instance.GetSeedCount("spring_cucumber") > 0);
                    bool hasLettuce = tm.DidBuyLettuceSeed || (GardenSeedInventoryManager.Instance != null && GardenSeedInventoryManager.Instance.GetSeedCount("spring_lettuce") > 0);
                    return "• " + Tick(hasTomato, "Tut_C9_T", "Domates tohumu", "Tomato seeds") +
                           "\n• " + Tick(hasCucumber, "Tut_C9_C", "Salatalık tohumu", "Cucumber seeds") +
                           "\n• " + Tick(hasLettuce, "Tut_C9_L", "Marul tohumu", "Lettuce seeds");

                case TutorialStep.Step10_PlantSeedsAndOpenStore:
                    int plantedPlots = FieldPlotController.AllPlots != null ? FieldPlotController.AllPlots.FindAll(p => p != null && p.State != PlotState.Empty).Count : 0;
                    int cp = Mathf.Max(tm.CropsPlantedInTutorial, plantedPlots);
                    bool isStoreOpen = tm.DidOpenStoreInTutorial || (StoreStatusManager.Instance != null && StoreStatusManager.Instance.IsOpen);
                    return "• " + Tick(cp >= 1, "Tut_C10_P", $"Tarla ekimi ({cp})", $"Plant a plot ({cp})") +
                           "\n• " + Tick(isStoreOpen, "Tut_C10_O", "Dükkanı aç", "Open the store");

                default:
                    return "";
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
            RefreshDisplay();
        }
    }
}
