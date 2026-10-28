using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    // A single outer scroll view; each card grows with its text and available width.
    public sealed class BankLoanDashboard : MonoBehaviour
    {
        private Font font;
        private BankLoanManager bank;
        private int level, fingerprint;
        private bool dirty=true, showResult;
        private float previousWidth, previousPixels, lastAction=-10;
        private string feedback="";
        private static Sprite rounded;
        private readonly List<(Button button, ActiveLoanData loan, Text hint)> payoffButtons=new List<(Button,ActiveLoanData,Text)>();
        private readonly Color ink=new Color(.93f,.96f,1), muted=new Color(.64f,.72f,.81f);
        private readonly Color surface=new Color(.08f,.115f,.16f), tile=new Color(.115f,.16f,.22f);
        private readonly Color mint=new Color(.3f,.88f,.69f), gold=new Color(1,.8f,.4f);
        private string L(string tr,string en)=>LocalizationManager.L("LoanDashboard_"+en,tr,en);
        private string Money(int amount)=>amount.ToString("N0")+" C";
        public void Bind(Font typeface,BankLoanManager manager,int storeLevel)
        {
            storeLevel=manager.CurrentOfferLevel;
            int hash=17;
            foreach(var loan in manager.GetActiveLoans())
                unchecked {hash=hash*31+loan.loanId.GetHashCode();hash=hash*31+loan.paidAmount;hash=hash*31+loan.remainingDays;}
            dirty|=bank!=manager || level!=storeLevel || fingerprint!=hash || font!=typeface;
            bank=manager;font=typeface;level=storeLevel;fingerprint=hash;
        }
        private void OnEnable(){dirty=true;}
        private void LateUpdate()
        {
            if(bank==null||font==null)return;
            if(level!=bank.CurrentOfferLevel)
            {
                level=bank.CurrentOfferLevel;
                feedback="";
                dirty=true;
            }
            var r=(RectTransform)transform;float width=r.rect.width,pixels=width*Mathf.Abs(r.lossyScale.x);
            if(width<100)return;
            if(dirty||Mathf.Abs(previousWidth-width)>.5f||Mathf.Abs(previousPixels-pixels)>1)
            {
                dirty=false;previousWidth=width;previousPixels=pixels;Draw(width,pixels<650||width<680);
                if(showResult)
                {
                    showResult=false;
                    Canvas.ForceUpdateCanvases();
                    var scroll=GetComponentInParent<ScrollRect>();
                    if(scroll!=null){scroll.StopMovement();scroll.verticalNormalizedPosition=1f;}
                }
            }
            int cash=EconomyManager.Instance?.Credits??0;
            foreach(var item in payoffButtons)
            {
                bool affordable=EconomyManager.Instance!=null&&cash>=item.loan.GetEarlyPayoffAmount();
                item.button.interactable=affordable;
                string text=affordable?L("Ödeyerek bu krediyi tamamen kapatabilirsin.","Pay this amount to settle the loan in full."):
                    L("Erken kapatma için bakiyen yetersiz.","Insufficient cash for early payoff.");
                if(item.hint.text!=text)item.hint.text=text;
            }
        }
        private RectTransform Rect(Transform parent,string name,float x,float y,float w,float h,Color? color=null)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(Mathf.Max(1,w),h);
            if(color.HasValue)
            {
                if(rounded==null)
                {
                    var original=UIStyleUtility.CreateRoundedPillSprite(64,64,12,Color.white);
                    rounded=Sprite.Create(original.texture,original.rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));
                }
                var image=go.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color.Value;image.raycastTarget=false;
            }
            return r;
        }
        private Text TextBlock(Transform parent,string text,float x,float y,float w,int size,Color color,bool bold=false,float minimum=26)
        {
            var rect=Rect(parent,"Text",x,y,w,minimum);var t=rect.gameObject.AddComponent<Text>();
            t.font=font;t.fontSize=size;t.text=text;t.color=color;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
            t.supportRichText=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
            t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(minimum,t.preferredHeight+4));
            return t;
        }
        private float Paragraph(Transform parent,string text,float x,float y,float w,int size,Color color,bool bold=false)
        {var t=TextBlock(parent,text,x,y,w,size,color,bold);return y+t.rectTransform.rect.height;}
        private float Metrics(Transform parent,float x,float y,float width,int columns,params (string title,string value,Color color)[] values)
        {
            const float gap=10;
            float cell=(width-(columns-1)*gap)/columns;
            for(int start=0;start<values.Length;start+=columns)
            {
                var cells=new List<RectTransform>();float rowHeight=82;
                for(int col=0;col<columns&&start+col<values.Length;col++)
                {
                    var v=values[start+col];var box=Rect(parent,"Metric",x+col*(cell+gap),y,cell,82,tile);cells.Add(box);
                    float end=Paragraph(box,v.title,12,8,cell-24,16,muted);
                    end=Paragraph(box,v.value,12,end+3,cell-24,22,v.color,true)+10;
                    rowHeight=Mathf.Max(rowHeight,end);
                }
                foreach(var box in cells)box.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,rowHeight);
                y+=rowHeight+gap;
            }
            return y;
        }
        private float ButtonRow(Transform parent,string label,float y,float width,Color color,Action action,out Button button)
        {
            var r=Rect(parent,"Action",16,y,width,48,color);var image=r.GetComponent<Image>();image.raycastTarget=true;
            button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.pressedColor=new Color(.65f,.8f,.85f);colors.disabledColor=new Color(.45f,.5f,.55f,.7f);button.colors=colors;
            button.onClick.AddListener(()=>
            {
                if(Time.unscaledTime-lastAction<.5f)return;
                lastAction=Time.unscaledTime;action();dirty=true;showResult=true;
            });
            var t=TextBlock(r,label,12,8,width-24,19,ink,true,32);t.alignment=TextAnchor.MiddleCenter;
            float h=Mathf.Max(48,t.rectTransform.rect.height+16);r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,h);
            return y+h;
        }
        private void Draw(float width,bool compact)
        {
            foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            payoffButtons.Clear();float w=width-32,y=0;
            y=Paragraph(transform,L("BANKA KREDİLERİ","BANK LOANS"),16,y,w,26,ink,true)+6;
            y=Paragraph(transform,L("İhtiyacına uygun finansmanı seç, ödemelerini tek yerden takip et.","Choose financing and track your repayments in one place."),16,y,w,18,muted)+16;
            var active=bank.GetActiveLoans();int debt=0,daily=0;
            foreach(var loan in active){debt+=loan.RemainingTotalRepayment;daily+=Mathf.Min(loan.dailyInstallment,loan.RemainingTotalRepayment);}
            y=Metrics(transform,16,y,w,compact?1:3,
                (L("Aktif kredi","Active loans"),active.Count.ToString(),ink),
                (L("Kalan toplam borç","Total remaining debt"),Money(debt),gold),
                (L("Günlük toplam taksit","Total daily payment"),Money(daily),mint))+6;
            y=Paragraph(transform,L("Taksitler her gece 00:00'da otomatik tahsil edilir. Erken kapatma tutarı, kalan borçtan ayrı gösterilir.","Payments are collected automatically at 00:00. Early payoff is shown separately from outstanding debt."),16,y,w,17,muted)+18;
            if(!string.IsNullOrEmpty(feedback))
                y=Paragraph(transform,feedback,16,y,w,19,mint,true)+18;
            y=Paragraph(transform,L("AKTİF KREDİLERİN","YOUR ACTIVE LOANS"),16,y,w,20,ink,true)+12;
            if(active.Count==0)
            {
                var empty=Rect(transform,"NoActiveLoans",16,y,w,70,surface);
                float h=Paragraph(empty,L("Henüz aktif kredin yok. Aşağıdaki teklifleri karşılaştırabilirsin.","No active loans. Compare the offers below."),16,14,w-32,18,muted)+14;
                empty.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,h);y+=h+20;
            }
            foreach(var loan in active)
            {
                var card=Rect(transform,"Loan_"+loan.loanId,16,y,w,100,surface);float inner=w-32,cy=14;
                cy=Paragraph(card,loan.LocalizedTitle,16,cy,inner,23,ink,true)+4;
                cy=Paragraph(card,L("Başlangıç: ","Started: ")+loan.startDateFormatted+L("  •  Kalan vade: ","  •  Remaining: ")+loan.remainingDays+L(" gün"," days"),16,cy,inner,17,muted)+12;
                cy=Metrics(card,16,cy,inner,compact?2:4,
                    (L("Çekilen tutar","Amount borrowed"),Money(loan.principalAmount),ink),
                    (L("Ödenen tutar","Paid so far"),Money(loan.paidAmount),mint),
                    (L("Kalan borç","Remaining debt"),Money(loan.RemainingTotalRepayment),gold),
                    (L("Günlük taksit","Daily installment"),Money(loan.dailyInstallment),ink));
                float progress=Mathf.Clamp01((float)loan.paidAmount/Mathf.Max(1,loan.totalRepayment));
                Rect(card,"PaymentTrack",16,cy,inner,6,tile);
                if(progress>0)Rect(card,"PaymentProgress",16,cy,inner*progress,6,mint);
                cy+=18;
                int payoff=loan.GetEarlyPayoffAmount();
                cy=Paragraph(card,L("Erken kapatma: ","Early payoff: ")+Money(payoff)+L("  •  Tasarruf: ","  •  Savings: ")+Money(Mathf.Max(0,loan.RemainingTotalRepayment-payoff)),16,cy,inner,20,mint,true)+10;
                cy=ButtonRow(card,L("ERKEN KAPAT","PAY OFF EARLY")+" • "+Money(payoff),cy,inner,new Color(.13f,.39f,.5f),()=>
                {
                    bool ok=bank.PayoffLoanEarly(loan);
                    feedback=ok?L("Kredi tamamen kapatıldı.","Loan fully settled."):L("Kredi kapatılamadı. Bakiyeni kontrol et.","Could not settle the loan. Check your balance.");
                },out Button payoffButton)+8;
                var hint=TextBlock(card,L("Erken kapatma için bakiyen yetersiz.","Insufficient cash for early payoff."),16,cy,inner,17,muted);
                // Reserve enough space for both balance states without rebuilding on each sale.
                float unavailableHeight=hint.rectTransform.rect.height;
                hint.text=L("Ödeyerek bu krediyi tamamen kapatabilirsin.","Pay this amount to settle the loan in full.");
                hint.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(54,Mathf.Max(unavailableHeight,hint.preferredHeight+4)));
                payoffButtons.Add((payoffButton,loan,hint));cy+=hint.rectTransform.rect.height+14;
                card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,cy);y+=cy+16;
            }
            y=Paragraph(transform,L("KREDİ TEKLİFLERİ • Seviye ","LOAN OFFERS • Level ")+level,16,y+8,w,20,ink,true)+12;
            foreach(var offer in bank.GetOffersForStoreLevel(level))
            {
                var card=Rect(transform,"Offer_"+offer.offerId,16,y,w,100,surface);float inner=w-32,cy=14;
                cy=Paragraph(card,offer.LocalizedTitle,16,cy,inner,23,ink,true)+4;
                cy=Paragraph(card,offer.LocalizedDescription,16,cy,inner,18,muted)+14;
                cy=Metrics(card,16,cy,inner,compact?2:4,
                    (L("Hesabına yatacak","You receive"),Money(offer.principalAmount),mint),
                    (L("Faiz oranı","Interest rate"),offer.interestRatePercent.ToString("0.#")+"%",ink),
                    (L("Günlük taksit","Daily installment"),Money(offer.dailyInstallment),ink),
                    (L("Toplam geri ödeme","Total repayment"),Money(offer.totalRepayment),gold));
                cy=Paragraph(card,L("Vade: ","Term: ")+offer.termDays+L(" gün  •  Toplam faiz: "," days  •  Total interest: ")+Money(offer.totalRepayment-offer.principalAmount),16,cy,inner,18,muted)+12;
                bool alreadyActive=bank.HasActiveLoanForOffer(offer);
                cy=ButtonRow(card,(alreadyActive?L("AKTİF KREDİN VAR","LOAN ALREADY ACTIVE"):L("KREDİ ÇEK","TAKE LOAN"))+" • "+Money(offer.principalAmount),cy,inner,new Color(.09f,.43f,.32f),()=>
                {
                    bool ok=bank.TakeLoan(offer);
                    feedback=ok?Money(offer.principalAmount)+L(" hesabına yatırıldı. Günlük taksit: "," credited. Daily payment: ")+Money(offer.dailyInstallment):L("Kredi işlemi tamamlanamadı.","Could not complete the loan request.");
                },out Button takeButton)+16;
                takeButton.interactable=EconomyManager.Instance!=null&&!alreadyActive;
                if(alreadyActive)
                    cy=Paragraph(card,L("Bu sırada aktif kredi borcun var. Önceki seviyeden kalan borçlar da bu sırayı kilitler. Borcu tamamen kapatınca bu teklif açılır; borcun olmayan diğer sıraları kullanabilirsin.","This loan slot has an outstanding balance, including loans from earlier levels. Repay it in full to unlock this offer. Other debt-free slots remain available."),16,cy,inner,17,muted)+16;
                card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,cy);y+=cy+16;
            }
            var root=(RectTransform)transform;root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,y+16);
            var layout=GetComponent<LayoutElement>();layout.minHeight=layout.preferredHeight=y+16;layout.flexibleHeight=0;
        }
    }
}
