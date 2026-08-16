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

        public string LocalizedText => LocalizationManager.L("TweetText_" + tweetId, tweetTextTr, tweetTextEn);
        public string LocalizedTime => LocalizationManager.L("TweetTime_" + tweetId, timeAgoTr, timeAgoEn);

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
        }
    }

    /// <summary>
    /// Farm2Shelf Sosyal Medya (Twitter / Chirper / X Feed) Yöneticisi.
    /// Dükkana gelen müşterilerin deneyimlerine göre canlı twit üretir,
    /// Oyuncu profili bilgilerini tutar ve duyuru twiti atılmasını sağlar.
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

        private List<SocialTweetData> tweetFeed = new List<SocialTweetData>();
        public event Action OnFeedUpdated;

        private int followerCount = 1420;

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

        private void InitializeDefaultTweets()
        {
            if (tweetFeed.Count > 0) return;

            string playerFullName = GetPlayerFullName();
            string storeName = GetStoreName();
            string playerHandle = GetPlayerHandle();

            // Varsayılan İlgi Çekici Başlangıç Twitleri (TR & EN)
            tweetFeed.Add(new SocialTweetData(
                "TWT_101",
                playerFullName,
                playerHandle,
                "👨‍🌾",
                new Color(0.20f, 0.70f, 0.95f),
                true,
                true,
                $"🚀 Marketimizin kapıları açıldı! Taze çiftlik mahsullerimizle hizmetinizdeyiz. Hepinizi @{storeName} bekliyoruz! 🌾🛒",
                $"🚀 Our store is officially open! Fresh farm crops served daily. Welcome to @{storeName}! 🌾🛒",
                "10dk önce",
                "10m ago",
                48,
                12,
                TweetSentiment.Official
            ));

            tweetFeed.Add(new SocialTweetData(
                "TWT_102",
                "Ahmet Yılmaz",
                "@AhmetYilmaz",
                "👨‍💼",
                new Color(0.25f, 0.35f, 0.65f),
                true,
                false,
                $"Bugün @{storeName} dükkanından domates ve yeşillik aldım. Mahsuller taptaze, fiyatlar da gayet makul! 🍅🥦👍",
                $"Bought tomatoes and greens at @{storeName} today. Super fresh produce and very fair prices! 🍅🥦👍",
                "25dk önce",
                "25m ago",
                14,
                3,
                TweetSentiment.Praise
            ));

            tweetFeed.Add(new SocialTweetData(
                "TWT_103",
                "Selin Koç",
                "@SelinKoc",
                "👩‍🎓",
                new Color(0.85f, 0.45f, 0.65f),
                false,
                false,
                $"@{storeName} kasasında 5 dakikadan az bekledim! Kasiyerler ışık hızında çalışıyor ⚡🛒",
                $"Waited under 5 minutes at @{storeName} checkout! Fast cashiers ⚡🛒",
                "1sa önce",
                "1h ago",
                29,
                5,
                TweetSentiment.Praise
            ));

            tweetFeed.Add(new SocialTweetData(
                "TWT_104",
                "Mehmet Öztürk",
                "@MehmetOzturk",
                "🧔",
                new Color(0.55f, 0.30f, 0.65f),
                false,
                false,
                $"@{storeName} reyonları cıvıl cıvıl, dükkandaki low-poly düzen harika duruyor! Favori marketim oldu. ❤️🌾",
                $"The shelves at @{storeName} look amazing and so well organized! My new favorite spot. ❤️🌾",
                "2sa önce",
                "2h ago",
                37,
                8,
                TweetSentiment.Praise
            ));

            tweetFeed.Add(new SocialTweetData(
                "TWT_105",
                "Emily Smith",
                "@EmilySmith",
                "👩‍⚕️",
                new Color(0.70f, 0.40f, 0.50f),
                true,
                false,
                $"Yoğun saatlerde @{storeName} sırasında biraz bekledik ama mahsullerin tazeliğine değer! 🥦👌",
                $"Bit of a line at @{storeName} during peak hours, but the freshness is totally worth it! 🥦👌",
                "3sa önce",
                "3h ago",
                19,
                4,
                TweetSentiment.Neutral
            ));
        }

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

        public int FollowerCount => followerCount;

        public List<SocialTweetData> GetFeed(int tabFilter)
        {
            if (tabFilter == 1) // Yorumlar / Şikayet & Övgüler
            {
                return tweetFeed.FindAll(t => !t.isPlayerTweet);
            }
            else if (tabFilter == 2) // Profilim / Kendi Twitlerim
            {
                return tweetFeed.FindAll(t => t.isPlayerTweet);
            }
            return tweetFeed; // 0: Sana Özel (Tüm Akış)
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

        public void PostPlayerAnnouncement(string tweetTr, string tweetEn)
        {
            string recId = "TWT_" + UnityEngine.Random.Range(10000, 99999);
            string playerFullName = GetPlayerFullName();
            string playerHandle = GetPlayerHandle();

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
                1,
                0,
                TweetSentiment.Official
            );

            tweetFeed.Insert(0, newTweet);
            followerCount += UnityEngine.Random.Range(5, 15);
            OnFeedUpdated?.Invoke();
        }

        /// <summary>
        /// Müşteri alışveriş sonrasında veya durum bazlı canlı twit üretici helper metodu.
        /// </summary>
        public void AddCustomerTweet(string customerName, string avatarEmoji, Color avatarBgColor, bool isVIP, TweetSentiment sentiment, string trText, string enText)
        {
            string recId = "TWT_" + UnityEngine.Random.Range(10000, 99999);
            string handle = "@" + customerName.Replace(" ", "");

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
                UnityEngine.Random.Range(1, 25),
                UnityEngine.Random.Range(0, 5),
                sentiment
            );

            tweetFeed.Insert(0, tweet);

            // Akışta maks 40 twit tut
            if (tweetFeed.Count > 40)
            {
                tweetFeed.RemoveAt(tweetFeed.Count - 1);
            }

            OnFeedUpdated?.Invoke();
        }
    }
}
