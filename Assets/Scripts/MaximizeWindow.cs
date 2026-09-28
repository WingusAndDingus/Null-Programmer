using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class MaximizeWindow : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private DraggableWindow draggableWindow;
    [SerializeField] private SpriteRenderer windowBody;
    [SerializeField] private SpriteRenderer titleBar;

    [Header("Optional IDE parts")]
    [SerializeField] private SpriteRenderer codePanel;
    [SerializeField] private SpriteRenderer fileTab;

    [Header("Controls that stay at the top right")]
    [Tooltip("Add the Close Button and Maximize Button objects, not their text children.")]
    [SerializeField] private Transform[] topRightControls = new Transform[0];

    public bool IsMaximized { get; private set; }

    private readonly List<PartState> savedLayout = new List<PartState>();
    private bool previousDraggingAllowed;
    private const float MinimumSize = 0.0001f;
    
    private class PartState
    {
        public Transform part;
        public Vector3 localPosition;
        public Vector3 localScale;
        public Vector3 worldPosition;
        public Vector3 worldScale;
        public SpriteRenderer sprite;
        public Bounds spriteBounds;
    }
    private void Reset()
    {
        windowBody = GetComponent<SpriteRenderer>();
        draggableWindow = GetComponentInChildren<DraggableWindow>(true);
        if (draggableWindow != null)
            titleBar = draggableWindow.GetComponent<SpriteRenderer>();
    }

    public void ToggleMaximize()
    {
        if (!isActiveAndEnabled)
            return;

        if (IsMaximized)
            Restore();
        else
            Maximize();
    }

    public void BringToFront()
    {
        if (draggableWindow != null)
            draggableWindow.BringToFront();
    }
    
    public void Maximize()
    {
        if (IsMaximized || !isActiveAndEnabled || !ValidateSetup())
            return;

        if (!draggableWindow.TryGetWindowBounds(out Bounds originalBounds))
        {
            Debug.LogError("No visible window renderers were found.", this);
            return;
        }

        Vector3 a = draggableWindow.DesktopBottomLeft.position;
        Vector3 b = draggableWindow.DesktopTopRight.position;
        Rect desktop = Rect.MinMaxRect(
            Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
            Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));

        float extraWidth = desktop.width - originalBounds.size.x;
        float extraHeight = desktop.height - originalBounds.size.y;
        if (desktop.width < MinimumSize || desktop.height < MinimumSize
                                        || extraWidth < -MinimumSize || extraHeight < -MinimumSize)
        {
            Debug.LogWarning("The normal window must fit inside the desktop markers "
                             + "before it can be maximized. Check its size and both markers.", this);
            return;
        }
        Vector3 topLeftShift = new Vector3(
            desktop.xMin - originalBounds.min.x,
            desktop.yMax - originalBounds.max.y, 0f);
        Vector3 topRightShift = new Vector3(
            desktop.xMax - originalBounds.max.x, topLeftShift.y, 0f);

        savedLayout.Clear();
        CaptureLayout(transform); // Explicit parent-before-child order.

        previousDraggingAllowed = draggableWindow.DraggingAllowed;
        draggableWindow.SetDraggingAllowed(false);
        BringToFront();
        
        foreach (PartState state in savedLayout)
        {
            Vector3 targetPosition = state.worldPosition +
                                     (IsTopRightControl(state.part) ? topRightShift : topLeftShift);
            Vector3 targetScale = state.worldScale;

            bool resizeBoth = state.sprite != null
                              && (state.sprite == windowBody || state.sprite == codePanel);
            bool resizeWidth = state.sprite != null
                               && (resizeBoth || state.sprite == titleBar || state.sprite == fileTab);

            if (resizeWidth)
            {
                Bounds bounds = state.spriteBounds;
                float width = bounds.size.x + extraWidth;
                float height = bounds.size.y + (resizeBoth ? extraHeight : 0f);
                Vector3 ratio = new Vector3(
                    width / bounds.size.x, height / bounds.size.y, 1f);

                Vector3 newCenter = new Vector3(
                    bounds.min.x + topLeftShift.x + width * 0.5f,
                    bounds.max.y + topLeftShift.y - height * 0.5f,
                    bounds.center.z);

                // If sprite pivot is not at its center
                Vector3 pivotToCenter = bounds.center - state.worldPosition;
                targetPosition = newCenter - Vector3.Scale(pivotToCenter, ratio);
                targetScale = Vector3.Scale(state.worldScale, ratio);
            }

            // A parent has already been resized. Compensate scale to
            // keep this child's world size equal to its chosen target size.
            Vector3 parentScale = state.part.parent != null
                ? state.part.parent.lossyScale : Vector3.one;
            state.part.localScale = new Vector3(
                targetScale.x / parentScale.x,
                targetScale.y / parentScale.y,
                targetScale.z / parentScale.z);
            state.part.position = targetPosition;
        }

        IsMaximized = true;
    }
    
    public void Restore()
    {
        RestoreLayout();
        if (isActiveAndEnabled)
            BringToFront();
    }
    
    private void OnDisable()
    {
        // Closing and reopening a window returns it to its normal layout.
        RestoreLayout();
    }
    
    private void RestoreLayout()
    {
        if (!IsMaximized)
            return;

        foreach (PartState state in savedLayout)
        {
            if (state.part == null)
                continue;
            state.part.localScale = state.localScale;
            state.part.localPosition = state.localPosition;
        }

        savedLayout.Clear();
        IsMaximized = false;
        if (draggableWindow != null)
            draggableWindow.SetDraggingAllowed(previousDraggingAllowed);
    }
    
    private void CaptureLayout(Transform part)
    {
        // Canvas layout follows the resized body independently. Don't capture and
        // reposition its anchored UI elements as though they were loose sprites.
        if (part != transform && part.GetComponent<Canvas>() != null)
            return;
        SpriteRenderer sprite = part.GetComponent<SpriteRenderer>();
        savedLayout.Add(new PartState
        {
            part = part,
            localPosition = part.localPosition,
            localScale = part.localScale,
            worldPosition = part.position,
            worldScale = part.lossyScale,
            sprite = sprite,
            spriteBounds = sprite != null ? sprite.bounds : default
        });

        for (int i = 0; i < part.childCount; i++)
            CaptureLayout(part.GetChild(i));
    }
    
    private bool IsTopRightControl(Transform part)
    {
        foreach (Transform control in topRightControls)
        {
            // Include the control's label and any other nested objects.
            if (control != null && (part == control || part.IsChildOf(control)))
                return true;
        }
        return false;
    }
    
    private bool ValidateSetup()
    {
        if (draggableWindow == null || !draggableWindow.isActiveAndEnabled
            || draggableWindow.WindowRoot != transform
            || windowBody == null || titleBar == null)
        {
            Debug.LogError("Put WindowMaximizer on the Window Root. Assign its body, "
                + "title bar, and that title bar's enabled DraggableWindow component.", this);
            return false;
        }

        Transform bottomLeft = draggableWindow.DesktopBottomLeft;
        Transform topRight = draggableWindow.DesktopTopRight;
        if (bottomLeft == null || topRight == null
            || bottomLeft.IsChildOf(transform) || topRight.IsChildOf(transform))
        {
            Debug.LogError("Assign both desktop markers on DraggableWindow. "
                + "Keep the markers outside the moving window's hierarchy.", this);
            return false;
        }

        SpriteRenderer[] resizable = { windowBody, titleBar, codePanel, fileTab };
        var assigned = new HashSet<SpriteRenderer>();
        foreach (SpriteRenderer sprite in resizable)
        {
            if (sprite == null)
                continue;
            if (!assigned.Add(sprite) || !sprite.transform.IsChildOf(transform)
                || sprite.sprite == null || !sprite.enabled
                || !sprite.gameObject.activeInHierarchy
                || sprite.bounds.size.x < MinimumSize || sprite.bounds.size.y < MinimumSize)
            {
                Debug.LogError("Each assigned sprite must be a different, visible part "
                    + "of this window. Leave unused optional fields empty.", this);
                return false;
            }
        }

        foreach (Transform control in topRightControls)
        {
            if (control == null || control == transform || !control.IsChildOf(transform))
            {
                Debug.LogError("Top Right Controls must contain this window's button "
                    + "objects. Remove empty entries from the list.", this);
                return false;
            }
            foreach (SpriteRenderer sprite in assigned)
            {
                if (sprite.transform == control || sprite.transform.IsChildOf(control))
                {
                    Debug.LogError("A Top Right Control cannot contain the window body "
                        + "or a resizable panel. Assign only the button objects.", this);
                    return false;
                }
            }
        }

        // World-scale compensation assumes the flat, unrotated hierarchy used desktop
        // Reject unsupported geometry before changing anything.
        foreach (Transform part in GetComponentsInChildren<Transform>(true))
        {
            Vector3 scale = part.lossyScale;
            if (Quaternion.Angle(part.rotation, Quaternion.identity) > 0.01f
                || scale.x < MinimumSize || scale.y < MinimumSize || scale.z < MinimumSize)
            {
                Debug.LogError("WindowMaximizer needs unrotated window parts with "
                    + "positive, nonzero scales.", part);
                return false;
            }
        }
        return true;
    }


}
