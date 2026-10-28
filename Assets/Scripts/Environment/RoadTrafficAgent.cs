using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    // All road vehicle controllers submit motion here, including parked buses and delivery vehicles.
    [DefaultExecutionOrder(500)]
    public sealed class RoadTrafficAgent : MonoBehaviour
    {
        static readonly List<RoadTrafficAgent> agents = new List<RoadTrafficAgent>();
        static RoadTrafficAgent parkingExitOwner;

        // Short reservation for the customer parking T-junction. Queued arrivals stop
        // east of this box, leaving an outgoing vehicle room to join the westbound lane.
        static bool InParkingMerge(Vector3 position, Quaternion rotation, RoadTrafficAgent agent)
        {
            Vector3 p = position + rotation * agent.center;
            Vector3 right = rotation * Vector3.right, forward = rotation * Vector3.forward;
            float ex = Mathf.Abs(right.x) * agent.half.x + Mathf.Abs(forward.x) * agent.half.y;
            float ez = Mathf.Abs(right.z) * agent.half.x + Mathf.Abs(forward.z) * agent.half.y;
            return p.x + ex > -44f && p.x - ex < -27.5f && p.z + ez > -8.8f && p.z - ez < -5.5f;
        }

        public static bool TryReserveParkingExit(Transform vehicle)
        {
            var owner = Register(vehicle);
            if (parkingExitOwner == owner) return true;
            if (parkingExitOwner != null && parkingExitOwner.isActiveAndEnabled) return false;
            foreach (var other in agents)
                if (other != null && other != owner && other.isActiveAndEnabled && !other.pending &&
                    InParkingMerge(other.transform.position, other.transform.rotation, other)) return false;
            parkingExitOwner = owner;
            return true;
        }

        public static void ReleaseParkingExit(Transform vehicle)
        {
            if (parkingExitOwner != null && parkingExitOwner.transform == vehicle) parkingExitOwner = null;
        }

        bool ParkingMergeClear(Vector3 position, Quaternion rotation)
        {
            return parkingExitOwner == null || !parkingExitOwner.isActiveAndEnabled || parkingExitOwner == this ||
                !InParkingMerge(position, rotation, this);
        }
        Vector3 center;
        Vector2 half;
        float broadRadius;
        readonly Dictionary<float,int> blockedJunctionFrames=new Dictionary<float,int>();
        Renderer[] visuals;
        bool[] visible;
        bool pending;
        bool motorcycle;
        public System.Action OnGridlock;
        int demandFrame=-10,signalWaitFrame=-10;
        Vector3 progressPosition;
        float blockedSeconds;
        RoadTrafficAgent blockingVehicle;
        public void NotifyTrafficWait(){demandFrame=Time.frameCount;}
        public void NotifySignalWait(){signalWaitFrame=Time.frameCount;}
        bool SignalQueue()
        {
            var current=this;
            for(int i=0;i<32 && current!=null;i++,current=current.blockingVehicle)
                if(current.signalWaitFrame>=Time.frameCount-1)return true;
            return false;
        }
        void CheckGridlock()
        {
            if(Time.deltaTime<=0f)return;
            Vector3 delta=transform.position-progressPosition;delta.y=0;
            if(delta.sqrMagnitude>.04f || demandFrame<Time.frameCount-1 || SignalQueue())
            {progressPosition=transform.position;blockedSeconds=0;return;}
            if(motorcycle || pending)return;
            blockedSeconds+=Time.unscaledDeltaTime;
            if(blockedSeconds<10f)return;
            blockedSeconds=0;
            // Owners clean reservations and preserve purchases/cargo before removing the visual.
            if(OnGridlock!=null)OnGridlock();else Destroy(gameObject);
        }
        Quaternion safeRotation;
        IList<Vector3> route;
        int routeIndex;
        public static void SetRoute(Transform vehicle, IList<Vector3> points, int index)
        {
            var agent = Register(vehicle);
            agent.route = points;
            agent.routeIndex = index;
        }

        public bool OccupiesJunction(float x, float radius)
        {
            Vector3 p = transform.position + transform.rotation * center;
            Vector3 right = transform.right, forward = transform.forward;
            float extentX = Mathf.Abs(right.x) * half.x + Mathf.Abs(forward.x) * half.y;
            float extentZ = Mathf.Abs(right.z) * half.x + Mathf.Abs(forward.z) * half.y;
            return Mathf.Abs(p.x - x) < radius + extentX && Mathf.Abs(p.z - MainBoulevard.Center) < radius + extentZ;
        }

        public float FrontExtent(Vector3 direction)
        {
            direction.y = 0f;
            direction.Normalize();
            return Mathf.Max(.35f, Vector3.Dot(transform.rotation * center, direction) +
                Mathf.Abs(Vector3.Dot(transform.right, direction)) * half.x +
                Mathf.Abs(Vector3.Dot(transform.forward, direction)) * half.y);
        }

        // Check the actual turn and enough downstream road for the rear to leave the box.
        public bool RouteHasSpace(float x, Vector3 direction, float radius)
        {
            // Only cache refusals within this render frame. A positive result must
            // always be checked against the latest vehicle positions before entry.
            if(blockedJunctionFrames.TryGetValue(x,out int frame) && frame==Time.frameCount)return false;
            bool clear=CheckRouteSpace(x,direction,radius);
            if(!clear)blockedJunctionFrames[x]=Time.frameCount;
            return clear;
        }
        bool CheckRouteSpace(float x, Vector3 direction, float radius)
        {
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            float travelled = 0f;
            bool entered = false;
            int index = routeIndex;
            float clearance = radius + half.magnitude + center.magnitude + 1f;
            while (travelled < 60f)
            {
                Vector3 target = position + direction * 60f;
                if (route != null && index < route.Count)
                {
                    target = route[index];
                    target.y = position.y;
                    if ((target - position).sqrMagnitude < .01f) { index++; continue; }
                }
                Vector3 delta = target - position;
                delta.y = 0;
                if (delta.sqrMagnitude < .0001f) return false;
                direction = delta.normalized;
                Quaternion desired = Quaternion.LookRotation(direction);
                int turns = Mathf.Max(1, Mathf.CeilToInt(Quaternion.Angle(rotation, desired) / 3f));
                for (int t = 1; t <= turns; t++)
                    if (!Clear(position, Quaternion.Slerp(rotation, desired, (float)t / turns))) return false;
                rotation = desired;
                float step = Mathf.Min(.25f, delta.magnitude);
                position += direction * step;
                travelled += step;
                if (!Clear(position, rotation)) return false;
                bool inside = Mathf.Abs(position.x - x) < clearance && Mathf.Abs(position.z - MainBoulevard.Center) < clearance;
                if (inside) entered = true;
                else if (entered) return true;
            }
            return false;
        }
        public bool Waiting { get; private set; }
        public bool IsStraightAtJunction(float junctionX)
        {
            if(route==null || !MainBoulevard.OnRoad(transform.position.z) || Mathf.Abs(transform.forward.x)<.97f)return false;
            float sign=Mathf.Sign(transform.forward.x),z=transform.position.z;
            Vector3 previous=transform.position;
            for(int i=routeIndex;i<route.Count;i++)
            {
                Vector3 p=route[i];
                if(Mathf.Abs(p.z-z)>.15f || sign*(p.x-previous.x)<-.05f)return false;
                if(sign*(p.x-junctionX)>11f+half.y)return true;
                previous=p;
            }
            return false;
        }
        public bool Pending => pending;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry(){agents.Clear();parkingExitOwner=null;}
        public static RoadTrafficAgent Register(Transform vehicle)
        {
            var agent=vehicle.GetComponent<RoadTrafficAgent>();
            if(agent!=null)return agent;
            agent=vehicle.gameObject.AddComponent<RoadTrafficAgent>();
            agent.Initialize();return agent;
        }
        void Initialize()
        {
            motorcycle = GetComponent<CourierMotorcycleController>() != null;
            // Renderer LOCAL bounds avoid world-axis inflation when a vehicle faces east/west.
            bool any=false;var bounds=new Bounds(Vector3.zero,Vector3.zero);
            foreach(var r in GetComponentsInChildren<MeshRenderer>())
            {
                if(r.GetComponent<TextMesh>()!=null)continue;
                var b=r.localBounds;var matrix=transform.worldToLocalMatrix*r.transform.localToWorldMatrix;
                for(int i=0;i<8;i++)
                {
                    Vector3 p=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(!any){bounds=new Bounds(p,Vector3.zero);any=true;}else bounds.Encapsulate(p);
                }
            }
            Vector3 scale=transform.lossyScale;
            center=Vector3.Scale(bounds.center,scale);center.y=0;
            half=new Vector2(Mathf.Max(.35f,bounds.extents.x*Mathf.Abs(scale.x)),Mathf.Max(.65f,bounds.extents.z*Mathf.Abs(scale.z)));
            broadRadius=half.magnitude+center.magnitude;
            visuals=GetComponentsInChildren<Renderer>();visible=new bool[visuals.Length];
            for(int i=0;i<visuals.Length;i++)visible[i]=visuals[i].enabled;
            safeRotation=transform.rotation;
            pending=!Clear(transform.position,transform.rotation);
            if(pending) SetVisible(false);
            agents.Add(this);
        }
        void SetVisible(bool show){for(int i=0;i<visuals.Length;i++)if(visuals[i]!=null)visuals[i].enabled=show&&visible[i];}
        void Update()
        {
            if(pending && Clear(transform.position,transform.rotation)){pending=false;SetVisible(true);}
        }
        void LateUpdate()
        {
            // Existing controllers also steer while stopped: don't allow that rotation through a neighbour.
            if(pending)return;
            var motionBlocker=blockingVehicle;
            if(Clear(transform.position,transform.rotation))safeRotation=transform.rotation;
            else transform.rotation=safeRotation;
            if(motionBlocker!=null)blockingVehicle=motionBlocker;
            CheckGridlock();
        }
        void OnDestroy(){if(parkingExitOwner==this)parkingExitOwner=null;agents.Remove(this);}
        bool Clear(Vector3 position,Quaternion rotation)
        {
            blockingVehicle=null;
            foreach(var other in agents)
            {
                if(other==null || other==this || other.pending || !other.isActiveAndEnabled)continue;
                if(Overlaps(position,rotation,other)){blockingVehicle=other;return false;}
            }
            return true;
        }
        internal static bool MayOverlapFootprints(Vector3 a,float radiusA,Vector3 b,float radiusB)
        {
            // Circumscribed bodies include pivot offsets and a conservative SAT gap.
            float reach=radiusA+radiusB+.7f,dx=b.x-a.x,dz=b.z-a.z;
            return dx*dx+dz*dz<=reach*reach;
        }
        bool Overlaps(Vector3 p,Quaternion q,RoadTrafficAgent b)
        {
            Vector3 otherPosition=b.transform.position;
            if(!MayOverlapFootprints(p,broadRadius,otherPosition,b.broadRadius))return false;
            Vector3 aCenter=p+q*center,bCenter=otherPosition+b.transform.rotation*b.center;
            if(Mathf.Abs(aCenter.y-bCenter.y)>3f)return false;
            Vector3 ar=q*Vector3.right,af=q*Vector3.forward,br=b.transform.right,bf=b.transform.forward;
            ar.y=af.y=br.y=bf.y=0;ar.Normalize();af.Normalize();br.Normalize();bf.Normalize();
            Vector3 diff=bCenter-aCenter;diff.y=0;
            return Axis(diff,ar,ar,af,br,bf,b) && Axis(diff,af,ar,af,br,bf,b) && Axis(diff,br,ar,af,br,bf,b) && Axis(diff,bf,ar,af,br,bf,b);
        }
        bool Axis(Vector3 diff,Vector3 axis,Vector3 ar,Vector3 af,Vector3 br,Vector3 bf,RoadTrafficAgent b)
        {
            float a=half.x*Mathf.Abs(Vector3.Dot(axis,ar))+half.y*Mathf.Abs(Vector3.Dot(axis,af));
            float other=b.half.x*Mathf.Abs(Vector3.Dot(axis,br))+b.half.y*Mathf.Abs(Vector3.Dot(axis,bf));
            // The 1.15 m motorcycle bays cannot accommodate a car-sized buffer.
            // Keep the full rendered body bounds; only reduce the extra gap for two bikes.
            float gap = motorcycle && b.motorcycle ? .12f : .35f;
            return Mathf.Abs(Vector3.Dot(diff,axis))<a+other+gap;
        }
        public static Vector3 Move(Transform vehicle,Vector3 requested)
        {
            var agent=Register(vehicle);
            Vector3 from=vehicle.position;
            if((requested-from).sqrMagnitude>.00000001f || (agent.route!=null && agent.routeIndex<agent.route.Count))agent.demandFrame=Time.frameCount;
            agent.Waiting=false;
            agent.blockingVehicle=null;
            if(agent.pending){agent.Waiting=true;return from;}
            requested=agent.KeepLaneCenter(from,requested);
            Vector3 beforeCrossing=requested;
            requested=RailwayCrossingController.ClampRoadMove(from,requested);
            if((beforeCrossing-requested).sqrMagnitude>.00000001f)agent.NotifySignalWait();
            Vector3 delta=requested-from;
            float distance=delta.magnitude;
            if(distance<.00001f)return from;
            Vector3 direction=delta/distance;
            float headingError=Vector3.Angle(vehicle.forward,direction);
            if(headingError>12f && headingError<168f)distance=Mathf.Min(distance,3.5f*Time.deltaTime);
            else if(agent.route!=null && agent.routeIndex+1<agent.route.Count)
            {
                Vector3 corner=agent.route[agent.routeIndex]-from;
                Vector3 after=agent.route[agent.routeIndex+1]-agent.route[agent.routeIndex];
                if(corner.sqrMagnitude<36f && Vector3.Angle(direction,after)>12f)distance=Mathf.Min(distance,3.5f*Time.deltaTime);
            }
            requested=from+direction*distance;
            // Brake into a queue or signal instead of driving at full speed to the final safety boundary.
            Vector3 previous=from;
            for(float look=.4f;look<=4f;look+=.4f)
            {
                agent.PredictAlongRoute(from,direction,look,out Vector3 probe,out Quaternion probeRotation);
                if(!agent.ParkingMergeClear(probe,probeRotation) || !agent.Clear(probe,probeRotation) || !MainRoadSignals.CanAdvance(agent,previous,probe,agent.half.y))
                {
                    float permitted=Mathf.Max(.02f,look-.4f)*2f*Time.deltaTime;
                    distance=Mathf.Min(distance,permitted);
                    requested=from+direction*distance;
                    break;
                }
                previous=probe;
            }
            // Swept checks prevent tunnelling at 3x speed or during a long frame.
            int steps=Mathf.Max(1,Mathf.CeilToInt(distance/.15f));
            Vector3 accepted=from;
            for(int i=1;i<=steps;i++)
            {
                Vector3 candidate=Vector3.Lerp(from,requested,(float)i/steps);
                if(!agent.ParkingMergeClear(candidate,vehicle.rotation) || !agent.Clear(candidate,vehicle.rotation) || !MainRoadSignals.CanAdvance(agent,accepted,candidate,agent.half.y,true))
                {agent.Waiting=true;break;}
                accepted=candidate;
            }
            if((accepted-from).sqrMagnitude<.00001f && delta.sqrMagnitude>.00001f)agent.Waiting=true;
            return accepted;
        }
        void PredictAlongRoute(Vector3 from,Vector3 fallback,float distance,out Vector3 point,out Quaternion rotation)
        {
            point=from;rotation=transform.rotation;
            if(route!=null)
            {
                for(int i=routeIndex;i<route.Count;i++)
                {
                    Vector3 target=route[i];target.y=point.y;
                    Vector3 delta=target-point;float length=delta.magnitude;
                    if(length<.001f)continue;
                    Vector3 direction=delta/length;
                    // Parking can reverse; retain its actual body orientation.
                    rotation=Quaternion.LookRotation(Vector3.Dot(transform.forward,direction)<-.5f?-direction:direction);
                    if(length>=distance){point+=direction*distance;return;}
                    point=target;distance-=length;
                }
                return; // Do not invent a continuation beyond the destination.
            }
            point=from+fallback*distance;
        }

        Vector3 KeepLaneCenter(Vector3 from, Vector3 requested)
        {
            if(route==null || routeIndex<=0 || routeIndex>=route.Count)return requested;
            Vector3 start=route[routeIndex-1], end=route[routeIndex];
            Vector3 segment=end-start;segment.y=0f;
            if(segment.sqrMagnitude<16f)return requested;
            bool alongX=Mathf.Abs(segment.z)<.01f;
            bool alongZ=Mathf.Abs(segment.x)<.01f;
            if(!alongX && !alongZ)return requested; // Preserve explicit intersection curves.
            Vector3 motion=requested-from;motion.y=0f;
            float travel=motion.magnitude;
            if(travel<.00001f || Vector3.Dot(motion.normalized,segment.normalized)<.8f)return requested;
            Vector3 relative=from-start;relative.y=0f;
            float t=Vector3.Dot(relative,segment)/segment.sqrMagnitude;
            if(t<0f || t>1f)return requested;
            float error=alongX ? start.z-from.z : start.x-from.x;
            // Spend the movement budget on re-centring before going forward.
            // Move's swept collision/signal checks still validate every correction.
            if(Mathf.Abs(error)>.015f)
            {
                Vector3 corrected=from;
                if(alongX)corrected.z+=Mathf.Clamp(error,-travel,travel);
                else corrected.x+=Mathf.Clamp(error,-travel,travel);
                return corrected;
            }
            if(alongX)requested.z=start.z;else requested.x=start.x;
            return requested;
        }
        public static Quaternion Rotate(Transform vehicle,Quaternion requested)
        {
            var agent=Register(vehicle);
            if(agent.pending)return vehicle.rotation;
            Quaternion accepted=vehicle.rotation;
            if(Quaternion.Angle(accepted,requested)>.05f)agent.demandFrame=Time.frameCount;
            int steps=Mathf.Max(1,Mathf.CeilToInt(Quaternion.Angle(accepted,requested)/3f));
            Quaternion start=accepted;
            for(int i=1;i<=steps;i++)
            {
                Quaternion q=Quaternion.Slerp(start,requested,(float)i/steps);
                if(!agent.Clear(vehicle.position,q))break;
                accepted=q;
            }
            return accepted;
        }
        public bool ExitHasSpace(float junctionX,bool alongX,float direction)
        {
            Vector3 exit=transform.position;
            if(alongX)exit.x=junctionX+direction*(4f+half.y);
            else exit.z=MainBoulevard.Center+direction*(10f+half.y);
            return Clear(exit,transform.rotation);
        }
        public static bool JunctionOccupied(float x,bool movingAlongX,RoadTrafficAgent self)
        {
            foreach(var a in agents)
            {
                if(a==null || a==self || a.pending || !a.isActiveAndEnabled)continue;
                // Include the whole body, especially the tail of a bus/truck during a turn.
                // Stopped approaches remain outside this box; even same-axis turns conflict.
                if(a.OccupiesJunction(x,9.8f) && !(self.IsStraightAtJunction(x) && a.IsStraightAtJunction(x)))return true;
            }
            return false;
        }
    }
}
