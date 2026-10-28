using UnityEngine;
namespace Farm2Shelf.Environment
{
    // One source for bay markings, reservation IDs, gates and pedestrian clearance.
    public static class CustomerParkingLayout
    {
        public const float EntryX=-23f, ExitX=-31f, AisleX=-27f;
        public const float FirstRowZ=6f, RowPitch=3.6f, BayDepth=3.4f;
        public static int Rows(int level)=>level<=1?3:level==2?5:8;
        public static Vector3 Slot(int side,int row)=>new Vector3(side==0?-36.4f:-17.6f,.05f,FirstRowZ+row*RowPitch);
        public static Vector3 Door(Vector3 slot)=>slot+new Vector3(0,0,slot.x<AisleX?-1.65f:1.65f);
        // Decorative geometry has no colliders: poles stay outside bays and swept gate paths.
        public static void CreateLamp(Transform parent,Vector3 position,Material metal,Material lens)
        {
            var root=new GameObject("Modern_Perimeter_Lamp").transform;root.SetParent(parent,false);root.localPosition=position;
            LampPart(root,"Base",new Vector3(0,.1f,0),new Vector3(.26f,.2f,.26f),metal);
            LampPart(root,"Post",new Vector3(0,1.9f,0),new Vector3(.12f,3.6f,.12f),metal);
            LampPart(root,"Head",new Vector3(0,3.7f,0),new Vector3(.85f,.12f,.5f),metal);
            var bulb=LampPart(root,"LED",new Vector3(0,3.62f,0),new Vector3(.7f,.035f,.4f),lens);
            var lightRoot=new GameObject("Downlight");lightRoot.transform.SetParent(root,false);lightRoot.transform.localPosition=new Vector3(0,3.55f,0);
            var light=lightRoot.AddComponent<Light>();light.type=LightType.Spot;light.range=10f;light.spotAngle=115f;
            light.intensity=1.8f;light.color=new Color(1f,.91f,.76f);light.shadows=LightShadows.None;
            lightRoot.transform.localRotation=Quaternion.Euler(90,0,0);
            if(DayNightCycleManager.Instance!=null)DayNightCycleManager.Instance.RegisterStreetLamp(bulb,light);
        }
        private static GameObject LampPart(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);return go;
        }
    }
}
