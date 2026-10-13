using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NullProgrammer.Day3
{
    // Gives Wednesday its own read status without resetting another day's GameManager email flag.
    [DefaultExecutionOrder(950)]
    public sealed class Day3InboxAdapter : MonoBehaviour
    {
        public IntegratedMailInbox inbox;
        public Button emailButton;
        public GameObject badge;
        public TMP_Text rowTMP, inboxTMP;
        public Text rowLegacy, inboxLegacy;
        public Day3LessonData lesson;
        private Day3Progress progress;
        private void Start()
        {
            progress=Day3Progress.Get(); emailButton.onClick.AddListener(Read);
            Refresh();
        }
        private void Read() { progress.mailRead=true; Refresh(); }
        private void LateUpdate() { if(progress!=null) Refresh(); }
        private void Refresh()
        {
            if(badge!=null) badge.SetActive(!progress.mailRead);
            string row=(progress.mailRead?"READ":"NEW")+"\n\nSteve\n\n"+lesson.subject;
            if(rowTMP!=null) { rowTMP.text=row; rowTMP.fontStyle=progress.mailRead?FontStyles.Normal:FontStyles.Bold; }
            else if(rowLegacy!=null) { rowLegacy.text=row; rowLegacy.fontStyle=progress.mailRead?FontStyle.Normal:FontStyle.Bold; }
            string heading=progress.mailRead?"Inbox":"Inbox (1 unread)";
            if(inboxTMP!=null) inboxTMP.text=heading; else if(inboxLegacy!=null) inboxLegacy.text=heading;
        }
        private void OnDestroy() { if(emailButton!=null) emailButton.onClick.RemoveListener(Read); }
    }
}
