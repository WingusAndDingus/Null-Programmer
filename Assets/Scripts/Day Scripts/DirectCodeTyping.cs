using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DirectCodeTyping : MonoBehaviour
{
    [Header("Your Existing Text Component")]
    [SerializeField] private TMP_Text codeTextComponent;

    private bool isEditingActive = false;
    private string currentCodeContent = "";
    private int cursorIndex = 0;
    private bool isSubscribed = false;

    private void Update()
    {
        if (isEditingActive && !isSubscribed && Keyboard.current != null)
        {
            Keyboard.current.onTextInput += OnTextInput;
            isSubscribed = true;
        }

        if (!isEditingActive || Keyboard.current == null) return;

        // Handle Arrow Keys for Navigation
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (cursorIndex > 0) cursorIndex--;
            UpdateDisplay();
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (cursorIndex < currentCodeContent.Length) cursorIndex++;
            UpdateDisplay();
        }
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            MoveCursorVertical(-1);
            UpdateDisplay();
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            MoveCursorVertical(1);
            UpdateDisplay();
        }

        // Handle Backspace
        if (Keyboard.current.backspaceKey.wasPressedThisFrame && cursorIndex > 0)
        {
            currentCodeContent = currentCodeContent.Remove(cursorIndex - 1, 1);
            cursorIndex--;
            UpdateDisplay();
        }

        // Handle Delete Key
        if (Keyboard.current.deleteKey.wasPressedThisFrame && cursorIndex < currentCodeContent.Length)
        {
            currentCodeContent = currentCodeContent.Remove(cursorIndex, 1);
            UpdateDisplay();
        }

        // Handle Enter key for new lines or submission
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            if (Keyboard.current.ctrlKey.isPressed)
            {
                SubmitCode();
                return;
            }
            else
            {
                currentCodeContent = currentCodeContent.Insert(cursorIndex, "\n");
                cursorIndex++;
                UpdateDisplay();
            }
        }
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    private void OnTextInput(char c)
    {
        if (!isEditingActive) return;

        if (c == '\b' || c == 127) return;
        if (c == '\r') c = '\n';

        currentCodeContent = currentCodeContent.Insert(cursorIndex, c.ToString());
        cursorIndex++;
        UpdateDisplay();
    }

    public void StartEditing(string initialCode)
    {
        isEditingActive = true;
        currentCodeContent = string.IsNullOrEmpty(initialCode) ? "" : initialCode;
        cursorIndex = currentCodeContent.Length; // Start cursor at the end
        UpdateDisplay();

        if (!isSubscribed && Keyboard.current != null)
        {
            Keyboard.current.onTextInput += OnTextInput;
            isSubscribed = true;
        }
    }

    private void UnsubscribeInput()
    {
        if (isSubscribed && Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
            isSubscribed = false;
        }
    }

    private void MoveCursorVertical(int direction)
    {
        int currentLineStart = currentCodeContent.LastIndexOf('\n', Mathf.Max(0, cursorIndex - 1)) + 1;
        int column = cursorIndex - currentLineStart;

        if (direction < 0) // Moving Up
        {
            if (currentLineStart > 0)
            {
                int prevLineEnd = currentLineStart - 2;
                int prevLineStart = currentCodeContent.LastIndexOf('\n', Mathf.Max(0, prevLineEnd)) + 1;
                int prevLineLength = (prevLineEnd - prevLineStart) + 1;
                cursorIndex = prevLineStart + Mathf.Min(column, prevLineLength);
            }
        }
        else // Moving Down
        {
            int nextLineStart = currentCodeContent.IndexOf('\n', cursorIndex);
            if (nextLineStart != -1)
            {
                nextLineStart++;
                int nextLineEnd = currentCodeContent.IndexOf('\n', nextLineStart);
                if (nextLineEnd == -1) nextLineEnd = currentCodeContent.Length;
                int nextLineLength = nextLineEnd - nextLineStart;
                cursorIndex = nextLineStart + Mathf.Min(column, nextLineLength);
            }
        }
    }

    private void UpdateDisplay()
    {
        if (codeTextComponent != null)
        {
            string displayString = currentCodeContent;
            if (cursorIndex <= displayString.Length)
            {
                displayString = displayString.Insert(cursorIndex, "_");
            }
            else
            {
                displayString += "_";
            }

            codeTextComponent.text = displayString;
        }
    }

    private void SubmitCode()
    {
        isEditingActive = false;
        UnsubscribeInput();

        if (codeTextComponent != null)
        {
            codeTextComponent.text = currentCodeContent;
        }

        string finalCode = currentCodeContent.Trim();

        if (SteveScreenShareManager.Instance != null)
        {
            SteveScreenShareManager.Instance.SubmitPlayerCode(finalCode);
        }
    }
}
