using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-local desktop entry. Persistent conversation data is stored separately.</summary>
public sealed class MessagingDesktopApp : MonoBehaviour
{
    public MessageConversation[] conversations;
    public MessagingWindowView view;
    public RectTransform badge;
    public TMP_Text badgeText;
    public Canvas badgeCanvas;
    private MessagingSession state;
    private MessageConversation selected;
    private int previousDay=-1;
    private bool rebuilding;
    private void Awake()
    {
        state=MessagingSession.Get(); view.gameObject.SetActive(false);
        state.Changed+=Refresh;
    }
    private void Start() { Unlock(); Refresh(); }
    private void Update()
    {
        if (GameManager.EnsureInstance().CurrentDay!=previousDay) Unlock();
        PositionBadge();
    }
    private void Unlock()
    {
        previousDay=GameManager.EnsureInstance().CurrentDay;
        if (conversations!=null) foreach (var conversation in conversations) state.Unlock(conversation,previousDay);
        Refresh();
    }
    public void Open()
    {
        view.gameObject.SetActive(true); view.FitContents(); view.drag.BringToFront();
        selected=null; Canvas.ForceUpdateCanvases(); Refresh(); // App opening does not mark a contact's messages read.
    }
    public void Close()
    {
        view.gameObject.SetActive(false);
        GameManager.EnsureInstance().SetWindowMaximized("MessagingWindow",false);
    }
    private void Select(MessageConversation conversation)
    {
        selected=conversation; state.Read(conversation.id); Refresh();
    }
    private void Reply(int index)
    {
        if (selected==null || !state.Reply(selected,index)) return;
        state.Read(selected.id); Refresh();
    }
    private void Refresh()
    {
        if (state==null || rebuilding) return;
        rebuilding=true;
        if (view!=null && view.gameObject.activeSelf && selected!=null) state.Read(selected.id);
        int total=0;
        if (conversations!=null) foreach (var c in conversations) if (c!=null) total+=state.Unread(c.id);
        if (badge!=null) badge.gameObject.SetActive(total>0);
        if (badgeText!=null) badgeText.text=total>9 ? "9+" : total.ToString();
        if (view!=null && view.gameObject.activeSelf)
        {
            MessagingWindowView.Clear(view.contacts); MessagingWindowView.Clear(view.history); MessagingWindowView.Clear(view.replies);
            if (conversations!=null) foreach (var c in conversations)
            {
                if (c==null || state.GetThread(c.id)==null) continue;
                var contact=c; int count=state.Unread(c.id);
                var thread=state.GetThread(c.id);
                string preview=thread!=null && thread.messages.Count>0 ? thread.messages[thread.messages.Count-1].text : "";
                view.AddContact(contact,preview,count,selected==contact,()=>Select(contact));
            }
            view.SetActiveContact(selected);
            if (selected==null)
                view.Label(view.history,"Choose a contact to open the conversation.",view.messageFontSize*.78f,TextAlignmentOptions.Center,new Color(.62f,.69f,.81f));
            else
            {
                var thread=state.GetThread(selected.id);
                // Reading was applied before rendering the unread counts.
                foreach (var entry in thread.messages)
                    view.AddMessage(entry,selected.contactName,selected.avatar);
                if (!thread.replied && selected.replies!=null)
                    for (int i=0;i<selected.replies.Length;i++)
                    { int index=i; if (selected.replies[i]!=null) view.ReplyChoice(selected.replies[i].text,()=>Reply(index)); }
                else view.Label(view.replies,"Conversation complete for now.",view.messageFontSize*.67f,TextAlignmentOptions.MidlineLeft,new Color(.62f,.69f,.81f));
            }
            Canvas.ForceUpdateCanvases(); view.scroll.verticalNormalizedPosition=0;
        }
        rebuilding=false;
        // Read may change counts while rendering; update the badge without recursing.
        total=0; if (conversations!=null) foreach(var c in conversations) if(c!=null) total+=state.Unread(c.id);
        if(badge!=null) badge.gameObject.SetActive(total>0);
        if(badgeText!=null) badgeText.text=total>9 ? "9+" : total.ToString();
    }
    private void PositionBadge()
    {
        if(badge==null || badgeCanvas==null || Camera.main==null) return;
        var sprite=GetComponent<SpriteRenderer>(); var point=sprite!=null ? sprite.bounds.max : transform.position;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)badgeCanvas.transform,Camera.main.WorldToScreenPoint(point),null,out var local)) badge.anchoredPosition=local;
    }
    private void OnDestroy() { if(state!=null) state.Changed-=Refresh; }
}

