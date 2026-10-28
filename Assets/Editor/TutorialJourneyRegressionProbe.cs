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
public static class TutorialJourneyRegressionProbe
{
    const string Request = "Temp/tutorial-journey-probe.request";
    const string Report = "tutorial-journey-probe-results.txt";
    static IEnumerator routine;
    static int frame = -1;
    static int failures;
    static Mouse mouse;
    static Touchscreen touch;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static TutorialJourneyRegressionProbe() { EditorApplication.update += Tick; }
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
    static IEnumerator ClickTag(string id, bool mobile = true)
    {
        foreach(var f in Frames(45)) yield return f;
        var rect=TutorialFocusTarget.FindRect(id);
        var button=rect!=null ? rect.GetComponent<Button>() ?? rect.GetComponentInChildren<Button>() : null;
        Check(button!=null && IsTopButton(button), "Guided target reachable: "+id);
        if(button==null)yield break;
        var tap=Tap(Center(button),mobile);while(tap.MoveNext())yield return tap.Current;
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

        var tm=TutorialManager.Instance;
        var phone=EKTPhoneManager.Instance;
        tm.StartTutorial();
        tm.enabled=false; // Explicit progression assertions; prevents the automatic timer racing test setup.
        tm.NotifyCameraPan();tm.NotifyCameraZoom();tm.NotifyCameraRotate();
        Check(tm.IsCurrentStepComplete(),"G1 camera objective completes");
        tm.AdvanceToNextStep();
        phone.OpenPhoneTablet();
        foreach(var f in Frames(40))yield return f;
        for(int app=0;app<8;app++) {
            var click=ClickTag("app_"+app);while(click.MoveNext())yield return click.Current;
            Check(tm.IsAppExplored(app),"G2 app recorded "+app);
            click=ClickTag("tablet_back");while(click.MoveNext())yield return click.Current;
        }
        Check(tm.IsCurrentStepComplete(),"G2 all eight apps complete");
        tm.AdvanceToNextStep();
        var open=ClickTag("app_6");while(open.MoveNext())yield return open.Current;
        var contract=ClickTag("om_tab_1");while(contract.MoveNext())yield return contract.Current;
        tm.NotifyTownContractQueued();
        for(int i=0;i<2;i++) {var s=StaffManager.Instance.HireCourier(false);StaffManager.Instance.UpdateCourierShift(s.id,i==0?"08:00 - 16:00":"16:00 - 24:00");}
        Check(tm.IsCurrentStepComplete(),"G3 contract and courier shift conditions complete");
        tm.AdvanceToNextStep();phone.ClosePhoneTabletInstant();phone.OpenPhoneTablet();
        open=ClickTag("app_0");while(open.MoveNext())yield return open.Current;
        open=ClickTag("store_tab_2");while(open.MoveNext())yield return open.Current;
        foreach(var role in TutorialManager.RequiredStoreRoles) {
            foreach(var f in Frames(45))yield return f;
            var card=TutorialFocusTarget.FindRect("hire_role_"+role);
            Check(card!=null,"G4 recruitment card exists "+role);
            int count=role==StaffRole.Reyoncu?4:2;
            for(int i=0;i<count;i++) {
                var s=StaffManager.Instance.HireStaffByRole(role);
                StaffManager.Instance.UpdateStaffShift(s.id,i<count/2?"08:00 - 16:00":"16:00 - 24:00");
                if(role==StaffRole.Reyoncu && i<2)tm.NotifyEarlyStaff(s);
            }
        }
        Check(tm.IsCurrentStepComplete(),"G4 all roles including security, shifts and early restockers complete");
        tm.AdvanceToNextStep();phone.ClosePhoneTabletInstant();phone.OpenTrendyShopApp();
        var fixtures=new List<PlacedFurnitureController>();
        for(int i=0;i<TutorialManager.RequiredFurniture.Length;i++) {
            var type=TutorialManager.RequiredFurniture[i];int count=TutorialManager.RequiredFurnitureCounts[i];
            tm.NotifyFurnitureItemPurchased(type,count);
            for(int n=0;n<count;n++) {
                var obj=new GameObject("TutorialFixture_"+type);
                var placed=obj.AddComponent<PlacedFurnitureController>();placed.Setup(type,new Vector3(500+i*10,0,n*5),Quaternion.identity);
                fixtures.Add(placed);
            }
        }
        Check(tm.IsCurrentStepComplete(),"G5 exact purchased and installed furniture conditions complete");
        Check(TutorialManager.RequiredFurniture.All(t=>tm.JourneyChecklist(5).Contains(FurnitureDatabase.GetDef(t).LocalizedName)),"G5 lists all nine required furniture types");
        tm.AdvanceToNextStep();phone.ClosePhoneTabletInstant();tm.NotifyBulkOrderPlaced();
        foreach(var item in fixtures.Where(x=>x.FurnitureType==FurnitureType.Shelf || x.FurnitureType==FurnitureType.Fridge))
            for(int r=0;r<4;r++)item.rows[r]=new ShelfRowData(r,"Regression product",0,50,5);
        Check(tm.IsCurrentStepComplete(),"G6 all 20 required shelf/fridge rows complete");
        tm.AdvanceToNextStep();
        for(int i=0;i<2;i++){var s=StaffManager.Instance.HireFarmWorker();StaffManager.Instance.UpdateFarmStaffShift(s.id,i==0?"08:00 - 16:00":"16:00 - 24:00");}
        phone.OpenTrendyShopApp();
        open=ClickTag("shop_cat_3");while(open.MoveNext())yield return open.Current;
        Check(phone.ActiveShoppingCategory==3,"G7 clipped Seeds category scrolls into view and accepts touch");
        phone.ClosePhoneTabletInstant();
        foreach(var id in new[]{"spring_tomato","spring_cucumber","spring_lettuce"}) {
            tm.NotifySeedPurchased(id,1);for(int n=0;n<10;n++)tm.NotifyCropPlanted(id);
        }
        Check(!tm.IsCurrentStepComplete(),"G7 cannot advance before Done");
        open=ClickTag("plant_finish");while(open.MoveNext())yield return open.Current;
        Check(tm.IsCurrentStepComplete(),"G7 Done touch completes planting objective");
        tm.AdvanceToNextStep();phone.OpenTrendyShopApp();
        open=ClickTag("shop_cat_4");while(open.MoveNext())yield return open.Current;
        Check(phone.ActiveShoppingCategory==4,"G8 off-screen Animals category scrolls into view and accepts touch");
        phone.ClosePhoneTabletInstant();LivestockManager.Instance.GrantAnimals(LivestockType.WhiteChicken,6);LivestockManager.Instance.GrantAnimals(LivestockType.HolsteinCow,3);
        tm.NotifyBuildingVisited("Chicken_Coop");tm.NotifyBuildingVisited("Cow_Barn");
        Check(tm.IsCurrentStepComplete(),"G8 animal quantities and visits complete");
        tm.AdvanceToNextStep();tm.NotifyBuildingVisited("Farm_Barn");
        for(int i=0;i<5;i++)tm.AcknowledgeTourStop();
        Check(tm.IsCurrentStepComplete(),"G9 all five information stops complete");
        tm.AdvanceToNextStep();
        open=ClickTag("hud_store_toggle");while(open.MoveNext())yield return open.Current;
        Check(tm.IsCurrentStepComplete(),"G10 store opens through guided touch");
        tm.AdvanceToNextStep();Check(tm.CurrentStep==TutorialStep.Completed,"Tutorial reaches Completed");
        ModalManager.CloseModal();tm.SkipTutorial();phone.ClosePhoneTabletInstant();
        FurnitureDeliveryManager.Instance.AddOrdersToPallet(new List<FurnitureType>{FurnitureType.Shelf,FurnitureType.Fridge});
        var pallet=UnityEngine.Object.FindFirstObjectByType<DeliveryPalletClickable>();
        IsometricCameraSetup.Instance.FocusOn(pallet.transform.position,true);
        foreach(var f in Frames(20))yield return f;
        FurnitureDeliveryManager.Instance.TryGetTutorialPalletBounds(out Bounds bounds);
        var screenPoint=Camera.main.WorldToScreenPoint(bounds.center);
        foreach(bool mobile in new[]{false,true})for(int n=0;n<12;n++) {
            Vector2 offset=new Vector2((n%3-1)*12,(n/3%3-1)*12);
            var tap=Tap((Vector2)screenPoint+offset,mobile);while(tap.MoveNext())yield return tap.Current;
            Check(PalletStorageInventoryModalUI.IsModalOpen,(mobile?"Touch":"Mouse")+" loaded pallet repeat/edge "+n);
            PalletStorageInventoryModalUI.HideModal();foreach(var f in Frames(10))yield return f;
        }
        tm.SetStep((TutorialStep)5);TutorialMentorGuideUI.ShowGuide(false);
        foreach(var f in Frames(20))yield return f;
        for(int n=0;n<10;n++) {
            var point=Camera.main.WorldToScreenPoint(bounds.center);
            var tap=Tap(point,true);while(tap.MoveNext())yield return tap.Current;
            Check(PalletStorageInventoryModalUI.IsModalOpen,"Tutorial spotlight pallet repeat "+n);
            PalletStorageInventoryModalUI.HideModal();foreach(var f in Frames(10))yield return f;
        }
    }    static bool IsTopButton(Button b)
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Center(b) },hits);
        return hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == b;
    }
}
#endif
