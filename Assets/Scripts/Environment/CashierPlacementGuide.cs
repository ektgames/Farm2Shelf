using Farm2Shelf.Core;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Farm2Shelf.Environment
{
    // Preview-only helpers share the same anchors as the customer queue and cashier AI.
    public sealed class CashierPlacementGuide : MonoBehaviour
    {
        private Text customerLabel;
        private Text staffLabel;
        private Material customerMaterial;
        private Material staffMaterial;
        private bool basketPickup;
        private bool trashDropoff;
        private bool productPickup;
        private bool atmInteraction;
        private bool arcadeInteraction;
        private bool waterInteraction;
        private bool coffeeInteraction;
        private bool snackInteraction;
        private bool candyInteraction;
        private bool slushieInteraction;
        private bool iceCreamInteraction;
        private bool donutInteraction;
        private bool jukeboxInteraction;
        private bool massageInteraction;

        public static void CreateAtmInteraction(Transform atm, bool arcade = false, bool waterDispenser = false, bool coffeeMachine = false, bool snackMachine = false, bool candyMachine = false, bool slushieMachine = false, bool iceCreamCart = false, bool donutCabinet = false, bool jukebox = false, bool massageChair = false)
        {
            var root = new GameObject("ATM_Interaction_Guide");
            root.transform.SetParent(atm, false);
            var guide = root.AddComponent<CashierPlacementGuide>();
            guide.atmInteraction = true;
            guide.arcadeInteraction = arcade;
            guide.waterInteraction = waterDispenser;
            guide.coffeeInteraction = coffeeMachine;
            guide.snackInteraction = snackMachine;
            guide.candyInteraction = candyMachine;
            guide.slushieInteraction = slushieMachine;
            guide.iceCreamInteraction = iceCreamCart;
            guide.donutInteraction = donutCabinet;
            guide.jukeboxInteraction = jukebox;
            guide.massageInteraction = massageChair;
            guide.customerMaterial = MakeMaterial(new Color(0.15f, 0.80f, 1f));
            guide.customerLabel = guide.MakeMarker("Customer", new Vector3(0f, 0.05f, -0.85f), Vector3.forward, guide.customerMaterial);
            guide.customerLabel.rectTransform.sizeDelta = new Vector2(240f, 80f);
            guide.RefreshLabels();
        }

        public static void CreateProductPickup(Transform shelf)
        {
            var root = new GameObject("Product_Pickup_Guide");
            root.transform.SetParent(shelf, false);
            var guide = root.AddComponent<CashierPlacementGuide>();
            guide.productPickup = true;
            guide.customerMaterial = MakeMaterial(new Color(0.15f, 0.80f, 1f));
            guide.customerLabel = guide.MakeMarker("Pickup", new Vector3(0f, 0.05f, -0.75f), Vector3.forward, guide.customerMaterial);
            guide.customerLabel.rectTransform.sizeDelta = new Vector2(240f, 80f);
            guide.RefreshLabels();
        }

        public static void CreateTrashDropoff(Transform bin)
        {
            var root = new GameObject("Trash_Dropoff_Guide");
            root.transform.SetParent(bin, false);
            var guide = root.AddComponent<CashierPlacementGuide>();
            guide.trashDropoff = true;
            guide.customerMaterial = MakeMaterial(new Color(0.2f, 0.85f, 0.4f));
            guide.customerLabel = guide.MakeMarker("Dropoff", new Vector3(0f, 0.05f, -StoreCleanlinessManager.BinInteractionOffset), Vector3.forward, guide.customerMaterial);
            guide.customerLabel.rectTransform.sizeDelta = new Vector2(240f, 80f);
            guide.RefreshLabels();
        }

        public static void CreateBasketPickup(Transform stand)
        {
            var root = new GameObject("Basket_Pickup_Guide");
            root.transform.SetParent(stand, false);
            var guide = root.AddComponent<CashierPlacementGuide>();
            guide.basketPickup = true;
            guide.customerMaterial = MakeMaterial(new Color(0.15f, 0.80f, 1f));
            guide.customerLabel = guide.MakeMarker("Pickup", new Vector3(0f, 0.05f, -1f), Vector3.forward, guide.customerMaterial);
            guide.customerLabel.rectTransform.sizeDelta = new Vector2(240f, 80f);
            guide.RefreshLabels();
        }

        public static void Create(Transform cashier, bool customerService = false)
        {
            var root = new GameObject("Cashier_Placement_Guide");
            root.transform.SetParent(cashier, false);
            var guide = root.AddComponent<CashierPlacementGuide>();
            guide.customerMaterial = MakeMaterial(new Color(0.15f, 0.80f, 1f));
            guide.staffMaterial = MakeMaterial(new Color(1f, 0.73f, 0.22f));
            Vector3 customer = cashier.InverseTransformPoint(FurnitureModelBuilder.GetCashierCustomerPosition(cashier));
            Vector3 staff = cashier.InverseTransformPoint(FurnitureModelBuilder.GetCashierStaffPosition(cashier));
            if (customerService)
            {
                root.name = "CustomerService_Placement_Guide";
                // Display the existing interaction/seat locations without changing the AI.
                customer = new Vector3(0f, 0.05f, -1f);
                Transform seat = cashier.Find("CustomerServiceVisual/StaffSeat");
                staff = seat != null ? cashier.InverseTransformPoint(seat.position) : new Vector3(0f, 0.53f, 0.45f);
            }
            guide.customerLabel = guide.MakeMarker("Customer", customer, Vector3.forward, guide.customerMaterial);
            guide.staffLabel = guide.MakeMarker("Staff", staff, Vector3.back, guide.staffMaterial);
            guide.RefreshLabels();
        }

        private static Material MakeMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color);
            return material;
        }

        private Text MakeMarker(string name, Vector3 position, Vector3 direction, Material material)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = position + Vector3.up * 0.025f;
            marker.transform.localRotation = Quaternion.LookRotation(direction);
            // A V-shaped arrow points to the actual standing position from behind it.
            MakeBar(marker.transform, new Vector3(0f, 0f, -0.22f), new Vector3(0.085f, 0.02f, 0.4f), 0f, material);
            MakeBar(marker.transform, new Vector3(-0.085f, 0f, -0.085f), new Vector3(0.065f, 0.02f, 0.25f), 45f, material);
            MakeBar(marker.transform, new Vector3(0.085f, 0f, -0.085f), new Vector3(0.065f, 0.02f, 0.25f), -45f, material);

            var labelObject = new GameObject(name + "_Label", typeof(RectTransform), typeof(Canvas), typeof(Text));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = position + Vector3.up * 0.60f;
            labelObject.transform.localScale = Vector3.one * 0.006f;
            Canvas canvas = labelObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 55;
            var rect = labelObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 56f);
            Text label = labelObject.GetComponent<Text>();
            label.font = UIStyleUtility.GetGlobalFont(26);
            label.fontSize = 26;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = material.GetColor("_BaseColor");
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            var outline = labelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.025f, 0.04f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return label;
        }

        private static void MakeBar(Transform parent, Vector3 position, Vector3 scale, float yaw, Material material)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = position;
            bar.transform.localScale = scale;
            bar.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Collider collider = bar.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            var renderer = bar.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void LateUpdate()
        {
            Camera camera = GameplayPerf.MainCamera;
            if (camera != null)
            {
                if (customerLabel != null) customerLabel.transform.rotation = camera.transform.rotation;
                if (staffLabel != null) staffLabel.transform.rotation = camera.transform.rotation;
            }
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (atmInteraction)
            {
                if (massageInteraction)
                {
                    if (customerLabel != null) customerLabel.text = LocalizationManager.L("Massage_PreviewInteraction", "KOLTUĞA YAKLAŞMA\nYÖNÜ", "MASSAGE CHAIR\nACCESS");
                    return;
                }
                if (jukeboxInteraction)
                {
                    if (customerLabel != null) customerLabel.text = LocalizationManager.L("Jukebox_PreviewInteraction", "MÜZİK SEÇME\nYÖNÜ", "JUKEBOX\nACCESS");
                    return;
                }
                if (donutInteraction)
                {
                    if (customerLabel != null) customerLabel.text = LocalizationManager.L("Donut_PreviewInteraction", "DONUT ALMA\nYÖNÜ", "DONUT CABINET\nACCESS");
                    return;
                }
                if (iceCreamInteraction)
                {
                    if (customerLabel != null) customerLabel.text = LocalizationManager.L("IceCream_PreviewInteraction", "DONDURMA ALMA\nYÖNÜ", "ICE CREAM\nPICKUP SIDE");
                    return;
                }
                if (slushieInteraction)
                {
                    if (customerLabel != null) customerLabel.text = LocalizationManager.L("Slushie_PreviewInteraction", "İÇECEK ALMA\nYÖNÜ", "DRINK DISPENSER\nACCESS");
                    return;
                }
                if (candyInteraction)
                {
                    if (customerLabel != null) customerLabel.text = LocalizationManager.L("Candy_PreviewInteraction", "ŞEKER ALMA\nYÖNÜ", "CANDY MACHINE\nACCESS");
                    return;
                }
                if (snackInteraction)
                {
                    if (customerLabel != null) customerLabel.text = LocalizationManager.L("Snack_PreviewInteraction", "ATIŞTIRMALIK ALMA\nYÖNÜ", "SNACK MACHINE\nACCESS");
                    return;
                }
                if (coffeeInteraction)
                {
                    if (customerLabel != null) customerLabel.text = LocalizationManager.L("Coffee_PreviewInteraction", "KAHVE ALMA\nYÖNÜ", "COFFEE MACHINE\nACCESS");
                    return;
                }
                if (waterInteraction)
                {
                    if (customerLabel != null) customerLabel.text = LocalizationManager.L("Water_PreviewInteraction", "SU ALMA\nYÖNÜ", "WATER DISPENSER\nACCESS");
                    return;
                }
                if (customerLabel != null) customerLabel.text = arcadeInteraction
                    ? LocalizationManager.L("Arcade_PreviewInteraction", "OYUN OYNAMA\nNOKTASI", "ARCADE PLAYER\nPOSITION")
                    : LocalizationManager.L("ATM_PreviewInteraction", "ATM KULLANIM\nYÖNÜ", "ATM ACCESS\nDIRECTION");
                return;
            }
            if (productPickup)
            {
                if (customerLabel != null) customerLabel.text = LocalizationManager.L("Shelf_PreviewPickup", "ÜRÜN ALMA\nYÖNÜ", "PRODUCT\nPICKUP");
                return;
            }
            if (trashDropoff)
            {
                if (customerLabel != null) customerLabel.text = LocalizationManager.L("Trash_PreviewDropoff", "ÇÖP ATMA\nNOKTASI", "TRASH\nDROPOFF");
                return;
            }
            if (basketPickup)
            {
                if (customerLabel != null) customerLabel.text = LocalizationManager.L("Basket_PreviewPickup", "SEPET ALMA\nNOKTASI", "BASKET\nPICKUP");
                return;
            }
            if (customerLabel != null) customerLabel.text = LocalizationManager.L("Cashier_PreviewCustomer", "MÜŞTERİ", "CUSTOMER");
            if (staffLabel != null) staffLabel.text = LocalizationManager.L("Cashier_PreviewStaff", "PERSONEL", "STAFF");
        }

        private void OnDestroy()
        {
            if (customerMaterial != null) Destroy(customerMaterial);
            if (staffMaterial != null) Destroy(staffMaterial);
        }
    }
}
