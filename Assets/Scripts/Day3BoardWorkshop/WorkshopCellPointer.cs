using UnityEngine;
using UnityEngine.EventSystems;

namespace NullProgrammer.Day3Workshop
{
    // One component per persistent grid cell; hover never executes the placement algorithm.
    public sealed class WorkshopCellPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private WorkshopWindow owner;
        private int row, column;
        public void Bind(WorkshopWindow view, int r, int c) { owner = view; row = r; column = c; }
        public void OnPointerEnter(PointerEventData eventData)
        { if (owner != null) owner.HoverCell(row, column, true); }
        public void OnPointerExit(PointerEventData eventData)
        { if (owner != null) owner.HoverCell(row, column, false); }
    }
}
