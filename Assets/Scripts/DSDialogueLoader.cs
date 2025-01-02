using DS.Enumerations;
using DS.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DSDialogueLoader : MonoBehaviour
{
   public DialogueGraph GetDialogueGraph(string graphName)
    {
        DSDialogueContainerSO container = AssetDatabase.LoadAssetAtPath<DSDialogueContainerSO>($"Assets/DialogueSystem/Dialogues/{graphName}/{graphName}.asset");

        if (container == null)
        {
            Debug.LogError($"There is no graph data in the path with the ID of {graphName}.");
            return null;
        }

        DialogueGraph graph = new DialogueGraph(container);
        return graph;
    }
}

public class DialogueNode
{
    public string DialogueText { get; set; }
    public DSDialogueType DialogueType { get; set; }
    public Dictionary<string,DialogueNode> NextNodes { get; set; }

    public DialogueNode(string dialogueText, DSDialogueType dialogueType)
    {
        DialogueText = dialogueText;
        DialogueType = dialogueType;
        NextNodes = new Dictionary<string, DialogueNode>();
    }
}

public class DialogueGraph
{
    public DialogueNode EventSelectorNode { get; private set; }
    public DialogueNode RepeatedDialogueNode { get; private set; }
    public Dictionary<string, DialogueNode> AllNodes { get; private set; }

    public DialogueGraph(DSDialogueContainerSO container)
    {
        var dialogueGroupsCopy = container.DialogueGroups.ToDictionary(
            kvp => kvp.Key,
            kvp => new List<DSDialogueSO>(kvp.Value)
        );

        var ungroupedDialoguesCopy = new List<DSDialogueSO>(container.UngroupedDialogues);


        AllNodes = new Dictionary<string, DialogueNode>();

        foreach (var group in container.DialogueGroups)
        {
            foreach (var dialogueSO in group.Value)
            {
                AddDialogueNode(dialogueSO);
            }
        }

        foreach (var dialogueSO in container.UngroupedDialogues)
        {
            AddDialogueNode(dialogueSO);
        }

        foreach (var dialogueSO in ungroupedDialoguesCopy.Concat(dialogueGroupsCopy.SelectMany(g => g.Value)))
        {
            if (AllNodes.TryGetValue(dialogueSO.DialogueName, out var node))
            {
                foreach (var choice in dialogueSO.Choices)
                {
                    if (choice.NextDialogue != null && AllNodes.TryGetValue(choice.NextDialogue.DialogueName, out var nextNode))
                    {
                        node.NextNodes[choice.Text] = nextNode;
                    }
                }
            }
        }

        EventSelectorNode = FindStartingNode(container, DSDialogueType.EventSelector);
        RepeatedDialogueNode = FindStartingNode(container, DSDialogueType.SecondTalk);
    }

    private void AddDialogueNode(DSDialogueSO dialogueSO)
    {
        if (!AllNodes.ContainsKey(dialogueSO.DialogueName))
        {
            var node = new DialogueNode(dialogueSO.Text, dialogueSO.DialogueType);
            AllNodes.Add(dialogueSO.DialogueName, node);
        }
    }

    private DialogueNode FindStartingNode(DSDialogueContainerSO container, DSDialogueType targetNodeType)
    {
        var dialogueGroupsCopy = container.DialogueGroups.ToDictionary(
            kvp => kvp.Key,
            kvp => new List<DSDialogueSO>(kvp.Value)
        );

        var ungroupedDialoguesCopy = new List<DSDialogueSO>(container.UngroupedDialogues);

        var startingDialogueSO = ungroupedDialoguesCopy
            .Concat(dialogueGroupsCopy.SelectMany(g => g.Value))
            .FirstOrDefault(d => d.DialogueType == targetNodeType && d.IsStartingDialogue);

        return startingDialogueSO != null && AllNodes.TryGetValue(startingDialogueSO.DialogueName, out var node) ? node : null;
    }

}
