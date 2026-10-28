using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    public sealed class RailwayFreightModalUI : MonoBehaviour
    {
        static RailwayFreightModalUI instance;
        public static bool IsOpen => instance != null && instance.gameObject.activeInHierarchy;
        readonly Color muted = new Color(.62f,.72f,.79f);
        readonly Color accent = new Color(.32f,.88f,.76f);
        RectTransform safeRoot, panel, content;
        Text title, subtitle, total, footer;
        readonly Text[] timetable = new Text[3];
        CanvasGroup group;
        ScrollRect scroll;
        Sprite rounded;
        float animationTime, refreshTime;
        bool closing;
        string lastManifest;

        public static void Show()
        {
            if (IsOpen) return;
            var go = new GameObject("Railway_Freight_Canvas");
            instance = go.AddComponent<RailwayFreightModalUI>();
            instance.Build();
            ModalManager.SetModalOpen(true);
        }
        public static void Close()
        {
            if (instance == null || instance.closing) return;
            instance.closing = true;
            instance.animationTime = 0;
        }
        void OnDestroy()
        {
            if (instance == this) { instance = null; ModalManager.SetModalOpen(false); }
            // The texture belongs to UIStyleUtility's shared cache; only this sliced sprite is ours.
            if (rounded != null) Destroy(rounded);
        }
        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin=min; r.anchorMax=max;
            r.offsetMin=r.offsetMax=Vector2.zero; return r;
        }
        Text Label(string name, Transform parent, Vector2 min, Vector2 max, int size, Color color)
        {
            var t=Rect(name,parent,min,max).gameObject.AddComponent<Text>();
            t.font=UIStyleUtility.GetGlobalFont(size); t.fontSize=size; t.color=color;
            t.raycastTarget=false; t.supportRichText=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;
            t.verticalOverflow=VerticalWrapMode.Truncate;
            t.resizeTextForBestFit=true; t.resizeTextMinSize=Mathf.Max(14,size-3); t.resizeTextMaxSize=size;
            t.alignment=TextAnchor.MiddleLeft; return t;
        }
        void Surface(RectTransform rect, Color color)
        {
            var img=rect.gameObject.AddComponent<Image>(); img.sprite=rounded;
            img.type=Image.Type.Sliced; img.color=color;
        }
        void Build()
        {
            var canvas=gameObject.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=960;
            var scaler=gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(960,540); scaler.matchWidthOrHeight=.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            var source=UIStyleUtility.CreateRoundedPillSprite(64,64,16,Color.white);
            rounded=Sprite.Create(source.texture,source.rect,new Vector2(.5f,.5f),100f,0,
                SpriteMeshType.FullRect,new Vector4(16,16,16,16));
            var back=Rect("Backdrop",transform,Vector2.zero,Vector2.one);
            back.gameObject.AddComponent<Image>().color=new Color(.015f,.025f,.04f,.78f);
            back.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            // Safe area is a sibling so panel taps never bubble to the backdrop button.
            safeRoot=Rect("SafeArea",transform,Vector2.zero,Vector2.one);
            panel=Rect("ShipmentWindow",safeRoot,new Vector2(.12f,.07f),new Vector2(.88f,.93f));
            Surface(panel,new Color(.07f,.10f,.15f));
            group=panel.gameObject.AddComponent<CanvasGroup>(); group.alpha=0;
            title=Label("Title",panel,new Vector2(.04f,.86f),new Vector2(.86f,.97f),25,Color.white); title.fontStyle=FontStyle.Bold;
            subtitle=Label("Subtitle",panel,new Vector2(.04f,.80f),new Vector2(.92f,.86f),15,muted);
            UIStyleUtility.CreateCornerCloseButton(panel,Close,42f);
            for(int i=0;i<3;i++)
            {
                float x=.04f+i*.31f;
                var card=Rect("Schedule_"+i,panel,new Vector2(x,.63f),new Vector2(x+.29f,.78f));
                Surface(card,new Color(.11f,.16f,.21f));
                timetable[i]=Label("Time",card,new Vector2(.08f,.08f),new Vector2(.94f,.94f),19,i==0?accent:Color.white);
            }
            var area=Rect("Scroll",panel,new Vector2(.04f,.20f),new Vector2(.96f,.60f));
            scroll=area.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false;
            scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=30; scroll.decelerationRate=.12f;
            var viewport=Rect("Viewport",area,Vector2.zero,new Vector2(.975f,1));
            viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,.01f);
            viewport.gameObject.AddComponent<RectMask2D>(); scroll.viewport=viewport;
            content=Rect("CargoCards",viewport,new Vector2(0,1),new Vector2(1,1)); content.pivot=new Vector2(.5f,1);
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing=8;
            layout.childControlWidth=true; layout.childControlHeight=true; layout.childForceExpandWidth=true; layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.content=content;
            var track=Rect("ScrollTrack",area,new Vector2(.985f,0),Vector2.one);
            track.gameObject.AddComponent<Image>().color=new Color(.13f,.19f,.24f);
            var handle=Rect("Handle",track,Vector2.zero,Vector2.one);
            var handleImage=handle.gameObject.AddComponent<Image>(); handleImage.color=accent;
            var bar=track.gameObject.AddComponent<Scrollbar>(); bar.handleRect=handle; bar.targetGraphic=handleImage;
            bar.direction=Scrollbar.Direction.BottomToTop; scroll.verticalScrollbar=bar;
            scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            var summary=Rect("Summary",panel,new Vector2(.04f,.09f),new Vector2(.96f,.18f));
            Surface(summary,new Color(.09f,.22f,.23f));
            total=Label("Total",summary,new Vector2(.025f,0),new Vector2(.975f,1),21,accent); total.fontStyle=FontStyle.Bold;
            footer=Label("Cutoff",panel,new Vector2(.04f,.015f),new Vector2(.96f,.08f),14,muted);
            Refresh();
        }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if(UnityEngine.InputSystem.Keyboard.current!=null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) Close();
