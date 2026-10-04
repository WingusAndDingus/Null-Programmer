using System.Threading.Tasks;
using UnityEngine;

public class DayOneController : DayScriptBase
{
    private void Awake()
    {
        dayNumber = 1;
    }

    protected override async void RunDaySequence()
    {
        GameManager.EnsureInstance();

        string step = GameManager.Instance.CurrentStep;

        switch (step)
        {
            case "Start":
                await RunStartStep();
                break;

            case "Task1_In_Progress":
                await RunTask1Step();
                break;

            case "ReadyForSleep":
                await RunEndOfDayStep();
                break;

            default:
                await RunStartStep();
                break;
        }
    }

    private async Task RunStartStep()
    {
        await PlayLineAsync("Me", HeadPersonality.InternalMonologue, "Ugh... My head is pounding. Where am I?", 4.0f);

        // Update state before switching monitor scene
        GameManager.Instance.setStep("Task1_In_Progress");
    }

    private async Task RunTask1Step()
    {
        // Dialogue or interactions for Task 1
        await PlayLineAsync("Clown", HeadPersonality.None, "Let's inspect the monitor setup.", 3.0f);

        GameManager.Instance.setStep("ReadyForSleep");
    }

    private async Task RunEndOfDayStep()
    {
        await PlayLineAsync("Me", HeadPersonality.InternalMonologue, "Time to log off and rest.", 4.0f);

        PersonalitySkills skillsEarnedDay1 = FindAnyObjectByType<PersonalitySkills>();
        if (skillsEarnedDay1 != null)
        {
            skillsEarnedDay1.AwardPoint("Day_1_Complete");
        }

        GameManager.Instance.AdvanceToNextDay();
        SwitchMonitorScene("Sleep");
    }
}
