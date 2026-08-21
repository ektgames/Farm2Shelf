using System;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// 3 Slotlu Oyun Kaydetme ve Yükleme Ekranı (Save/Load Modal UI).
    /// Türkçe ve İngilizce çift dilli desteklenir.
    /// </summary>
    public class SaveLoadSlotModalUI : MonoBehaviour
    {
        public static SaveLoadSlotModalUI Instance { get; private set; }

        private GameObject canvasObj;
        private bool isSaveMode = true; // true: KAYDET, false: YÜKLE
        private Action onCompleteCallback;

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
            if (canvasObj != null && canvasObj.activeSelf)
            {
                BuildUI();
            }
        }

        public void ShowSaveModal(Action callback = null)
        {
            isSaveMode = true;
            onCompleteCallback = callback;
            BuildUI();
        }

        public void ShowLoadModal(Action callback = null)
        {
            isSaveMode = false;
            onCompleteCallback = callback;
            BuildUI();
        }

        public void HideModal()
        {
            if (canvasObj != null) Destroy(canvasObj);
        }

        private void BuildUI()
        {
            if (canvasObj != null) Destroy(canvasObj);

            canvasObj = new GameObject("SaveLoad_Slot_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1250;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Arka Plan Karartma (Overlay Backdrop)
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
            bdImg.raycastTarget = true;

            // Modal Paneli (900x680)
            GameObject panelObj = new GameObject("Slot_Panel");
            panelObj.transform.SetParent(backdrop.transform, false);

            RectTransform pRect = panelObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(920f, 680f);

            Image pBg = panelObj.AddComponent<Image>();
            Color accentBorder = isSaveMode ? new Color(0.20f, 0.75f, 0.95f) : new Color(0.95f, 0.65f, 0.15f);
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(920, 680, 18, 3, accentBorder, new Color(0.08f, 0.11f, 0.16f, 0.98f));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 290f);
            tRect.sizeDelta = new Vector2(600f, 50f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.text = isSaveMode ?
                LocalizationManager.L("Save_Title", "💾 OYUNU KAYDET (3 YUVA)", "💾 SAVE GAME (3 SLOTS)") :
                LocalizationManager.L("Load_Title", "📂 KAYITLI OYUN YÜKLE (3 YUVA)", "📂 LOAD GAME (3 SLOTS)");
            tText.fontSize = 26;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = accentBorder;

            // Kapat Butonu (X)
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(panelObj.transform, false);
            RectTransform clRect = closeObj.AddComponent<RectTransform>();
            clRect.anchoredPosition = new Vector2(420f, 290f);
            clRect.sizeDelta = new Vector2(40f, 40f);

            Image clBg = closeObj.AddComponent<Image>();
            clBg.sprite = UIStyleUtility.CreateRoundedPillSprite(40, 40, 8, new Color(0.85f, 0.20f, 0.25f));

            Button clBtn = closeObj.AddComponent<Button>();
            clBtn.targetGraphic = clBg;
            clBtn.onClick.AddListener(HideModal);

            GameObject clTxtObj = new GameObject("X");
            clTxtObj.transform.SetParent(closeObj.transform, false);
            RectTransform cltRect = clTxtObj.AddComponent<RectTransform>();
            cltRect.anchorMin = Vector2.zero;
            cltRect.anchorMax = Vector2.one;

            Text clTxt = clTxtObj.AddComponent<Text>();
            clTxt.font = font;
            clTxt.text = "✖";
            clTxt.fontSize = 18;
            clTxt.alignment = TextAnchor.MiddleCenter;
            clTxt.color = Color.white;
            clTxt.raycastTarget = false;

            // 3 ADET SLOT KARTI OLUŞTUR
            float startY = 170f;
            float spacingY = 180f;

            for (int slot = 1; slot <= 3; slot++)
            {
                int currentSlot = slot;
                SaveGameData slotData = SaveSystemManager.Instance != null ? SaveSystemManager.Instance.GetSlotData(currentSlot) : new SaveGameData { slotIndex = currentSlot, isEmptySlot = true };

                GameObject slotCard = new GameObject("SlotCard_" + currentSlot);
                slotCard.transform.SetParent(panelObj.transform, false);
                RectTransform scRect = slotCard.AddComponent<RectTransform>();
                scRect.anchoredPosition = new Vector2(0f, startY - (currentSlot - 1) * spacingY);
                scRect.sizeDelta = new Vector2(850f, 160f);

                Image scBg = slotCard.AddComponent<Image>();
                Color slotBorder = slotData.isEmptySlot ? new Color(0.30f, 0.35f, 0.40f) : new Color(0.15f, 0.70f, 0.40f);
                scBg.sprite = UIStyleUtility.CreateOutlinePillSprite(850, 160, 14, 2, slotBorder, new Color(0.12f, 0.16f, 0.22f, 0.95f));

                // Slot Başlığı (ör. SLOT 1)
                GameObject slotTitleObj = new GameObject("SlotTitle");
                slotTitleObj.transform.SetParent(slotCard.transform, false);
                RectTransform stRect = slotTitleObj.AddComponent<RectTransform>();
                stRect.anchoredPosition = new Vector2(-310f, 48f);
                stRect.sizeDelta = new Vector2(180f, 36f);

                Text stTxt = slotTitleObj.AddComponent<Text>();
                stTxt.font = font;
                string slotWord = LocalizationManager.L("Slot_Word", "YUVA", "SLOT");
                stTxt.text = $"📁 <b>{slotWord} {currentSlot}</b>";
                stTxt.fontSize = 20;
                stTxt.fontStyle = FontStyle.Bold;
                stTxt.alignment = TextAnchor.MiddleLeft;
                stTxt.color = slotData.isEmptySlot ? Color.gray : new Color(0.35f, 0.85f, 0.95f);

                // Slot Detay Metni
                GameObject detailsObj = new GameObject("SlotDetails");
                detailsObj.transform.SetParent(slotCard.transform, false);
                RectTransform dRect = detailsObj.AddComponent<RectTransform>();
                dRect.anchoredPosition = new Vector2(-60f, -8f);
                dRect.sizeDelta = new Vector2(660f, 110f);

                Text dTxt = detailsObj.AddComponent<Text>();
                dTxt.font = font;
                dTxt.fontSize = 15;
                dTxt.alignment = TextAnchor.MiddleLeft;

                if (slotData.isEmptySlot)
                {
                    dTxt.text = LocalizationManager.L(
                        "Slot_Empty",
                        "<color=#808080><i>[BOŞ KAYIT YUVASI] — Henüz bu slota kayıt yapılmadı.</i></color>",
                        "<color=#808080><i>[EMPTY SAVE SLOT] — No save data recorded in this slot.</i></color>"
                    );
                }
                else
                {
                    string statusColor = slotData.isStoreOpen ?
                        LocalizationManager.L("Store_Open", "<color=#00E676>Dükkan Açık 🟢</color>", "<color=#00E676>Store Open 🟢</color>") :
                        LocalizationManager.L("Store_Closed", "<color=#FF5252>Dükkan Kapalı 🔴</color>", "<color=#FF5252>Store Closed 🔴</color>");

                    string dateLabel = LocalizationManager.L("Label_SaveDate", "Kayıt Tarihi:", "Save Date:");
                    string moneyLabel = LocalizationManager.L("Label_Money", "Bakiye:", "Balance:");
                    string storeLabel = LocalizationManager.L("Label_Store", "Mağaza:", "Store:");
                    string levelLabel = LocalizationManager.L("Label_Level", "Seviye", "Level");
                    string timeLabel = LocalizationManager.L("Label_Time", "Oyun Saati:", "Game Time:");
                    string dayLabel = LocalizationManager.L("Label_Day", "Gün", "Day");
                    string staffLabel = LocalizationManager.L("Label_Staff", "Personel:", "Staff:");
                    string activeWord = LocalizationManager.L("Label_Active", "Aktif", "Active");
                    string barnLabel = LocalizationManager.L("Label_Barn", "Ahır:", "Barn:");

                    dTxt.text = $"📅 <b>{dateLabel}</b> {slotData.saveTimestamp}\n" +
                                $"💰 <b>{moneyLabel}</b> <color=#00E676>{slotData.playerMoney:N0}C</color>  |  🏪 <b>{storeLabel}</b> {levelLabel} {slotData.storeLevel} ({statusColor})\n" +
                                $"⏰ <b>{timeLabel}</b> {dayLabel} {slotData.gameDay} - {slotData.gameHour:D2}:{slotData.gameMinute:D2}  |  👥 <b>{staffLabel}</b> {slotData.activeStaffCount} {activeWord}  |  🌾 <b>{barnLabel}</b> {slotData.barnCropKg} KG";
                }

                // Slot Eylem Butonu (KAYDET veya YÜKLE)
                GameObject actionBtnObj = new GameObject("ActionBtn_" + currentSlot);
                actionBtnObj.transform.SetParent(slotCard.transform, false);
                RectTransform abRect = actionBtnObj.AddComponent<RectTransform>();
                abRect.anchoredPosition = new Vector2(310f, 0f);
                abRect.sizeDelta = new Vector2(180f, 52f);

                Image abBg = actionBtnObj.AddComponent<Image>();
                Color btnColor = isSaveMode ? new Color(0.20f, 0.65f, 0.90f) : (slotData.isEmptySlot ? new Color(0.35f, 0.40f, 0.45f) : new Color(0.20f, 0.75f, 0.35f));
                abBg.sprite = UIStyleUtility.CreateRoundedPillSprite(180, 52, 10, btnColor);

                Button abBtn = actionBtnObj.AddComponent<Button>();
                abBtn.targetGraphic = abBg;
                abBtn.interactable = isSaveMode || !slotData.isEmptySlot;
                abBtn.onClick.AddListener(() => OnSlotActionClicked(currentSlot));

                GameObject abTxtObj = new GameObject("Label");
                abTxtObj.transform.SetParent(actionBtnObj.transform, false);
                RectTransform abtRect = abTxtObj.AddComponent<RectTransform>();
                abtRect.anchorMin = Vector2.zero;
                abtRect.anchorMax = Vector2.one;

                Text abTxt = abTxtObj.AddComponent<Text>();
                abTxt.font = font;
                string saveBtnTxt = LocalizationManager.L("Btn_Save", "💾 KAYDET", "💾 SAVE");
                string loadBtnTxt = LocalizationManager.L("Btn_Load", "📂 YÜKLE", "📂 LOAD");
                string emptyBtnTxt = LocalizationManager.L("Btn_Empty", "BOŞ", "EMPTY");

                abTxt.text = isSaveMode ? saveBtnTxt : (slotData.isEmptySlot ? emptyBtnTxt : loadBtnTxt);
                abTxt.fontSize = 16;
                abTxt.fontStyle = FontStyle.Bold;
                abTxt.alignment = TextAnchor.MiddleCenter;
                abTxt.color = Color.white;
                abTxt.raycastTarget = false;

                // Slot Sil Butonu (Doluysa gösterilir)
                if (!slotData.isEmptySlot)
                {
                    GameObject delBtnObj = new GameObject("DelBtn_" + currentSlot);
                    delBtnObj.transform.SetParent(slotCard.transform, false);
                    RectTransform dbRect = delBtnObj.AddComponent<RectTransform>();
                    dbRect.anchoredPosition = new Vector2(395f, 52f);
                    dbRect.sizeDelta = new Vector2(34f, 34f);

                    Image dbBg = delBtnObj.AddComponent<Image>();
                    dbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(34, 34, 6, new Color(0.85f, 0.20f, 0.25f));

                    Button dbBtn = delBtnObj.AddComponent<Button>();
                    dbBtn.targetGraphic = dbBg;
                    dbBtn.onClick.AddListener(() => OnSlotDeleteClicked(currentSlot));

                    GameObject dbTxtObj = new GameObject("Label");
                    dbTxtObj.transform.SetParent(delBtnObj.transform, false);
                    RectTransform dbtRect = dbTxtObj.AddComponent<RectTransform>();
                    dbtRect.anchorMin = Vector2.zero;
                    dbtRect.anchorMax = Vector2.one;

                    Text dbTxt = dbTxtObj.AddComponent<Text>();
                    dbTxt.font = font;
                    dbTxt.text = "🗑️";
                    dbTxt.fontSize = 16;
                    dbTxt.alignment = TextAnchor.MiddleCenter;
                    dbTxt.color = Color.white;
                    dbTxt.raycastTarget = false;
                }
            }
        }

        private void OnSlotActionClicked(int slotIndex)
        {
            if (isSaveMode)
            {
                if (SaveSystemManager.Instance != null)
                {
                    bool success = SaveSystemManager.Instance.SaveCurrentGame(slotIndex);
                    if (success)
                    {
                        BuildUI(); // Yenile
                        ModalManager.ShowModal(
                            LocalizationManager.L("Save_Success_Title", "Kayıt Başarılı! 💾", "Save Successful! 💾"),
                            LocalizationManager.L("Save_Success_Body", $"Oyun durumu Yuva {slotIndex}'e başarıyla kaydedildi!", $"Game state was successfully saved to Slot {slotIndex}!"),
                            LocalizationManager.L("Btn_OK", "Tamam", "OK")
                        );
                    }
                }
            }
            else
            {
                if (SaveSystemManager.Instance != null)
                {
                    bool success = SaveSystemManager.Instance.LoadGameFromSlot(slotIndex);
                    if (success)
                    {
                        HideModal();
                        if (MainMenuUI.Instance != null) MainMenuUI.Instance.HideMenu();
                        ModalManager.ShowModal(
                            LocalizationManager.L("Load_Success_Title", "Kayıt Yüklendi! 📂", "Save Loaded! 📂"),
                            LocalizationManager.L("Load_Success_Body", $"Yuva {slotIndex}'teki kayıt başarıyla yüklendi! İyi oyunlar!", $"Save data from Slot {slotIndex} was successfully loaded! Have fun!"),
                            LocalizationManager.L("Btn_OK", "Tamam", "OK")
                        );
                        onCompleteCallback?.Invoke();
                    }
                }
            }
        }

        private void OnSlotDeleteClicked(int slotIndex)
        {
            if (SaveSystemManager.Instance != null)
            {
                SaveSystemManager.Instance.DeleteSlotData(slotIndex);
                BuildUI(); // UI yenile
            }
        }
    }
}
