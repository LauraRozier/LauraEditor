#if UNITY_EDITOR
using LauraEditor.Runtime.Separator;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace LauraEditor.Editor.Separator
{
    [CustomEditor(typeof(SeparatorHeader))]
    public class Editor : UnityEditor.Editor
    {
        void OnUndoRedo() => Update(target as SeparatorHeader, null, true);
        void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;
        void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        public static string GetSimpleTitle(string prefix, string title) =>
            null == prefix || prefix.Length <= 0
                ? title
                : GetSimpleTitle(prefix[0], title);

        public static string GetSimpleTitle(char prefix, string title)
        {
            UserSetting<int> maxCharLength = Config.SepConfig.MaxLength;
            int charLength = maxCharLength - title.Length;
            int leftSize = 0;
            int rightSize = 0;

            switch (Config.SepConfig.Alignment.value) {
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
            UserSetting<SepType> targetType = Config.SepConfig.FillType;
            UserSetting<SepAlignment> targetAlignment = Config.SepConfig.Alignment;
            SeparatorHeader[] allHeader = FindObjectsOfType<SeparatorHeader>();

            foreach (SeparatorHeader header in allHeader) {
                header.type = targetType;
                header.alignment = targetAlignment;
                Update(header, null, true);
            }
        }

        public override void OnInspectorGUI()
        {
            SerializedProperty typeProperty = serializedObject.FindProperty("type");
            SeparatorHeader header = target as SeparatorHeader;

            serializedObject.Update();

            SerializedProperty titleProperty = serializedObject.FindProperty("title");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(titleProperty);

            if (EditorGUI.EndChangeCheck()) {
                Update(header, titleProperty.stringValue, false);
                // Refresh the hierarchy to reflect the new name
                EditorApplication.RepaintHierarchyWindow();
            }

            // Sync current header with settings
            if ((SepType)typeProperty.enumValueIndex != Config.SepConfig.FillType.value) {
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

                if (GUILayout.Button("Create Empty")) {
                    GameObject obj = new GameObject("Empty");
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
#endif
