using DS.Elements;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Windows
{
    using Data.Save;
    using Data.Error;
    using Enumerations;
    using Utilities;
    using static UnityEngine.GraphicsBuffer;

    public class DSGraphView : GraphView
    {
        private DSEditorWindow editorWindow;
        private DSSearchWindow _searchWindow;

        private SerializableDictionary<string, DSNodeErrorData> ungroupedNodes;
        private SerializableDictionary<string, DSGroupErrorData> groups;
        private SerializableDictionary<Group, SerializableDictionary<string, DSNodeErrorData>> groupedNodes;

        private int repeatedNamesAmount;

        public int RepeatedNameAmount
        {
            get { return repeatedNamesAmount; } 
            set 
            {
                repeatedNamesAmount = value;

                if (repeatedNamesAmount == 0)
                {
                    editorWindow.EnableSaving();
                }

                if (repeatedNamesAmount == 1)
                {
                    editorWindow.DiasableSaving();
                }
            }
        }

        public DSGraphView(DSEditorWindow dSEditorWindow) 
        {
            editorWindow = dSEditorWindow;

            ungroupedNodes = new SerializableDictionary<string, DSNodeErrorData>();
            groups = new SerializableDictionary<string, DSGroupErrorData>();
            groupedNodes = new SerializableDictionary<Group, SerializableDictionary<string, DSNodeErrorData>>();

            AddManipulators();
            AddSearchWindow();
            AddGridBackground();

            OnElementsDeleted();
            OnGroupElementsAdded();
            OnGroupElementsRemoved();
            OnGroupRenamed();
            OnGraphViewChanged();

            AddStyles();
        }

        #region Overrides
        // Providing the output only goes to input port
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort == port) { return; }
                if (startPort.node == port.node) { return;}
                if (startPort.direction == port.direction) { return; }

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }
        #endregion

        #region Manipulators
        // adding element functionalities to graph
        private void AddManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            
            // Concludes drag drop select the nodes and groups etc.
            this.AddManipulator(new ContentDragger());

            // ORDER IS IMPORTANT
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Creating right click contextual menu for creating
            this.AddManipulator(CreateNodeContextualMenu("Add Node (Single Choice)", DSNodeType.SingleChoice));
            this.AddManipulator(CreateNodeContextualMenu("Add Node (Multiple Choice)", DSNodeType.MultipleChoice));
            this.AddManipulator(CreateGroupContextualMenu());

        }

        private IManipulator CreateGroupContextualMenu()
        {
            // Showing in contextual menu and adding the functionality
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(
                menuEvent => menuEvent.menu.AppendAction("Add Group", actionEvent => CreateGroup("DialogueGroup", GetLocalMousePosition(actionEvent.eventInfo.localMousePosition)))
            );

            return contextualMenuManipulator;
        }

        private IManipulator CreateNodeContextualMenu(string actionTitle, DSNodeType dialogueType)
        {
            // Showing in contextual menu and adding the functionality
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(
                menuEvent => menuEvent.menu.AppendAction(actionTitle, actionEvent => AddElement(CreateNode("DialogueName", dialogueType, GetLocalMousePosition(actionEvent.eventInfo.localMousePosition))))        
            );

            return contextualMenuManipulator;
        }
        #endregion

        #region Element Create Methods
        public DSGroup CreateGroup(string title, Vector2 localMousePosition)
        {
            DSGroup group = new DSGroup(title, localMousePosition);

            AddGroup(group);
            AddElement(group);

            foreach (GraphElement selectedElement in selection)
            {
                if (selectedElement is not DSNode)
                {
                    continue;
                }

                DSNode node = (DSNode)selectedElement;

                group.AddElement(node);
            }

            return group;
        }

        public DSNode CreateNode(string nodeName, DSNodeType in_nodeType, Vector2 position, bool shouldDraw = true)
        {
            // Creating the node with type of the class name
            Type nodeType = Type.GetType($"DS.Elements.DS{in_nodeType}Node");
            DSNode node = (DSNode)Activator.CreateInstance(nodeType);

            node.Initialize(nodeName, this, position);

            if (shouldDraw )
            {
                node.Draw();
            }

            AddUngroupedNode(node);

            return node;
        }
        #endregion

        #region Callbacks

        private void OnElementsDeleted()
        {
            deleteSelection = (operationName, askUser) =>
            {
                Type groupType = typeof(DSGroup);
                Type edgeType = typeof(Edge);

                List<DSGroup> groupsToDelete = new List<DSGroup>();
                List<Edge> edgesToDelete = new List<Edge>();
                List<DSNode> nodesToDelete = new List<DSNode>();
                foreach (GraphElement element in selection)
                {
                    if (element is DSNode node)
                    {
                        nodesToDelete.Add(node);

                        continue;
                    }

                    if (element.GetType() == edgeType)
                    {
                        Edge edge = (Edge)element;

                        edgesToDelete.Add(edge);

                        continue;
                    }

                    if (element.GetType() != groupType)
                    {
                        continue;
                    }

                    DSGroup group = (DSGroup)element;
                    RemoveGroup(group);

                    groupsToDelete.Add(group);
                }

                // The order is important to comes first from node deletion
                foreach (DSGroup group in groupsToDelete)
                {
                    List<DSNode> groupedNodes = new List<DSNode>();

                    foreach(GraphElement element in group.containedElements)
                    {
                        if (element is not DSNode node)
                        {
                            continue;
                        }

                        DSNode groupNode = (DSNode)element;
                        groupedNodes.Add(groupNode);
                    }

                    group.RemoveElements(groupedNodes);

                    RemoveElement(group);
                }

                DeleteElements(edgesToDelete);

                foreach (DSNode node in nodesToDelete)
                {
                    if (node.Group != null)
                    {
                        node.Group.RemoveElement(node);
                    }

                    RemoveUngroupedNode(node);

                    node.DisconnectAllPorts();
                    RemoveElement(node);
                }
            };
        }

        private void OnGroupElementsAdded()
        {
            elementsAddedToGroup = (group, elements) =>
            {
                foreach (GraphElement element in elements)
                {
                    if (element is not DSNode)
                    {
                        continue;
                    }

                    DSGroup nodeGroup = (DSGroup)group;
                    DSNode node = (DSNode) element;

                    RemoveUngroupedNode(node);
                    AddGroupedNode(node, nodeGroup);
                }
            };
        }

        private void OnGroupElementsRemoved()
        {
            elementsRemovedFromGroup = (group, elements) =>
            {
                foreach (GraphElement element in elements)
                {
                    if (element is not DSNode)
                    {
                        continue;
                    }

                    DSNode node = (DSNode)element;

                    RemoveGroupedNode(node, group);
                    AddUngroupedNode(node);
                }
            };
        }

        private void OnGroupRenamed()
        {
            groupTitleChanged = (group, newTitle) =>
            {
                DSGroup dSGroup = (DSGroup)group;

                dSGroup.title = newTitle.RemoveWhitespaces().RemoveSpecialCharacters();

                if (string.IsNullOrEmpty(dSGroup.title))
                {
                    if (!string.IsNullOrEmpty(dSGroup.OldTitle))
                    {
                        RepeatedNameAmount++;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(dSGroup.OldTitle))
                    {
                        RepeatedNameAmount--;
                    }
                }

                RemoveGroup(dSGroup);

                dSGroup.OldTitle = dSGroup.title;

                AddGroup(dSGroup);
            };
        }

        private void OnGraphViewChanged()
        {
            graphViewChanged = (changes) => 
            {
                if (changes.edgesToCreate != null)
                {
                    foreach (Edge edge in changes.edgesToCreate)
                    {
                        DSNode nextNode = (DSNode)edge.input.node;

                        DSChoiceSaveData choiceData = (DSChoiceSaveData)edge.output.userData;

                        choiceData.NodeID = nextNode.ID;
                    }
                }

                if (changes.elementsToRemove != null)
                {
                    Type edgeType = typeof(Edge);

                    foreach (GraphElement element in changes.elementsToRemove)
                    {
                        if (element.GetType() != edgeType)
                        {
                            continue;
                        }

                        Edge edge = (Edge)element;
                        DSChoiceSaveData choiceData = (DSChoiceSaveData)edge.output.userData;
                        choiceData.NodeID = "";
                    }
                }

                return changes;
            };
        }

        #endregion

        #region Repeated Elements
        public void AddUngroupedNode(DSNode node)
        {
            string nodeName = node.DialogueName.ToLower();

            if (!ungroupedNodes.ContainsKey(nodeName))
            {
                DSNodeErrorData errorData = new DSNodeErrorData();
                errorData.Nodes.Add(node);

                ungroupedNodes.Add(nodeName, errorData);

                return;
            }

            List<DSNode> ungroupedNodesList = ungroupedNodes[nodeName].Nodes;

            ungroupedNodesList.Add(node);

            Color errorColor = ungroupedNodes[nodeName].ErrorData.Color;
            node.SetErrorStyle(errorColor);

            if (ungroupedNodesList.Count == 2)
            {
                RepeatedNameAmount++;
                ungroupedNodesList[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveUngroupedNode(DSNode node)
        {
            string nodeName = node.DialogueName.ToLower();
            List<DSNode> ungroupedNodesList = ungroupedNodes[nodeName].Nodes;

            ungroupedNodesList.Remove(node);

            node.ResetStyle();

            if (ungroupedNodesList.Count == 1)
            {
                RepeatedNameAmount--;
                ungroupedNodesList[0].ResetStyle();

                return;
            }

            if (ungroupedNodesList.Count == 0)
            {
                ungroupedNodes.Remove(nodeName);
            }
        }

        public void AddGroup(DSGroup group)
        {
            string name = group.title.ToLower();

            if (!groups.ContainsKey(name))
            {
                DSGroupErrorData errorData = new DSGroupErrorData();

                errorData.Groups.Add(group);

                groups.Add(name, errorData);

                return;
            }

            List<DSGroup> groupsList = groups[name].Groups;

            groupsList.Add(group);

            Color errorColor = groups[name].ErrorData.Color;
            group.SetErrorStyle(errorColor);

            if (groupsList.Count == 2)
            {
                RepeatedNameAmount++;
                groupsList[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveGroup(DSGroup group)
        {
            string oldGroupName = group.OldTitle.ToLower();
            List<DSGroup> groupsList = groups[oldGroupName].Groups;

            groupsList.Remove(group);

            group.ResetStyle();

            if (groupsList.Count == 1)
            {
                RepeatedNameAmount--;
                groupsList[0].ResetStyle();
            }

            if (groupsList.Count == 0)
            {
                groups.Remove(oldGroupName);
            }
        }

        public void AddGroupedNode(DSNode node, DSGroup group)
        {
            string nodeName = node.DialogueName.ToLower();

            node.Group = group;

            if (!groupedNodes.ContainsKey(group))
            {
                groupedNodes.Add(group, new SerializableDictionary<string, DSNodeErrorData>());
            }

            if (!groupedNodes[group].ContainsKey(nodeName))
            {
                DSNodeErrorData errorData = new DSNodeErrorData();

                errorData.Nodes.Add(node);
                groupedNodes[group].Add(nodeName, errorData);

                return;
            }

            List<DSNode> groupedNodesList = groupedNodes[group][nodeName].Nodes;

            groupedNodesList.Add(node);
            Color errorColor = groupedNodes[group][nodeName].ErrorData.Color;
            node.SetErrorStyle(errorColor);

            if (groupedNodesList.Count == 2)
            {
                RepeatedNameAmount++;
                groupedNodesList[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveGroupedNode(DSNode node, Group group)
        {
            string nodeName = node.DialogueName.ToLower();

            node.Group = null;

            List<DSNode> groupedNodesList = groupedNodes[group][nodeName].Nodes;

            groupedNodesList.Remove(node);

            node.ResetStyle();

            if (groupedNodesList.Count == 1)
            {
                RepeatedNameAmount--;
                groupedNodesList[0].ResetStyle();

                return;
            }

            if (groupedNodesList.Count == 0)
            {
                groupedNodes[group].Remove(nodeName);

                if (groupedNodes[group].Count == 0)
                {
                    groupedNodes.Remove(group);
                }
            }
        }

        #endregion

        #region Element Addition
        private void AddGridBackground()
        {
            GridBackground gridBackground = new GridBackground();
            gridBackground.StretchToParentSize();

            Insert(0, gridBackground);
        }

        private void AddSearchWindow()
        {
            if (_searchWindow == null)
            {
                _searchWindow = ScriptableObject.CreateInstance<DSSearchWindow>();

                _searchWindow.Initialize(this);
            }

            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
        }

        private void AddStyles()
        {
            this.AddStyleSheets(
                "DialogueSystem/DSGraphViewStyles.uss",
                "DialogueSystem/DSNodeStyles.uss"
                );
        }
        #endregion

        #region Utilities
        // Since the local and world pos is different we should use local in context of right positioning when creating the node etc.
        public Vector2 GetLocalMousePosition(Vector2 position, bool isSearchWindow = false)
        {
            Vector2 worldMousePosition = position;

            if (isSearchWindow)
            {
                worldMousePosition -= editorWindow.position.position;
            }

            Vector2 localMousePosition = contentViewContainer.WorldToLocal(worldMousePosition);

            return localMousePosition;
        }

        public void ClearGraph()
        {
            graphElements.ForEach(graphElement => RemoveElement(graphElement));

            groups.Clear();
            groupedNodes.Clear();
            ungroupedNodes.Clear();

            RepeatedNameAmount = 0;
        }
        #endregion

    }
}

