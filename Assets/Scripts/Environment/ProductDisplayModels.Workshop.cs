namespace Farm2Shelf.Environment
{
    internal static partial class ProductDisplayModels
    {
        private static bool DrawWorkshop(Drawing a,string id)
        {
            switch(id)
            {
                case "gourmet_strawberry_jam":
                    Jar(a,Red,Gold,Cream);FruitSeal(a,Red,.35f);break;
                case "gourmet_gh_strawberry_marmalade":
                    Jar(a,Wine,Gold,Pink,.72f);FruitSeal(a,Red,.35f);a.Box(0,.81f,0,.14f,.025f,.65f,Cream);break;
                case "gourmet_pumpkin_dessert":
                    Jar(a,Orange,Brown,Cream,.72f);a.Ball(0,.32f,.37f,.24f,.17f,.025f,Orange);a.Box(0,.44f,.37f,.025f,.08f,.02f,Olive);break;
                case "gourmet_melon_marmalade":
                    Jar(a,Gold,Green,Cream,.78f);a.Ball(0,.34f,.37f,.24f,.20f,.025f,Gold);break;
                case "gourmet_watermelon_jam":
                    Jar(a,Peach,Red,Mint,.84f);a.Box(0,.36f,.37f,.22f,.09f,.02f,Green);a.Box(0,.42f,.37f,.22f,.06f,.02f,Pink);break;
                case "gourmet_grape_molasses":
                    Bottle(a,Cocoa,Paper,Gold,1.05f,.68f);GrapeSeal(a,.36f,.40f);break;
                case "gourmet_carrot_juice":
                    Bottle(a,Orange,Cream,Green,1.25f,.58f);CarrotSeal(a,.31f);break;
                case "gourmet_winter_carrot_juice":
                    Bottle(a,Orange,Ice,Blue,1.30f,.52f);CarrotSeal(a,.28f);break;
                case "gourmet_autumn_carrot_nectar":
                    Bottle(a,Peach,Paper,Brown,1.12f,.65f);CarrotSeal(a,.35f);break;
                case "gourmet_beet_detox":
                    Bottle(a,Wine,Cream,Black,1.25f,.56f);a.Ball(0,.48f,.30f,.18f,.18f,.025f,Wine);a.LeafShape(0,.61f,.30f,.10f,.16f,Leaf,15);break;
                case "gourmet_watermelon_coldjuice":
                    Bottle(a,Pink,Mint,Green,1.08f,.68f);a.Box(0,.40f,.36f,.23f,.06f,.025f,Green);a.Box(0,.46f,.36f,.23f,.06f,.025f,Red);break;
                case "gourmet_melon_smoothie":
                    Bottle(a,Gold,Cream,Green,1.02f,.72f);a.Ball(0,.40f,.38f,.20f,.17f,.025f,Gold);break;
                case "gourmet_grape_juice":
                    Bottle(a,Purple,Cream,Gold,1.34f,.54f);GrapeSeal(a,.29f,.49f);break;
                case "gourmet_turnip_juice":
                    Bottle(a,Wine,White,Red,1.40f,.50f);a.Ball(0,.52f,.27f,.20f,.16f,.025f,Purple);break;
                case "gourmet_tomato_paste":
                    Jar(a,Red,Gold,Paper,.92f);a.Ball(0,.40f,.37f,.22f,.18f,.025f,Red);a.LeafShape(0,.52f,.37f,.12f,.08f,Green);break;
                case "gourmet_pepper_paste":
                    Jar(a,Meat,Red,Cream,.86f);a.LeafShape(0,.37f,.37f,.12f,.24f,Red,-25);break;
                case "gourmet_cucumber_pickle":
                    Jar(a,Olive,Gold,Green,1.05f);JarContents(a,Leaf,0);break;
                case "gourmet_garlic_puree":
                    Jar(a,Cream,Green,Paper,.60f);a.Ball(0,.26f,.37f,.18f,.18f,.025f,White);break;
                case "gourmet_black_garlic_paste":
                    Jar(a,Dark,Gold,Black,.65f);a.Ball(0,.28f,.37f,.18f,.18f,.025f,Paper);break;
                case "gourmet_onion_sauce":
                    Jar(a,Brown,Gold,Cream,.75f);a.Ball(0,.32f,.37f,.22f,.18f,.025f,Gold);break;
                case "gourmet_roasted_eggplant":
                    Jar(a,Paper,Purple,Cream,.88f);a.Ball(0,.37f,.37f,.11f,.24f,.025f,Purple);break;
                case "gourmet_artichoke_preserve":
                    Jar(a,Sand,Gold,Olive,1f);JarContents(a,Cream,1);break;
                case "gourmet_sweet_peas":
                    Jar(a,Leaf,Gold,Cream,.95f);JarContents(a,Lime,2);break;
                case "gourmet_green_beans":
                    Jar(a,Olive,Red,Cream,1.06f);JarContents(a,Leaf,0);break;
                case "gourmet_okra_preserve":
                    Jar(a,Leaf,Gold,Paper,.97f);JarContents(a,Olive,0);break;
                case "gourmet_brussels_pickle":
                    Jar(a,Lime,Green,Cream,1.10f);JarContents(a,Leaf,1);break;
                case "gourmet_potato_chips":
                    Packet(a,Paper,Gold,.86f,.95f);ChipsBadge(a,Yellow);a.Box(0,.80f,.21f,.5f,.04f,.02f,Brown);break;
                case "gourmet_popcorn_snack":
                    Packet(a,Red,Cream,.80f,1.1f);for(int i=0;i<7;i++)a.Ball((i%3-1)*.12f,.37f+(i/3)*.10f,.22f,.15f,.12f,.03f,Cream);break;
                case "gourmet_roasted_sunflower":
                    Packet(a,Navy,Paper,.72f,1.05f);for(int i=0;i<8;i++)a.Ball((i%4-1.5f)*.10f,.39f+(i/4)*.15f,.22f,.045f,.11f,.025f,Dark);break;
                case "gourmet_asparagus_bites":
                    Packet(a,Paper,Green,.72f,.98f);for(int i=-1;i<=1;i++)a.Box(i*.12f,.43f,.22f,.055f,.28f,.02f,Olive,i*10);break;
                case "gourmet_zucchini_chips":
                    Packet(a,Green,Cream,.83f,.96f);ChipsBadge(a,Lime);break;
                case "gourmet_broccoli_bites":
                    Packet(a,Paper,Green,.85f,1.05f);for(int i=-1;i<=1;i++)a.Ball(i*.13f,.44f,.22f,.16f,.16f,.03f,Leaf);break;
                case "gourmet_cauliflower_bites":
                    Packet(a,Orange,Cream,.85f,1.02f);for(int i=-1;i<=1;i++)a.Ball(i*.13f,.44f,.22f,.16f,.16f,.03f,Cream);break;
                case "gourmet_sunflower_oil":
                    Bottle(a,Gold,Paper,Black,1.5f,.48f);SunflowerBadge(a,.26f);break;
                case "gourmet_pumpkin_seed_oil":
                    Bottle(a,Olive,Black,Gold,1.3f,.56f);a.Ball(0,.49f,.30f,.19f,.15f,.025f,Orange);break;
                case "gourmet_garlic_infused_oil":
                    Bottle(a,Yellow,Cream,Green,1.45f,.47f);a.Ball(0,.55f,.25f,.16f,.16f,.025f,White);break;
                case "gourmet_chili_infused_oil":
                    Bottle(a,Orange,Black,Red,1.5f,.45f);a.LeafShape(0,.57f,.245f,.10f,.24f,Red,-25);break;
                case "gourmet_med_salad":
                    SaladTray(a,Leaf,0);a.Ball(-.2f,.26f,.1f,.16f,.13f,.16f,Red);a.Ball(.2f,.26f,-.1f,.16f,.13f,.16f,Red);break;
                case "gourmet_baby_spinach": SaladTray(a,Green,0);break;
                case "gourmet_chard_rolls":
                    Tray(a,Cream);for(int i=-2;i<=2;i++)a.Ball(i*.16f,.21f,0,.13f,.15f,.46f,Olive);TraySticker(a,Green);break;
                case "gourmet_wild_arugula": SaladTray(a,Green,1);break;
                case "gourmet_fresh_cress": SaladTray(a,Leaf,2);break;
                case "gourmet_sauerkraut":
                    Jar(a,Cream,Gold,Green,.90f);for(int i=-2;i<=2;i++)a.Box(i*.07f,.68f,.31f,.026f,.20f,.018f,Sand,i*7);break;
                case "gourmet_red_cabbage_slaw":
                    SaladTray(a,Purple,1);for(int i=-2;i<=2;i++)a.Box(i*.12f,.28f,0,.02f,.015f,.35f,Lilac);break;
                case "gourmet_braised_leeks":
                    Tray(a,Cream);for(int i=0;i<6;i++){float x=(i%3-1)*.26f,z=i/3==0?-.15f:.15f;a.Cylinder(x,.21f,z,.10f,.17f,Mint);a.Cylinder(x,.30f,z,.07f,.015f,Cream);}TraySticker(a,Green);break;
                case "gourmet_pink_radish_pickle":
                    Tray(a,White);for(int i=0;i<6;i++){float x=(i%3-1)*.25f,z=i/3==0?-.14f:.14f;a.Cylinder(x,.18f,z,.13f,.10f,Pink);a.Cylinder(x,.235f,z,.10f,.012f,Cream);}TraySticker(a,Pink);break;
                case "gourmet_winter_radish_slaw":
                    SaladTray(a,Cream,1);for(int i=-2;i<=2;i++)a.Box(i*.14f,.29f,0,.035f,.02f,.36f,White);break;
                default:return false;
            }
            return true;
        }
        private static void FruitSeal(Drawing a,int color,float z)
        {a.Ball(0,.36f,z+.02f,.18f,.21f,.025f,color);a.LeafShape(0,.49f,z+.02f,.13f,.08f,Leaf);}
        private static void CarrotSeal(Drawing a,float z)
        {a.LeafShape(0,.46f,z,.14f,.25f,Orange,-20);a.LeafShape(.05f,.62f,z,.10f,.10f,Green,20);}
        private static void GrapeSeal(Drawing a,float z,float y)
        {for(int i=0;i<5;i++)a.Ball((i%2==0?-.04f:.04f),y+i*.035f,z,.08f,.08f,.025f,Purple);}
        private static void JarContents(Drawing a,int color,int shape)
        {
            for(int i=-1;i<=1;i++)
                a.Ball(i*.15f,.72f,.25f,shape==0?.08f:.12f,shape==0?.25f:.12f,.08f,color);
        }
        private static void SaladTray(Drawing a,int color,int shape)
        {
            Tray(a,Ice);
            for(int i=0;i<8;i++)
            {
                float x=(i%4-1.5f)*.20f,z=i/4==0?-.14f:.14f;
                if(shape==1)a.Box(x,.22f,z,.12f,.04f,.20f,color,i*9);
                else a.Ball(x,.21f,z,.22f,shape==2?.10f:.17f,.24f,color);
            }
            TraySticker(a,Green);
        }
    }
}
