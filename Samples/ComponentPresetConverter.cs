using System;
using Unity.Entities;
using UnityEngine;

namespace ComponentPresets
{
    public class ComponentPresetConverter : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField]
        private string _componentPresetName;

        public virtual void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            ComponentCollection.AddComponents(entity, dstManager, (PresetType)Enum.Parse(typeof(PresetType), _componentPresetName));
        }
    }
}
