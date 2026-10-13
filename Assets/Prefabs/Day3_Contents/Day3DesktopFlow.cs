using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NullProgrammer.Day3
{
    [DefaultExecutionOrder(-800)]
    public sealed class Day3DesktopFlow : MonoBehaviour
    {
        public Day3LessonData lesson;
        public IntegratedMailInbox inbox;
        public ZoomScreenShareController zoom;
        public GameObject incomingCall, zoomPanel;
        public Day3ModalView view;
        public TMP_Text objective, stats;
        public Button submitButton, joinCallButton, openIDEButton;
        public GameObject zoomChoices;
        public Button[] zoomResponses;
        private Day3Progress p;
        private GameManager gm;
        private DialogueManager dialogue;
        private bool busy, wasOpen;
        private int selection=-1;
        private readonly List<Collider2D> blocked2D=new List<Collider2D>();
        private readonly List<Collider> blocked3D=new List<Collider>();
        private void Awake()
        {
            p=Day3Progress.Get(); gm=GameManager.EnsureInstance(); gm.setCurrentDay(3);
            if(view!=null) view.gameObject.SetActive(false);
            if(incomingCall!=null) incomingCall.SetActive(false);
            if(zoomPanel!=null) zoomPanel.SetActive(false);
        }
        private void Start()
        {
            dialogue=DialogueManager.Instance;
            if(view==null)
                view=FindObjectsByType<Day3ModalView>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                    .FirstOrDefault(candidate=>candidate.gameObject.scene==gameObject.scene);
            if(dialogue==null || inbox==null || inbox.WindowObject==null || view==null || lesson==null || submitButton==null)
            {
                Debug.LogError("Day 3 installation is incomplete. Select Wednesday Learning Flow and check Lesson, Inbox, View and Submit Button. View must reference Wednesday IDE Window. If that object is absent, exit Play mode and rerun the installer from your working baseline desktop, then play the new generated scene.",this);
                enabled=false; return;
            }
            view.gameObject.SetActive(false);
            view.Bind(this); submitButton.onClick.AddListener(SubmitToSteve);
            if(joinCallButton!=null) joinCallButton.onClick.AddListener(AcceptCall);
            if(openIDEButton!=null) openIDEButton.onClick.AddListener(OpenIDE);
            if(zoomChoices!=null) zoomChoices.SetActive(false);
            if(zoomResponses!=null)
                for(int i=0;i<zoomResponses.Length;i++) { int choice=i; zoomResponses[i].onClick.AddListener(()=>ChooseZoomResponse(choice)); }
            wasOpen=inbox.WindowObject.activeInHierarchy;
            if(p.stage==Day3Progress.Stage.CallAvailable) ShowIncomingCall();
            else if(p.stage==Day3Progress.Stage.Call) StartCoroutine(Call());
            else if(p.stage==Day3Progress.Stage.Voices) StartCoroutine(Introduction());
            gm.SuspicionChanged+=SuspicionChanged;
            RefreshHUD();
        }
        private void LateUpdate()
        {
            if(p==null || inbox==null) return;
            bool open=inbox.WindowObject.activeInHierarchy;
            if(p.stage==Day3Progress.Stage.Email && p.mailRead && wasOpen && !open)
            { p.stage=Day3Progress.Stage.CallAvailable; ShowIncomingCall(); }
            wasOpen=open; RefreshHUD();
        }
        private void ShowIncomingCall()
        {
            if(zoom!=null) zoom.TriggerIncomingCall();
            if(incomingCall!=null) incomingCall.SetActive(true);
        }
        public void AcceptCall()
        {
            if(busy || p.stage!=Day3Progress.Stage.CallAvailable) return;
            p.stage=Day3Progress.Stage.Call;
            // Reuse the authored Zoom panels; Day 3 provides its own greeting and dialogue.
            if(incomingCall!=null) incomingCall.SetActive(false);
            StartCoroutine(Call());
        }
        private IEnumerator Call()
        {
            busy=true; BlockWorld(); if(zoomPanel!=null) zoomPanel.SetActive(true);
            try
            {
                while(p.callLine<lesson.call.Length)
                { yield return dialogue.PlayDialogueLine(lesson.call[p.callLine]); p.callLine++; }
                if(!p.zoomRewarded)
                {
                    if(zoomChoices==null || zoomResponses==null || zoomResponses.Length!=3)
                    { Debug.LogError("Wednesday Zoom response UI missing. Run Repair Current Day 3 Scene.",this); yield break; }
                    zoomChoices.SetActive(true);
                    while(!p.zoomRewarded) yield return null;
                    zoomChoices.SetActive(false);
                    yield return dialogue.PlayDialogueLine(Day3LessonData.Line("Steve",HeadPersonality.None,
                        p.zoomChoice==2?"We need evidence that it works. Test the edges and overlaps before sending it back.":"Good. Put that plan into practice, then test the awkward cases."));
                }
                if(zoomPanel!=null) zoomPanel.SetActive(false);
                p.stage=Day3Progress.Stage.Voices;
            }
            finally { busy=false; UnblockWorld(); }
            yield return Introduction();
        }
        public void ChooseZoomResponse(int choice)
        {
            if(p==null || p.stage!=Day3Progress.Stage.Call || p.zoomRewarded || choice<0 || choice>2) return;
            p.RespondToSteve(choice);
        }
        private IEnumerator Introduction()
        {
            busy=true; BlockWorld();
            try
            {
                while(p.voiceLine<lesson.introduction.Length)
                { yield return dialogue.PlayDialogueLine(lesson.introduction[p.voiceLine]); p.voiceLine++; }
                p.stage=Day3Progress.Stage.IDE; gm.setStep("Day3_OpenIDE");
            }
            finally { busy=false; UnblockWorld(); }
        }
        public void OpenIDE()
        {
            if(busy || view.gameObject.activeInHierarchy) return;
            if((int)p.stage<(int)Day3Progress.Stage.IDE) { objective.text="Read and close Steve's email, then finish the Zoom call before opening the lesson."; return; }
            if((int)p.stage>=(int)Day3Progress.Stage.Submit) { objective.text=p.stage==Day3Progress.Stage.Complete?"Wednesday complete.":"Board verified. Send your update to Steve."; return; }
            BlockWorld(); view.gameObject.SetActive(true); dialogue.HideDialogueUI();
            if(p.stage==Day3Progress.Stage.Demo) ShowDemo(); else ShowExercise();
        }
        public void CloseIDE()
        {
            if(busy) return;
            view.gameObject.SetActive(false); dialogue.HideDialogueUI(); UnblockWorld();
        }
        private void ShowExercise()
        {
            view.lessonPanel.SetActive(true); view.demoPanel.SetActive(false);
            var e=lesson.exercises[p.exercise]; selection=-1;
            view.title.text="Board.cpp  /  "+e.title;
            view.progress.text="Exercise "+(p.exercise+1)+" / "+lesson.exercises.Length+"  |  simulated C++ tests";
            view.teaching.text="Architect: "+e.architect+"\n\nSentinel: "+e.sentinel;
            view.code.text=e.faultyCode+"\n\n"+e.question;
            for(int i=0;i<view.options.Length;i++) { view.optionLabels[i].text=((char)('A'+i))+". "+e.options[i]; view.options[i].interactable=true; view.options[i].GetComponent<Image>().color=new Color(.13f,.23f,.32f); }
            view.feedback.text=p.passed[p.exercise]?"Already passed. Continue when ready.":"Choose a replacement, then Run Test. Hints are optional and have no penalty.";
            view.output.text="Test output appears here."; view.Preview(null);
            view.run.interactable=false; view.next.interactable=p.passed[p.exercise];
            SetActions(true);
            if(!p.introduced[p.exercise]) StartCoroutine(Teach());
        }
        private IEnumerator Teach()
        {
            busy=true; SetActions(false);
            try
            {
                var e=lesson.exercises[p.exercise];
                yield return dialogue.PlayDialogueLine(Day3LessonData.Line("Architect",HeadPersonality.GrandArchitect,e.architect));
                yield return dialogue.PlayDialogueLine(Day3LessonData.Line("Sentinel",HeadPersonality.Sentinel,e.sentinel));
                p.introduced[p.exercise]=true;
            }
            finally { busy=false; SetActions(true); view.run.interactable=selection>=0; view.next.interactable=p.passed[p.exercise]; }
        }
        public void Choose(int index)
        {
            if(busy || p.stage!=Day3Progress.Stage.IDE) return;
            selection=index; view.run.interactable=true;
            for(int i=0;i<view.options.Length;i++) view.options[i].GetComponent<Image>().color=i==index?new Color(.18f,.48f,.55f):new Color(.13f,.23f,.32f);
        }
        public void RunTest()
        {
            if(busy || selection<0 || p.stage!=Day3Progress.Stage.IDE) return;
            var e=lesson.exercises[p.exercise]; var result=Day3TestEngine.Evaluate(p.exercise,selection);
            bool earned=p.ApplyFirstAttempt(result.passed,e.reward);
            if(result.passed) p.passed[p.exercise]=true;
            view.output.text=(result.passed?"PASS\n":"FAIL\n")+result.output;
            view.Preview(result);
            string explanation=result.passed?e.success:e.failures[selection];
            view.feedback.text=explanation+"\n\n"+e.transfer+(earned?"\n+1 "+(e.reward==HeadPersonality.Sentinel?"Sentinel":"Architect"):"");
            view.next.interactable=p.passed[p.exercise];
            StartCoroutine(Speak(e.reward,explanation));
        }
        public void Hint(HeadPersonality voice)
        {
            if(busy || p.stage!=Day3Progress.Stage.IDE) return;
            var e=lesson.exercises[p.exercise]; string hint=voice==HeadPersonality.Sentinel?e.sentinelHint:e.architectHint;
            view.feedback.text=hint; StartCoroutine(Speak(voice,hint));
        }
        private IEnumerator Speak(HeadPersonality voice,string text)
        {
            busy=true; SetActions(false);
            try { yield return dialogue.PlayDialogueLine(Day3LessonData.Line(voice==HeadPersonality.Sentinel?"Sentinel":"Architect",voice,text)); }
            finally { busy=false; SetActions(true); view.run.interactable=selection>=0; view.next.interactable=p.passed[p.exercise]; }
        }
        public void NextExercise()
        {
            if(busy || !p.passed[p.exercise] || p.stage!=Day3Progress.Stage.IDE) return;
            if(p.exercise+1<lesson.exercises.Length) { p.exercise++; ShowExercise(); }
            else { p.stage=Day3Progress.Stage.Demo; ShowDemo(); }
        }
        private void ShowDemo()
        {
            view.lessonPanel.SetActive(false); view.demoPanel.SetActive(true);
            view.title.text="Board placement / See your code in action";
            view.progress.text="Length-3 ships. Hover to preview, click a starting cell to place.";
            p.EnsureBoard(); DrawBoard();
            view.feedback.text="Architect: Same grid, now managed by a Board.\nSentinel: Try the edges and the orange fixture ship at row 4, columns 4-6.";
        }
        public void Rotate() { if(busy) return; p.horizontal=!p.horizontal; DrawBoard(); }
        public void ResetBoard() { if(busy || p.stage!=Day3Progress.Stage.Demo) return; p.ResetBoard(); DrawBoard(); view.feedback.text="Board reset. Test all four objectives again; skill rewards are never repeated."; }
        public void Place(int row,int col)
        {
            if(busy || p.stage!=Day3Progress.Stage.Demo) return;
            var result=p.board.Place(row,col,3,p.horizontal);
            if(result==Day3BoardModel.Result.Placed)
            { if(p.horizontal) p.placedHorizontal=true; else p.placedVertical=true; }
            else if(result==Day3BoardModel.Result.Bounds) p.testedBounds=true;
            else if(result==Day3BoardModel.Result.Overlap) p.testedOverlap=true;
            string text=result==Day3BoardModel.Result.Placed?(p.horizontal?"Same row, consecutive columns. The orientation calculation placed all three cells.":"Same column, consecutive rows. Vertical orientation placed all three cells."):
                result==Day3BoardModel.Result.Bounds?"Rejected before touching the grid. The boundary check did its job.":"One occupied cell is enough to reject the ship. The board remains unchanged.";
            view.feedback.text=(result==Day3BoardModel.Result.Placed?"Architect: ":"Sentinel: ")+text;
            DrawBoard(); StartCoroutine(DemoSpeak(result==Day3BoardModel.Result.Placed?HeadPersonality.GrandArchitect:HeadPersonality.Sentinel,text));
        }
        private IEnumerator DemoSpeak(HeadPersonality voice,string text)
        {
            busy=true; view.close.interactable=false;
            try { yield return dialogue.PlayDialogueLine(Day3LessonData.Line(voice==HeadPersonality.Sentinel?"Sentinel":"Architect",voice,text)); }
            finally { busy=false; view.close.interactable=true; }
        }
        public void DrawBoard()
        {
            if(p.board==null) return;
            for(int i=0;i<view.boardCells.Length;i++)
            { int id=p.board.At(i/10,i%10); view.boardCells[i].GetComponent<Image>().color=id==0?new Color(.12f,.20f,.27f):id==1?new Color(.95f,.55f,.18f):new Color(.2f,.72f,.55f); }
            view.status.text="Orientation: "+(p.horizontal?"horizontal":"vertical")+"\n\n"+Check(p.placedHorizontal,"Place horizontally")+"\n"+Check(p.placedVertical,"Place vertically")+"\n"+Check(p.testedBounds,"Reject an out-of-bounds placement")+"\n"+Check(p.testedOverlap,"Reject an overlapping placement")+"\n\nRows / columns: 0-9\nOrange: existing ship\nGreen: your valid placements\nRejected placements never alter cells.";
            view.finish.interactable=p.DemoComplete;
        }
        public void PreviewPlacement(int row,int col)
        {
            if(busy || p.stage!=Day3Progress.Stage.Demo) return;
            DrawBoard(); bool valid=p.board.Validate(row,col,3,p.horizontal)==Day3BoardModel.Result.Placed;
            for(int i=0;i<3;i++) { int r=row+(p.horizontal?0:i),c=col+(p.horizontal?i:0); if(r<10&&c<10) view.boardCells[r*10+c].GetComponent<Image>().color=valid?new Color(.35f,.8f,.8f):new Color(.9f,.25f,.25f); }
        }
        private static string Check(bool value,string text) => (value?"[done] ":"[ ] ")+text;
        public void FinishDemo()
        {
            if(busy || !p.DemoComplete || p.stage!=Day3Progress.Stage.Demo) return;
            p.CompleteDemo(); CloseIDE(); StartCoroutine(WrapUp());
        }
        private IEnumerator WrapUp()
        {
            busy=true; BlockWorld();
            try { foreach(var line in lesson.conclusion) yield return dialogue.PlayDialogueLine(line); }
            finally { busy=false; UnblockWorld(); }
        }
        public void SubmitToSteve()
        {
            if(busy || p.stage!=Day3Progress.Stage.Submit) return;
            p.Submit(); p.stage=Day3Progress.Stage.Complete; gm.setStep("Day3_Complete");
            StartCoroutine(SubmissionLine());
        }
        private IEnumerator SubmissionLine()
        {
            busy=true; BlockWorld();
            try { yield return dialogue.PlayDialogueLine(Day3LessonData.Line("Steve",HeadPersonality.None,"Good work. You've tested both orientations, the edges, and overlaps. That's a solid foundation for tomorrow.")); }
            finally { busy=false; UnblockWorld(); }
        }
        private void SetActions(bool value)
        {
            view.close.interactable=value; view.run.interactable=value; view.next.interactable=value;
            view.architectHint.interactable=view.sentinelHint.interactable=value;
            foreach(var button in view.options) button.interactable=value;
        }
        private void RefreshHUD()
        {
            if(objective==null || stats==null || p==null) return;
            stats.text="Steve suspicion "+gm.SteveSuspicion+"/100   |   Architect "+gm.GrandArchitectStat+"   |   Sentinel "+gm.SentinelStat;
            submitButton.gameObject.SetActive(p.stage==Day3Progress.Stage.Submit && !busy);
            if(joinCallButton!=null) joinCallButton.gameObject.SetActive(p.stage==Day3Progress.Stage.CallAvailable && !busy);
            if(openIDEButton!=null) openIDEButton.gameObject.SetActive((p.stage==Day3Progress.Stage.IDE || p.stage==Day3Progress.Stage.Demo) && !busy && !view.gameObject.activeInHierarchy);
            if(busy) return;
            objective.text=p.stage==Day3Progress.Stage.Email?"Wednesday: Read Steve's email, then close the mail window.":
                p.stage==Day3Progress.Stage.CallAvailable?"Wednesday: Accept Steve's Zoom call.":p.stage==Day3Progress.Stage.IDE?"Wednesday: Open the IDE to diagnose and test Board.cpp.":
                p.stage==Day3Progress.Stage.Demo?"Wednesday: Open the IDE to finish your board demonstration.":p.stage==Day3Progress.Stage.Submit?"Wednesday: Send your tested work to Steve.":p.stage==Day3Progress.Stage.Complete?"Wednesday complete. Board design and placement verified.":"Wednesday: Finish the discussion.";
        }
        private void SuspicionChanged(int value) => RefreshHUD();
        private void BlockWorld()
        {
            foreach(var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None)) if(c.enabled) { blocked2D.Add(c); c.enabled=false; }
            foreach(var c in FindObjectsByType<Collider>(FindObjectsSortMode.None)) if(c.enabled) { blocked3D.Add(c); c.enabled=false; }
        }
        private void UnblockWorld()
        {
            foreach(var c in blocked2D) if(c!=null) c.enabled=true;
            foreach(var c in blocked3D) if(c!=null) c.enabled=true;
            blocked2D.Clear(); blocked3D.Clear();
        }
        private void OnDestroy()
        {
            UnblockWorld(); if(gm!=null) gm.SuspicionChanged-=SuspicionChanged;
            if(submitButton!=null) submitButton.onClick.RemoveListener(SubmitToSteve);
        }
    }
}
