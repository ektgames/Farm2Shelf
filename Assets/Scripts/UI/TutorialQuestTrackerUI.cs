using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

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

        private static void BuildTrackerBox(Transform parent)
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive) return;

            TutorialStep step = TutorialManager.Instance.CurrentStep;
            int stepNum = (int)step;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            float cardW = 520f;
            float cardH = isMinimized ? 56f : 315f;

            GameObject cardObj = new GameObject("TrackerCard");
            cardObj.transform.SetParent(parent, false);

            RectTransform cRect = cardObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 0f);
            cRect.anchorMax = new Vector2(0f, 0f);
            cRect.pivot = new Vector2(0f, 0f);
            cRect.anchoredPosition = new Vector2(30f, 30f);
            cRect.sizeDelta = new Vector2(cardW, cardH);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(Mathf.RoundToInt(cardW), Mathf.RoundToInt(cardH), 20, 3, new Color(0.20f, 0.85f, 0.55f, 0.95f), new Color(0.10f, 0.13f, 0.18f, 0.96f));

            // 1. Üst Başlık Şeridi
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = new Vector2(0f, -8f);
            hRect.sizeDelta = new Vector2(-16f, 40f);

            Image hBg = headerObj.AddComponent<Image>();
            hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(Mathf.RoundToInt(cardW - 16f), 40, 12, new Color(0.14f, 0.20f, 0.28f, 0.95f));

            // Başlık Yazısı
            GameObject hTxtObj = new GameObject("Txt");
            hTxtObj.transform.SetParent(headerObj.transform, false);
            RectTransform htRect = hTxtObj.AddComponent<RectTransform>();
            htRect.anchorMin = Vector2.zero;
            htRect.anchorMax = Vector2.one;
            htRect.offsetMin = new Vector2(14f, 0f);
            htRect.offsetMax = new Vector2(-90f, 0f);

            Text hTxt = hTxtObj.AddComponent<Text>();
            hTxt.font = font;
            string stepTitle = GetStepShortTitle(step);
            hTxt.text = $"🎓 <b>{LocalizationManager.L("Tut_QuestPrefix", "GÖREV", "QUEST")} {stepNum}/10:</b> <color=#00FFA3>{stepTitle}</color>";
            hTxt.fontSize = 15;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleLeft;
            hTxt.color = Color.white;

            // 📋 Tüm Görevler Butonu
            GameObject allBtnObj = new GameObject("AllQuestsBtn");
            allBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform abRect = allBtnObj.AddComponent<RectTransform>();
            abRect.anchorMin = new Vector2(1f, 0.5f);
            abRect.anchorMax = new Vector2(1f, 0.5f);
            abRect.pivot = new Vector2(1f, 0.5f);
            abRect.anchoredPosition = new Vector2(-42f, 0f);
            abRect.sizeDelta = new Vector2(34f, 30f);

            Image abBg = allBtnObj.AddComponent<Image>();
            abBg.sprite = UIStyleUtility.CreateRoundedPillSprite(34, 30, 8, new Color(0.18f, 0.45f, 0.35f, 0.90f));

            Button allBtn = allBtnObj.AddComponent<Button>();
            allBtn.targetGraphic = abBg;
            allBtn.onClick.AddListener(() => {
                showAllQuestsModal = !showAllQuestsModal;
                RefreshDisplay();
            });

            GameObject abTxtObj = new GameObject("Txt");
            abTxtObj.transform.SetParent(allBtnObj.transform, false);
            RectTransform abtRect = abTxtObj.AddComponent<RectTransform>();
            abtRect.anchorMin = Vector2.zero;
            abtRect.anchorMax = Vector2.one;

            Text abTxt = abTxtObj.AddComponent<Text>();
            abTxt.font = font;
            abTxt.text = "📋";
            abTxt.fontSize = 15;
            abTxt.alignment = TextAnchor.MiddleCenter;

            // Küçültme / Büyütme Butonu (Minimize/Expand)
            GameObject minBtnObj = new GameObject("MinBtn");
            minBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform mbRect = minBtnObj.AddComponent<RectTransform>();
            mbRect.anchorMin = new Vector2(1f, 0.5f);
            mbRect.anchorMax = new Vector2(1f, 0.5f);
            mbRect.pivot = new Vector2(1f, 0.5f);
            mbRect.anchoredPosition = new Vector2(-6f, 0f);
            mbRect.sizeDelta = new Vector2(34f, 30f);

            Image mbBg = minBtnObj.AddComponent<Image>();
            mbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(34, 30, 8, new Color(0.25f, 0.32f, 0.42f, 0.90f));

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
            mbTxt.fontSize = 13;
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
            sRect.anchoredPosition = new Vector2(0f, -50f);
            sRect.sizeDelta = new Vector2(-20f, 22f);

            Text sTxt = stripObj.AddComponent<Text>();
            sTxt.font = font;
            sTxt.text = Get10StepRoadmapString(stepNum);
            sTxt.fontSize = 11;
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
            dRect.anchoredPosition = new Vector2(0f, -74f);
            dRect.sizeDelta = new Vector2(-24f, 85f);

            Text dTxt = descObj.AddComponent<Text>();
            dTxt.font = font;
            dTxt.text = GetStepInstruction(step);
            dTxt.fontSize = 12;
            dTxt.lineSpacing = 1.15f;
            dTxt.alignment = TextAnchor.UpperLeft;
            dTxt.color = new Color(0.90f, 0.92f, 0.96f);

            // 3. Canlı İlerleme & Kontrol Kutusu (Live Progress Checklist with Tikler)
            GameObject progObj = new GameObject("ProgressBox");
            progObj.transform.SetParent(cardObj.transform, false);
            RectTransform pRect = progObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0f, 1f);
            pRect.anchorMax = new Vector2(1f, 1f);
            pRect.pivot = new Vector2(0.5f, 1f);
            pRect.anchoredPosition = new Vector2(0f, -165f);
            pRect.sizeDelta = new Vector2(-24f, 95f);

            Image pBg = progObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(Mathf.RoundToInt(cardW - 24f), 95, 10, 1, new Color(0.30f, 0.40f, 0.52f, 0.6f), new Color(0.12f, 0.16f, 0.22f, 0.90f));

            GameObject pTxtObj = new GameObject("Txt");
            pTxtObj.transform.SetParent(progObj.transform, false);
            RectTransform ptRect = pTxtObj.AddComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero;
            ptRect.anchorMax = Vector2.one;
            ptRect.offsetMin = new Vector2(12f, 6f);
            ptRect.offsetMax = new Vector2(-12f, -6f);

            Text pTxt = pTxtObj.AddComponent<Text>();
            pTxt.font = font;
            pTxt.text = GetStepLiveChecklist(step);
            pTxt.fontSize = 12;
            pTxt.lineSpacing = 1.18f;
            pTxt.alignment = TextAnchor.MiddleLeft;
            pTxt.color = new Color(0.95f, 0.95f, 0.95f);

            // 4. Alt Butonlar (Devam Et & Eğitimi Geç)
            bool showNextBtn = (step == TutorialStep.Step1_CameraControls || step == TutorialStep.Step2_ExploreTabletApps);

            if (showNextBtn)
            {
                CreateActionButton(cardObj.transform, new Vector2(-80f, 22f), new Vector2(160f, 34f), "Devam ▶", "Next ▶", new Color(0.20f, 0.80f, 0.45f), font, 13, () => {
                    TutorialManager.Instance.AdvanceToNextStep();
                });
            }

            CreateActionButton(cardObj.transform, new Vector2(showNextBtn ? 140f : 0f, 22f), new Vector2(150f, 34f), "Eğitimi Atla ⏭️", "Skip Tutorial ⏭️", new Color(0.35f, 0.40f, 0.48f), font, 12, () => {
                TutorialManager.Instance.SkipTutorial();
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

            RectTransform mRect = modalObj.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0f, 0f);
            mRect.anchorMax = new Vector2(0f, 0f);
            mRect.pivot = new Vector2(0f, 0f);
            mRect.anchoredPosition = new Vector2(30f, 355f);
            mRect.sizeDelta = new Vector2(520f, 360f);

            Image mBg = modalObj.AddComponent<Image>();
            mBg.sprite = UIStyleUtility.CreateOutlinePillSprite(520, 360, 16, 2, new Color(0.25f, 0.85f, 0.55f), new Color(0.08f, 0.11f, 0.15f, 0.98f));

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -8f);
            tRect.sizeDelta = new Vector2(-20f, 32f);

            Text tTxt = titleObj.AddComponent<Text>();
            tTxt.font = font;
            tTxt.text = "📋 " + LocalizationManager.L("Tut_AllQuestsTitle", "TÜM EĞİTİM GÖREVLERİ & İLERLEME", "ALL TUTORIAL QUESTS & PROGRESS");
            tTxt.fontSize = 14;
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
            lRect.offsetMax = new Vector2(-16f, -42f);

            Text lTxt = listObj.AddComponent<Text>();
            lTxt.font = font;
            lTxt.fontSize = 12;
            lTxt.lineSpacing = 1.15f;

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

        private static string GetStepShortTitle(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.Step1_CameraControls:
                    return LocalizationManager.L("Tut_S1_Title", "Kamera & Dokunmatik Kontroller", "Camera & Mobile Controls");
                case TutorialStep.Step2_ExploreTabletApps:
                    return LocalizationManager.L("Tut_S2_Title", "EKT Tablet Uygulamaları", "EKT Tablet Apps");
                case TutorialStep.Step3_HireStoreStaffAndCallEarly:
                    return LocalizationManager.L("Tut_S3_Title", "Personel Alımı & Erken Çağır", "Hire Staff & Call Early");
                case TutorialStep.Step4_AssignStoreShifts:
                    return LocalizationManager.L("Tut_S4_Title", "Mağaza Vardiya Düzeni", "Store Staff Shifts");
                case TutorialStep.Step5_BuyInitialFurniture:
                    return LocalizationManager.L("Tut_S5_Title", "İlk Mobilyaları Satın Al", "Buy Starting Furniture");
                case TutorialStep.Step6_UnpackAndPlaceFurniture:
                    return LocalizationManager.L("Tut_S6_Title", "Mobilya & Reyon Kurulumu", "Install Furniture from Pallet");
                case TutorialStep.Step7_PlaceWholesaleBulkOrder:
                    return LocalizationManager.L("Tut_S7_Title", "Toptancı Toplu Sipariş", "Wholesaler Bulk Order");
                case TutorialStep.Step8_HireFarmStaffAndShifts:
                    return LocalizationManager.L("Tut_S8_Title", "Çiftlik Personeli & Vardiyalar", "Farm Staff & Shifts");
                case TutorialStep.Step9_BuyStartingSeeds:
                    return LocalizationManager.L("Tut_S9_Title", "Başlangıç Tohumları Al", "Buy Starting Crop Seeds");
                case TutorialStep.Step10_PlantSeedsAndOpenStore:
                    return LocalizationManager.L("Tut_S10_Title", "Tohum Ekimi & Dükkanı Aç!", "Plant Seeds & Open Store!");
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
                        "• <b>Kaydırma (Pan):</b> Parmağını ekranda sürükle (WASD).\n" +
                        "• <b>Yakınlaştırma (Zoom):</b> İki parmağınla kıstır (Mouse Scroll).\n" +
                        "• <b>Döndürme (Rotate):</b> İki parmağını çevir (Q / E Tuşları).",
                        "• <b>Pan Map:</b> Drag with one finger (WASD keys).\n" +
                        "• <b>Zoom View:</b> Pinch with two fingers (Mouse Scroll).\n" +
                        "• <b>Rotate View:</b> Twist with two fingers (Q / E keys)."
                    );

                case TutorialStep.Step2_ExploreTabletApps:
                    return LocalizationManager.L(
                        "Tut_S2_Inst",
                        "Sağ alttaki <b>📱 EKT TABLET</b> butonuna bas. Açılan 5 uygulamayı incele:\n" +
                        "🛒 Mağaza Yönetimi • 🌾 Çiftlik • 🛍️ Alışveriş • 💳 Finans • 𝕏 Sosyal Medya",
                        "Tap the <b>📱 EKT TABLET</b> button at bottom right. Explore the 5 apps:\n" +
                        "🛒 Store Mgmt • 🌾 Farm • 🛍️ Shopping • 💳 Finance • 𝕏 Social Media"
                    );

                case TutorialStep.Step3_HireStoreStaffAndCallEarly:
                    return LocalizationManager.L(
                        "Tut_S3_Inst",
                        "Tablette <b>Mağaza Yönetimi ➔ İşe Alım</b> sekmesine git. <b>3 Kasiyer</b> ve <b>3 Reyoncu</b> işe al. Ardından <b>Personel Kadrosu</b> sekmesinde sabah vardiyasındaki bir reyoncunun <b>⚡ Erken Çağır</b> butonuna bas!\n💡 <i>İpucu: Temizlikçi/Güvenlik isteğe bağlıdır.</i>",
                        "In Tablet, go to <b>Store Mgmt ➔ Hire Staff</b>. Hire <b>3 Cashiers</b> and <b>3 Restockers</b>. Then in <b>Staff List</b>, tap <b>⚡ Call Early</b> on a morning restocker!\n💡 <i>Tip: Cleaner/Security are optional.</i>"
                    );

                case TutorialStep.Step4_AssignStoreShifts:
                    return LocalizationManager.L(
                        "Tut_S4_Inst",
                        "Tablette <b>Mağaza Yönetimi ➔ Vardiyalar</b> sekmesini aç. Aldığın 3 Kasiyer ve 3 Reyoncuyu Sabah (06-14), Öğle (14-22) ve Akşam (22-06) vardiyalarına dağıt.",
                        "In Tablet, go to <b>Store Mgmt ➔ Shifts</b>. Distribute the 3 Cashiers and 3 Restockers across Morning (06-14), Noon (14-22), and Night (22-06) shifts."
                    );

                case TutorialStep.Step5_BuyInitialFurniture:
                    return LocalizationManager.L(
                        "Tut_S5_Inst",
                        "Tablette <b>Alışveriş (TrendyShop) ➔ Mobilyalar</b> sekmesine gir. Sepete şunları ekle:\n" +
                        "• 3x Standart Reyon • 1x Sepet Standı • 3x Depo Rafı • 1x Kasa • 2x Buzdolabı\n" +
                        "Sepet ikonuna bas ve <b>Ödeme Yap</b> ile siparişi ver!",
                        "In Tablet, open <b>Shopping ➔ Furniture</b>. Add to cart:\n" +
                        "• 3x Display Shelf • 1x Cart Stand • 3x Storage Rack • 1x Cashier • 2x Fridge\n" +
                        "Open Cart and tap <b>Checkout</b> to complete order!"
                    );

                case TutorialStep.Step6_UnpackAndPlaceFurniture:
                    return LocalizationManager.L(
                        "Tut_S6_Inst",
                        "Mal Kabul kapısının yanındaki <b>Teslimat Paletine</b> git. Gelen kutulara tıklayarak mobilyaları dükkan içine ve depo raflarını depoya kur.",
                        "Go to the <b>Delivery Pallet</b> near Goods Receipt. Click on boxes to place shelves inside the store and storage racks in warehouse."
                    );

                case TutorialStep.Step7_PlaceWholesaleBulkOrder:
                    return LocalizationManager.L(
                        "Tut_S7_Inst",
                        "Tablette <b>Alışveriş</b> sekmesinde üstteki yeşil <b>📦 Toplu Sipariş</b> butonuna bas ve onayla!\n" +
                        "💡 <i>Toplu Sipariş, tüm temel ürünleri %20 indirimli olarak toptancı kamyonuyla kapına getirir.</i>",
                        "In Tablet <b>Shopping</b> tab, tap the green <b>📦 Bulk Order</b> button and confirm!\n" +
                        "💡 <i>Bulk Order delivers all essential products with an automatic 20% discount straight to your loading dock.</i>"
                    );

                case TutorialStep.Step8_HireFarmStaffAndShifts:
                    return LocalizationManager.L(
                        "Tut_S8_Inst",
                        "Tablette <b>Çiftlik ➔ İşe Alım</b> sekmesinden <b>3 Çiftçi</b> işe al. Ardından <b>Vardiyalar</b> sekmesinden çiftçileri 3 farklı vardiyaya (Sabah, Öğle, Akşam) ata.",
                        "In Tablet <b>Farm ➔ Hire Staff</b>, hire <b>3 Farmers</b>. Then in <b>Shifts</b>, assign them across 3 daily shifts (Morning, Noon, Night)."
                    );

                case TutorialStep.Step9_BuyStartingSeeds:
                    return LocalizationManager.L(
                        "Tut_S9_Inst",
                        "Tablette <b>Alışveriş ➔ Tohumlar</b> sekmesine gir. İlk 3 tohumdan 1'er paket satın al:\n" +
                        "• 1x Domates Tohumu 🍅 • 1x Salatalık Tohumu 🥒 • 1x Marul Tohumu 🥬",
                        "In Tablet <b>Shopping ➔ Seeds</b> tab, buy 1 pack of each of the first 3 seeds:\n" +
                        "• 1x Tomato Seeds 🍅 • 1x Cucumber Seeds 🥒 • 1x Lettuce Seeds 🥬"
                    );

                case TutorialStep.Step10_PlantSeedsAndOpenStore:
                    return LocalizationManager.L(
                        "Tut_S10_Inst",
                        "Çiftliğin sağ tarafındaki boş tarlalara tıkla ve aldığın tohumları ek. Ekim bitince ekranın üstündeki <b>DÜKKAN KAPALI</b> butonuna basarak dükkanı müşterilere aç!",
                        "Click on empty field plots on the right and plant your seeds. Once finished, tap <b>STORE CLOSED</b> on top HUD to open your store!"
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
                    string pan = tm.DidPanCamera ? "<color=#00FFA3>✅ [✓] Harita Kaydırma (Pan)</color>" : "<color=#FFD700>⏳ [ ] Harita Kaydırma (Pan)</color>";
                    string zoom = tm.DidZoomCamera ? "<color=#00FFA3>✅ [✓] Yakınlaştırma (Zoom)</color>" : "<color=#FFD700>⏳ [ ] Yakınlaştırma (Zoom)</color>";
                    string rot = tm.DidRotateCamera ? "<color=#00FFA3>✅ [✓] Açı Döndürme (Rotate)</color>" : "<color=#FFD700>⏳ [ ] Açı Döndürme (Rotate)</color>";
                    return $"• {pan}\n• {zoom}\n• {rot}";

                case TutorialStep.Step2_ExploreTabletApps:
                    string a0 = tm.IsAppExplored(0) ? "<color=#00FFA3>✅ [✓] 🛒 Mağaza</color>" : "<color=#FFD700>⏳ [ ] 🛒 Mağaza</color>";
                    string a1 = tm.IsAppExplored(1) ? "<color=#00FFA3>✅ [✓] 🌾 Çiftlik</color>" : "<color=#FFD700>⏳ [ ] 🌾 Çiftlik</color>";
                    string a2 = tm.IsAppExplored(2) ? "<color=#00FFA3>✅ [✓] 🛍️ Alışveriş</color>" : "<color=#FFD700>⏳ [ ] 🛍️ Alışveriş</color>";
                    string a3 = tm.IsAppExplored(3) ? "<color=#00FFA3>✅ [✓] 💳 Finans</color>" : "<color=#FFD700>⏳ [ ] 💳 Finans</color>";
                    string a4 = tm.IsAppExplored(4) ? "<color=#00FFA3>✅ [✓] 𝕏 Sosyal</color>" : "<color=#FFD700>⏳ [ ] 𝕏 Sosyal</color>";
                    return $"<b>İncelenen Tablet Uygulamaları ({tm.ExploredAppsCount}/5):</b>\n{a0}  {a1}  {a2}\n{a3}  {a4}";

                case TutorialStep.Step3_HireStoreStaffAndCallEarly:
                    int cash = tm.GetStoreRoleCount(StaffRole.Kasiyer);
                    int rest = tm.GetStoreRoleCount(StaffRole.Reyoncu);
                    string cStr = (cash >= 3) ? $"<color=#00FFA3>✅ [✓] 3 Kasiyer İşe Alındı ({cash}/3)</color>" : $"<color=#FFD700>⏳ [ ] 3 Kasiyer İşe Al ({cash}/3)</color>";
                    string rStr = (rest >= 3) ? $"<color=#00FFA3>✅ [✓] 3 Reyoncu İşe Alındı ({rest}/3)</color>" : $"<color=#FFD700>⏳ [ ] 3 Reyoncu İşe Al ({rest}/3)</color>";
                    string early = tm.DidCallRestockerEarly ? "<color=#00FFA3>✅ [✓] Sabah Reyoncusu Erken Çağırıldı</color>" : "<color=#FFD700>⏳ [ ] Sabah Reyoncusunu Erken Çağır</color>";
                    return $"• {cStr}\n• {rStr}\n• {early}";

                case TutorialStep.Step4_AssignStoreShifts:
                    bool shDay = tm.HasStoreShift("Gündüz") || tm.HasStoreShift("06:00");
                    bool shEve = tm.HasStoreShift("Akşam") || tm.HasStoreShift("14:00");
                    bool shNight = tm.HasStoreShift("Gece") || tm.HasStoreShift("22:00");
                    string sD = shDay ? "<color=#00FFA3>✅ [✓] Sabah Vardiyası (06-14)</color>" : "<color=#FFD700>⏳ [ ] Sabah Vardiyasına Ata</color>";
                    string sE = shEve ? "<color=#00FFA3>✅ [✓] Öğle Vardiyası (14-22)</color>" : "<color=#FFD700>⏳ [ ] Öğle Vardiyasına Ata</color>";
                    string sN = shNight ? "<color=#00FFA3>✅ [✓] Akşam Vardiyası (22-06)</color>" : "<color=#FFD700>⏳ [ ] Akşam Vardiyasına Ata</color>";
                    return $"• {sD}\n• {sE}\n• {sN}";

                case TutorialStep.Step5_BuyInitialFurniture:
                    int sh = tm.GetBoughtCount(FurnitureType.Shelf);
                    int cs = tm.GetBoughtCount(FurnitureType.ShoppingCart);
                    int st = tm.GetBoughtCount(FurnitureType.StorageShelf);
                    int ca = tm.GetBoughtCount(FurnitureType.Cashier);
                    int fr = tm.GetBoughtCount(FurnitureType.Fridge);
                    string tSh = (sh >= 3) ? $"<color=#00FFA3>✅ [✓] 3x Standart Raf ({sh}/3)</color>" : $"<color=#FFD700>⏳ [ ] 3x Standart Raf ({sh}/3)</color>";
                    string tCs = (cs >= 1) ? $"<color=#00FFA3>✅ [✓] 1x Sepet Standı ({cs}/1)</color>" : $"<color=#FFD700>⏳ [ ] 1x Sepet Standı ({cs}/1)</color>";
                    string tSt = (st >= 3) ? $"<color=#00FFA3>✅ [✓] 3x Depo Metal Rafı ({st}/3)</color>" : $"<color=#FFD700>⏳ [ ] 3x Depo Metal Rafı ({st}/3)</color>";
                    string tCa = (ca >= 1) ? $"<color=#00FFA3>✅ [✓] 1x Market Kasası ({ca}/1)</color>" : $"<color=#FFD700>⏳ [ ] 1x Market Kasası ({ca}/1)</color>";
                    string tFr = (fr >= 2) ? $"<color=#00FFA3>✅ [✓] 2x Buzdolabı ({fr}/2)</color>" : $"<color=#FFD700>⏳ [ ] 2x Buzdolabı ({fr}/2)</color>";
                    return $"• {tSh}  • {tCs}\n• {tSt}  • {tCa}\n• {tFr}";

                case TutorialStep.Step6_UnpackAndPlaceFurniture:
                    int placed = tm.TotalFurniturePlacedInTutorial;
                    string plStr = (placed >= 8) ? $"<color=#00FFA3>✅ [✓] Mobilya Kurulumu Tamamlandı! ({placed}/10)</color>" : $"<color=#FFD700>⏳ [ ] Teslimat Paletindeki Mobilyaları Kur ({placed}/10)</color>";
                    return $"• {plStr}\n<color=#8EE2FF>Kutulara tıklayıp mağaza içine ve depoya yerleştir.</color>";

                case TutorialStep.Step7_PlaceWholesaleBulkOrder:
                    string bo = tm.DidPlaceBulkOrder ? "<color=#00FFA3>✅ [✓] Toptancı Toplu Siparişi Verildi 🚛</color>" : "<color=#FFD700>⏳ [ ] 📦 Toplu Sipariş Butonuna Bas ve Onayla</color>";
                    return $"• {bo}\n<color=#8EE2FF>Alışveriş sekmesinde yeşil 'Toplu Sipariş' butonuna bas.</color>";

                case TutorialStep.Step8_HireFarmStaffAndShifts:
                    int farm = tm.GetFarmRoleCount(StaffRole.Çiftçi);
                    string fStr = (farm >= 3) ? $"<color=#00FFA3>✅ [✓] 3 Çiftçi İşe Alındı ({farm}/3)</color>" : $"<color=#FFD700>⏳ [ ] 3 Çiftçi İşe Al ({farm}/3)</color>";
                    bool fDay = tm.HasFarmShift("Gündüz") || tm.HasFarmShift("06:00");
                    bool fEve = tm.HasFarmShift("Akşam") || tm.HasFarmShift("14:00");
                    bool fNight = tm.HasFarmShift("Gece") || tm.HasFarmShift("22:00");
                    string fSh = (fDay && fEve && fNight) ? "<color=#00FFA3>✅ [✓] Çiftlik Vardiyaları Düzenlendi (Sabah/Öğle/Akşam)</color>" : "<color=#FFD700>⏳ [ ] Çiftçileri 3 Farklı Vardiyaya Ata</color>";
                    return $"• {fStr}\n• {fSh}";

                case TutorialStep.Step9_BuyStartingSeeds:
                    string st1 = tm.DidBuyTomatoSeed ? "<color=#00FFA3>✅ [✓] 1x Domates Tohumu 🍅</color>" : "<color=#FFD700>⏳ [ ] 1x Domates Tohumu 🍅</color>";
                    string st2 = tm.DidBuyCucumberSeed ? "<color=#00FFA3>✅ [✓] 1x Salatalık Tohumu 🥒</color>" : "<color=#FFD700>⏳ [ ] 1x Salatalık Tohumu 🥒</color>";
                    string st3 = tm.DidBuyLettuceSeed ? "<color=#00FFA3>✅ [✓] 1x Marul Tohumu 🥬</color>" : "<color=#FFD700>⏳ [ ] 1x Marul Tohumu 🥬</color>";
                    return $"• {st1}\n• {st2}\n• {st3}";

                case TutorialStep.Step10_PlantSeedsAndOpenStore:
                    int cp = tm.CropsPlantedInTutorial;
                    string cpStr = (cp >= 3) ? $"<color=#00FFA3>✅ [✓] 3 Tarla Parseline Ekim Yapıldı ({cp}/3)</color>" : $"<color=#FFD700>⏳ [ ] Tarlaya Tohumları Ek ({cp}/3 Parsel)</color>";
                    string op = tm.DidOpenStoreInTutorial ? "<color=#00FFA3>✅ [✓] Dükkan Müşterilere Açıldı 🟢</color>" : "<color=#FFD700>⏳ [ ] Üstteki DÜKKANI AÇ Butonuna Bas</color>";
                    return $"• {cpStr}\n• {op}";

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
