#if UNITY_EDITOR
using UnityEngine;

namespace ComponentPresets
{
    [CreateAssetMenu(fileName = "NewComponentPreset", menuName = "ComponentPreset")]
    public class ComponentPreset : ScriptableObject
    {
        [HideInInspector]
        [SerializeReference]
        public object[] components;
    }
    internal class UnserializableInstanceContainer
    {
        public string objectString;
    }
}
#endif