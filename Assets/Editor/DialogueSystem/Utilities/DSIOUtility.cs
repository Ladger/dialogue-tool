using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;

namespace DS.Utilities
{
    using Data.Save;
    using DS.Data;
    using DS.ScriptableObjects;
    using Elements;
    using Windows;

    public static class DSIOUtility
    {
        private static DSGraphView graphView;
        private static string graphFileName;
        private static string containerFolderPath;

        private static List<DSGroup> groups;
        private static List<DSNode> nodes;

        private static Dictionary<string, DSDialogueGroupSO> createdDialogueGroups;
        private static Dictionary<string, DSDialogueSO> createdDialogues;

        private static Dictionary<string, DSGroup> loadedGroups;
        private static Dictionary <string, DSNode> loadedNodes;

        public static void Initialize(DSGraphView dsGraphView, string graphName)
        {
            graphFileName = graphName;
            containerFolderPath = $"Assets/DialogueSystem/Dialogues/{graphFileName}";

            graphView = dsGraphView;

            groups = new List<DSGroup>();
            nodes = new List<DSNode>();

            createdDialogueGroups = new Dictionary<string, DSDialogueGroupSO> ();
            createdDialogues = new Dictionary<string, DSDialogueSO> ();

            loadedGroups = new Dictionary<string, DSGroup> ();
            loadedNodes = new Dictionary<string, DSNode> ();
        }

        #region Save Methods
        public static void Save()
        {
            CreateStaticFolders();

            GetElementsFromGraphView();

            DSGraphSaveDataSO graphData = CreateAsset<DSGraphSaveDataSO>("Assets/Editor/DialogueSystem/Graphs", $"{graphFileName}Graph");
            graphData.Initialize(graphFileName);

            DSDialogueContainerSO dialogueContainerSO = CreateAsset<DSDialogueContainerSO>(containerFolderPath, graphFileName);
            dialogueContainerSO.Initialize(graphFileName);

            SaveGroups(graphData, dialogueContainerSO);
            SaveNodes(graphData, dialogueContainerSO);

            SaveAsset(graphData);
            SaveAsset(dialogueContainerSO);
        }
        #endregion

        #region Save Node Methods
        private static void SaveNodes(DSGraphSaveDataSO graphData, DSDialogueContainerSO dialogueContainerSO)
        {
            SerializableDictionary<string, List<string>> groupedNodeNames = new SerializableDictionary<string, List<string>> ();
            List<string> ungroupedNodeNames = new List<string> ();
            foreach (DSNode node in nodes)
            {
                SaveNodeToGraph(graphData, node);
                SaveNodeToScriptableObject(dialogueContainerSO, node);

                if (node.Group != null)
                {
                    groupedNodeNames.AddItem(node.Group.title, node.DialogueName);

                    continue;
                }

                ungroupedNodeNames.Add(node.DialogueName);
            }

            UpdateDialoguesChoicesConnections();

            UpdateOldGroupedNodes(graphData, groupedNodeNames);
            UpdateOldUngroupedNodes(graphData, ungroupedNodeNames);
            
        }

        private static void SaveNodeToGraph(DSGraphSaveDataSO graphData, DSNode node)
        {
            List<DSChoiceSaveData> choices = CloneNodeChoices(node.Choices);

            DSNodeSaveData nodeData = new DSNodeSaveData()
            {
                ID = node.ID,
                Name = node.DialogueName,
                Choices = choices,
                Text = node.Text,
                GroupID = node.Group?.ID,
                DialogueType = node.DialogueType,
                NodeType = node.NodeType,
                Position = node.GetPosition().position
            };

            graphData.Nodes.Add(nodeData);
        }

        private static void SaveNodeToScriptableObject(DSDialogueContainerSO dialogueContainerSO, DSNode node)
        {
            DSDialogueSO dialogue;
            if (node.Group != null)
            {
                dialogue = CreateAsset<DSDialogueSO>($"{containerFolderPath}/Groups/{node.Group.title}/Dialogues", node.DialogueName);

                dialogueContainerSO.DialogueGroups.AddItem(createdDialogueGroups[node.Group.ID], dialogue);
            }
            else
            {
                dialogue = CreateAsset<DSDialogueSO>($"{containerFolderPath}/Global/Dialogues", node.DialogueName);

                dialogueContainerSO.UngroupedDialogues.Add(dialogue);
            }

            dialogue.Initialize(
                node.DialogueName,
                node.Text,
                ConvertNodeChoicesToDialogueChoices(node.Choices),
                node.DialogueType,
                node.NodeType,
                node.IsStartingNode()
            );

            createdDialogues.Add(node.ID, dialogue);
            SaveAsset(dialogue);
        }

