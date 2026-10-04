using System.Threading.Tasks;
using UnityEngine;

public class DayTwoController : DayScriptBase
{
    private void Awake()
    {
        dayNumber = 2;
    }

    protected override async void RunDaySequence()
    {
        GameManager.EnsureInstance();
        // ... Day 2 unique dialogue and choices
    }
}
