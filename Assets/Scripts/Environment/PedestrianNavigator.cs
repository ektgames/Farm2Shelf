using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    public sealed class PedestrianNavigator : MonoBehaviour
    {
        static readonly List<PedestrianNavigator> actors=new List<PedestrianNavigator>();
        static int planningFrame=-1,plans;
        static readonly Queue<PedestrianNavigator> planningQueue=new Queue<PedestrianNavigator>();
        bool planningQueued;
        int lastPlanRequestFrame;
        static readonly float[] avoidanceAngles={45f,75f,-45f,-75f};
        static readonly float[] yieldAngles={90f,-90f,135f,-135f,180f};
        static readonly float[] passingAngles={90f,-90f};
        List<Vector3> path;
        PedestrianNavigation.RouteRequest routeRequest, crowdRequest;
        Vector3 destination;
        int index,version=-1;
        float retryAt;
        Vector3 progressAnchor, detour;
        float progressAt, detourUntil, nextAvoidanceAt;
        int progressVersion = -1;
        PedestrianNavigator blockingActor;
        PedestrianNavigator yieldingTo;
        float yieldUntil, crowdRetryAt, lastStepAt;
        List<Vector3> crowdPath;
        int crowdIndex;
        readonly List<Vector3> crowdPositions = new List<Vector3>();
        bool exitingSeat;
        Vector3 seatExit;
        public void BeginSeatExit(Vector3 point)
        {
            seatExit=point;exitingSeat=true;path=null;routeRequest=null;crowdRequest=null;Unreachable=false;
        }
        public bool Unreachable { get; private set; }
        void OnEnable(){if(!actors.Contains(this))actors.Add(this);}
        void OnDisable(){actors.Remove(this);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){actors.Clear();planningQueue.Clear();planningFrame=-1;plans=0;}
        // A fixed Update order must not let the first two blocked walkers consume
        // every frame's planning budget while everyone behind them waits forever.
        bool TryAcquirePlan()
        {
            lastPlanRequestFrame=Time.frameCount;
            if(!planningQueued){planningQueued=true;planningQueue.Enqueue(this);}
            while(planningQueue.Count>0)
            {
                var first=planningQueue.Peek();
                if(first!=null && first.isActiveAndEnabled && Time.frameCount-first.lastPlanRequestFrame<=2)break;
                planningQueue.Dequeue();
                if(first!=null)first.planningQueued=false;
            }
            if(planningFrame!=Time.frameCount){planningFrame=Time.frameCount;plans=0;}
            if(plans>=2 || planningQueue.Count==0 || planningQueue.Peek()!=this)return false;
            planningQueue.Dequeue();planningQueued=false;plans++;return true;
        }
        public Vector3 Step(Vector3 goal,float distance)
        {
            Vector3 from=transform.position;goal.y=from.y;
            if(distance<=0)return from;
            lastStepAt=Time.time;
            if(exitingSeat)
            {
                seatExit.y=from.y;
                if((seatExit-from).sqrMagnitude<.0064f)exitingSeat=false;
                else if(PedestrianNavigation.Clear(from,seatExit,true))
                {
                    Vector3 exitStep=Vector3.MoveTowards(from,seatExit,distance);
                    return PeopleClear(exitStep)?exitStep:from;
                }
                else exitingSeat=false;
            }
            int revision=PedestrianNavigation.Version;
            if(PedestrianNavigation.IsBlocked(from))
            {
                // Recover old saves / newly placed furniture covering a person by taking the nearest clear exit.
                for(float radius=.3f;radius<=3f;radius+=.3f)
                    for(int a=0;a<16;a++)
                    {
                        Vector3 exit=from+Quaternion.Euler(0,a*22.5f,0)*Vector3.forward*radius;
                        if(!PedestrianNavigation.IsBlocked(exit)&&PedestrianNavigation.Clear(from,exit,true))
                        {path=null;return Vector3.MoveTowards(from,exit,distance);}
                    }
                return from;
            }
            bool changed=version!=revision||(destination-goal).sqrMagnitude>.01f;
            if(changed){routeRequest=null;crowdRequest=null;destination=goal;path=null;crowdPath=null;index=0;retryAt=0;detourUntil=0f;Unreachable=false;version=revision;}
            if (yieldingTo != null && Time.time < yieldUntil)
            {
                Vector3 step=Vector3.MoveTowards(from,detour,distance);
                if (PedestrianNavigation.Clear(from,step) && PeopleClear(step)) return step;
                return from;
            }
            yieldingTo=null;
            if(crowdRequest!=null)
            {
                if(TryAcquirePlan())crowdRequest.Advance();
                if(crowdRequest.Complete)
                {
                    crowdPath=crowdRequest.Result;crowdIndex=0;crowdRequest=null;
                    crowdRetryAt=Time.unscaledTime+.8f;
                }
            }
            if (crowdPath != null)
            {
                while(crowdIndex<crowdPath.Count && (crowdPath[crowdIndex]-from).sqrMagnitude<.01f &&
                    (crowdIndex==crowdPath.Count-1 || PedestrianNavigation.Clear(from,crowdPath[crowdIndex+1]))) crowdIndex++;
                if(crowdIndex<crowdPath.Count)
                {
                    Vector3 step=Vector3.MoveTowards(from,crowdPath[crowdIndex],distance);
                    if(PedestrianNavigation.Clear(from,step) && PeopleClear(step)) return step;
                }
                crowdPath=null;
            }
            // Replan a stale corner approach instead of repeating the same segment forever.
            if (progressVersion != revision || changed || (from-progressAnchor).sqrMagnitude > .01f)
            { progressAnchor=from; progressAt=Time.unscaledTime; progressVersion=revision; }
            else if (routeRequest==null && Time.unscaledTime-progressAt > 1.25f)
            { path=null; index=0; retryAt=0; progressAt=Time.unscaledTime; }
            if (Time.time < detourUntil)
            {
                Vector3 giveWay = Vector3.MoveTowards(from,detour,distance);
                if (PedestrianNavigation.Clear(from,giveWay) && PeopleClear(giveWay))
                    return giveWay;
                detourUntil=0f;
            }
            Vector3 target=goal;
            if(!PedestrianNavigation.Clear(from,goal))
            {
                if(path==null || index>=path.Count)
                {
                    if(Time.unscaledTime<retryAt)return from;
                    if(!TryAcquirePlan())return from;
                    if(routeRequest==null)routeRequest=PedestrianNavigation.RequestRoute(from,goal);
                    routeRequest.Advance();
                    if(!routeRequest.Complete)return from;
                    path=routeRequest.Result;routeRequest=null;index=0;
                    if(path==null){retryAt=Time.unscaledTime+1f;Unreachable=true;return from;}
                    Unreachable=false;
                }
                // Proximity alone cannot consume a corner: the next leg may still
                // intersect the shelf until we finish reaching this waypoint.
                while(index<path.Count-1 && (path[index]-from).sqrMagnitude<.012f &&
                    PedestrianNavigation.Clear(from,path[index+1]))index++;
                // Smooth only across genuinely clear segments, never across obstacle corners.
                for(int next=index+1;next<path.Count;next++)
                {if(!PedestrianNavigation.Clear(from,path[next]))break;index=next;}
                target=path[index];
            }
            else {path=null;routeRequest=null;Unreachable=false;}
            Vector3 candidate=Vector3.MoveTowards(from,target,distance);
            if(!PedestrianNavigation.Clear(from,candidate)){path=null;retryAt=0;return from;}
            if(PeopleClear(candidate))return candidate;
            var originalBlocker=blockingActor;
            // Plan around occupied space before attempting tiny lateral steps.
            // These routes retain their intermediate points instead of smoothing through people.
            if(crowdRequest==null && Time.unscaledTime>=crowdRetryAt)
            {
                if(TryAcquirePlan())
                {
                    crowdRetryAt=Time.unscaledTime+.8f; crowdPositions.Clear();
                    foreach(var actor in actors)
                        if(actor!=null && actor!=this && actor.isActiveAndEnabled &&
                           Mathf.Abs(actor.transform.position.y-from.y)<.6f &&
                           (actor.transform.position-from).sqrMagnitude<64f)
                            crowdPositions.Add(actor.transform.position);
                    crowdRequest=PedestrianNavigation.RequestRoute(from,goal,CrowdSegmentClear);
                    crowdRequest.Advance();
                }
                // One walker retreats and keeps its commitment long enough for
                // the other to pass. The request also reaches the blocking walker.
                if(originalBlocker!=null && Time.time-originalBlocker.lastStepAt<.5f)
                {
                    if(GetInstanceID()<originalBlocker.GetInstanceID()) originalBlocker.TryYieldTo(this);
                    else if(TryYieldTo(originalBlocker)) return from;
                }
            }
            Vector3 direction=(target-from).normalized;
            // Soft body avoidance; both oncoming walkers keep to their own right.
            foreach(float angle in avoidanceAngles)
            {
                Vector3 side=from+Quaternion.Euler(0,angle,0)*direction*Mathf.Min(distance,.12f);
                if(PedestrianNavigation.Clear(from,side)&&PeopleClear(side))return side;
            }
            // A tiny sideways step is insufficient in a one-person aisle. Commit
            // to a clear waiting position; fixed priority prevents mutual retreat.
            if (Time.time >= nextAvoidanceAt)
            {
                nextAvoidanceAt=Time.time+.4f;
                PeopleClear(candidate);
                var blocker=blockingActor;
                bool yieldBack=blocker!=null && GetInstanceID()>blocker.GetInstanceID();
                float[] angles=yieldBack ? yieldAngles : passingAngles;
                foreach(float angle in angles)
                {
                    Vector3 waiting=from+Quaternion.Euler(0,angle,0)*direction*.65f;
                    if (!PedestrianNavigation.Clear(from,waiting) || !PeopleClear(waiting)) continue;
                    detour=waiting; detourUntil=Time.time+1.1f; path=null;
                    return Vector3.MoveTowards(from,waiting,distance);
                }
            }
            return from;
        }
        bool CrowdSegmentClear(Vector3 a,Vector3 b)
        {
            a.y=b.y=0f;Vector3 step=b-a;
            foreach(var position in crowdPositions)
            {
                Vector3 delta=position-a;delta.y=0f;
                float dot=Vector3.Dot(delta,step);
                float t=step.sqrMagnitude>.000001f?Mathf.Clamp01(dot/step.sqrMagnitude):0f;
                // Permit an already touching walker to separate, not to cross through.
                if(delta.sqrMagnitude<.1764f && dot<0f) continue;
                if((delta-step*t).sqrMagnitude<.1764f)return false;
            }
            return true;
        }
        bool TryYieldTo(PedestrianNavigator priority)
        {
            if(yieldingTo!=null && Time.time<yieldUntil)return false;
            Vector3 away=transform.position-priority.transform.position;away.y=0f;
            if(away.sqrMagnitude<.001f) away=-transform.forward;
            away.Normalize();
            foreach(float length in new[]{.65f,1.1f,1.6f})
                foreach(float angle in new[]{90f,-90f,45f,-45f,0f})
                {
                    Vector3 point=transform.position+Quaternion.Euler(0,angle,0)*away*length;
                    if(!PedestrianNavigation.Clear(transform.position,point)||!PeopleClear(point))continue;
                    detour=point;yieldingTo=priority;yieldUntil=Time.time+2f;
                    crowdPath=null;path=null;return true;
                }
            return false;
        }
        bool PeopleClear(Vector3 point)
        {
            blockingActor=null;
            foreach(var other in actors)
            {
                if(other==null||other==this||!other.isActiveAndEnabled)continue;
                Vector3 delta=other.transform.position-point;
                if(Mathf.Abs(delta.y)>.6f)continue;delta.y=0;
                Vector3 old=other.transform.position-transform.position;old.y=0;
                // Permit separating agents that already overlap in an older save.
                Vector3 step=point-transform.position;step.y=0;
                float along=step.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(old,step)/step.sqrMagnitude):0f;
                float closest=(old-step*along).sqrMagnitude;
                if(closest<.16f&&closest<old.sqrMagnitude-.00001f){blockingActor=other;return false;}
            }
            return true;
        }
    }
}
