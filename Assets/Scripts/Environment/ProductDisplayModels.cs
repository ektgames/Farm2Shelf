using System;
using System.Collections.Generic;
using Farm2Shelf.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Farm2Shelf.Environment
{
    // Display geometry only. Inventory, prices, picking and stocking stay in their existing systems.
    internal static partial class ProductDisplayModels
    {
        private static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        private static Dictionary<string, string> names;
        private static Material material;
        private static Texture2D palette;
        // A shared opaque palette keeps every product to one renderer and one material.
        private const int White=0, Cream=1, Gold=2, Bread=3, Brown=4, Dark=5, Black=6,
            Red=7, Pink=8, Orange=9, Yellow=10, Green=11, Leaf=12, Lime=13, Purple=14,
            Blue=15, Sky=16, Teal=17, Silver=18, Paper=19, Meat=20, Salmon=21,
            Olive=22, Cocoa=23, Lilac=24, Ice=25, Wine=26, Mint=27, Sand=28, Navy=29, Peach=30, Steel=31;
        private static readonly string[] colors = {
            "F6F4EB","E9DCB6","CEAA52","C98835","89552F","332D2B","19252D",
            "DC3831","EF8C9E","EE8730","EDCE44","346E34","579044","A1BB52","674474",
            "3271A0","8DC3DB","339E9B","BDC6C6","C4A57C","AB3C42","E88C73",
            "65743C","593528","B29BC6","D4E4E5","693346","98C8A3","DBC59D","273D59","EBB17C","697B82"
        };

        internal static string Resolve(string id, string label)
        {
            if (!string.IsNullOrEmpty(id)) return id;
            if (names == null)
            {
                names = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in WholesaleDatabase.GetAllProducts())
                { Alias(p.name,p.id); Alias(p.nameEn,p.id); Alias(p.LocalizedName,p.id); Alias(p.id,p.id); }
                foreach (var s in GardenSeedDatabase.GetAllSeeds())
                {
                    Alias(s.id,s.id); Alias(s.name,s.id); Alias(s.nameEn,s.id);
                    Alias(ProductPassportService.StripCropSuffix(s.name),s.id);
                    Alias(ProductPassportService.StripCropSuffix(s.nameEn),s.id);
                }
            }
            if (label != null && names.TryGetValue(label.Trim(),out string result)) return result;
            var seed = GardenSeedDatabase.FindSeedByLabel(label);
            return seed != null ? seed.id : "";
        }

        private static void Alias(string name,string id)
        {
            if (!string.IsNullOrWhiteSpace(name)) names[name.Trim()] = id;
        }

        internal static bool Attach(Transform root,string id,string label,float scale)
        {
            string key = Resolve(id,label);
            if (string.IsNullOrEmpty(key)) return false;
            if (!meshes.TryGetValue(key,out Mesh mesh) || mesh == null)
            {
                var art = new Drawing();
                if (!DrawWholesale(art,key) && !DrawHarvest(art,key) && !DrawWorkshop(art,key)) return false;
                mesh = art.Finish(key);
                meshes[key] = mesh;
            }
            var visual = new GameObject("Display_"+key);
            visual.transform.SetParent(root,false);
            visual.transform.localScale = Vector3.one * scale;
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            return true;
        }

        private static Material GetMaterial()
        {
            if (material != null) return material;
            palette = new Texture2D(32,1,TextureFormat.RGBA32,false,false) { name="Product display palette", filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp };
            var pixels = new Color[32];
            for (int i=0;i<32;i++) ColorUtility.TryParseHtmlString("#"+colors[i],out pixels[i]);
            palette.SetPixels(pixels); palette.Apply(false,true);
            material = new Material(Procedural3DProductBuilder.CreateMaterial(Color.white,0f,.32f)) { name="Product display - shared palette", enableInstancing=true };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap",palette);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex",palette);
            return material;
        }

        // Coordinates are authored individually in modelling units. Finished meshes have a
        // grounded, centred pivot and a conservative envelope for both old and imported shelves.
        private sealed class Drawing
        {
            private readonly List<Vector3> v = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();
            private void Triangle(Vector3 a,Vector3 b,Vector3 c,int color)
            {
                int n=v.Count; Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
                v.Add(a);v.Add(b);v.Add(c);
                for(int i=0;i<3;i++){normals.Add(normal);uv.Add(new Vector2((color+.5f)/32f,.5f));triangles.Add(n+i);}
            }
            private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int color)
            { Triangle(a,b,c,color);Triangle(a,c,d,color); }
            internal void Box(float x,float y,float z,float w,float h,float d,int color,float tilt=0)
            {
                Quaternion q=Quaternion.Euler(0,0,tilt); Vector3 p=new Vector3(x,y,z);
                var a=new Vector3[8];
                for(int i=0;i<8;i++) a[i]=p+q*new Vector3((i&1)==0?-w/2:w/2,(i&2)==0?-h/2:h/2,(i&4)==0?-d/2:d/2);
                Quad(a[0],a[2],a[3],a[1],color);Quad(a[4],a[5],a[7],a[6],color);
                Quad(a[0],a[4],a[6],a[2],color);Quad(a[1],a[3],a[7],a[5],color);
                Quad(a[0],a[1],a[5],a[4],color);Quad(a[2],a[6],a[7],a[3],color);
            }
            internal void Ball(float x,float y,float z,float w,float h,float d,int color,int stripeColor=-1)
            {
                const int sides=12,rings=6;
                Vector3 Point(int r,int s){float t=Mathf.PI*r/rings,a=2*Mathf.PI*s/sides;return new Vector3(x+w*.5f*Mathf.Sin(t)*Mathf.Cos(a),y+h*.5f*Mathf.Cos(t),z+d*.5f*Mathf.Sin(t)*Mathf.Sin(a));}
                for(int r=0;r<rings;r++)for(int s=0;s<sides;s++)
                    Quad(Point(r,s),Point(r,s+1),Point(r+1,s+1),Point(r+1,s),stripeColor>=0 && s%2==0?stripeColor:color);
            }
            internal void Cylinder(float x,float y,float z,float radius,float height,int color,float topRadius=-1)
            {
                if(topRadius<0)topRadius=radius;
                const int sides=12;
                for(int i=0;i<sides;i++)
                {
                    float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;
                    Vector3 lo=new Vector3(x+radius*Mathf.Cos(a),y-height/2,z+radius*Mathf.Sin(a));
                    Vector3 ln=new Vector3(x+radius*Mathf.Cos(b),y-height/2,z+radius*Mathf.Sin(b));
                    Vector3 hi=new Vector3(x+topRadius*Mathf.Cos(a),y+height/2,z+topRadius*Mathf.Sin(a));
                    Vector3 hn=new Vector3(x+topRadius*Mathf.Cos(b),y+height/2,z+topRadius*Mathf.Sin(b));
                    Quad(lo,hi,hn,ln,color);
                    Triangle(new Vector3(x,y+height/2,z),hn,hi,color);
                    Triangle(new Vector3(x,y-height/2,z),lo,ln,color);
                }
            }
            internal void Ring(float x,float y,float z,float radius,float tube,int color,float start=0,float sweep=360)
            {
                const int segments=20,sides=6;
                Vector3 Point(int i,int j){float a=(start+sweep*i/segments)*Mathf.Deg2Rad,b=j*2*Mathf.PI/sides;return new Vector3(x+(radius+tube*Mathf.Cos(b))*Mathf.Cos(a),y+tube*Mathf.Sin(b),z+(radius+tube*Mathf.Cos(b))*Mathf.Sin(a));}
                for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)Quad(Point(i,j),Point(i,j+1),Point(i+1,j+1),Point(i+1,j),color);
            }
            internal void LeafShape(float x,float y,float z,float w,float h,int color,float tilt=0)
            {
                Quaternion q=Quaternion.Euler(0,0,tilt);Vector3 p=new Vector3(x,y,z);
                Vector3 a=p+q*new Vector3(0,-h/2,0),b=p+q*new Vector3(-w/2,0,0),c=p+q*new Vector3(0,h/2,0),d=p+q*new Vector3(w/2,0,0),m=p+new Vector3(0,0,.06f);
                Triangle(a,d,m,color);Triangle(d,c,m,color);Triangle(c,b,m,color);Triangle(b,a,m,color);
                Triangle(a,m,d,color);Triangle(d,m,c,color);Triangle(c,m,b,color);Triangle(b,m,a,color);
            }
            internal Mesh Finish(string id)
            {
                // Long loose vegetables lie across the shallow produce bins.
                switch(id)
                {
                    case "spring_carrot": case "autumn_wintercarrot": case "winter_carrot":
                    case "spring_asparagus": case "winter_leek": case "summer_okra":
                        Quaternion turn=Quaternion.Euler(0,0,90);
                        for(int i=0;i<v.Count;i++){v[i]=turn*v[i];normals[i]=turn*normals[i];}
                        break;
                }
                Bounds bounds=new Bounds(v[0],Vector3.zero);foreach(var point in v)bounds.Encapsulate(point);
                float factor=Mathf.Min(.18f/Mathf.Max(.001f,bounds.size.x),.22f/Mathf.Max(.001f,bounds.size.y),.125f/Mathf.Max(.001f,bounds.size.z));
                Vector3 pivot=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                for(int i=0;i<v.Count;i++)v[i]=(v[i]-pivot)*factor;
                var mesh=new Mesh{name="Hand designed product "+id};
                mesh.SetVertices(v);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
                mesh.UploadMeshData(false);
                return mesh;
            }
        }

        private static void Label(Drawing a,int color,float width=.48f,float y=.48f,float z=.266f)
        {
            a.Box(0,y,z,width,.30f,.012f,Cream);
            a.Box(0,y+.08f,z+.009f,width*.8f,.045f,.008f,color);
            a.Box(0,y-.08f,z+.009f,width*.62f,.023f,.008f,color);
        }
        private static void Packet(Drawing a,int color,int contents,float width=.75f,float height=1f)
        {
            a.Box(0,height/2,0,width,height,.36f,color);
            a.Box(0,height-.03f,0,width+.04f,.06f,.39f,Paper);
            a.Box(0,.03f,0,width+.04f,.06f,.39f,Paper);
            a.Box(0,height*.48f,.188f,width*.70f,height*.43f,.015f,contents);
            a.Box(0,height*.78f,.20f,width*.72f,.10f,.016f,White);
        }
        private static void Bottle(Drawing a,int body,int band,int cap,float height=1.3f,float width=.52f)
        {
            a.Cylinder(0,height*.35f,0,width/2,height*.70f,body);
            a.Cylinder(0,height*.77f,0,width/2,height*.14f,body,width*.18f);
            a.Cylinder(0,height*.88f,0,width*.18f,height*.10f,body);
            a.Cylinder(0,height*.96f,0,width*.21f,height*.08f,cap);
            a.Cylinder(0,height*.38f,0,width*.505f,height*.30f,band);
            Label(a,band,width*.6f,height*.38f,width*.51f);
        }
        private static void Jar(Drawing a,int fill,int lid,int band,float height=.85f)
        {
            a.Cylinder(0,height*.45f,0,.34f,height*.85f,fill);
            a.Cylinder(0,height*.87f,0,.32f,height*.10f,Ice);
            a.Cylinder(0,height*.97f,0,.36f,height*.12f,lid);
            a.Cylinder(0,height*.43f,0,.345f,height*.34f,band);
            Label(a,fill,.48f,height*.43f,.35f);
        }
        private static void Tray(Drawing a,int color=Black)
        {
            a.Box(0,.05f,0,1f,.1f,.68f,color);
            a.Box(0,.12f,.32f,1f,.08f,.05f,color);a.Box(0,.12f,-.32f,1f,.08f,.05f,color);
            a.Box(-.48f,.12f,0,.05f,.08f,.64f,color);a.Box(.48f,.12f,0,.05f,.08f,.64f,color);
        }
        private static void GrainWindow(Drawing a,int color,int count=9)
        {for(int i=0;i<count;i++)a.Ball((i%3-1)*.13f,.30f+(i/3)*.10f,.205f,.10f,.065f,.035f,color);}
        private static void Eggs(Drawing a,bool organic)
        {
            Tray(a,Paper);
            for(int i=0;i<10;i++)a.Ball((i%5-2)*.17f,.23f,(i/5==0?-.15f:.15f),.15f,.24f,.15f,organic?Sand:Cream);
            a.Box(0,.25f,0,1.02f,.06f,.10f,organic?Green:Red);
        }
    }
}
