using UnityEditor;
using UnityEngine;

namespace Farm2Shelf.Editor
{
    // Speech files stay mono and compressed on mobile; only the current clip is loaded.
    public sealed class StoreAnnouncementAudioImporter : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Audio/Announcements/")) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = true;
            var settings = importer.defaultSampleSettings;
            settings.preloadAudioData = false;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.65f;
            settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride = 24000;
            importer.defaultSampleSettings = settings;
        }
    }
}
