using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    public sealed class MainRoadSignals : MonoBehaviour
    {
        static MainRoadSignals active;
        static readonly float[] junctions=MainBoulevard.Junctions;
        sealed class Head { public bool alongX; public float junctionX; public int lastPhase=-1; public Renderer[] bulbs; public Light ground; }
        readonly List<Head> heads=new List<Head>();
        sealed class Grant { public RoadTrafficAgent agent; public bool straight; }
        readonly Dictionary<float, List<Grant>> reservations = new Dictionary<float, List<Grant>>();
        const float JunctionRadius = 10f;
        Material off,red,amber,green;
        // Simulation seconds: main 36 + amber 3 + clearance 3 + side 12 + amber 3 + clearance 3.
        const float CycleSeconds=60f;
        const float CorridorSpeed=10.5f;
        int PhaseAt(float x)
        {
            float offset=(x-junctions[0])/CorridorSpeed;
            // Scaled Unity time is shared by all signals and survives rebuilding the map.
            // Read the same clock for visuals and stop-line decisions, independent of Update order.
            float cycleTime=(float)(Time.timeAsDouble % CycleSeconds);
            float t=Mathf.Repeat(cycleTime-offset,CycleSeconds);
            return t<36?0:t<39?1:t<42?2:t<54?3:t<57?4:5;
        }
        public static void Build(Transform parent)
        {
            var root=new GameObject("Main_Road_Traffic_Signals");root.transform.SetParent(parent,false);
            var controller=root.AddComponent<MainRoadSignals>();active=controller;
            controller.Create();
        }
        void Create()
        {
            off=Mat("Signal_Unlit",new Color(.065f,.075f,.08f),false);
            red=Mat("Signal_Red",new Color(1,.025f,.015f),true);
            amber=Mat("Signal_Amber",new Color(1,.53f,.015f),true);
            green=Mat("Signal_Green",new Color(.02f,1,.25f),true);
            foreach(float x in junctions)
            {
                // Only the main highway junctions. Existing shop gates and railway remain separate.
                AddHead(new Vector3(x-10.5f,0,-21.65f),true,90,x,new Vector3(x-11f,2.6f,MainBoulevard.EastOuter));
                AddHead(new Vector3(x+10.5f,0,-5.35f),true,-90,x,new Vector3(x+11f,2.6f,MainBoulevard.WestOuter));
                AddHead(new Vector3(x-3.65f,0,-2f),false,180,x,new Vector3(x-1.5f,2.6f,-2f));
                AddHead(new Vector3(x+3.65f,0,-25f),false,0,x,new Vector3(x+1.5f,2.6f,-26f));
            }
            UpdateHeads();
        }
        void AddHead(Vector3 position,bool alongX,float yaw,float junctionX,Vector3 reflectionPosition)
        {
            // Bridge-side signals stand on the elevated sidewalk, not beneath its deck.
            position.y=MainBoulevard.OnBridge(position.x)
                ? CityTrafficManager.GetBridgeElevation(position.x,MainBoulevard.Center,out _)+.14f : .16f;
            reflectionPosition.y=CityTrafficManager.GetBridgeElevation(reflectionPosition.x,reflectionPosition.z,out _)+2.6f;
            var root=new GameObject("Signal_Head").transform;root.SetParent(transform,false);root.localPosition=position;root.localRotation=Quaternion.Euler(0,yaw,0);
            Box(root,"Pole",new Vector3(0,1.65f,0),new Vector3(.12f,3.3f,.12f),off);
            Box(root,"Base",new Vector3(0,.18f,0),new Vector3(.36f,.36f,.36f),off);
            Box(root,"Housing",new Vector3(0,3.3f,0),new Vector3(.5f,1.4f,.32f),off);
            var lamps=new Renderer[3];
            for(int i=0;i<3;i++)
            {
                var lens=GameObject.CreatePrimitive(PrimitiveType.Sphere);lens.name="Signal_Lens";lens.transform.SetParent(root,false);
                lens.transform.localPosition=new Vector3(0,3.75f-i*.44f,-.19f);lens.transform.localScale=new Vector3(.32f,.32f,.12f);
                Object.Destroy(lens.GetComponent<Collider>());lamps[i]=lens.GetComponent<Renderer>();lamps[i].sharedMaterial=off;lamps[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                Box(root,"Visor",new Vector3(0,3.94f-i*.44f,-.27f),new Vector3(.44f,.07f,.35f),off);
            }
            var pool=new GameObject("Red_Light_On_Asphalt");pool.transform.SetParent(transform,false);
            pool.transform.localPosition=reflectionPosition;
            pool.transform.localRotation=Quaternion.Euler(90,0,0);
            var light=pool.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=42f;light.innerSpotAngle=24f;light.color=new Color(1,.035f,.02f);light.range=3.2f;light.intensity=3f;light.shadows=LightShadows.None;
            heads.Add(new Head{alongX=alongX,junctionX=junctionX,bulbs=lamps,ground=light});
        }
        void Update(){UpdateHeads();}
        void UpdateHeads()
        {
            foreach(var h in heads)
            {
                int phase=PhaseAt(h.junctionX);
                if(h.lastPhase==phase)continue;
                h.lastPhase=phase;
                bool go=h.alongX?phase==0:phase==3;
                bool caution=h.alongX?phase==1:phase==4;
                h.bulbs[0].sharedMaterial=!go&&!caution?red:off;
                h.bulbs[1].sharedMaterial=caution?amber:off;
                h.bulbs[2].sharedMaterial=go?green:off;
                h.ground.enabled=!go&&!caution;
            }
        }
        public static bool CanAdvance(RoadTrafficAgent agent,Vector3 from,Vector3 to,float length,bool reserve = false)
        {
            if(active==null)return true;
            Vector3 movement=to-from;
            bool alongX=Mathf.Abs(movement.x)>=Mathf.Abs(movement.z);
            foreach(float x in junctions)
            {
                if(!active.reservations.TryGetValue(x,out var grants))
                {grants=new List<Grant>();active.reservations[x]=grants;}
                bool owns=false,hasTurningGrant=false;
                for(int i=grants.Count-1;i>=0;i--)
                {
                    var g=grants[i];
                    if(g.agent==null || !g.agent.isActiveAndEnabled || g.agent.Pending || !g.agent.OccupiesJunction(x,JunctionRadius+.75f))
                    {grants.RemoveAt(i);continue;}
                    owns |= g.agent==agent;
                    hasTurningGrant |= !g.straight;
                }
                // Keep the grant throughout a turn and through signal changes until the rear clears.
                if (owns) continue;
                if(alongX && !MainBoulevard.OnRoad(from.z))continue;
                if(!alongX && Mathf.Abs(from.x-x)>3.1f)continue;
                float sign=Mathf.Sign(alongX?movement.x:movement.z);
                float before=sign*((alongX?x:MainBoulevard.Center)-(alongX?from.x:from.z));
                float after=sign*((alongX?x:MainBoulevard.Center)-(alongX?to.x:to.z));
                Vector3 approach = alongX ? Vector3.right * sign : Vector3.forward * sign;
                float stop=JunctionRadius+agent.FrontExtent(approach);
                // Once across the stop line, clear the junction even if the phase changes.
                if(before>=stop-.001f && after<stop)
                {
                    int phase=active.PhaseAt(x);
                    bool allowed=alongX?phase==0:phase==3;
                    bool straight=alongX && agent.IsStraightAtJunction(x);
                    bool conflicts=grants.Count>0 && (!straight || hasTurningGrant);
                    if(!allowed){agent.NotifySignalWait();return false;}
                    if(conflicts || RoadTrafficAgent.JunctionOccupied(x,alongX,agent) ||
                        !agent.RouteHasSpace(x, movement.normalized, JunctionRadius)) return false;
                    // Look-ahead braking must never acquire a junction that the car has not entered.
                    if (reserve) grants.Add(new Grant{agent=agent,straight=straight});
                }
            }
            return true;
        }
        void OnDestroy(){if(active==this)active=null;foreach(var m in new[]{off,red,amber,green})if(m!=null)Destroy(m);}
        static Material Mat(string name,Color c,bool glow){return glow?ShaderHelper.CreateUnlitOpaqueMaterial(c,name):ShaderHelper.CreateLitMaterial(c,name);}
        static void Box(Transform root,string name,Vector3 p,Vector3 s,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;g.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;Object.Destroy(g.GetComponent<Collider>());}
    }
}
