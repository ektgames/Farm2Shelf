#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;
using Farm2Shelf.Environment;
using Farm2Shelf.CameraSystem;

// Opt-in, editor-only regression probe. Never runs in a player or writes game saves.
[InitializeOnLoad]
public static class InteractionRegressionProbe
{
    const string Request = "Temp/interaction-probe.request";
    const string Report = "interaction-probe-results.txt";
    static IEnumerator routine;
    static int frame = -1;
    static int failures;
    static Mouse mouse;
    static Touchscreen touch;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static InteractionRegressionProbe() { EditorApplication.update += Tick; }
    public static void Begin()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this probe in an isolated batch-mode project copy.");
        Directory.CreateDirectory("Temp");
        File.WriteAllText(Request, "run");
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
    }
    static void Log(string s) { File.AppendAllText(Report, s + "\n"); Debug.Log("[InteractionProbe] " + s); }
    static void Check(bool value, string label) { if (!value) failures++; Log((value ? "PASS " : "FAIL ") + label); }
    static void Tick()
    {
        if (!Application.isBatchMode) return;
        if (!File.Exists(Request)) return;
        try
        {
            if (!EditorApplication.isPlaying)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.WriteAllText(Report, "Starting Unity Play Mode input regression\n");
                EditorApplication.isPlaying = true;
                return;
            }
            if (Time.frameCount == frame || Time.realtimeSinceStartup < 2) return;
            frame = Time.frameCount;
            if (routine == null) routine = Run();
            if (!routine.MoveNext()) Finish();
        }
        catch (Exception e) { failures++; Log("ERROR " + e); Finish(); }
    }
    static void Finish()
    {
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
        File.Delete(Request);
        routine = null;
        Log("DONE failures=" + failures);
        int exitCode = failures > 0 ? 1 : 0;
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
    }
    static IEnumerable Frames(int count)
    {
        // Headless Unity can run hundreds of frames per second. Wait for UI animation
        // and modal release guards in real time, not an assumed 60-Hz frame rate.
        double until = EditorApplication.timeSinceStartup + count / 60d;
        int frames = 0;
        while (frames++ < count || EditorApplication.timeSinceStartup < until) yield return null;
    }
    static Vector2 Center(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }
    static IEnumerator Tap(Vector2 point, bool mobile)
    {
        if (mobile) InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = point });
        else InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
        foreach (var f in Frames(3)) yield return f;
        if (mobile) InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = point });
        else InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        foreach (var f in Frames(4)) yield return f;
    }
    static IEnumerator Run()
    {
        // Do not start/load a save slot. Scene defaults are disposable Play Mode data.
        foreach (var f in Frames(10)) yield return f;
        var intro = EKTReklamIntroManager.Instance;
        if (intro != null)
        {
            intro.StopAllCoroutines();
            foreach (string name in new[] { "activeIntroCanvas", "blackCurtainObj" })
            {
                var go = typeof(EKTReklamIntroManager).GetField(name,Private).GetValue(intro) as GameObject;
                if (go != null) go.SetActive(false);
            }
            typeof(EKTReklamIntroManager).GetProperty("HasIntroFinished").SetValue(null,true);
        }
        MainMenuUI.Instance?.HideMenu();
        TutorialManager.Instance?.SkipTutorial();
        ModalManager.CloseModal();
        Log("Initial gate: " + WorldInteractionGate.BlockReason(new Vector2(Screen.width/2,Screen.height/2)));
        mouse = InputSystem.AddDevice<Mouse>("RegressionMouse");
        touch = InputSystem.AddDevice<Touchscreen>("RegressionTouch");
        UiReliableClick.ConfigureSharedEventSystem();
        // Batch mode has no focused Game View; route injected devices into the player.
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        foreach (var f in Frames(10)) yield return f;
        foreach (bool mobile in new[] { false, true })
        {
            EKTPhoneManager.Instance.OpenTrendyShopApp();
            foreach (var f in Frames(40)) yield return f;
            var phone = EKTPhoneManager.Instance;
            var cart = (IDictionary)typeof(EKTPhoneManager).GetField("shoppingCart",Private).GetValue(phone);
            int before = cart.Count;
            var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.gameObject.activeInHierarchy && b.name.Contains("Sepete Ekle") && IsTopButton(b));
            Check(button != null, (mobile ? "Touch" : "Mouse") + " shopping Add button found");
            if (button != null)
            {
                var controls = button.transform.parent;
                Log("Button screen=" + Center(button) + " size=" + Screen.width + "x" + Screen.height);
                var tap = Tap(Center(button), mobile); while(tap.MoveNext()) yield return tap.Current;
                Check(cart.Count == before + 1, (mobile ? "Touch" : "Mouse") + " Add creates exactly one cart entry");
                Log("Module=" + EventSystem.current.currentInputModule + " mouse=" + mouse.position.ReadValue() + " pressed=" + mouse.leftButton.isPressed + " touch=" + touch.primaryTouch.phase.ReadValue());
                for (int repeat = 0; repeat < 5; repeat++)
                {
                    var plus = controls.GetComponentsInChildren<Button>().FirstOrDefault(b => b.GetComponentInChildren<Text>().text == "+");
                    if (plus == null) { Check(false,"Repeat Add control missing"); break; }
                    int quantity = cart.Values.Cast<int>().Sum();
                    var addTap = Tap(Center(plus),mobile); while(addTap.MoveNext()) yield return addTap.Current;
                    Check(cart.Values.Cast<int>().Sum() == quantity + 1,(mobile ? "Touch" : "Mouse") + " repeated cart click " + repeat);
                }
            }
            Check(!TouchInputHelper.Dispatch3DClick(new Vector2(Screen.width/2,Screen.height/2)),"Tablet blocks world input");
            phone.ClosePhoneTabletInstant();
            foreach (var f in Frames(10)) yield return f;
            var pallet = UnityEngine.Object.FindFirstObjectByType<DeliveryPalletClickable>();
            IsometricCameraSetup.Instance.FocusOn(pallet.transform.position,true);
            foreach (var f in Frames(6)) yield return f;
            var pos = Camera.main.WorldToScreenPoint(pallet.GetComponent<Collider>().bounds.center);
            Log("Pallet screen=" + pos + " gate=" + WorldInteractionGate.BlockReason(pos));
            Log("Pallet ray hits=" + Physics.RaycastAll(Camera.main.ScreenPointToRay(pos),250f).Length + " timeScale=" + Time.timeScale);
            var palletTap = Tap(pos, mobile); while(palletTap.MoveNext()) yield return palletTap.Current;
            Check(PalletStorageInventoryModalUI.IsModalOpen, (mobile ? "Touch" : "Mouse") + " shop pallet opens");
            PalletStorageInventoryModalUI.HideModal();
            foreach (var f in Frames(10)) yield return f;
            var workshop = UnityEngine.Object.FindAnyObjectByType<WorkshopPalletClickable>();
            IsometricCameraSetup.Instance.FocusOn(workshop.transform.position, true);
            foreach (var f in Frames(5)) yield return f;
            var workshopTap = Tap(Camera.main.WorldToScreenPoint(workshop.GetComponent<Collider>().bounds.center), mobile);
            while(workshopTap.MoveNext()) yield return workshopTap.Current;
            Check(PalletStorageInventoryModalUI.IsModalOpen, (mobile ? "Touch" : "Mouse") + " workshop pallet opens");
            PalletStorageInventoryModalUI.HideModal();
            foreach (var f in Frames(10)) yield return f;
            IsometricCameraSetup.Instance.FocusOn(pallet.transform.position,true);
            foreach (var target in new Component[] {
                UnityEngine.Object.FindAnyObjectByType<BarnController>(),
                UnityEngine.Object.FindAnyObjectByType<FieldPlotController>(),
                UnityEngine.Object.FindAnyObjectByType<LivestockBuildingController>(),
                UnityEngine.Object.FindAnyObjectByType<TaxiStandClickable>() })
            {
                if (target == null) { Check(false,"World target missing"); continue; }
                IsometricCameraSetup.Instance.FocusOn(target.transform.position,true);
                Physics.SyncTransforms();
                foreach (var f in Frames(6)) yield return f;
                var collider = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>();
                var targetTap = Tap(Camera.main.WorldToScreenPoint(collider.bounds.center),mobile);
                while(targetTap.MoveNext()) yield return targetTap.Current;
                Check(ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen,(mobile ? "Touch " : "Mouse ") + target.GetType().Name);
                BarnInventoryModalUI.Instance?.HideModal();
                LivestockCoopModalUI.Instance?.HideModal();
                typeof(FieldPlotController).GetMethod("CloseRadialMenu",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
                ModalManager.CloseModal();
                EKTPhoneManager.Instance.ClosePhoneTabletInstant();
                foreach (var f in Frames(8)) yield return f;
            }
        }
        // Checkout through native UI events, then open the delivered box through its pallet.
        EKTPhoneManager.Instance.OpenTrendyShopApp();
        foreach (var f in Frames(40)) yield return f;
        var checkout = GameObject.Find("CheckoutBtn").GetComponent<Button>();
        var checkoutTap = Tap(Center(checkout),true); while(checkoutTap.MoveNext()) yield return checkoutTap.Current;
        foreach (var f in Frames(10)) yield return f;
        var pay = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .First(b => b.gameObject.activeInHierarchy && b.name.Contains("ÖDEME YAP"));
        var payTap = Tap(Center(pay),true); while(payTap.MoveNext()) yield return payTap.Current;
        Check(FurnitureDeliveryManager.Instance.GetActiveBoxTypes().Count > 0,"UI checkout delivers purchased furniture");
        ModalManager.CloseModal();
        EKTPhoneManager.Instance.ClosePhoneTabletInstant();
        foreach (var f in Frames(10)) yield return f;
        var delivery = UnityEngine.Object.FindAnyObjectByType<DeliveryPalletClickable>();
        IsometricCameraSetup.Instance.FocusOn(delivery.transform.position,true);
        var inventoryTap = Tap(Camera.main.WorldToScreenPoint(delivery.GetComponent<Collider>().bounds.center),true);
        while(inventoryTap.MoveNext()) yield return inventoryTap.Current;
        var assembleFromInventory = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.gameObject.activeInHierarchy && b.name == "Btn_Assemble" && IsTopButton(b));
        Check(assembleFromInventory != null,"Delivered inventory exposes Assemble button");
        if (assembleFromInventory != null)
        {
            var startTap = Tap(Center(assembleFromInventory),true); while(startTap.MoveNext()) yield return startTap.Current;
            Check(FurniturePlacementManager.Instance.IsPlacing,"Pallet Assemble enters placement via touch");
            FurniturePlacementManager.Instance.CancelPlacement();
        }
        PalletStorageInventoryModalUI.HideModal();
        var manager = FurniturePlacementManager.Instance;
        foreach (var def in FurnitureDatabase.GetAllDefs())
        {
            if (def.type == FurnitureType.DividerFence) continue;
            manager.StartPlacement(def.type,null);
            foreach (var f in Frames(2)) yield return f;
            var ghost = typeof(FurniturePlacementManager).GetField("ghostObj",Private).GetValue(manager) as GameObject;
            var hud = typeof(FurniturePlacementManager).GetField("placementHUDCanvas",Private).GetValue(manager) as GameObject;
            Check(manager.IsPlacing && ghost != null && hud != null && hud.activeInHierarchy,"Preview and placement HUD: " + def.type);
            manager.CancelPlacement();
            foreach (var f in Frames(2)) yield return f;
            Check(!manager.IsPlacing && (hud == null || !hud.activeInHierarchy),"Cancel frees input: " + def.type);
        }
        FurnitureDeliveryManager.Instance.AddOrdersToPallet(new List<FurnitureType>{FurnitureType.Shelf});
        foreach (var f in Frames(10)) yield return f;
        var box = FurnitureDeliveryManager.Instance.GetFirstBoxOfType(FurnitureType.Shelf);
        manager.StartPlacement(FurnitureType.Shelf,box);
        foreach (var f in Frames(10)) yield return f;
        var root = (GameObject)typeof(FurniturePlacementManager).GetField("placementHUDCanvas",Private).GetValue(manager);
        var rotate = root.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<Text>().text.Contains("Döndür"));
        float yaw = (float)typeof(FurniturePlacementManager).GetField("currentYRotation",Private).GetValue(manager);
        var rotateTap=Tap(Center(rotate),true); while(rotateTap.MoveNext()) yield return rotateTap.Current;
        Check((float)typeof(FurniturePlacementManager).GetField("currentYRotation",Private).GetValue(manager) != yaw,"Touch Rotate button");
        var assemble = root.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<Text>().text == "Kur");
        var placeTap=Tap(Center(assemble),true); while(placeTap.MoveNext()) yield return placeTap.Current;
        Check(!manager.IsPlacing && FurnitureDeliveryManager.Instance.GetFirstBoxOfType(FurnitureType.Shelf)==null,"Touch Assemble places shelf and consumes one box");
        // Losing a held touch must cancel it, then allow a fresh device to click.
        var palletAfterPlacement = UnityEngine.Object.FindAnyObjectByType<DeliveryPalletClickable>();
        IsometricCameraSetup.Instance.FocusOn(palletAfterPlacement.transform.position,true);
        foreach (var f in Frames(10)) yield return f;
        Vector2 recoveryPoint = Camera.main.WorldToScreenPoint(palletAfterPlacement.GetComponent<Collider>().bounds.center);
        InputSystem.QueueStateEvent(touch,new TouchState { touchId=22, phase=UnityEngine.InputSystem.TouchPhase.Began, position=recoveryPoint });
        foreach (var f in Frames(3)) yield return f;
        InputSystem.RemoveDevice(touch);
        touch = InputSystem.AddDevice<Touchscreen>("ReplacementTouch");
        foreach (var f in Frames(6)) yield return f;
        Check(!PalletStorageInventoryModalUI.IsModalOpen,"Canceled/removed touch does not open pallet");
        var recoveryTap=Tap(recoveryPoint,true); while(recoveryTap.MoveNext()) yield return recoveryTap.Current;
        Check(PalletStorageInventoryModalUI.IsModalOpen,"Fresh touch recovers after device removal");
        PalletStorageInventoryModalUI.HideModal();
    }
    static bool IsTopButton(Button b)
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Center(b) },hits);
        return hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == b;
    }
}
#endif
