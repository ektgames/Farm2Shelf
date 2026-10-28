using System;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    public sealed class MayorWelcomeUI : MonoBehaviour
    {
        private static MayorWelcomeUI instance;
        public static bool IsOpen => instance != null;
        private Action accept, decline;
        private Text speech, nextLabel;
        private GameObject next, yes, no;
        private RectTransform safe;
        private Sprite portrait;
        private int page;
        private float elapsed;
        private string text;
        private bool closing;
        private readonly string[] tr = {
            "Kasabamıza hoş geldin! Ben belediye başkanıyım. Burada çiftçimizin emeği, esnafımızın dükkânı ve komşularımızın ihtiyaçları birbirine bağlıdır. Yeni mağazan bu hikâyenin önemli bir parçası olacak.",
            "Taksi durağımız kasabanın mahallelerini birbirine bağlar. Çift yönlü raylarıyla tren istasyonumuz ise ürünlerini uzak pazarlara ulaştırır. Her gün saat 12.30'da gelen yük treni, yarım saatlik duruşun ardından yoluna devam eder.",
            "Hastanemiz sağlığımızı, polis karakolumuz güvenliğimizi, itfaiyemiz de acil durumlarda kasabamızı korur. Evler, çiftlikler ve işletmeler bir arada yaşar; online market kontratlarıyla bu komşularımıza hizmet verebilirsin.",
            "Önce ekibini kuracak, raflarını yerleştirecek ve çiftliğinde ilk tohumlarını ekeceksin. Ardından mağazanı açıp kendi yolunu çizeceksin. Sana on görev boyunca eşlik etmemi ister misin? Eğitime katılabilir veya doğrudan serbest oyuna başlayabilirsin."
        };
        private readonly string[] en = {
            "Welcome to our town! I am the mayor. Our farmers' work, local shops and neighbours' needs are all connected. Your new store will become an important part of that story.",
            "Our taxi stand connects the neighbourhoods. The railway station, with tracks in both directions, takes your goods to distant markets. A freight train arrives daily at 12:30 and departs after a thirty-minute stop.",
            "Our hospital cares for our health, the police station keeps us safe, and the fire station protects the town in emergencies. Homes, farms and businesses live side by side. Online market contracts let you serve these neighbours.",
            "First you will build your team, install shelves and plant your first seeds. Then you will open your store and choose your own path. Would you like me to guide you through ten quests? Join the tutorial or go straight to free play."
        };

        public static void Show(Action onAccept, Action onDecline)
        {
            if (instance != null) Destroy(instance.gameObject);
            instance = new GameObject("Mayor_Welcome").AddComponent<MayorWelcomeUI>();
            instance.accept = onAccept; instance.decline = onDecline;
            instance.Build();
        }

        private void Build()
        {
            GameSimulationTime.SetPaused(GameSimulationTime.PauseReason.Welcome, true);
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 2100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540); scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            var shade = Box(transform, "Backdrop", Vector2.zero, Vector2.one, new Color(.025f,.04f,.065f,.94f));
            safe = Box(shade, "SafeArea", Vector2.zero, Vector2.one, Color.clear);
            safe.GetComponent<Image>().raycastTarget = false;
            var card = Box(safe, "WelcomeCard", new Vector2(.06f,.08f), new Vector2(.94f,.92f), new Color(.08f,.12f,.18f));
            var reveal = card.gameObject.AddComponent<UnityEngine.UI.Button>();
            reveal.transition = Selectable.Transition.None;
            reveal.onClick.AddListener(() => { if (!closing && text != null) elapsed = text.Length; });
            var imageRect = Box(card, "MayorPortrait", new Vector2(.02f,.05f), new Vector2(.34f,.89f), Color.white);
            var image = imageRect.GetComponent<Image>(); image.raycastTarget = false; image.preserveAspect = true;
            Texture2D texture = Resources.Load<Texture2D>("Tutorial/mayor_portrait");
            if (texture != null) { portrait = Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f); image.sprite = portrait; }
            Label(card, new Vector2(.36f,.82f),new Vector2(.96f,.96f),
                LocalizationManager.L("Mayor_Welcome_Title","BELEDİYE BAŞKANI","MAYOR"),24,new Color(1,.80f,.32f));
            speech = Label(card,new Vector2(.36f,.24f),new Vector2(.96f,.80f),"",21,Color.white);
            speech.alignment = TextAnchor.UpperLeft;
            next = Button(card,new Vector2(.64f,.06f),new Vector2(.96f,.20f),
                LocalizationManager.L("Mayor_Next","Devam","Continue"),()=> {
                    if (elapsed * 36 < text.Length) elapsed = text.Length;
                    else { page++; BeginPage(); }
                },out nextLabel);
            yes = Button(card,new Vector2(.36f,.04f),new Vector2(.65f,.21f),
                LocalizationManager.L("Mayor_Yes","Evet, eğitime katıl","Yes, join tutorial"),()=>Finish(true),out _);
            no = Button(card,new Vector2(.67f,.04f),new Vector2(.96f,.21f),
                LocalizationManager.L("Mayor_No","Hayır, eğitimi atla","No, skip tutorial"),()=>Finish(false),out _);
            BeginPage();
        }

        private void BeginPage()
        {
            page = Mathf.Clamp(page,0,tr.Length-1);
            text = LocalizationManager.L("Mayor_Welcome_Page"+page,tr[page],en[page]); elapsed=0;
            next.SetActive(page < tr.Length-1); yes.SetActive(false); no.SetActive(false);
        }
        private void Update()
        {
            if (safe == null) return;
            Rect r=Screen.safeArea;
            safe.anchorMin=new Vector2(r.xMin/Screen.width,r.yMin/Screen.height);
            safe.anchorMax=new Vector2(r.xMax/Screen.width,r.yMax/Screen.height);
            elapsed+=Time.unscaledDeltaTime;
            speech.text=text.Substring(0,Mathf.Min(text.Length,(int)(elapsed*36)));
            if (page==tr.Length-1 && elapsed*36>=text.Length) {yes.SetActive(true);no.SetActive(true);}
        }
        private void Finish(bool tutorial)
        {
            if (closing) return;
            closing=true; gameObject.SetActive(false);
            if (instance==this) instance=null;
            GameSimulationTime.SetPaused(GameSimulationTime.PauseReason.Welcome,false);
            if(tutorial) accept?.Invoke(); else decline?.Invoke();
            Destroy(gameObject);
        }
        private void OnDestroy()
        {
            if(instance==this) {instance=null;GameSimulationTime.SetPaused(GameSimulationTime.PauseReason.Welcome,false);}
            if(portrait!=null) Destroy(portrait);
        }
        private static RectTransform Box(Transform parent,string name,Vector2 min,Vector2 max,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            go.GetComponent<Image>().color=color;return r;
        }
        private static Text Label(Transform parent,Vector2 min,Vector2 max,string text,int size,Color color)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=new Vector2(8,4);r.offsetMax=new Vector2(-8,-4);
            var t=go.GetComponent<Text>();t.font=UIStyleUtility.GetGlobalFont(size);t.fontSize=size;t.text=text;t.color=color;
            t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=14;t.resizeTextMaxSize=size;
            return t;
        }
        private static GameObject Button(Transform parent,Vector2 min,Vector2 max,string title,UnityEngine.Events.UnityAction click,out Text label)
        {
            var r=Box(parent,"Choice",min,max,Color.white);
            r.GetComponent<Image>().sprite=UIStyleUtility.CreateRoundedPillSprite(240,70,14,new Color(.13f,.43f,.39f));
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.onClick.AddListener(click);
            label=Label(r,Vector2.zero,Vector2.one,title,18,Color.white);return r.gameObject;
        }
    }
}
