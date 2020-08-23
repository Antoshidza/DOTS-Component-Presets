//This file is generated so don't change it manually.
using System;
using UnityEngine;
using Unity.Entities;

namespace ComponentPresets
{
	public enum PresetType
	{
		Test
	}
    public static class ComponentCollection
    {
		private static Action<Entity, EntityManager>[] _addComponentActions;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Initialize()
		{
			_addComponentActions = new Action<Entity, EntityManager>[1];
			_addComponentActions[0] = (Entity entity, EntityManager entityManager) => {
				entityManager.AddComponentData(entity, new Unity.Transforms.LocalToWorld { Value = new Unity.Mathematics.float4x4 { c0 = new Unity.Mathematics.float4 { x = 0f, y = 0f, z = 0f, w = 0f }, c1 = new Unity.Mathematics.float4 { x = 0f, y = 0f, z = 0f, w = 0f }, c2 = new Unity.Mathematics.float4 { x = 0f, y = 0f, z = 0f, w = 0f }, c3 = new Unity.Mathematics.float4 { x = 0f, y = 0f, z = 0f, w = 0f } } });
				entityManager.AddComponentData(entity, new Unity.Transforms.Translation { Value = new Unity.Mathematics.float3 { x = 0f, y = 0f, z = 0f } });
				entityManager.AddComponentData(entity, new Unity.Transforms.Frozen());};
		}
        public static void AddComponents(Entity entity, EntityManager entityManager, PresetType presetType)
			=> _addComponentActions[(int)presetType].Invoke(entity, entityManager);
    }
}