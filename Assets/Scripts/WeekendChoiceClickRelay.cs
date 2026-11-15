using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Receives pointer clicks even when the associated Toggle is disabled.
/// WeekendChoiceController adds this component to each choice Toggle at runtime.
/// </summary>
public sealed class WeekendChoiceClickRelay : MonoBehaviour, IPointerClickHandler
{
    private Action onClicked;

    public void Initialize(Action callback)
    {
        onClicked = callback;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        onClicked?.Invoke();
    }
}
