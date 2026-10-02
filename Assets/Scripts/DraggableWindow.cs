using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.Rendering;


public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [SerializeField] private Transform windowRoot;
    [SerializeField] private Transform desktopBottomLeft;
    [SerializeField] private Transform desktopTopRight;
    
    private static readonly List<DraggableWindow> openWindows = new List<DraggableWindow>();
    private const int FirstWindowOrder = 100;

    private SortingGroup windowGroup;
    private Renderer[] windowRenderers;
    private Camera dragCamera;
    private Vector3 offset;
    private float depth;
    private int dragPointerId;
    private bool warnedAboutSize;
    
    
    public Transform WindowRoot => windowRoot;
    public Transform DesktopBottomLeft => desktopBottomLeft;
    public Transform DesktopTopRight => desktopTopRight;
    public bool DraggingAllowed { get; private set; } = true;

    
    public void SetDraggingAllowed(bool allowed)
    {
        DraggingAllowed = allowed;
        if (!allowed)
            dragCamera = null;
    }

    private void OnEnable()
    {
        if (windowRoot == null)
        {
            Debug.LogError("Assign WindowRoot to DraggableWindow", this);
            return;
        }

        windowGroup = windowRoot.GetComponent<SortingGroup>();
        if (windowGroup == null)
            windowGroup = windowRoot.gameObject.AddComponent<SortingGroup>();
        windowGroup.enabled = true;
        
        windowRenderers = windowRoot.GetComponentsInChildren<Renderer>();
        warnedAboutSize = false;
        BringToFront();
        if(DraggingAllowed)
            MoveWithinDesktop(windowRoot.position);
    }
    
    private void OnDisable()
    {
        openWindows.Remove(this);
        dragCamera = null;
        RefreshWindowOrder();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !DraggingAllowed
            || windowRoot == null || dragCamera != null)
            return;

        dragCamera = eventData.pressEventCamera;
        if (dragCamera == null)
            return;

        dragPointerId = eventData.pointerId;
        BringToFront();
        depth = dragCamera.WorldToScreenPoint(windowRoot.position).z;
        offset = windowRoot.position - PointerWorldPosition(eventData.pressPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !DraggingAllowed
            || dragCamera == null || eventData.pointerId != dragPointerId)
            return;

        MoveWithinDesktop(PointerWorldPosition(eventData.position) + offset);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left
            && eventData.pointerId == dragPointerId)
            dragCamera = null;
    }
    
    public void BringToFront()
    {
        if (windowGroup == null || !isActiveAndEnabled)
            return;

        openWindows.Remove(this);
        openWindows.Add(this);
        RefreshWindowOrder();
    }
    
    private static void RefreshWindowOrder()
    {
        openWindows.RemoveAll(window => window == null || !window.isActiveAndEnabled);
        for (int i = 0; i < openWindows.Count; i++)
        {
            if (openWindows[i].windowGroup == null) continue;
            openWindows[i].windowGroup.sortingOrder = FirstWindowOrder + i * 10;
            // Reserve the next sorting slot for this window's world-space UI.
            foreach (Canvas canvas in openWindows[i].windowRoot.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.renderMode != RenderMode.WorldSpace || !canvas.overrideSorting) continue;
                canvas.sortingLayerID = openWindows[i].windowGroup.sortingLayerID;
                canvas.sortingOrder = FirstWindowOrder + i * 10 + 1;
            }
        }
    }

    private Vector3 PointerWorldPosition(Vector2 screenPosition)
    {
        return dragCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, depth));
    }
    
    private void MoveWithinDesktop(Vector3 desiredPosition)
    {
        if (windowRoot == null)
            return;
        if (desktopBottomLeft == null || desktopTopRight == null)
        {
            Debug.LogError("Assign both desktop corner markers on DraggableWindow.", this);
            return;
        }
        if (!TryGetWindowBounds(out Bounds bounds))
            return;

        Vector3 a = desktopBottomLeft.position;
        Vector3 b = desktopTopRight.position;
        float left = Mathf.Min(a.x, b.x);
        float right = Mathf.Max(a.x, b.x);
        float bottom = Mathf.Min(a.y, b.y);
        float top = Mathf.Max(a.y, b.y);

        // Using the complete bounds accounts for parent scaling, an off-center
        // pivot, and title bars or buttons extending beyond the body sprite.
        if (bounds.size.x > right - left || bounds.size.y > top - bottom)
        {
            if (!warnedAboutSize)
            {
                Debug.LogWarning("This window is larger than the desktop area. "
                                 + "Reduce its size or move the corner markers farther apart.", this);
                warnedAboutSize = true;
            }
            return;
        }
        warnedAboutSize = false;

        Vector3 movement = desiredPosition - windowRoot.position;
        movement.x = Mathf.Clamp(movement.x, left - bounds.min.x, right - bounds.max.x);
        movement.y = Mathf.Clamp(movement.y, bottom - bounds.min.y, top - bounds.max.y);
        movement.z = 0f;
        windowRoot.position += movement;
    }
    
    public bool TryGetWindowBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        if (windowRenderers == null)
            return false;

        foreach (Renderer part in windowRenderers)
        {
            if (part == null || !part.enabled || !part.gameObject.activeInHierarchy)
                continue;
            if (part is SpriteRenderer spriteRenderer && spriteRenderer.sprite == null)
                continue;

            if (!found)
                bounds = part.bounds;
            else
                bounds.Encapsulate(part.bounds);
            found = true;
        }
        return found;
    }
}
