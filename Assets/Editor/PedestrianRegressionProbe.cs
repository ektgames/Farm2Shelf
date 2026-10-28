#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;
using Object=UnityEngine.Object;

// Explicit batch-only tests; never opens or saves the player's game.
public static class PedestrianRegressionProbe
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static int passed,failed;
    static readonly List<string> report=new List<string>();
    static GameObject root;
    static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Flags).Invoke(obj,args);
    static void Check(bool ok,string label){report.Add((ok?"PASS ":"FAIL ")+label);if(ok)passed++;else failed++;}
    static void Reset()
    {
        foreach(var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(g);
        typeof(PedestrianNavigation).GetMethod("Reset",Flags).Invoke(null,null);
        typeof(PedestrianNavigator).GetMethod("Reset",Flags).Invoke(null,null);
        root=new GameObject("Farm2Shelf_Environment");
    }
    static void Box(string name,Vector3 center,Vector3 size,bool furniture=false)
    {
        var g=new GameObject(name);g.transform.SetParent(root.transform);g.transform.position=center;
        var c=g.AddComponent<BoxCollider>();c.size=size;c.isTrigger=furniture;
        if(furniture){var n=g.AddComponent<UnityEngine.AI.NavMeshObstacle>();n.size=size;}
        Physics.SyncTransforms();PedestrianNavigation.Invalidate();
    }
    static GameObject Actor(Vector3 position)
    {
        var a=new GameObject("TestWalker");a.transform.position=position;
        var n=a.AddComponent<PedestrianNavigator>();Call(n,"OnEnable");return a;
    }
    static Vector3 Step(GameObject a,Vector3 goal,float speed=.08f)
    {
        typeof(PedestrianNavigator).GetField("planningFrame",Flags).SetValue(null,-1);
        var next=PedestrianNavigation.Move(a,goal,speed);a.transform.position=next;return next;
    }
    static void Walk(GameObject a,Vector3 goal,string label)
    {
        bool clear=true;
        for(int i=0;i<1200 && Vector3.Distance(a.transform.position,goal)>.08f;i++)
        {
            var from=a.transform.position;var next=Step(a,goal);
            clear &= PedestrianNavigation.Clear(from,next);
        }
        Check(clear && Vector3.Distance(a.transform.position,goal)<.09f,label);
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch project.");
        try
        {
            Reset();Box("Staff_3Seater_Sofa",new Vector3(0,.5f,0),new Vector3(2.5f,1,1),true);
            Check(PedestrianNavigation.IsBlocked(Vector3.zero),"Trigger furniture registered as solid navigation obstacle");
            Walk(Actor(new Vector3(-3,.05f,0)),new Vector3(3,.05f,0),"Walk around sofa without crossing it");
            Reset();Box("Counter_Wall",new Vector3(0,.5f,0),new Vector3(2.45f,1,1.1f));
            Walk(Actor(new Vector3(0,.05f,-2)),new Vector3(0,.05f,2),"Walk around checkout to staff side");
            Reset();Box("Shelf_Wall",new Vector3(0,.5f,0),new Vector3(2,1,3));
            var blocked=Actor(new Vector3(-3,.05f,0));var before=blocked.transform.position;
            for(int i=0;i<20;i++)Step(blocked,new Vector3(0,.05f,0));
            Check(blocked.transform.position==before,"Blocked target cannot pull walker through shelf");
            Reset();Box("Door_Wall_Left",new Vector3(-2,.5f,0),new Vector3(2,1,.3f));Box("Door_Wall_Right",new Vector3(2,.5f,0),new Vector3(2,1,.3f));
            Walk(Actor(new Vector3(0,.05f,-3)),new Vector3(0,.05f,3),"Walk through open doorway");
            Reset();var one=Actor(new Vector3(-2,.05f,0));var two=Actor(new Vector3(2,.05f,0));bool separated=true;
            for(int i=0;i<400;i++){Step(one,new Vector3(2,.05f,0));Step(two,new Vector3(-2,.05f,0));separated &= Vector3.Distance(one.transform.position,two.transform.position)>=.39f;}
            Check(separated && one.transform.position.x>1.9f && two.transform.position.x< -1.9f,"Opposing walkers pass without overlap or deadlock");
            Furniture();
            for(int level=1;level<=3;level++) Seats(level);
        }
        catch(Exception e){failed++;report.Add("ERROR "+e);}
        report.Add($"TOTAL passed={passed} failed={failed}");
        File.WriteAllLines("pedestrian-results.txt",report);
        Debug.Log(string.Join("\n",report));EditorApplication.Exit(failed==0?0:1);
    }
    static void Furniture()
    {
        Reset();
        var manager=new GameObject("Placement").AddComponent<FurniturePlacementManager>();manager.enabled=false;Call(manager,"Awake");
        foreach(var type in new[]{FurnitureType.Shelf,FurnitureType.ProduceShelf,FurnitureType.StorageShelf,FurnitureType.Cashier,FurnitureType.Fridge,FurnitureType.Freezer,FurnitureType.GourmetShelf,FurnitureType.MassageChair})
            foreach(float yaw in new[]{0f,90f,180f,270f})
            {
                var f=new GameObject("Furniture").AddComponent<PlacedFurnitureController>();
                typeof(PlacedFurnitureController).GetProperty("FurnitureType").SetValue(f,type);
                f.transform.rotation=Quaternion.Euler(0,yaw,0);Call(f,"OnEnable");PedestrianNavigation.Invalidate();
                var front=f.GetFrontInteractionPosition();front.y=.05f;
                Check(!PedestrianNavigation.IsBlocked(front),$"{type} {yaw}: front interaction outside footprint");
                var a=Actor(new Vector3(-4,.05f,-4));Walk(a,front,$"{type} {yaw}: walk to interaction side");
                Object.DestroyImmediate(a);Object.DestroyImmediate(f);PedestrianNavigation.Invalidate();
            }
        Check(manager.IsFrontOrDoorwayBlocked(new Vector3(-5,0,-2),0,FurnitureType.Shelf),"Door blocking shelf rejected");
        Check(!manager.IsFrontOrDoorwayBlocked(new Vector3(-5,0,-2),0,FurnitureType.WelcomeMat),"Walkable doormat allowed at entrance");
    }
    static void Seats(int level)
    {
        Reset();
        var env=new GameObject("EnvironmentManager").AddComponent<EnvironmentBuilder>();env.enabled=false;Call(env,"Awake");
        typeof(EnvironmentBuilder).GetField("currentUpgradeLevel",Flags).SetValue(env,level);
        float back=-3+(level==1?18:level==2?27:36);
        float sofaX=level==1?7:5;
        Box("Staff_3Seater_Sofa",new Vector3(sofaX,.5f,back-.5f),new Vector3(2.5f,1,1),true);
        Box("Back_Wall",new Vector3(7,1,back),new Vector3(8,2,.3f));
        float start=-3+(level==1?9.5f:level==2?14.5f:19.5f);
        Box("Left_Wall",new Vector3(3,1,(back+start)/2),new Vector3(.3f,2,back-start));
        Box("Right_Wall",new Vector3(11,1,(back+start)/2),new Vector3(.3f,2,back-start));
        Box("BreakTable_Set",new Vector3(4.8f,.5f,start+(level==3?3.8f:2.5f)),level==3?new Vector3(2.4f,.9f,1.8f):new Vector3(1.8f,.9f,1.6f),true);
        if(level>1)
        {
            Box("CoffeeTable",new Vector3(5,.5f,back-2.8f),new Vector3(1.5f,.5f,.8f),true);
            Box("Staff_2Seater_Sofa",new Vector3(5,.5f,back-5.1f),new Vector3(1.9f,1,1),true);
        }
        var staff=new GameObject("StaffManager").AddComponent<StaffTaskController>();staff.enabled=false;Call(staff,"Awake");
        var list=(List<StaffTaskController.StaffTaskData>)typeof(StaffTaskController).GetField("staffTaskList",Flags).GetValue(staff);
        var data=new StaffTaskController.StaffTaskData{staffObj=Actor(new Vector3(7,.05f,back-4)),staffMember=new StaffMember("test", "Test", StaffRole.Kasiyer, "08:00 - 16:00", 100, false, false),currentState=StaffTaskController.StaffAIState.ProceedingToTask};list.Add(data);
        Vector3 approach=(Vector3)Call(staff,"GetBreakRoomTargetPosition",data);
        var slots=(System.Collections.IList)typeof(StaffTaskController).GetField("activeSofaSeats",Flags).GetValue(staff);
        for(int i=0;i<slots.Count;i++)
        {
            Vector3 access=(Vector3)typeof(StaffTaskController).GetMethod("GetSeatApproach",Flags).Invoke(null,new[]{slots[i]});
            var route=PedestrianNavigation.Route(new Vector3(7,.05f,start+.6f),access);
            Check(route!=null&&!PedestrianNavigation.IsBlocked(access),$"Level {level}: seat {i} accessible from entrance");
        }
        Call(staff,"CleanupStaleSofaOccupants");
        Check(data.assignedSofaSeatIndex==0,$"Level {level}: reservation survives walking to seat");
        bool standingClear=true;
        for(int i=0;i<60;i++)
        {
            Vector3 spot=(Vector3)Call(staff,"GetBreakRoomStandingPosition",i);
            standingClear &= !PedestrianNavigation.IsBlocked(spot)&&spot.z<back&&spot.z>start;
        }
        Check(standingClear,$"Level {level}: 60 staff waiting destinations stay inside room and outside furniture");
        Check(Mathf.Abs(approach.y-.05f)<.001f&&!PedestrianNavigation.IsBlocked(approach),$"Level {level}: seat walking target on clear floor");
        data.waypoints=StaffTaskController.BuildStructuredStaffWaypoints(data.staffObj.transform.position,approach);
        bool completed=false;
        for(int i=0;i<1200&&!completed;i++)
        {
            typeof(PedestrianNavigator).GetField("planningFrame",Flags).SetValue(null,-1);
            Call(staff,"FollowWaypoints",data,.025f,(Action)(()=>completed=true));
        }
        Check(completed,$"Level {level}: real staff waypoint callback completes at seat approach");
        Walk(data.staffObj,approach,$"Level {level}: reach sofa approach");
        data.currentState=StaffTaskController.StaffAIState.WaitingInBreakRoom;
        Call(staff,"ExecuteBreakRoomRestAndSeating",data,.02f);
        Check(data.isSitting,$"Level {level}: sitting transition completes");
        var worker=new StaffTaskController.StaffTaskData {staffObj=Actor(new Vector3(9,.05f,back-4)),currentState=StaffTaskController.StaffAIState.WorkingOnTask};
        list.Add(worker);
        Call(staff,"FreeSofaSeat",data);
        Check(worker.assignedSofaSeatIndex<0,$"Level {level}: freed seat is not assigned to working staff");
        Check(!data.isSitting && Mathf.Abs(data.staffObj.transform.position.y-.05f)<.001f,$"Level {level}: standing restores floor height");
        Vector3 goal=new Vector3(7,.05f,back-4);
        for(int i=0;i<300;i++)Step(data.staffObj,goal);
        Check(Vector3.Distance(data.staffObj.transform.position,goal)<.1f,$"Level {level}: leaves sofa and reaches next task");
    }
}
#endif
