using UnityEngine;
using UnityEngine.UI;

public class DayNightImage : MonoBehaviour
{
    [SerializeField] private SpriteRenderer renderer;

    [SerializeField] private Sprite daytimeImage;
    [SerializeField] private Sprite nighttimeImage;

    private bool isNight = false;

    public void ToggleDayNight()
    {
        isNight = !isNight;

        if (isNight)
            renderer.sprite = nighttimeImage;
        else
            renderer.sprite = daytimeImage;
    }

    public void SetNight()
    {
        isNight = true;
        renderer.sprite = nighttimeImage;
    }

    public void SetDay()
    {
        isNight = false;
        renderer.sprite = daytimeImage;
    }
}
