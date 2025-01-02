
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DS.Elements
{
    using Data.Save;
    using Windows;
    using DS.Utilities;
    using Enumerations;
    using UnityEditor.Experimental.GraphView;

    public class DSSingleChoiceNode : DSNode
    {
        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 position)
        {
            base.Initialize(nodeName, dsGraphView, position);

            NodeType = DSNodeType.SingleChoice;

            DSChoiceSaveData data = new DSChoiceSaveData()
            {
                Text = "Next Dialogue"
            };

            Choices.Add(data);
        }

        public override void Draw()
        {
            base.Draw();

            foreach (DSChoiceSaveData choice in Choices)
            {
                Port outputPort = this.CreatePort(choice.Text);

                outputPort.userData = choice;

                outputContainer.Add(outputPort);

            }

            RefreshExpandedState();
        }
    }
}

