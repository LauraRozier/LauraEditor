using LauraEditor.Runtime.Separator;
using UnityEditor;
using UnityEngine;

namespace LauraEditor.Editor.Separator
{
    [CustomEditor(typeof(SeparatorHeader))]
    public class Editor : UnityEditor.Editor
    {
        public void OnUndoRedo() => Update(target as SeparatorHeader, null, true);

        private void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;
        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        public static string GetSimpleTitle(string prefix, string title) =>
            prefix == null || prefix.Length <= 0
                ? title
                : GetSimpleTitle(prefix[0], title);

        public static string GetSimpleTitle(char prefix, string title)
        {
            var maxCharLength = Config.SepConfig.MaxLength;
            var charLength = maxCharLength - title.Length;
            var leftSize = 0;
            var rightSize = 0;

            switch (Config.SepConfig.Alignment.value)
            {
            case SepAlignment.Start:
                {
                    leftSize = Config.SepConfig.MinFillLength;
                    rightSize = charLength - leftSize;
                    break;
                }
            case SepAlignment.End:
                {
                    rightSize = Config.SepConfig.MinFillLength;
                    leftSize = charLength - rightSize;
                    break;
                }
            case SepAlignment.Center:
                {
                    leftSize = charLength / 2;
                    rightSize = charLength / 2;
                    break;
                }
            }

            string left = leftSize > 0 ? new string(prefix, leftSize) : "";
            string right = rightSize > 0 ? new string(prefix, rightSize) : "";
            return $"{left} {title.ToUpper()} {right}";
        }

        public static string GetFormattedTitle(string title) =>
            Config.SepConfig.FillType.value == SepType.Custom
                ? GetSimpleTitle(Config.SepConfig.CustomFill, title)
                : GetSimpleTitle('━', title);

        public static void Update(SeparatorHeader header, string title = null, bool markAsDirty = false)
        {
            header.name = GetFormattedTitle(title ?? header.title);

            if (markAsDirty)
                EditorUtility.SetDirty(header);
        }

        public static void UpdateAll()
        {
            var targetType = Config.SepConfig.FillType;
            var targetAlignment = Config.SepConfig.Alignment;
            var allHeader = FindObjectsOfType<SeparatorHeader>();

            foreach (var header in allHeader)
            {
                header.type = targetType;
                header.alignment = targetAlignment;
                Update(header, null, true);
            }
        }

        public override void OnInspectorGUI()
        {
            var typeProperty = serializedObject.FindProperty("type");
            var header = target as SeparatorHeader;

            serializedObject.Update();

            var titleProperty = serializedObject.FindProperty("title");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(titleProperty);

            if (EditorGUI.EndChangeCheck())
            {
                Update(header, titleProperty.stringValue, false);
                // Refresh the hierarchy to reflect the new name
                EditorApplication.RepaintHierarchyWindow();
            }

            // Sync current header with settings
            if ((SepType)typeProperty.enumValueIndex != Config.SepConfig.FillType.value)
            {
                typeProperty.enumValueIndex = (int)Config.SepConfig.FillType.value;
                Update(header, null, false);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Options"))
                    SettingsService.OpenUserPreferences(Config.k_PreferencesPath);

                if (GUILayout.Button("Refresh"))
                    UpdateAll();

                if (GUILayout.Button("Create Empty"))
                {
                    var obj = new GameObject("Empty");
                    obj.transform.SetSiblingIndex((target as SeparatorHeader).transform.GetSiblingIndex() + 1);
                    Undo.RegisterCreatedObjectUndo(obj, "Create Empty");
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
