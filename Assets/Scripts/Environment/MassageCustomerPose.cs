using System.Collections.Generic;
using Farm2Shelf.Utils;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    // Temporary seated visuals; the walking model and carried basket are restored verbatim.
    public sealed class MassageCustomerPose : MonoBehaviour
    {
        private readonly List<GameObject> hidden = new List<GameObject>();
        private GameObject seatedLimbs;
        private Transform basket;
        private Vector3 basketPosition, basketScale, standingPosition, seatedPosition;
        private Quaternion basketRotation, standingRotation, seatedRotation;
        private Vector3 basketWorldPosition;
        private float blend;
        private float riseBlend;
        private bool restored;

        public void Begin(Transform chair, GameObject carriedBasket)
        {
            standingPosition = transform.position;
            standingRotation = transform.rotation;
            seatedRotation = chair.rotation * Quaternion.Euler(-8f, 180f, 0f);
            // Measured seat surface of the fitted imported chair: Y=.58, Z=.055.
            Vector3 hip = chair.TransformPoint(new Vector3(0f, .68f, .04f));
            seatedPosition = hip - seatedRotation * Vector3.Scale(new Vector3(0f,.8f,0f), transform.lossyScale);
            seatedLimbs = new GameObject("MassageSeatedLimbs");
            seatedLimbs.transform.SetParent(transform, false);
            foreach (string side in new[] { "L", "R" })
            {
                float x = side == "L" ? -.14f : .14f;
                Transform leg = transform.Find("Leg_" + side);
                Transform arm = transform.Find("Arm_" + side);
                Material pants = GetMaterial(leg, "Leg_" + side + "_Mesh");
                Material shoe = GetMaterial(leg, "Leg_" + side + "_Mesh/Shoe");
                Material sleeve = GetMaterial(arm, "Arm_" + side + "_Mesh");
                Material skin = GetMaterial(arm, "Arm_" + side + "_Mesh/Hand");
                Hide(leg); Hide(arm);
                Segment("Thigh", new Vector3(x,.8f,0), new Vector3(x,.77f,.28f), .19f,.21f,pants);
                Segment("Shin", new Vector3(x,.77f,.28f), new Vector3(x,.46f,.4f), .18f,.2f,pants);
                Segment("Shoe", new Vector3(x,.44f,.36f),new Vector3(x,.44f,.55f),.2f,.12f,shoe);
                float ax = x < 0 ? -.31f : .31f;
                Segment("Sleeve",new Vector3(ax,1.26f,0),new Vector3(ax,.94f,.12f),.16f,.18f,sleeve);
                Segment("Forearm",new Vector3(ax,.94f,.12f),new Vector3(ax,.9f,.3f),.14f,.15f,sleeve);
                Segment("Hand",new Vector3(ax,.9f,.3f),new Vector3(ax,.9f,.39f),.14f,.13f,skin);
            }
            if (carriedBasket != null)
            {
                basket=carriedBasket.transform;
                basketPosition=basket.localPosition; basketRotation=basket.localRotation; basketScale=basket.localScale;
                basketWorldPosition=chair.TransformPoint(new Vector3(.67f,0f,-.65f));
                basket.position=basketWorldPosition; basket.rotation=Quaternion.identity;
                float bottom=float.PositiveInfinity;
                foreach(var r in basket.GetComponentsInChildren<Renderer>())
                    bottom=Mathf.Min(bottom,r.bounds.min.y);
                if(!float.IsInfinity(bottom))basketWorldPosition.y+=chair.position.y+.02f-bottom;
            }
            Apply(0f);
        }

        private static Material GetMaterial(Transform limb, string path)
        {
            Transform part=limb != null ? limb.Find(path) : null;
            Renderer r=part != null ? part.GetComponent<Renderer>() : null;
            return r != null ? r.sharedMaterial : null;
        }
        private void Hide(Transform part)
        {
            if(part != null && part.gameObject.activeSelf) { hidden.Add(part.gameObject);part.gameObject.SetActive(false); }
        }
        private void Segment(string name, Vector3 a, Vector3 b, float width, float depth, Material material)
        {
            GameObject part=PrimitiveFactory.CreateVisualCube(name,seatedLimbs.transform);
            part.transform.localPosition=(a+b)*.5f;
            part.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
            part.transform.localScale=new Vector3(width,(b-a).magnitude,depth);
            part.GetComponent<Renderer>().sharedMaterial=material;
        }
        public void Apply(float deltaTime)
        {
            blend=Mathf.Min(1f,blend+deltaTime/.45f);
            float t=Mathf.SmoothStep(0f,1f,blend);
            transform.position=Vector3.Lerp(standingPosition,seatedPosition,t);
            transform.rotation=Quaternion.Slerp(standingRotation,seatedRotation,t);
            if(basket != null)
            {
                basket.position=basketWorldPosition;
                basket.rotation=Quaternion.identity;
            }
        }
        public bool StandUp(float deltaTime)
        {
            riseBlend=Mathf.Min(1f,riseBlend+deltaTime/.4f);
            float t=Mathf.SmoothStep(0f,1f,riseBlend);
            transform.position=Vector3.Lerp(seatedPosition,standingPosition,t);
            transform.rotation=Quaternion.Slerp(seatedRotation,standingRotation,t);
            if(basket!=null){basket.position=basketWorldPosition;basket.rotation=Quaternion.identity;}
            return riseBlend>=1f;
        }
        public void Restore()
        {
            if(restored)return;
            restored=true;
            transform.position=standingPosition;transform.rotation=standingRotation;
            foreach(GameObject part in hidden)if(part != null)part.SetActive(true);
            if(seatedLimbs != null){seatedLimbs.SetActive(false);Destroy(seatedLimbs);}
            if(basket != null)
            {
                basket.localPosition=basketPosition;basket.localRotation=basketRotation;basket.localScale=basketScale;
            }
        }
    }
}
