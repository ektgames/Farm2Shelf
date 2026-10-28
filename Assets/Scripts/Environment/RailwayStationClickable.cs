using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    public sealed class RailwayStationClickable : MonoBehaviour, IPointerClickHandler
    {
        public void OpenManifest()
        {
            if (!TouchInputHelper.IsWorldInteractionBlocked()) RailwayFreightModalUI.Show();
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (data != null && !data.dragging) OpenManifest();
        }
    }
}
