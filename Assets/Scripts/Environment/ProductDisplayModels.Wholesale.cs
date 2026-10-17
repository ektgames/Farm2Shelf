namespace Farm2Shelf.Environment
{
    internal static partial class ProductDisplayModels
    {
        private static bool DrawWholesale(Drawing a,string id)
        {
            switch(id)
            {
                case "p1": // Scored loaf, not a carton.
                    a.Ball(0,.25f,0,1.35f,.50f,.65f,Bread);
                    for(int i=-1;i<=1;i++)a.Box(i*.29f,.46f,0,.08f,.025f,.36f,Cream,-22);
                    break;
                case "p2":
                    a.Box(0,.55f,0,.65f,1.1f,.55f,White);a.Box(0,.43f,0,.66f,.42f,.56f,Blue);
                    a.Box(0,1.13f,0,.65f,.12f,.55f,Ice);a.Cylinder(.16f,1.25f,.08f,.10f,.12f,Blue);
                    Label(a,Blue,.44f,.43f,.29f);break;
                case "p3": // White strained cheese tub with green rim.
                    a.Box(0,.24f,0,.85f,.45f,.60f,White);a.Box(0,.48f,0,.90f,.08f,.65f,Green);
                    Label(a,Green,.52f,.24f,.31f);break;
                case "p4": Eggs(a,false);break;
                case "p5":
                    Packet(a,Blue,Gold,.48f,1.4f);
                    for(int i=-2;i<=2;i++)a.Box(i*.06f,.63f,.205f,.025f,.52f,.02f,Yellow);
                    break;
                case "p6": Packet(a,Red,Cream);GrainWindow(a,White);break;
                case "p7": Packet(a,Paper,Cream,.78f,1.05f);Wheat(a,Gold);break;
                case "p8": Packet(a,White,Sky,.75f,1f);a.Box(-.1f,.45f,.21f,.17f,.17f,.025f,White);a.Box(.10f,.32f,.21f,.17f,.17f,.025f,White);break;
                case "p9": Bottle(a,Yellow,Green,Red);SunflowerBadge(a,.28f);break;
                case "p10":
                    Jar(a,Olive,Black,Green,.65f);
                    for(int i=-1;i<=1;i++)a.Ball(i*.14f,.53f,.25f,.13f,.17f,.08f,Black);break;
                case "p11": // Water jug with carry handle.
                    Bottle(a,Sky,Blue,Blue,1.05f,.80f);
                    a.Box(-.15f,1.08f,0,.04f,.18f,.05f,Blue);a.Box(.15f,1.08f,0,.04f,.18f,.05f,Blue);a.Box(0,1.18f,0,.34f,.045f,.05f,Blue);
                    for(int i=0;i<3;i++)a.Cylinder(0,.16f+i*.14f,0,.408f,.025f,Ice);break;
                case "p12":
                    a.Ring(0,.14f,0,.37f,.13f,Bread);
                    for(int i=0;i<16;i++){float t=i*UnityEngine.Mathf.PI/8;a.Ball(UnityEngine.Mathf.Cos(t)*.37f,.26f,UnityEngine.Mathf.Sin(t)*.37f,.055f,.025f,.035f,Cream);}break;
                case "p13":
                    a.Ball(0,.22f,0,.90f,.43f,.62f,Bread);a.Box(0,.38f,.16f,.55f,.04f,.08f,Cream,-10);
                    for(int i=-2;i<=2;i++)a.Ball(i*.1f,.42f,0,.035f,.02f,.06f,Dark);break;
                case "p14": Packet(a,Navy,Paper,.80f,1.0f);GrainWindow(a,Cream);break;
                case "p15": Packet(a,Red,Orange,.80f,.95f);GrainWindow(a,Orange,12);break;
                case "p16": // Exact ID prevents tomato-paste being drawn as a tomato.
                    a.Cylinder(0,.45f,0,.34f,.85f,Red);a.Cylinder(0,.88f,0,.35f,.045f,Silver);a.Cylinder(0,.025f,0,.35f,.045f,Silver);
                    Label(a,Red,.48f,.47f,.35f);a.Ball(0,.49f,.37f,.16f,.13f,.02f,Red);break;
                case "p17":
                    a.Cylinder(0,.52f,0,.31f,1f,White);a.Cylinder(0,1.04f,0,.33f,.08f,Blue);Label(a,Blue,.45f,.55f,.32f);
                    for(int i=-1;i<=1;i++)a.Ball(i*.10f,1.085f,0,.04f,.012f,.04f,Navy);break;
                case "p18": Packet(a,Red,Dark,.75f,1.1f);a.LeafShape(0,.5f,.22f,.22f,.3f,Gold,25);break;
                case "p19": Packet(a,Cocoa,Gold,.66f,.8f);a.Ball(0,.36f,.22f,.28f,.12f,.035f,Cocoa);a.Box(0,.45f,.22f,.18f,.05f,.02f,Cream);break;
                case "p20":
                    a.Box(0,.55f,0,.63f,1.1f,.53f,Orange);a.Box(0,1.14f,0,.63f,.09f,.53f,Cream);a.Cylinder(.15f,1.23f,.08f,.09f,.10f,White);
                    Label(a,Orange,.46f,.54f,.28f);a.Ball(0,.55f,.30f,.25f,.25f,.04f,Peach);a.LeafShape(.08f,.71f,.30f,.10f,.12f,Green,40);break;
                case "p21": a.Box(0,.2f,0,1,.38f,.62f,Gold);a.Box(0,.205f,0,.48f,.40f,.64f,Cream);Label(a,Blue,.40f,.22f,.33f);break;
                case "p22": a.Box(0,.20f,0,.95f,.38f,.6f,Yellow);a.Box(0,.21f,0,.34f,.40f,.62f,Red);break;
                case "p23": Packet(a,Blue,Yellow,.85f,.95f);for(int i=-2;i<=2;i++)a.Box(i*.09f,.45f,.21f,.05f,.29f,.03f,Cream,i*7);break;
                case "p24":
                    a.Box(0,.08f,0,1.10f,.16f,1.1f,Red);a.Cylinder(0,.17f,0,.45f,.025f,Bread);a.Cylinder(0,.19f,0,.40f,.02f,Yellow);
                    for(int i=0;i<6;i++){float t=i*UnityEngine.Mathf.PI/3;a.Cylinder(UnityEngine.Mathf.Cos(t)*.25f,.21f,UnityEngine.Mathf.Sin(t)*.25f,.065f,.02f,Red);}break;
                case "p25":
                    a.Box(0,.25f,0,.95f,.5f,.65f,Blue);a.Box(0,.52f,0,1,.07f,.70f,White);
                    a.Ball(-.19f,.57f,0,.27f,.06f,.27f,Cream);a.Ball(.12f,.57f,0,.27f,.06f,.27f,Pink);Label(a,Blue,.55f,.26f,.335f);break;
                case "p26": FishTray(a,Blue,Salmon);break;
                case "p27":
                    a.Ring(0,.13f,0,.32f,.12f,Bread,15,150);a.Ball(0,.18f,.28f,.48f,.29f,.24f,Bread);
                    for(int i=-1;i<=1;i++)a.Box(i*.12f,.31f,.28f,.035f,.02f,.15f,Gold);break;
                case "p28": // Triangular slice with cream layers.
                    Cake(a);break;
                case "p29": Bottle(a,Lilac,Purple,Purple,1.25f,.65f);break;
                case "p30": Bottle(a,Olive,Cream,Black,.95f,.70f);a.Box(0,1.05f,0,.10f,.15f,.10f,Black);a.Box(.10f,1.14f,0,.32f,.06f,.10f,Black);break;
                case "p31":
                    a.Box(0,.60f,0,.48f,1.05f,.22f,White);a.Box(0,.53f,.12f,.49f,.30f,.02f,Teal);a.Box(0,1.13f,0,.51f,.05f,.25f,Silver);a.Cylinder(0,.05f,0,.15f,.10f,Blue);break;
                case "p32": Bottle(a,Brown,Orange,Orange,1.25f,.60f);a.Ball(0,.49f,.32f,.20f,.18f,.03f,Peach);break;
                case "p33":
                    a.Cylinder(0,.60f,0,.25f,1.15f,Navy);a.Cylinder(0,1.19f,0,.26f,.04f,Silver);a.Cylinder(0,.025f,0,.26f,.04f,Silver);
                    a.Box(0,.70f,.255f,.10f,.40f,.02f,Yellow,-25);a.Box(.04f,.49f,.255f,.10f,.25f,.02f,Yellow,-25);a.Ring(0,1.22f,0,.065f,.018f,Dark);break;
                case "p34":
                    for(int i=0;i<6;i++){float x=(i%3-1)*.30f,z=i/3==0?-.17f:.17f;a.Cylinder(x,.34f,z,.13f,.65f,Green);a.Cylinder(x,.72f,z,.07f,.16f,Green);a.Cylinder(x,.82f,z,.08f,.05f,Silver);}
                    a.Box(0,.32f,0,.91f,.24f,.61f,Sky);Label(a,Blue,.60f,.32f,.32f);break;
                case "p35":
                    a.Box(0,.08f,0,.65f,.15f,1.15f,Cocoa);a.Box(0,.09f,-.22f,.68f,.18f,.72f,Purple);
                    for(int i=0;i<6;i++)a.Box((i%2==0?-.16f:.16f),.18f,.20f+(i/2)*.13f,.25f,.05f,.10f,Cocoa);break;
                case "p36": Packet(a,Orange,Gold,.82f,1.05f);ChipsBadge(a,Yellow);break;
                case "p37":
                    a.Box(0,.18f,0,1,.34f,.52f,Blue);
                    for(int i=-1;i<=1;i++){a.Ball(i*.29f,.37f,0,.25f,.09f,.32f,Bread);a.Ball(i*.29f,.40f,0,.22f,.035f,.28f,Cream);}break;
                case "p38": Bottle(a,Green,Cream,Gold,1.5f,.48f);a.LeafShape(0,.55f,.26f,.16f,.22f,Olive,30);break;
                case "p39": Packet(a,Wine,Paper,.75f,.90f);for(int i=-1;i<=1;i++)a.Ring(i*.15f,.44f,.24f,.055f,.025f,Cream,30,250);break;
                case "p40": Packet(a,Green,Paper,.75f,.90f);for(int i=-1;i<=1;i++){a.Ball(i*.15f,.44f,.22f,.12f,.20f,.05f,Cream);a.Ball(i*.15f,.44f,.25f,.055f,.13f,.025f,Olive);}break;
                case "p41":
                    Tray(a);for(int i=0;i<16;i++)a.Box((i%8-3.5f)*.10f,.17f+(i/8)*.045f,0,.065f,.045f,.49f,Meat);TraySticker(a,Red);break;
                case "p42":
                    Tray(a);for(int i=0;i<9;i++)a.Box((i%3-1)*.26f,.20f,(i/3-1)*.17f,.22f,.16f,.14f,Meat,(i%2)*12);TraySticker(a,Red);break;
                case "p43":
                    Tray(a);a.Ball(-.21f,.22f,0,.40f,.25f,.49f,Peach);a.Ball(.21f,.22f,0,.40f,.25f,.49f,Peach);TraySticker(a,Green);break;
                case "p44": Tray(a);a.Ring(0,.24f,0,.23f,.10f,Meat,15,330);a.Box(.27f,.25f,0,.06f,.08f,.08f,Cream);TraySticker(a,Red);break;
                case "p45":
                    Tray(a);a.Ball(0,.23f,0,.79f,.24f,.50f,Meat);a.Ring(.13f,.35f,0,.11f,.028f,Cream);a.Box(-.16f,.35f,0,.028f,.015f,.34f,Pink,24);TraySticker(a,Gold);break;
                case "p46":
                    a.Box(0,.18f,0,.88f,.33f,.54f,White);a.Box(0,.36f,-.22f,.88f,.39f,.08f,White);
                    for(int i=-1;i<=1;i+=2){a.Ball(i*.21f,.37f,.05f,.25f,.20f,.23f,White);a.Box(i*.21f,.26f,.15f,.09f,.20f,.09f,Silver);}break;
                case "p47":
                    a.Box(0,.12f,0,.72f,.24f,1.1f,Navy);a.Box(0,.25f,-.20f,.27f,.012f,.17f,Black);
                    for(int i=-1;i<=1;i++)a.Box(i*.06f,.26f,-.20f,.02f,.015f,.055f,Sky);a.Box(0,.12f,.56f,.17f,.08f,.02f,Black);break;
                case "p48":
                    a.Ring(0,.08f,0,.29f,.04f,White);a.Ring(0,.08f,0,.20f,.04f,White);
                    a.Box(-.28f,.10f,.25f,.13f,.09f,.19f,Silver);a.Box(.28f,.10f,-.25f,.10f,.08f,.18f,Silver);break;
                case "p49":
                    a.Ball(0,.23f,0,.67f,.44f,1,Steel);a.Box(0,.435f,.19f,.018f,.015f,.30f,Black);a.Ball(0,.44f,.15f,.06f,.05f,.16f,Black);break;
                case "p50":
                    a.Box(0,.06f,0,.36f,.12f,1.25f,Black);a.Box(0,.15f,0,.61f,.16f,.63f,Steel);a.Box(0,.24f,0,.50f,.025f,.52f,Navy);
                    a.Ring(0,.26f,0,.15f,.025f,Teal);a.Box(0,.28f,.05f,.02f,.01f,.13f,White);break;
                case "p51": Jar(a,White,Teal,Ice,.48f);break;
                case "p52":
                    a.Box(0,.42f,0,.72f,.80f,.43f,Gold);a.Box(0,.43f,.23f,.40f,.31f,.02f,Cream);a.Cylinder(0,.88f,0,.11f,.12f,Gold);a.Box(0,1.03f,0,.34f,.22f,.31f,Black);break;
                case "p53": Bottle(a,Brown,Cream,Black,.78f,.45f);a.Cylinder(0,.90f,0,.09f,.22f,Black);break;
                case "p54":
                    Tray(a,Blue);for(int i=-1;i<=1;i++){a.Box(i*.27f,.23f,0,.23f,.20f,.48f,Bread);for(int j=0;j<3;j++)a.Box(i*.27f,.16f+j*.05f,.25f,.23f,.018f,.018f,Cream);}TraySticker(a,Blue);break;
                case "p55":
                    Tray(a);for(int i=0;i<6;i++)a.Ball((i%3-1)*.27f,.21f,(i/3==0?-.14f:.14f),.21f,.20f,.24f,Brown);TraySticker(a,Red);break;
                case "p56":
                    Tray(a);for(int i=-1;i<=1;i++){a.Ball(i*.26f,.22f,-.05f,.23f,.22f,.31f,Meat);a.Box(i*.26f,.20f,.19f,.045f,.07f,.30f,Cream);}TraySticker(a,Gold);break;
                case "p57": FishTray(a,Black,Salmon);a.Box(-.33f,.32f,0,.15f,.04f,.58f,Gold);break;
                case "p58":
                    a.Box(0,.21f,0,.75f,.40f,.52f,Yellow);a.Box(0,.04f,0,.77f,.08f,.54f,Bread);
                    for(int i=0;i<3;i++)a.Ball((i-1)*.21f,.23f,.266f,.08f,.08f,.015f,Gold);break;
                case "p59":
                    a.Box(0,.10f,-.10f,.43f,.20f,.72f,Red);a.Box(0,.10f,.40f,.36f,.15f,.30f,Silver);
                    a.Box(-.09f,.18f,.40f,.065f,.01f,.09f,Black);a.Box(.09f,.18f,.40f,.065f,.01f,.09f,Black);break;
                case "p60":
                    a.Box(0,.35f,0,1,.65f,.42f,Black);a.Box(0,.36f,.22f,.89f,.52f,.025f,Steel);
                    for(int x=-3;x<=3;x++)for(int y=0;y<4;y++)a.Ball(x*.11f,.18f+y*.12f,.24f,.038f,.038f,.015f,Black);
                    a.Box(0,.70f,0,.22f,.025f,.06f,Teal);break;
                case "livestock_egg": Eggs(a,true);break;
                case "livestock_milk": Bottle(a,White,Green,Green,1.2f,.62f);break;
                default:return false;
            }
            return true;
        }
        private static void Wheat(Drawing a,int color)
        {
            a.Box(0,.48f,.21f,.02f,.35f,.015f,color);
            for(int i=0;i<3;i++){a.LeafShape(-.06f,.38f+i*.09f,.22f,.08f,.13f,color,40);a.LeafShape(.06f,.38f+i*.09f,.22f,.08f,.13f,color,-40);}
        }
        private static void SunflowerBadge(Drawing a,float z)
        {
            for(int i=0;i<6;i++){float t=i*UnityEngine.Mathf.PI/3;a.Ball(UnityEngine.Mathf.Cos(t)*.09f,.52f+UnityEngine.Mathf.Sin(t)*.09f,z,.09f,.09f,.025f,Yellow);}
            a.Ball(0,.52f,z+.02f,.10f,.10f,.025f,Brown);
        }
        private static void ChipsBadge(Drawing a,int color)
        {for(int i=-1;i<=1;i++)a.Ball(i*.12f,.42f+i*.04f,.22f,.17f,.24f,.025f,color);}
        private static void TraySticker(Drawing a,int color)
        {a.Box(.30f,.35f,.18f,.25f,.018f,.21f,Cream);a.Box(.30f,.365f,.18f,.19f,.01f,.045f,color);}
        private static void FishTray(Drawing a,int tray,int flesh)
        {
            Tray(a,tray);a.Ball(0,.21f,0,.83f,.21f,.48f,flesh);
            for(int i=-2;i<=2;i++)a.Box(i*.13f,.315f,0,.02f,.012f,.32f,Cream);
            TraySticker(a,tray);
        }
        private static void Cake(Drawing a)
        {
            // Narrowing layers form a real wedge silhouette, with frosting and a cherry.
            for(int i=0;i<6;i++)
            {
                float z=-.35f+i*.13f,w=.75f-i*.115f;
                a.Box(0,.23f,z,w,.44f,.135f,Cocoa);
                a.Box(0,.17f,z,w+.005f,.06f,.135f,Cream);a.Box(0,.34f,z,w+.005f,.045f,.135f,Cream);
            }
            a.Ball(0,.51f,-.20f,.20f,.18f,.20f,Red);a.Box(0,.64f,-.20f,.025f,.12f,.025f,Green,-18);
        }
    }
}
