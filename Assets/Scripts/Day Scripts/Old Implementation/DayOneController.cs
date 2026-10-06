using System.Threading.Tasks;
using UnityEngine;

public class DayOneController : DayScriptBase
{
    private void Awake()
    {
        dayNumber = 1;
    }

    protected override async Task RunDaySequenceAsync()
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

        // Progress to next step only when interaction/gameplay event signals completion
        GameManager.Instance.setStep("Task1_In_Progress");
    }

    private async Task RunTask1Step()
    {
        await PlayLineAsync("Clown", HeadPersonality.None, "Let's inspect the monitor setup.", 3.0f);
    }

    private async Task RunEndOfDayStep()
    {
        await PlayLineAsync("Me", HeadPersonality.InternalMonologue, "Time to log off and rest.", 4.0f);

        PersonalitySkills skillsEarnedDay1 = FindAnyObjectByType<PersonalitySkills>();
        if (skillsEarnedDay1 != null)
        {
            skillsEarnedDay1.AwardPoint("Day_1_Complete");
        }
    }
}
