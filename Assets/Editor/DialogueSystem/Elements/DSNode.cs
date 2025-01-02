using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor.UIElements;
using System;
using System.Linq;

namespace DS.Elements
{
    using Windows;
    using Enumerations;
    using Utilities;
    using Data.Save;
    using System.Linq;

    public class DSNode : Node
    {
        public string ID {  get; set; }
        public string DialogueName { get; set;}
        public List<DSChoiceSaveData> Choices { get; set;}
        public string Text { get; set;}
        public DSNodeType NodeType { get; set;}
        public DSDialogueType DialogueType { get; set;}
        public DSGroup Group { get; set;}

        protected DSGraphView graphView;
        private Color defaultBackgroundColor;


        public virtual void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 position)
        {
            ID = Guid.NewGuid().ToString();
            DialogueName = nodeName;
            Choices = new List<DSChoiceSaveData>();
            Text = "Dialogue text.";
            DialogueType = DSDialogueType.Dialogue;

            defaultBackgroundColor = new Color(29f / 255f, 29f / 255f, 30f / 255f);
            graphView = dsGraphView;

            SetPosition(new Rect(position, Vector2.zero));

            mainContainer.AddToClassList("ds-node__main-container");
            extensionContainer.AddToClassList("ds-node__extension-container");
        }

        #region Overrides
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Disconnect Input", actionEvent => DisconnectInputPorts());
            evt.menu.AppendAction("Disconnect Output", actionEvent => DisconnectOutputPorts());

            base.BuildContextualMenu(evt);
        }
        #endregion


        // All characterization of the node specialities.
        public virtual void Draw()
        {
            /* TITLE CONTAINER */

            TextField dialogueNameTextField = DSElementUtility.CreateTextField(DialogueName, null, callback =>
            {
                TextField target = (TextField) callback.target;
                target.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();

                if (string.IsNullOrEmpty(target.value))
                {
                    if (!string.IsNullOrEmpty(DialogueName))
                    {
                        graphView.RepeatedNameAmount++;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(DialogueName))
                    {
                        graphView.RepeatedNameAmount--;
                    }
                }

                if (Group == null)
                {
                    graphView.RemoveUngroupedNode(this);

                    DialogueName = target.value;

                    graphView.AddUngroupedNode(this);

                    return;
                }

                DSGroup currentGroup = Group;
                graphView.RemoveGroupedNode(this, Group);

                DialogueName = callback.newValue;

                graphView.AddGroupedNode(this, currentGroup);
                
            });

            dialogueNameTextField.AddClasses(
                "ds-node__textfield",
                "ds-node__filename-textfield",
                "ds-node__textfield__hidden"
                );

            titleContainer.Insert(0, dialogueNameTextField);

            /* INPUT CONTAINER */

            Port inputPort = this.CreatePort("Dialogue Choice", Orientation.Horizontal, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Dialogue Connection";

            inputContainer.Add(inputPort);

            /* EXTENSION CONTAINER */

            VisualElement customDataContainer = new VisualElement();
            customDataContainer.AddToClassList("ds-node__custom-data-container");

            // Should be shortened again in DSElementUtility.
            EnumField categoryEnumField = new EnumField("Dialogue Type", DialogueType);
            categoryEnumField.RegisterValueChangedCallback(evt =>
            {
                DialogueType = (DSDialogueType)evt.newValue;
            });
            customDataContainer.Add(categoryEnumField);

            Foldout textFoldout = DSElementUtility.CreateFoldout("Dialogue Text");

            TextField textTextField = DSElementUtility.CreateTextArea(Text, null, callback =>
            {
                Text = callback.newValue;
            });

            textTextField.AddClasses(
                "ds-node__textfield",
                "ds-node__quote-textfield"
                );

            textFoldout.Add(textTextField);

            customDataContainer.Add(textFoldout);



            extensionContainer.Add(customDataContainer);

            
        }

        #region Utility Methods
        public void DisconnectAllPorts()
        {
            DisconnectInputPorts();
            DisconnectOutputPorts();
        }

        private void DisconnectInputPorts()
        {
            DisconnectPorts(inputContainer);
        }

        private void DisconnectOutputPorts()
        {
            DisconnectPorts(outputContainer);
        }

        private void DisconnectPorts(VisualElement container)
        {
            foreach (Port port in container.Children())
            {
                if (!port.connected) { continue; }

                graphView.DeleteElements(port.connections);
            }
        }

        public bool IsStartingNode()
        {
            Port port = (Port) inputContainer.Children().First();

            return !port.connected;
        }

        public void SetErrorStyle(Color color)
        {
            mainContainer.style.backgroundColor = color;
        }

        public void ResetStyle()
        {
            mainContainer.style.backgroundColor = defaultBackgroundColor;
        }
        #endregion
    }
}

