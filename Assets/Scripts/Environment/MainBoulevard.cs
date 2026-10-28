using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    // Shared dimensions for road geometry, route planning and signal interlocks.
    public static class MainBoulevard
    {
        public const float Center = -13.5f;
        public const float EastRoadEnd = 400f, EastTrafficPortal = 380f;
        // Include sidewalk width and the full crown radius, not just the trunk.
        public static bool BoundaryTreeClearsRoad(float z) => z < SouthEdge-5f || z > NorthEdge+5f;
        public const float NorthEdge = -6f, SouthEdge = -21f;
        public const float WestOuter = -7.5f, WestInner = -10.5f;
        public const float EastInner = -16.5f, EastOuter = -19.5f;
        public const float TaxiTurnX = 75f;
        public static readonly float[] Junctions = { -226f, -188f, -150f, -112f, -75f, 75f, 112.5f, 150f };
        public static float BridgeSupportTop(float centerX,float width)
        {
            float minimum=float.PositiveInfinity;
            for(int i=0;i<=32;i++)
                minimum=Mathf.Min(minimum,CityTrafficManager.GetBridgeElevation(centerX-width*.5f+width*i/32f,Center,out _));
            // Deck is 0.30 m thick. Extra clearance includes its tilted bottom corners.
            return minimum-.40f;
        }
        public static bool OnRoad(float z) => z >= SouthEdge - .1f && z <= NorthEdge + .1f;
        public static bool OnBridge(float x) => x > -109f && x < -81f;
        public static bool Opening(float x, float margin = 0f)
        {
            if (x > 167f-margin && x < 194f+margin) return true; // Railway crossing.
            foreach (float junction in Junctions) if (Mathf.Abs(x-junction) < 9f+margin) return true;
            return false;
        }

        public static List<Vector3> ShopUTurnRoute(Vector3 start)
        {
            float lane = Mathf.Abs(start.z-EastOuter)<.1f ? EastOuter : EastInner;
            float radius = (WestOuter-lane)*.5f;
            float turnCenter = (WestOuter+lane)*.5f;
            var path = new List<Vector3> { start, new Vector3(Mathf.Max(start.x,TaxiTurnX-18f),.05f,lane),
                new Vector3(TaxiTurnX,.05f,lane) };
            // Radius 4.5 m: turn entirely within the taxi-street junction opening.
            for (int i=1;i<=36;i++)
            {
                float angle = Mathf.PI*i/36f;
                path.Add(new Vector3(TaxiTurnX+radius*Mathf.Sin(angle),.05f,turnCenter-radius*Mathf.Cos(angle)));
            }
            path.Add(new Vector3(TaxiTurnX-15f,.05f,WestOuter));
            path.Add(new Vector3(-10f,.05f,WestOuter));
            return path;
        }

        public static List<Vector3> RoundJunctionTurns(List<Vector3> source)
        {
            if(source==null || source.Count<2)return source;
            var result=new List<Vector3>(source);
            foreach(float junction in Junctions)result=RebuildJunctionCurve(result,junction);
            return result;
        }
        private static List<Vector3> RebuildJunctionCurve(List<Vector3> source,float x)
        {
            const float radius=10f;
            bool Inside(Vector3 p)=>Mathf.Abs(p.x-x)<radius-.001f && Mathf.Abs(p.z-Center)<radius-.001f;
            var split=new List<Vector3>{source[0]};
            for(int i=1;i<source.Count;i++)
            {
                Vector3 a=source[i-1],b=source[i],d=b-a;var cuts=new List<float>();
                void Cut(float t)
                {
                    if(t<=.00001f||t>=.99999f)return;
                    Vector3 p=a+d*t;
                    if(Mathf.Abs(p.x-x)<=radius+.001f && Mathf.Abs(p.z-Center)<=radius+.001f)cuts.Add(t);
                }
                if(Mathf.Abs(d.x)>.0001f){Cut((x-radius-a.x)/d.x);Cut((x+radius-a.x)/d.x);}
                if(Mathf.Abs(d.z)>.0001f){Cut((Center-radius-a.z)/d.z);Cut((Center+radius-a.z)/d.z);}
                cuts.Sort();
                foreach(float t in cuts)if(FlatDistance(split[split.Count-1],a+d*t)>.001f)split.Add(a+d*t);
                if(FlatDistance(split[split.Count-1],b)>.001f)split.Add(b);
            }
            var result=new List<Vector3>{split[0]};
            int index=0;
            while(index<split.Count-1)
            {
                if(!Inside((split[index]+split[index+1])*.5f)){result.Add(split[++index]);continue;}
                int start=index;
                while(index<split.Count-1 && Inside((split[index]+split[index+1])*.5f))index++;
                Vector3 a=split[start],b=split[index];
                Vector3 incoming=split[start+1]-a,outgoing=b-split[index-1];incoming.y=outgoing.y=0;
                float cosine=Vector3.Dot(incoming.normalized,outgoing.normalized);
                // Keep an explicitly modelled U-turn, or an incomplete leg inside a junction.
                if(cosine<-.8f || Inside(a) || Inside(b))
                {for(int j=start+1;j<=index;j++)result.Add(split[j]);continue;}
                if(cosine>.999f){result.Add(b);continue;}
                float handle=Mathf.Min(10f,FlatDistance(a,b)*.55f);
                Vector3 c=a+incoming.normalized*handle,d=b-outgoing.normalized*handle;
                int count=Mathf.Max(16,Mathf.CeilToInt(FlatDistance(a,b)*3f));
                for(int j=1;j<=count;j++)
                {
                    float t=j/(float)count,u=1-t;
                    result.Add(u*u*u*a+3*u*u*t*c+3*u*t*t*d+t*t*t*b);
                }
            }
            return result;
        }
        private static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}

        // The bridge now retains the boulevard's four lane centres. A painted
        // central buffer replaces the raised median: no sideways lane jump.
        public static List<Vector3> FitBridgeRoute(List<Vector3> source)
        {
            if(source==null || source.Count<2)return source;
            var result=new List<Vector3>{BridgePoint(source[0])};
            for(int i=1;i<source.Count;i++)
            {
                Vector3 a=source[i-1],b=source[i];
                if(Mathf.Abs(a.z-b.z)<.05f && OnRoad(a.z) && Mathf.Min(a.x,b.x)<-80f && Mathf.Max(a.x,b.x)>-110f)
                {
                    float sign=Mathf.Sign(b.x-a.x);
                    if(sign!=0)
                    {
                        float begin=sign>0?Mathf.Max(a.x,-110f):Mathf.Min(a.x,-80f);
                        float end=sign>0?Mathf.Min(b.x,-80f):Mathf.Max(b.x,-110f);
                        for(float x=begin;sign*(end-x)>.01f;x+=sign*.5f)
                        {
                            Vector3 p=BridgePoint(new Vector3(x,.05f,a.z));
                            if(FlatDistance(result[result.Count-1],p)>.02f)result.Add(p);
                        }
                    }
                }
                Vector3 last=BridgePoint(b);
                if(FlatDistance(result[result.Count-1],last)>.02f)result.Add(last);
            }
            return result;
        }
        private static Vector3 BridgePoint(Vector3 p)
        {
            p.y=CityTrafficManager.GetBridgeElevation(p.x,p.z,out _);
            return p;
        }

        public static void Build(Transform environment)
        {
            ClearOldCorridor(environment);
            Transform root = new GameObject("Four_Lane_Tree_Lined_Boulevard").transform;
            root.SetParent(environment,false);
            var asphalt = ShaderHelper.CreateLitMaterial(new Color(.105f,.12f,.135f),"Boulevard_Asphalt");
            var white = ShaderHelper.CreateLitMaterial(new Color(.91f,.91f,.85f),"Boulevard_Road_Paint");
            var curb = ShaderHelper.CreateLitMaterial(new Color(.62f,.65f,.64f),"Boulevard_Granite_Kerb");
            var soil = ShaderHelper.CreateLitMaterial(new Color(.15f,.23f,.11f),"Median_Planting_Bed");
            var grass = ShaderHelper.CreateLitMaterial(new Color(.23f,.39f,.16f),"Median_Groundcover");
            var bark = ShaderHelper.CreateLitMaterial(new Color(.27f,.18f,.12f),"Median_Tree_Bark");
            var leaves = ShaderHelper.CreateLitMaterial(new Color(.16f,.34f,.13f),"Median_Tree_Crown");
            foreach (var m in new[]{asphalt,white,curb,soil,grass,bark,leaves}) m.enableInstancing=true;
            Box(root,"West_Carriageways",new Vector3(-234.5f,-.025f,Center),new Vector3(251f,.09f,15f),asphalt);
            Box(root,"East_Carriageways",new Vector3((EastRoadEnd-81f)*.5f,-.025f,Center),new Vector3(EastRoadEnd+81f,.09f,15f),asphalt);

            // Independent raised islands leave all street junctions and tracks open.
            float islandStart = -360f;
            for (float x=-360f;x<=EastRoadEnd;x+=1f)
            {
                bool gap = Opening(x,1f) || Mathf.Abs(x)<2f || OnBridge(x) || x>=EastRoadEnd;
                if (!gap) continue;
                if (x-islandStart>=3f)
                {
                    float length=x-islandStart;
                    Box(root,"Median_Granite_Base",new Vector3(islandStart+length*.5f,.13f,Center),new Vector3(length,.26f,2.7f),curb);
                    Box(root,"Median_Soil",new Vector3(islandStart+length*.5f,.265f,Center),new Vector3(length-.4f,.03f,2.3f),soil);
                    Box(root,"Median_Groundcover",new Vector3(islandStart+length*.5f,.29f,Center),new Vector3(length-.6f,.035f,1.9f),grass);
                    for(float treeX=islandStart+4f;treeX<x-3f;treeX+=9f) Tree(root,treeX,bark,leaves);
                    // Reflectors at island noses help read the opening at night.
                    foreach(float nose in new[]{islandStart+.35f,x-.35f})
                    {
                        Box(root,"Island_Bollard",new Vector3(nose,.65f,Center),new Vector3(.18f,.75f,.18f),curb);
                        Box(root,"Island_Reflector",new Vector3(nose,.86f,Center),new Vector3(.21f,.18f,.21f),white);
                    }
                }
                islandStart=x+1f;
            }
            for(float x=-359f;x<EastRoadEnd-1f;x+=4f)
            {
                if(OnBridge(x) || Opening(x,1f)) continue;
                foreach(float z in new[]{-9f,-18f})
                    Box(root,"Lane_Dash",new Vector3(x,.035f,z),new Vector3(2f,.012f,.12f),white);
                foreach(float z in new[]{NorthEdge-.12f,SouthEdge+.12f,-12.05f,-14.95f})
                    Box(root,"Edge_Line",new Vector3(x,.034f,z),new Vector3(4.05f,.012f,.10f),white);
            }
            // Flared mouths provide room for the full bus body during a right turn.
            // Pavements and crossings use the same 9 m opening, then join the 6 m street.
            foreach(float j in Junctions)
                foreach(float z in new[]{NorthEdge+1.5f,SouthEdge-1.5f})
                    Box(root,"Junction_Asphalt_Apron",new Vector3(j,-.025f,z),new Vector3(9f,.09f,3f),asphalt);
            foreach(bool north in new[]{false,true})
            {
                var gaps=new List<Vector2>{new Vector2(-109f,-81f),new Vector2(167f,194f)};
                foreach(float j in Junctions)gaps.Add(new Vector2(j-4.5f,j+4.5f));
                if(north){foreach(float gate in new[]{CustomerParkingLayout.EntryX,CustomerParkingLayout.ExitX}) gaps.Add(new Vector2(gate-2.25f,gate+2.25f));gaps.Add(new Vector2(11f,16f));}
                gaps.Sort((a,b)=>a.x.CompareTo(b.x));
                float cursor=-360f,z=north?NorthEdge+1.5f:SouthEdge-1.5f;
                foreach(Vector2 gap in gaps)
                {
                    if(gap.x>cursor)Box(root,"Boulevard_Sidewalk",new Vector3((cursor+gap.x)*.5f,.08f,z),new Vector3(gap.x-cursor,.16f,3f),curb);
                    cursor=Mathf.Max(cursor,gap.y);
                }
                if(cursor<EastRoadEnd)Box(root,"Boulevard_Sidewalk",new Vector3((cursor+EastRoadEnd)*.5f,.08f,z),new Vector3(EastRoadEnd-cursor,.16f,3f),curb);
            }
            foreach(float gate in new[]{CustomerParkingLayout.EntryX,CustomerParkingLayout.ExitX})
                CrosswalkVisualBuilder.Create(root,"Parking_Gate_Crossing",new Vector3(gate,0,NorthEdge+1.5f),true,4.5f);
            for(float x=-346f;x<EastRoadEnd-10f;x+=24f)
            {
                if(OnBridge(x) || (x>164f && x<197f))continue;
                bool nearJunction=false;
                foreach(float j in Junctions)if(Mathf.Abs(x-j)<13f)nearJunction=true;
                if(nearJunction)continue;
                foreach(bool north in new[]{false,true})
                {
                    if(north && (Mathf.Abs(x-CustomerParkingLayout.EntryX)<4f || Mathf.Abs(x-CustomerParkingLayout.ExitX)<4f || (x>8f && x<19f)))continue;
                    CustomerParkingLayout.CreateLamp(root,new Vector3(x,0,north?NorthEdge+2.3f:SouthEdge-2.3f),curb,white);
                }
            }
            CrosswalkVisualBuilder.Create(root,"Townhall_Pedestrian_Crossing",new Vector3(0,0,Center),false,15f);
            foreach(float junction in Junctions)
            {
                CrosswalkVisualBuilder.Create(root,"Boulevard_North_Arm_Crossing",new Vector3(junction,0,NorthEdge+1.5f),true,9f);
                CrosswalkVisualBuilder.Create(root,"Boulevard_South_Arm_Crossing",new Vector3(junction,0,SouthEdge-1.5f),true,9f);
                foreach(float x in new[]{junction-6f,junction+6f})
                    if(x < -110f || x > -80f) CrosswalkVisualBuilder.Create(root,"Boulevard_Zebra_Crossing",new Vector3(x,0,Center),false,15f);
                // Stop lines are before the shared intersection reservation boundary.
                Box(root,"Eastbound_Stop_Line",new Vector3(junction-10.3f,.037f,-18f),new Vector3(.3f,.015f,6f),white);
                Box(root,"Westbound_Stop_Line",new Vector3(junction+10.3f,.037f,-9f),new Vector3(.3f,.015f,6f),white);
                foreach(float lane in new[]{WestOuter,WestInner,EastInner,EastOuter})
                    Arrow(root,new Vector3(junction+(lane>Center?17f:-17f),.04f,lane),lane>Center?-1:1,white);
            }
            // Painted U-turn guide stays inside the median break, without blocking cross traffic.
            var turn = ShopUTurnRoute(new Vector3(TaxiTurnX-18f,.05f,EastInner));
            for(int i=3;i<turn.Count-2;i+=2)
            {
                Vector3 a=turn[i], b=turn[i+1]; a.y=b.y=.045f;
                Segment(root,"U_Turn_Dashed_Guide",a,b,.11f,white);
            }
            var sign=Box(root,"U_Turn_Sign",new Vector3(TaxiTurnX-13f,1.7f,-14.9f),new Vector3(.08f,1f,.85f),curb);
            var text=new GameObject("U_Turn_Symbol"); text.transform.SetParent(sign,false);
            text.transform.localPosition=new Vector3(-.55f,0,0); text.transform.localRotation=Quaternion.Euler(0,-90,0);
            text.transform.SetParent(root,true);
            text.transform.localScale=Vector3.one;
            var label=text.AddComponent<TextMesh>();label.text="U";label.fontSize=48;label.characterSize=.08f;label.anchor=TextAnchor.MiddleCenter;label.color=Color.white;
            var resources=root.gameObject.AddComponent<BoulevardResources>();
            resources.materials=new[]{asphalt,white,curb,soil,grass,bark,leaves};
            resources.paintMesh=CombinePaint(root,white);
        }

        private static void ClearOldCorridor(Transform root)
        {
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if(!renderer.gameObject.activeInHierarchy)continue;
                string n=renderer.name.ToLowerInvariant();Bounds b=renderer.bounds;
                if(n=="parking_entry_driveway" || n=="parking_exit_driveway")continue;
                bool bridgePart=false;
                for(Transform t=renderer.transform;t!=null;t=t.parent)
                    if(t.name=="Curved_Arch_Highway_Bridge"){bridgePart=true;break;}
                if(bridgePart)continue;
                bool sidewalk=n.Contains("sidewalk") || n.Contains("avewalk") || n.Contains("curb") || n.Contains("walkx");
                bool path=n.Contains("walkway") || n.Contains("driveway") || n.Contains("entrance_path");
                var filter=renderer.GetComponent<MeshFilter>();
                bool crossing=filter!=null && filter.sharedMesh!=null && filter.sharedMesh.name.StartsWith("Crosswalk_Shared_");
                // Delete old crossing arms around the expanded boulevard; one owner rebuilds all four.
                if(crossing && b.center.z>SouthEdge-4f && b.center.z<NorthEdge+4f)
                {renderer.gameObject.SetActive(false);continue;}
                // Replace the entire south pavement band, including the pieces that
                // did not overlap the road and therefore escaped the former cleanup.
                if(sidewalk && b.max.y<.5f && b.size.x>b.size.z &&
                    ((b.max.z>SouthEdge-3.01f && b.min.z< SouthEdge+.05f) ||
                     (b.max.z>NorthEdge-.05f && b.min.z<NorthEdge+3.01f)))
                {renderer.gameObject.SetActive(false);continue;}
                bool besideJunction=false;
                foreach(float j in Junctions)if(Mathf.Abs(b.center.x-j)<5.1f)besideJunction=true;
                if(sidewalk && besideJunction && b.max.y<.5f && b.size.z>b.size.x)
                {
                    if(b.min.z<NorthEdge+3f && b.max.z>NorthEdge+3.1f && b.min.z>=NorthEdge-.1f)
                    {
                        float cut=NorthEdge+3f-b.min.z;
                        renderer.transform.position+=Vector3.forward*(cut*.5f);
                        Vector3 scale=renderer.transform.localScale;scale.z*=(b.size.z-cut)/b.size.z;renderer.transform.localScale=scale;
                        continue;
                    }
                    if(b.min.z<SouthEdge-3.1f && b.max.z>SouthEdge-3f && b.max.z<NorthEdge)
                    {
                        float cut=b.max.z-(SouthEdge-3f);
                        renderer.transform.position-=Vector3.forward*(cut*.5f);
                        Vector3 scale=renderer.transform.localScale;scale.z*=(b.size.z-cut)/b.size.z;renderer.transform.localScale=scale;
                        continue;
                    }
                }
                if(b.max.z<=SouthEdge || b.min.z>=NorthEdge)continue;
                if(Mathf.Abs(b.center.z+9f)<.1f && b.size.x>30 && b.size.z<7 &&
                    (n.Contains("asphalt")||n.Contains("road_horizontal")))
                {renderer.gameObject.SetActive(false);continue;}
                bool paint=n.Contains("line")||n.Contains("stripe")||n.Contains("crosswalk")||n.Contains("zebra");
                if((sidewalk||path||paint) && b.max.y<.5f)
                {
                    if((sidewalk||path) && b.min.z<SouthEdge-.1f && b.max.z<NorthEdge && b.size.z>b.size.x)
                    {
                        float cut=b.max.z-SouthEdge;
                        Vector3 p=renderer.transform.position;p.z-=cut*.5f;renderer.transform.position=p;
                        Vector3 scale=renderer.transform.localScale;scale.z*=Mathf.Max(.01f,(b.size.z-cut)/b.size.z);renderer.transform.localScale=scale;
                    }
                    else renderer.gameObject.SetActive(false);
                }
            }
            // Retire legacy corridor lamps; the boulevard owns regularly spaced replacements.
            foreach(var lamp in root.GetComponentsInChildren<Transform>())
            {
                if(lamp.name!="StreetLamp_Post" && lamp.name!="Clean_Street_Lamp" && lamp.name!="NE_StreetLamp")continue;
                Vector3 p=lamp.position;
                if(p.z< SouthEdge-3.5f || p.z>NorthEdge+3.5f || OnBridge(p.x))continue;
                lamp.gameObject.SetActive(false);
            }
        }

        private static void Tree(Transform root,float x,Material bark,Material leaves)
        {
            Transform tree=new GameObject("Median_Tree").transform;tree.SetParent(root,false);tree.localPosition=new Vector3(x,.28f,Center);
            Part(tree,PrimitiveType.Cylinder,"Trunk",new Vector3(0,1.35f,0),new Vector3(.23f,1.35f,.23f),bark);
            for(int i=0;i<3;i++)
            {
                float angle=i*2.0944f;
                Vector3 p=new Vector3(Mathf.Cos(angle)*.45f,3.15f+(i%2)*.35f,Mathf.Sin(angle)*.35f);
                Part(tree,PrimitiveType.Sphere,"Leaf_Cluster",p,new Vector3(1.8f,2.1f,1.7f),leaves);
            }
            Part(tree,PrimitiveType.Sphere,"Crown",new Vector3(0,4f,0),new Vector3(1.5f,1.8f,1.45f),leaves);
        }
        private static void Arrow(Transform root,Vector3 p,int direction,Material m)
        {
            Box(root,"Direction_Arrow",p,new Vector3(1.5f,.012f,.12f),m);
            Vector3 tip=p+Vector3.right*direction*.8f;
            Segment(root,"Direction_Arrow",tip,tip+new Vector3(-direction*.6f,0,.5f),.12f,m);
            Segment(root,"Direction_Arrow",tip,tip+new Vector3(-direction*.6f,0,-.5f),.12f,m);
        }
        private static void Segment(Transform root,string name,Vector3 a,Vector3 b,float width,Material m)
        {
            var t=Box(root,name,(a+b)*.5f,new Vector3(width,.012f,Vector3.Distance(a,b)),m);
            t.rotation=Quaternion.LookRotation(b-a);
        }
        private static Transform Box(Transform p,string n,Vector3 v,Vector3 s,Material m) => Part(p,PrimitiveType.Cube,n,v,s,m);
        private static Transform Part(Transform p,PrimitiveType type,string n,Vector3 v,Vector3 s,Material m)
        {
            // Decorative road parts never need physics. Player builds may omit a
            // primitive's optional collider; do not create or dereference one.
            var g=PrimitiveFactory.CreateVisual(type,n,p);
            if(type==PrimitiveType.Cube)g.GetComponent<MeshFilter>().sharedMesh=PrimitiveFactory.GetReadableCubeMesh();
            g.transform.localPosition=v;g.transform.localScale=s;
            g.GetComponent<MeshRenderer>().sharedMaterial=m;
            return g.transform;
        }
        private static Mesh CombinePaint(Transform root,Material paint)
        {
            var parts=new List<CombineInstance>();var remove=new List<GameObject>();
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
                if(mf.GetComponent<MeshRenderer>().sharedMaterial==paint && mf.name!="Island_Reflector" && mf.name!="LED")
                {parts.Add(new CombineInstance{mesh=mf.sharedMesh,transform=root.worldToLocalMatrix*mf.transform.localToWorldMatrix});remove.Add(mf.gameObject);}
            var mesh=new Mesh{name="Boulevard_Combined_Paint",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(parts.ToArray());
            var go=new GameObject("Combined_Lane_Paint");go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=paint;r.shadowCastingMode=ShadowCastingMode.Off;
            foreach(var g in remove){g.SetActive(false);Object.Destroy(g);}
            return mesh;
        }
    }

    public sealed class BoulevardResources : MonoBehaviour
    {
        public Material[] materials;
        public Mesh paintMesh;
        private void OnDestroy()
        {
            if(paintMesh!=null) Destroy(paintMesh);
            if(materials!=null) foreach(var m in materials) if(m!=null) Destroy(m);
        }
    }
}
