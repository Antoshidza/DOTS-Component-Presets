using ComponentPresets;
using Unity.Entities;
using UnityEngine;

public class ComponentPresetConverter : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField]
    private PresetType _presetType;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        ComponentCollection.AddComponents(entity, dstManager, _presetType);
    }
}
