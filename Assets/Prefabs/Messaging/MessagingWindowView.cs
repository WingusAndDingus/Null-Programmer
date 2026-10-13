using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class MessagingWindowView : MonoBehaviour
{
    public Canvas canvas;
    public SpriteRenderer body, titleBar;
    public TMP_Text heading;
    public Transform contacts, history, replies;
    public ScrollRect scroll;
    public DraggableWindow drag;
    public TMP_FontAsset messageFont;
    public float messageFontSize=36;
    private float unitsPerPixel;
    public void FitContents()
    {
        if (canvas == null || body == null || titleBar == null) return;
        Bounds b = body.bounds; float padding = Mathf.Min(b.size.x,b.size.y)*.025f;
        float left=b.min.x+padding, right=b.max.x-padding, bottom=b.min.y+padding;
        float top=Mathf.Min(b.max.y,titleBar.bounds.min.y)-padding;
        if (top<=bottom || right<=left) return;
        if (unitsPerPixel<=0) unitsPerPixel=(right-left)/900f;
        var rect=(RectTransform)canvas.transform; Vector3 scale=rect.parent.lossyScale;
        rect.localScale=new Vector3(unitsPerPixel/scale.x,unitsPerPixel/scale.y,1f/scale.z);
        rect.rotation=Quaternion.identity; rect.position=new Vector3((left+right)/2,(bottom+top)/2,b.center.z-.02f);
        rect.sizeDelta=new Vector2((right-left)/unitsPerPixel,(top-bottom)/unitsPerPixel);
        canvas.worldCamera=Camera.main;
        var group=GetComponent<SortingGroup>();
        if (group!=null) { canvas.sortingLayerID=group.sortingLayerID; canvas.sortingOrder=group.sortingOrder+1; }
    }
    private void LateUpdate() => FitContents();
    public static void Clear(Transform root)
    {
        foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
    }
    public TMP_Text Label(Transform parent,string value,int fontSize=30)
    {
        var go=new GameObject("Message",typeof(RectTransform),typeof(TextMeshProUGUI),typeof(LayoutElement));
        go.transform.SetParent(parent,false);
        var label=go.GetComponent<TextMeshProUGUI>(); label.font=messageFont!=null ? messageFont : TMP_Settings.defaultFontAsset;
        label.fontSharedMaterial=label.font.material; label.text=value; label.fontSize=messageFontSize;
        label.color=Color.white; label.raycastTarget=false; label.richText=false;
        var layout=go.GetComponent<LayoutElement>(); layout.minHeight=messageFontSize*1.6f;
        return label;
    }
    public Button Choice(Transform parent,string text,UnityEngine.Events.UnityAction action)
    {
        var go=new GameObject("Choice",typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement));
        go.transform.SetParent(parent,false); go.GetComponent<Image>().color=new Color(.13f,.25f,.35f);
        
        var button=go.GetComponent<Button>(); button.targetGraphic=go.GetComponent<Image>(); button.onClick.AddListener(action);
        var label=Label(go.transform,text,27); var rect=label.rectTransform;
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=new Vector2(12,7); rect.offsetMax=new Vector2(-12,-7);
        label.alignment=TextAlignmentOptions.MidlineLeft;
        label.GetComponent<LayoutElement>().ignoreLayout=true;
        var sizing=go.GetComponent<LayoutElement>();
        sizing.preferredHeight=Mathf.Max(messageFontSize*1.8f,label.GetPreferredValues(text,Mathf.Max(80,((RectTransform)parent).rect.width-44),0).y+20);
        return button;
    }
}
