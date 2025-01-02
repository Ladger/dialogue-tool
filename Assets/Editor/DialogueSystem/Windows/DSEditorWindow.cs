using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


namespace DS.Windows
{
    using System;
    using System.IO;
    using Utilities;

    public class DSEditorWindow : EditorWindow
    {
        private DSGraphView graphView;

        private readonly string defaultFileName = "NewDialogueFile";

        private static TextField fileNameTextField;
        private Button saveButton;

        [MenuItem("Tools/DS/Dialogue Graph")]
        public static void Open()
        {
            DSEditorWindow wnd = GetWindow<DSEditorWindow>();
            wnd.titleContent = new GUIContent("Dialogue Graph");
        }

        public void OnEnable()
        {
            AddGraphView();
            AddToolbar();

            AddStyles();
        }

        #region Elements Addition
        private void AddGraphView()
        {
            // Adding grid look to root container with constructor creation
            graphView = new DSGraphView(this);
            graphView.StretchToParentSize();

            rootVisualElement.Add(graphView);
        }

        private void AddToolbar()
        {
            Toolbar toolbar = new Toolbar();

            fileNameTextField = DSElementUtility.CreateTextField(defaultFileName, "File Name:", callback =>
            {
                fileNameTextField.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();
            });

            saveButton = DSElementUtility.CreateButton("Save", () => Save());

            Button loadButton = DSElementUtility.CreateButton("Load", () => Load());
            Button clearButton = DSElementUtility.CreateButton("Clear", () => Clear());
            Button resetButton = DSElementUtility.CreateButton("Reset", () => ResetGraph());

            toolbar.Add(fileNameTextField);
            toolbar.Add(saveButton);
            toolbar.Add(loadButton);
            toolbar.Add(clearButton);
            toolbar.Add(resetButton);

            toolbar.AddStyleSheets("DialogueSystem/DSToolbarStyles.uss");

            rootVisualElement.Add(toolbar);
        }

        private void AddStyles()
        {
            rootVisualElement.AddStyleSheets(
                "DialogueSystem/DSVariables.uss"
                );
        }
        #endregion

        #region Toolbar Actions
        private void Save()
        {
            if (string.IsNullOrEmpty(fileNameTextField.value))
            {
                EditorUtility.DisplayDialog(
                    "Invalid File Name",
                    "Please make sure the file name you typed is correct.",
                    "Okay"
                    );

                return;
            }

            DSIOUtility.Initialize(graphView, fileNameTextField.value);
            DSIOUtility.Save();
        }

        private void Load()
        {
            string filePath = EditorUtility.OpenFilePanel("Dialogue Graphs", "Assets/Editor/DialogueSystem/Graphs", "asset");

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            Clear();

            DSIOUtility.Initialize(graphView, Path.GetFileNameWithoutExtension(filePath));
            DSIOUtility.Load();
        }

        private void Clear()
        {
            graphView.ClearGraph();
        }

        private void ResetGraph()
        {
            Clear();

            UpdateFileName(defaultFileName);
        }
        #endregion

        #region Utility Methods
        public static void UpdateFileName(string newFileName)
        {
            fileNameTextField.value = newFileName;
        }

        public void EnableSaving() { saveButton.SetEnabled(true); }
        public void DiasableSaving() { saveButton.SetEnabled(false); }
        #endregion
    }
}

