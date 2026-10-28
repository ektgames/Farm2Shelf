using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    // Directed road lanes shared by delivery movement and price quotations.
    // Address projections split a road; they never create a cross-country edge.
    public static class CourierRoadNetwork
    {
        public static readonly Vector3 QuoteOrigin = new Vector3(14.7f, .05f, 6f);
        private struct Road { public Vector2 a, b; public float offset; }
        private sealed class Lane
        {
            public int from, to;
            public Vector2 direction;
            public Vector3 entry, exit;
        }
        private static readonly List<Road> roads = CreateRoads();
        private static readonly List<Road> publicRoads=CreatePublicRoads();
        private static List<Road> CreatePublicRoads()
        {
            var result=CreateRoads();result.RemoveAll(r=>r.offset==0);
            result.Add(new Road{a=new Vector2(93.5f,38f),b=new Vector2(93.5f,50f),offset=1.5f});
            return result;
        }
        public static Vector3 PublicLaneStop(Vector3 address)
        {
            Road road=publicRoads[NearestRoad(Flat(address),publicRoads)];
            var center=Project(Flat(address),road);var d=(road.b-road.a).normalized;
            var right=new Vector2(d.y,-d.x);
            float side=Vector2.Dot(Flat(address)-center,right)>=0?1f:-1f;
            return World(center+right*(road.offset*side));
        }
        public static bool OnPublicPavement(Vector3 position)
        {
            foreach(var road in publicRoads)
            {
                float halfWidth=road.offset>3f?7.5f:3f;
                if((Flat(position)-Project(Flat(position),road)).sqrMagnitude<halfWidth*halfWidth+.01f)return true;
            }
            return false;
        }
        private static readonly Dictionary<Vector3, int> distances = new Dictionary<Vector3, int>();
        private static Vector3 World(Vector2 p) => new Vector3(p.x, .05f, p.y);
        private static Vector2 Flat(Vector3 p) => new Vector2(p.x, p.z);
        private static void Add(List<Vector3> points, Vector3 p)
        {
            if (points.Count == 0 || (points[points.Count - 1] - p).sqrMagnitude > .0001f) points.Add(p);
        }
        private static List<Road> CreateRoads()
        {
            var r = new List<Road>();
            void EW(float a, float b, float z, float offset = 1.5f) =>
                r.Add(new Road { a = new Vector2(a,z), b = new Vector2(b,z), offset = offset });
            void NS(float x, float a, float b, float offset = 1.5f) =>
                r.Add(new Road { a = new Vector2(x,a), b = new Vector2(x,b), offset = offset });
            EW(-340, 180, MainBoulevard.Center, 6f);
            foreach (float z in new[] {50f, 175f, -55f, -128f})
            {
                EW(-230, -112, z);
                EW(z == 175f ? -37.5f : -75f, 160, z);
            }
            foreach (float x in new[] {-226f,-188f,-150f,-112f}) NS(x,-128,175);
            NS(-75,-128,50); NS(75,-128,50);
            foreach (float x in new[] {-37.5f,0f,37.5f}) NS(x,50,175);
            NS(0,-128,-55); NS(112.5f,-128,175); NS(150,-128,175);
            // Private motorcycle aisle has no southern road connection.
            NS(14.7f,-2,50,0);
            return r;
        }
        private static Vector2 Project(Vector2 p, Road r)
        {
            Vector2 d = r.b-r.a;
            return r.a + d * Mathf.Clamp01(Vector2.Dot(p-r.a,d)/d.sqrMagnitude);
        }
        private static int NearestRoad(Vector2 p,List<Road> network=null)
        {
            var roads=network??CourierRoadNetwork.roads;
            int best=0; float distance=float.PositiveInfinity;
            for(int i=0;i<roads.Count;i++)
            {
                float d=(p-Project(p,roads[i])).sqrMagnitude;
                if(d<distance) { distance=d; best=i; }
            }
            return best;
        }
        public static Vector3 RoadsideStop(Vector3 address)
        {
            Vector2 p=Flat(address);int nearest=-1;float best=float.PositiveInfinity;
            for(int i=0;i<roads.Count;i++)
            {
                if(roads[i].offset==0)continue; // Private loading aisle is not a delivery address.
                float d=(p-Project(p,roads[i])).sqrMagnitude;
                if(d<best){best=d;nearest=i;}
            }
            Road road=roads[nearest];Vector2 direction=(road.b-road.a).normalized;
            Vector2 center=Project(p,road),right=new Vector2(direction.y,-direction.x);
            // Keep the stop clear of crossing roads and turning envelopes.
            foreach(var crossing in roads)
            {
                Vector2 crossDirection=(crossing.b-crossing.a).normalized;
                if(Mathf.Abs(Vector2.Dot(direction,crossDirection))>.5f)continue;
                Vector2 junction=Mathf.Abs(direction.x)>.5f?new Vector2(crossing.a.x,road.a.y):new Vector2(road.a.x,crossing.a.y);
                if((Project(junction,road)-junction).sqrMagnitude>.001f || (Project(junction,crossing)-junction).sqrMagnitude>.001f)continue;
                float gap=Mathf.Abs(junction.y-MainBoulevard.Center)<.1f?16f:9f;
                float offset=Vector2.Dot(center-junction,direction);
                if(Mathf.Abs(offset)<gap)
                {
                    float preferred=Vector2.Dot(p-junction,direction)>=0?1f:-1f;
                    Vector2 candidate=junction+direction*(gap*preferred);
                    if((Project(candidate,road)-candidate).sqrMagnitude>.01f)candidate=junction-direction*(gap*preferred);
                    center=Project(candidate,road);
                }
            }
            float side=Vector2.Dot(p-center,right)>=0?1f:-1f;
            float curb=road.offset>3f?6.35f:2.05f;
            return World(center+right*(side*curb));
        }
        public static List<Vector3> BuildDeliveryRoute(Vector3 from,Vector3 address)
        {
            Vector3 stop=RoadsideStop(address);
            var result=BuildRoute(from,stop);
            if(result.Count<2)return result;
            int last=result.Count-1;Vector3 end=result[last],leg=end-result[last-1];leg.y=0;
            if(leg.magnitude>4.5f && (stop-end).sqrMagnitude<1f)
            {
                var direction=leg.normalized;var start=end-direction*4f;var shift=stop-end;
                result[last]=start;
                for(int i=1;i<=8;i++)
                {
                    float t=i/8f;
                    Add(result,start+direction*(4f*t)+shift*(t*t*(3f-2f*t)));
                }
            }
            return result;
        }

        public static int DistanceMeters(Vector3 destination)
        {
            if(distances.TryGetValue(destination,out int distance)) return distance;
            List<Vector3> route=BuildRoute(QuoteOrigin,destination);
            float length=0;
            for(int i=1;i<route.Count;i++) length+=Vector3.Distance(route[i-1],route[i]);
            distance=Mathf.Max(1,Mathf.CeilToInt(length));
            distances[destination]=distance;
            return distance;
        }
        public static int DeliveryFee(Vector3 destination) => 40 + Mathf.CeilToInt(DistanceMeters(destination)*.35f);

        public static List<Vector3> BuildRoute(Vector3 from, Vector3 to,bool publicOnly=false)
        {
            var roads=publicOnly?publicRoads:CourierRoadNetwork.roads;
            var nodes=new List<Vector2>();
            int Node(Vector2 p)
            {
                for(int i=0;i<nodes.Count;i++) if((nodes[i]-p).sqrMagnitude<.0001f) return i;
                nodes.Add(p); return nodes.Count-1;
            }
            foreach(Road r in roads) { Node(r.a); Node(r.b); }
            for(int i=0;i<roads.Count;i++) for(int j=i+1;j<roads.Count;j++)
            {
                Road a=roads[i], b=roads[j];
                bool horizontal=Mathf.Abs(a.a.y-a.b.y)<.01f;
                if(horizontal==(Mathf.Abs(b.a.y-b.b.y)<.01f)) continue;
                Vector2 p=horizontal?new Vector2(b.a.x,a.a.y):new Vector2(a.a.x,b.a.y);
                if((Project(p,a)-p).sqrMagnitude<.001f && (Project(p,b)-p).sqrMagnitude<.001f) Node(p);
            }
            int sourceRoad=NearestRoad(Flat(from),roads), targetRoad=NearestRoad(Flat(to),roads);
            int source=Node(Project(Flat(from),roads[sourceRoad])), target=Node(Project(Flat(to),roads[targetRoad]));
            if(source==target) return new List<Vector3>{from};
            var lanes=new List<Lane>();
            var neighbours=new List<int>[nodes.Count];
            for(int i=0;i<nodes.Count;i++) neighbours[i]=new List<int>();
            for(int ri=0;ri<roads.Count;ri++)
            {
                Road road=roads[ri]; var onRoad=new List<int>();
                for(int n=0;n<nodes.Count;n++) if((Project(nodes[n],road)-nodes[n]).sqrMagnitude<.001f) onRoad.Add(n);
                onRoad.Sort((a,b)=>(nodes[a]-road.a).sqrMagnitude.CompareTo((nodes[b]-road.a).sqrMagnitude));
                for(int k=1;k<onRoad.Count;k++)
                {
                    int a=onRoad[k-1],b=onRoad[k];
                    neighbours[a].Add(b); neighbours[b].Add(a);
                    void Lane(int u,int v)
                    {
                        Vector2 dir=(nodes[v]-nodes[u]).normalized;
                        Vector2 right=new Vector2(dir.y,-dir.x)*road.offset;
                        lanes.Add(new Lane {from=u,to=v,direction=dir,entry=World(nodes[u]+right),exit=World(nodes[v]+right)});
                    }
                    Lane(a,b); Lane(b,a);
                }
            }
            bool Junction(int n)
            {
                if(neighbours[n].Count>2) return true;
                return neighbours[n].Count==2 && Mathf.Abs(Vector2.Dot(
                    (nodes[neighbours[n][0]]-nodes[n]).normalized,
                    (nodes[neighbours[n][1]]-nodes[n]).normalized))<.99f;
            }
            foreach(Lane lane in lanes)
            {
                float limit=Vector2.Distance(nodes[lane.from],nodes[lane.to])*.4f;
                float Radius(int n) => Junction(n)?Mathf.Min(limit,Mathf.Abs(nodes[n].y-MainBoulevard.Center)<.01f?10f:4f):0;
                lane.entry+=World(lane.direction*Radius(lane.from))-World(Vector2.zero);
                lane.exit-=World(lane.direction*Radius(lane.to))-World(Vector2.zero);
            }
            // A curb is served on its own side of the street, never across a median.
            bool SameSide(Lane lane, Vector3 p, int n)
            {
                Vector2 right=new Vector2(lane.direction.y,-lane.direction.x);
                return Vector2.Dot(Flat(p)-nodes[n],right)>=-.2f;
            }
            var costs=new float[lanes.Count]; var previous=new int[lanes.Count]; var visited=new bool[lanes.Count];
            for(int i=0;i<lanes.Count;i++)
            {
                previous[i]=-1;
                costs[i]=lanes[i].from==source && SameSide(lanes[i],from,source)?
                    Vector3.Distance(from,lanes[i].entry)+Vector3.Distance(lanes[i].entry,lanes[i].exit):float.PositiveInfinity;
            }
            int finish=-1;
            for(int step=0;step<lanes.Count;step++)
            {
                int best=-1;
                for(int i=0;i<lanes.Count;i++) if(!visited[i] && !float.IsPositiveInfinity(costs[i]) && (best<0 || costs[i]<costs[best])) best=i;
                if(best<0) break;
                visited[best]=true; Lane current=lanes[best];
                if(current.to==target && SameSide(current,to,target)) { finish=best; break; }
                for(int i=0;i<lanes.Count;i++)
                {
                    Lane next=lanes[i];
                    if(next.from!=current.to || next.to==current.from) continue;
                    float cost=costs[best]+Vector3.Distance(current.exit,next.entry)+Vector3.Distance(next.entry,next.exit);
                    if(cost<costs[i]) { costs[i]=cost; previous[i]=best; }
                }
            }
            if(finish<0) throw new InvalidOperationException("No connected courier road route: "+from+" -> "+to);
            var path=new List<int>();
            for(int i=finish;i>=0;i=previous[i]) path.Add(i);
            path.Reverse(); var result=new List<Vector3>(); Add(result,from);
            Lane last=null;
            foreach(int index in path)
            {
                Lane lane=lanes[index];
                if(last!=null && Vector2.Dot(last.direction,lane.direction)<.99f)
                {
                    Vector3 a=last.exit,b=lane.entry;
                    float bend=Vector3.Distance(a,b)*.55f;
                    Vector3 c=a+new Vector3(last.direction.x,0,last.direction.y)*bend;
                    Vector3 d=b-new Vector3(lane.direction.x,0,lane.direction.y)*bend;
                    for(int j=1;j<13;j++)
                    {
                        float t=j/12f,u=1-t;
                        Add(result,u*u*u*a+3*u*u*t*c+3*u*t*t*d+t*t*t*b);
                    }
                }
                Add(result,lane.entry); Add(result,lane.exit); last=lane;
            }
            // Merge forward from a curb; never begin with a 90-degree sideways hop.
            if(result.Count>2)
            {
                Vector3 first=result[1]-result[0],next=result[2]-result[1];
                if(first.sqrMagnitude<1f && next.sqrMagnitude>16f && Mathf.Abs(Vector3.Dot(first.normalized,next.normalized))<.2f)
                    result[1]+=next.normalized*3f;
            }
            return MainBoulevard.FitBridgeRoute(result);
        }
    }
}
