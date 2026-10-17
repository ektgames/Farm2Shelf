using System.Collections.Generic;
using Farm2Shelf.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Farm2Shelf.Environment
{
    // Shared, batched decorative geometry. No primitive colliders or extra input handlers.
    public static class DetailedDecorationBuilder
    {
        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private static Material Surface(string name, Color color, float metal = 0f, float gloss = 0.35f, bool glow = false)
        {
            if (materials.TryGetValue(name, out var cached) && cached != null) return cached;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "Decor_" + name; m.color = color; m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", gloss); m.enableInstancing = true;
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 0.65f); }
            materials[name] = m; return m;
        }
        private static Material Gold => Surface("BrushedBrass", new Color(.64f,.43f,.15f), .8f,.55f);
        private static Material Dark => Surface("Graphite", new Color(.035f,.045f,.065f), .35f,.5f);
        private static Material Marble => Surface("IvoryStone", new Color(.82f,.81f,.75f), .1f,.65f);
        private static Material Green => Surface("Leaves", new Color(.09f,.28f,.13f));
        private static Material Cyan => Surface("AquaGlow", new Color(.1f,.7f,.85f), .15f,.6f,true);
        private static Material Rose => Surface("Rose", new Color(.79f,.18f,.35f),0,.3f);
        public static bool Handles(FurnitureType t) => t == FurnitureType.ChandelierCrystal || t == FurnitureType.RedCarpet ||
            t == FurnitureType.DigitalMenuBoard || t == FurnitureType.FlowerArch || t == FurnitureType.HologramProjector ||
            t == FurnitureType.WelcomeMat || t == FurnitureType.FountainSmall || t == FurnitureType.AdBanner || t == FurnitureType.NeonSign;
        public static bool IsCeiling(FurnitureType t) => t == FurnitureType.ChandelierCrystal || t == FurnitureType.DigitalMenuBoard;

        // Explicit heights keep overhead decorations independent of floor furniture.
        public static Bounds GetPlacementBounds(FurnitureType type, Vector3 position, float width, float depth)
        {
            float height = 1.8f, center = .9f;
            switch (type)
            {
                case FurnitureType.ChandelierCrystal: center = 2.85f; height = .8f; break;
                case FurnitureType.DigitalMenuBoard: center = 2.5f; height = 1.5f; break;
                case FurnitureType.NeonSign: center = 2.2f; height = .74f; break;
                case FurnitureType.WallClock: center = 1.85f; height = .7f; break;
                case FurnitureType.FlowerArch: center = 1.43f; height = 2.86f; break;
                case FurnitureType.WelcomeMat:
                case FurnitureType.RedCarpet: center = .025f; height = .05f; break;
                case FurnitureType.FountainSmall: center = .65f; height = 1.3f; break;
                case FurnitureType.HologramProjector: center = .72f; height = 1.44f; break;
            }
            return new Bounds(position + Vector3.up * center,
                new Vector3(Mathf.Max(.1f, width - .10f), height, Mathf.Max(.1f, depth - .10f)));
        }

        public static void ConfigurePlacementColliders(GameObject root, FurnitureType type)
        {
            var box = root.GetComponent<BoxCollider>();
            if (box == null) box = root.AddComponent<BoxCollider>();
            var oldNav = root.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (oldNav != null) oldNav.enabled = false;
            box.enabled = true;
            box.isTrigger = true;
            switch (type)
            {
                case FurnitureType.WelcomeMat:
                    box.center = new Vector3(0,.025f,0); box.size = new Vector3(1.35f,.055f,.85f); break;
                case FurnitureType.RedCarpet:
                    box.center = new Vector3(0,.025f,0); box.size = new Vector3(1.5f,.055f,2.5f); break;
                case FurnitureType.ChandelierCrystal:
                    box.center = new Vector3(0,2.85f,0); box.size = new Vector3(1.4f,.8f,1.4f); break;
                case FurnitureType.NeonSign:
                    box.center = new Vector3(0,2.2f,.05f); box.size = new Vector3(1.6f,.74f,.12f); break;
                case FurnitureType.DigitalMenuBoard:
                    box.center = new Vector3(0,2.5f,.25f); box.size = new Vector3(1.7f,1.5f,.6f); break;
                case FurnitureType.FlowerArch:
                    // Never add a solid box across the open archway.
                    box.enabled = false;
                    if (oldNav != null) Object.Destroy(oldNav);
                    ArchCollider(root, "ArchLeftFoot", new Vector3(-1.2f,1.05f,0), new Vector3(.42f,2.1f,.55f), true);
                    ArchCollider(root, "ArchRightFoot", new Vector3(1.2f,1.05f,0), new Vector3(.42f,2.1f,.55f), true);
                    ArchCollider(root, "ArchCrown", new Vector3(0,2.65f,0), new Vector3(2.8f,.35f,.55f), false);
                    return;
                case FurnitureType.FountainSmall:
                    box.center = new Vector3(0,.55f,0); box.size = new Vector3(1.7f,1.1f,1.7f); box.isTrigger = false; break;
                case FurnitureType.AdBanner:
                    box.center = new Vector3(0,.86f,0); box.size = new Vector3(1.16f,1.72f,.7f); box.isTrigger = false; break;
                case FurnitureType.HologramProjector:
                    box.center = new Vector3(0,.22f,0); box.size = new Vector3(.85f,.44f,.85f); box.isTrigger = false; break;
            }
            if (!box.isTrigger)
            {
                var nav = oldNav != null ? oldNav : root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                nav.enabled=true; nav.shape=UnityEngine.AI.NavMeshObstacleShape.Box;
                nav.center=box.center; nav.size=box.size; nav.carving=true;
            }
            else if (oldNav != null) Object.Destroy(oldNav);
        }
        private static void ArchCollider(GameObject root, string name, Vector3 center, Vector3 size, bool solid)
        {
            var t=root.transform.Find(name);
            if(t==null){var o=new GameObject(name);o.transform.SetParent(root.transform,false);t=o.transform;}
            var col=t.GetComponent<BoxCollider>();if(col==null)col=t.gameObject.AddComponent<BoxCollider>();
            col.center=center;col.size=size;col.isTrigger=!solid;
            if(solid)
            {
                var nav=t.GetComponent<UnityEngine.AI.NavMeshObstacle>();if(nav==null)nav=t.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                nav.shape=UnityEngine.AI.NavMeshObstacleShape.Box;nav.center=center;nav.size=size;nav.carving=true;
            }
        }

        public static void Build(GameObject root, FurnitureType type, bool ghost)
        {
            switch (type)
            {
                case FurnitureType.ChandelierCrystal: Chandelier(root.transform); break;
                case FurnitureType.RedCarpet: Carpet(root.transform, false); break;
                case FurnitureType.WelcomeMat: Carpet(root.transform, true); break;
                case FurnitureType.FlowerArch: Arch(root.transform); break;
                case FurnitureType.FountainSmall: Fountain(root.transform); break;
                case FurnitureType.AdBanner: Advert(root.transform); break;
                case FurnitureType.NeonSign: Neon(root.transform); break;
                case FurnitureType.DigitalMenuBoard: Digital(root.transform); break;
                case FurnitureType.HologramProjector: Hologram(root.transform); break;
            }
            Batch(root.transform);
            if (!ghost && (type == FurnitureType.HologramProjector || type == FurnitureType.FountainSmall))
                root.AddComponent<DecorationVisualEffects>().Initialize(type);
        }
        private static Mesh detailSphere;
        private static Mesh GetDetailSphere()
        {
            if(detailSphere != null) return detailSphere;
            const int rings=8, sides=12;
            var vertices=new Vector3[(rings+1)*(sides+1)];
            var normals=new Vector3[vertices.Length];
            var triangles=new List<int>();
            for(int y=0;y<=rings;y++)
                for(int x=0;x<=sides;x++)
                {
                    float latitude=y*Mathf.PI/rings, longitude=x*Mathf.PI*2/sides;
                    Vector3 n=new Vector3(Mathf.Sin(latitude)*Mathf.Cos(longitude),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(longitude));
                    int index=y*(sides+1)+x;vertices[index]=n*.5f;normals[index]=n;
                    if(y<rings && x<sides)
                    {
                        int a=index,b=index+sides+1;
                        triangles.Add(a);triangles.Add(a+1);triangles.Add(b);
                        triangles.Add(a+1);triangles.Add(b+1);triangles.Add(b);
                    }
                }
            detailSphere=new Mesh{name="DecorationDetailSphere"};
            detailSphere.vertices=vertices;detailSphere.normals=normals;detailSphere.SetTriangles(triangles,0);detailSphere.RecalculateBounds();
            return detailSphere;
        }
        private static GameObject Part(Transform parent, string name, PrimitiveType kind, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject o;
            if(kind==PrimitiveType.Sphere)
            {
                o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
                o.GetComponent<MeshFilter>().sharedMesh=GetDetailSphere();
            }
            else o=GameObject.CreatePrimitive(kind);
            o.name=name; o.transform.SetParent(parent,false);
            o.transform.localPosition=pos; o.transform.localScale=size;
            var c=o.GetComponent<Collider>(); if(c!=null){c.enabled=false; Object.DestroyImmediate(c);}
            o.GetComponent<Renderer>().sharedMaterial=mat; return o;
        }
        private static void Box(Transform p,string n,Vector3 v,Vector3 s,Material m) => Part(p,n,PrimitiveType.Cube,v,s,m);
        private static void Ball(Transform p,Vector3 v,Vector3 s,Material m) => Part(p,"Detail",PrimitiveType.Sphere,v,s,m);
        private static void Rod(Transform p,Vector3 a,Vector3 b,float radius,Material m)
        {
            var o=Part(p,"Tube",PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(radius*2,(b-a).magnitude*.5f,radius*2),m);
            o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        private static void Ring(Transform p,Vector3 center,float radius,float tube,Material m, Quaternion rotation)
        {
            const int n=48,k=8; var v=new Vector3[n*k];var tri=new int[n*k*6];
            for(int i=0;i<n;i++) for(int j=0;j<k;j++)
            {
                float a=i*Mathf.PI*2/n,b=j*Mathf.PI*2/k;
                v[i*k+j]=new Vector3(Mathf.Cos(a)*(radius+tube*Mathf.Cos(b)),tube*Mathf.Sin(b),Mathf.Sin(a)*(radius+tube*Mathf.Cos(b)));
                int q=(i*k+j)*6,x=i*k+j,y=((i+1)%n)*k+j,z=((i+1)%n)*k+(j+1)%k,w=i*k+(j+1)%k;
                tri[q]=x;tri[q+1]=z;tri[q+2]=y;tri[q+3]=x;tri[q+4]=w;tri[q+5]=z;
            }
            var mesh=new Mesh{name="Decor_Ring"};mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var o=new GameObject("Moulding",typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(p,false);o.transform.localPosition=center;o.transform.localRotation=rotation;
            o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=m;
        }
        private static void Word(Transform p,string text,Vector3 pos,float size,Color color,Quaternion rotation,Vector2 availableSize)
        {
            var o=new GameObject("Inscription");o.transform.SetParent(p,false);o.transform.localPosition=pos;o.transform.localRotation=rotation;
            var t=o.AddComponent<TextMesh>();t.text=text;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=64;t.characterSize=size;
            t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;
            var renderer=o.GetComponent<MeshRenderer>();
            renderer.sharedMaterial=t.font.material;
            // Fit the generated glyphs in their own XY plane, before the mat's rotation.
            // characterSize alone does not constrain a TextMesh to the furniture surface.
            Bounds bounds=renderer.localBounds;
            float width=bounds.size.x, height=bounds.size.y;
            if(width>0.0001f && height>0.0001f)
            {
                float fit=Mathf.Min(1f,Mathf.Min(availableSize.x/width,availableSize.y/height));
                o.transform.localScale=Vector3.one*fit;
                o.transform.localPosition=pos-rotation*new Vector3(bounds.center.x,bounds.center.y,0)*fit;
            }
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
        }
        private static void Carpet(Transform p,bool mat)
        {
            float w=mat?1.35f:1.5f,d=mat?.85f:2.5f;
            var fabric=Surface(mat?"Coir":"CrimsonVelvet",mat?new Color(.48f,.32f,.15f):new Color(.48f,.015f,.045f),0,.15f);
            Box(p,"RubberBacking",new Vector3(0,.014f,0),new Vector3(w,.028f,d),Dark);
            Box(p,"WovenPile",new Vector3(0,.032f,0),new Vector3(w-.045f,.018f,d-.045f),fabric);
            var border=mat?Dark:Gold;
            foreach(float x in new[]{-1f,1f}) Box(p,"StitchedEdge",new Vector3(x*(w*.5f-.07f),.044f,0),new Vector3(.025f,.005f,d-.12f),border);
            foreach(float z in new[]{-1f,1f}) Box(p,"StitchedEdge",new Vector3(0,.044f,z*(d*.5f-.07f)),new Vector3(w-.12f,.005f,.025f),border);
            for(int i=0;i<26;i++) Box(p,"Weave",new Vector3(0,.042f,-d*.43f+i*d*.86f/25),new Vector3(w-.2f,.002f,.003f),border);
            if(mat) Word(p,"WELCOME",new Vector3(0,.051f,0),.14f,new Color(.95f,.84f,.6f),Quaternion.Euler(90,0,0),new Vector2(w-.32f,d-.38f));
        }
        private static void Chandelier(Transform p)
        {
            Part(p,"CeilingRose",PrimitiveType.Cylinder,new Vector3(0,3.21f,0),new Vector3(.36f,.04f,.36f),Gold);
            Rod(p,new Vector3(0,3.21f,0),new Vector3(0,2.85f,0),.025f,Gold);
            var crystal=Surface("CutCrystal",new Color(.64f,.86f,.89f),.32f,.95f);
            foreach(float r in new[]{.62f,.38f})
            {
                float y=r>.5f?2.87f:2.71f; Ring(p,new Vector3(0,y,0),r,.022f,Gold,Quaternion.identity);
                for(int i=0;i<20;i++)
                {
                    float a=i*Mathf.PI*.1f;Vector3 top=new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);
                    Rod(p,top,top+Vector3.down*.075f,.006f,Gold);
                    var gem=Part(p,"CrystalPrism",PrimitiveType.Cube,top+Vector3.down*.15f,new Vector3(.04f,.19f,.04f),crystal);
                    gem.transform.localRotation=Quaternion.Euler(0,i*18+45,0);
                }
            }
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Rod(p,new Vector3(0,3.04f,0),new Vector3(Mathf.Cos(a)*.62f,2.87f,Mathf.Sin(a)*.62f),.012f,Gold);}
            Ball(p,new Vector3(0,2.77f,0),Vector3.one*.2f,Surface("WarmLED",new Color(1,.76f,.38f),0,.5f,true));
        }
        private static void Arch(Transform p)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Box(p,"Pedestal",new Vector3(side*1.2f,.1f,0),new Vector3(.38f,.2f,.5f),Marble);
                Rod(p,new Vector3(side*1.2f,.2f,0),new Vector3(side*1.2f,2.05f,0),.05f,Gold);
            }
            Vector3 last=new Vector3(1.2f,2.05f,0);
            for(int i=1;i<=24;i++)
            {
                float a=i*Mathf.PI/24;Vector3 v=new Vector3(Mathf.Cos(a)*1.2f,2.05f+Mathf.Sin(a)*.63f,0);
                Rod(p,last,v,.045f,Gold);last=v;
            }
            for(int i=0;i<42;i++)
            {
                float a=i*Mathf.PI/41;Vector3 v=new Vector3(Mathf.Cos(a)*1.2f,2.05f+Mathf.Sin(a)*.63f,0);
                Ball(p,v+new Vector3(0,.05f,.025f),new Vector3(.2f,.11f,.2f),Green);
                for(int k=0;k<5;k++){float b=k*Mathf.PI*2/5;Ball(p,v+new Vector3(Mathf.Cos(b)*.052f,Mathf.Sin(b)*.052f,-.065f),new Vector3(.09f,.085f,.04f),i%3==0?Rose:Marble);}
                Ball(p,v+new Vector3(0,0,-.09f),Vector3.one*.055f,Gold);
            }
            for(int s=-1;s<=1;s+=2)for(int i=0;i<10;i++) Ball(p,new Vector3(s*1.2f,.35f+i*.16f,0),new Vector3(.18f,.23f,.16f),Green);
        }
        private static void Fountain(Transform p)
        {
            Part(p,"Plinth",PrimitiveType.Cylinder,new Vector3(0,.08f,0),new Vector3(1.7f,.08f,1.7f),Marble);
            Part(p,"Basin",PrimitiveType.Cylinder,new Vector3(0,.2f,0),new Vector3(1.55f,.1f,1.55f),Marble);
            Ring(p,new Vector3(0,.31f,0),.72f,.065f,Marble,Quaternion.identity);
            Part(p,"WaterSurface",PrimitiveType.Cylinder,new Vector3(0,.315f,0),new Vector3(1.32f,.008f,1.32f),Cyan);
            Rod(p,new Vector3(0,.32f,0),new Vector3(0,.87f,0),.075f,Marble);
            Part(p,"UpperBowl",PrimitiveType.Cylinder,new Vector3(0,.82f,0),new Vector3(.63f,.065f,.63f),Marble);
            Ring(p,new Vector3(0,.89f,0),.29f,.04f,Gold,Quaternion.identity);
            Ball(p,new Vector3(0,.91f,0),Vector3.one*.12f,Gold);
            for(int k=0;k<8;k++)
            {
                float a=k*Mathf.PI/4;Vector3 last=new Vector3(0,.94f,0);
                for(int i=1;i<=12;i++){float t=i/12f;var v=new Vector3(Mathf.Cos(a)*.55f*t,.94f+.75f*t-1.35f*t*t,Mathf.Sin(a)*.55f*t);Rod(p,last,v,.009f,Cyan);last=v;}
            }
        }
        private static void Advert(Transform p)
        {
            foreach(float x in new[]{-.5f,.5f})
            {
                Rod(p,new Vector3(x,.05f,-.25f),new Vector3(x,1.65f,0),.026f,Gold);
                Rod(p,new Vector3(x,.05f,.35f),new Vector3(x,1.65f,0),.026f,Gold);
                Box(p,"RubberFoot",new Vector3(x,.03f,-.25f),new Vector3(.1f,.06f,.12f),Dark);
            }
            Box(p,"BoardFrame",new Vector3(0,1.02f,-.04f),new Vector3(1.14f,1.32f,.09f),Gold);
            Box(p,"Slate",new Vector3(0,1.02f,-.091f),new Vector3(1.04f,1.22f,.02f),Dark);
            // Deliberately abstract pseudo-letter strokes, not readable copy.
            for(int row=0;row<7;row++)for(int col=0;col<9-row%3;col++)
            {
                float x=-.42f+col*.09f,y=1.52f-row*.16f;
                Box(p,"LetterStroke",new Vector3(x,y,-.105f),new Vector3(.052f,.011f,.004f),row<2?Gold:Marble);
                Box(p,"LetterStroke",new Vector3(x-.02f,y-.022f,-.105f),new Vector3(.009f,.05f,.004f),row<2?Gold:Marble);
            }
        }
        private static void Neon(Transform p)
        {
            Box(p,"NeonBacking",new Vector3(0,2.2f,.034f),new Vector3(1.6f,.72f,.045f),Dark);
            // The wall-mount convention faces inward along local +Z.
            foreach(float y in new[]{1.88f,2.52f}) Rod(p,new Vector3(-.73f,y,.075f),new Vector3(.73f,y,.075f),.014f,Cyan);
            foreach(float x in new[]{-.73f,.73f}) Rod(p,new Vector3(x,1.88f,.075f),new Vector3(x,2.52f,.075f),.014f,Cyan);
            // Abstract handwritten neon strokes, intentionally not a readable brand name.
            // All points stay inside the backing, independently of fonts and resolution.
            var lettering=Surface("NeonScript",new Color(.8f,.12f,.55f),0,.5f,true);
            for(int glyph=0;glyph<6;glyph++)
            {
                float x=-.60f+glyph*.205f;
                Vector3 previous=new Vector3(x,2.12f,.084f);
                for(int segment=1;segment<=14;segment++)
                {
                    float u=segment/14f;
                    float flourish=Mathf.Sin(u*Mathf.PI*2+(glyph%3)*.65f);
                    Vector3 next=new Vector3(x+u*.16f,2.2f+flourish*.105f+u*.035f,.084f);
                    Rod(p,previous,next,.010f,lettering);
                    previous=next;
                }
                Rod(p,new Vector3(x+.065f,2.13f,.084f),new Vector3(x+.09f,2.32f,.084f),.009f,lettering);
            }
            for(int i=0;i<7;i++) Rod(p,new Vector3(-.36f+i*.12f,1.99f,.08f),new Vector3(-.31f+i*.12f,1.99f,.08f),.01f,Cyan);
        }
        private static void Digital(Transform p)
        {
            Box(p,"CeilingMount",new Vector3(0,3.22f,.24f),new Vector3(.9f,.055f,.28f),Dark);
            foreach(float x in new[]{-.34f,.34f}) Rod(p,new Vector3(x,3.22f,.24f),new Vector3(x,2.84f,.24f),.017f,Gold);
            Box(p,"Projector",new Vector3(0,2.8f,.24f),new Vector3(.85f,.16f,.32f),Dark);
            Ball(p,new Vector3(0,2.79f,.06f),new Vector3(.1f,.07f,.035f),Cyan);
            // Wall image uses inward-facing, shallow geometry: no leaking real-time light.
            Box(p,"WallImageFrame",new Vector3(0,2.16f,.025f),new Vector3(1.7f,.93f,.012f),Dark);
            var ink=Surface("ScreenBlue",new Color(.04f,.14f,.22f),0,.35f,true);
            Box(p,"ProjectedImage",new Vector3(0,2.16f,.034f),new Vector3(1.62f,.85f,.008f),ink);
            for(int i=0;i<3;i++)
            {
                float x=-.5f+i*.5f;Ball(p,new Vector3(x,2.22f,.055f),new Vector3(.22f,.22f,.008f),i==0?Rose:i==1?Gold:Green);
                Box(p,"PriceLine",new Vector3(x,1.95f,.05f),new Vector3(.29f,.025f,.008f),Marble);
            }
            Box(p,"TitleLine",new Vector3(0,2.47f,.05f),new Vector3(1.3f,.025f,.008f),Cyan);
        }
        private static void Hologram(Transform p)
        {
            Part(p,"Pedestal",PrimitiveType.Cylinder,new Vector3(0,.18f,0),new Vector3(.85f,.18f,.85f),Dark);
            Ring(p,new Vector3(0,.35f,0),.36f,.018f,Cyan,Quaternion.identity);
            Part(p,"Optic",PrimitiveType.Cylinder,new Vector3(0,.375f,0),new Vector3(.48f,.025f,.48f),Gold);
            for(int i=0;i<3;i++)Ring(p,new Vector3(0,1.0f,0),.28f,.009f,Cyan,Quaternion.Euler(i*60,0,25));
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Ball(p,new Vector3(Mathf.Cos(a)*.31f,.4f,Mathf.Sin(a)*.31f),Vector3.one*.055f,Cyan);}
        }
        private static void Batch(Transform root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();var disposable=new List<GameObject>();var generated=new List<Mesh>();
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if(mf.sharedMesh==null)continue;var r=mf.GetComponent<MeshRenderer>();if(r==null)continue;
                if(!groups.TryGetValue(r.sharedMaterial,out var list)){list=new List<CombineInstance>();groups[r.sharedMaterial]=list;}
                list.Add(new CombineInstance{mesh=mf.sharedMesh,transform=root.worldToLocalMatrix*mf.transform.localToWorldMatrix});
                if(mf.sharedMesh.name=="Decor_Ring")generated.Add(mf.sharedMesh);disposable.Add(mf.gameObject);
            }
            var owner=root.gameObject.AddComponent<DecorationMeshOwner>();
            foreach(var pair in groups)
            {
                var mesh=new Mesh{name="DecorBatch",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());
                var o=new GameObject(pair.Key.name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(root,false);
                o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;owner.meshes.Add(mesh);
            }
            foreach(var o in disposable)Object.DestroyImmediate(o);
            foreach(var m in generated)Object.DestroyImmediate(m);
        }
    }
    public class DecorationMeshOwner : MonoBehaviour
    {
        public List<Mesh> meshes=new List<Mesh>();
        private void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);}
    }
    public class DecorationVisualEffects : MonoBehaviour
    {
        private FurnitureType type;
        private Light accentLight;
        private readonly List<Transform> dots=new List<Transform>();
        private readonly List<Material> owned=new List<Material>();
        public void Initialize(FurnitureType value)
        {
            type=value;
            if(type==FurnitureType.HologramProjector)
            {
                var lamp=new GameObject("DiscoAccent");lamp.transform.SetParent(transform,false);
                lamp.transform.localPosition=new Vector3(0,.55f,0);
                accentLight=lamp.AddComponent<Light>();accentLight.type=LightType.Point;
                accentLight.range=2.2f;accentLight.intensity=.65f;accentLight.shadows=LightShadows.None;
            }
            int count=type==FurnitureType.HologramProjector?12:16;
            for(int i=0;i<count;i++)
            {
                var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name="DecorEffect";o.transform.SetParent(transform,false);
                var c=o.GetComponent<Collider>();c.enabled=false;Destroy(c);
                var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));Color color=type==FurnitureType.HologramProjector?Color.HSVToRGB(i/(float)count,.72f,.65f):new Color(.18f,.6f,.72f);
                mat.SetColor("_BaseColor",color);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.45f);owned.Add(mat);
                var r=o.GetComponent<Renderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
                o.transform.localScale=type==FurnitureType.HologramProjector?new Vector3(.22f,.008f,.22f):Vector3.one*.03f;dots.Add(o.transform);
            }
        }
        private void Update()
        {
            float t=Time.time;
            if(accentLight!=null)accentLight.color=Color.HSVToRGB(Mathf.Repeat(t*.07f,1),.65f,.8f);
            for(int i=0;i<dots.Count;i++)
            {
                float a=t*.55f+i*Mathf.PI*2/dots.Count;
                if(type==FurnitureType.HologramProjector){float r=.65f+.55f*(.5f+.5f*Mathf.Sin(t*.65f+i));dots[i].localPosition=new Vector3(Mathf.Cos(a)*r,.022f,Mathf.Sin(a)*r);}
                else
                {
                    float u=Mathf.Repeat(t*.7f+i/(float)dots.Count,1);
                    float jetAngle=(i%8)*Mathf.PI/4;
                    dots[i].localPosition=new Vector3(Mathf.Cos(jetAngle)*.55f*u,.94f+.75f*u-1.35f*u*u,Mathf.Sin(jetAngle)*.55f*u);
                }
            }
        }
        private void OnDestroy(){foreach(var mat in owned)if(mat!=null)Destroy(mat);}
    }
}
