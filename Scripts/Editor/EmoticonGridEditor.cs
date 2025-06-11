using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EmoticonContainer))]
public class EmoticonGridEditor : Editor
{
    private const int GridSize = 3;
    private const float Spacing = 10f;
    private const float CellSize = 120f;
    private int _currentPage = 0;
    private int _selectedCellIndex = -1;
    private const int CellsPerPage = (GridSize * GridSize) + 4;

    private SerializedProperty selectedCellProperty;

    private void SelectCell(int index)
    {
        _selectedCellIndex = index;
        if (index != -1)
        {
            selectedCellProperty = serializedObject.FindProperty($"emoticons.Array.data[{index}]");
        }
        else
        {
            selectedCellProperty = null;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EmoticonContainer container = (EmoticonContainer)target;
        EditorGUI.BeginChangeCheck();

        // 페이지 길이가 1 미만이면 1로 설정
        if (container.pageLength < 1)
        {
            container.pageLength = 1;
            EditorUtility.SetDirty(target);
        }

        // 페이지 길이 설정 UI
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Page Length");

        // - 버튼 (1페이지 이하일 때만 비활성화)
        if (container.pageLength <= 1)
            GUI.enabled = false;
        if (GUILayout.Button("-", GUILayout.Width(25)))
        {
            bool delete = EditorUtility.DisplayDialog(
                "Warning",
                "현재 페이지가 삭제됩니다. 페이지를 삭제하면 기존의 작업 내용이 소실될 수 있습니다.",
                "삭제",
                "취소");

            if (delete)
            {
                // 현재 페이지의 데이터를 제거하고 뒤의 데이터를 앞으로 당김
                int startIndex = _currentPage * CellsPerPage;
                int endIndex = startIndex + CellsPerPage;

                // 현재 페이지 이후의 데이터를 앞으로 이동
                for (int i = startIndex; i < container.emoticons.Length - CellsPerPage; i++)
                {
                    container.emoticons[i] = container.emoticons[i + CellsPerPage];
                }

                container.pageLength--;

                // 현재 페이지가 마지막 페이지였다면 이전 페이지로 이동
                if (_currentPage >= container.pageLength)
                {
                    _currentPage = container.pageLength - 1;
                }

                SelectCell(-1);
            }
        }

        GUI.enabled = true;

        // 가운데 정렬을 위한 여백과 페이지 수 표시
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"{container.pageLength}", EditorStyles.boldLabel, GUILayout.Width(25));
        GUILayout.FlexibleSpace();

        // + 버튼
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            container.pageLength++;
            SelectCell(-1);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 배열 크기 자동 조절
        int totalSlots = container.pageLength * CellsPerPage;
        if (container.emoticons == null || container.emoticons.Length != totalSlots)
        {
            var newEmoticons = new EmoticonData[totalSlots];
            if (container.emoticons != null)
            {
                for (int i = 0; i < Mathf.Min(container.emoticons.Length, totalSlots); i++)
                {
                    newEmoticons[i] = container.emoticons[i] ?? new EmoticonData();
                }
            }

            for (int i = (container.emoticons?.Length ?? 0); i < totalSlots; i++)
            {
                newEmoticons[i] = new EmoticonData();
            }

            container.emoticons = newEmoticons;
            SelectCell(-1);
        }

        // 페이지 네비게이션
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = _currentPage > 0;
        if (GUILayout.Button("◀ Previous", GUILayout.Width(100)))
        {
            _currentPage--;
            SelectCell(-1);
            GUI.FocusControl(null);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Page {_currentPage + 1} / {container.pageLength}",
            EditorStyles.boldLabel,
            GUILayout.Width(100));
        GUILayout.FlexibleSpace();

        GUI.enabled = _currentPage < container.pageLength - 1;
        if (GUILayout.Button("Next ▶", GUILayout.Width(100)))
        {
            _currentPage++;
            SelectCell(-1);
            GUI.FocusControl(null);
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20);

        int baseIndex = _currentPage * CellsPerPage;

        // 상단 셀
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        DrawCell(container, baseIndex + 9);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(Spacing);

        // 중앙 3x3 그리드와 좌우 셀
        for (int row = 0; row < GridSize; row++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (row == 1)
            {
                DrawCell(container, baseIndex + 10);
                GUILayout.Space(Spacing);
            }
            else
            {
                GUILayout.Space(CellSize + Spacing);
            }

            for (int col = 0; col < GridSize; col++)
            {
                DrawCell(container, baseIndex + (row * GridSize) + col);
                if (col < GridSize - 1) GUILayout.Space(Spacing);
            }

            if (row == 1)
            {
                GUILayout.Space(Spacing);
                DrawCell(container, baseIndex + 11);
            }
            else
            {
                GUILayout.Space(CellSize + Spacing);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (row < GridSize - 1) EditorGUILayout.Space(Spacing);
        }

        EditorGUILayout.Space(Spacing);

        // 하단 셀
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        DrawCell(container, baseIndex + 12);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 선택된 셀 정보 표시 및 편집
        if (selectedCellProperty != null)
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // SerializedProperty를 사용하여 모든 필드를 자동으로 표시
            var iterator = selectedCellProperty.Copy();
            var endProperty = selectedCellProperty.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }

            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);

            // 씬에 있는 모든 EmoticonGridDisplay 찾아서 업데이트
            var displays = FindObjectsOfType<EmoticonGridDisplay>();
            foreach (var display in displays)
            {
                display.UpdateAllEmoticons();
            }
        }
    }

    private void DrawCell(EmoticonContainer container, int index)
    {
        bool isSelected = _selectedCellIndex == index;
        var boxStyle = new GUIStyle(EditorStyles.helpBox);
        if (isSelected)
        {
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.7f, 0.8f, 1f, 0.5f));
        }

        EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(CellSize), GUILayout.Height(CellSize));

        Rect cellRect = GUILayoutUtility.GetRect(CellSize - 10, CellSize - 10);
        if (container.emoticons[index].sprite != null)
        {
            GUI.DrawTexture(cellRect, container.emoticons[index].sprite.texture, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUI.DrawRect(cellRect, new Color(0.8f, 0.8f, 0.8f, 0.3f));
            var style = new GUIStyle(EditorStyles.miniLabel);
            style.alignment = TextAnchor.MiddleCenter;
            EditorGUI.LabelField(cellRect, "Click to Edit", style);
        }

        EditorGUILayout.EndVertical();

        var fullCellRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && fullCellRect.Contains(Event.current.mousePosition))
        {
            SelectCell(index);
            GUI.FocusControl(null);
            Event.current.Use();
            Repaint();
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
