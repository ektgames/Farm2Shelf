using UnityEngine;
using UnityEngine.AI;

namespace Farm2Shelf.Environment
{
    public static partial class ProceduralWestDistrictBuilder
    {
        private static GameObject VillaDetail(Transform parent, string name, Vector3 p, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = p;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        private static void BuildVillaDriveway(Transform parent)
        {
            Material paving = GetMaterial("VillaDrivewayStone", new Color(.38f,.39f,.38f), 0,.25f);
            // The 3.2m approach passes south of the pool, through its own fence opening.
            VillaDetail(parent, "Driveway_To_Road", new Vector3(4.75f,.035f,-13.8f), new Vector3(22.5f,.05f,3.2f), paving);
            // Sidewalk top is .08m in this district. A shallow sloped apron meets it.
            var apron = VillaDetail(parent, "Driveway_Kerb_Apron", new Vector3(16.1f,.055f,-13.8f), new Vector3(.41f,.012f,3.2f), paving);
            apron.transform.localRotation = Quaternion.Euler(0,0,-8.5f);
            VillaDetail(parent, "Driveway_Sidewalk_Crossing", new Vector3(15.3f,.085f,-13.8f), new Vector3(1.2f,.012f,3.2f), paving);
        }

        private static void BuildRenewedVillaStructure(Transform parent, int id, Vector3 position,
            out Vector3 doorLocalPos, out int floorCount)
        {
            int style = (id - 1) % 6;
            bool alternate = id > 6;
            string[] styles = { "Terraced_Modern", "Mediterranean", "Nordic_Gable", "Twin_Pavilion", "Timber_Loft", "Garden_Penthouse" };
            var root = new GameObject("Villa_" + id + "_" + styles[style]).transform;
            root.SetParent(parent,false);
            root.localPosition = position;
            Color[] colors = { new Color(.91f,.9f,.85f), new Color(.88f,.77f,.60f), new Color(.69f,.75f,.77f),
                new Color(.94f,.93f,.88f), new Color(.35f,.30f,.25f), new Color(.78f,.80f,.77f) };
            Material wall = GetMaterial("RenewedVillaFacade_"+id, colors[style]*(alternate?.91f:1f),0,.3f);
            Material stone = GetMaterial("VillaLimestone",new Color(.67f,.65f,.59f),0,.35f);
            Material dark = GetMaterial("VillaRenewedTrim",new Color(.12f,.15f,.17f),.25f,.4f);
            Material wood = GetMaterial("VillaRenewedTimber",new Color(.49f,.29f,.15f),0,.35f);
            Material glass = GetMaterial("VillaRenewedGlazing",new Color(.13f,.30f,.36f),.3f,.85f);
            Material roof = GetMaterial("VillaRenewedRoof_"+style, style==1?new Color(.48f,.20f,.12f):new Color(.18f,.21f,.24f),0,.3f);
            const float w=8.5f,d=13f,h=3.2f;
            floorCount = style==5 && alternate ? 3 : 2;
            VillaDetail(root,"Stone_Foundation",new Vector3(0,.15f,0),new Vector3(w+.4f,.3f,d+.4f),stone);
            for(int floor=0;floor<floorCount;floor++)
            {
                float fw=w,fd=d,x=0,z=0;
                if(floor>0)
                {
                    if(style==0 || style==5) { fw=6.2f;fd=10f;x=-.9f;z=alternate?-1.1f:1.1f; }
                    if(style==3) { fw=7f;fd=9.6f;z=alternate?-1.4f:1.4f; }
                    if(style==4) { fw=7.5f;fd=11f;x=.3f; }
                    if(floor==2) { fw=5f;fd=9.6f;x=-1f;z=0; }
                }
                var level = new GameObject("Storey_"+floor).transform;
                level.SetParent(root,false);level.localPosition=new Vector3(x,floor*h,z);
                VillaDetail(level,"Facade",new Vector3(0,.3f+h*.5f,0),new Vector3(fw,h,fd),style==4?wood:wall);
                VillaDetail(level,"Cornice",new Vector3(0,.3f+h,0),new Vector3(fw+.3f,.16f,fd+.3f),style==1?stone:dark);
                BuildVillaDetailedWindows(level,fw,fd,1,h,glass,dark);
                if(style==4)
                    for(float zz=-fd*.5f;zz<=fd*.5f;zz+=.55f)
                        VillaDetail(level,"Timber_Batten",new Vector3(-fw*.5f-.03f,1.9f,zz),new Vector3(.08f,3.1f,.065f),dark);
            }
            float top=.3f+floorCount*h;
            if(style==1 || style==2)
                VillaPitchedRoof(root,new Vector3(0,top,0),w+.8f,d+.8f,alternate?32f:23f,roof);
            else if(style==3)
            {
                VillaPitchedRoof(root,new Vector3(0,top,-3.4f),w+.5f,6.5f,20f,roof);
                VillaPitchedRoof(root,new Vector3(0,top,3.4f),w+.5f,6.5f,20f,roof);
            }
            else
            {
                float rw=style==4?8f:6.6f,rd=style==4?11.5f:10.5f;
                VillaDetail(root,"Flat_Roof",new Vector3(style==4?.3f:-.9f,top+.08f,style==4?0:alternate?-1.1f:1.1f),new Vector3(rw,.18f,rd),roof);
                // Ground-floor roof terrace, with an actual supported balcony and railing.
                VillaDetail(root,"Terrace_Deck",new Vector3(3.5f,3.58f,0),new Vector3(2f,.14f,12f),stone);
                VillaDetail(root,"Terrace_Railing",new Vector3(4.45f,4.1f,0),new Vector3(.08f,1f,12f),glass);
                for(float zz=-5.5f;zz<=5.5f;zz+=2.75f)
                    VillaDetail(root,"Terrace_Post",new Vector3(4.45f,4.1f,zz),new Vector3(.08f,1.1f,.08f),dark);
            }
            if(style==1)
                foreach(float z in new[]{-5f,5f})
                    VillaDetail(root,"Entrance_Colonnade",new Vector3(4.7f,1.9f,z),new Vector3(.35f,3.4f,.35f),stone);
            doorLocalPos=new Vector3(w*.5f+.02f,.3f,0);
            BuildVillaEntranceDoor(root,doorLocalPos,dark,wood);
            var collider=root.gameObject.AddComponent<BoxCollider>();
            collider.center=new Vector3(0,top*.5f,0);collider.size=new Vector3(w+.4f,top,d+.4f);
            var obstacle=root.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=collider.center;obstacle.size=collider.size;
            obstacle.carving=true;obstacle.carveOnlyStationary=true;
        }

        private static void VillaPitchedRoof(Transform parent,Vector3 basePos,float width,float depth,float angle,Material mat)
        {
            float rise=Mathf.Tan(angle*Mathf.Deg2Rad)*width*.5f;
            // Close both gables rather than leaving hollow roof triangles.
            var gables = new GameObject("Roof_Gables");
            gables.transform.SetParent(parent, false);
            gables.transform.localPosition = basePos;
            var mesh = new Mesh { name = "Villa_Gable_Mesh" };
            mesh.vertices = new[] {
                new Vector3(-width*.5f,0,depth*.5f),new Vector3(width*.5f,0,depth*.5f),new Vector3(0,rise,depth*.5f),
                new Vector3(width*.5f,0,-depth*.5f),new Vector3(-width*.5f,0,-depth*.5f),new Vector3(0,rise,-depth*.5f) };
            mesh.triangles = new[] {0,1,2,3,4,5};
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            gables.AddComponent<MeshFilter>().sharedMesh=mesh;
            gables.AddComponent<MeshRenderer>().sharedMaterial=mat;
            foreach(float side in new[]{-1f,1f})
            {
                var slope=VillaDetail(parent,"Pitched_Roof",basePos+new Vector3(side*width*.25f,rise*.5f,0),
                    new Vector3(width*.5f/Mathf.Cos(angle*Mathf.Deg2Rad),.18f,depth),mat);
                slope.transform.localRotation=Quaternion.Euler(0,0,-side*angle);
            }
        }
    }
}
