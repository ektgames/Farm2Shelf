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

        private int followerCount = 250;
        private List<SocialTweetData> tweetFeed = new List<SocialTweetData>();
        public event Action OnFeedUpdated;

        public int FollowerCount => followerCount;
        public List<SocialTweetData> GetTweetFeed() => tweetFeed;

        public void RestoreSocialMediaData(int followers, List<SocialTweetData> feed)
        {
            this.followerCount = Mathf.Max(0, followers);
            if (feed != null && feed.Count > 0)
            {
                this.tweetFeed.Clear();
                this.tweetFeed.AddRange(feed);
            }
            OnFeedUpdated?.Invoke();
        }

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

            // Oyun başlangıcında sadece dükkanımızı açtığımızı duyuran tek resmi twit yer alır
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
                "Az önce",
                "Just now",
                24,
                6,
                TweetSentiment.Official
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
