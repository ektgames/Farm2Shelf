using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    public static partial class ProceduralCarModelBuilder
    {
        // Only CityTrafficManager and customer cars use this factory. Taxi, courier
        // and delivery factories deliberately remain independent.
        private enum BodyStyle { Sedan, Sport, Convertible, SUV, Minibus, Bus, Fire, Ambulance }
        private struct TrafficDesign
        {
            public BodyStyle style;
            public float width, length, radius, axleInset, wheelOffset, belt, roof;
            public Color paint;
        }
        private struct TrafficPart { public Mesh mesh; public Material material; }
        private static readonly Dictionary<VehicleType, TrafficPart[]> trafficMeshCache = new Dictionary<VehicleType, TrafficPart[]>();
        private static TrafficPart[] detailedWheel;

        private static TrafficDesign TrafficSpec(VehicleType type)
        {
            var d = new TrafficDesign { style = BodyStyle.Sedan, width = 1.55f, length = 3.7f,
                radius = .35f, axleInset = .9f, wheelOffset = .02f, belt = .70f, roof = 1.155f,
                paint = new Color(.64f, .045f, .06f) };
            switch (type)
            {
                case VehicleType.PoliceCar: d.length = 3.8f; d.paint = new Color(.06f,.08f,.11f); d.roof = 1.29f; break;
                case VehicleType.SedanBlue: d.paint = new Color(.055f,.19f,.48f); break;
                case VehicleType.SedanSilver: d.paint = new Color(.57f,.63f,.69f); break;
                case VehicleType.SUVBlack: case VehicleType.SUVWhite: case VehicleType.SUVOrange:
                    d.style = BodyStyle.SUV; d.width = 1.68f; d.length = 4f; d.radius = .38f;
                    d.axleInset = 1.1f; d.wheelOffset = .04f; d.belt = .88f; d.roof = 1.395f;
                    d.paint = type == VehicleType.SUVBlack ? new Color(.055f,.07f,.085f) :
                        type == VehicleType.SUVWhite ? new Color(.88f,.90f,.88f) : new Color(.87f,.29f,.045f); break;
                case VehicleType.CoupeSportYellow: case VehicleType.CoupeSportRed: case VehicleType.ConvertibleCyan:
                    d.style = type == VehicleType.ConvertibleCyan ? BodyStyle.Convertible : BodyStyle.Sport;
                    d.radius = .32f; d.belt = .60f; d.roof = .985f;
                    d.paint = type == VehicleType.CoupeSportYellow ? new Color(.95f,.66f,.035f) :
                        type == VehicleType.CoupeSportRed ? new Color(.73f,.035f,.055f) : new Color(.025f,.55f,.65f); break;
                case VehicleType.MinibusYellow: case VehicleType.MinibusDarkBlue:
                    d.style = BodyStyle.Minibus; d.width = 1.68f; d.length = 4.2f; d.radius = .38f;
                    d.belt = .91f; d.roof = 1.34f;
                    d.paint = type == VehicleType.MinibusYellow ? new Color(.94f,.63f,.055f) : new Color(.045f,.12f,.29f); break;
                case VehicleType.CityBus:
                    d.style = BodyStyle.Bus; d.width = 1.85f; d.length = 6.4f; d.radius = .45f;
                    d.axleInset = 1.1f; d.wheelOffset = .04f; d.belt = 1.02f; d.roof = 1.805f;
                    d.paint = new Color(.04f,.39f,.25f); break;
                case VehicleType.FireTruck:
                    d.style = BodyStyle.Fire; d.width = 1.85f; d.length = 5.4f; d.radius = .5f;
                    d.axleInset = 1.1f; d.wheelOffset = .04f; d.belt = 1.30f; d.roof = 1.80f;
                    d.paint = new Color(.69f,.035f,.025f); break;
                case VehicleType.Ambulance:
                    d.style = BodyStyle.Ambulance; d.width = 1.75f; d.length = 4.2f; d.radius = .38f;
                    d.belt = .96f; d.roof = 1.49f; d.paint = new Color(.91f,.93f,.90f); break;
            }
            return d;
        }

        private static GameObject CreateDetailedTrafficVehicle(VehicleType type, out List<Transform> wheels)
        {
            TrafficDesign d = TrafficSpec(type);
            if (!trafficMeshCache.TryGetValue(type, out var parts) || parts.Length == 0 || parts[0].mesh == null || parts[0].material == null)
            {
                parts = SculptTrafficBody(type, d);
                trafficMeshCache[type] = parts;
            }
            var root = new GameObject("Vehicle_" + type);
            AttachTrafficParts(root.transform, parts, "Coachwork");
            if (detailedWheel == null || detailedWheel[0].mesh == null || detailedWheel[0].material == null)
                detailedWheel = SculptTrafficWheel();
            wheels = new List<Transform>(4);
            float axle = d.length * .5f - d.axleInset;
            // Local X rotation is the existing traffic animation contract.
            foreach (float z in new[] { axle, -axle })
            foreach (float side in new[] { -1f, 1f })
            {
                var wheel = new GameObject("Wheel");
                wheel.transform.SetParent(root.transform, false);
                wheel.transform.localPosition = new Vector3(side * (d.width * .5f + d.wheelOffset), d.radius, z);
                wheel.transform.localScale = new Vector3(1f, d.radius, d.radius);
                AttachTrafficParts(wheel.transform, detailedWheel, "AlloyWheel");
                wheels.Add(wheel.transform);
            }
            float lightY = d.style == BodyStyle.Fire ? .65f : d.style == BodyStyle.Bus ? .6f :
                d.style == BodyStyle.Sport || d.style == BodyStyle.Convertible ? .30f : .45f;
            AddLightsAndBumpers(root.transform, d.width, d.length,
                TrafficMaterial("Lens", new Color(.88f,.96f,1f), .15f, .92f),
                TrafficMaterial("TailLens", new Color(.63f,.015f,.025f), .15f, .8f), lightY);
            // Preserve separate headlight renderers for the existing day/night controller.
            foreach (string name in new[] { "Headlight_L", "Headlight_R", "Taillight_L", "Taillight_R" })
            {
                Transform lens = root.transform.Find(name);
                if (lens != null) lens.localScale = new Vector3(.35f, .105f, .065f);
            }
            return root;
        }

        private static Material TrafficMaterial(string key, Color color, float metal = .0f, float gloss = .4f)
        {
            Material mat = GetMaterial("TrafficPremium_" + key, color, metal, gloss);
            mat.enableInstancing = true;
            return mat;
        }
        private static void AttachTrafficParts(Transform root, TrafficPart[] parts, string prefix)
        {
            foreach (var part in parts)
            {
                var go = new GameObject(prefix + "_" + part.material.name);
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = part.mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = part.material;
                renderer.receiveShadows = true;
            }
        }

        private static TrafficPart[] SculptTrafficBody(VehicleType type, TrafficDesign d)
        {
            var mesh = new TrafficSculpt();
            Material paint = TrafficMaterial(type.ToString(), d.paint, .48f, .72f);
            Material dark = TrafficMaterial("Graphite", new Color(.025f,.032f,.04f), .15f, .32f);
            Material glass = TrafficMaterial("SmokedGlass", new Color(.075f,.16f,.21f), .65f, .92f);
            Material metal = TrafficMaterial("BrushedAlloy", new Color(.57f,.65f,.69f), .82f, .65f);
            Material white = TrafficMaterial("Ivory", new Color(.88f,.90f,.87f), .2f, .55f);
            Material red = TrafficMaterial("SignalRed", new Color(.82f,.025f,.035f), .15f, .7f);
            float w = d.width * .5f, l = d.length * .5f, axle = l - d.axleInset;
            bool coach = d.style == BodyStyle.Bus || d.style == BodyStyle.Minibus;
            bool utility = d.style == BodyStyle.Fire || d.style == BodyStyle.Ambulance;
            mesh.Box(dark, new Vector3(0,.28f,0), new Vector3(d.width*.85f,.16f,d.length*.94f), .04f);
            // Sculpted shoulders, lower sill and genuine raised wheel openings.
            var sections = new SortedSet<float> { -l, -l+.12f, 0, l-.16f, l };
            foreach (float z in new[] { -axle, axle })
                for (int i=0;i<=8;i++) sections.Add(z + Mathf.Cos(i*Mathf.PI/8)* (d.radius+.035f));
            var rings = new List<Vector3[]>();
            foreach (float z in sections)
            {
                float end = Mathf.Clamp01((Mathf.Abs(z)/l-.72f)/.28f);
                float half = w*(1f-.085f*end);
                float top = d.belt - (coach || utility ? .025f : .10f)*end;
                float bottom=.29f;
                foreach(float az in new[]{ -axle, axle })
                {
                    float dz=Mathf.Abs(z-az), r=d.radius+.035f;
                    if(dz<r) bottom=Mathf.Max(bottom,d.radius+Mathf.Sqrt(r*r-dz*dz));
                }
                bottom=Mathf.Min(bottom,top-.045f);
                float lowerBevel = Mathf.Min(.025f, (top-bottom)*.20f);
                float shoulder = top-Mathf.Min(.08f, (top-bottom)*.35f);
                rings.Add(new[] { new Vector3(-half*.94f,bottom,z),new Vector3(-half,bottom+lowerBevel,z),
                    new Vector3(-half,shoulder,z),new Vector3(-half*.90f,top,z),
                    new Vector3(half*.90f,top,z),new Vector3(half,shoulder,z),
                    new Vector3(half,bottom+lowerBevel,z),new Vector3(half*.94f,bottom,z) });
            }
            mesh.Loft(paint,rings);
            if(coach) SculptCoach(mesh,d,paint,dark,glass,white,metal);
            else if(utility) SculptEmergency(mesh,d,paint,dark,glass,white,red,metal);
            else SculptPassengerCabin(mesh,d,paint,dark,glass,metal);

            // Recessed grille, bumper blades, lower intake, number plate and rear diffuser.
            mesh.Box(dark,new Vector3(0,.43f,l+.012f),new Vector3(d.width*.52f,.20f,.045f),.025f);
            for(int i=0;i<5;i++) mesh.Box(metal,new Vector3(0,.36f+i*.033f,l+.038f),new Vector3(d.width*.43f,.012f,.012f),.003f);
            mesh.Box(dark,new Vector3(0,.29f,l-.015f),new Vector3(d.width*.81f,.08f,.14f),.025f);
            mesh.Box(dark,new Vector3(0,.29f,-l+.01f),new Vector3(d.width*.80f,.10f,.15f),.025f);
            foreach(float end in new[]{-1f,1f})
            {
                mesh.Box(white,new Vector3(0,.38f,end*(l+.041f)),new Vector3(.31f,.085f,.012f),.008f);
                for(int i=0;i<5;i++) mesh.Box(dark,new Vector3(-.10f+i*.046f,.38f,end*(l+.049f)),new Vector3(.021f,.038f,.004f),.001f);
            }
            foreach(float side in new[]{-1f,1f})
            {
                mesh.Box(dark,new Vector3(side*w,.31f,0),new Vector3(.045f,.06f,Mathf.Max(.3f,2*axle-2*d.radius)),.016f);
                // Mirrors stay within the original wheel/rim envelope.
                float mirrorZ=coach||utility?l-.55f:.75f;
                mesh.Box(dark,new Vector3(side*(w+.025f),d.belt+.12f,mirrorZ),new Vector3(.12f,.035f,.045f),.01f);
                mesh.Box(paint,new Vector3(side*(w+.09f),d.belt+.14f,mirrorZ),new Vector3(.13f,.095f,.17f),.025f);
                mesh.Box(glass,new Vector3(side*(w+.09f),d.belt+.14f,mirrorZ-.088f),new Vector3(.10f,.063f,.008f),.007f);
                foreach(float z in new[]{-axle,axle}) mesh.Arch(dark,side*(w+.006f),d.radius,z,d.radius+.035f,d.radius+.068f);
            }
            if(type==VehicleType.PoliceCar)
            {
                foreach(float side in new[]{-1f,1f})
                {
                    mesh.Box(white,new Vector3(side*(w+.008f),.57f,0),new Vector3(.015f,.20f,1.36f),.015f);
                    mesh.Box(metal,new Vector3(side*(w+.02f),.59f,.25f),new Vector3(.012f,.105f,.09f),.025f);
                    for(int i=0;i<4;i++) mesh.Box(dark,new Vector3(side*(w+.02f),.58f,-.38f+i*.14f),new Vector3(.008f,.04f,.075f),.004f);
                }
                AddEmergencyBar(mesh,new Vector3(0,d.roof+.065f,-.10f),dark,red);
            }
            return mesh.Bake("Traffic_"+type);
        }

        private static void SculptPassengerCabin(TrafficSculpt m, TrafficDesign d, Material paint, Material dark, Material glass, Material metal)
        {
            float w=d.width*.5f, y=d.belt, roof=d.roof;
            bool suv=d.style==BodyStyle.SUV, sport=d.style==BodyStyle.Sport, open=d.style==BodyStyle.Convertible;
            float back=suv?-1.24f:-1.04f, topBack=suv?-1.04f:-.63f, topFront=sport?.30f:.40f, front=suv?1.08f:.98f;
            float tw=w*(suv?.87f:.80f), bw=w*.91f;
            if(!open)
            {
                // Dark seals behind inset panes keep every pillar visible at game scale.
                m.Cabin(dark,bw,tw,y-.005f,roof-.035f,back,topBack,topFront,front);
                m.Box(paint,new Vector3(0,roof-.018f,(topBack+topFront)*.5f),new Vector3(tw*2+.035f,.065f,topFront-topBack+.10f),.025f);
                foreach(float s in new[]{-1f,1f})
                {
                    m.Quad(glass,new Vector3(s*(bw+.006f),y+.045f,back+.11f),new Vector3(s*(tw+.008f),roof-.09f,topBack+.08f),
                        new Vector3(s*(tw+.008f),roof-.09f,-.14f),new Vector3(s*(bw+.006f),y+.045f,-.14f),Vector3.right*s);
                    m.Quad(glass,new Vector3(s*(bw+.006f),y+.045f,-.055f),new Vector3(s*(tw+.008f),roof-.09f,-.055f),
                        new Vector3(s*(tw+.008f),roof-.09f,topFront-.065f),new Vector3(s*(bw+.006f),y+.045f,front-.12f),Vector3.right*s);
                    m.Beam(metal,new Vector3(s*bw,y+.018f,back+.06f),new Vector3(s*bw,y+.018f,front-.07f),.018f);
                }
                m.Quad(glass,new Vector3(-bw+.055f,y+.035f,front+.007f),new Vector3(bw-.055f,y+.035f,front+.007f),
                    new Vector3(tw-.055f,roof-.09f,topFront+.055f),new Vector3(-tw+.055f,roof-.09f,topFront+.055f),Vector3.forward);
                m.Quad(glass,new Vector3(bw-.065f,y+.04f,back-.009f),new Vector3(-bw+.065f,y+.04f,back-.009f),
                    new Vector3(-tw+.065f,roof-.10f,topBack-.055f),new Vector3(tw-.065f,roof-.10f,topBack-.055f),Vector3.back);
            }
            else
            {
                Material leather=TrafficMaterial("CognacLeather",new Color(.32f,.13f,.06f),0,.32f);
                m.Box(dark,new Vector3(0,y+.012f,-.23f),new Vector3(1.22f,.045f,1.35f),.09f);
                foreach(float side in new[]{-1f,1f})
                {
                    m.Box(leather,new Vector3(side*.32f,y+.055f,-.22f),new Vector3(.43f,.08f,.43f),.055f);
                    m.Box(leather,new Vector3(side*.32f,y+.17f,-.43f),new Vector3(.43f,.29f,.10f),.04f);
                    m.Box(leather,new Vector3(side*.32f,y+.34f,-.43f),new Vector3(.22f,.10f,.085f),.03f);
                    m.Beam(metal,new Vector3(side*bw,y,.72f),new Vector3(side*tw,roof,.47f),.035f);
                }
                m.Quad(glass,new Vector3(-bw,y,.72f),new Vector3(bw,y,.72f),new Vector3(tw,roof,.47f),new Vector3(-tw,roof,.47f),Vector3.forward);
                m.Beam(metal,new Vector3(-tw,roof,.47f),new Vector3(tw,roof,.47f),.035f);
                m.Box(dark,new Vector3(0,y+.08f,.47f),new Vector3(1.22f,.14f,.18f),.035f);
            }
            foreach(float s in new[]{-1f,1f})
            {
                // Door shut lines and inset pull handles, separate from glass pillars.
                foreach(float z in new[]{back+.09f,-.10f,front-.10f}) m.Box(dark,new Vector3(s*(w+.002f),(y+.34f)*.5f,z),new Vector3(.009f,y-.34f,.012f),.002f);
                foreach(float z in new[]{-.55f,.30f}) m.Box(metal,new Vector3(s*(w+.012f),y-.09f,z),new Vector3(.018f,.024f,.13f),.008f);
            }
            if(suv)
            {
                foreach(float s in new[]{-1f,1f}) m.Box(metal,new Vector3(s*tw*.84f,roof+.045f,-.25f),new Vector3(.035f,.045f,1.64f),.015f);
            }
            if(sport)
            {
                m.Box(dark,new Vector3(0,.70f,-1.56f),new Vector3(1.28f,.045f,.19f),.018f);
                foreach(float s in new[]{-1f,1f}) m.Box(dark,new Vector3(s*.46f,.64f,-1.56f),new Vector3(.035f,.12f,.06f),.01f);
                foreach(float s in new[]{-1f,1f}) m.Box(dark,new Vector3(s*.43f,y-.035f,1.20f),new Vector3(.18f,.014f,.35f),.025f);
            }
        }

        private static void SculptCoach(TrafficSculpt m, TrafficDesign d, Material paint, Material dark, Material glass, Material white, Material metal)
        {
            float w=d.width*.5f,l=d.length*.5f,y=d.belt,h=d.roof;
            m.Box(d.style==BodyStyle.Bus?white:paint,new Vector3(0,(y+h)*.5f,0),new Vector3(d.width*.97f,h-y,d.length-.12f),.075f);
            int count=d.style==BodyStyle.Bus?7:4;
            for(int side=-1;side<=1;side+=2)
            {
                float span=d.length-.75f, step=span/count;
                for(int i=0;i<count;i++)
                {
                    float z=-span*.5f+step*(i+.5f);
                    m.Box(dark,new Vector3(side*w*.987f,(y+h)*.5f,z),new Vector3(.018f,h-y-.13f,step-.045f),.035f);
                    m.Box(glass,new Vector3(side*w*.999f,(y+h)*.5f+.015f,z),new Vector3(.009f,h-y-.20f,step-.10f),.025f);
                }
                m.Box(metal,new Vector3(side*w,y-.08f,0),new Vector3(.018f,.035f,d.length-.30f),.008f);
            }
            m.Box(dark,new Vector3(0,(y+h)*.5f,l-.042f),new Vector3(d.width-.14f,h-y-.10f,.025f),.04f);
            m.Box(glass,new Vector3(0,(y+h)*.5f,l-.024f),new Vector3(d.width-.23f,h-y-.20f,.014f),.03f);
            if(d.style==BodyStyle.Bus)
            {
                Material amber=TrafficMaterial("RouteDisplay",new Color(.95f,.56f,.07f),.1f,.3f);
                m.Box(dark,new Vector3(0,h-.105f,l-.013f),new Vector3(1.05f,.13f,.025f),.02f);
                for(int i=0;i<9;i++) m.Box(amber,new Vector3(-.39f+i*.095f,h-.105f,l+.003f),new Vector3(.052f,.055f,.008f),.008f);
                foreach(float z in new[]{l-.65f,-.55f})
                {
                    m.Box(dark,new Vector3(w+.008f,.87f,z),new Vector3(.02f,1.28f,.60f),.025f);
                    m.Box(glass,new Vector3(w+.023f,1.03f,z),new Vector3(.009f,.85f,.50f),.02f);
                    m.Box(metal,new Vector3(w+.03f,.88f,z),new Vector3(.008f,1.15f,.025f),.004f);
                }
                m.Box(white,new Vector3(0,h+.04f,-.65f),new Vector3(1.16f,.08f,1.42f),.035f);
            }
            // Two front wipers and rear ventilation slats.
            foreach(float s in new[]{-1f,1f}) m.Beam(dark,new Vector3(s*.18f,y+.07f,l-.006f),new Vector3(s*.49f,y+.30f,l-.006f),.017f);
            for(int i=0;i<5;i++) m.Box(dark,new Vector3(0,.58f+i*.055f,-l+.008f),new Vector3(.80f,.018f,.015f),.005f);
        }

        private static void SculptEmergency(TrafficSculpt m, TrafficDesign d, Material paint, Material dark, Material glass, Material white, Material red, Material metal)
        {
            float w=d.width*.5f,l=d.length*.5f;
            bool fire=d.style==BodyStyle.Fire;
            m.Box(paint,new Vector3(0,(d.belt+d.roof)*.5f,l-.77f),new Vector3(d.width*.96f,d.roof-d.belt,1.48f),.065f);
            m.Box(glass,new Vector3(0,d.roof-.27f,l-.025f),new Vector3(d.width-.20f,.36f,.025f),.045f);
            foreach(float s in new[]{-1f,1f})
            {
                m.Box(glass,new Vector3(s*w*.97f,d.roof-.27f,l-.67f),new Vector3(.016f,.35f,.94f),.04f);
                m.Box(metal,new Vector3(s*w,d.belt-.10f,l-1.12f),new Vector3(.018f,.03f,.15f),.009f);
            }
            float rearHeight=fire?1.50f:1.57f;
            float rearLength=d.length-1.6f;
            m.Box(paint,new Vector3(0,(d.belt+rearHeight)*.5f,-.78f),new Vector3(d.width*.97f,rearHeight-d.belt,rearLength),.055f);
            foreach(float s in new[]{-1f,1f})
            {
                m.Box(fire?white:red,new Vector3(s*(w+.009f),.96f,-.15f),new Vector3(.016f,.115f,d.length-.35f),.018f);
                if(fire)
                {
                    for(int bay=0;bay<3;bay++)
                    {
                        float z=-l+.53f+bay*.85f;
                        m.Box(dark,new Vector3(s*(w+.012f),1.17f,z),new Vector3(.015f,.54f,.75f),.025f);
                        for(int slat=0;slat<7;slat++) m.Box(metal,new Vector3(s*(w+.023f),.96f+slat*.065f,z),new Vector3(.012f,.047f,.67f),.007f);
                    }
                }
                else
                {
                    m.Box(red,new Vector3(s*(w+.02f),1.28f,-.70f),new Vector3(.02f,.35f,.105f),.009f);
                    m.Box(red,new Vector3(s*(w+.021f),1.28f,-.70f),new Vector3(.02f,.105f,.35f),.009f);
                }
            }
            if(fire)
            {
                foreach(float x in new[]{-.38f,.38f}) m.Box(metal,new Vector3(x,1.97f,-.60f),new Vector3(.055f,.085f,3.4f),.018f);
                for(int i=0;i<13;i++) m.Box(metal,new Vector3(0,1.97f,-2.15f+i*.26f),new Vector3(.78f,.035f,.045f),.009f);
                m.Box(dark,new Vector3(0,1.65f,-.55f),new Vector3(.60f,.13f,.85f),.03f);
            }
            else
            {
                foreach(float s in new[]{-1f,1f}) m.Box(glass,new Vector3(s*.38f,1.28f,-l+.006f),new Vector3(.48f,.29f,.02f),.025f);
                m.Box(dark,new Vector3(0,1.11f,-l+.022f),new Vector3(.012f,.75f,.01f),.002f);
            }
            AddEmergencyBar(m,new Vector3(0,d.roof+.065f,l-.77f),dark,red);
        }
        private static void AddEmergencyBar(TrafficSculpt m,Vector3 p,Material dark,Material red)
        {
            m.Box(dark,p,new Vector3(.96f,.045f,.22f),.018f);
            m.Box(red,p+new Vector3(-.28f,.055f,0),new Vector3(.34f,.085f,.20f),.025f);
            m.Box(TrafficMaterial("SignalBlue",new Color(.025f,.22f,.95f),.15f,.7f),p+new Vector3(.28f,.055f,0),new Vector3(.34f,.085f,.20f),.025f);
        }

        private static TrafficPart[] SculptTrafficWheel()
        {
            var m=new TrafficSculpt();
            Material rubber=TrafficMaterial("Tire",new Color(.035f,.039f,.043f),0,.23f);
            Material rim=TrafficMaterial("ForgedRim",new Color(.66f,.71f,.74f),.88f,.68f);
            Material hub=TrafficMaterial("BrakeDisc",new Color(.19f,.22f,.25f),.7f,.35f);
            // Axis X, unit radius: separate bevelled shoulder, tread and alloy lip.
            m.Revolve(rubber,new[]{new Vector2(-.14f,.69f),new Vector2(-.14f,.88f),new Vector2(-.10f,1),new Vector2(.10f,1),new Vector2(.14f,.88f),new Vector2(.14f,.69f)},24);
            foreach(float s in new[]{-1f,1f})
            {
                m.Revolve(rim,new[]{new Vector2(s*.143f,.54f),new Vector2(s*.15f,.66f),new Vector2(s*.13f,.72f)},24);
                m.Revolve(hub,new[]{new Vector2(s*.12f,0),new Vector2(s*.12f,.56f)},24);
                for(int i=0;i<5;i++)
                {
                    float a=i*Mathf.PI*2/5;
                    m.Beam(rim,new Vector3(s*.151f,Mathf.Cos(a)*.13f,Mathf.Sin(a)*.13f),new Vector3(s*.151f,Mathf.Cos(a+.18f)*.61f,Mathf.Sin(a+.18f)*.61f),.085f);
                }
                m.Revolve(rim,new[]{new Vector2(s*.16f,0),new Vector2(s*.16f,.17f)},16);
            }
            return m.Bake("Traffic_SharedAlloyWheel");
        }

        // Mesh authoring helpers: static details are merged by material, never given
        // colliders. Cached meshes/materials are reused for every car of that design.
        private sealed class TrafficSculpt
        {
            private sealed class Surface
            {
                public readonly List<Vector3> vertices=new List<Vector3>();
                public readonly List<int> indices=new List<int>();
            }
            private readonly Dictionary<Material,Surface> surfaces=new Dictionary<Material,Surface>();
            public void Quad(Material mat,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward)
            {
                if(!surfaces.TryGetValue(mat,out var s)) { s=new Surface(); surfaces.Add(mat,s); }
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),outward)<0) { var swap=b;b=d;d=swap; }
                int i=s.vertices.Count; s.vertices.Add(a);s.vertices.Add(b);s.vertices.Add(c);s.vertices.Add(d);
                s.indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            }
            public void Loft(Material mat,List<Vector3[]> rings)
            {
                for(int r=0;r<rings.Count-1;r++)
                    for(int i=0;i<rings[r].Length;i++)
                    {
                        int j=(i+1)%rings[r].Length;
                        Vector3 center=Vector3.zero; foreach(var v in rings[r]) center+=v; center/=rings[r].Length;
                        Quad(mat,rings[r][i],rings[r+1][i],rings[r+1][j],rings[r][j],(rings[r][i]+rings[r][j])*.5f-center);
                    }
                foreach(int r in new[]{0,rings.Count-1})
                {
                    Vector3 center=Vector3.zero;foreach(var v in rings[r]) center+=v;center/=rings[r].Length;
                    for(int i=0;i<rings[r].Length;i++) Quad(mat,center,rings[r][i],rings[r][(i+1)%rings[r].Length],center,r==0?Vector3.back:Vector3.forward);
                }
            }
            public void Box(Material mat,Vector3 center,Vector3 size,float bevel)
            {
                float x=size.x*.5f,y=size.y*.5f,z=size.z*.5f;
                float b=Mathf.Min(bevel,Mathf.Min(x,Mathf.Min(y,z))*.8f);
                var rings=new List<Vector3[]>();
                foreach(float zz in new[]{-z,-z+b,z-b,z})
                {
                    float inset=Mathf.Abs(zz)>=z-.00001f?b:0;
                    float xx=x-inset,yy=y-inset,c=Mathf.Min(b,Mathf.Min(xx,yy)*.5f);
                    rings.Add(new[]{center+new Vector3(-xx,-yy+c,zz),center+new Vector3(-xx,yy-c,zz),center+new Vector3(-xx+c,yy,zz),center+new Vector3(xx-c,yy,zz),
                        center+new Vector3(xx,yy-c,zz),center+new Vector3(xx,-yy+c,zz),center+new Vector3(xx-c,-yy,zz),center+new Vector3(-xx+c,-yy,zz)});
                }
                Loft(mat,rings);
            }
            public void Cabin(Material mat,float bw,float tw,float y,float h,float back,float topBack,float topFront,float front)
            {
                Quad(mat,new Vector3(-bw,y,back),new Vector3(-tw,h,topBack),new Vector3(-tw,h,topFront),new Vector3(-bw,y,front),Vector3.left);
                Quad(mat,new Vector3(bw,y,front),new Vector3(tw,h,topFront),new Vector3(tw,h,topBack),new Vector3(bw,y,back),Vector3.right);
                Quad(mat,new Vector3(-bw,y,front),new Vector3(-tw,h,topFront),new Vector3(tw,h,topFront),new Vector3(bw,y,front),Vector3.forward);
                Quad(mat,new Vector3(bw,y,back),new Vector3(tw,h,topBack),new Vector3(-tw,h,topBack),new Vector3(-bw,y,back),Vector3.back);
            }
            public void Beam(Material mat,Vector3 a,Vector3 b,float thickness)
            {
                Vector3 direction=(b-a).normalized;
                Vector3 u=Vector3.Cross(direction,Mathf.Abs(direction.y)>.9f?Vector3.forward:Vector3.up).normalized*thickness*.5f;
                Vector3 v=Vector3.Cross(direction,u).normalized*thickness*.5f;
                Quad(mat,a-u-v,b-u-v,b+u-v,a+u-v,-v);
                Quad(mat,a+u-v,b+u-v,b+u+v,a+u+v,u);
                Quad(mat,a+u+v,b+u+v,b-u+v,a-u+v,v);
                Quad(mat,a-u+v,b-u+v,b-u-v,a-u-v,-u);
                Quad(mat,a-u-v,a+u-v,a+u+v,a-u+v,-direction);
                Quad(mat,b-u-v,b+u-v,b+u+v,b-u+v,direction);
            }
            public void Arch(Material mat,float x,float y,float z,float inner,float outer)
            {
                for(int i=0;i<16;i++)
                {
                    float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
                    Quad(mat,new Vector3(x,y+Mathf.Sin(a)*inner,z+Mathf.Cos(a)*inner),new Vector3(x,y+Mathf.Sin(b)*inner,z+Mathf.Cos(b)*inner),
                        new Vector3(x,y+Mathf.Sin(b)*outer,z+Mathf.Cos(b)*outer),new Vector3(x,y+Mathf.Sin(a)*outer,z+Mathf.Cos(a)*outer),Vector3.right*Mathf.Sign(x));
                }
            }
            public void Revolve(Material mat,Vector2[] profile,int segments)
            {
                for(int p=0;p<profile.Length-1;p++) for(int i=0;i<segments;i++)
                {
                    float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                    Vector2 u=profile[p],v=profile[p+1];
                    Vector3 normal=new Vector3(Mathf.Abs(u.x-v.x)<.0001f?Mathf.Sign(u.x):0,Mathf.Cos((a+b)*.5f),Mathf.Sin((a+b)*.5f));
                    Quad(mat,new Vector3(u.x,Mathf.Cos(a)*u.y,Mathf.Sin(a)*u.y),new Vector3(v.x,Mathf.Cos(a)*v.y,Mathf.Sin(a)*v.y),
                        new Vector3(v.x,Mathf.Cos(b)*v.y,Mathf.Sin(b)*v.y),new Vector3(u.x,Mathf.Cos(b)*u.y,Mathf.Sin(b)*u.y),normal);
                }
            }
            public TrafficPart[] Bake(string name)
            {
                var parts=new List<TrafficPart>();
                foreach(var pair in surfaces)
                {
                    var mesh=new Mesh { name=name+"_"+pair.Key.name };
                    mesh.SetVertices(pair.Value.vertices);mesh.SetTriangles(pair.Value.indices,0);
                    mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.UploadMeshData(true);
                    parts.Add(new TrafficPart { mesh=mesh,material=pair.Key });
                }
                return parts.ToArray();
            }
        }
    }
}
