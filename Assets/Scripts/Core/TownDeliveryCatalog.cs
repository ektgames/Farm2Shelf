using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    // One address book for online customers and contracts. Stable IDs survive saves.
    public static class TownDeliveryCatalog
    {
        public static TownContractPartner[] Complete(TownContractPartner[] seed)
        {
            var result=new List<TownContractPartner>();
            foreach(var p in seed) if(!p.id.StartsWith("se_lot_")) result.Add(p);
            TownContractPartner Put(string id,string tr,string en,float x,float z,TownContractKind kind=TownContractKind.Residential)
            {
                var p=result.Find(v=>v.id==id);
                if(p==null)
                {
                    p=new TownContractPartner {id=id,nameTr=tr,nameEn=en,iconEmoji="📍",kind=kind,
                        dailyPremium=100,dailyQty=3,
                        preferredShelves=new[]{FurnitureType.Shelf,FurnitureType.Fridge,FurnitureType.ProduceShelf}};
                    result.Add(p);
                }
                p.doorstepPos=new Vector3(x,.05f,z);
                return p;
            }
            // Every physical apartment parcel, including both sides of the same avenue.
            float[] avenues={-37.5f,0,0,37.5f};
            for(int col=0;col<4;col++) for(int row=0;row<4;row++)
            {
                string id=col==0&&row==0?"apt_a":col==1&&row==0?"apt_b":
                    col==1&&row==1?"apt_c":col==3&&row==0?"apt_d":"north_apartment_"+col+"_"+row;
                Put(id,"Kuzey Apartmanı "+(col*4+row+1),"North Apartment "+(col*4+row+1),
                    avenues[col]+(col<2?-3.5f:3.5f),68+row*30);
            }
            string[] ne={"e1","e2","f1","f2","g1","g2","h1","h2"};
            for(int i=0;i<ne.Length;i++)
                Put("ne_apt_"+ne[i],"Kuzeydoğu Apartmanı "+(i+1),"Northeast Apartment "+(i+1),i<4?109:116,68+i%4*30);
            for(int col=0;col<3;col++) for(int row=0;row<4;row++)
            {
                int n=col*4+row+1;
                Put("villa_"+n,"Batı Sahil Villası #"+n,"West Coast Villa #"+n,-191.5f+col*38,22+row*43);
            }
            float[] houses={-58,-32,32,58};
            for(int col=0;col<4;col++) for(int row=0;row<2;row++)
            {
                int n=col*2+row+1;
                Put("house_"+n,"Kasaba Konutları No:"+n,"Town House #"+n,houses[col],row==0?-22.5f:-51.5f);
            }
            // Delivery stops face the road, rather than building centres or former plots.
            void Move(string id,float x,float z) { var p=result.Find(v=>v.id==id); if(p!=null)p.doorstepPos=new Vector3(x,.05f,z); }
            Move("school",-191.5f,-37); Move("hospital",-153.5f,-37); Move("bank",-115.5f,-37);
            Move("library",-191.5f,-90);
            Move("fire",-153.5f,-69); Move("police",-153.5f,-89); Move("gas",-153.5f,-110);
            Move("arcade",-115.5f,-71); Move("stadium",-115.5f,-102);
            Move("ne_taxi",93.5f,46.5f);
            Move("cafe_botanic",71.5f,-69); Move("cafe_books",71.5f,-91.5f); Move("cafe_farm",71.5f,-114);
            Move("mosque",-3.5f,-91.5f); Move("mosque_court",-3.5f,-62);
            Put("bus_stop","Otobüs Durağı","Bus Stop",4.5f,-5.8f,TownContractKind.Civic);
            Put("railway_station","Tren İstasyonu","Railway Station",153.5f,23,TownContractKind.Civic);
            Put("farmhouse","Çiftlik Evi","Farmhouse",25,46.5f);
            Put("cow_barn","İnek Ahırı","Cow Barn",71.5f,33.6f);
            Put("chicken_coop","Tavuk Kümesi","Chicken Coop",71.5f,10.8f);
            foreach(var address in result)address.doorstepPos=CourierRoadNetwork.RoadsideStop(address.doorstepPos);
            return result.ToArray();
        }
        public static string CanonicalId(string id)
        {
            // Old empty plots were replaced by the Garden Quarter houses.
            switch(id)
            {
                case "se_lot_a": return "garden_neighborhood_0_1";
                case "se_lot_b": return "garden_neighborhood_0_2";
                case "se_lot_c": return "garden_neighborhood_0_3";
                case "se_lot_d": return "garden_neighborhood_1_1";
                case "se_lot_e": return "garden_neighborhood_1_2";
                default:return id;
            }
        }
        public static TownContractPartner Resolve(string id,string nameTr,Vector3 oldPosition)
        {
            var partner=TownContractManager.GetPartner(CanonicalId(id));
            if(partner!=null)return partner;
            foreach(var p in TownContractManager.Catalog) if(p.nameTr==nameTr)return p;
            // Older saves stored only a display name and coordinates.
            float best=float.PositiveInfinity;
            foreach(var p in TownContractManager.Catalog)
            {
                float d=(p.doorstepPos-oldPosition).sqrMagnitude;
                if(d<best){best=d;partner=p;}
            }
            return partner;
        }
        public static (string nameTr,string nameEn,Vector3 doorstepPos)[] OnlineDestinations()
        {
            var all=TownContractManager.Catalog;
            var result=new (string,string,Vector3)[all.Length];
            for(int i=0;i<all.Length;i++)result[i]=(all[i].nameTr,all[i].nameEn,all[i].doorstepPos);
            return result;
        }
    }
}
