using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    public sealed class StockMarketDashboard : MonoBehaviour
    {
        private Font font;
        private StockMarketManager market;
        private string selected;
        private int quantity;
        private Action<string,int> selectionChanged;
        private bool dirty = true;
        private float lastTrade = -10f;
        private int dataHash, lastCredits = int.MinValue;
        private Text cashText, tradeHint;
        private Button buyButton;
        private float lastWidth, lastPixels;
        private string notice = "";
        private static Sprite rounded;
        private readonly Color ink = new Color(.92f,.95f,1f), muted = new Color(.59f,.67f,.77f);
        private readonly Color surface = new Color(.075f,.105f,.15f), raised = new Color(.115f,.155f,.21f);
        private readonly Color green = new Color(.22f,.86f,.64f), red = new Color(1f,.43f,.49f), blue = new Color(.35f,.72f,1f);
        private string L(string tr,string en) => LocalizationManager.L("StockDashboard_"+en,tr,en);
        public void Bind(Font typeface, StockMarketManager manager, string ticker, int count, Action<string,int> changed)
        {
            int hash = 17;
            foreach(var stock in manager.GetAllStocks())
            {
                unchecked { hash = hash * 31 + stock.currentPrice.GetHashCode(); hash = hash * 31 + stock.ownedShares;
                    hash = hash * 31 + stock.totalInvested.GetHashCode(); }
            }
            dirty |= market!=manager || selected!=ticker || quantity!=count || hash!=dataHash;
            dataHash=hash; font=typeface; market=manager; selected=ticker; quantity=Mathf.Max(1,count); selectionChanged=changed;
        }
        private void OnEnable()
        {
            if(EconomyManager.Instance!=null) EconomyManager.Instance.OnCreditsChanged+=MoneyChanged;
            dirty=true;
        }
        private void OnDisable()
        {
            if(EconomyManager.Instance!=null) EconomyManager.Instance.OnCreditsChanged-=MoneyChanged;
        }
        private void MoneyChanged(int credits) { lastCredits=int.MinValue; }
        private void LateUpdate()
        {
            if(market==null || font==null) return;
            var rect=(RectTransform)transform;
            float width=rect.rect.width;
            if(width<100) return;
            float pixels=width*Mathf.Abs(rect.lossyScale.x);
            if(dirty || Mathf.Abs(lastWidth-width)>.5f || Mathf.Abs(lastPixels-pixels)>1f)
            { dirty=false; lastWidth=width; lastPixels=pixels; Draw(width,pixels<650 || width<680); lastCredits=int.MinValue; }
            int credits=EconomyManager.Instance?.Credits??0;
            if(credits!=lastCredits)
            {
                lastCredits=credits;
                if(cashText!=null)cashText.text=credits.ToString("N0")+" C";
                var stock=market.GetStock(selected);
                if(buyButton!=null&&stock!=null)
                {
                    buyButton.interactable=EconomyManager.Instance!=null&&credits>=Mathf.RoundToInt(stock.currentPrice*quantity);
                    if(tradeHint!=null)tradeHint.text=TradeHint(stock,buyButton.interactable);
                }
            }
        }
        private RectTransform Box(Transform parent,string name,float x,float y,float w,float h,Color? color=null)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform; r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(Mathf.Max(1,w),h);
            if(color.HasValue)
            {
                if(rounded==null)
                {
                    var source=UIStyleUtility.CreateRoundedPillSprite(64,64,12,Color.white);
                    rounded=Sprite.Create(source.texture,source.rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));
                }
                var image=go.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color.Value;image.raycastTarget=false;
            }
            return r;
        }
        private Text Label(Transform parent,string value,float x,float y,float w,float h,int size,Color color,bool bold=false)
        {
            var r=Box(parent,"Label",x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;
            t.fontSize=size;t.resizeTextForBestFit=true;t.resizeTextMinSize=Mathf.Max(12,size-4);t.resizeTextMaxSize=size;
            t.supportRichText=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
            t.alignment=TextAnchor.MiddleLeft;t.color=color;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.raycastTarget=false;
            return t;
        }
        private Button ActionButton(Transform parent,string text,float x,float y,float w,float h,Color color,Action click,bool enabled=true)
        {
            var r=Box(parent,"Button",x,y,w,h,color);var image=r.GetComponent<Image>();image.raycastTarget=true;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.interactable=enabled;
            var colors=button.colors;colors.highlightedColor=new Color(1.1f,1.1f,1.1f);colors.pressedColor=new Color(.7f,.8f,.9f);colors.disabledColor=new Color(.4f,.45f,.5f,.7f);button.colors=colors;
            button.onClick.AddListener(()=>click());
            Label(r,text,8,4,w-16,h-8,18,ink,true).alignment=TextAnchor.MiddleCenter;
            return button;
        }
        private Text Metric(Transform parent,string title,string value,float x,float y,float w,Color color)
        {
            var r=Box(parent,"Metric",x,y,w,72,raised);
            Label(r,title,12,7,w-24,22,15,muted);
            return Label(r,value,12,31,w-24,32,22,color,true);
        }
        private void Draw(float width,bool compact)
        {
            foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var stocks=market.GetAllStocks();
            if(stocks==null || stocks.Count==0)
            {
                Label(transform,L("Piyasa verileri hazırlanıyor.","Loading market data."),16,16,width-32,64,22,muted);
                SetHeight(100);return;
            }
            var target=market.GetStock(selected)??stocks[0];selected=target.tickerSymbol;
            float value=0,profit=0;foreach(var s in stocks){value+=s.TotalCurrentValue;profit+=s.ProfitLoss;}
            float gap=12,pad=16,w=width-2*pad;
            Label(transform,L("BORSA","STOCK MARKET"),pad,0,w,32,25,ink,true);
            Label(transform,L("Fiyatlar oyun saatine göre her saat yenilenir.","Prices update every in-game hour."),pad,36,w,30,16,muted);
            float mw=(w-2*gap)/3;
            if(compact)
            {
                cashText=Metric(transform,L("Kullanılabilir bakiye","Available cash"),(EconomyManager.Instance?.Credits??0).ToString("N0")+" C",pad,78,w,ink);
                mw=(w-gap)/2;
                Metric(transform,L("Portföy değeri","Portfolio value"),value.ToString("N0")+" C",pad,162,mw,ink);
                Metric(transform,L("Açık kâr / zarar","Unrealized P/L"),(profit>=0?"+":"")+profit.ToString("N0")+" C",pad+mw+gap,162,mw,profit>=0?green:red);
            }
            else
            {
                cashText=Metric(transform,L("Kullanılabilir bakiye","Available cash"),(EconomyManager.Instance?.Credits??0).ToString("N0")+" C",pad,78,mw,ink);
                Metric(transform,L("Portföy değeri","Portfolio value"),value.ToString("N0")+" C",pad+mw+gap,78,mw,ink);
                Metric(transform,L("Açık kâr / zarar","Unrealized P/L"),(profit>=0?"+":"")+profit.ToString("N0")+" C",pad+2*(mw+gap),78,mw,profit>=0?green:red);
            }
            float top=compact?250:166;
            float listW=compact?w:Mathf.Clamp(w*.3f,220,290);
            Label(transform,L("PİYASA • Hisse seç","MARKET • Select a stock"),pad,top,listW,30,17,muted,true);
            int columns=compact&&w>=480?2:1;
            float itemW=(listW-gap*(columns-1))/columns;
            for(int i=0;i<stocks.Count;i++)
            {
                var stock=stocks[i];float x=pad+(i%columns)*(itemW+gap),y=top+40+(i/columns)*110;
                var card=Box(transform,"Stock_"+stock.tickerSymbol,x,y,itemW,100,stock==target?new Color(.12f,.24f,.33f):surface);
                var image=card.GetComponent<Image>();image.raycastTarget=true;
                var button=card.gameObject.AddComponent<Button>();button.targetGraphic=image;
                button.onClick.AddListener(()=>{selected=stock.tickerSymbol;notice="";selectionChanged?.Invoke(selected,quantity);dirty=true;});
                Box(card,"Selection",0,12,3,76,stock==target?blue:raised);
                Label(card,stock.tickerSymbol,14,7,itemW*.42f,24,19,ink,true);
                var change=Label(card,stock.PriceChangePercent.ToString("+0.00;-0.00;0.00")+"%",itemW*.48f,7,itemW*.52f-14,24,17,stock.PriceChangePercent>=0?green:red,true);change.alignment=TextAnchor.MiddleRight;
                Label(card,stock.LocalizedCompanyName,14,34,itemW-28,30,15,muted);
                Label(card,stock.currentPrice.ToString("N2")+" C",14,68,itemW-28,25,19,ink,true);
            }
            float detailY=compact?top+50+Mathf.CeilToInt(stocks.Count/(float)columns)*110:top;
            float detailX=compact?pad:pad+listW+gap,detailW=compact?w:w-listW-gap;
            var detail=Box(transform,"SelectedStock",detailX,detailY,detailW,710,surface);
            float inner=detailW-32;
            Label(detail,target.tickerSymbol+"  /  "+target.LocalizedCompanyName,16,12,inner,52,22,ink,true);
            Label(detail,target.LocalizedCategory,16,64,inner,38,16,muted);
            Label(detail,target.currentPrice.ToString("N2")+" C",16,108,inner*.55f,40,29,ink,true);
            Label(detail,L("Son saat ","Last hour ")+target.PriceChangePercent.ToString("+0.00;-0.00;0.00")+"%",16+inner*.57f,108,inner*.43f,40,17,target.PriceChangePercent>=0?green:red);
            Label(detail,L("FİYAT GEÇMİŞİ • Son 24 kayıt","PRICE HISTORY • Last 24 samples"),16,158,inner,28,15,muted,true);
            var chart=Box(detail,"PriceChart",16,192,inner,140,new Color(.045f,.07f,.11f));
            var curve=Box(chart,"Curve",8,8,inner-16,124);
            // MaskableGraphic registers with the viewport mask in OnEnable.
            // Its renderer must exist before the graphic is added/enabled.
            curve.gameObject.AddComponent<CanvasRenderer>();
            var graphic=curve.gameObject.AddComponent<StockHistoryGraphic>();
            graphic.SetHistory(target.priceHistory,target.PriceChangePercent>=0?green:red);
            float low=target.currentPrice,high=low;
            foreach(float p in target.priceHistory ?? new List<float>()){low=Mathf.Min(low,p);high=Mathf.Max(high,p);}
            Label(detail,L("Düşük: ","Low: ")+low.ToString("N2")+" C",16,338,inner*.5f,26,15,muted);
            Label(detail,L("Yüksek: ","High: ")+high.ToString("N2")+" C",16+inner*.5f,338,inner*.5f,26,15,muted).alignment=TextAnchor.MiddleRight;
            float half=(inner-gap)/2;
            Metric(detail,L("Sahip olduğun hisse","Shares owned"),target.ownedShares.ToString("N0"),16,378,half,ink);
            Metric(detail,L("Ortalama alış","Average cost"),target.averageBuyPrice.ToString("N2")+" C",16+half+gap,378,half,ink);
            Label(detail,L("Bu hissedeki kâr / zarar: ","Position P/L: ")+(target.ProfitLoss>=0?"+":"")+target.ProfitLoss.ToString("N0")+" C ("+target.ProfitLossPercent.ToString("+0.0;-0.0;0.0")+"%)",16,458,inner,36,17,target.ProfitLoss>=0?green:red,true);
            Label(detail,L("İŞLEM ADEDİ","NUMBER OF SHARES"),16,506,inner,24,15,muted,true);
            int[] options={1,10,50,100,500};float qw=(inner-4*8)/5;
            for(int i=0;i<options.Length;i++)
            {
                int q=options[i];ActionButton(detail,q.ToString(),16+i*(qw+8),536,qw,44,q==quantity?new Color(.14f,.4f,.6f):raised,()=>{quantity=q;selectionChanged?.Invoke(selected,q);dirty=true;});
            }
            int total=Mathf.RoundToInt(target.currentPrice*quantity);
            Label(detail,quantity.ToString("N0")+L(" hisse • İşlem tutarı: "," shares • Trade value: ")+total.ToString("N0")+" C",16,588,inner,32,18,ink,true);
            bool canBuy=EconomyManager.Instance!=null&&EconomyManager.Instance.Credits>=total;
            bool canSell=target.ownedShares>=quantity;
            buyButton=ActionButton(detail,L("HİSSE AL","BUY SHARES"),16,630,half,48,new Color(.09f,.46f,.34f),()=>Trade(true),canBuy);
            ActionButton(detail,L("HİSSE SAT","SELL SHARES"),16+half+gap,630,half,48,new Color(.55f,.20f,.29f),()=>Trade(false),canSell);
            tradeHint=Label(transform,TradeHint(target,canBuy),detailX,detailY+722,detailW,60,17,muted);
            SetHeight(Mathf.Max(detailY+800,top+40+stocks.Count*110));
        }
        private string TradeHint(StockData stock,bool canBuy)
        {
            if(!string.IsNullOrEmpty(notice)) return notice;
            string hint=(!canBuy?L("Alım için bakiye yetersiz. ","Insufficient cash to buy. "):"")+
                (stock.ownedShares<quantity?L("Satış için yeterli hissen yok.","Not enough shares to sell."):"");
            return string.IsNullOrEmpty(hint)?L("Adet seç, ardından al veya sat. İşlem güncel fiyatla yapılır.","Choose an amount, then buy or sell at the current price."):hint;
        }
        private void Trade(bool buy)
        {
            if(Time.unscaledTime-lastTrade<.4f)return;lastTrade=Time.unscaledTime;
            bool ok=buy?market.BuyShares(selected,quantity):market.SellShares(selected,quantity);
            notice=ok?L("İşlem tamamlandı. Portföyün güncellendi.","Trade complete. Your portfolio is updated."):L("İşlem yapılamadı. Bakiyeni ve hisse adedini kontrol et.","Trade failed. Check your cash and share balance.");
            dirty=true;

        }
        private void SetHeight(float height)
        {
            var r=(RectTransform)transform;r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            var layout=GetComponent<LayoutElement>()??gameObject.AddComponent<LayoutElement>();layout.minHeight=layout.preferredHeight=height;layout.flexibleHeight=0;
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StockHistoryGraphic : MaskableGraphic
    {
        private List<float> history; private Color stroke;
        public void SetHistory(List<float> values,Color tint){history=values;stroke=tint;raycastTarget=false;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=GetPixelAdjustedRect();
            void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
            {
                int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddVert(d,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
            for(int i=0;i<4;i++){float y=r.yMin+r.height*i/3;Quad(new Vector2(r.xMin,y),new Vector2(r.xMax,y),new Vector2(r.xMax,y+1),new Vector2(r.xMin,y+1),new Color(.3f,.4f,.5f,.22f));}
            if(history==null||history.Count<2)return;
            float min=history[0],max=min;foreach(float p in history){min=Mathf.Min(min,p);max=Mathf.Max(max,p);}
            float span=Mathf.Max(1,max-min);min-=span*.1f;max+=span*.1f;
            Vector2 Point(int i)=>new Vector2(r.xMin+r.width*i/(history.Count-1),r.yMin+6+(r.height-12)*(history[i]-min)/(max-min));
            for(int i=1;i<history.Count;i++)
            {
                Vector2 a=Point(i-1),b=Point(i),d=(b-a).normalized,n=new Vector2(-d.y,d.x)*1.5f;
                Quad(new Vector2(a.x,r.yMin),new Vector2(b.x,r.yMin),b,a,new Color(stroke.r,stroke.g,stroke.b,.13f));
                Quad(a-n,b-n,b+n,a+n,stroke);
            }
        }
    }
}
