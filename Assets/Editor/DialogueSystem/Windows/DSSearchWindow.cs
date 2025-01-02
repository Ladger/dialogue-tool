using DS.Enumerations;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DS.Windows
{
    using Elements;

    public class DSSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DSGraphView _graphView;
        private Texture2D indentationIcon;

        public void Initialize(DSGraphView dsGraphView)
        {
            _graphView = dsGraphView;

            indentationIcon = new Texture2D(1, 1);
            indentationIcon.SetPixel(0, 0, Color.clear);
            indentationIcon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchTreeEntries = new List<SearchTreeEntry>()
            {
                new SearchTreeGroupEntry(new GUIContent("Create Element")),
                new SearchTreeGroupEntry(new GUIContent("Dialogue Node"), 1),
                new SearchTreeEntry(new GUIContent("Single Choice", indentationIcon))
                {
                    level = 2,
                    userData = DSNodeType.SingleChoice
                },
                new SearchTreeEntry(new GUIContent("Multiple Choice", indentationIcon))
                {
                    level = 2,
                    userData = DSNodeType.MultipleChoice
                },
                new SearchTreeGroupEntry(new GUIContent("Dialogue Group"), 1),
                new SearchTreeEntry(new GUIContent("Single Group", indentationIcon))
                {
                    level= 2,
                    userData = new Group()
                }
            };

            return searchTreeEntries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            Vector2 localMousePosition = _graphView.GetLocalMousePosition(context.screenMousePosition, true);
            switch (SearchTreeEntry.userData)
            {
                case (DSNodeType.SingleChoice):
                    DSSingleChoiceNode dSSingleChoiceNode = (DSSingleChoiceNode)_graphView.CreateNode("DialogueName", DSNodeType.SingleChoice, localMousePosition);
                    _graphView.AddElement(dSSingleChoiceNode);

                    return true;
                case (DSNodeType.MultipleChoice):
                    DSMultipleChoiceNode dSMultipleChoiceNode = (DSMultipleChoiceNode)_graphView.CreateNode("DialogueName", DSNodeType.MultipleChoice, localMousePosition);
                    _graphView.AddElement(dSMultipleChoiceNode);

                    return true;
                case (Group _):
                    _graphView.CreateGroup("DialogueGroup", localMousePosition);

                    return true;
                default: return false;
            }
        }
    }
}

