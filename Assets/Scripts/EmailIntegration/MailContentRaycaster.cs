using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;

/// <summary>Don't let covered world-space email controls steal clicks from the IDE.</summary>
public sealed class MailContentRaycaster : GraphicRaycaster
{
    [SerializeField] private IntegratedMailInbox owner;
    private DraggableWindow[] windows;
    public void SetOwner(IntegratedMailInbox value) { owner = value; }

    public override void Raycast(PointerEventData data, List<RaycastResult> results)
    {
        if (owner == null || owner.Drag == null || eventCamera == null) return;
        SortingGroup mine = owner.Drag.WindowRoot.GetComponent<SortingGroup>();
        if (mine == null) return;
        if (windows == null)
            windows = Object.FindObjectsByType<DraggableWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (DraggableWindow other in windows)
        {
            if (other == null || other == owner.Drag || !other.isActiveAndEnabled) continue;
            SortingGroup group = other.WindowRoot.GetComponent<SortingGroup>();
            if (group == null) continue;
            int layer = SortingLayer.GetLayerValueFromID(group.sortingLayerID);
            int myLayer = SortingLayer.GetLayerValueFromID(mine.sortingLayerID);
            if (layer < myLayer || (layer == myLayer && group.sortingOrder <= mine.sortingOrder)) continue;
            if (!other.TryGetWindowBounds(out Bounds b)) continue;
            Vector3 pointer = eventCamera.ScreenToWorldPoint(new Vector3(data.position.x, data.position.y,
                eventCamera.WorldToScreenPoint(b.center).z));
            if (pointer.x >= b.min.x && pointer.x <= b.max.x && pointer.y >= b.min.y && pointer.y <= b.max.y)
                return;
        }
        base.Raycast(data, results);
    }
}
