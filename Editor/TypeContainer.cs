#if UNITY_EDITOR
using System;

namespace ComponentPresets
{
    [Serializable]
    public class TypeContainer
    {
        public string _typeName;

        public TypeContainer(Type type) => _typeName = $"{type.FullName}, {type.Assembly.FullName}";
        public Type ExtractType() => Type.GetType(_typeName);
    }
}
#endif