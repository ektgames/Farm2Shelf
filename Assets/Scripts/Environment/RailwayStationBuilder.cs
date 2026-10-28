using UnityEngine;
using System.Collections.Generic;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    // Separate from the road graph: these corridors are reserved for future trains.
    public static class RailwayStationBuilder
    {
        public const float NorthboundTrackX = 178f;
        public const float SouthboundTrackX = 183f;
        public const float TrackSouthZ = -195f;
        public const float TrackNorthZ = 240f;
        public const float CrossingZ = MainBoulevard.Center;

        public static void Build(Transform parent)
        {
            var root = new GameObject("East_Railway_Station").transform;
            root.SetParent(parent, false);
            var lighting = root.gameObject.AddComponent<RailwayStationLighting>();
            var stone = Mat("Station_Stone", new Color(.72f,.69f,.61f));
            var cream = Mat("Station_Plaster", new Color(.88f,.81f,.66f));
            var dark = Mat("Station_Metal", new Color(.12f,.18f,.22f));
            var glass = Mat("Station_Glass", new Color(.19f,.43f,.53f));
            var roof = Mat("Station_Roof", new Color(.42f,.17f,.12f));
            var steel = Mat("Rail_Steel", new Color(.52f,.56f,.59f));
            var gravel = Mat("Rail_Ballast", new Color(.30f,.29f,.27f));
            var sleeper = Mat("Rail_Sleepers", new Color(.27f,.21f,.16f));
            var yellow = Mat("Station_SafetyYellow", new Color(1f,.76f,.12f));
            var white = Mat("Crossing_White", new Color(.94f,.94f,.89f));
            var red = Mat("Crossing_Red", new Color(.82f,.08f,.06f));
            var lawn = Mat("Station_Ground", new Color(.28f,.62f,.28f));
            Box(root,"Railway_Ground_Extension",new Vector3(178,-.18f,22.5f),new Vector3(47,.1f,439),lawn);

            // Front faces west, towards Avenue B. No building touches its sidewalk (ends X=154.5).
            Box(root,"Station_Forecourt",new Vector3(160,.05f,22),new Vector3(11,.14f,39),stone);
            // A continuous paved apron replaces the grass slit between the building and platform.
            Box(root,"Station_Paved_Apron",new Vector3(164.875f,.04f,23),new Vector3(20.75f,.16f,43),stone);
            Box(root,"Building_Platform_Concourse",new Vector3(170.125f,.35f,23),new Vector3(1.25f,.65f,23),stone,true);
            foreach(float z in new[]{10.5f,35.5f})
                for(int step=0;step<3;step++)
                    Box(root,"Concourse_Access_Step",new Vector3(168.5f,.21f+step*.18f,z+(z<23?step:-step)*.4f),new Vector3(4.5f,.18f,.4f),stone);
            Box(root,"Concourse_Canopy",new Vector3(170.25f,4.6f,23),new Vector3(1.7f,.18f,23),dark);
            Box(root,"Station_Building",new Vector3(165,2.45f,23),new Vector3(9,4.8f,23),cream,true).gameObject.AddComponent<RailwayStationClickable>();
            Box(root,"Station_Foundation",new Vector3(165,.22f,23),new Vector3(9.5f,.4f,23.5f),stone);
            Box(root,"Station_Roof",new Vector3(165,5.05f,23),new Vector3(10.4f,.5f,24.4f),roof);
            Box(root,"Station_Roof_Trim",new Vector3(165,4.74f,23),new Vector3(10.6f,.15f,24.6f),dark);
            for (int i=0;i<5;i++)
            {
                float z=14+i*4.5f;
                Box(root,"Front_Window_Frame",new Vector3(160.44f,2.6f,z),new Vector3(.16f,2.2f,2.5f),dark);
                var window=Box(root,"Front_Window",new Vector3(160.34f,2.6f,z),new Vector3(.05f,1.85f,2.1f),glass);
                lighting.Windows.Add(window.gameObject);
                if(i!=2)
                {
                    Box(root,"Platform_Window_Frame",new Vector3(169.56f,2.6f,z),new Vector3(.16f,2.2f,2.5f),dark);
                    window=Box(root,"Platform_Window",new Vector3(169.66f,2.6f,z),new Vector3(.05f,1.85f,2.1f),glass);
                    lighting.Windows.Add(window.gameObject);
                }
            }
            Box(root,"Platform_Exit_Door",new Vector3(169.65f,2.1f,23),new Vector3(.12f,2.8f,2.5f),glass);
            Box(root,"Entrance_Double_Door",new Vector3(160.25f,1.65f,23),new Vector3(.12f,3.1f,3.2f),glass);
            Box(root,"Entrance_Canopy",new Vector3(158.4f,3.8f,23),new Vector3(4,.22f,6),dark);
            Box(root,"Station_Sign",new Vector3(160.2f,4.15f,23),new Vector3(.15f,.65f,7),dark);
            // Train pictogram reads from the street without language-specific text.
            Box(root,"Sign_Train",new Vector3(160.1f,4.15f,23),new Vector3(.04f,.42f,.8f),white);
            for(int i=-1;i<=1;i+=2)
                Box(root,"Sign_Rail",new Vector3(160.07f,3.94f,23+i*.25f),new Vector3(.04f,.08f,.1f),yellow);

            foreach(float x in new[]{173f,188f})
            {
                Box(root,"Passenger_Platform",new Vector3(x,.35f,23),new Vector3(4.5f,.65f,43),stone,true);
                float edge=x<180?x+1.9f:x-1.9f;
                Box(root,"Platform_Safety_Line",new Vector3(edge,.69f,23),new Vector3(.22f,.025f,42),yellow);
                Box(root,"Platform_Canopy",new Vector3(x,4.6f,23),new Vector3(4.3f,.18f,29),dark);
                foreach(float edgeX in new[]{x-2.13f,x+2.13f})
                    Box(root,"Canopy_Fascia",new Vector3(edgeX,4.53f,23),new Vector3(.12f,.32f,29),cream);
                foreach(float lampZ in new[]{13f,23f,33f})
                    AddLamp(root,lighting,new Vector3(x,4.36f,lampZ),dark,white);
                for(float z=10;z<=36;z+=6.5f)
                {
                    Box(root,"Canopy_Column",new Vector3(x,2.6f,z),new Vector3(.16f,4,.16f),dark);
                    Box(root,"Platform_Bench",new Vector3(x,.98f,z+1),new Vector3(1,.18f,2),sleeper);
                    Box(root,"Bench_Base",new Vector3(x,.8f,z+1),new Vector3(.55f,.25f,1.5f),dark);
                    Box(root,"Bench_Backrest",new Vector3(x+(x<180?-.43f:.43f),1.4f,z+1),new Vector3(.14f,.7f,2),sleeper);
                }
            }
            // Both platforms are reached without walking across the rails.
            Box(root,"Footbridge_Deck",new Vector3(180.5f,6.5f,42),new Vector3(19,.3f,2.5f),stone);
            foreach(float z in new[]{40.8f,43.2f})
            {
                Box(root,"Footbridge_Handrail",new Vector3(180.5f,7.5f,z),new Vector3(19,.12f,.1f),dark);
                for(float x=171.2f;x<=189.8f;x+=1.55f)
                    Box(root,"Footbridge_Railing_Post",new Vector3(x,7.08f,z),new Vector3(.08f,.85f,.08f),dark);
            }
            foreach(float x in new[]{172f,189f})
            {
                Box(root,"Bridge_Support",new Vector3(x,3.5f,43.7f),new Vector3(.3f,5.7f,.3f),dark);
                Box(root,"Bridge_Lamp_Post",new Vector3(x,7.7f,43.1f),new Vector3(.1f,2.2f,.1f),dark);
                AddLamp(root,lighting,new Vector3(x,8.75f,43.1f),dark,white);
            }
            foreach(float z in new[]{8f,38f})
            {
                Box(root,"Forecourt_Lamp_Post",new Vector3(156.5f,2.2f,z),new Vector3(.12f,4.2f,.12f),dark);
                AddLamp(root,lighting,new Vector3(156.5f,4.3f,z),dark,white);
            }
            AddLamp(root,lighting,new Vector3(158.4f,3.55f,23),dark,white);
            foreach(float x in new[]{172f,189f})
                for(int i=0;i<24;i++)
                    Box(root,"Platform_Stair",new Vector3(x,.68f+(i+1)*.24f,30+i*.5f),new Vector3(1.8f,.25f,.52f),stone);

            Box(root,"Level_Crossing_Continuous_Deck",new Vector3(180.5f,.05f,MainBoulevard.Center),new Vector3(11,.18f,18f),stone);
            // Continuous crushed-stone bed between both tracks, instead of a strip of grass.
            foreach(var segment in new[]{new Vector2(TrackSouthZ,-15.2f),new Vector2(-2.8f,TrackNorthZ)})
                Box(root,"Double_Track_Ballast_Infill",new Vector3(180.5f,-.01f,(segment.x+segment.y)/2),new Vector3(1.4f,.12f,segment.y-segment.x),gravel);
            foreach(float x in new[]{NorthboundTrackX,SouthboundTrackX})
            {
                var track=new GameObject(x==NorthboundTrackX?"Northbound_Train_Only":"Southbound_Train_Only").transform;
                track.SetParent(root,false);
                // Ballast stops at asphalt; embedded rail deck remains level at the road crossing.
                foreach(var segment in new[]{new Vector2(TrackSouthZ,-15.2f),new Vector2(-2.8f,TrackNorthZ)})
                    Box(track,"Ballast",new Vector3(x,-.01f,(segment.x+segment.y)/2),new Vector3(3.6f,.12f,segment.y-segment.x),gravel);
                for(float z=TrackSouthZ;z<TrackNorthZ;z+=.8f)
                    if(z< -15.4f || z> -2.6f)
                        Box(track,"Sleeper",new Vector3(x,.08f,z),new Vector3(2.5f,.14f,.24f),sleeper);
                foreach(float dx in new[]{-.72f,.72f})
                    Box(track,"Continuous_Rail",new Vector3(x+dx,.18f,(TrackSouthZ+TrackNorthZ)*.5f),new Vector3(.09f,.12f,TrackNorthZ-TrackSouthZ),steel);
                Box(track,"Level_Crossing_Deck",new Vector3(x,.075f,MainBoulevard.Center),new Vector3(3.4f,.15f,15),stone);
            }
            // Road vehicles retain their existing east/west lanes, never the railway corridors.
            var control=root.gameObject.AddComponent<RailwayCrossingController>();
            var arms=new Transform[2];
            for(int i=0;i<2;i++)
            {
                float x=i==0?174:187;
                float z=i==0?-21.5f:-5.5f;
                Box(root,"Crossing_Barrier_Base",new Vector3(x,.65f,z),new Vector3(.65f,1.3f,.65f),dark,true);
                var pivot=new GameObject("Barrier_Pivot").transform;
                pivot.SetParent(root,false); pivot.localPosition=new Vector3(x,1.25f,z);
                pivot.localRotation=Quaternion.Euler(i==0?-88:88,0,0);
                float direction=i==0?1:-1;
                for(int n=0;n<32;n++)
                    Box(pivot,"Barrier_Stripe",new Vector3(0,0,direction*(n*.5f+.25f)),new Vector3(.18f,.2f,.5f),n%2==0?white:red);
                arms[i]=pivot;
                Box(root,"Stop_Line",new Vector3(x+(i==0?-1:1),.035f,MainBoulevard.Center),new Vector3(.2f,.025f,15),white);
                Box(root,"Crossing_Signal_Post",new Vector3(x,1.8f,z),new Vector3(.12f,3.6f,.12f),dark);
                var sign=Box(root,"Crossbuck",new Vector3(x,3.2f,z),new Vector3(.15f,.16f,1.2f),white);
                sign.localRotation=Quaternion.Euler(35,0,0);
                sign=Box(root,"Crossbuck",new Vector3(x,3.2f,z),new Vector3(.15f,.16f,1.2f),white);
                sign.localRotation=Quaternion.Euler(-35,0,0);
            }
            control.Configure(arms);
            root.gameObject.AddComponent<DailyFreightTrainController>().Configure(control);
            // Fence separates the railway from the street, with openings for the station and crossing.
            foreach(float x in new[]{175.5f,191f})
                for(float z=TrackSouthZ;z<TrackNorthZ-2;z+=3)
                {
                    if(z>-26 && z<0) continue;
                    if(x<180 && z>=0 && z<48) continue;
                    Box(root,"Railway_Fence_Post",new Vector3(x,.7f,z),new Vector3(.1f,1.4f,.1f),dark);
                    foreach(float y in new[]{.5f,1.2f})
                        Box(root,"Railway_Fence_Rail",new Vector3(x,y,z+1.5f),new Vector3(.06f,.06f,3),dark);
                }
            CombineRepeatedDetails(root);
        }

        static void AddLamp(Transform root,RailwayStationLighting lighting,Vector3 position,Material housing,Material lens)
        {
            Box(root,"Station_Luminaire_Housing",position+Vector3.up*.09f,new Vector3(.65f,.18f,1.2f),housing);
            var bulb=Box(root,"Station_Luminaire_Lens",position,new Vector3(.5f,.07f,1),lens);
            var lightObject=new GameObject("Station_Warm_Light");
            lightObject.transform.SetParent(root,false);
            lightObject.transform.localPosition=position+Vector3.down*.18f;
            var light=lightObject.AddComponent<Light>();
            light.type=LightType.Point;
            light.color=new Color(1f,.84f,.61f);
            light.intensity=2.2f; light.range=10f;
            light.shadows=LightShadows.None; light.enabled=false;
            lighting.Bulbs.Add(bulb.gameObject); lighting.Lights.Add(light);
        }

        // Hundreds of sleepers/fence pieces share two draw meshes instead of individual renderers.
        static void CombineRepeatedDetails(Transform root)
        {
            var groups = new Dictionary<Material,List<CombineInstance>>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.name!="Sleeper" && !filter.name.StartsWith("Railway_Fence_")) continue;
                var renderer=filter.GetComponent<MeshRenderer>();
                var material=renderer.sharedMaterial;
                if(!groups.TryGetValue(material,out var list)) groups[material]=list=new List<CombineInstance>();
                list.Add(new CombineInstance { mesh=filter.sharedMesh, transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix });
                filter.gameObject.SetActive(false);
                if(Application.isPlaying) Object.Destroy(filter.gameObject); else Object.DestroyImmediate(filter.gameObject);
            }
            foreach(var pair in groups)
            {
                var go=new GameObject("Railway_Details_"+pair.Key.name);
                go.transform.SetParent(root,false);
                var mesh=new Mesh { name="Railway_Combined_Details", indexFormat=UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray());
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
            }
        }

        static Material Mat(string name,Color color)
        {
            var m=new Material(ShaderHelper.GetLitShader()){name=name};
            ShaderHelper.BindOpaqueColorMaps(m,color); return m;
        }
        static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool solid=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
            go.transform.SetParent(parent,false); go.transform.localPosition=p; go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;
            var collider=go.GetComponent<Collider>();
            if(!solid && collider!=null){collider.enabled=false;if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);}
            return go.transform;
        }
    }
}
