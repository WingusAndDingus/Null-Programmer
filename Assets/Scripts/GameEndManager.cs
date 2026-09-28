using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameEndManager : MonoBehaviour
{
    public TextMeshProUGUI endingTitleText;
    public TextMeshProUGUI feedbackText;
    public Image playerUmlImage;
    public Image standardUmlImage;

    public void SetupEndScreen(string title, string feedback, Sprite playerUML, Sprite standardUML)
    {
        endingTitleText.text = title;
        feedbackText.text = feedback;
        playerUmlImage.sprite = playerUML;
        standardUmlImage.sprite = standardUML;
    }
}