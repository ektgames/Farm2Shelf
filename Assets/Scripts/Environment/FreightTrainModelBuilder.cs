using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    // One locomotive + seven individually detailed freight wagons. Forward is local +Z.
    public static class FreightTrainModelBuilder
    {
        public const float RearExtent = 36.5f;
        public const float FrontExtent = 2.8f;
        public static Transform Build(Transform parent)
        {
            var root=new GameObject("Daily_Freight_Train_7_Wagons").transform;
            root.SetParent(parent,false);
            var frame=Mat("Train_Chassis",new Color(.09f,.11f,.13f),.65f);
            var metal=Mat("Train_Steel",new Color(.48f,.53f,.56f),.8f);
            var blue=Mat("Locomotive_Deep_Teal",new Color(.04f,.30f,.37f),.35f);
            var glass=Mat("Train_Cab_Glass",new Color(.13f,.26f,.32f),.7f);
            var ivory=Mat("Train_Ivory",new Color(.86f,.85f,.73f),.25f);
            var gold=Mat("Train_Safety_Yellow",new Color(.97f,.66f,.08f),.15f);
            var wood=Mat("Timber_Load",new Color(.47f,.28f,.12f),.05f);
            var rust=Mat("Container_Terracotta",new Color(.62f,.22f,.12f),.25f);
            var green=Mat("Container_Sage",new Color(.22f,.37f,.28f),.25f);
            var loco=Group(root,"Diesel_Locomotive",0);
            Underframe(loco,5.2f,frame,metal);
            Box(loco,"Engine_Hood",new Vector3(0,1.8f,-.65f),new Vector3(2.05f,1.7f,3.25f),blue);
            Box(loco,"Cab",new Vector3(0,2.25f,1.45f),new Vector3(2.2f,2.35f,1.65f),blue);
            Box(loco,"Cab_Roof",new Vector3(0,3.49f,1.4f),new Vector3(2.35f,.16f,1.95f),ivory);
            Box(loco,"Windshield",new Vector3(0,2.8f,2.29f),new Vector3(1.8f,.7f,.045f),glass);
            Box(loco,"Windshield_Mullion",new Vector3(0,2.8f,2.33f),new Vector3(.07f,.75f,.04f),metal);
            Box(loco,"Pilot",new Vector3(0,.8f,2.6f),new Vector3(2.2f,.55f,.23f),frame);
            for(int i=-3;i<=3;i++)
                Box(loco,"Pilot_Warning_Stripe",new Vector3(i*.29f,.85f,2.73f),new Vector3(.14f,.4f,.025f),gold).localRotation=Quaternion.Euler(0,0,-25);
            foreach(float side in new[]{-1f,1f})
            {
                Box(loco,"Side_Window",new Vector3(side*1.12f,2.8f,1.5f),new Vector3(.04f,.7f,1.05f),glass);
                Box(loco,"Livery_Stripe",new Vector3(side*1.04f,1.5f,-.65f),new Vector3(.04f,.23f,3.2f),ivory);
                for(int i=0;i<10;i++) Box(loco,"Radiator_Slat",new Vector3(side*1.05f,2.2f,-2+i*.24f),new Vector3(.035f,.65f,.08f),frame);
                for(int i=0;i<3;i++) Box(loco,"Cab_Access_Step",new Vector3(side*1.18f,.65f+i*.21f,1.5f),new Vector3(.24f,.08f,.55f),metal);
                Box(loco,"Walkway_Handrail",new Vector3(side*1.16f,1.7f,-.9f),new Vector3(.05f,.05f,3),gold);
                foreach(float z in new[]{-2.2f,-.8f,.5f}) Box(loco,"Handrail_Post",new Vector3(side*1.16f,1.3f,z),new Vector3(.05f,.8f,.05f),gold);
                Box(loco,"Headlight",new Vector3(side*.68f,2,2.31f),new Vector3(.3f,.22f,.07f),ivory);
            }
            foreach(float z in new[]{-1.65f,-.65f})
            {
                Cylinder(loco,"Cooling_Fan",new Vector3(0,2.7f,z),new Vector3(.8f,.07f,.8f),frame,Quaternion.identity);
                Box(loco,"Fan_Crossbar",new Vector3(0,2.79f,z),new Vector3(.78f,.035f,.05f),metal);
            }
            Cylinder(loco,"Exhaust",new Vector3(.65f,2.93f,-.2f),new Vector3(.22f,.28f,.22f),frame,Quaternion.identity);
            for(int i=0;i<7;i++)
            {
                var wagon=Group(root,"Freight_Wagon_"+(i+1),-5.1f-i*4.7f);
                Underframe(wagon,4.35f,frame,metal);
                var paint=i%3==0?rust:i%3==1?blue:green;
                if(i==2 || i==5)
                {
                    Cylinder(wagon,"Tank_Barrel",new Vector3(0,2.05f,0),new Vector3(1.85f,1.75f,1.85f),ivory,Quaternion.Euler(90,0,0));
                    foreach(float z in new[]{-1.4f,1.4f})
                    {
                        Cylinder(wagon,"Tank_End",new Vector3(0,2.05f,z),new Vector3(1.88f,.06f,1.88f),metal,Quaternion.Euler(90,0,0));
                        Box(wagon,"Tank_Cradle",new Vector3(0,1.16f,z),new Vector3(1.9f,.45f,.3f),frame);
                    }
                    Cylinder(wagon,"Filler_Hatch",new Vector3(0,3.05f,0),new Vector3(.5f,.09f,.5f),metal,Quaternion.identity);
                    for(int r=0;r<7;r++) Box(wagon,"Tank_Ladder_Rung",new Vector3(1,1.35f+r*.25f,0),new Vector3(.08f,.04f,.55f),gold);
                }
                else if(i==4)
                {
                    for(int row=0;row<3;row++) for(int col=0;col<4-row;col++)
                        Cylinder(wagon,"Timber_Log",new Vector3(-.72f+col*.48f+row*.24f,1.35f+row*.42f,0),new Vector3(.46f,1.9f,.46f),wood,Quaternion.Euler(90,0,0));
                    foreach(float side in new[]{-1f,1f}) foreach(float z in new[]{-1.6f,1.6f})
                        Box(wagon,"Load_Stanchion",new Vector3(side,1.85f,z),new Vector3(.1f,1.7f,.12f),gold);
                }
                else
                {
                    Box(wagon,"Container",new Vector3(0,2.06f,0),new Vector3(2.12f,2.05f,3.95f),paint);
                    foreach(float side in new[]{-1f,1f})
                    {
                        for(int rib=0;rib<15;rib++) Box(wagon,"Corrugation",new Vector3(side*1.08f,2.06f,-1.83f+rib*.26f),new Vector3(.05f,1.91f,.055f),paint);
                        foreach(float z in new[]{-1.92f,1.92f}) Box(wagon,"Container_Corner",new Vector3(side*1.08f,2.06f,z),new Vector3(.09f,2.12f,.09f),metal);
                        Box(wagon,"Cargo_Plaque",new Vector3(side*1.12f,2.15f,0),new Vector3(.025f,.35f,.8f),ivory);
                    }
                    foreach(float x in new[]{-.65f,.65f}) Box(wagon,"Door_Locking_Bar",new Vector3(x,2.05f,-2),new Vector3(.05f,1.85f,.05f),metal);
                }
            }
            // The complete rigid consist moves as one transform. Material batching keeps mobile draw calls low.
            Combine(root);
            return root;
        }
        static void Underframe(Transform p,float length,Material frame,Material steel)
        {
            Box(p,"Deck",new Vector3(0,.98f,0),new Vector3(2.35f,.22f,length),frame);
            foreach(float end in new[]{-1f,1f})
            {
                Box(p,"Coupler",new Vector3(0,.83f,end*(length/2+.18f)),new Vector3(.28f,.24f,.48f),steel);
                Box(p,"Bogie",new Vector3(0,.6f,end*(length/2-.85f)),new Vector3(1.7f,.23f,1.1f),frame);
                foreach(float dz in new[]{-.38f,.38f}) foreach(float side in new[]{-1f,1f})
                {
                    var pos=new Vector3(side*.78f,.61f,end*(length/2-.85f)+dz);
                    Cylinder(p,"Wheel",pos,new Vector3(.76f,.12f,.76f),frame,Quaternion.Euler(0,0,90));
                    Cylinder(p,"Wheel_Hub",pos+Vector3.right*side*.13f,new Vector3(.33f,.035f,.33f),steel,Quaternion.Euler(0,0,90));
                }
            }
        }
        static Transform Group(Transform parent,string name,float z){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=Vector3.forward*z;return t;}
        static Material Mat(string name,Color color,float metallic){var m=new Material(ShaderHelper.GetLitShader()){name=name};ShaderHelper.BindOpaqueColorMaps(m,color);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.38f);return m;}
        static Transform Box(Transform p,string n,Vector3 v,Vector3 s,Material m){return Shape(p,n,v,s,m,PrimitiveType.Cube,Quaternion.identity);}
        static Transform Cylinder(Transform p,string n,Vector3 v,Vector3 s,Material m,Quaternion q){return Shape(p,n,v,s,m,PrimitiveType.Cylinder,q);}
        static Transform Shape(Transform p,string n,Vector3 v,Vector3 s,Material m,PrimitiveType type,Quaternion q)
        {var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=v;g.transform.localScale=s;g.transform.localRotation=q;g.GetComponent<Renderer>().sharedMaterial=m;var c=g.GetComponent<Collider>();if(c!=null){c.enabled=false;Object.Destroy(c);}return g.transform;}
        static void Combine(Transform root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>();var m=r.sharedMaterial;
                if(!groups.TryGetValue(m,out var list))groups[m]=list=new List<CombineInstance>();
                list.Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});
                f.gameObject.SetActive(false);Object.Destroy(f.gameObject);
            }
            foreach(var item in groups)
            {
                var g=new GameObject(item.Key.name);g.transform.SetParent(root,false);
                var mesh=new Mesh{name="FreightTrain_Detailed",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(item.Value.ToArray());
                g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=item.Key;
            }
        }
    }
}
