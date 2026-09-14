using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int GrandArchitectStat { get; private set; }
    public int PragmatistStat { get; private set; }
    public int SentinelStat { get; private set; }
    public int GeniusStat { get; private set; }
    public int DetectiveStat { get; private set; }

    public int SteveSuspicion { get; private set; }
    public int CodeQuality { get; private set; }
    public int Connectedness { get; private set; }

    //public event Action SteveCrisis;



    private void Awake()
    {
        Instance = this;
    }

    public void ChangeGrandArchitectStat(int value)
    {
        GrandArchitectStat += value;
    }

    public void ChangePragmatistStat(int value)
    {
        PragmatistStat += value;
    }

    public void ChangeSentinelStat(int value)
    {
        SentinelStat += value;
    }

    public void ChangeGeniusStat(int value)
    {
        GeniusStat += value;
    }

    public void ChangeDetectiveStat(int value)
    {
        DetectiveStat += value;
    }

    public void ChangeSteveSuspicion(int value)
    {
        SteveSuspicion += value;

        //if (SteveSuspicion > 20)
        //{
        //    SteveCrisis?.Invoke();
        //}
    }

    public void ChangeCodeQualityStat(int value)
    {
        CodeQuality += value;
    }

    public void ChangeConnectednessStat(int value)
    {
        Connectedness += value;
    }
}

//public class SomeOtherClass : MonoBehaviour
//{
//    private void OnEnable()
//    {
//        GameManager.Instance.SteveCrisis += SteveDoesUndesirableStuff;
//    }

//    private void AddSuspicion()
//    {
//        GameManager.Instance.ChangeSteveSuspicion(1);
//    }

//    private void SteveDoesUndesirableStuff()
//    {
//        Undesirable Stuff Happens;
//    }
//}