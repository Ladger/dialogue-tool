using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI option1Text;
    [SerializeField] private TextMeshProUGUI option2Text;
    [SerializeField] private TextMeshProUGUI nextButtonText;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject optionSelectorUI;

    private Vector2 offScreenPos;
    private Vector2 onScreenPos;
    private RectTransform dialogueBox;
    private int selectionIndex;

    private System.Action onShowComplete;

    private void Awake()
    {
        dialogueBox = GetComponent<RectTransform>();

    }

    public void ShowDialogueBox(System.Action onComplete = null)
    {

    }

    public void HideDialogueBox(System.Action onComplete = null)
    {

    }

    public void SetDialogueBase()
    {
    }

    public void SetDialogueText(string text)
    {
        dialogueText.text = text;
    }

    public void SetNextButtonText(string text)
    {
        nextButtonText.text = text;
    }

    public void SetNextButtonActiveness(bool state)
    {
        nextButton.SetActive(state);
    }

    public void SetOptionText(string option1, string option2)
    {
        option1Text.text = option1;
        option2Text.text = option2;
    }

    public void SetOptionSelectorActiveness(bool state)
    {
        optionSelectorUI.SetActive(state);
    }

    public void ResetDialogueUI()
    {
        SetDialogueText(""); // Clear dialogue text
        SetNextButtonActiveness(false);
        SetOptionSelectorActiveness(false);
        SetNextButtonText("Click to continue...");
    }

}
