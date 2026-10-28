using System;
using System.Collections;
using UnityEngine;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    [Serializable]
    public class StoreAnnouncementSaveData
    {
        public int dayKey = -1;
        public int lastSlot = -1;
        public int nextEntry = 3;
        public int[] playCounts;
    }

    /// <summary>Only the advancing game clock dispatches announcements; restores never do.</summary>
    public sealed class StoreAnnouncementManager : MonoBehaviour
    {
        public static StoreAnnouncementManager Instance { get; private set; }
        private StoreAnnouncementSaveData state = new StoreAnnouncementSaveData();
        private AudioSource voice;
        private Coroutine loading;
        private AudioClip loadedClip;
        private bool paused;
        private bool warnedMissing;
        public bool IsAudible => isActiveAndEnabled && voice != null && voice.isPlaying &&
            !paused && !voice.mute && voice.volume > 0.001f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 0f;
            voice.pitch = 1f;
            voice.priority = 80;
            // Midnight intentionally pauses simulation. Its closing recording must still finish.
            voice.ignoreListenerPause = true;
        }

        public void OnClockMinute(int hour, int minute)
        {
            if (MainMenuUI.IsMenuVisible || minute != 0 || hour < 8 || hour > 24 || (hour % 4 != 0 && hour != 22))
                return;
            if(LocalizationManager.Instance!=null && LocalizationManager.Instance.CurrentLanguage==GameLanguage.English)
            {StopPlayback();return;}
            var clock = TimeManager.Instance;
            if (clock == null) return;
            if (hour < 24 && (StoreStatusManager.Instance == null || !StoreStatusManager.Instance.IsOpen)) return;
            int key = (clock.Year * 120) + ((int)clock.CurrentSeason * 30) + clock.Day;
            if (state.dayKey != key) { state.dayKey = key; state.lastSlot = -1; }
            if (hour <= state.lastSlot) return;
            state.lastSlot = hour;

            int entry = hour == 8 ? 0 : hour == 22 ? 1 : hour == 24 ? 2 : SelectEntry(hour);
            if (entry < 0) return;
            StopPlayback();
            string path = "Audio/Announcements/tr/" + StoreAnnouncementCatalog.Entries[entry].Id;
            loading = StartCoroutine(LoadAndPlay(path));
        }

        private int SelectEntry(int hour)
        {
            var entries = StoreAnnouncementCatalog.Entries;
            if (state.playCounts == null || state.playCounts.Length != entries.Length)
                state.playCounts = new int[entries.Length];
            int selected = -1;
            for (int i = 0; i < entries.Length - 3; i++)
            {
                int index = 3 + ((Mathf.Max(3, state.nextEntry) - 3 + i) % (entries.Length - 3));
                if (hour < entries[index].FromHour || hour >= entries[index].UntilHour) continue;
                selected = index;
                break; // Advance in catalog order, skipping entries outside their time window.
            }
            if (selected >= 0)
            {
                state.playCounts[selected]++;
                state.nextEntry = selected + 1 < entries.Length ? selected + 1 : 3;
            }
            return selected;
        }

        private IEnumerator LoadAndPlay(string path)
        {
            ResourceRequest request = Resources.LoadAsync<AudioClip>(path);
            yield return request;
            loadedClip = request.asset as AudioClip;
            if (loadedClip == null)
            {
                if (!warnedMissing)
                {
                    warnedMissing = true;
                    Debug.LogWarning("[StoreAnnouncements] Ses kayıtları henüz eklenmemiş: " + path);
                }
                loading = null;
                yield break;
            }
            loadedClip.LoadAudioData();
            while (loadedClip != null && loadedClip.loadState == AudioDataLoadState.Loading) yield return null;
            if (loadedClip == null || loadedClip.loadState != AudioDataLoadState.Loaded)
            { StopPlayback(); yield break; }
            if (MainMenuUI.IsMenuVisible) { StopPlayback(); yield break; }
            UpdateVolume();
            voice.clip = loadedClip;
            voice.Play();
            loading = null;
        }

        private void Update()
        {
            if (voice == null) return;
            if(LocalizationManager.Instance!=null && LocalizationManager.Instance.CurrentLanguage==GameLanguage.English)
            {StopPlayback();return;}
            if (MainMenuUI.IsMenuVisible || (TimeManager.Instance != null && TimeManager.Instance.Hour < 8))
            { StopPlayback(); return; }
            if (TimeManager.Instance != null && TimeManager.Instance.Hour < 24 &&
                StoreStatusManager.Instance != null && !StoreStatusManager.Instance.IsOpen)
            { StopPlayback(); return; }
            UpdateVolume();
            bool shouldPause = PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen;
            if (shouldPause && !paused && voice.isPlaying) { voice.Pause(); paused = true; }
            else if (!shouldPause && paused) { voice.UnPause(); paused = false; }
            if (loadedClip != null && loading == null && !paused && !voice.isPlaying) ReleaseClip();
        }

        private void UpdateVolume()
        {
            var audio = AudioManager.Instance;
            voice.volume = audio != null ? audio.SFXVolume : 0.8f;
            voice.mute = audio != null && audio.IsSFXMuted;
        }

        private void ReleaseClip()
        {
            voice.clip = null;
            if (loadedClip != null) Resources.UnloadAsset(loadedClip);
            loadedClip = null;
        }

        private void StopPlayback()
        {
            if (loading != null) { StopCoroutine(loading); loading = null; }
            if (voice != null) { voice.Stop(); ReleaseClip(); }
            paused = false;
        }

        public StoreAnnouncementSaveData CaptureState() => new StoreAnnouncementSaveData
        { dayKey = state.dayKey, lastSlot = state.lastSlot, nextEntry = state.nextEntry,
            playCounts = state.playCounts == null ? null : (int[])state.playCounts.Clone() };

        public void RestoreState(StoreAnnouncementSaveData saved)
        {
            StopPlayback();
            state = saved == null ? new StoreAnnouncementSaveData() : new StoreAnnouncementSaveData
            {
                dayKey = saved.dayKey, lastSlot = saved.lastSlot,
                nextEntry = Mathf.Clamp(saved.nextEntry, 3, StoreAnnouncementCatalog.Entries.Length - 1),
                playCounts = saved.playCounts == null ? null : (int[])saved.playCounts.Clone()
            };
        }

        private void OnDestroy()
        {
            StopPlayback();
            if (Instance == this) Instance = null;
        }

        private void OnDisable() => StopPlayback();
    }
}
