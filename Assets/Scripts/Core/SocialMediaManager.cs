using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    public enum TweetSentiment
    {
        Complaint = -1,
        Neutral = 0,
        Praise = 1,
        Official = 2
    }

    [Serializable]
    public class SocialCommentData
    {
        public string commentId;
        public string authorName;
        public string authorHandle;
        public string avatarEmoji;
        public Color avatarBgColor;
        public string textTr;
        public string textEn;
        public string timeAgoTr;
        public string timeAgoEn;
        public int likesCount;

        public string LocalizedText => LocalizationManager.L("CommentText_" + commentId, textTr, textEn);
        public string LocalizedTime => LocalizationManager.L("CommentTime_" + commentId, timeAgoTr, timeAgoEn);

        public SocialCommentData() { }

        public SocialCommentData(string commentId, string authorName, string authorHandle, string avatarEmoji, Color avatarBgColor, string textTr, string textEn, string timeAgoTr, string timeAgoEn, int likesCount)
        {
            this.commentId = commentId;
            this.authorName = authorName;
            this.authorHandle = authorHandle;
            this.avatarEmoji = avatarEmoji;
            this.avatarBgColor = avatarBgColor;
            this.textTr = textTr;
            this.textEn = textEn;
            this.timeAgoTr = timeAgoTr;
            this.timeAgoEn = timeAgoEn;
            this.likesCount = likesCount;
        }
    }

    [Serializable]
    public class SocialTweetData
    {
        public string tweetId;
        public string authorName;
        public string authorHandle;
        public string avatarEmoji;
        public Color avatarBgColor;
        public bool isVerified;
        public bool isPlayerTweet;

        public string tweetTextTr;
        public string tweetTextEn;

        public string timeAgoTr;
        public string timeAgoEn;

        public int likesCount;
        public int retweetsCount;
        public bool isLikedByPlayer;
        public bool isRetweetedByPlayer;
        public TweetSentiment sentiment;

        public List<SocialCommentData> comments = new List<SocialCommentData>();

        public string LocalizedText => LocalizationManager.L("TweetText_" + tweetId, tweetTextTr, tweetTextEn);
        public string LocalizedTime => LocalizationManager.L("TweetTime_" + tweetId, timeAgoTr, timeAgoEn);

        public SocialTweetData() { }

        public SocialTweetData(string tweetId, string authorName, string authorHandle, string avatarEmoji, Color avatarBgColor, bool isVerified, bool isPlayerTweet, string tweetTextTr, string tweetTextEn, string timeAgoTr, string timeAgoEn, int likesCount, int retweetsCount, TweetSentiment sentiment)
        {
            this.tweetId = tweetId;
            this.authorName = authorName;
            this.authorHandle = authorHandle;
            this.avatarEmoji = avatarEmoji;
            this.avatarBgColor = avatarBgColor;
            this.isVerified = isVerified;
            this.isPlayerTweet = isPlayerTweet;
            this.tweetTextTr = tweetTextTr;
            this.tweetTextEn = tweetTextEn;
            this.timeAgoTr = timeAgoTr;
            this.timeAgoEn = timeAgoEn;
            this.likesCount = likesCount;
            this.retweetsCount = retweetsCount;
            this.isLikedByPlayer = false;
            this.isRetweetedByPlayer = false;
            this.sentiment = sentiment;
            this.comments = new List<SocialCommentData>();
        }
    }

    /// <summary>
    /// Farm2Shelf Dinamik Sosyal Medya (Chirper / X Feed) Yöneticisi.
    /// Gün içinde markete gelen gerçek müşterilerin deneyimlerine göre iyi/kötü twitler ve yorumlar üretir,
    /// Oyuncunun attığı twitlerin altına dinamik yorumlar getirir,
    /// Takipçi sayısını (başlangıç: 500) performansa göre artırır/azaltır ve her gün değişen gündem başlıklarını yönetir.
    /// </summary>
    public class SocialMediaManager : MonoBehaviour
    {
        private static SocialMediaManager instance;
        public static SocialMediaManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("SocialMediaManager");
                    instance = obj.AddComponent<SocialMediaManager>();
                }
                return instance;
            }
        }

        // Başlangıç takipçi sayısı her zaman 500 ile başlar!
        private int followerCount = 500;
        private List<SocialTweetData> tweetFeed = new List<SocialTweetData>();
        public event Action OnFeedUpdated;

        private int cachedDayForTrends = -1;
        private string cachedTrendsTextTr = "";
        private string cachedTrendsTextEn = "";

        public int FollowerCount => followerCount;
        public List<SocialTweetData> GetTweetFeed() => tweetFeed;

        public void RestoreSocialMediaData(int followers, List<SocialTweetData> feed)
        {
            this.followerCount = Mathf.Max(0, followers > 0 ? followers : 500);
            if (feed != null && feed.Count > 0)
            {
                this.tweetFeed.Clear();
                this.tweetFeed.AddRange(feed);
            }
            OnFeedUpdated?.Invoke();
        }

        private int lastProcessedHour = -1;
        private int lastProcessedDay = -1;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDefaultTweets();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
                TimeManager.Instance.OnTimeUpdated += HandleTimeUpdated;
                TimeManager.Instance.OnNewDayStarted -= HandleNewDayStarted;
                TimeManager.Instance.OnNewDayStarted += HandleNewDayStarted;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
                TimeManager.Instance.OnNewDayStarted -= HandleNewDayStarted;
            }
        }

        private void HandleNewDayStarted(TimeManager.Season season, int day, int year)
        {
            // Yeni gün başladığında saatlik takip değişkenlerini sıfırla
            lastProcessedHour = -1;
            lastProcessedDay = day;
        }

        private void HandleTimeUpdated(int hour, int minute)
        {
            int currentDay = (TimeManager.Instance != null) ? TimeManager.Instance.Day : 1;

            // Sabah 08:00 ile Gece 24:00 arasında her saat başı twit akışını güncelle
            if (hour >= 8 && hour <= 24)
            {
                if (hour != lastProcessedHour || currentDay != lastProcessedDay)
                {
                    lastProcessedHour = hour;
                    lastProcessedDay = currentDay;
                    PublishHourlyCommunityTweet(hour);
                }
            }
        }

        private void PublishHourlyCommunityTweet(int hour)
        {
            string storeName = GetStoreName();
            string recId = "TWT_H_" + hour + "_" + UnityEngine.Random.Range(1000, 9999);
            string timeStrTr = $"Saat {hour:D2}:00";
            string timeStrEn = $"{hour:D2}:00";

            string authorName;
            string handle;
            string emoji;
            Color avatarColor;
            string tweetTr;
            string tweetEn;
            int likes = UnityEngine.Random.Range(20, 180);
            int rts = UnityEngine.Random.Range(5, 55);

            if (hour >= 8 && hour <= 10)
            {
                // SABAH KUŞAĞI (08:00 - 10:00)
                var morningPool = new (string name, string handle, string emoji, Color color, string tr, string en)[]
                {
                    ("Selin Çetin", "@selin_c", "🥖", new Color(0.95f, 0.60f, 0.20f), $"Sabah sabah @{storeName} fırınından çıkan sıcacık ekmek kokusu tüm sokağı sarmış, günaydın! 🥖☀️", $"Warm bread scent from @{storeName} bakery filled the whole street, good morning! 🥖☀️"),
                    ("Ozan Parlak", "@ozanparlak", "🍊", new Color(0.95f, 0.55f, 0.20f), "Güne taze sıkılmış portakal suyu ve çıtır simitle başlamak gibisi yok 🍊🥐", "Nothing beats starting the day with fresh orange juice and crisp bagels 🍊🥐"),
                    ("Derya Can", "@deryacan", "🥛", new Color(0.40f, 0.70f, 0.95f), $"Sabah yürüyüşü dönüşü @{storeName} uğrayıp taze süt ve yumurta aldım, kahvaltı hazır 🥛🍳", $"Stopped by @{storeName} after my morning walk for milk and eggs, breakfast ready 🥛🍳"),
                    ("Cemil Usta", "@cemilusta", "🍉", new Color(0.85f, 0.30f, 0.45f), "Sabahın ilk ışıklarında manav reyonu pırıl pırıl parlıyor, bereketi bol olsun 🌾✨", "Produce aisle sparkling bright in morning light, wishing abundant sales 🌾✨")
                };
                int idx = UnityEngine.Random.Range(0, morningPool.Length);
                var p = morningPool[idx];
                authorName = p.name; handle = p.handle; emoji = p.emoji; avatarColor = p.color; tweetTr = p.tr; tweetEn = p.en;
            }
            else if (hour >= 11 && hour <= 14)
            {
                // ÖĞLE KUŞAĞI (11:00 - 14:00)
                var noonPool = new (string name, string handle, string emoji, Color color, string tr, string en)[]
                {
                    ("Buket Akın", "@buketakin", "🥑", new Color(0.45f, 0.75f, 0.35f), $"Öğle molasında @{storeName} reyonlarından taze meyve ve atıştırmalık kaptım, süper hızlıydı ⚡🍎", $"Grabbed fresh fruits and snacks from @{storeName} during lunch break, super fast ⚡🍎"),
                    ("Mert Yılmaz", "@mertyilmaz", "🏎️", new Color(0.25f, 0.75f, 0.50f), "Market arabasını iki elle tutup Formula 1 pilotu gibi reyonlar arasında süzülenler 🏎️🛒", "Steering the shopping cart with both hands like a Formula 1 driver through aisles 🏎️🛒"),
                    ("Gizem Ünal", "@gizemunal", "🥗", new Color(0.30f, 0.80f, 0.50f), $"Organik salata malzemelerini @{storeName} üzerinden taze taze tamamladık 🥗🥑", $"Got all our crisp organic salad ingredients fresh from @{storeName} 🥗🥑"),
                    ("Tolga Ersoy", "@tolgaersoy", "🥒", new Color(0.35f, 0.80f, 0.40f), "Salatalığın çıtırlığı ve domatesin kokusu resmen öğle yemeğimi şölene çevirdi 🥒🍅", "Crunchy cucumbers and sweet tomato aroma turned lunch into a feast 🥒🍅")
                };
                int idx = UnityEngine.Random.Range(0, noonPool.Length);
                var p = noonPool[idx];
                authorName = p.name; handle = p.handle; emoji = p.emoji; avatarColor = p.color; tweetTr = p.tr; tweetEn = p.en;
            }
            else if (hour >= 15 && hour <= 17)
            {
                // İKİNDİ KUŞAĞI (15:00 - 17:00)
                var afternoonPool = new (string name, string handle, string emoji, Color color, string tr, string en)[]
                {
                    ("Hande Çam", "@handecam", "🧁", new Color(0.90f, 0.45f, 0.75f), $"İkindi çayının yanına taze kurabiye ve meyve almak için @{storeName} tek adresim ☕🍪", $"My only stop for fresh afternoon tea cookies and fruit is @{storeName} ☕🍪"),
                    ("Barış Korkmaz", "@bariskorkmaz", "🏷️", new Color(0.95f, 0.80f, 0.10f), "Kasadaki indirimli sarı etiketleri görünce gözleri parlayanlar derneği 🏷️✨", "Society of shoppers whose eyes light up seeing bright yellow discount tags 🏷️✨"),
                    ("Tuna Aydın", "@tunaaydin", "🐠", new Color(0.20f, 0.65f, 0.90f), "Girişteki devasa akvaryuma bakarken 10 dakikamı kaybettim ama kesinlikle değdi 🐠💙", "Lost 10 minutes admiring the huge store aquarium, totally worth it 🐠💙"),
                    ("Banu Erdem", "@banuerdem", "☕", new Color(0.65f, 0.45f, 0.35f), "Kahve otomatından taze çekilmiş kahve alıp reyonları gezmek tam bir terapi ☕🎶", "Grabbing fresh grounded coffee from the machine and browsing shelves is pure therapy ☕🎶")
                };
                int idx = UnityEngine.Random.Range(0, afternoonPool.Length);
                var p = afternoonPool[idx];
                authorName = p.name; handle = p.handle; emoji = p.emoji; avatarColor = p.color; tweetTr = p.tr; tweetEn = p.en;
            }
            else if (hour >= 18 && hour <= 20)
            {
                // AKŞAM KUŞAĞI (18:00 - 20:00)
                var eveningPool = new (string name, string handle, string emoji, Color color, string tr, string en)[]
                {
                    ("Kerem Pala", "@kerempala", "🚗", new Color(0.20f, 0.55f, 0.85f), $"İş çıkışı @{storeName} otoparkına yanaştım, akşam yemeği için taze sebzelerimizi aldık 🚗🥦", $"Parked at @{storeName} after work, grabbed fresh vegetables for dinner 🚗🥦"),
                    ("Ece Güneş", "@ecegunes", "🛍️", new Color(0.90f, 0.40f, 0.50f), $"Akşam trafiğinden kaçıp @{storeName} dükkanında huzurlu bir alışveriş turu attım 🛍️🌾", $"Escaped evening traffic and enjoyed a peaceful grocery run at @{storeName} 🛍️🌾"),
                    ("Oğuz Yıldırım", "@oguzyildirim", "🍇", new Color(0.55f, 0.35f, 0.75f), "Meyve reyonundaki üzüm ve elmalar resmen parıldıyor, akşama ziyafet var 🍇🍎", "Grapes and apples in the fruit section are literally sparkling, feast tonight 🍇🍎"),
                    ("Alper Tunç", "@alpertunc", "⚡", new Color(0.50f, 0.65f, 0.80f), $"Akşam yoğunluğuna rağmen @{storeName} kasaları çok seri çalıştı, helal olsun ⚡👏", $"Despite the evening rush, @{storeName} checkout lines were super smooth ⚡👏")
                };
                int idx = UnityEngine.Random.Range(0, eveningPool.Length);
                var p = eveningPool[idx];
                authorName = p.name; handle = p.handle; emoji = p.emoji; avatarColor = p.color; tweetTr = p.tr; tweetEn = p.en;
            }
            else
            {
                // GECE KUŞAĞI (21:00 - 24:00)
                var nightPool = new (string name, string handle, string emoji, Color color, string tr, string en)[]
                {
                    ("Sibel Varol", "@sibelvarol", "🍦", new Color(0.95f, 0.45f, 0.70f), $"Gece gece tatlı krizi tutup @{storeName} dondurma dolabının önünde nöbete durmak 🍦🌙", $"Late night sweet craving standing guard at @{storeName} ice cream freezer 🍦🌙"),
                    ("Kaan Arda", "@kaanarda", "🧊", new Color(0.40f, 0.60f, 0.85f), "Gece 02:00'de mutfakta buzdolabının kapağını açıp hiçbir şey almadan bakanlar kulübü 🧊👀", "The 2 AM club opening the fridge door and staring inside for 5 minutes without taking anything 🧊👀"),
                    ("Melis Doğu", "@melisdogu", "🛒", new Color(0.90f, 0.75f, 0.30f), $"Kapanışa doğru sakin sakin alışveriş yapmanın huzuru bambaşka @{storeName} 🛒✨", $"The peaceful tranquility of quiet grocery shopping near closing time at @{storeName} 🛒✨"),
                    ("Yasin Duru", "@yasinduru", "🌙", new Color(0.35f, 0.65f, 0.75f), $"Saat 24:00 oldu, günün son poşetini de açtık. @{storeName} ekibine iyi geceler! 🌙😴", $"Midnight arrives, packed the last bag. Good night to the @{storeName} team! 🌙😴")
                };
                int idx = UnityEngine.Random.Range(0, nightPool.Length);
                var p = nightPool[idx];
                authorName = p.name; handle = p.handle; emoji = p.emoji; avatarColor = p.color; tweetTr = p.tr; tweetEn = p.en;
            }

            SocialTweetData hourlyTweet = new SocialTweetData(
                recId,
                authorName,
                handle,
                emoji,
                avatarColor,
                false,
                false,
                tweetTr,
                tweetEn,
                timeStrTr,
                timeStrEn,
                likes,
                rts,
                TweetSentiment.Neutral
            );

            // 1 adet destekleyici/yorumlayıcı alt yanıt
            hourlyTweet.comments.Add(new SocialCommentData(
                $"CMT_{recId}_1", "Deniz Acar", "@denizacar", "✨", new Color(0.20f, 0.65f, 0.90f),
                "Kesinlikle öyle, günün her saati ayrı bir keyif ✨👍",
                "Totally agree, a unique vibe at every hour of the day ✨👍",
                timeStrTr, timeStrEn, UnityEngine.Random.Range(4, 18)
            ));

            tweetFeed.Insert(0, hourlyTweet);

            if (tweetFeed.Count > 50)
            {
                tweetFeed.RemoveAt(tweetFeed.Count - 1);
            }

            OnFeedUpdated?.Invoke();
        }

        private void InitializeDefaultTweets()
        {
            string playerFullName = GetPlayerFullName();
            string storeName = GetStoreName();
            string playerHandle = GetPlayerHandle();

            if (tweetFeed.Count == 0)
            {
                SocialTweetData welcomeTweet = new SocialTweetData(
                    "TWT_101",
                    playerFullName,
                    playerHandle,
                    "👨‍🌾",
                    new Color(0.20f, 0.70f, 0.95f),
                    true,
                    true,
                    $"🚀 Marketimizin kapıları açıldı! Taze çiftlik mahsullerimizle hizmetinizdeyiz. Hepinizi @{storeName} bekliyoruz! 🌾🛒",
                    $"🚀 Our store is officially open! Fresh farm crops served daily. Welcome to @{storeName}! 🌾🛒",
                    "Az önce",
                    "Just now",
                    48,
                    16,
                    TweetSentiment.Official
                );

                welcomeTweet.comments.Add(new SocialCommentData(
                    "CMT_101_1", "Elif Kaya", "@elifkaya", "👩‍💼", new Color(0.95f, 0.40f, 0.60f),
                    "Hayırlı olsun! Domatesler ve taze meyveler harika görünüyor, kesinlikle uğrayacağım 😍",
                    "Congratulations! The tomatoes and fresh fruits look amazing, I'll definitely stop by 😍",
                    "1 dk önce", "1m ago", 18
                ));

                welcomeTweet.comments.Add(new SocialCommentData(
                    "CMT_101_2", "Can Özkan", "@canozkan", "🧔", new Color(0.30f, 0.60f, 0.90f),
                    "Mahallemize böyle taze bir market lazımdı. Fiyatlar da uygunsa süper olur 👏",
                    "Our neighborhood needed a fresh market like this. If prices are reasonable, it's perfect 👏",
                    "Az önce", "Just now", 12
                ));

                welcomeTweet.comments.Add(new SocialCommentData(
                    "CMT_101_3", "Seda Yılmaz", "@sedayilmaz", "👱‍♀️", new Color(0.90f, 0.50f, 0.20f),
                    "Akşam iş çıkışı gelip manav reyonuna bakacağım, hayırlı kazançlar! 🛒✨",
                    "Will visit after work to check out the produce section, best of luck! 🛒✨",
                    "Az önce", "Just now", 9
                ));

                tweetFeed.Add(welcomeTweet);
            }

            // 20 Adet Dünya & Süpermarket Gündemiyle Alakalı Esprili / Mizahi Twitleri Ekle
            AddWittyCommunityTweets();
        }

        private void AddWittyCommunityTweets()
        {
            // Eğer zaten esprili twitler eklenmişse tekrar ekleme
            if (tweetFeed.Exists(t => t.tweetId.StartsWith("TWT_WITTY_"))) return;

            var wittyList = new (string name, string handle, string emoji, Color color, string tr, string en, int likes, int rts, string timeTr, string timeEn)[]
            {
                ("Selin Çetin", "@selin_c", "🥖", new Color(0.95f, 0.60f, 0.20f), "Markete sadece ekmek almaya girip 4 poşet abur cuburla çıkan tek ben miyim? 🥖🛒😂", "Am I the only one who enters a store just for bread and leaves with 4 bags of snacks? 🥖🛒😂", 142, 38, "12 dk önce", "12m ago"),
                ("Emre Tekin", "@emretekin", "🏎️", new Color(0.30f, 0.70f, 0.90f), "Bozuk tekerlekli alışveriş arabası beni marketin içinde drift yaptırtmaya zorluyor 🏎️🛒", "The shopping cart with a squeaky wheel is forcing me to drift through the aisles 🏎️🛒", 98, 24, "18 dk önce", "18m ago"),
                ("Zeynep Koç", "@zeynep_koc", "🪙", new Color(0.85f, 0.40f, 0.65f), "Kasadaki kişi '1 kuruşunuz var mı?' dediğinde cebimde arkeolojik kazı başlatıyorum 🪙🔍", "When the cashier asks 'Do you have exact change?' and I start an archaeological dig in my pocket 🪙🔍", 215, 62, "25 dk önce", "25m ago"),
                ("Kaan Arda", "@kaanarda", "🧊", new Color(0.40f, 0.60f, 0.85f), "Gece 02:00'de mutfakta buzdolabının kapağını açıp hiçbir şey almadan 5 dakika bakanlar kulübü 🧊👀", "The 2 AM club opening the fridge door and staring inside for 5 minutes without taking anything 🧊👀", 340, 95, "32 dk önce", "32m ago"),
                ("Deniz Acar", "@denizacar", "🍅", new Color(0.90f, 0.35f, 0.35f), "Organik domatesin kokusunu alınca çocukluğumdaki köy bahçelerine ışınlandım resmen 🍅✨", "Smelled fresh organic tomatoes and got instantly teleported back to childhood farm memories 🍅✨", 128, 19, "40 dk önce", "40m ago"),
                ("Mert Yılmaz", "@mertyilmaz", "⚡", new Color(0.25f, 0.75f, 0.50f), "Kasiyer ürünleri Formula 1 pit stop hızında okuturken ben daha poşeti açmaya çalışıyorum 🏎️🛍️", "Cashier scanning groceries at Formula 1 pit stop speed while I struggle to open the plastic bag 🏎️🛍️", 412, 110, "45 dk önce", "45m ago"),
                ("Ayşe Polat", "@aysepolat", "👶", new Color(0.90f, 0.70f, 0.20f), "Market arabasına binen çocuklar dünyanın en mutlu ve dertsiz insanı olabilir mi? 🛒👶", "Are toddlers riding inside grocery carts officially the happiest humans alive? 🛒👶", 185, 43, "50 dk önce", "50m ago"),
                ("Tolga Ersoy", "@tolgaersoy", "🥒", new Color(0.35f, 0.80f, 0.40f), "Salatalığın kilo fiyatına bakıp 'acaba evde saksıda mı yetiştirsem' diye düşünme evresi 🥒🌱", "Looking at cucumber prices and seriously considering planting a greenhouse in my living room 🥒🌱", 270, 77, "1 sa önce", "1h ago"),
                ("Gülşen Tan", "@gulsentan", "🥐", new Color(0.95f, 0.55f, 0.30f), "Dükkandan gelen taze ekmek kokusu beni diyetimden vazgeçirmek için özel olarak tasarlanmış 🥖🤤", "Fresh baked bakery smell was definitely engineered by scientists to destroy my diet 🥖🤤", 310, 84, "1 sa önce", "1h ago"),
                ("Cemil Usta", "@cemilusta", "🍉", new Color(0.85f, 0.30f, 0.45f), "Karpuz seçerken vurup dinleme uzmanlığı dedelerden torunlara geçen milli spordur 🍉👂", "Tapping watermelons to listen to the echo is an elite national sport passed down generations 🍉👂", 520, 145, "1 sa önce", "1h ago"),
                ("Hazal Kaya", "@hazalkaya", "😅", new Color(0.70f, 0.45f, 0.85f), "Markette reyon aralarında kaybolup 3 kere aynı personelle göz göze gelme gerginliği 😅👀", "Getting lost in aisles and awkwardly making eye contact with the same store staff 3 times 😅👀", 240, 56, "2 sa önce", "2h ago"),
                ("Onur Şen", "@onursen", "🥛", new Color(0.40f, 0.70f, 0.95f), "Son kullanma tarihi en uzaktaki sütü almak için rafın en arkasına kolunu uzatanlar derneği 🥛💪", "Society of shoppers reaching all the way to the back of the shelf for the latest expiry milk 🥛💪", 395, 102, "2 sa önce", "2h ago"),
                ("Sibel Varol", "@sibelvarol", "🍦", new Color(0.95f, 0.45f, 0.70f), "Dondurma dolabının önünde durup 10 dakika çikolatalı mı vanilyalı mı krizine girmek 🍦🤔", "Standing in front of the ice cream freezer having an existential chocolate vs vanilla crisis 🍦🤔", 165, 31, "2 sa önce", "2h ago"),
                ("Barış Korkmaz", "@bariskorkmaz", "🏷️", new Color(0.95f, 0.80f, 0.10f), "İndirim etiketini sarı renkte görünce kalbimin ritmi değişiyor resmen 🏷️💛", "My heart rate legitimately spikes whenever I spot a bright yellow discount tag 🏷️💛", 285, 68, "3 sa önce", "3h ago"),
                ("Ece Güneş", "@ecegunes", "🍓", new Color(0.90f, 0.40f, 0.50f), "Evde 5 kavanoz reçel varken markette 'bu çilek reçeli çok tatlı duruyor' deyip almak 🍓🤦‍♀️", "Having 5 jam jars at home yet buying another one because 'this strawberry jam looks cute' 🍓🤦‍♀️", 195, 40, "3 sa önce", "3h ago"),
                ("Alper Tunç", "@alpertunc", "🛒", new Color(0.50f, 0.65f, 0.80f), "Hızlı kasa sırasına 11 ürünle giren kişiye atılan ölümcül toplu bakışlar 🛒👀⚡", "The collective laser glare directed at the person entering the express checkout with 11 items 🛒👀⚡", 460, 125, "3 sa önce", "3h ago"),
                ("Banu Erdem", "@banuerdem", "☕", new Color(0.65f, 0.45f, 0.35f), "Kahve otomatından kahve alırken çıkan o öğütme sesi dünyadaki en huzurlu ses olabilir ☕🎶", "That grinding sound from the fresh bean coffee vending machine is pure therapy ☕🎶", 220, 48, "4 sa önce", "4h ago"),
                ("Serkan Doğan", "@serkandogan", "🍊", new Color(0.95f, 0.55f, 0.20f), "Taze sıkılmış portakal suyu reyonunun önünden geçerken C vitamini yüklemesi alıyorum 🍊🥤", "Getting a full Vitamin C surge just walking past the freshly squeezed orange juice stand 🍊🥤", 175, 35, "4 sa önce", "4h ago"),
                ("Gamze Yıldız", "@gamzeyildiz", "📝", new Color(0.35f, 0.75f, 0.65f), "Alışveriş listesini evde unutup dükkanın ortasında hafıza jimnastiği yapmak 📝🧠", "Leaving the grocery shopping list at home and doing extreme mental gymnastics in aisle 4 📝🧠", 315, 82, "4 sa önce", "4h ago"),
                ("Tuna Aydın", "@tunaaydin", "🐠", new Color(0.20f, 0.65f, 0.90f), "Girişteki devasa akvaryuma dakikalarca bakıp alışveriş yapmayı unutanlar burada mı? 🐠🛒", "Who else stares at the store aquarium for 10 minutes and completely forgets what they came to buy? 🐠🛒", 290, 74, "5 sa önce", "5h ago"),
                ("Buket Akın", "@buketakin", "🥑", new Color(0.45f, 0.75f, 0.35f), "Avokadonun olgununu bulabilmek için reyon başında 15 dakika dedektiflik yapanlar 🥑🔍", "Doing 15 minutes of forensic detective work just to find the perfectly ripe avocado 🥑🔍", 380, 92, "5 sa önce", "5h ago"),
                ("Levent Sezer", "@levent_s", "🧀", new Color(0.95f, 0.70f, 0.15f), "Peynir reyonunda tadım yaptıran teyze dünyanın en tatlı insanı olabilir mi acaba 🧀❤️", "Is the elderly lady offering cheese samples officially the sweetest human on Earth? 🧀❤️", 430, 115, "6 sa önce", "6h ago"),
                ("Gizem Ünal", "@gizemunal", "🥦", new Color(0.30f, 0.80f, 0.50f), "Brokoli alırken sağlıklı yaşam gurusu, çikolata reyonuna geçince tatlı canavarı olmak 🥦🍫", "Healthy living guru at the broccoli aisle, pure sugar monster at the chocolate rack 🥦🍫", 265, 58, "6 sa önce", "6h ago"),
                ("Kerem Pala", "@kerempala", "🚗", new Color(0.20f, 0.55f, 0.85f), "Otoparkta tam çıkış kapısının önüne park yeri bulmanın verdiği o asil gurur hissi 🚗👑", "The unmatched royal pride when you snag a parking spot right in front of the exit 🚗👑", 350, 88, "7 sa önce", "7h ago"),
                ("Hande Çam", "@handecam", "🧁", new Color(0.90f, 0.45f, 0.75f), "Fırın reyonundan sıcak kurabiye kokusu gelince irade sıfırlandı resmen 🍪🫠", "Hot cookie scent coming from the bakery section completely erased my willpower 🍪🫠", 210, 44, "7 sa önce", "7h ago"),
                ("Murat Aktaş", "@murataktas", "🛍️", new Color(0.60f, 0.40f, 0.85f), "Poşeti açamadığı için parmaklarını hafifçe üfleyenler cemiyeti burada mı? 🛍️💨", "Society of people blowing on their fingertips trying to open plastic bags 🛍️💨", 490, 130, "8 sa önce", "8h ago"),
                ("Pınar Eren", "@pinar_eren", "🍋", new Color(0.95f, 0.85f, 0.20f), "Limon sıkacağının limon olmadan hiçbir işe yaramadığı o hüzünlü aydınlanma anı 🍋💭", "That tragic moment of realization when your lemon squeezer is useless without lemons 🍋💭", 170, 32, "8 sa önce", "8h ago"),
                ("Hakan Koçak", "@hakankocak", "🍕", new Color(0.90f, 0.35f, 0.20f), "Dondurulmuş pizza reyonunun önünde akşam menüsünü baştan yazanlar kulübü 🍕👨‍🍳", "Rewriting the entire dinner menu while standing in front of the frozen pizza case 🍕👨‍🍳", 315, 76, "9 sa önce", "9h ago"),
                ("Selin Bozkurt", "@selinbzkrt", "🌾", new Color(0.75f, 0.60f, 0.35f), "Tarladan yeni gelmiş taze buğday ekmeği sıcakkken üzerine tereyağı sürmek... Şiir gibi 🥖🧈", "Spreading butter on freshly baked warm farm wheat bread... Absolute poetry 🥖🧈", 405, 105, "9 sa önce", "9h ago"),
                ("Yasin Duru", "@yasinduru", "🛒", new Color(0.35f, 0.65f, 0.75f), "Market arabasını tekerlekleri üzerinde iki ayağa kaldırıp süren içimizdeki çocuk 🛒🤸‍♂️", "The inner child tilting the shopping cart on two wheels while walking down aisle 3 🛒🤸‍♂️", 280, 64, "10 sa önce", "10h ago"),
                ("Derya Can", "@deryacan", "🥕", new Color(0.95f, 0.50f, 0.15f), "Taze havuçların çıtırlığı ve rengi resmen doğanın sanat eseri 🥕✨", "The vibrant color and crisp crunch of fresh garden carrots is pure nature art 🥕✨", 195, 39, "10 sa önce", "10h ago"),
                ("Oğuz Yıldırım", "@oguzyildirim", "🍇", new Color(0.55f, 0.35f, 0.75f), "Çekirdeksiz taze üzüm kutusunu bitirmeden filmi başlatamayanlar derneği 🍇🎬", "Can't start the movie without finishing half a box of fresh seedless grapes 🍇🎬", 335, 80, "11 sa önce", "11h ago"),
                ("Zehra Kurt", "@zehrakurt", "🍯", new Color(0.95f, 0.75f, 0.25f), "Karakovan balının o petek dokusuna bakıp hipnoz olan tek ben miyim? 🍯🐝", "Am I the only one getting hypnotized looking at pure natural honeycomb jars? 🍯🐝", 225, 47, "11 sa önce", "11h ago"),
                ("Ersin Taner", "@ersintaner", "📦", new Color(0.40f, 0.50f, 0.65f), "Koli açılırken çıkan bant sesini ASMR olarak dinlemek normal mi acaba? 📦🎧", "Is it socially acceptable to consider cardboard box unboxing tape sound pure ASMR? 📦🎧", 260, 52, "12 sa önce", "12h ago"),
                ("Melis Doğu", "@melisdogu", "🌻", new Color(0.90f, 0.75f, 0.30f), "Doğal ayçiçeği tarlalarından gelen taptaze lezzetler sofranın enerjisini değiştiriyor 🌻💛", "Farm-fresh crops straight from sunny sunflower fields truly transform dinner energy 🌻💛", 310, 71, "12 sa önce", "12h ago")
            };

            for (int i = 0; i < wittyList.Length; i++)
            {
                var item = wittyList[i];
                tweetFeed.Add(new SocialTweetData(
                    $"TWT_WITTY_{i + 1}",
                    item.name,
                    item.handle,
                    item.emoji,
                    item.color,
                    false,
                    false,
                    item.tr,
                    item.en,
                    item.timeTr,
                    item.timeEn,
                    item.likes,
                    item.rts,
                    TweetSentiment.Neutral
                ));
            }
        }

        #region Müşteri Canlı Twit Üreticileri (50+ Zengin Varyasyon)

        public (string tr, string en) GenerateMegaPraiseTweet(string storeName, int itemsCount)
        {
            var pool = new (string tr, string en)[]
            {
                ($"Bugün @{storeName} dükkanından tam {itemsCount} parça taze ürün aldım! Tarladan yeni toplanmış gibi taptaze 🛒🌾", $"Just bought {itemsCount} fresh items from @{storeName}! Straight from the field freshness 🛒🌾"),
                ($"@{storeName} alışveriş arabasını ağzına kadar doldurduk! Taze sebze ve meyvelerin kokusu harikaydı, elinize sağlık 🍅🍇", $"Filled our cart to the brim at @{storeName}! The scent of fresh fruits and veggies was amazing 🍅🍇"),
                ($"Haftalık mutfak alışverişini @{storeName} üzerinden hallettim. {itemsCount} parça aldım, hepsi birinci sınıf kalite ✨🧺", $"Did my weekly grocery run at @{storeName}. Got {itemsCount} items, all top tier quality ✨🧺"),
                ($"@{storeName} reyonları öyle dolu ve cezbediciydi ki kendimi tutamayıp {itemsCount} parça ürün aldım! Daimi müşterinizim 👏😍", $"The shelves at @{storeName} were so well-stocked I couldn't resist buying {itemsCount} items! Regular customer now 👏😍"),
                ($"Organik ürün arayanlara şiddetle @{storeName} tavsiye ediyorum. Bagajı doldurdum resmen, fiyatlar da çok makul 🌿🚗", $"Highly recommend @{storeName} for organic produce lovers. Loaded my trunk, very reasonable prices 🌿🚗"),
                ($"@{storeName} bu bölgede gördüğüm en taze ürünlere sahip market. Sepet dolusu alışveriş yaptım, helal olsun 🥦🌽", $"Best fresh produce market in the district @{storeName}. Bought a full basket, well done 🥦🌽"),
                ($"Kasadan {itemsCount} parça ürünle geçtim, kasiyer o kadar hızlı ve güler yüzlüydü ki sıra hiç beklemedim ⚡😊", $"Checked out with {itemsCount} items, cashier was super fast and friendly, zero queue waiting ⚡😊"),
                ($"Köyden yeni gelmiş gibi taze domates ve meyveler! @{storeName} sayesinde soframız şenlendi 🍅🍇", $"Tomatoes and fruits taste just like from a village farm! Thanks @{storeName} for enriching our meals 🍅🍇"),
                ($"@{storeName} içindeki ferahlık ve düzen inanılmaz. Alışveriş yaparken insan dinleniyor resmen 🍃🛒", $"The ambient vibe and organization inside @{storeName} is fantastic. Relaxing shopping experience 🍃🛒"),
                ($"Bugün @{storeName} marketine ilk defa geldim ve bayıldım! {itemsCount} ürünle mutlu bir şekilde ayrılıyorum 💚🌟", $"First time shopping at @{storeName} today and loved it! Leaving happy with {itemsCount} items 💚🌟"),
                ($"Bu devirde hem taze hem hesaplı ürün bulmak zordu, @{storeName} imdadımıza yetişti 🥑🥗", $"Hard to find both fresh and affordable groceries nowadays, @{storeName} saved the day 🥑🥗"),
                ($"@{storeName} reyon görevlileri çok ilgili ve kibar. Aradığım her şeyi anında buldum, teşekkürler! 👏🌸", $"Floor staff at @{storeName} was polite and helpful. Found everything in seconds, thank you! 👏🌸"),
                ($"Çiftlikten sofraya konseptini hakkıyla yapan tek market @{storeName}. Kesinlikle 5 yıldızı hak ediyor ⭐⭐⭐⭐⭐", $"The only store truly mastering the farm-to-table concept is @{storeName}. Deserves 5 solid stars ⭐⭐⭐⭐⭐"),
                ($"@{storeName} meyvelerin canlılığına ve parlaklığına hayran kaldım, tarladan yeni koparılmış 🍓🍎", $"Amazed by how vibrant and crisp the fruits are at @{storeName}, freshly picked 🍓🍎"),
                ($"Tam {itemsCount} parça organik ürün aldım, eve gidip taze bir salata yapmanın heyecanındayım 🥗😋", $"Got {itemsCount} organic items, can't wait to make a fresh garden salad at home 🥗😋"),
                ($"@{storeName} dükkanında hem çiftlik ürünleri hem reyon düzeni mükemmeldi. {itemsCount} parça aldım, tekrar geleceğim 🛒🥖", $"Both farm goods and shelf order were top notch at @{storeName}. Bought {itemsCount} items, will return 🛒🥖")
            };

            int idx = UnityEngine.Random.Range(0, pool.Length);
            return pool[idx];
        }

        public (string tr, string en) GenerateStandardPraiseTweet(string storeName, int itemsCount)
        {
            var pool = new (string tr, string en)[]
            {
                ($"@{storeName} dükkanına uğradım, reyonlar temiz ve fiyatlar gayet uygundu! 🌿👍", $"Stopped by @{storeName}, shelves were clean and prices were great! 🌿👍"),
                ($"İş çıkışı @{storeName} uğrayıp birkaç parça taze şey aldım. Çok pratik ve ferah bir market 🛒✨", $"Dropped by @{storeName} after work for some fresh stuff. Super convenient and airy store 🛒✨"),
                ($"@{storeName} ekmek ve sebzeler her zaman taze kalıyor, mahalleye çok iyi geldi bu market 🥖🥦", $"Bread and veggies stay fresh at @{storeName}, great addition to the neighborhood 🥖🥦"),
                ($"Hızlıca alışverişimi yapıp çıktım, @{storeName} kasasında hiç beklemedim çok seriydi ⚡💳", $"In and out quickly, checkout at @{storeName} was lightning fast ⚡💳"),
                ($"@{storeName} dükkanının içi pırıl pırıl, yerler ve raflar tertemiz parlıyor ✨🧹", $"Spotless interior at @{storeName}, floors and shelves sparkling clean ✨🧹"),
                ($"Akşam yemeği için taze yeşillik lazımdı, @{storeName} sayesinde taptaze hallettim 🥬🥗", $"Needed fresh greens for dinner, got crisp quality thanks to @{storeName} 🥬🥗"),
                ($"@{storeName} marketindeki düzen ve reyon etiketleri çok anlaşılır, tebrikler 🏷️👏", $"Clean organization and clear shelf price tags at @{storeName}, good job 🏷️👏"),
                ($"Fiyat / performans olarak bölgenin en makul marketi kesinlikle @{storeName} 📊👍", $"Best price-to-quality value in town is definitely @{storeName} 📊👍"),
                ($"@{storeName} dükkanında çalınan müzik bile insanı sakinleştiriyor, huzurlu bir alışveriş oldu 🎵🛍️", $"Even the music inside @{storeName} is calming, peaceful shopping trip 🎵🛍️"),
                ($"Her sabah taze mahsulleri görmek güne güzel başlatıyor @{storeName} ☀️🌾", $"Seeing fresh morning crops at @{storeName} always starts the day right ☀️🌾"),
                ($"@{storeName} personeli çok güler yüzlü ve yardımsever, reyonlar hep düzenli 🧑‍🌾🤝", $"Staff at @{storeName} is friendly and helpful, aisles always tidy 🧑‍🌾🤝"),
                ($"Taze meyveler için artık tek adresim @{storeName}, komşulara da tavsiye ettim 🍇🍊", $"My go-to spot for fresh fruit is now @{storeName}, recommended to neighbors 🍇🍊"),
                ($"@{storeName} otoparkı çok rahat, park edip 5 dakikada alışverişimi hallettim 🚗⚡", $"Parking at @{storeName} was easy, parked and shopped in 5 minutes 🚗⚡"),
                ($"Yerli ve doğal ürünlerin desteklenmesi çok güzel, @{storeName} doğru yolda 🌱🇹🇷", $"Love supporting local and natural goods, @{storeName} is on the right path 🌱"),
                ($"@{storeName} kasadaki temassız ödeme ve fiş düzeni çok hızlı çalışıyor 💳⚡", $"Contactless payment and receipts work seamlessly at @{storeName} 💳⚡"),
                ($"Bugün @{storeName} dükkanından aldığım domateslerin tadı nefisti, elinize sağlık 🍅🤤", $"Tomatoes I bought today from @{storeName} tasted incredible, great work 🍅🤤")
            };

            int idx = UnityEngine.Random.Range(0, pool.Length);
            return pool[idx];
        }

        public (string tr, string en) GenerateComplaintTweet(string storeName)
        {
            var pool = new (string tr, string en)[]
            {
                ($"@{storeName} marketine gittim fakat aradığım ürünleri raflarda bulamadım, reyonlar boş kalmış! 😕🛒", $"Went to @{storeName} but couldn't find what I needed, shelves were empty! 😕🛒"),
                ($"@{storeName} reyonlarındaki bazı raflar tamamen tükenmişti, lütfen stokları daha sık yenileyin 📦❌", $"Some shelves at @{storeName} were completely out of stock, please restock frequently 📦❌"),
                ($"Akşam @{storeName} dükkanına uğradım ama taze sebze kalmamıştı, eli boş dönmek zorunda kaldım 🚶‍♂️💨", $"Stopped by @{storeName} this evening but no veggies left, had to leave empty-handed 🚶‍♂️💨"),
                ($"@{storeName} kasa sırası biraz yavaştı, daha fazla personel veya açık kasa olsa süper olurdu ⏳👀", $"Checkout queue at @{storeName} was a bit slow, more staff would help ⏳👀"),
                ($"Aradığım meyveyi bulamadım @{storeName}, depoda ürün var mıydı acaba? Raflar boştu 🍎🤷‍♂️", $"Couldn't find fruits at @{storeName}, any stock in warehouse? Shelves were empty 🍎🤷‍♂️"),
                ($"@{storeName} dükkanında bazı ürünlerin fiyat etiketleri eksikti, hangisi ne kadar anlayamadım 🏷️❓", $"Missing price tags on some items at @{storeName}, couldn't tell prices 🏷️❓"),
                ($"Market arabaları biraz zor sürülüyor @{storeName}, tekerleklere bir bakım yapılsa iyi olur 🛒🔧", $"Shopping carts were hard to steer at @{storeName}, wheels need oiling 🛒🔧"),
                ($"@{storeName} yerlerde biraz dökülen yaprak ve çöp vardı, temizliğe biraz daha dikkat lütfen 🧹⚠️", $"Noticed fallen leaves and litter on the floor at @{storeName}, cleaner floors please 🧹⚠️"),
                ($"İndirimde görünen ürün reyonlarda bitmişti @{storeName}, erkenden tükeniyor galiba 📉📦", $"Promoted discount items were all gone at @{storeName}, runs out too early 📉📦"),
                ($"@{storeName} kasada sıra beklerken biraz bunaldım, yoğun saatlerde personel takviyesi şart 👥⏰", $"Felt cramped waiting at checkout in @{storeName}, need more cashiers during rush hours 👥⏰"),
                ($"Organik ürünlerin bazıları biraz pahalı geldi @{storeName}, biraz daha kampanya bekliyoruz 💸👛", $"Some organic produce seemed a bit pricey at @{storeName}, hoping for sales 💸👛"),
                ($"@{storeName} dükkanında istediğim boyutta sepet bulamadım, hepsi doluydu 🧺❌", $"Couldn't find an available shopping basket at @{storeName}, all in use 🧺❌"),
                ($"Akşam kapanışa doğru geldim, raflar talan edilmiş gibiydi @{storeName}. Lütfen ürünleri doldurun 🏪💨", $"Came near closing time, shelves looked depleted at @{storeName}. Please keep shelves filled 🏪💨"),
                ($"@{storeName} reyoncu arkadaş biraz yorgun görünüyordu, reyonlar geç diziliyor 📦🐢", $"Restocker looked exhausted at @{storeName}, shelves take a while to fill 📦🐢"),
                ($"Aradığım temel gıda ürününü bulamadan çıktım @{storeName}, umarım yarın raflar doludur 🚪😔", $"Left without finding essential groceries at @{storeName}, hope shelves are stocked tomorrow 🚪😔"),
                ($"@{storeName} giriş kapısı biraz kalabalıktı, giriş-çıkış yönlendirmesi geliştirilebilir 🚪🚶‍♀️", $"Entrance area felt congested at @{storeName}, foot traffic flow could improve 🚪🚶‍♀️")
            };

            int idx = UnityEngine.Random.Range(0, pool.Length);
            return pool[idx];
        }

        #endregion

        public string GetPlayerFullName()
        {
            if (StoreStatusManager.Instance != null && !string.IsNullOrWhiteSpace(StoreStatusManager.Instance.PlayerName))
            {
                return StoreStatusManager.Instance.PlayerName;
            }
            return LocalizationManager.L("Default_PlayerName", "Alex Morgan", "Alex Morgan");
        }

        public string GetStoreName()
        {
            if (StoreStatusManager.Instance != null && !string.IsNullOrWhiteSpace(StoreStatusManager.Instance.CompanyName))
            {
                return StoreStatusManager.Instance.CompanyName;
            }
            return LocalizationManager.L("Default_StoreName", "Fresh Shelf Market", "Fresh Shelf Market");
        }

        public string GetPlayerHandle()
        {
            string name = GetPlayerFullName();
            string handle = "@" + name.Replace(" ", "").Replace("ç", "c").Replace("Ç", "C").Replace("ğ", "g").Replace("Ğ", "G").Replace("ı", "i").Replace("İ", "I").Replace("ö", "o").Replace("Ö", "O").Replace("ş", "s").Replace("Ş", "S").Replace("ü", "u").Replace("Ü", "U");
            return handle;
        }

        public float GetStoreRating()
        {
            int praiseCount = 0;
            int complaintCount = 0;

            foreach (var t in tweetFeed)
            {
                if (t.sentiment == TweetSentiment.Praise) praiseCount++;
                else if (t.sentiment == TweetSentiment.Complaint) complaintCount++;
            }

            int total = praiseCount + complaintCount;
            if (total == 0) return 4.9f;

            float ratio = (float)praiseCount / total;
            float rating = 3.0f + (ratio * 2.0f);
            return Mathf.Clamp((float)Math.Round(rating, 1), 3.0f, 5.0f);
        }

        public string GetDailyTrendsFormatted()
        {
            int currentDay = (TimeManager.Instance != null) ? TimeManager.Instance.Day : 1;
            if (currentDay == cachedDayForTrends && !string.IsNullOrEmpty(cachedTrendsTextTr))
            {
                return LocalizationManager.L("Social_DailyTrends", cachedTrendsTextTr, cachedTrendsTextEn);
            }

            cachedDayForTrends = currentDay;
            string sName = GetStoreName().Replace(" ", "");

            // Her gün değişen zengin gündem havuzu
            string[][] trendPoolTr = new string[][]
            {
                new string[] { $"#{sName} (18.4B)", "#TazeHasat (14.2B)", "#HızlıKasa (10.5B)", "#OrganikTarım (7.1B)", "#Farm2Shelf (4.3B)" },
                new string[] { "#GününFırsatı (22.1B)", $"#{sName} (16.9B)", "#YerliÜretim (11.4B)", "#ÇiftliktenSofraya (8.2B)", "#SağlıklıBeslenme (5.0B)" },
                new string[] { "#SüpermarketGündemi (25.3B)", "#TazeMeyve (17.0B)", $"#{sName} (13.8B)", "#İndirimGünü (9.6B)", "#MüşteriMemnuniyeti (6.2B)" },
                new string[] { "#HaftaSonuAlışverişi (28.7B)", $"#{sName} (19.4B)", "#SütÜrünleri (13.1B)", "#YerelEsnaf (8.9B)", "#AkıllıMarket (5.8B)" },
                new string[] { "#TarlaFiyatına (21.5B)", $"#{sName} (15.7B)", "#EnTazeReyon (12.3B)", "#OrganikSebze (7.9B)", "#KasaSırası (4.7B)" }
            };

            string[][] trendPoolEn = new string[][]
            {
                new string[] { $"#{sName} (18.4K)", "#FreshHarvest (14.2K)", "#FastCheckout (10.5K)", "#OrganicFarming (7.1K)", "#Farm2Shelf (4.3K)" },
                new string[] { "#DealOfTheDay (22.1K)", $"#{sName} (16.9K)", "#LocalProduce (11.4K)", "#FarmToTable (8.2K)", "#HealthyLiving (5.0K)" },
                new string[] { "#SupermarketTrends (25.3K)", "#FreshFruit (17.0K)", $"#{sName} (13.8K)", "#DiscountDay (9.6K)", "#HappyShoppers (6.2K)" },
                new string[] { "#WeekendShopping (28.7K)", $"#{sName} (19.4K)", "#DairyFresh (13.1K)", "#LocalMarket (8.9K)", "#SmartGrocery (5.8K)" },
                new string[] { "#DirectFromFarm (21.5K)", $"#{sName} (15.7K)", "#FreshShelves (12.3K)", "#OrganicVeggies (7.9K)", "#ExpressQueue (4.7K)" }
            };

            int poolIndex = (currentDay - 1) % trendPoolTr.Length;
            if (poolIndex < 0) poolIndex = 0;

            string[] selectedTr = trendPoolTr[poolIndex];
            string[] selectedEn = trendPoolEn[poolIndex];

            cachedTrendsTextTr = $"<size=15><color=#40C4FF><b>🔥 GÜNDEMDEKİ BAŞLIKLAR (Gün {currentDay})</b></color></size>\n\n" +
                                 $"<b>1.</b> {selectedTr[0]}\n" +
                                 $"<b>2.</b> {selectedTr[1]}\n" +
                                 $"<b>3.</b> {selectedTr[2]}\n" +
                                 $"<b>4.</b> {selectedTr[3]}\n" +
                                 $"<b>5.</b> {selectedTr[4]}";

            cachedTrendsTextEn = $"<size=15><color=#40C4FF><b>🔥 TRENDING TOPICS (Day {currentDay})</b></color></size>\n\n" +
                                 $"<b>1.</b> {selectedEn[0]}\n" +
                                 $"<b>2.</b> {selectedEn[1]}\n" +
                                 $"<b>3.</b> {selectedEn[2]}\n" +
                                 $"<b>4.</b> {selectedEn[3]}\n" +
                                 $"<b>5.</b> {selectedEn[4]}";

            return LocalizationManager.L("Social_DailyTrends", cachedTrendsTextTr, cachedTrendsTextEn);
        }

        public List<SocialTweetData> GetFeed(int tabFilter)
        {
            if (tabFilter == 0) // 1. Sana Özel (Müşterilerin bizimle ilgili twitleri + 20 adet dünya/market esprili twiti)
            {
                return tweetFeed.FindAll(t => !t.isPlayerTweet);
            }
            else if (tabFilter == 1) // 2. Yorumlar (Bizim attığımız duyuru twitlerinin altına gelen müşteri yorumları)
            {
                return tweetFeed.FindAll(t => t.isPlayerTweet && t.comments != null && t.comments.Count > 0);
            }
            else if (tabFilter == 2) // 3. Twitlerim (Sadece bizim attığımız twitler ve beğeni/retweet istatistikleri)
            {
                return tweetFeed.FindAll(t => t.isPlayerTweet);
            }
            return tweetFeed;
        }

        public void ToggleLike(SocialTweetData tweet)
        {
            if (tweet == null) return;
            tweet.isLikedByPlayer = !tweet.isLikedByPlayer;
            tweet.likesCount += tweet.isLikedByPlayer ? 1 : -1;
            OnFeedUpdated?.Invoke();
        }

        public void ToggleRetweet(SocialTweetData tweet)
        {
            if (tweet == null) return;
            tweet.isRetweetedByPlayer = !tweet.isRetweetedByPlayer;
            tweet.retweetsCount += tweet.isRetweetedByPlayer ? 1 : -1;
            OnFeedUpdated?.Invoke();
        }

        /// <summary>
        /// Oyuncu duyuru twiti attığında çalışır.
        /// Takipçi kazandırır ve twitin altına gerçek müşteri yorumları getirir!
        /// </summary>
        public void PostPlayerAnnouncement(string tweetTr, string tweetEn)
        {
            string recId = "TWT_" + UnityEngine.Random.Range(10000, 99999);
            string playerFullName = GetPlayerFullName();
            string playerHandle = GetPlayerHandle();
            string storeName = GetStoreName();

            SocialTweetData newTweet = new SocialTweetData(
                recId,
                playerFullName,
                playerHandle,
                "👨‍🌾",
                new Color(0.20f, 0.70f, 0.95f),
                true,
                true,
                tweetTr,
                tweetEn,
                "Şimdi",
                "Just now",
                UnityEngine.Random.Range(12, 45),
                UnityEngine.Random.Range(3, 14),
                TweetSentiment.Official
            );

            // Oyuncunun twitinin altına otomatik gerçek müşteri yorumları gelsin!
            string[] customerNames = new string[] { "Burak Demir", "Seda Yılmaz", "Kerem Acar", "Merve Şahin", "Oğuzhan Kurt" };
            string[] customerHandles = new string[] { "@bdemir", "@sedayilmaz", "@kerem_a", "@mervesahin", "@oguzkurt" };
            string[] emojis = new string[] { "🧑", "👩", "🧔", "👱‍♀️", "👨" };

            string[][] sampleRepliesTr = new string[][]
            {
                new string[] { "Harika duyuru, akşam uğrayıp sepeti dolduracağım! 🛒", "Great announcement, will drop by this evening to shop! 🛒" },
                new string[] { $"@{storeName} gerçekten bu bölgenin en kaliteli marketi oldu, tebrikler!", $"@{storeName} really became the highest quality market around, congrats!" },
                new string[] { "Fiyatlar ve ürün tazeliği böyle devam ederse daimi müşterinizim 👍", "If fresh quality and good prices continue like this, I'm a regular 👍" },
                new string[] { "Yeni hasat domates ve meyveler geldi mi? Hemen geliyorum 🍅🍇", "Did the new harvest tomatoes and fruits arrive? On my way 🍅🍇" }
            };

            int replyCount = UnityEngine.Random.Range(2, 4);
            for (int r = 0; r < replyCount; r++)
            {
                int rIdx = r % sampleRepliesTr.Length;
                int cIdx = (r + UnityEngine.Random.Range(0, 3)) % customerNames.Length;

                newTweet.comments.Add(new SocialCommentData(
                    $"CMT_{recId}_{r + 1}",
                    customerNames[cIdx],
                    customerHandles[cIdx],
                    emojis[cIdx],
                    new Color(UnityEngine.Random.Range(0.2f, 0.8f), UnityEngine.Random.Range(0.3f, 0.8f), UnityEngine.Random.Range(0.4f, 0.9f)),
                    sampleRepliesTr[rIdx][0],
                    sampleRepliesTr[rIdx][1],
                    "Az önce",
                    "Just now",
                    UnityEngine.Random.Range(2, 18)
                ));
            }

            tweetFeed.Insert(0, newTweet);

            // Başarılı etkileşim: +15 ile +35 arası takipçi kazandırır!
            followerCount += UnityEngine.Random.Range(15, 35);
            OnFeedUpdated?.Invoke();
        }

        /// <summary>
        /// Gün içinde markete gelen gerçek müşterilerin attığı iyi veya kötü twitleri akışa ekler.
        /// İyi twitler takipçi kazandırır, kötü twitler takipçi kaybettirir!
        /// </summary>
        public void AddCustomerTweet(string customerName, string avatarEmoji, Color avatarBgColor, bool isVIP, TweetSentiment sentiment, string trText, string enText)
        {
            string recId = "TWT_" + UnityEngine.Random.Range(10000, 99999);
            string handle = "@" + customerName.Replace(" ", "").Replace("ç", "c").Replace("Ç", "C").Replace("ğ", "g").Replace("Ğ", "G").Replace("ı", "i").Replace("İ", "I").Replace("ö", "o").Replace("Ö", "O").Replace("ş", "s").Replace("Ş", "S").Replace("ü", "u").Replace("Ü", "U");

            SocialTweetData tweet = new SocialTweetData(
                recId,
                customerName,
                handle,
                string.IsNullOrEmpty(avatarEmoji) ? "👤" : avatarEmoji,
                avatarBgColor == default ? new Color(0.40f, 0.50f, 0.65f) : avatarBgColor,
                isVIP,
                false,
                trText,
                enText,
                "Az önce",
                "Just now",
                UnityEngine.Random.Range(1, 35),
                UnityEngine.Random.Range(0, 8),
                sentiment
            );

            // Müşteri twitinin altına diğer müşterilerden 1-2 destekleyici veya yorumlayıcı yanıt gelsin (Çift Dilli / Bilingual)
            if (sentiment == TweetSentiment.Praise)
            {
                var praiseReplies = new (string name, string handle, string emoji, Color color, string tr, string en)[]
                {
                    ("Büşra Çelik", "@busrac", "👩", new Color(0.85f, 0.45f, 0.55f), "Kesinlikle katılıyorum, reyonlar ve fiyatlar çok başarılıydı ✨", "Totally agree, the shelves and prices were great ✨"),
                    ("Koray Yaman", "@korayyaman", "🧔", new Color(0.35f, 0.65f, 0.90f), "Ben de her hafta uğruyorum, ürünler gerçekten taptaze 👌", "I visit every week too, the produce is truly fresh 👌"),
                    ("Nazlı Demir", "@nazlidemir", "👱‍♀️", new Color(0.95f, 0.50f, 0.70f), "Meyve reyonundaki kokular harika, afiyet olsun 😍", "The aroma in the fruit aisle is wonderful, enjoy! 😍")
                };
                int rIdx = UnityEngine.Random.Range(0, praiseReplies.Length);
                var pr = praiseReplies[rIdx];

                tweet.comments.Add(new SocialCommentData(
                    $"CMT_{recId}_1", pr.name, pr.handle, pr.emoji, pr.color,
                    pr.tr, pr.en,
                    "Az önce", "Just now", UnityEngine.Random.Range(3, 14)
                ));

                // İyi yorum: Takipçi kazandırır!
                followerCount += UnityEngine.Random.Range(5, 18);
            }
            else if (sentiment == TweetSentiment.Complaint)
            {
                var complaintReplies = new (string name, string handle, string emoji, Color color, string tr, string en)[]
                {
                    ("Murat Aslan", "@murataslan", "👨‍🦱", new Color(0.60f, 0.65f, 0.70f), "Umarım market yönetimi en kısa sürede bu durumu düzeltir 😕", "I hope store management fixes this situation soon 😕"),
                    ("Sinem Kurt", "@sinemkurt", "👩‍💼", new Color(0.80f, 0.40f, 0.50f), "Yoğun saatlerde stoklar çabuk bitebiliyor, erken gitmek lazım 📉", "Stock can run out fast during rush hours, better go early 📉"),
                    ("Ahmet Vural", "@ahmetvural", "👨", new Color(0.40f, 0.55f, 0.75f), "Haklısınız, reyon görevlilerinin daha sık rafları doldurması gerek 📦", "You're right, restockers need to replenish shelves more frequently 📦")
                };
                int rIdx = UnityEngine.Random.Range(0, complaintReplies.Length);
                var cr = complaintReplies[rIdx];

                tweet.comments.Add(new SocialCommentData(
                    $"CMT_{recId}_1", cr.name, cr.handle, cr.emoji, cr.color,
                    cr.tr, cr.en,
                    "Az önce", "Just now", UnityEngine.Random.Range(4, 15)
                ));

                // Kötü yorum: Takipçi kaybettirir!
                followerCount = Mathf.Max(0, followerCount - UnityEngine.Random.Range(6, 16));
            }

            tweetFeed.Insert(0, tweet);

            // Akışta maksimum 50 twit tut (eski twitleri temizle)
            if (tweetFeed.Count > 50)
            {
                tweetFeed.RemoveAt(tweetFeed.Count - 1);
            }

            OnFeedUpdated?.Invoke();
        }
    }
}