        private static List<DSDialogueChoiceData> ConvertNodeChoicesToDialogueChoices(List<DSChoiceSaveData> choices)
        {
            List<DSDialogueChoiceData> dialogueChoices = new List<DSDialogueChoiceData>();

            foreach (DSChoiceSaveData choice in choices)
            {
                DSDialogueChoiceData data = new DSDialogueChoiceData()
                {
                    Text = choice.Text,
                };

                dialogueChoices.Add(data);
            }

            return dialogueChoices;
        }

        private static void UpdateDialoguesChoicesConnections()
        {
            foreach (DSNode node in nodes)
            {
                DSDialogueSO dialogue = createdDialogues[node.ID];

                for (int i = 0; i < node.Choices.Count; i++)
                {
                    DSChoiceSaveData choice = node.Choices[i];

                    if (string.IsNullOrEmpty(choice.NodeID))
                    {
                        continue;
                    }

                    dialogue.Choices[i].NextDialogue = createdDialogues[choice.NodeID];

                    SaveAsset(dialogue);
                }
            }
        }

        private static void UpdateOldGroupedNodes(DSGraphSaveDataSO graphData, SerializableDictionary<string, List<string>> groupedNodeNames)
        {
            if (graphData.OldGroupedNodeNames != null && graphData.OldGroupedNodeNames.Count != 0)
            {
                foreach (KeyValuePair<string, List<string>> oldGroupedNode in graphData.OldGroupedNodeNames)
                {
                    List<string> nodesToRemove = new List<string>();

                    if (groupedNodeNames.ContainsKey(oldGroupedNode.Key))
                    {
                        nodesToRemove = oldGroupedNode.Value.Except(groupedNodeNames[oldGroupedNode.Key]).ToList();
                    }

                    foreach(string nodeToRemove in nodesToRemove)
                    {
                        RemoveAsset($"{containerFolderPath}/Groups/{oldGroupedNode.Key}/Dialogues", nodeToRemove);
                    }
                }
            }

            graphData.OldGroupedNodeNames = new SerializableDictionary<string, List<string>>(groupedNodeNames);
        }

        private static void UpdateOldUngroupedNodes(DSGraphSaveDataSO graphData, List<string> ungroupedNodeNames)
        {
            if (graphData.OldUngroupedNodeNames != null && graphData.OldUngroupedNodeNames.Count != 0) 
            {
                List<string> nodesToRemove = graphData.OldUngroupedNodeNames.Except(ungroupedNodeNames).ToList();
                foreach (string nodeName in nodesToRemove)
                {
                    RemoveAsset($"{containerFolderPath}/Global/Dialogues", nodeName);
                }
            }

            graphData.OldUngroupedNodeNames = new List<string>(ungroupedNodeNames);
        }

        
        #endregion

        #region Save Group Methods
        private static void SaveGroups(DSGraphSaveDataSO graphData, DSDialogueContainerSO dialogueContainerSO)
        {
            List<string> groupNames = new List<string>();

            foreach (DSGroup group in groups)
            {
                SaveGroupToGraph(group, graphData);
                SaveGrouptoScriptableObject(group, dialogueContainerSO);

                groupNames.Add(group.title);
            }

            UpdateOldGroups(groupNames, graphData);
        }

        private static void SaveGroupToGraph(DSGroup group, DSGraphSaveDataSO graphData)
        {
            DSGroupSaveData groupData = new DSGroupSaveData()
            {
                ID = group.ID,
                Name = group.name,
                Position = group.GetPosition().position
            };

            graphData.Groups.Add(groupData);
        }

        private static void SaveGrouptoScriptableObject(DSGroup group, DSDialogueContainerSO dialogueContainerSO)
        {
            string groupName = group.title;
            CreateFolder($"{containerFolderPath}/Groups", groupName);
            CreateFolder($"{containerFolderPath}/Groups/{groupName}", "Dialogues");

            DSDialogueGroupSO dialogueGroup = CreateAsset<DSDialogueGroupSO>($"{containerFolderPath}/Groups/{groupName}", groupName);
            dialogueGroup.Initialize(groupName);

            createdDialogueGroups.Add(group.ID, dialogueGroup);

            dialogueContainerSO.DialogueGroups.Add(dialogueGroup, new List<DSDialogueSO>());

            SaveAsset(dialogueGroup);
        }

        private static void UpdateOldGroups(List<string> currentGroupsNames, DSGraphSaveDataSO graphData)
        {
            if (graphData.OldGroupNames != null && graphData.OldGroupNames.Count != 0)
            {
                List<string> groupsToRemove = graphData.OldGroupNames.Except(currentGroupsNames).ToList();

                foreach (string groupName in groupsToRemove)
                {
                    RemoveFolder($"{containerFolderPath}/Groups/{groupName}");
                }
            }

            graphData.OldGroupNames = new List<string>(currentGroupsNames);

        }
        #endregion

