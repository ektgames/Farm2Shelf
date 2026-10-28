using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    [DefaultExecutionOrder(100)]
    public sealed class DailyFreightTrainController : MonoBehaviour
    {
        const float Approach = 720f, Arrival = RailFreightSales.ArrivalMinute, Departure = 780f, End = RailFreightSales.ExitMinute;
        Transform train;
        RailwayCrossingController crossing;
        int lastDay = -1;
        float previousMinute = -1;
        public void Configure(RailwayCrossingController value) { crossing=value; }
        void LateUpdate()
        {
            var clock=TimeManager.Instance;
            if(clock==null || crossing==null)return;
            int day=(clock.Year-1)*120+(int)clock.CurrentSeason*30+clock.Day;
            bool north=day%2==1;
            float minute=clock.MinuteOfDayPrecise;
            bool restored=day!=lastDay || previousMinute<0 || minute<previousMinute || minute-previousMinute>2f;
            bool active=minute>=Approach && minute<End;
            if(active && train==null) train=FreightTrainModelBuilder.Build(transform);
            if(train!=null)
            {
                train.gameObject.SetActive(active);
                train.localRotation=Quaternion.Euler(0,north?0:180,0);
                train.localPosition=new Vector3(north?RailwayStationBuilder.NorthboundTrackX:RailwayStationBuilder.SouthboundTrackX,0,Position(minute,north));
            }
            RailFreightSales.Advance(day, minute);
            float current=Position(minute,north),future=Position(minute+10,north);
            // Reserve ten game minutes before the front arrives. Release only after the last wagon clears.
            float low=Mathf.Min(current,future)-(north?FreightTrainModelBuilder.RearExtent:FreightTrainModelBuilder.FrontExtent);
            float high=Mathf.Max(current,future)+(north?FreightTrainModelBuilder.FrontExtent:FreightTrainModelBuilder.RearExtent);
            bool crossingNeeded=minute>=Approach-10 && minute<End && low< -2 && high> -16;
            if(crossingNeeded) crossing.Reserve(this); else crossing.Release(this);
            if(restored) crossing.SynchronizePose();
            lastDay=day;previousMinute=minute;
        }
        static float Position(float minute,bool north)
        {
            float start=north?-155f:200f,stop=north?40f:6f,end=north?235f:-190f;
            if(minute<Arrival) return Mathf.Lerp(start,stop,Mathf.SmoothStep(0,1,Mathf.InverseLerp(Approach,Arrival,minute)));
            if(minute<Departure)return stop;
            return Mathf.Lerp(stop,end,Mathf.SmoothStep(0,1,Mathf.InverseLerp(Departure,End,minute)));
        }
        void OnDisable(){if(crossing!=null)crossing.Release(this);if(train!=null)train.gameObject.SetActive(false);previousMinute=-1;}
    }
}
