using UnityEngine;

namespace LauraEditor.Runtime.Separator
{
    public enum SepType
    {
        Default,
        Custom
    }

    public enum SepAlignment
    {
        Start,
        Center,
        End
    }

    public class SeparatorHeader : MonoBehaviour
    {
        public string title = "Separator";

        [HideInInspector] public SepType type;
        [HideInInspector] public SepAlignment alignment;

        private void OnDrawGizmos()
        {
            // Lock the postion
            transform.position = Vector3.zero;
        }
    }
}
