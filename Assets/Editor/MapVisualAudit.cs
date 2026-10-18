#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Explicit, local request only. Does not save scenes, player data or camera settings.
[InitializeOnLoad]
public static class MapVisualAudit
{
    const string Request = "Temp/map-visual-audit.request";
    static double readyAt;
    static MapVisualAudit() { EditorApplication.update += Tick; }
    [Serializable] public class Entry { public string path; public Vector3 min, max; }
    [Serializable] public class Entries { public Entry[] items; }
    static string PathOf(Transform t) { return t.parent == null ? t.name : PathOf(t.parent)+"/"+t.name; }
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool("MapAuditStartedPlay", true);
            EditorApplication.isPlaying = true;
            readyAt = 0;
            return;
        }
        if (readyAt == 0) { readyAt = EditorApplication.timeSinceStartup + 5; return; }
        if (EditorApplication.timeSinceStartup < readyAt) return;
        string folder = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        try { Capture(folder); }
        catch (Exception e) { Directory.CreateDirectory(folder); File.WriteAllText(folder+"/error.txt",e.ToString()); }
        finally
        {
            readyAt = 0;
            if (SessionState.GetBool("MapAuditStartedPlay",false))
            { SessionState.SetBool("MapAuditStartedPlay",false); EditorApplication.isPlaying=false; }
        }
    }
    static void Capture(string folder)
    {
        Directory.CreateDirectory(folder);
        var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        File.WriteAllText(folder+"/bounds.json",JsonUtility.ToJson(new Entries { items=renderers.Select(r=>new Entry {
            path=PathOf(r.transform),min=r.bounds.min,max=r.bounds.max }).ToArray() }));
        var host=new GameObject("MapAuditCamera");
        var cam=host.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;
        cam.transform.rotation=Quaternion.Euler(90,0,0);cam.nearClipPlane=.3f;cam.farClipPlane=400;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.gray;
        var lightHost=new GameObject("MapAuditDaylight");var light=lightHost.AddComponent<Light>();
        light.type=LightType.Directional;light.intensity=1.1f;light.transform.rotation=Quaternion.Euler(65,-25,0);
        var ambient=RenderSettings.ambientMode;var color=RenderSettings.ambientLight;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.6f,.6f);
        var rt=new RenderTexture(1600,1200,24);var previous=RenderTexture.active;
        var tex=new Texture2D(1600,1200,TextureFormat.RGB24,false);
        try
        {
            cam.targetTexture=rt;
            var regions=new[] {
                ("river_north",-95f,175f,28f),("river_middle",-95f,-9f,28f),("river_south",-95f,-55f,28f),
                ("new_neighborhood",112f,-70f,65f),("northeast",115f,115f,72f),
                ("north_apartments",0f,115f,78f),("farm",55f,20f,38f),
                ("townhall",0f,-33f,38f),("south",0f,-92f,58f),
                ("west_south",-170f,-85f,58f),("west_villas",-170f,80f,100f)};
            foreach(var r in regions)
            {
                cam.transform.position=new Vector3(r.Item2,180,r.Item3);cam.orthographicSize=r.Item4;
                cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1200),0,0);tex.Apply();
                File.WriteAllBytes(folder+"/"+r.Item1+".png",tex.EncodeToPNG());
            }
            File.WriteAllText(folder+"/done.txt","Captured "+regions.Length+" regions; renderers="+renderers.Length);
        }
        finally
        {
            RenderTexture.active=previous;cam.targetTexture=null;
            RenderSettings.ambientMode=ambient;RenderSettings.ambientLight=color;
            UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(lightHost);
        }
    }
}
#endif
