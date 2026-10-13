using UnityEngine;
using UnityEngine.EventSystems;
namespace NullProgrammer.Day3
{
    public sealed class Day3DesktopIconLink : MonoBehaviour, IPointerClickHandler
    {
        public Day3DesktopFlow flow;
        public bool acceptCall;
        public void OnPointerClick(PointerEventData data) => Activate();
        private void OnMouseDown() => Activate();
        private void Activate()
        {
            if(flow==null) return;
            if(acceptCall) flow.AcceptCall(); else flow.OpenIDE();
        }
    }
}
