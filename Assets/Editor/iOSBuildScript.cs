using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Farm2Shelf.Editor
{
    /// <summary>
    /// GitHub Actions CI/CD için iOS Xcode projesi dışa aktarma (Export) derleme betiği.
    /// Bundle ID (com.ektgames.farm2shelf), Şirket Adı (EKTGAMES) ve iOS PlayerSettings ayarlarını kesinleştirir.
    /// </summary>
    public static class iOSBuildScript
    {
        public static void BuildForiOS()
        {
            Debug.Log("[iOSBuildScript] === iOS Xcode Projesi Dışa Aktarma Başlatılıyor ===");

            // 1. iOS Oyuncu Ayarlarını (Player Settings) Kesinleştir
            PlayerSettings.companyName = "EKTGAMES";
            PlayerSettings.productName = "Farm2Shelf";
            PlayerSettings.iOS.applicationDisplayName = "Farm2Shelf";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.ektgames.farm2shelf");
            PlayerSettings.bundleVersion = "0.1.0";

            // Komut satırından dinamik -buildNumber parametresi oku (Varsayılan: "1")
            string buildNumberStr = GetCommandLineArg("-buildNumber");
            if (string.IsNullOrEmpty(buildNumberStr))
            {
                buildNumberStr = "1";
            }
            PlayerSettings.iOS.buildNumber = buildNumberStr;

            Debug.Log($"[iOSBuildScript] Yapılandırılan Bundle ID: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS)}");
            Debug.Log($"[iOSBuildScript] Yapılandırılan Sürüm: {PlayerSettings.bundleVersion}, Build No: {PlayerSettings.iOS.buildNumber}");

            // 2. Çıktı Dizini Belirle (-customBuildPath veya -buildPath veya varsayılan "build/iOS")
            string customOutputPath = GetCommandLineArg("-customBuildPath");
            if (string.IsNullOrEmpty(customOutputPath))
            {
                customOutputPath = GetCommandLineArg("-buildPath");
            }
            if (string.IsNullOrEmpty(customOutputPath))
            {
                customOutputPath = "build/iOS";
            }

            if (!Directory.Exists(customOutputPath))
            {
                Directory.CreateDirectory(customOutputPath);
            }

            // 3. Aktif Sahneleri Dinamik Olarak Topla (EditorBuildSettings)
            List<string> enabledScenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                {
                    enabledScenes.Add(scene.path);
                }
            }

            if (enabledScenes.Count == 0)
            {
                // Düşme Kontrolü (Fallback Check)
                if (File.Exists("Assets/Scenes/SampleScene.unity"))
                {
                    enabledScenes.Add("Assets/Scenes/SampleScene.unity");
                    Debug.LogWarning("[iOSBuildScript] EditorBuildSettings boş, varsayılan SampleScene.unity kullanılıyor.");
                }
                else
                {
                    Debug.LogError("[iOSBuildScript] ❌ KRİTİK HATA: EditorBuildSettings içinde etkinleştirilmiş sahne yok ve SampleScene.unity bulunamadı!");
                    EditorApplication.Exit(1);
                    return;
                }
            }

            Debug.Log($"[iOSBuildScript] Derlemeye dahil edilen sahneler ({enabledScenes.Count}): " + string.Join(", ", enabledScenes));

            // 4. Unity Build Options Yapılandırması
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = enabledScenes.ToArray(),
                locationPathName = customOutputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            Debug.Log($"[iOSBuildScript] Xcode projesi şu konuma ihraç ediliyor: {customOutputPath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[iOSBuildScript] ✅ iOS Xcode projesi başarıyla ihraç edildi! Toplam boyut: {summary.totalSize} bayt");
            }
            else
            {
                Debug.LogError($"[iOSBuildScript] ❌ iOS İhraç Başarısız Oldu! Hata Sonucu: {summary.result}");
                EditorApplication.Exit(1);
            }
        }

        private static string GetCommandLineArg(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
