using UnityEngine;
using UnityEngine.EventSystems;
namespace NullProgrammer.Day3
{
    public sealed class Day3CellHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [System.NonSerialized] public Day3DesktopFlow flow;
        [System.NonSerialized] public int index;
        public void OnPointerEnter(PointerEventData data) { if(flow!=null) flow.PreviewPlacement(index/10,index%10); }
        public void OnPointerExit(PointerEventData data) { if(flow!=null) flow.DrawBoard(); }
    }
}
