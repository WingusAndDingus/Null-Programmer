using UnityEngine;
using UnityEngine.EventSystems;

public class StartButtonListener: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Startbutton parentManager;

    private void Awake()
    {
        parentManager = GetComponentInParent<Startbutton>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (parentManager != null) parentManager.SetHovered(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (parentManager != null) parentManager.SetHovered(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    { 
        //debugging
        Debug.Log("Click detected on Start Game");
        if (parentManager != null) parentManager.ChangeScene();
        //throw new System.NotImplementedException();
    }
}