        #region Load Methods
        public static void Load()
        {
            DSGraphSaveDataSO graphData = LoadAsset<DSGraphSaveDataSO>("Assets/Editor/DialogueSystem/Graphs", graphFileName);

            if (graphData == null) 
            {
                EditorUtility.DisplayDialog(
                    "Couldn't Load the File",
                    "Please check the path you use to call the graph. \n" + $"Assets/Editor/DialogueSystem/Graphs/{graphFileName}",
                    "Okay");

                return;
            }

            DSEditorWindow.UpdateFileName(graphData.FileName);

            LoadGroups(graphData.Groups);
            LoadNodes(graphData.Nodes);
            LoadNodesConnections();
        }

        private static void LoadGroups(List<DSGroupSaveData> groups)
        {
            foreach (DSGroupSaveData groupData in groups)
            {
                DSGroup group = graphView.CreateGroup(groupData.Name, groupData.Position);

                group.ID = groupData.ID;

                loadedGroups.Add(group.ID, group);
            }
        }

        private static void LoadNodes(List<DSNodeSaveData> nodes)
        {
            foreach (DSNodeSaveData nodeData in nodes)
            {
                List<DSChoiceSaveData> choices = CloneNodeChoices(nodeData.Choices);
                DSNode node = graphView.CreateNode(nodeData.Name, nodeData.NodeType, nodeData.Position, false);

                node.ID = nodeData.ID;
                node.Choices = choices;
                node.Text = nodeData.Text;
                node.DialogueType = nodeData.DialogueType;

                node.Draw();

                graphView.AddElement(node);

                loadedNodes.Add(node.ID, node);

                if (string.IsNullOrEmpty(nodeData.GroupID))
                {
                    continue;
                }

                DSGroup group = loadedGroups[nodeData.GroupID];

                node.Group = group;

                group.AddElement(node);
            }
        }

        private static void LoadNodesConnections()
        {
            foreach (KeyValuePair<string, DSNode> loadedNode in loadedNodes)
            {
                foreach (Port choicePort in loadedNode.Value.outputContainer.Children())
                {
                    DSChoiceSaveData choiceData = (DSChoiceSaveData)choicePort.userData;

                    if (string.IsNullOrEmpty(choiceData.NodeID)) { continue; }

                    DSNode nextNode = loadedNodes[choiceData.NodeID];

                    Port nextNodeInputPort = (Port)nextNode.inputContainer.Children().First();

                    Edge edge = choicePort.ConnectTo(nextNodeInputPort);

                    graphView.AddElement(edge);

                    loadedNode.Value.RefreshPorts();
                }
            }
        }
        #endregion

        #region Creation Methods
        private static void CreateStaticFolders()
        {
            CreateFolder("Assets/Editor/DialogueSystem", "Graphs");

            CreateFolder("Assets", "DialogueSystem");
            CreateFolder("Assets/DialogueSystem", "Dialogues");

            CreateFolder("Assets/DialogueSystem/Dialogues", graphFileName);
            CreateFolder(containerFolderPath, "Global");
            CreateFolder(containerFolderPath, "Groups");
            CreateFolder($"{containerFolderPath}/Global", "Dialogues");
        }
        #endregion

        #region Fetch Methods
        private static void GetElementsFromGraphView()
        {
            Type type = typeof(DSGroup);
            graphView.graphElements.ForEach(graphElement =>
            {
                if (graphElement is DSNode node)
                {
                    nodes.Add(node);

                    return;
                }

                if (graphElement.GetType() == type)
                {
                    DSGroup group = (DSGroup)graphElement;
                    groups.Add(group);

                    return;
                }
            });
        }
        #endregion

        #region Utility Methods
        private static void CreateFolder(string path, string folderName)
        {
            if (AssetDatabase.IsValidFolder($"{path}/{folderName}"))
            {
                return;
            }

            AssetDatabase.CreateFolder(path, folderName);
        }

        private static void RemoveFolder(string fullPath)
        {
            FileUtil.DeleteFileOrDirectory($"{fullPath}.meta");
            FileUtil.DeleteFileOrDirectory($"{fullPath}/");
        }


        private static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            T asset = LoadAsset<T>(path, assetName);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();

                AssetDatabase.CreateAsset(asset, $"{path}/{assetName}.asset");
            }

            return asset;
        }

        private static T LoadAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            return AssetDatabase.LoadAssetAtPath<T>($"{path}/{assetName}.asset");
        }

        private static void RemoveAsset(string path, string assetName)
        {
            AssetDatabase.DeleteAsset($"{path}/{assetName}.asset");
        }

        private static void SaveAsset(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static List<DSChoiceSaveData> CloneNodeChoices(List<DSChoiceSaveData> nodeChoices)
        {
            List<DSChoiceSaveData> choices = new List<DSChoiceSaveData>();

            foreach (DSChoiceSaveData choice in nodeChoices)
            {
                DSChoiceSaveData choiceData = new DSChoiceSaveData()
                {
                    Text = choice.Text,
                    NodeID = choice.NodeID,
                };

                choices.Add(choiceData);
            }

            return choices;
        }
        #endregion
    }
}

