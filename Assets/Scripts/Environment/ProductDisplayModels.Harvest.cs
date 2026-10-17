using UnityEngine;

namespace Farm2Shelf.Environment
{
    internal static partial class ProductDisplayModels
    {
        private static bool DrawHarvest(Drawing a,string id)
        {
            switch(id)
            {
                case "spring_tomato":
                    a.Ball(0,.36f,0,.83f,.68f,.79f,Red);Crown(a,0,.70f,0,.38f,Green);break;
                case "spring_cucumber":
                    a.Ball(0,.16f,0,1.22f,.31f,.32f,Green);
                    for(int i=-2;i<=2;i++)a.Ball(i*.19f,.30f,.055f,.035f,.035f,.035f,Lime);
                    a.Box(.61f,.17f,0,.09f,.055f,.06f,Olive);break;
                case "spring_lettuce": LeafHead(a,Lime,Leaf,.85f,7);break;
                case "spring_strawberry": Strawberry(a,0,0,0,1,Red);break;
                case "spring_carrot": Carrot(a,Orange,1);break;
                case "spring_radish":
                    a.Ball(0,.26f,0,.56f,.52f,.52f,Pink);a.Cylinder(0,.05f,0,.035f,.20f,White,.12f);LeafSpray(a,.51f,Green,3,.42f);break;
                case "spring_spinach":
                    for(int i=-2;i<=2;i++){a.Box(i*.09f,.21f,0,.025f,.40f,.03f,Leaf,i*12);a.LeafShape(i*.10f,.55f,0,.30f,.60f,Green,i*18);}a.Box(0,.16f,.04f,.30f,.06f,.08f,Paper);break;
                case "spring_pea":
                    a.Ball(0,.13f,0,1.10f,.25f,.30f,Leaf);
                    for(int i=-2;i<=2;i++)a.Ball(i*.19f,.24f,0,.18f,.18f,.18f,Lime);break;
                case "spring_artichoke":
                    a.Cylinder(0,.17f,0,.09f,.34f,Leaf);a.Ball(0,.47f,0,.57f,.62f,.57f,Olive);
                    for(int i=0;i<7;i++){float t=i*Mathf.PI*2/7;a.LeafShape(Mathf.Cos(t)*.22f,.55f,Mathf.Sin(t)*.22f,.22f,.40f,Leaf,(i-3)*15);}break;
                case "spring_asparagus":
                    for(int i=-2;i<=2;i++){a.Cylinder(i*.11f,.45f,0,.045f,.85f,Leaf);a.Cylinder(i*.11f,.93f,0,.065f,.20f,Olive,0);}
                    a.Box(0,.36f,.06f,.57f,.07f,.03f,Paper);break;
                case "summer_pepper":
                    for(int i=0;i<3;i++){float t=i*Mathf.PI*2/3;a.Ball(Mathf.Cos(t)*.16f,.32f,Mathf.Sin(t)*.16f,.42f,.63f,.42f,Red);}
                    Crown(a,0,.65f,0,.30f,Green);break;
                case "summer_zucchini":
                    a.Ball(0,.20f,0,1.05f,.40f,.41f,Leaf);a.Box(.54f,.21f,0,.16f,.11f,.12f,Olive);
                    for(int i=-1;i<=1;i++)a.Box(0,.37f,i*.08f,.65f,.018f,.015f,Lime);break;
                case "summer_greenbean":
                    for(int i=-1;i<=1;i++){a.Ball(i*.10f,.1f+i*.015f,0,.10f,.12f,1.05f,Leaf);a.Box(i*.1f,.1f,.53f,.03f,.025f,.12f,Green);}break;
                case "summer_eggplant":
                    a.Ball(0,.28f,0,.57f,.55f,.92f,Purple);Crown(a,0,.34f,-.39f,.31f,Green);a.Box(0,.43f,-.46f,.07f,.17f,.07f,Green,-18);break;
                case "summer_corn":
                    a.Ball(0,.20f,0,.42f,.38f,1f,Yellow);
                    for(int x=-1;x<=1;x++)for(int z=-3;z<=3;z++)a.Ball(x*.10f,.37f,z*.12f,.085f,.075f,.095f,Gold);
                    a.LeafShape(-.21f,.26f,0,.15f,.88f,Leaf,-65);a.LeafShape(.21f,.26f,0,.15f,.88f,Green,65);break;
                case "summer_okra":
                    for(int i=-1;i<=1;i++){a.Cylinder(i*.16f,.40f,0,.085f,.74f,Leaf,.015f);a.Cylinder(i*.16f,.04f,0,.10f,.08f,Green);}break;
                case "summer_melon":
                    a.Ball(0,.39f,0,.85f,.77f,.85f,Gold);
                    for(int i=0;i<8;i++){float t=i*Mathf.PI/4;a.Ball(Mathf.Cos(t)*.28f,.39f,Mathf.Sin(t)*.28f,.12f,.66f,.12f,Sand);}a.Box(0,.83f,0,.06f,.12f,.06f,Olive);break;
                case "summer_watermelon":
                    a.Ball(0,.37f,0,1.13f,.73f,.79f,Green,Leaf);
                    break;
                case "summer_sunflower":
                    Tray(a,Paper);for(int i=0;i<15;i++)a.Ball((i%5-2)*.16f,.18f,(i/5-1)*.18f,.08f,.07f,.15f,Dark);break;
                case "summer_grape":
                    for(int row=0;row<4;row++)for(int j=0;j<4-row;j++)a.Ball((j-(3-row)*.5f)*.20f,.72f-row*.17f,((j+row)%2)*.11f,.24f,.24f,.24f,Purple);
                    a.Box(0,.91f,0,.045f,.18f,.045f,Brown,-20);a.LeafShape(.18f,.88f,0,.25f,.22f,Green,-30);break;
                case "autumn_potato":
                    a.Ball(0,.26f,0,.93f,.51f,.62f,Paper);for(int i=-1;i<=1;i++)a.Ball(i*.23f,.45f,.10f,.05f,.025f,.05f,Brown);break;
                case "autumn_onion":
                    a.Ball(0,.31f,0,.66f,.61f,.64f,Gold);a.Cylinder(0,.67f,0,.13f,.22f,Paper,.025f);a.Cylinder(0,.025f,0,.10f,.045f,Brown);break;
                case "autumn_turnip":
                    a.Ball(0,.29f,0,.63f,.56f,.60f,Cream);a.Ball(0,.45f,0,.55f,.25f,.53f,Purple);a.Cylinder(0,.02f,0,.025f,.16f,Cream,.10f);LeafSpray(a,.60f,Leaf,3,.33f);break;
                case "autumn_garlic": Garlic(a,Cream);break;
                case "autumn_beet":
                    a.Ball(0,.30f,0,.65f,.59f,.64f,Wine);a.Cylinder(0,.01f,0,.02f,.20f,Wine,.10f);LeafSpray(a,.59f,Green,3,.45f);break;
                case "autumn_cabbage": LeafHead(a,Mint,Leaf,.92f,6);break;
                case "autumn_wintercarrot": Carrot(a,Orange,1.15f);break;
                case "autumn_pumpkin":
                    a.Ball(0,.35f,0,.85f,.69f,.85f,Orange);
                    for(int i=0;i<8;i++){float t=i*Mathf.PI/4;a.Ball(Mathf.Cos(t)*.27f,.34f,Mathf.Sin(t)*.27f,.31f,.62f,.31f,Orange);}
                    a.Cylinder(0,.77f,0,.08f,.22f,Olive,.05f);break;
                case "autumn_broccoli": Florets(a,Green,Leaf);break;
                case "autumn_cauliflower": Florets(a,Cream,Leaf);break;
                case "winter_chard":
                    for(int i=-1;i<=1;i++){a.Box(i*.14f,.31f,0,.035f,.55f,.04f,Pink,i*15);a.LeafShape(i*.18f,.62f,0,.35f,.63f,Green,i*25);a.Box(i*.14f,.58f,.07f,.02f,.31f,.02f,Pink,i*15);}break;
                case "winter_arugula":
                    for(int i=-2;i<=2;i++){a.Box(i*.10f,.32f,0,.02f,.55f,.03f,Leaf,i*13);for(int j=0;j<3;j++)a.LeafShape(i*.12f,.40f+j*.13f,.03f,.27f,.20f,Green,i*18);}break;
                case "winter_cress":
                    for(int i=-2;i<=2;i++){a.Box(i*.10f,.29f,0,.018f,.50f,.025f,Leaf,i*12);for(int j=0;j<3;j++)a.Ball(i*.12f,.40f+j*.09f,0,.18f,.12f,.09f,Leaf);}a.Box(0,.15f,0,.40f,.05f,.08f,Paper);break;
                case "winter_cabbage": LeafHead(a,Purple,Lilac,.92f,6);break;
                case "winter_leek":
                    for(int i=-1;i<=1;i++){a.Cylinder(i*.14f,.30f,0,.06f,.60f,White);a.Cylinder(i*.14f,.65f,0,.055f,.24f,Mint);a.LeafShape(i*.17f,.95f,0,.12f,.55f,Leaf,i*13);}break;
                case "winter_radish":
                    a.Ball(0,.31f,0,.44f,.62f,.45f,White);a.Cylinder(0,.01f,0,.018f,.19f,White,.08f);LeafSpray(a,.61f,Green,4,.35f);break;
                case "winter_carrot": Carrot(a,Peach,1.1f);break;
                case "winter_greenhousestrawberry": Strawberry(a,-.16f,0,0,.80f,Red);Strawberry(a,.21f,0,.12f,.67f,Pink);break;
                case "winter_brusselssprout":
                    for(int i=0;i<4;i++){float x=i%2==0?-.19f:.19f,z=i/2==0?-.18f:.18f;a.Ball(x,.18f,z,.35f,.34f,.35f,Lime);a.LeafShape(x,.20f,z+.16f,.24f,.26f,Leaf,25);}break;
                case "winter_garlic": Garlic(a,White);break;
                default:return false;
            }
            return true;
        }
        private static void Crown(Drawing a,float x,float y,float z,float width,int color)
        {
            a.Cylinder(x,y+.06f,z,.028f,.15f,color);
            for(int i=0;i<5;i++){float t=i*2*Mathf.PI/5;a.Ball(x+Mathf.Cos(t)*width*.26f,y,z+Mathf.Sin(t)*width*.26f,width*.45f,.035f,width*.18f,color);}
        }
        private static void Strawberry(Drawing a,float x,float y,float z,float size,int color)
        {
            a.Ball(x,y+.29f*size,z,.53f*size,.57f*size,.48f*size,color);
            for(int i=0;i<8;i++){float t=i*Mathf.PI/4;a.Ball(x+Mathf.Cos(t)*.22f*size,y+(.30f+(i%2)*.11f)*size,z+Mathf.Sin(t)*.21f*size,.025f*size,.045f*size,.025f*size,Yellow);}
            Crown(a,x,y+.56f*size,z,.42f*size,Green);
        }
        private static void LeafSpray(Drawing a,float y,int color,int count,float height)
        {for(int i=0;i<count;i++)a.LeafShape((i-(count-1)*.5f)*.09f,y+height*.35f,0,.17f,height,color,(i-(count-1)*.5f)*25);}
        private static void Carrot(Drawing a,int color,float length)
        {
            a.Cylinder(0,.42f*length,0,.015f,.84f*length,color,.16f);
            for(int i=0;i<3;i++)a.Box(0,.37f+i*.15f,.10f,.11f,.018f,.02f,Bread);
            LeafSpray(a,.84f*length,Leaf,4,.35f);
        }
        private static void Garlic(Drawing a,int color)
        {
            for(int i=0;i<6;i++){float t=i*Mathf.PI/3;a.Ball(Mathf.Cos(t)*.15f,.24f,Mathf.Sin(t)*.15f,.25f,.44f,.25f,color);}
            a.Cylinder(0,.50f,0,.10f,.26f,Paper,.025f);
        }
        private static void LeafHead(Drawing a,int heart,int outer,float size,int leaves)
        {
            a.Ball(0,size*.39f,0,size*.75f,size*.75f,size*.75f,heart);
            for(int i=0;i<leaves;i++){float t=i*Mathf.PI*2/leaves;a.Ball(Mathf.Cos(t)*size*.25f,size*.30f,Mathf.Sin(t)*size*.25f,size*.42f,size*.55f,size*.33f,outer);}
            a.LeafShape(0,size*.55f,size*.38f,size*.36f,size*.45f,heart,15);
        }
        private static void Florets(Drawing a,int head,int stem)
        {
            a.Cylinder(0,.22f,0,.13f,.44f,stem,.18f);
            for(int i=0;i<7;i++){float t=i*Mathf.PI*2/7;a.Ball(Mathf.Cos(t)*.24f,.53f+(i%2)*.05f,Mathf.Sin(t)*.24f,.37f,.37f,.37f,head);}
            a.Ball(0,.64f,0,.40f,.34f,.40f,head);
        }
    }
}
