using UnityEditor;
using UnityEngine;
using LauraEditor.Runtime.Separator;

namespace LauraEditor.Editor.Separator
{
    public static class MenuItems
    {
        [MenuItem("GameObject/Create Separator", false, 0)]
        public static void CreateHeader()
        {
            // Mark as EditorOnly, so it will not included in final build
            GameObject separator = new GameObject { tag = "EditorOnly" };
            // Hide the transform
            separator.transform.hideFlags = HideFlags.NotEditable | HideFlags.HideInInspector;
            separator.AddComponent<SeparatorHeader>();

            // Update the header
            Editor.Update(separator.GetComponent<SeparatorHeader>());

            // Register undo
            Undo.RegisterCreatedObjectUndo(separator, "Create Separator");
            // Select the created header
            Selection.activeGameObject = separator;
        }
    }
}
