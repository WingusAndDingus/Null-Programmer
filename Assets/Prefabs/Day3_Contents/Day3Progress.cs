using UnityEngine;

namespace NullProgrammer.Day3
{
    public sealed class Day3Progress : MonoBehaviour
    {
        public enum Stage { Email, CallAvailable, Call, Voices, IDE, Demo, Submit, Complete }
        public Stage stage;
        public bool mailRead, zoomRewarded, demoRewarded, submitted;
        public int callLine, voiceLine, exercise;
        public int zoomChoice=-1;
        public bool[] attempted=new bool[6], passed=new bool[6], introduced=new bool[6];
        public bool horizontal=true, placedHorizontal, placedVertical, testedBounds, testedOverlap;
        public Day3BoardModel board;
        public bool DemoComplete => placedHorizontal && placedVertical && testedBounds && testedOverlap;
        public static Day3Progress Get()
        {
            var manager=GameManager.EnsureInstance();
            var p=manager.GetComponent<Day3Progress>();
            if(p==null) p=manager.gameObject.AddComponent<Day3Progress>();
            return p;
        }
        public bool ApplyFirstAttempt(bool correct,HeadPersonality personality)
        {
            if(attempted[exercise]) return false;
            attempted[exercise]=true;
            if(!correct) return false;
            Reward(personality); return true;
        }
        public void CompleteDemo()
        {
            if(!DemoComplete || demoRewarded) return;
            demoRewarded=true;
            Reward(HeadPersonality.GrandArchitect); Reward(HeadPersonality.Sentinel);
            stage=Stage.Submit;
        }
        public void RespondToSteve(int choice)
        {
            if(zoomRewarded) return;
            zoomRewarded=true; zoomChoice=choice;
            var gm=GameManager.EnsureInstance();
            gm.ChangeSteveSuspicion(choice==2 ? 3 : -2);
            if(choice==0) Reward(HeadPersonality.Sentinel);
            else if(choice==1) Reward(HeadPersonality.GrandArchitect);
        }
        public void Submit()
        {
            if(stage!=Stage.Submit || submitted) return;
            submitted=true; GameManager.EnsureInstance().ChangeSteveSuspicion(-3);
        }
        public void EnsureBoard()
        {
            if(board!=null) return;
            board=new Day3BoardModel(); board.Place(4,4,3,true);
        }
        public void ResetBoard()
        {
            board=null; EnsureBoard();
            placedHorizontal=placedVertical=testedBounds=testedOverlap=false;
        }
        private static void Reward(HeadPersonality personality)
        {
            var gm=GameManager.EnsureInstance();
            if(personality==HeadPersonality.GrandArchitect) gm.ChangeGrandArchitectStat(1);
            if(personality==HeadPersonality.Sentinel) gm.ChangeSentinelStat(1);
        }
    }
}
