using UnityEngine;

[CreateAssetMenu(menuName = "Null Programmer/Messaging/Conversation")]
public sealed class MessageConversation : ScriptableObject
{
    public string id = "family_checkin";
    public string contactName = "Family";
    public Sprite avatar;
    [Min(1)] public int availableFromDay = 1;
    [TextArea(2,8)] public string openingMessage;
    public Reply[] replies;
    [System.Serializable]
    public sealed class Reply
    {
        [TextArea(1,4)] public string text;
        [TextArea(1,6)] public string response;
        public int connectednessDelta;
        public int suspicionDelta;
    }
}

