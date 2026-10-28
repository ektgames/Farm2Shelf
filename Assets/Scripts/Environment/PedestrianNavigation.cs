using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    // Kinematic navigation: no rigidbody pushing, no fallback that walks through an obstacle.
    public static class PedestrianNavigation
    {
        public const float Radius=.24f;
        const float Cell=.35f;
        static readonly List<Bounds> obstacles=new List<Bounds>();
        static readonly List<Bounds> fixedObstacles=new List<Bounds>();
        static readonly Dictionary<Vector2Int,bool> occupancy=new Dictionary<Vector2Int,bool>();
        const float BucketSize = 4f;
        static readonly Dictionary<Vector2Int,List<Bounds>> buckets = new Dictionary<Vector2Int,List<Bounds>>();
        static Vector2Int Bucket(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / BucketSize), Mathf.FloorToInt(p.z / BucketSize));
        static int signature,scanFrame=-1;
        static Transform map;
        static int version;
        static bool rescanWalls = true;
        public static int Version {get{Refresh();return version;}}
        public static void Invalidate(){scanFrame=-1;signature=int.MinValue;rescanWalls=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){obstacles.Clear();fixedObstacles.Clear();occupancy.Clear();buckets.Clear();map=null;signature=0;scanFrame=-1;version=0;rescanWalls=true;}
        static bool PassThrough(FurnitureType type)=>PlacedFurnitureController.IsWalkableFloorDecoration(type)||DetailedDecorationBuilder.IsCeiling(type)||FurnitureDatabase.IsWallMountedDecoration(type);
        public static Vector2 Footprint(FurnitureType type)
        {
            var manager=FurniturePlacementManager.Instance;
            Vector2 size=manager!=null?manager.GetFurnitureFootprintSize(type):new Vector2(1.2f,1f);
            // Checkout staff stand is behind the counter, not inside a full-height interaction box.
            if(type==FurnitureType.Cashier){size.x=Mathf.Max(size.x,2.45f);size.y=Mathf.Min(size.y,1.1f);}
            return size;
        }
        static Bounds Rect(Vector3 pos,Vector2 size,float yaw,float padding=Radius)
        {
            float a=yaw*Mathf.Deg2Rad;
            float x=Mathf.Abs(Mathf.Cos(a))*size.x+Mathf.Abs(Mathf.Sin(a))*size.y;
            float z=Mathf.Abs(Mathf.Sin(a))*size.x+Mathf.Abs(Mathf.Cos(a))*size.y;
            return new Bounds(new Vector3(pos.x,0,pos.z),new Vector3(x+2*padding,2,z+2*padding));
        }
        static bool Contains(Bounds b,Vector3 p)=>p.x>b.min.x&&p.x<b.max.x&&p.z>b.min.z&&p.z<b.max.z;
        static void Refresh()
        {
            if(scanFrame==Time.frameCount)return;scanFrame=Time.frameCount;
            // Keep the environment reference; scene-wide name searches are unnecessary per frame.
            Transform current=map;
            if(current==null || rescanWalls)
            {
                var root=GameObject.Find("Farm2Shelf_Environment");
                current=root!=null?root.transform:null;
            }
            bool changed=map!=current || rescanWalls;
            if(changed)
            {
                map=current;rescanWalls=false;fixedObstacles.Clear();
                if(map!=null)foreach(var c in map.GetComponentsInChildren<Collider>())
                {
                    if(!c.enabled || c.GetComponentInParent<PlacedFurnitureController>()!=null)continue;
                    bool furnitureObstacle = c.GetComponent<UnityEngine.AI.NavMeshObstacle>() != null;
                    if(c.isTrigger && !furnitureObstacle)continue;
                    string n=c.name.ToLowerInvariant();
                    bool solid=furnitureObstacle||n.Contains("partition")||n.Contains("wall")||n.Contains("duvar")||n.Contains("fence")||n.Contains("locker")||n.Contains("cabinet");
                    if(!solid || n.Contains("over_") || n.Contains("ceiling") || n.Contains("roof") || n.Contains("doubledoor") || n.Contains("slidingdoor") || n.Contains("boundary"))continue;
                    Bounds b=c.bounds;if(b.min.y>1.7f || b.max.y<.3f)continue;
                    b.Expand(new Vector3(Radius*2,0,Radius*2));b=new Bounds(new Vector3(b.center.x,0,b.center.z),new Vector3(b.size.x,2,b.size.z));fixedObstacles.Add(b);
                }
            }
            int hash=17;
            foreach(var f in PlacedFurnitureController.AllPlacedFurniture)
                if(f!=null&&f.isActiveAndEnabled)unchecked{hash=hash*31+f.GetInstanceID();hash=hash*31+f.transform.position.GetHashCode();hash=hash*31+f.transform.rotation.GetHashCode();hash=hash*31+(int)f.FurnitureType;}
            if(!changed&&hash==signature)return;signature=hash;version++;occupancy.Clear();
            obstacles.Clear();obstacles.AddRange(fixedObstacles);
            foreach(var f in PlacedFurnitureController.AllPlacedFurniture)
            {
                if(f==null||!f.isActiveAndEnabled||PassThrough(f.FurnitureType))continue;
                Vector2 size=Footprint(f.FurnitureType);
                if(f.FurnitureType==FurnitureType.FlowerArch)
                {
                    foreach(float sign in new[]{-1f,1f})obstacles.Add(Rect(f.transform.TransformPoint(new Vector3(sign*(size.x/2-.18f),0,0)),new Vector2(.36f,size.y),f.transform.eulerAngles.y));
                }
                else obstacles.Add(Rect(f.transform.position,size,f.transform.eulerAngles.y));
            }
            buckets.Clear();
            foreach(var bounds in obstacles)
            {
                var min=Bucket(bounds.min);var max=Bucket(bounds.max);
                for(int x=min.x;x<=max.x;x++)for(int z=min.y;z<=max.y;z++)
                {
                    var key=new Vector2Int(x,z);
                    if(!buckets.TryGetValue(key,out var items)){items=new List<Bounds>();buckets[key]=items;}
                    items.Add(bounds);
                }
            }
        }
        public static bool IsBlocked(Vector3 p)
        {
            Refresh();
            if(buckets.TryGetValue(Bucket(p),out var nearby))
                foreach(var b in nearby)if(Contains(b,p))return true;
            return false;
        }
        public static bool Clear(Vector3 from,Vector3 to,bool escaping=false)
        {
            Refresh();from.y=to.y=0;Vector3 delta=to-from;
            float length=delta.magnitude;if(length<.0001f)return !IsBlocked(to);
            var ray=new Ray(from,delta/length);
            var min=Bucket(Vector3.Min(from,to));var max=Bucket(Vector3.Max(from,to));
            for(int x=min.x;x<=max.x;x++)for(int z=min.y;z<=max.y;z++)
            {
                if(!buckets.TryGetValue(new Vector2Int(x,z),out var nearby))continue;
                foreach(var b in nearby)
                {
                    if(escaping&&Contains(b,from))continue;
                    if(Contains(b,from)||Contains(b,to)||b.IntersectRay(ray,out float distance)&&distance<=length)return false;
                }
            }
            return true;
        }
        static Vector2Int Key(Vector3 p)=>new Vector2Int(Mathf.RoundToInt(p.x/Cell),Mathf.RoundToInt(p.z/Cell));
        static Vector3 Point(Vector2Int p,float y)=>new Vector3(p.x*Cell,y,p.y*Cell);
        static bool Free(Vector2Int p)
        {
            if(!occupancy.TryGetValue(p,out bool blocked)){blocked=IsBlocked(Point(p,0));occupancy[p]=blocked;}
            return !blocked;
        }
        // Search state survives between frames. A crowded / unreachable destination
        // must not monopolize the render thread, especially at accelerated game speed.
        public sealed class RouteRequest
        {
            readonly IEnumerator<int> work;
            public List<Vector3> Result { get; internal set; }
            public bool Complete { get; private set; }
            static int budgetFrame=-1;
            static long spent;
            static readonly long frameBudget=System.Diagnostics.Stopwatch.Frequency/500; // 2 ms
            internal RouteRequest(Vector3 from,Vector3 goal,System.Func<Vector3,Vector3,bool> crowd)
            { work=Search(from,goal,crowd).GetEnumerator(); }
            IEnumerable<int> Search(Vector3 from,Vector3 goal,System.Func<Vector3,Vector3,bool> crowd)
            {
                if(crowd!=null && !crowd(goal,goal))yield break;
                foreach(var tick in RouteGrid(from,goal,Cell,16000,crowd,this))yield return tick;
                if(Result==null)
                    foreach(var tick in RouteGrid(from,goal,Cell/3f,32000,crowd,this))yield return tick;
            }
            public void Advance()
            {
                if(Complete)return;
                if(budgetFrame!=Time.frameCount){budgetFrame=Time.frameCount;spent=0;}
                if(spent>=frameBudget)return;
                long start=System.Diagnostics.Stopwatch.GetTimestamp();
                int chunks=0;
                do
                {
                    if(!work.MoveNext()){Complete=true;work.Dispose();break;}
                }while(++chunks<8 && System.Diagnostics.Stopwatch.GetTimestamp()-start+spent<frameBudget);
                spent+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
            }
            internal void Finish()
            {
                while(!Complete)
                    if(!work.MoveNext()){Complete=true;work.Dispose();}
            }
        }
        public static RouteRequest RequestRoute(Vector3 from,Vector3 goal,System.Func<Vector3,Vector3,bool> crowdClear=null)
            =>new RouteRequest(from,goal,crowdClear);
        public static List<Vector3> Route(Vector3 from,Vector3 goal,System.Func<Vector3,Vector3,bool> crowdClear=null)
        {
            var request=RequestRoute(from,goal,crowdClear);
            request.Finish();
            return request.Result;
        }
        static IEnumerable<int> RouteGrid(Vector3 from, Vector3 goal, float spacing, int limit, System.Func<Vector3,Vector3,bool> crowdClear, RouteRequest request)
        {
            bool SegmentClear(Vector3 a, Vector3 b) => Clear(a,b) && (crowdClear == null || crowdClear(a,b));
            Vector2Int GridKey(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x/spacing), Mathf.RoundToInt(p.z/spacing));
            Vector3 GridPoint(Vector2Int p, float y) => new Vector3(p.x*spacing,y,p.y*spacing);
            var fineOccupancy = new Dictionary<Vector2Int,bool>();
            bool GridFree(Vector2Int p)
            {
                if (spacing == Cell) return Free(p);
                if (!fineOccupancy.TryGetValue(p, out bool blocked))
                { blocked=IsBlocked(GridPoint(p,0)); fineOccupancy[p]=blocked; }
                return !blocked;
            }
            Refresh();goal.y=from.y;
            if(IsBlocked(goal))yield break;
            if(SegmentClear(from,goal)){request.Result=new List<Vector3>{goal};yield break;}
            Vector2Int start=GridKey(from);
            // Choose a nearby grid connector with an unobstructed segment, rather than rounding through a wall.
            bool found=false;float best=float.MaxValue;
            for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++)
            {
                var k=GridKey(from)+new Vector2Int(x,z);Vector3 p=GridPoint(k,from.y);
                float d=(p-from).sqrMagnitude;
                if(d<best&&GridFree(k)&&SegmentClear(from,p)){best=d;start=k;found=true;}
            }
            if(!found)yield break;
            var queue=new MinHeap();var cost=new Dictionary<Vector2Int,float>();var came=new Dictionary<Vector2Int,Vector2Int>();var closed=new HashSet<Vector2Int>();
            cost[start]=0;queue.Add(start,Vector3.Distance(GridPoint(start,0),new Vector3(goal.x,0,goal.z)));
            Vector2Int end=GridKey(goal);int padding=Mathf.CeilToInt(12f/spacing);
            int minX=Mathf.Min(start.x,end.x)-padding,maxX=Mathf.Max(start.x,end.x)+padding,minZ=Mathf.Min(start.y,end.y)-padding,maxZ=Mathf.Max(start.y,end.y)+padding;
            int expanded=0;
            while(queue.Count>0&&expanded++<limit)
            {
                if((expanded & 31)==0)yield return expanded;
                Vector2Int node=queue.Pop();if(!closed.Add(node))continue;
                Vector3 p=GridPoint(node,from.y);
                if((p-goal).sqrMagnitude<1.2f&&SegmentClear(p,goal))
                {
                    var path=new List<Vector3>{goal,p};
                    while(node!=start){node=came[node];path.Add(GridPoint(node,from.y));}
                    path.Reverse();request.Result=path;yield break;
                }
                for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
                {
                    if(dx==0&&dz==0)continue;
                    var next=node+new Vector2Int(dx,dz);
                    if(next.x<minX||next.x>maxX||next.y<minZ||next.y>maxZ||closed.Contains(next)||!GridFree(next))continue;
                    if(dx!=0&&dz!=0&&(!GridFree(node+new Vector2Int(dx,0))||!GridFree(node+new Vector2Int(0,dz))))continue;
                    Vector3 q=GridPoint(next,from.y);if(!SegmentClear(p,q))continue;
                    float g=cost[node]+(dx!=0&&dz!=0?1.414214f:1)*spacing;
                    if(cost.TryGetValue(next,out float old)&&old<=g)continue;
                    cost[next]=g;came[next]=node;queue.Add(next,g+Vector3.Distance(q,goal));
                }
            }
            yield break;
        }
        sealed class MinHeap
        {
            readonly List<KeyValuePair<Vector2Int,float>> values=new List<KeyValuePair<Vector2Int,float>>();
            public int Count=>values.Count;
            public void Add(Vector2Int p,float score){var item=new KeyValuePair<Vector2Int,float>(p,score);values.Add(item);int i=values.Count-1;while(i>0){int parent=(i-1)/2;if(values[parent].Value<=score)break;values[i]=values[parent];i=parent;}values[i]=item;}
            public Vector2Int Pop(){var result=values[0].Key;var last=values[values.Count-1];values.RemoveAt(values.Count-1);if(values.Count==0)return result;int i=0;while(i*2+1<values.Count){int c=i*2+1;if(c+1<values.Count&&values[c+1].Value<values[c].Value)c++;if(values[c].Value>=last.Value)break;values[i]=values[c];i=c;}values[i]=last;return result;}
        }
        public static Vector3 Move(GameObject actor,Vector3 goal,float distance)
        {
            if(actor==null)return goal;
            var agent=actor.GetComponent<PedestrianNavigator>();if(agent==null)agent=actor.AddComponent<PedestrianNavigator>();
            return agent.Step(goal,distance);
        }
    }
}
