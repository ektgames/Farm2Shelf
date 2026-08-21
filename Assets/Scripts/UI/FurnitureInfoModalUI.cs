using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Mobilya ve Dekorasyonların üzerine tıklandığında açılan detaylı Bilgi, Stok ve Pasif Gelir Arayüzü.
    /// </summary>
    public class FurnitureInfoModalUI : MonoBehaviour
    {
        public static FurnitureInfoModalUI Instance { get; private set; }
        public static bool IsFurnitureModalOpen => Instance != null && Instance.modalCanvasObj != null && Instance.modalCanvasObj.activeSelf;

        private static bool globalIsHideAssignedActive = false;

        private GameObject modalCanvasObj;
        private PlacedFurnitureController currentFurniture;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
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
            if (IsFurnitureModalOpen && currentFurniture != null)
            {
                ShowModal(currentFurniture);
            }
        }

        public void ShowModal(PlacedFurnitureController furniture)
        {
            if (furniture == null || furniture.gameObject == null) return;
            this.currentFurniture = furniture;

            try
            {
                ModalManager.SetModalOpen(true);

                // Varsa eski pencereyi temizle
                if (modalCanvasObj != null) Destroy(modalCanvasObj);

                // UI Canvas Oluşturma
                modalCanvasObj = new GameObject("Furniture_Info_Modal_Canvas");
                Canvas canvas = modalCanvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 180;

                CanvasScaler scaler = modalCanvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                modalCanvasObj.AddComponent<GraphicRaycaster>();

                // Arka Plan Karartma (Overlay Backdrop)
                GameObject backdrop = new GameObject("Backdrop");
                backdrop.transform.SetParent(modalCanvasObj.transform, false);
                RectTransform bdRect = backdrop.AddComponent<RectTransform>();
                bdRect.anchorMin = Vector2.zero;
                bdRect.anchorMax = Vector2.one;
                bdRect.sizeDelta = Vector2.zero;

                Image bdImg = backdrop.AddComponent<Image>();
                bdImg.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);

                Button bdBtn = backdrop.AddComponent<Button>();
                bdBtn.onClick.AddListener(CloseModal);

                FurnitureItemDef def = FurnitureDatabase.GetDef(furniture.FurnitureType);

                // Müşteri Hizmetleri Masası ise Özel Görev & Tanım Modalını Göster!
                if (furniture.FurnitureType == FurnitureType.CustomerServiceDesk)
                {
                    ShowCustomerServiceDeskModal(backdrop, furniture, def);
                    return;
                }

                // Kasa veya Alışveriş Sepeti Stantı ise Genel Mobilya Bilgi Modalını Göster!
                if (furniture.FurnitureType == FurnitureType.Cashier || furniture.FurnitureType == FurnitureType.ShoppingCart)
                {
                    ShowGenericFurnitureModal(backdrop, furniture, def);
                    return;
                }

                // Eğer bir Dekorasyon ögesi ise Pasif Gelir Modalını Göster!
                if (def != null && def.category == FurnitureCategory.Decoration)
                {
                    ShowDecorationModal(backdrop, furniture, def);
                    return;
                }

                // Normal Mobilya & Raf Modalı (740x690)
                GameObject panel = new GameObject("Modal_Panel");
                panel.transform.SetParent(backdrop.transform, false);
                RectTransform pRect = panel.AddComponent<RectTransform>();
                pRect.anchorMin = new Vector2(0.5f, 0.5f);
                pRect.anchorMax = new Vector2(0.5f, 0.5f);
                pRect.pivot = new Vector2(0.5f, 0.5f);
                pRect.sizeDelta = new Vector2(740, 690);

                Image pImg = panel.AddComponent<Image>();
                pImg.color = new Color(0.10f, 0.14f, 0.20f, 0.95f);

                // Üst Başlık Şeridi
                GameObject header = new GameObject("Header");
                header.transform.SetParent(panel.transform, false);
                RectTransform hRect = header.AddComponent<RectTransform>();
                hRect.anchorMin = new Vector2(0, 1);
                hRect.anchorMax = new Vector2(1, 1);
                hRect.pivot = new Vector2(0.5f, 1);
                hRect.anchoredPosition = Vector2.zero;
                hRect.sizeDelta = new Vector2(0, 65);

                Image hImg = header.AddComponent<Image>();
                hImg.color = (furniture.FurnitureType == FurnitureType.StorageShelf) ? new Color(0.85f, 0.40f, 0.10f, 1f) : new Color(0.12f, 0.65f, 0.85f, 1f);

                string titleName = def != null ? def.name : furniture.FurnitureType.ToString();
                string iconEmoji = def != null ? def.iconEmoji : "🗄️";

                Text hText = CreateText(header, $"{iconEmoji} {titleName} - Stok & Raf Bilgisi", 24, FontStyle.Bold, Color.white);
                hText.alignment = TextAnchor.MiddleCenter;

                // Özet Bilgi Çubuğu
                int totalStock = 0;
                int totalCapacity = 0;
                if (furniture.rows != null)
                {
                    foreach (var r in furniture.rows)
                    {
                        if (r == null) continue;
                        totalStock += r.currentStock;
                        totalCapacity += r.maxCapacity;
                    }
                }

                GameObject subHeader = new GameObject("SubHeader");
                subHeader.transform.SetParent(panel.transform, false);
                RectTransform shRect = subHeader.AddComponent<RectTransform>();
                shRect.anchorMin = new Vector2(0, 1);
                shRect.anchorMax = new Vector2(1, 1);
                shRect.pivot = new Vector2(0.5f, 1);
                shRect.anchoredPosition = new Vector2(0, -70);
                shRect.sizeDelta = new Vector2(-20, 35);

                int numRows = furniture.rows != null ? furniture.rows.Length : 4;
                string zoneStr = (furniture.FurnitureType == FurnitureType.StorageShelf) ? $"📦 Depo ({numRows} Sıra x 50)" : $"📍 Mağaza ({numRows} Sıra x 50)";
                float fillRatio = totalCapacity > 0 ? ((float)totalStock / totalCapacity * 100f) : 0f;
                Text shText = CreateText(subHeader, $"{zoneStr} | Toplam Stok: {totalStock} / {totalCapacity} Adet (%{fillRatio:F0} Dolu)", 16, FontStyle.Bold, new Color(0.85f, 0.90f, 0.95f));
                shText.alignment = TextAnchor.MiddleCenter;

                // Raf Kat Kartları Konteyneri (Maskeli Kaydırılabilir Liste)
                GameObject scrollObj = new GameObject("Rows_ScrollArea");
                scrollObj.transform.SetParent(panel.transform, false);
                RectTransform sRect = scrollObj.AddComponent<RectTransform>();
                sRect.anchorMin = new Vector2(0, 0);
                sRect.anchorMax = new Vector2(1, 1);
                sRect.offsetMin = new Vector2(20, 95);  // Alt Butonlar Barının Üstü
                sRect.offsetMax = new Vector2(-20, -115); // Üst Özet Bilgi Barının Altı

                Image sBgImg = scrollObj.AddComponent<Image>();
                sBgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.60f);

                scrollObj.AddComponent<RectMask2D>();

                ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;

                GameObject contentObj = new GameObject("Rows_Content");
                contentObj.transform.SetParent(scrollObj.transform, false);
                RectTransform cRect = contentObj.AddComponent<RectTransform>();
                cRect.anchorMin = new Vector2(0, 1);
                cRect.anchorMax = new Vector2(1, 1);
                cRect.pivot = new Vector2(0.5f, 1);
                cRect.sizeDelta = new Vector2(0, 0);

                VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 8;
                vlg.padding = new RectOffset(6, 6, 6, 6);
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;

                ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRect.content = cRect;

                if (furniture.rows != null)
                {
                    for (int i = 0; i < furniture.rows.Length; i++)
                    {
                        var rData = furniture.rows[i];
                        if (rData == null) continue;

                        BuildRowCard(contentObj, rData);
                    }
                }

                // Alt Butonlar Barı (Taşı, Sat, Kapat)
                BuildFooterButtonsBar(panel, furniture, def);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FurnitureInfoModalUI] Modal oluşturulurken hata: {ex}");
                CloseModal();
            }
        }

        private void ShowDecorationModal(GameObject backdrop, PlacedFurnitureController furniture, FurnitureItemDef def)
        {
            GameObject panel = new GameObject("Modal_Panel_Decoration");
            panel.transform.SetParent(backdrop.transform, false);
            RectTransform pRect = panel.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = new Vector2(680, 480);

            Image pImg = panel.AddComponent<Image>();
            pImg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);

            // Üst Mor / Lüks Dekor Başlık Şeridi
            GameObject header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            RectTransform hRect = header.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1);
            hRect.anchorMax = new Vector2(1, 1);
            hRect.pivot = new Vector2(0.5f, 1);
            hRect.anchoredPosition = Vector2.zero;
            hRect.sizeDelta = new Vector2(0, 65);

            Image hImg = header.AddComponent<Image>();
            hImg.color = new Color(0.55f, 0.20f, 0.70f, 1f);

            Text hText = CreateText(header, $"🎨 {def.iconEmoji} {def.name} - Pasif Gelir & Etkileşim", 22, FontStyle.Bold, Color.white);
            hText.alignment = TextAnchor.MiddleCenter;

            // Orta İçerik Kartları Alanı
            GameObject contentBox = new GameObject("ContentBox");
            contentBox.transform.SetParent(panel.transform, false);
            RectTransform cboxRect = contentBox.AddComponent<RectTransform>();
            cboxRect.anchorMin = Vector2.zero;
            cboxRect.anchorMax = Vector2.one;
            cboxRect.offsetMin = new Vector2(24, 90);
            cboxRect.offsetMax = new Vector2(-24, -80);

            VerticalLayoutGroup vlg = contentBox.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            if (def.passiveIncomePerUse > 0)
            {
                // Ticari Satış / Otomat Cihazları
                BuildInfoCard(contentBox, "💰 Müşteri Satış / Kullanım Geliri", $"+{def.passiveIncomePerUse:N0}C / Kullanım", new Color(0.30f, 0.85f, 0.45f));
                BuildInfoCard(contentBox, "📊 Kazanılan Toplam Pasif Gelir", $"+{furniture.TotalEarnedPassiveIncome:N0}C", new Color(0.95f, 0.80f, 0.25f));
                BuildInfoCard(contentBox, "⚡ Durum ve Otomat Türü", "🟢 Aktif Ticari Otomat (Otomatik Satış)", new Color(0.20f, 0.85f, 0.95f));
            }
            else
            {
                // Görsel / Hizmet Amaçlı Standart Dekorasyonlar (Bank, Çöp Kovası, ATM, Bitki vb.)
                BuildInfoCard(contentBox, "🎨 Dekorasyon Amacı", "Mağaza Görseli & Müşteri Konfor Alanı", new Color(0.85f, 0.90f, 0.95f));
                BuildInfoCard(contentBox, "✨ Mağaza Prestij Katkısı", "Şık Mağaza Görünümü & Ambiyans", new Color(0.95f, 0.75f, 0.30f));
                BuildInfoCard(contentBox, "⚡ Durum", "🟢 Aktif Dekoratif Öğe", new Color(0.20f, 0.85f, 0.95f));
            }

            // Alt Butonlar Barı (Taşı, Sat, Kapat)
            BuildFooterButtonsBar(panel, furniture, def);
        }

        private void ShowCustomerServiceDeskModal(GameObject backdrop, PlacedFurnitureController furniture, FurnitureItemDef def)
        {
            GameObject panel = new GameObject("Modal_Panel_CustomerServiceDesk");
            panel.transform.SetParent(backdrop.transform, false);
            RectTransform pRect = panel.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = new Vector2(740, 560);

            Image pImg = panel.AddComponent<Image>();
            pImg.color = new Color(0.10f, 0.14f, 0.20f, 0.95f);

            // Üst Başlık Şeridi (Mavi / Cyan)
            GameObject header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            RectTransform hRect = header.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1);
            hRect.anchorMax = new Vector2(1, 1);
            hRect.pivot = new Vector2(0.5f, 1);
            hRect.anchoredPosition = Vector2.zero;
            hRect.sizeDelta = new Vector2(0, 65);

            Image hImg = header.AddComponent<Image>();
            hImg.color = new Color(0.12f, 0.65f, 0.85f, 1f);

            Text hText = CreateText(header, "💁‍♂️ Müşteri Hizmetleri Masası - İstasyon Bilgisi", 22, FontStyle.Bold, Color.white);
            hText.alignment = TextAnchor.MiddleCenter;

            // Orta İçerik Kartları Alanı
            GameObject contentBox = new GameObject("ContentBox");
            contentBox.transform.SetParent(panel.transform, false);
            RectTransform cboxRect = contentBox.AddComponent<RectTransform>();
            cboxRect.anchorMin = Vector2.zero;
            cboxRect.anchorMax = Vector2.one;
            cboxRect.offsetMin = new Vector2(24, 95);
            cboxRect.offsetMax = new Vector2(-24, -75);

            VerticalLayoutGroup vlg = contentBox.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            BuildInfoCard(contentBox, "ℹ️ İstasyon Tanımı & Amacı", "Müşteri Hizmetleri Masası, dükkana gelen müşterilerin danışmanlık aldığı özel istasyondur.", new Color(0.90f, 0.95f, 1.0f));
            BuildInfoCard(contentBox, "⚡ Vardiya & Çalışan Görevi", "Vardiyadaki Müşteri Hizmetleri çalışanı bu masada oturarak gelen müşterilere rehberlik eder.", new Color(0.95f, 0.80f, 0.25f));
            BuildInfoCard(contentBox, "🚀 Müşteri Avantajları", "Danışmadan bilgi alan müşteriler %25 daha hızlı yürür ve sepetlerine ekstra 1-2 ürün eklerler.", new Color(0.30f, 0.85f, 0.45f));

            // Alt Butonlar Barı (Taşı, Sat, Kapat)
            BuildFooterButtonsBar(panel, furniture, def);
        }

        private void BuildInfoCard(GameObject parent, string title, string val, Color valColor)
        {
            GameObject card = new GameObject("Card_" + title);
            card.transform.SetParent(parent.transform, false);

            LayoutElement le = card.AddComponent<LayoutElement>();
            le.minHeight = 85f;
            le.preferredHeight = 85f;

            Image cBg = card.AddComponent<Image>();
            cBg.color = new Color(0.18f, 0.22f, 0.30f, 0.95f);

            Text tText = CreateText(card, title, 14, FontStyle.Bold, new Color(0.80f, 0.85f, 0.90f));
            RectTransform ttRect = tText.GetComponent<RectTransform>();
            ttRect.anchorMin = new Vector2(0.04f, 0.60f);
            ttRect.anchorMax = new Vector2(0.96f, 0.95f);
            tText.alignment = TextAnchor.MiddleLeft;

            Text vText = CreateText(card, val, 15, FontStyle.Bold, valColor);
            RectTransform vtRect = vText.GetComponent<RectTransform>();
            vtRect.anchorMin = new Vector2(0.04f, 0.05f);
            vtRect.anchorMax = new Vector2(0.96f, 0.60f);
            vText.alignment = TextAnchor.MiddleLeft;
            vText.horizontalOverflow = HorizontalWrapMode.Wrap;
            vText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void BuildRowCard(GameObject parent, ShelfRowData rData)
        {
            GameObject cardObj = new GameObject($"RowCard_{rData.rowId}");
            cardObj.transform.SetParent(parent.transform, false);

            LayoutElement le = cardObj.AddComponent<LayoutElement>();
            le.minHeight = 85;
            le.preferredHeight = 85;

            Image cImg = cardObj.AddComponent<Image>();
            cImg.color = new Color(0.15f, 0.20f, 0.28f, 0.90f);

            // Sol Taraf: Raf Numarası Etiketi & Tıklama Butonu
            GameObject labelBox = new GameObject("RowLabel");
            labelBox.transform.SetParent(cardObj.transform, false);
            RectTransform lbRect = labelBox.AddComponent<RectTransform>();
            lbRect.anchorMin = new Vector2(0, 0);
            lbRect.anchorMax = new Vector2(0.20f, 1);
            lbRect.offsetMin = new Vector2(8, 8);
            lbRect.offsetMax = new Vector2(-4, -8);

            Image lbImg = labelBox.AddComponent<Image>();
            lbImg.color = new Color(0.22f, 0.28f, 0.38f, 1f);

            Text lbText = CreateText(labelBox, $"{rData.rowId}. Raf", 20, FontStyle.Bold, Color.yellow);
            lbText.alignment = TextAnchor.MiddleCenter;

            // Orta Taraf: Ürün İsmi & Stok Bilgisi & İlerleme Çubuğu
            bool isStorageShelf = (currentFurniture != null && currentFurniture.FurnitureType == FurnitureType.StorageShelf);

            if (isStorageShelf && rData != null && (rData.currentStock <= 0 || rData.IsEmpty))
            {
                rData.productName = "";
                rData.productId = "";
                rData.unitPrice = 0f;
                rData.currentStock = 0;
            }

            GameObject infoBox = new GameObject("InfoBox");
            infoBox.transform.SetParent(cardObj.transform, false);
            RectTransform ibRect = infoBox.AddComponent<RectTransform>();
            ibRect.anchorMin = new Vector2(0.22f, 0);
            ibRect.anchorMax = isStorageShelf ? new Vector2(0.98f, 1) : new Vector2(0.70f, 1);
            ibRect.offsetMin = new Vector2(4, 8);
            ibRect.offsetMax = new Vector2(-8, -8);

            bool isUnassigned = rData.IsUnassigned;
            bool isEnglish = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;

            string displayName;
            if (isStorageShelf)
            {
                displayName = (rData.currentStock <= 0 || rData.IsEmpty) ?
                    (isEnglish ? "Empty Pallet Spot (Awaiting Delivery)" : "Boş Palet Yeri (Koli Bekleniyor)") :
                    rData.productName;
            }
            else
            {
                displayName = isUnassigned ?
                    (isEnglish ? "Empty (No Product Assigned)" : "Boş (Ürün Atanmamış)") :
                    rData.productName;
            }

            string priceStr = (!isStorageShelf && !isUnassigned && rData.unitPrice > 0) ? $" ({rData.unitPrice:F2}C)" : "";

            Text pText = CreateText(infoBox, $"{displayName}{priceStr}", 17, FontStyle.Bold, (isStorageShelf && rData.currentStock <= 0) || isUnassigned ? new Color(0.65f, 0.65f, 0.65f) : Color.white);
            RectTransform ptRect = pText.GetComponent<RectTransform>();
            ptRect.anchorMin = new Vector2(0, 0.5f);
            ptRect.anchorMax = new Vector2(1, 1);
            ptRect.offsetMin = Vector2.zero;
            ptRect.offsetMax = Vector2.zero;
            pText.alignment = TextAnchor.MiddleLeft;

            // Stok Sayı Metni
            Color stockColor;
            if (isStorageShelf)
            {
                stockColor = (rData.currentStock == 0) ? new Color(0.65f, 0.65f, 0.65f) : new Color(0.40f, 0.90f, 0.50f);
            }
            else
            {
                if (isUnassigned) stockColor = new Color(0.55f, 0.55f, 0.55f);
                else if (rData.currentStock == 0) stockColor = new Color(0.95f, 0.40f, 0.30f);
                else stockColor = new Color(0.40f, 0.90f, 0.50f);
            }

            string unitLabel = isStorageShelf ? (isEnglish ? "Box" : "Koli") : (isEnglish ? "Unit" : "Adet");
            Text sText = CreateText(infoBox, $"{rData.currentStock} / {rData.maxCapacity} {unitLabel}", 16, FontStyle.Bold, stockColor);
            RectTransform stRect = sText.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0, 0.05f);
            stRect.anchorMax = new Vector2(1, 0.45f);
            stRect.offsetMin = Vector2.zero;
            stRect.offsetMax = Vector2.zero;
            sText.alignment = TextAnchor.MiddleLeft;

            // AKSİYON BUTONLARI (Yalnızca Mağaza Raflarında Gösterilir):
            if (!isStorageShelf)
            {
                string selectText = isUnassigned ? (isEnglish ? "📦 Select Item" : "📦 Ürün Seç") : (isEnglish ? "⚙️ Change" : "⚙️ Değiştir");
                GameObject selectBtnObj = CreateButton(cardObj, selectText, new Color(0.18f, 0.65f, 0.35f), () => {
                    ShowProductSelectionSubModal(currentFurniture, rData);
                });
                RectTransform sbRect = selectBtnObj.GetComponent<RectTransform>();
                sbRect.anchorMin = new Vector2(0.72f, 0.15f);
                sbRect.anchorMax = new Vector2(0.98f, 0.85f);
                sbRect.offsetMin = Vector2.zero;
                sbRect.offsetMax = Vector2.zero;
            }
        }

        private void DispatchStorageRowToStoreShelf(PlacedFurnitureController storageShelf, ShelfRowData sRow)
        {
            if (storageShelf == null || sRow == null || sRow.currentStock <= 0) return;

            PlacedFurnitureController[] allFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            PlacedFurnitureController targetStoreShelf = null;
            int targetRowIdx = -1;

            // 1. Önce bu ürünün zaten atandığı dükkan rafını ara
            foreach (var f in allFurniture)
            {
                if (f == null || f.rows == null || f.FurnitureType == FurnitureType.StorageShelf) continue;
                for (int i = 0; i < f.rows.Length; i++)
                {
                    var r = f.rows[i];
                    if (r != null && r.productName == sRow.productName && r.currentStock < r.maxCapacity)
                    {
                        targetStoreShelf = f;
                        targetRowIdx = i;
                        break;
                    }
                }
                if (targetStoreShelf != null) break;
            }

            // 2. Eğer aynı ürünlü raf yoksa, boş/atanmamış dükkan rafı ara
            if (targetStoreShelf == null)
            {
                foreach (var f in allFurniture)
                {
                    if (f == null || f.rows == null || f.FurnitureType == FurnitureType.StorageShelf) continue;
                    for (int i = 0; i < f.rows.Length; i++)
                    {
                        var r = f.rows[i];
                        if (r != null && (r.IsUnassigned || r.IsEmpty))
                        {
                            targetStoreShelf = f;
                            targetRowIdx = i;
                            break;
                        }
                    }
                    if (targetStoreShelf != null) break;
                }
            }

            if (targetStoreShelf != null && targetRowIdx >= 0)
            {
                var targetRow = targetStoreShelf.rows[targetRowIdx];
                int space = targetRow.maxCapacity - targetRow.currentStock;
                int transferAmount = Mathf.Min(space, sRow.currentStock);

                targetRow.productName = sRow.productName;
                targetRow.productId = sRow.productId;
                targetRow.unitPrice = sRow.unitPrice;
                targetRow.currentStock += transferAmount;

                sRow.currentStock -= transferAmount;
                if (sRow.currentStock <= 0)
                {
                    sRow.currentStock = 0;
                    sRow.productName = "";
                    sRow.productId = "";
                }

                storageShelf.UpdateAll3DProductMeshes();
                targetStoreShelf.UpdateAll3DProductMeshes();

                ShowModal(storageShelf); // Modali yenile
            }
            else
            {
                ModalManager.ShowModal("Uygun Mağaza Rafı Yok! ⚠️", "Dükkanda bu ürünü koyabileceğin boş veya uygun bir reyon rafı bulunamadı!", "Tamam");
            }
        }

        private void ShowProductSelectionSubModal(PlacedFurnitureController furniture, ShelfRowData rData)
        {
            if (furniture == null || rData == null || modalCanvasObj == null) return;

            // Alt Pop-up Paneli (740x660 Panel)
            GameObject subPanel = new GameObject("SubModal_Product_Selection");
            subPanel.transform.SetParent(modalCanvasObj.transform, false);
            RectTransform spRect = subPanel.AddComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 0.5f);
            spRect.anchorMax = new Vector2(0.5f, 0.5f);
            spRect.pivot = new Vector2(0.5f, 0.5f);
            spRect.sizeDelta = new Vector2(740, 660);

            Image spImg = subPanel.AddComponent<Image>();
            spImg.color = new Color(0.08f, 0.12f, 0.18f, 0.99f);

            // 1. Üst Başlık Şeridi (Pinned Header - Height 55)
            GameObject header = new GameObject("Header");
            header.transform.SetParent(subPanel.transform, false);
            RectTransform hRect = header.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1);
            hRect.anchorMax = new Vector2(1, 1);
            hRect.pivot = new Vector2(0.5f, 1);
            hRect.sizeDelta = new Vector2(0, 55);

            Image hImg = header.AddComponent<Image>();
            hImg.color = new Color(0.12f, 0.55f, 0.85f, 1f);

            FurnitureItemDef fDef = FurnitureDatabase.GetDef(furniture.FurnitureType);
            string shelfName = fDef != null ? fDef.name : furniture.FurnitureType.ToString();
            Text hText = CreateText(header, $"📦 {shelfName} - {rData.rowId}. Rafa Ürün Atama", 20, FontStyle.Bold, Color.white);
            hText.alignment = TextAnchor.MiddleCenter;

            // 2. Üst Kontrol & Filtre Barı (Arama & Atanmışları Gizle Toggle - Height 55)
            GameObject filterBar = new GameObject("FilterBar");
            filterBar.transform.SetParent(subPanel.transform, false);
            RectTransform fbRect = filterBar.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0, 1);
            fbRect.anchorMax = new Vector2(1, 1);
            fbRect.pivot = new Vector2(0.5f, 1);
            fbRect.anchoredPosition = new Vector2(0, -55);
            fbRect.sizeDelta = new Vector2(0, 55);

            Image fbImg = filterBar.AddComponent<Image>();
            fbImg.color = new Color(0.10f, 0.15f, 0.22f, 0.95f);

            // Arama Kutusu (Sol Taraf: Büyüteç + InputField)
            GameObject searchContainer = new GameObject("SearchContainer");
            searchContainer.transform.SetParent(filterBar.transform, false);
            RectTransform scRect = searchContainer.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.02f, 0.12f);
            scRect.anchorMax = new Vector2(0.55f, 0.88f);
            scRect.offsetMin = Vector2.zero;
            scRect.offsetMax = Vector2.zero;

            InputField searchInput = CreateInputField(searchContainer, "🔍 Ürün Ara...", null);
            RectTransform inputRt = searchInput.GetComponent<RectTransform>();
            inputRt.anchorMin = Vector2.zero;
            inputRt.anchorMax = Vector2.one;
            inputRt.offsetMin = Vector2.zero;
            inputRt.offsetMax = Vector2.zero;

            // Atanmış Ürünleri Gizle Butonu (Sağ Taraf)
            string initialToggleLabel = globalIsHideAssignedActive ? "👁️ Atanmışları Gizle: EVET" : "👁️ Atanmışları Gizle: HAYIR";
            Color initialToggleColor = globalIsHideAssignedActive ? new Color(0.85f, 0.50f, 0.15f) : new Color(0.20f, 0.30f, 0.42f);

            GameObject hideToggleObj = CreateButton(filterBar, initialToggleLabel, initialToggleColor, null);
            RectTransform htRect = hideToggleObj.GetComponent<RectTransform>();
            htRect.anchorMin = new Vector2(0.58f, 0.12f);
            htRect.anchorMax = new Vector2(0.98f, 0.88f);
            htRect.offsetMin = Vector2.zero;
            htRect.offsetMax = Vector2.zero;

            Button hideBtn = hideToggleObj.GetComponent<Button>();
            Image hideBtnImg = hideToggleObj.GetComponent<Image>();
            Text hideBtnText = hideToggleObj.GetComponentInChildren<Text>();

            // 3. Alt Sabit Buton Barı (Pinned Footer - Height 65)
            GameObject subFooter = new GameObject("SubFooter");
            subFooter.transform.SetParent(subPanel.transform, false);
            RectTransform sfRect = subFooter.AddComponent<RectTransform>();
            sfRect.anchorMin = new Vector2(0, 0);
            sfRect.anchorMax = new Vector2(1, 0);
            sfRect.pivot = new Vector2(0.5f, 0);
            sfRect.sizeDelta = new Vector2(0, 65);

            Image sfImg = subFooter.AddComponent<Image>();
            sfImg.color = new Color(0.06f, 0.09f, 0.14f, 1f);

            GameObject closeBtn = CreateButton(subFooter, "❌ İptal / Kapat", new Color(0.45f, 0.50f, 0.55f), () => Destroy(subPanel));
            RectTransform cbRect = closeBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.30f, 0.18f);
            cbRect.anchorMax = new Vector2(0.70f, 0.82f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;

            // 4. Orta Maskeli Kaydırılabilir Liste Konteyneri
            GameObject scrollObj = new GameObject("ScrollArea");
            scrollObj.transform.SetParent(subPanel.transform, false);
            RectTransform sRect = scrollObj.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.offsetMin = new Vector2(16, 70);   // Alt Sabit Barın Üstü
            sRect.offsetMax = new Vector2(-16, -115); // Üst Filtre Barının Altı (55 + 55 + 5 marjin)

            Image scrollBgImg = scrollObj.AddComponent<Image>();
            scrollBgImg.color = new Color(0.05f, 0.08f, 0.12f, 0.80f);

            scrollObj.AddComponent<RectMask2D>();

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = cRect;

            // --- HANGİ ÜRÜNLERİN DÜKKANDA KAÇ ADET RAFTA ATANMIŞ OLDUĞUNU HESAPLA ---
            Dictionary<string, int> assignedCounts = new Dictionary<string, int>();
            PlacedFurnitureController[] allShelves = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            foreach (var s in allShelves)
            {
                if (s == null || s.rows == null || s.FurnitureType == FurnitureType.StorageShelf) continue;
                foreach (var r in s.rows)
                {
                    if (r != null && !r.IsUnassigned)
                    {
                        if (!assignedCounts.ContainsKey(r.productName))
                            assignedCounts[r.productName] = 0;
                        assignedCounts[r.productName]++;
                    }
                }
            }

            // --- KATEGORİ SÜZME VE ÜRÜN HAVUZU OLUSTURMA ---
            int currentLevel = (Farm2Shelf.Environment.EnvironmentBuilder.Instance != null) ? Farm2Shelf.Environment.EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            List<WholesaleProductDef> productsPool = new List<WholesaleProductDef>();

            if (furniture.FurnitureType == FurnitureType.ProduceShelf)
            {
                TimeManager.Season currentSeason = (TimeManager.Instance != null) ? TimeManager.Instance.CurrentSeason : TimeManager.Season.İlkbahar;

                // MANAV RAFI İÇİN ŞU AN TOHUMU SATIN ALINABİLEN (Aktif Mevsim + Yeterli Seviye) ÜRÜNLERİ FİLTRELE!
                List<GardenSeedDef> purchasableSeeds = GardenSeedDatabase.GetAllSeeds().FindAll(s => s.season == currentSeason && s.requiredLevel <= currentLevel);

                if (furniture.rows != null)
                {
                    foreach (var r in furniture.rows)
                    {
                        if (r != null && !r.IsUnassigned)
                        {
                            GardenSeedDef existingDef = GardenSeedDatabase.GetSeedById(r.productId);
                            if (existingDef != null && !purchasableSeeds.Contains(existingDef))
                            {
                                purchasableSeeds.Add(existingDef);
                            }
                        }
                    }
                }

                foreach (var seed in purchasableSeeds)
                {
                    string cropName = seed.name.Replace(" Tohumu", "").Replace(" Tohum", "");
                    int wholesalePrice = Mathf.Max(1, Mathf.RoundToInt(seed.unitSalePrice / 1.4f));
                    WholesaleProductDef cropProd = new WholesaleProductDef(
                        seed.id,
                        cropName,
                        seed.iconEmoji,
                        FurnitureType.ProduceShelf,
                        seed.requiredLevel,
                        wholesalePrice,
                        50,
                        40f
                    );
                    productsPool.Add(cropProd);
                }
            }
            else
            {
                productsPool = WholesaleDatabase.GetAllProducts().FindAll(p => p.requiredLevel <= currentLevel);
                if (furniture.FurnitureType != FurnitureType.StorageShelf)
                {
                    productsPool = productsPool.FindAll(p => p.targetShelfType == furniture.FurnitureType);
                }
            }

            // DİNAMİK LİSTE YENİLEME METODU
            System.Action RebuildProductList = () =>
            {
                // Mevcut çocuk kartları temizle
                foreach (Transform child in contentObj.transform)
                {
                    Destroy(child.gameObject);
                }

                // 1. RAFI BOŞ BIRAK / ÜRÜNÜ KALDIR SEÇENEĞİ
                if (furniture.FurnitureType != FurnitureType.StorageShelf)
                {
                    GameObject unassignCard = new GameObject("UnassignCard");
                    unassignCard.transform.SetParent(contentObj.transform, false);

                    LayoutElement uLe = unassignCard.AddComponent<LayoutElement>();
                    uLe.minHeight = 54;
                    uLe.preferredHeight = 54;

                    Image uBg = unassignCard.AddComponent<Image>();
                    uBg.color = new Color(0.28f, 0.16f, 0.18f, 1f);

                    Text uText = CreateText(unassignCard, "🚫 Rafı Boş Bırak (Ürün Atamasını Kaldır)", 15, FontStyle.Bold, new Color(0.95f, 0.60f, 0.60f));
                    RectTransform utRect = uText.GetComponent<RectTransform>();
                    utRect.anchorMin = new Vector2(0.03f, 0f);
                    utRect.anchorMax = new Vector2(0.70f, 1f);
                    uText.alignment = TextAnchor.MiddleLeft;

                    GameObject unassignBtn = CreateButton(unassignCard, "🧹 Boşalt", new Color(0.75f, 0.25f, 0.25f), () => {
                        int stock = rData.currentStock;
                        if (stock > 0 && !rData.IsUnassigned)
                        {
                            int availableSpace = GetTotalAvailableStorageCapacityForProduct(rData.productName);
                            if (availableSpace < stock)
                            {
                                ShowStorageWarningModal(
                                    "⚠️ Depoda Yeterli Yer Yok!",
                                    $"Rafta kalan <b>{stock} adet {rData.productName}</b> ürününü aktarabilmek için depoda yeterli boş alan bulunmuyor!\n\n<i>(Mevcut Boş Depo Kapasitesi: {availableSpace} Adet)</i>\n\nLütfen önce depoda yer açın veya yeni bir Depo Rafı kurun."
                                );
                                return;
                            }

                            string pName = rData.productName;
                            string pId = rData.productId;
                            float pPrice = rData.unitPrice;

                            TransferStockToStorageShelves(pName, pId, pPrice, stock);
                        }

                        rData.productName = "Boş";
                        rData.productId = "";
                        rData.unitPrice = 0f;
                        rData.currentStock = 0;

                        furniture.UpdateRow3DProductMeshes(rData.rowId);
                        Destroy(subPanel);
                        ShowModal(furniture);
                    });

                    RectTransform ubRect = unassignBtn.GetComponent<RectTransform>();
                    ubRect.anchorMin = new Vector2(0.72f, 0.15f);
                    ubRect.anchorMax = new Vector2(0.97f, 0.85f);
                    ubRect.offsetMin = Vector2.zero;
                    ubRect.offsetMax = Vector2.zero;
                }

                // 2. FİLTRELENMİŞ ÜRÜNLER
                string query = searchInput != null && !string.IsNullOrEmpty(searchInput.text) ? searchInput.text.Trim().ToLower() : "";

                List<WholesaleProductDef> filteredList = new List<WholesaleProductDef>();
                foreach (var prod in productsPool)
                {
                    assignedCounts.TryGetValue(prod.name, out int count);

                    // Atanmışları gizle seçeneği aktifse ve ürün en az 1 rafta varsa gizle!
                    if (globalIsHideAssignedActive && count > 0) continue;

                    // Canlı arama filtresi
                    if (!string.IsNullOrEmpty(query) && !prod.name.ToLower().Contains(query)) continue;

                    filteredList.Add(prod);
                }

                if (filteredList.Count == 0)
                {
                    GameObject emptyCard = new GameObject("EmptyCard");
                    emptyCard.transform.SetParent(contentObj.transform, false);

                    LayoutElement le = emptyCard.AddComponent<LayoutElement>();
                    le.minHeight = 85;

                    Image eBg = emptyCard.AddComponent<Image>();
                    eBg.color = new Color(0.20f, 0.25f, 0.35f, 0.90f);

                    string emptyMsg = !string.IsNullOrEmpty(query) 
                        ? $"🔍 \"{query}\" aramasıyla eşleşen ürün bulunamadı!" 
                        : (globalIsHideAssignedActive ? "ℹ️ Tüm ürünler raflara atanmış durumda (Atanmışları Göster'e basabilirsiniz)" : "⚠️ Bu raf türü için henüz uygun ürün bulunmuyor!");

                    Text eText = CreateText(emptyCard, emptyMsg, 15, FontStyle.Bold, Color.yellow);
                    eText.alignment = TextAnchor.MiddleCenter;
                }
                else
                {
                    foreach (var prod in filteredList)
                    {
                        GameObject prodCard = new GameObject("ProdCard_" + prod.id);
                        prodCard.transform.SetParent(contentObj.transform, false);

                        LayoutElement le = prodCard.AddComponent<LayoutElement>();
                        le.minHeight = 62;
                        le.preferredHeight = 62;

                        Image pBg = prodCard.AddComponent<Image>();
                        assignedCounts.TryGetValue(prod.name, out int assignedCount);

                        // Atanmışsa şık altın/koyu vurgulu kart rengi
                        pBg.color = (assignedCount > 0) ? new Color(0.18f, 0.22f, 0.28f, 1f) : new Color(0.14f, 0.20f, 0.28f, 1f);

                        // Metin ve Stok/Raf Detay Gösterimi
                        string cardInfoText = (assignedCount > 0)
                            ? $"{prod.iconEmoji} {prod.name} (Satış Fiyatı: {prod.SalePricePerUnit:N0}C)\n<color=#F5C242>📌 {assignedCount} adet rafta mevcut</color>"
                            : $"{prod.iconEmoji} {prod.name} (Satış Fiyatı: {prod.SalePricePerUnit:N0}C)";

                        Text pText = CreateText(prodCard, cardInfoText, 15, FontStyle.Bold, Color.white);
                        pText.supportRichText = true;
                        RectTransform ptRect = pText.GetComponent<RectTransform>();
                        ptRect.anchorMin = new Vector2(0.03f, 0f);
                        ptRect.anchorMax = new Vector2(0.70f, 1f);
                        pText.alignment = TextAnchor.MiddleLeft;

                        var selectedProd = prod;
                        GameObject selectBtn = CreateButton(prodCard, "✅ Rafa Koy", new Color(0.18f, 0.65f, 0.35f), () => {
                            rData.productName = selectedProd.name;
                            rData.productId = selectedProd.id;
                            rData.unitPrice = selectedProd.SalePricePerUnit;
                            rData.currentStock = 0; // ÜRÜN İLK DEFA ATANDIĞINDA 0 GELECEK, REYONCU DİZİNCE DOLACAK!

                            furniture.UpdateRow3DProductMeshes(rData.rowId);
                            Destroy(subPanel);
                            ShowModal(furniture); // Ana modali güncelle

                            if (TutorialManager.Instance != null)
                            {
                                TutorialManager.Instance.NotifyProductAssignedToShelf();
                            }
                        });

                        RectTransform sbRect = selectBtn.GetComponent<RectTransform>();
                        sbRect.anchorMin = new Vector2(0.72f, 0.15f);
                        sbRect.anchorMax = new Vector2(0.97f, 0.85f);
                        sbRect.offsetMin = Vector2.zero;
                        sbRect.offsetMax = Vector2.zero;
                    }
                }
            };

            // Olay Dinleyicileri (Event Listeners)
            searchInput.onValueChanged.AddListener((val) => RebuildProductList());

            if (hideBtn != null)
            {
                hideBtn.onClick.AddListener(() => {
                    globalIsHideAssignedActive = !globalIsHideAssignedActive;
                    if (hideBtnText != null)
                    {
                        hideBtnText.text = globalIsHideAssignedActive ? "👁️ Atanmışları Gizle: EVET" : "👁️ Atanmışları Gizle: HAYIR";
                    }
                    if (hideBtnImg != null)
                    {
                        hideBtnImg.color = globalIsHideAssignedActive ? new Color(0.85f, 0.50f, 0.15f) : new Color(0.20f, 0.30f, 0.42f);
                    }
                    RebuildProductList();
                });
            }

            // İlk Listeleme
            RebuildProductList();
        }

        private void ShowGenericFurnitureModal(GameObject backdrop, PlacedFurnitureController furniture, FurnitureItemDef def)
        {
            GameObject panel = new GameObject("Modal_Panel");
            panel.transform.SetParent(backdrop.transform, false);
            RectTransform pRect = panel.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = new Vector2(740, 560);

            Image pImg = panel.AddComponent<Image>();
            pImg.color = new Color(0.10f, 0.14f, 0.20f, 0.95f);

            // Üst Başlık
            GameObject header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            RectTransform hRect = header.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1);
            hRect.anchorMax = new Vector2(1, 1);
            hRect.pivot = new Vector2(0.5f, 1);
            hRect.sizeDelta = new Vector2(0, 65);

            Image hImg = header.AddComponent<Image>();
            hImg.color = new Color(0.15f, 0.60f, 0.85f, 1f);

            string titleName = def != null ? def.name : furniture.FurnitureType.ToString();
            string iconEmoji = def != null ? def.iconEmoji : "🏬";

            Text hText = CreateText(header, $"{iconEmoji} {titleName}", 24, FontStyle.Bold, Color.white);
            hText.alignment = TextAnchor.MiddleCenter;

            // Kart İçeriği
            GameObject cardObj = new GameObject("Card_Body");
            cardObj.transform.SetParent(panel.transform, false);
            RectTransform cRect = cardObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.anchoredPosition = new Vector2(0, -85);
            cRect.sizeDelta = new Vector2(-60, 360);

            Image cImg = cardObj.AddComponent<Image>();
            cImg.color = new Color(0.14f, 0.18f, 0.26f, 0.90f);

            string desc = def != null ? def.description : "Mağaza alanı stantı.";
            Text cardText = CreateText(cardObj, $"{desc}\n\n📍 Seviye: 1 | Durum: Aktif & Hizmette\n🏬 Fonksiyon: Mağaza Standı / Hizmet Noktası", 18, FontStyle.Normal, new Color(0.90f, 0.95f, 1.0f));
            cardText.alignment = TextAnchor.UpperLeft;
            cardText.horizontalOverflow = HorizontalWrapMode.Wrap;
            cardText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform ctRect = cardText.GetComponent<RectTransform>();
            ctRect.offsetMin = new Vector2(25, 25);
            ctRect.offsetMax = new Vector2(-25, -25);

            // Alt Butonlar Barı (Taşı, Sat, Kapat)
            BuildFooterButtonsBar(panel, furniture, def);
        }

        private void BuildFooterButtonsBar(GameObject parentPanel, PlacedFurnitureController furniture, FurnitureItemDef def)
        {
            GameObject footer = new GameObject("Footer");
            footer.transform.SetParent(parentPanel.transform, false);
            RectTransform fRect = footer.AddComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0, 0);
            fRect.anchorMax = new Vector2(1, 0);
            fRect.pivot = new Vector2(0.5f, 0);
            fRect.sizeDelta = new Vector2(0, 85);

            int price = def != null ? def.price : 200;
            int refundPrice = Mathf.Max(10, Mathf.RoundToInt(price * 0.50f));

            // 1. Sol Buton: Mobilyayı Sök & Taşı (Turuncu/Amber)
            GameObject moveBtnObj = CreateButton(footer, "🛠️ Taşı", new Color(0.85f, 0.50f, 0.15f), () => {
                CloseModal();
                if (furniture != null) furniture.PickUpFurniture();
            });
            RectTransform mbRect = moveBtnObj.GetComponent<RectTransform>();
            mbRect.anchorMin = new Vector2(0.03f, 0.18f);
            mbRect.anchorMax = new Vector2(0.33f, 0.82f);
            mbRect.offsetMin = Vector2.zero;
            mbRect.offsetMax = Vector2.zero;

            // 2. Orta Buton: Mobilyayı Sat (KIRMIZI / 50% İADE)
            GameObject sellBtnObj = CreateButton(footer, $"💰 Sat ({refundPrice:N0} Cr)", new Color(0.85f, 0.20f, 0.15f), () => {
                PromptSellFurnitureConfirmation(furniture, def, refundPrice);
            });
            RectTransform sbRect = sellBtnObj.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0.36f, 0.18f);
            sbRect.anchorMax = new Vector2(0.66f, 0.82f);
            sbRect.offsetMin = Vector2.zero;
            sbRect.offsetMax = Vector2.zero;

            // 3. Sağ Buton: Kapat (Koyu Gri)
            GameObject closeBtnObj = CreateButton(footer, "❌ Kapat", new Color(0.35f, 0.40f, 0.45f), CloseModal);
            RectTransform cbRect = closeBtnObj.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.69f, 0.18f);
            cbRect.anchorMax = new Vector2(0.97f, 0.82f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;
        }

        private void PromptSellFurnitureConfirmation(PlacedFurnitureController furniture, FurnitureItemDef def, int refundPrice)
        {
            if (furniture == null) return;
            string itemName = def != null ? def.name : furniture.FurnitureType.ToString();

            GameObject confirmCanvas = new GameObject("Sell_Confirm_Modal_Canvas");
            Canvas canvas = confirmCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 240;

            CanvasScaler scaler = confirmCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            confirmCanvas.AddComponent<GraphicRaycaster>();

            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(confirmCanvas.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

            GameObject dialogPanel = new GameObject("Dialog_Panel");
            dialogPanel.transform.SetParent(backdrop.transform, false);
            RectTransform dpRect = dialogPanel.AddComponent<RectTransform>();
            dpRect.anchorMin = new Vector2(0.5f, 0.5f);
            dpRect.anchorMax = new Vector2(0.5f, 0.5f);
            dpRect.pivot = new Vector2(0.5f, 0.5f);
            dpRect.sizeDelta = new Vector2(620, 360);

            Image dpImg = dialogPanel.AddComponent<Image>();
            dpImg.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);

            // Üst Kırmızı Başlık Şeridi
            GameObject header = new GameObject("Header");
            header.transform.SetParent(dialogPanel.transform, false);
            RectTransform hRect = header.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1);
            hRect.anchorMax = new Vector2(1, 1);
            hRect.pivot = new Vector2(0.5f, 1);
            hRect.sizeDelta = new Vector2(0, 55);

            Image hImg = header.AddComponent<Image>();
            hImg.color = new Color(0.85f, 0.20f, 0.15f, 1f);

            Text hText = CreateText(header, "⚠️ Mobilya Satış Onayı", 22, FontStyle.Bold, Color.white);
            hText.alignment = TextAnchor.MiddleCenter;

            // Mesaj Metni
            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(dialogPanel.transform, false);
            RectTransform bRect = bodyObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 1);
            bRect.anchorMax = new Vector2(1, 1);
            bRect.pivot = new Vector2(0.5f, 1);
            bRect.anchoredPosition = new Vector2(0, -75);
            bRect.sizeDelta = new Vector2(-50, 180);

            Text bText = CreateText(bodyObj, $"<b>{itemName}</b> nesnesini %50 iade fiyatıyla <b>+{refundPrice:N0}C</b> karşılığında satmak istediğinize emin misiniz?\n\n<i>(Bu işlem geri alınamaz ve mobilya haritadan kaldırılır)</i>", 18, FontStyle.Normal, new Color(0.92f, 0.95f, 0.98f));
            bText.alignment = TextAnchor.MiddleCenter;
            bText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Alt Onay Butonları
            GameObject confirmFooter = new GameObject("ConfirmFooter");
            confirmFooter.transform.SetParent(dialogPanel.transform, false);
            RectTransform cfRect = confirmFooter.AddComponent<RectTransform>();
            cfRect.anchorMin = Vector2.zero;
            cfRect.anchorMax = new Vector2(1, 0);
            cfRect.pivot = new Vector2(0.5f, 0);
            cfRect.sizeDelta = new Vector2(0, 75);

            // 1. EVET, SAT BUTONU
            GameObject yesBtn = CreateButton(confirmFooter, $"Evet, Sat (+{refundPrice:N0} Cr)", new Color(0.85f, 0.20f, 0.15f), () => {
                Destroy(confirmCanvas);
                CloseModal();

                if (furniture != null)
                {
                    Vector3 pos = furniture.transform.position;
                    if (EconomyManager.Instance != null) EconomyManager.Instance.AddCredits(refundPrice);
                    if (FinanceManager.Instance != null) FinanceManager.Instance.RecordIncome("Satış", $"{itemName} Satışı (%50 İade)", refundPrice);

                    ShowFloatingSellNotice(pos, refundPrice);
                    Destroy(furniture.gameObject);
                }
            });
            RectTransform yRect = yesBtn.GetComponent<RectTransform>();
            yRect.anchorMin = new Vector2(0.08f, 0.20f);
            yRect.anchorMax = new Vector2(0.47f, 0.80f);
            yRect.offsetMin = Vector2.zero;
            yRect.offsetMax = Vector2.zero;

            // 2. İPTAL BUTONU
            GameObject noBtn = CreateButton(confirmFooter, "İptal", new Color(0.35f, 0.40f, 0.48f), () => {
                Destroy(confirmCanvas);
            });
            RectTransform nRect = noBtn.GetComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0.53f, 0.20f);
            nRect.anchorMax = new Vector2(0.92f, 0.80f);
            nRect.offsetMin = Vector2.zero;
            nRect.offsetMax = Vector2.zero;
        }

        private void ShowFloatingSellNotice(Vector3 pos, int amount)
        {
            GameObject popupObj = new GameObject("Popup_SellNotice");
            popupObj.transform.position = pos + Vector3.up * 1.8f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 60f);
            popupObj.transform.localScale = Vector3.one * 0.012f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = textObj.AddComponent<Text>();
            txt.font = UIStyleUtility.GetGlobalFont(22);
            txt.text = $"+{amount:N0} Cr İade Edildi 💰";
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.30f, 0.95f, 0.45f);

            Destroy(popupObj, 1.5f);
        }

        public void CloseModal()
        {
            if (modalCanvasObj != null)
            {
                Destroy(modalCanvasObj);
                modalCanvasObj = null;
            }
            ModalManager.SetModalOpen(false);
        }

        private Text CreateText(GameObject parent, string content, int fontSize, FontStyle style, Color color)
        {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(parent.transform, false);

            RectTransform rt = txtObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Text t = txtObj.AddComponent<Text>();
            int targetFontSize = Mathf.RoundToInt(fontSize * 1.25f);
            t.font = UIStyleUtility.GetGlobalFont(targetFontSize);
            t.text = content;
            t.fontSize = targetFontSize;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(10, fontSize - 2);
            t.resizeTextMaxSize = Mathf.RoundToInt(fontSize * 1.30f);
            t.fontStyle = style;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        private GameObject CreateButton(GameObject parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("Btn_" + label);
            btnObj.transform.SetParent(parent.transform, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = color;

            Button b = btnObj.AddComponent<Button>();
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(onClick);

            Text t = CreateText(btnObj, label, 18, FontStyle.Bold, Color.white);
            t.alignment = TextAnchor.MiddleCenter;

            return btnObj;
        }

        private InputField CreateInputField(GameObject parent, string placeholderText, UnityEngine.Events.UnityAction<string> onValueChanged)
        {
            GameObject inputObj = new GameObject("InputField_Search");
            inputObj.transform.SetParent(parent.transform, false);

            Image bgImg = inputObj.AddComponent<Image>();
            bgImg.color = new Color(0.14f, 0.18f, 0.25f, 1f);

            InputField inputField = inputObj.AddComponent<InputField>();
            inputField.targetGraphic = bgImg;

            // Placeholder Metni
            GameObject placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(inputObj.transform, false);
            RectTransform phRect = placeholderGo.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(12, 0);
            phRect.offsetMax = new Vector2(-12, 0);

            Text phText = placeholderGo.AddComponent<Text>();
            phText.font = UIStyleUtility.GetGlobalFont(16);
            phText.text = placeholderText;
            phText.fontSize = 16;
            phText.fontStyle = FontStyle.Italic;
            phText.color = new Color(0.60f, 0.65f, 0.70f, 0.85f);
            phText.alignment = TextAnchor.MiddleLeft;

            // Giriş Metni Bileşeni
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(inputObj.transform, false);
            RectTransform tRect = textGo.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(12, 0);
            tRect.offsetMax = new Vector2(-12, 0);

            Text txt = textGo.AddComponent<Text>();
            txt.font = UIStyleUtility.GetGlobalFont(16);
            txt.text = "";
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Normal;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            inputField.textComponent = txt;
            inputField.placeholder = phText;

            if (onValueChanged != null)
            {
                inputField.onValueChanged.AddListener(onValueChanged);
            }

            return inputField;
        }

        private int GetTotalAvailableStorageCapacityForProduct(string productName)
        {
            int totalSpace = 0;
            PlacedFurnitureController[] allFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            foreach (var f in allFurniture)
            {
                if (f == null || f.rows == null || f.FurnitureType != FurnitureType.StorageShelf) continue;
                foreach (var r in f.rows)
                {
                    if (r == null) continue;
                    if (r.productName == productName || r.IsUnassigned || r.IsEmpty || r.currentStock <= 0)
                    {
                        int spaceLeft = r.maxCapacity - (r.currentStock > 0 ? r.currentStock : 0);
                        if (spaceLeft > 0)
                        {
                            totalSpace += spaceLeft;
                        }
                    }
                }
            }
            return totalSpace;
        }

        private bool TransferStockToStorageShelves(string productName, string productId, float unitPrice, int stockToTransfer)
        {
            if (stockToTransfer <= 0) return true;

            int remainingToTransfer = stockToTransfer;
            PlacedFurnitureController[] allFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);

            // 1. ÖNCELİK: Zaten bu ürünün bulunduğu depo raflarına doldur
            foreach (var f in allFurniture)
            {
                if (remainingToTransfer <= 0) break;
                if (f == null || f.rows == null || f.FurnitureType != FurnitureType.StorageShelf) continue;

                bool shelfUpdated = false;
                foreach (var r in f.rows)
                {
                    if (remainingToTransfer <= 0) break;
                    if (r != null && !r.IsUnassigned && r.productName == productName && r.currentStock > 0)
                    {
                        int spaceLeft = r.maxCapacity - r.currentStock;
                        if (spaceLeft > 0)
                        {
                            int add = Mathf.Min(spaceLeft, remainingToTransfer);
                            r.currentStock += add;
                            remainingToTransfer -= add;
                            shelfUpdated = true;
                        }
                    }
                }
                if (shelfUpdated)
                {
                    f.UpdateRow3DProductMeshes(0);
                }
            }

            // 2. ÖNCELİK: Boş / Atanmamış depo raflarına doldur
            foreach (var f in allFurniture)
            {
                if (remainingToTransfer <= 0) break;
                if (f == null || f.rows == null || f.FurnitureType != FurnitureType.StorageShelf) continue;

                bool shelfUpdated = false;
                for (int i = 0; i < f.rows.Length; i++)
                {
                    if (remainingToTransfer <= 0) break;
                    var r = f.rows[i];
                    if (r != null && (r.IsUnassigned || r.IsEmpty || r.currentStock <= 0))
                    {
                        r.productName = productName;
                        r.productId = productId;
                        r.unitPrice = unitPrice;

                        int spaceLeft = r.maxCapacity;
                        int add = Mathf.Min(spaceLeft, remainingToTransfer);
                        r.currentStock = add;
                        remainingToTransfer -= add;
                        shelfUpdated = true;
                    }
                }
                if (shelfUpdated)
                {
                    f.UpdateRow3DProductMeshes(0);
                }
            }

            return remainingToTransfer == 0;
        }

        private void ShowStorageWarningModal(string title, string message)
        {
            GameObject warnCanvas = new GameObject("Storage_Warning_Modal_Canvas");
            Canvas canvas = warnCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 260;

            CanvasScaler scaler = warnCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            warnCanvas.AddComponent<GraphicRaycaster>();

            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(warnCanvas.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

            GameObject dialogPanel = new GameObject("Dialog_Panel");
            dialogPanel.transform.SetParent(backdrop.transform, false);
            RectTransform dpRect = dialogPanel.AddComponent<RectTransform>();
            dpRect.anchorMin = new Vector2(0.5f, 0.5f);
            dpRect.anchorMax = new Vector2(0.5f, 0.5f);
            dpRect.pivot = new Vector2(0.5f, 0.5f);
            dpRect.sizeDelta = new Vector2(640, 360);

            Image dpImg = dialogPanel.AddComponent<Image>();
            dpImg.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);

            // Üst Turuncu/Kırmızı Başlık Şeridi
            GameObject header = new GameObject("Header");
            header.transform.SetParent(dialogPanel.transform, false);
            RectTransform hRect = header.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1);
            hRect.anchorMax = new Vector2(1, 1);
            hRect.pivot = new Vector2(0.5f, 1);
            hRect.sizeDelta = new Vector2(0, 55);

            Image hImg = header.AddComponent<Image>();
            hImg.color = new Color(0.88f, 0.35f, 0.12f, 1f);

            Text hText = CreateText(header, title, 22, FontStyle.Bold, Color.white);
            hText.alignment = TextAnchor.MiddleCenter;

            // Mesaj Metni
            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(dialogPanel.transform, false);
            RectTransform bRect = bodyObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 1);
            bRect.anchorMax = new Vector2(1, 1);
            bRect.pivot = new Vector2(0.5f, 1);
            bRect.anchoredPosition = new Vector2(0, -75);
            bRect.sizeDelta = new Vector2(-50, 180);

            Text bText = CreateText(bodyObj, message, 17, FontStyle.Normal, new Color(0.92f, 0.95f, 0.98f));
            bText.alignment = TextAnchor.MiddleCenter;
            bText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bText.supportRichText = true;

            // Alt Anladım Butonu
            GameObject confirmFooter = new GameObject("ConfirmFooter");
            confirmFooter.transform.SetParent(dialogPanel.transform, false);
            RectTransform cfRect = confirmFooter.AddComponent<RectTransform>();
            cfRect.anchorMin = Vector2.zero;
            cfRect.anchorMax = new Vector2(1, 0);
            cfRect.pivot = new Vector2(0.5f, 0);
            cfRect.sizeDelta = new Vector2(0, 75);

            GameObject okBtn = CreateButton(confirmFooter, "🆗 Anladım", new Color(0.18f, 0.55f, 0.85f), () => {
                Destroy(warnCanvas);
            });
            RectTransform oRect = okBtn.GetComponent<RectTransform>();
            oRect.anchorMin = new Vector2(0.30f, 0.20f);
            oRect.anchorMax = new Vector2(0.70f, 0.80f);
            oRect.offsetMin = Vector2.zero;
            oRect.offsetMax = Vector2.zero;
        }
    }
}
