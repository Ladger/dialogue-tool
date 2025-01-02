using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace DS.Elements
{
    using Data.Save;
    using Windows;
    using Utilities;
    using Enumerations;
    using System;

    public class DSMultipleChoiceNode : DSNode
    {
        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 position)
        {
            base.Initialize(nodeName, dsGraphView, position);

            NodeType = DSNodeType.MultipleChoice;

            DSChoiceSaveData data = new DSChoiceSaveData()
            {
                Text = "New Choice"
            };

            Choices.Add(data);
        }

        public override void Draw()
        {
            base.Draw();

            Button addChoiceButton = DSElementUtility.CreateButton("Add Choice", () =>
            {
                DSChoiceSaveData data = new DSChoiceSaveData()
                {
                    Text = "New Choice"
                };

                Choices.Add(data);

                Port choicePort = CreateChoicePort(data);

                outputContainer.Add(choicePort);
            });

            addChoiceButton.AddToClassList("ds-node__button");

            mainContainer.Insert(1,addChoiceButton);

            foreach (DSChoiceSaveData choice in Choices)
            {
                Port choicePort = CreateChoicePort(choice);
                

                outputContainer.Add(choicePort);

            }

            RefreshExpandedState();
        }

        #region Elements Creation
        private Port CreateChoicePort(object userData)
        {
            Port choicePort = this.CreatePort();

            choicePort.userData = userData;

            DSChoiceSaveData choiceSaveData = (DSChoiceSaveData)userData;

            Button deleteChoiceButton = DSElementUtility.CreateButton("X", () =>
            {
                if (Choices.Count == 1) { return; }

                if (choicePort.connected)
                {
                    graphView.DeleteElements(choicePort.connections);
                }

                Choices.Remove(choiceSaveData);

                graphView.RemoveElement(choicePort);
            });

            deleteChoiceButton.AddToClassList("ds-node__button");

            TextField choiceTextField = DSElementUtility.CreateTextField(choiceSaveData.Text, null, callback =>
            {
                choiceSaveData.Text = callback.newValue;
            });

            choiceTextField.AddClasses(
                "ds-node__quote-textfield",
                "ds-node__choice-textfield",
                "ds-node__textfield__hidden"
                );

            // Order is vital.
            choicePort.Add(choiceTextField);
            choicePort.Add(deleteChoiceButton);
            return choicePort;
        }
        #endregion
    }
}

