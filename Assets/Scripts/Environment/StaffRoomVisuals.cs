using System.Collections.Generic;
using UnityEngine;
namespace Farm2Shelf.Environment
{
 public static class StaffRoomVisuals
 {
  public static readonly List<Transform> Seats=new List<Transform>();
  public static int Revision {get;private set;}
  static Mesh rounded;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  static void Reset(){Seats.Clear();Revision++;rounded=null;}
  public static void BeginRoom(){Seats.Clear();Revision++;}
  public static void Seat(Transform parent,Vector3 surface,Vector3 facing)
  {
   var t=new GameObject("Seat_Surface_Anchor").transform;t.SetParent(parent,false);
   t.localPosition=surface;t.localRotation=Quaternion.LookRotation(facing);Seats.Add(t);
  }
  static void Part(Transform parent,string name,Vector3 p,Vector3 size,Material m)
  {
   var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
   g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;
   Object.Destroy(g.GetComponent<Collider>());
  }
  public static void Chair(Transform parent,Vector3 p,Material wood,Material frame)
  {
   var root=new GameObject("Dining_Chair").transform;root.SetParent(parent,false);root.localPosition=p;
   Vector3 toward=-p;toward.y=0;root.localRotation=Quaternion.LookRotation(toward.normalized);
   Part(root,"Upholstered_Seat",new Vector3(0,.43f,0),new Vector3(.48f,.10f,.48f),wood);
   Part(root,"Curved_Back",new Vector3(0,.73f,-.22f),new Vector3(.48f,.48f,.07f),wood);
   foreach(float x in new[]{-.18f,.18f})foreach(float z in new[]{-.18f,.18f})
    Part(root,"Chair_Leg",new Vector3(x,.2f,z),new Vector3(.045f,.4f,.045f),frame);
   Seat(root,new Vector3(0,.48f,0),Vector3.forward);
  }
  public static void TableLegs(Transform parent,float width,float depth,float height,Material material)
  {
   foreach(float x in new[]{-1f,1f})foreach(float z in new[]{-1f,1f})
    Part(parent,"Tapered_Table_Leg",new Vector3(x*(width*.5f-.13f),height*.5f,z*(depth*.5f-.13f)),new Vector3(.07f,height,.07f),material);
  }
  public static GameObject CoffeeTable(Transform parent,Vector3 position,Material material)
  {
   var g=new GameObject("Staff_Coffee_Table");g.transform.SetParent(parent,false);g.transform.position=position;
   Part(g.transform,"Solid_Wood_Top",new Vector3(0,.44f,0),new Vector3(1.4f,.08f,.75f),material);
   Part(g.transform,"Lower_Shelf",new Vector3(0,.17f,0),new Vector3(1.16f,.045f,.56f),material);
   TableLegs(g.transform,1.4f,.75f,.4f,material);return g;
  }
  public static void Finish(Transform room)
  {
   if(rounded==null)rounded=BuildRoundedBox();
   var materials=new HashSet<Material>();
   foreach(var filter in room.GetComponentsInChildren<MeshFilter>())
   {
    if(filter.sharedMesh!=null && filter.sharedMesh.name=="Cube")filter.sharedMesh=rounded;
    var renderer=filter.GetComponent<Renderer>();if(renderer!=null)materials.Add(renderer.sharedMaterial);
   }
   foreach(var m in materials)
   {
    if(m==null)continue;bool metal=m.name.Contains("Metal"),fabric=m.name.Contains("Fabric")||m.name.Contains("Cushion");
    if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",metal?.48f:fabric?.12f:.28f);
    if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal?.65f:0f);
    m.enableInstancing=true;
   }
  }
  static Mesh BuildRoundedBox()
  {
   var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
   float[] steps={-.5f,-.42f,.42f,.5f};
   foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
   {
    var u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up;var v=Vector3.Cross(normal,u);int start=vertices.Count;
    for(int y=0;y<4;y++)for(int x=0;x<4;x++)
    {
     var p=normal*.5f+u*steps[x]+v*steps[y];var core=new Vector3(Mathf.Clamp(p.x,-.42f,.42f),Mathf.Clamp(p.y,-.42f,.42f),Mathf.Clamp(p.z,-.42f,.42f));
     var n=(p-core).normalized;vertices.Add(core+n*.08f);normals.Add(n);uv.Add(new Vector2(steps[x]+.5f,steps[y]+.5f));
    }
    for(int y=0;y<3;y++)for(int x=0;x<3;x++){int i=start+y*4+x;tris.AddRange(new[]{i,i+1,i+4,i+1,i+5,i+4});}
   }
   var mesh=new Mesh{name="Staff_Furniture_Bevelled_Box"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();mesh.UploadMeshData(true);return mesh;
  }
 }
}
