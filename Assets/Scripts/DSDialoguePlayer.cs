using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DS.Enumerations;
using System.Linq;

public class DSDialoguePlayer : MonoBehaviour
{
    [SerializeField] private float typingDuration = 0.015f;
    [SerializeField] private float fastenedTypingDuration = 0.05f;

    private DSDialogueLoader _loader;
    [SerializeField] private DialogueUI _dialogueUI;
    private DialogueGraph _graph;
    private DialogueNode _currentNode;

    private float _originalTypingDuration;
    private bool _isLastDialogue;

    // Start is called before the first frame update
    void Start()
    {
        _loader = GetComponent<DSDialogueLoader>();

        _originalTypingDuration = typingDuration;

    }

    public void StartDialogue(bool hasTalkedToday, int eventID)
    {

        _dialogueUI.gameObject.SetActive(true);
        _dialogueUI.ResetDialogueUI();

        _graph = _loader.GetDialogueGraph("Dino");


        // if talked start from repeated, if not start from the related event from the selector node
        _currentNode = hasTalkedToday ? _graph.RepeatedDialogueNode : _graph.EventSelectorNode.NextNodes[eventID.ToString()];


        CheckNodeAttributes();
        StartCoroutine(TypeSentence(_currentNode.DialogueText, OnTypeSentenceComplete));

    }

    public void StopDialogue()
    {
        _dialogueUI.ResetDialogueUI();
        _dialogueUI.gameObject.SetActive(false);
    }

    public void NextDialogue()
    {
        if (CheckIsLast()) return;

        _currentNode = _currentNode.NextNodes["Next Dialogue"];

        CheckNodeAttributes();
        StartCoroutine(TypeSentence(_currentNode.DialogueText, OnTypeSentenceComplete));
    }

    public void OnOptionSelected(int choice)
    {
        if (CheckIsLast()) return;

        List<string> optionTexts = new List<string>(_currentNode.NextNodes.Keys);

        _currentNode = _currentNode.NextNodes[optionTexts[choice]];
        CheckNodeAttributes();

        if (_currentNode.DialogueType == DSDialogueType.Vendor)
        {
            _dialogueUI.gameObject.SetActive(false);

            StopDialogue();
            return;
        }

        StartCoroutine(TypeSentence(_currentNode.DialogueText, OnTypeSentenceComplete));
    }
    
    private void CheckNodeAttributes() // To check whether options are enable, close button enable, close button text etc.
    {
        if (_currentNode.NextNodes.Count > 1)
        {
            List<string> optionTexts = new List<string>(_currentNode.NextNodes.Keys);
            _dialogueUI.SetOptionText(optionTexts[0], optionTexts[1]);
        }
        else if (_currentNode.NextNodes.Count == 1)
        {
            _dialogueUI.SetNextButtonText("Click to continue...");
        }
        else
        {
            _dialogueUI.SetNextButtonText("Close");
            _isLastDialogue = true;
        }

        _dialogueUI.SetOptionSelectorActiveness(false);
        _dialogueUI.SetNextButtonActiveness(false);
    }

    private bool CheckIsLast()
    {
        if (_currentNode.NextNodes.Count == 0)
        {
            StopDialogue();
            return true;
        }

        return false;
    }

    IEnumerator TypeSentence(string sentence, System.Action onComplete)
    {
        string currentSentence = "";
        foreach (char letter in sentence.ToCharArray())
        {
            currentSentence += letter;
            _dialogueUI.SetDialogueText(currentSentence);
            yield return new WaitForSeconds(typingDuration);
        }

        onComplete?.Invoke();
    }

    private void OnTypeSentenceComplete()
    {
        if (_currentNode.NextNodes.Count > 1)
        {
            _dialogueUI.SetOptionSelectorActiveness(true);
        }
        else
        {
            _dialogueUI.SetNextButtonActiveness(true);
        }
    }
}
