using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.Rendering;


public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Persistence")]
    [SerializeField] private WindowIdentity windowIdentity;

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


    private void Awake()
    {
        if (windowIdentity == null)
        {
            windowIdentity =
                GetComponentInParent<WindowIdentity>();
        }
    }

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

        if (windowIdentity == null || !windowIdentity.HasValidId)
        {
            Debug.LogError(
                "DraggableWindow needs a valid WindowIdentity.", this);

            return;
        }

        windowGroup = windowRoot.GetComponent<SortingGroup>();

        if (windowGroup == null)
            windowGroup = windowRoot.gameObject.AddComponent<SortingGroup>();

        windowGroup.enabled = true;

        windowRenderers = windowRoot.GetComponentsInChildren<Renderer>();
        warnedAboutSize = false;

        // Make sure this window has a persistent stack position.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GetOrCreateWindowStackOrder(windowIdentity.Id);
        }

        // Register this scene's instance WITHOUT moving it to the front.
        if (!openWindows.Contains(this))
        {
            openWindows.Add(this);
        }

        RefreshWindowOrder();

        // Window-position restoration logic is kept here.
        if (DraggingAllowed)
        {
            if (GameManager.Instance != null
                && GameManager.Instance.TryGetWindowPosition(
                    windowIdentity.Id,
                    out Vector2 savedPosition))
            {
                RestoreWindowPosition(savedPosition);
            }
            else
            {
                MoveWithinDesktop(windowRoot.position);
            }
        }
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
        {
            SaveWindowPosition();
            dragCamera = null;
        }
    }

    public void BringToFront()
    {
        if (windowGroup == null || !isActiveAndEnabled)
            return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.BringWindowToFront(windowIdentity.Id);
        }

        if (!openWindows.Contains(this))
        {
            openWindows.Add(this);
        }

        RefreshWindowOrder();
    }

    private static void RefreshWindowOrder()
    {
        openWindows.RemoveAll(
            window => window == null || !window.isActiveAndEnabled);

        if (GameManager.Instance != null)
        {
            openWindows.Sort((a, b) =>
            {
                int aOrder =
                    GameManager.Instance.GetOrCreateWindowStackOrder(
                        a.windowIdentity.Id);

                int bOrder =
                    GameManager.Instance.GetOrCreateWindowStackOrder(
                        b.windowIdentity.Id);

                return aOrder.CompareTo(bOrder);
            });
        }

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

    private void SaveWindowPosition()
    {
        if (GameManager.Instance == null)
            return;

        if (windowIdentity == null || !windowIdentity.HasValidId)
        {
            return;
        }

        if (!TryGetAllowedPositionRange(
            out float minX,
            out float maxX,
            out float minY,
            out float maxY))
        {
            return;
        }

        float normalizedX =
            Mathf.InverseLerp(minX, maxX, windowRoot.position.x);

        float normalizedY =
            Mathf.InverseLerp(minY, maxY, windowRoot.position.y);

        GameManager.Instance.SetWindowPosition(
            windowIdentity.Id,
            new Vector2(normalizedX, normalizedY));
    }

    private bool TryGetAllowedPositionRange(
    out float minX,
    out float maxX,
    out float minY,
    out float maxY)
    {
        minX = maxX = minY = maxY = 0f;

        if (windowRoot == null
            || desktopBottomLeft == null
            || desktopTopRight == null)
        {
            return false;
        }

        if (!TryGetWindowBounds(out Bounds bounds))
            return false;

        Vector3 a = desktopBottomLeft.position;
        Vector3 b = desktopTopRight.position;

        float desktopLeft = Mathf.Min(a.x, b.x);
        float desktopRight = Mathf.Max(a.x, b.x);
        float desktopBottom = Mathf.Min(a.y, b.y);
        float desktopTop = Mathf.Max(a.y, b.y);

        // Distance from the window root/pivot to each visible edge.
        float leftOffset =
            windowRoot.position.x - bounds.min.x;

        float rightOffset =
            bounds.max.x - windowRoot.position.x;

        float bottomOffset =
            windowRoot.position.y - bounds.min.y;

        float topOffset =
            bounds.max.y - windowRoot.position.y;

        minX = desktopLeft + leftOffset;
        maxX = desktopRight - rightOffset;

        minY = desktopBottom + bottomOffset;
        maxY = desktopTop - topOffset;

        return minX <= maxX && minY <= maxY;
    }

    private void RestoreWindowPosition(Vector2 normalizedPosition)
    {
        if (!TryGetAllowedPositionRange(
            out float minX,
            out float maxX,
            out float minY,
            out float maxY))
        {
            return;
        }

        Vector3 targetPosition = windowRoot.position;

        targetPosition.x = Mathf.Lerp(
            minX,
            maxX,
            Mathf.Clamp01(normalizedPosition.x));

        targetPosition.y = Mathf.Lerp(
            minY,
            maxY,
            Mathf.Clamp01(normalizedPosition.y));

        MoveWithinDesktop(targetPosition);
    }
}
