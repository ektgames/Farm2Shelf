using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    // Future trains reserve before approaching and release only after their last carriage clears.
    // Multiple trains can reserve together: one departing train cannot open the other train's gate.
    public sealed class RailwayCrossingController : MonoBehaviour
    {
        public static RailwayCrossingController Active { get; private set; }
        void OnEnable() { Active=this; }
        readonly HashSet<Object> reservations = new HashSet<Object>();
        Transform[] arms;
        float closure;
        public bool IsClosed => closure >= .999f;
        public bool IsRoadBlocked => reservations.Count > 0 || closure > .001f;
        public void Configure(Transform[] pivots) { arms = pivots; }
        public void Reserve(Object train) { if(train != null) reservations.Add(train); }
        public void Release(Object train) { if(train != null) reservations.Remove(train); }
        public void SynchronizePose()
        {
            closure=reservations.Count>0?1:0;
            ApplyPose();
        }
        public static float RoadSpeed(Vector3 position,Vector3 direction,float speed)
        {
            if(Active==null || !Active.IsRoadBlocked || !MainBoulevard.OnRoad(position.z) || Mathf.Abs(direction.x)<.5f)return speed;
            float distance=direction.x>0?169-position.x:position.x-192;
            // Vehicles already inside must clear the tracks, rather than stopping on them.
            if(distance<-.1f)return speed;
            return Mathf.Min(speed,Mathf.Max(0,distance-.05f)*2f);
        }
        public static Vector3 ClampRoadMove(Vector3 from,Vector3 to)
        {
            if(Active==null || !Active.IsRoadBlocked || !MainBoulevard.OnRoad(from.z))return to;
            if(to.x>from.x && from.x<=169 && to.x>169) to.x=169;
            if(to.x<from.x && from.x>=192 && to.x<192) to.x=192;
            return to;
        }
        void Update()
        {
            reservations.RemoveWhere(train => train == null);
            closure=Mathf.MoveTowards(closure,reservations.Count>0?1:0,Time.deltaTime/2f);
            ApplyPose();
        }
        void ApplyPose()
        {
            if(arms==null)return;
            for(int i=0;i<arms.Length;i++)
                if(arms[i]!=null) arms[i].localRotation=Quaternion.Euler((i==0?-88:88)*(1-closure),0,0);
        }
        void OnDisable() { reservations.Clear(); closure=0; if(Active==this)Active=null; }
    }
}
