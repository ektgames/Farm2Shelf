using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    public partial class TutorialMentorGuideUI
    {
        private string focusedWorldKey;
        private Transform focusedWorld;
        private Coroutine journeyPan;

        private System.Collections.IEnumerator PanJourneyCamera(Vector3 destination)
        {
            var camera=Farm2Shelf.CameraSystem.IsometricCameraSetup.Instance;
            if(camera==null) {journeyPan=null;yield break;}
            Vector3 start=camera.FocusPoint;
            float time=0;
            while(time<1.2f)
            {
                if(root==null || !root.activeSelf) {journeyPan=null;yield break;}
                time+=Time.unscaledDeltaTime;
                camera.FocusOn(Vector3.Lerp(start,destination,Mathf.SmoothStep(0,1,time/1.2f)),false);
                yield return null;
            }
            camera.FocusOn(destination,false);
            journeyPan=null;
        }

        private bool OpenApp(EKTPhoneManager phone, List<RectTransform> into, int app, bool visible)
        {
            if (!TabletOpen) { Add(into,"phone"); return false; }
            if (phone != null && phone.IsHomeScreenVisible) { Add(into,"app_"+app); return false; }
            if (!visible) { Add(into,"tablet_back"); return false; }
            return true;
        }

        private void CollectJourneyFocus(List<RectTransform> into, ref bool world)
        {
            var tm=TutorialManager.Instance;
            var phone=EKTPhoneManager.Instance;
            if(tm==null || !tm.IsTutorialActive) {world=true;return;}
            if (EKTPhoneManager.IsCartOpen || ModalManager.IsGlobalPopupOpen || (ModalManager.IsModalOpen && !TabletOpen)) {world=true;return;}
            switch(tm.JourneyStep)
            {
                case 1: world=true; break;
                case 2:
                    if(!TabletOpen) Add(into,"phone");
                    else if(phone!=null && phone.IsHomeScreenVisible) {
                        for(int i=0;i<8;i++) if(!tm.IsAppExplored(i)) {Add(into,"app_"+i);break;}
                    } else Add(into,"tablet_back");
                    break;
                case 3:
                    if(tm.TutorialMotorcycles<2) { FocusShopping(phone,into,7); return; }
                    if(!OpenApp(phone,into,6,phone!=null && phone.IsOnlineMarketVisible)) return;
                    if(!tm.FirstMotorcycleAssigned) {
                        if(phone.ActiveOnlineMarketTab!=1) Add(into,"om_tab_1");
                        else FocusRecruitment(into,"om_contract_motor_0");
                    } else if(!tm.HasAcceptedTownContract()) {
                        if(phone.ActiveOnlineMarketTab!=1) Add(into,"om_tab_1");
                        else Add(into,"om_contracts_list");
                    } else if(!tm.CouriersHired) {
                        if(phone.ActiveOnlineMarketTab!=3) Add(into,"om_tab_3");
                        else if(tm.CourierGenderCount(true)+phone.GetPendingCourierCount(true)<2) FocusRecruitment(into,"hire_courier_female");
                        else if(tm.CourierGenderCount(false)+phone.GetPendingCourierCount(false)<2) FocusRecruitment(into,"hire_courier_male");
                        else FocusRecruitment(into,"hire_confirm_2");
                    } else {
                        if(phone.ActiveOnlineMarketTab!=4) Add(into,"om_tab_4");
                        else Add(into,"courier_shift_list");
                    }
                    break;
                case 4:
                    if(!OpenApp(phone,into,0,phone!=null && phone.IsStoreAppVisible)) return;
                    if(!tm.StoreHired) {
                        if(phone.ActiveStoreTab!=2) {Add(into,"store_tab_2");return;}
                        foreach(var role in TutorialManager.RequiredStoreRoles) {
                            int target=role==StaffRole.Reyoncu?4:2;
                            if(tm.GetStoreRoleCount(role)+phone.GetPendingRecruitmentCount(0,role)<target) {
                                FocusRecruitment(into,"hire_role_"+role);return;
                            }
                        }
                        FocusRecruitment(into,"hire_confirm_0");
                    } else if(phone.ActiveStoreTab!=3) Add(into,"store_tab_3");
                    else Add(into,"store_shift_list");
                    break;
                case 5:
                    if(!tm.FurnitureBought) FocusShopping(phone,into,0);
                    else if(TabletOpen) Add(into,"tablet_close");
                    else if(PalletStorageInventoryModalUI.IsModalOpen) Add(into,"delivery_pallet_inventory");
                    else world=true;
                    break;
                case 6:
                    FocusWholesale(tm,phone,into,ref world);
                    if(tm.DidPlaceBulkOrder && TabletOpen) {world=false;Add(into,"tablet_close");}
                    break;
                case 7:
                    if (tm.SeedsPlanted && !tm.PlantingFinished && !TabletOpen)
                    {
                        SeedPlantingModeUI.EnsureTutorialFinish();
                        Add(into,"plant_finish");
                        break;
                    }
                    if(!tm.FarmersReady) {
                        FocusFarm(tm,phone,into);
                        if(phone!=null && phone.IsFarmAppVisible && phone.ActiveFarmTab==3) {
                            into.Clear();Add(into,"farm_shift_list");
                        }
                    } else if(!tm.SeedsBought) FocusShopping(phone,into,3);
                    else if(TabletOpen) Add(into,"tablet_close");
                    else world=true;
                    break;
                case 8:
                    if(LivestockManager.Instance==null || LivestockManager.Instance.TotalChickens<6 || LivestockManager.Instance.TotalCows<3)
                        FocusShopping(phone,into,4);
                    else if(TabletOpen) Add(into,"tablet_close");
                    else world=true;
                    break;
                case 9:
                    if(TabletOpen) Add(into,"tablet_close"); else world=true;
                    break;
                case 10:
                    if(TabletOpen) Add(into,"tablet_close"); else Add(into,"hud_store_toggle");
                    break;
            }
        }

        private bool TryJourneyWorldFocus()
        {
            var tm=TutorialManager.Instance;
            if(tm==null || !tm.IsTutorialActive || TabletOpen || ModalManager.IsModalOpen) return false;
            string name=null;
            GameObject stockTarget=null;
            if(tm.JourneyStep==6 && tm.DidPlaceBulkOrder) {
                foreach(var item in PlacedFurnitureController.AllPlacedFurniture) {
                    if(item==null || (item.FurnitureType!=FurnitureType.Shelf && item.FurnitureType!=FurnitureType.Fridge)) continue;
                    if(tm.StockedFurniture(item.FurnitureType)>=(item.FurnitureType==FurnitureType.Shelf?3:2)) continue;
                    bool complete=item.rows!=null && item.rows.Length>=4;
                    if(complete) foreach(var row in item.rows)
                        if(row==null || row.IsUnassigned) {complete=false;break;}
                    if(!complete) {stockTarget=item.gameObject;name="Stock_"+item.GetInstanceID();break;}
                }
            }
            if(tm.JourneyStep==7 && tm.FarmersReady && tm.SeedsBought && !tm.SeedsPlanted) name="Farmland_Soil_Plots_Grid";
            if(tm.JourneyStep==8 && LivestockManager.Instance!=null &&
                LivestockManager.Instance.TotalChickens>=6 && LivestockManager.Instance.TotalCows>=3)
                name=!tm.ChickenVisited ? "Chicken_Coop" : !tm.CowVisited ? "Cow_Barn" : null;
            if(tm.JourneyStep==9) {
                string[] stops={"Farm_Barn","Workshop_Complex","Taxi","East_Railway_Station","Building_Complex"};
                if(tm.TourStop<stops.Length) name=stops[tm.TourStop];
            }
            if(name==null) {focusedWorldKey=null;focusedWorld=null;return false;}
            string key=tm.JourneyStep+":"+name;
            if(focusedWorldKey!=key || focusedWorld==null) {
                GameObject target=stockTarget!=null ? stockTarget : GameObject.Find(name);
                if(name=="Taxi") {
                    var taxi=Object.FindFirstObjectByType<TaxiStandClickable>();
                    if(taxi!=null) target=taxi.gameObject;
                }
                if(target==null) return false;
                focusedWorld=target.transform;focusedWorldKey=key;
                Bounds b=WorldBounds(focusedWorld);
                if(journeyPan!=null) StopCoroutine(journeyPan);
                journeyPan=StartCoroutine(PanJourneyCamera(b.center));
            }
            Camera camera=Camera.main;
            if(camera==null || focusedWorld==null) return false;
            // Planting and information stops allow dragging and closing information windows.
            if(tm.JourneyStep==7 || (tm.JourneyStep==9 && (tm.TourStop>0 || tm.BarnVisited))) {
                spotlight.SetDimStrength(.25f);spotlight.SetTarget(new Rect(0,0,Screen.width,Screen.height),false);return true;
            }
            Bounds bounds=WorldBounds(focusedWorld);
            Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=-new Vector2(float.PositiveInfinity,float.PositiveInfinity);
            for(int i=0;i<8;i++) {
                Vector3 p=camera.WorldToScreenPoint(new Vector3((i&1)==0?bounds.min.x:bounds.max.x,(i&2)==0?bounds.min.y:bounds.max.y,(i&4)==0?bounds.min.z:bounds.max.z));
                if(p.z<=0) return false;
                min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
            spotlight.SetDimStrength(.78f);spotlight.SetTarget(Inflate(Rect.MinMaxRect(min.x,min.y,max.x,max.y),16),true,false);
            return true;
        }

        private static Bounds WorldBounds(Transform target)
        {
            var renderers=target.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0) return new Bounds(target.position,Vector3.one*5);
            Bounds b=renderers[0].bounds;
            for(int i=1;i<renderers.Length;i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private string TourSpeech(int stop)
        {
            string[] tr={
                "Ahır, tarladan ve hayvanlarından gelen ürünlerin deposudur. Ahıra dokunarak envanteri aç. Buradan ürünleri mağazaya, atölyeye veya tren istasyonuna gönderebilirsin. Bilgiyi okuduktan sonra pencereyi kapat ve Devam'a bas.",
                "Atölyede ham ürünlerini işleyerek daha değerli ürünler üretebilirsin. Makine, malzeme ve üretim kapasitesini birlikte planla. Hazırsan Devam'a bas.",
                "Taksi durağını satın alıp işletebilirsin. Taksiler kasabadaki yolcuları taşıyarak sana ek gelir sağlar. Filonu büyüterek işletmeni geliştirebilirsin. Hazırsan Devam'a bas.",
                "Tren istasyonuna gönderdiğin ürünler burada bekler. Günlük tren 12.30'da yükü alır, 13.00'te ayrılır; haritadan çıkınca satış geliri hesabına geçer. İstasyonda bekleyen ürün ve fiyatlarını görebilirsin. Devam'a bas.",
                "Mağazana geri döndük. Ekibin, reyonların ve çiftliğin hazır. Bir sonraki adımda kapıları müşterilerine açacaksın. Devam'a bas."
            };
            string[] en={
                "The barn stores crops and animal products. Tap it to open inventory. Send goods to the store, workshop or railway station. Close the window after reading, then press Continue.",
                "Workshops process raw materials into higher-value products. Plan machinery, ingredients and capacity together. Press Continue when ready.",
                "You can purchase and run the taxi stand. Your taxis earn extra income by transporting passengers around town. Expand your fleet to grow the business. Press Continue when ready.",
                "Dispatched goods wait at the station. The daily train collects them at 12:30 and departs at 13:00. Sales are paid when it leaves the map. The station lists waiting goods and prices. Press Continue.",
                "Back at your store! Your team, shelves and farm are ready. Next you will open the doors to your customers. Press Continue."
            };
            stop=Mathf.Clamp(stop,0,4);
            return LocalizationManager.L("MayorTour"+stop,tr[stop],en[stop]);
        }
    }
}
