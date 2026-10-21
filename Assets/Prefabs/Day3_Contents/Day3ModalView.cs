using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NullProgrammer.Day3
{
    public sealed class Day3ModalView : MonoBehaviour
    {
        public TMP_Text title, teaching, code, feedback, output, progress, status;
        public TMP_Text[] optionLabels;
        public Button[] options;
        public Button run, next, architectHint, sentinelHint, rotate, reset, finish, close;
        public GameObject lessonPanel, demoPanel;
        public Image[] previewCells;
        public Button[] boardCells;
        public Day3DesktopFlow Flow { get; set; }
        public void Bind(Day3DesktopFlow flow)
        {
            Flow=flow;
            for(int i=0;i<options.Length;i++) { int index=i; options[i].onClick.AddListener(()=>flow.Choose(index)); }
            run.onClick.AddListener(flow.RunTest); next.onClick.AddListener(flow.NextExercise);
            architectHint.onClick.AddListener(()=>flow.Hint(HeadPersonality.GrandArchitect));
            sentinelHint.onClick.AddListener(()=>flow.Hint(HeadPersonality.Sentinel));
            rotate.onClick.AddListener(flow.Rotate); reset.onClick.AddListener(flow.ResetBoard);
            finish.onClick.AddListener(flow.FinishDemo); close.onClick.AddListener(flow.CloseIDE);
            for(int i=0;i<boardCells.Length;i++)
            {
                int index=i; var cell=boardCells[i];
                cell.onClick.AddListener(()=>flow.Place(index/10,index%10));
                var hover=cell.gameObject.GetComponent<Day3CellHover>();
                if(hover!=null) { hover.flow=flow; hover.index=index; }
            }
        }
        public void Preview(Day3TestEngine.Run result)
        {
            foreach(var image in previewCells) image.color=new Color(.12f,.20f,.27f);
            if(result==null) return;
            foreach(int i in result.cells) if(i>=0&&i<previewCells.Length) previewCells[i].color=new Color(.2f,.75f,.55f);
            foreach(int i in result.danger) if(i>=0&&i<previewCells.Length) previewCells[i].color=new Color(.95f,.35f,.25f);
        }
    }
}
