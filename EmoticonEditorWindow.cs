using UnityEngine;
using UnityEditor;

public class EmoticonEditorWindow : EditorWindow
{
    private EmoticonContainer _selectedContainer;
    private Editor _containerEditor;
    private Vector2 _scrollPosition;
    private const string DefaultContainerPath = "Assets/Mingle/Dev/KSK_Test/10.ScriptableObjects/EmoticonContainer.asset";
    
    [MenuItem("Window/Emoticon Editor")]
    public static void ShowWindow()
    {
        GetWindow<EmoticonEditorWindow>("Emoticon Editor");
    }

    private void OnEnable()
    {
        _selectedContainer = AssetDatabase.LoadAssetAtPath<EmoticonContainer>(DefaultContainerPath); 
    }
    
    void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        _selectedContainer = EditorGUILayout.ObjectField("Emoticon Container", _selectedContainer, typeof(EmoticonContainer), false) as EmoticonContainer;

        if (_selectedContainer)
        {
            if (!_containerEditor) _containerEditor = Editor.CreateEditor(_selectedContainer);

            _containerEditor.OnInspectorGUI();

            if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(_selectedContainer);
                AssetDatabase.SaveAssets();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void OnDestroy()
    {
        if (_containerEditor != null)
            DestroyImmediate(_containerEditor);
    }
}