#else
            if(Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
            animationTime+=Time.unscaledDeltaTime;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(animationTime/.24f));
            group.alpha=closing?1-t:t;
            panel.anchoredPosition=new Vector2(0,closing?-60*t:60*(t-1));
            if(closing && t>=1) { Destroy(gameObject); return; }
            var safe=Screen.safeArea;
            safeRoot.anchorMin=new Vector2(safe.xMin/Mathf.Max(1,Screen.width),safe.yMin/Mathf.Max(1,Screen.height));
            safeRoot.anchorMax=new Vector2(safe.xMax/Mathf.Max(1,Screen.width),safe.yMax/Mathf.Max(1,Screen.height));
            refreshTime+=Time.unscaledDeltaTime;
            if(!closing && refreshTime>=.3f) { refreshTime=0; Refresh(); }
        }
        static string L(string key,string tr,string en)=>LocalizationManager.L(key,tr,en);
        void Refresh()
        {
            title.text=L("RailWindowTitle","TREN İSTASYONU","TRAIN STATION");
            subtitle.text=L("RailWindowSubtitle","Sevkiyat merkezi • Her gün bir sefer","Freight terminal • One service daily");
            timetable[0].text="12:30\n"+L("RailCollectLabel","Yükleme","Collection");
            timetable[1].text="13:00\n"+L("RailDepartLabel","Hareket","Departure");
            timetable[2].text="13:30\n"+L("RailPayLabel","Haritadan çıkış / ödeme","Map exit / payment");
            total.text=L("RailWaitingValue","Bekleyen yük toplamı","Waiting cargo total")+"   "+RailFreightSales.Total(RailFreightSales.Waiting).ToString("N0")+" C";
            footer.text=L("RailCutoff","12:30'dan sonra gönderilen ürünler ertesi gün alınır.","Shipments sent after 12:30 are collected the next day.");
            var signature=new StringBuilder(title.text).Append(subtitle.text);
            foreach(var list in new[]{RailFreightSales.Waiting,RailFreightSales.Onboard})
            {
                signature.Append('|');
                foreach(var c in list) signature.Append(c.productId).Append(':').Append(c.quantity).Append(':').Append(c.unitPrice).Append(';');
            }
            string key=signature.ToString(); if(key==lastManifest)return; lastManifest=key;
            float position=scroll.verticalNormalizedPosition;
            foreach(Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            Section(L("RailWaitingHeader","İSTASYONDA BEKLEYENLER","WAITING AT STATION"),RailFreightSales.Waiting);
            if(RailFreightSales.Onboard.Count>0)
                Section(L("RailOnboardHeader","TRENDE • ÖDEME BEKLENİYOR","ON BOARD • PAYMENT PENDING"),RailFreightSales.Onboard);
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.verticalNormalizedPosition=position;
        }
        void Section(string heading,IReadOnlyList<RailCargo> cargo)
        {
            var header=Rect("Section",content,Vector2.zero,Vector2.one);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight=30;
            Label("Heading",header,Vector2.zero,Vector2.one,17,accent).text=heading;
            if(cargo.Count==0)
            {
                var empty=Rect("Empty",content,Vector2.zero,Vector2.one); empty.gameObject.AddComponent<LayoutElement>().preferredHeight=90;
                Surface(empty,new Color(.10f,.14f,.19f));
                Label("Message",empty,new Vector2(.04f,.08f),new Vector2(.96f,.92f),19,muted).text=
                    L("RailEmptyHint","Henüz sevkiyat yok.\nAhırdan ürün gönderdiğinde burada listelenir.","No shipments yet.\nSend products from your barn to see them here.");
            }
            foreach(var c in cargo)
            {
                var row=Rect("Cargo",content,Vector2.zero,Vector2.one); row.gameObject.AddComponent<LayoutElement>().preferredHeight=84;
                Surface(row,new Color(.10f,.15f,.20f));
                Label("Product",row,new Vector2(.035f,.45f),new Vector2(.72f,.94f),20,Color.white).text=ProductPassportService.ResolveProductDisplayName(c.productId);
                string unit=GardenSeedDatabase.GetSeedById(c.productId)!=null?"KG":L("Unit_Pieces","Adet","Pcs");
                Label("Quantity",row,new Vector2(.035f,.05f),new Vector2(.72f,.45f),16,muted).text=$"{c.quantity:N0} {unit} × {c.unitPrice:N0} C";
                var value=Label("Value",row,new Vector2(.73f,.1f),new Vector2(.965f,.9f),21,accent);
                value.alignment=TextAnchor.MiddleRight; value.text=$"{c.quantity*c.unitPrice:N0} C";
            }
        }
    }
}
