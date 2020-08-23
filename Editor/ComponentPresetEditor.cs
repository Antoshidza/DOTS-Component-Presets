#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Transforms;

namespace ComponentPresets
{
    [CustomEditor(typeof(ComponentPreset))]
    public class ComponentPresetEditor : Editor
    {
        private const string targetComponentsNameSpace = "FantasyTavernManager.Components";

        private static int selectedTypeIndex = 0;

        private ComponentPreset _targetData;
        private Type[] _componentTypes;

        public override VisualElement CreateInspectorGUI()
        {
            _targetData = target as ComponentPreset;
            _componentTypes = GetComponentTypes();
            return base.CreateInspectorGUI();
        }
        private Type[] GetComponentTypes()
        {
            var result = new List<Type>();
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                result.AddRange(
                    assembly
                    .GetTypes()
                    .Where(t =>
                        t.Namespace == targetComponentsNameSpace &&
                        (t.GetInterfaces().Contains(typeof(IComponentData)) ||
                        t.GetInterfaces().Contains(typeof(IBufferElementData)) ||
                        t.GetInterfaces().Contains(typeof(ISharedComponentData)))
                    ).ToArray()
                );

            //Builtin components. Add new if needed
            result.AddRange(
                new Type[]
                {
                    typeof(LocalToWorld),
                    typeof(LocalToParent),
                    typeof(WorldToLocal),
                    typeof(Rotation),
                    typeof(Translation),
                    typeof(Scale),
                    typeof(ScalePivot),
                    typeof(Frozen),
                    typeof(Parent),
                    typeof(Child),
                    typeof(Static),
                    typeof(Disabled),
                    typeof(LinkedEntityGroup)
                }
            );

            return result.ToArray();
        }
        public override void OnInspectorGUI()
        {
            #region Header
            EditorGUILayout.Space();

            if(GUILayout.Button("save"))
                Save();
            EditorGUILayout.BeginHorizontal();
            if(_componentTypes.Length == 0)
            {
                EditorGUILayout.HelpBox("there is no selected types", MessageType.Error);
                EditorGUILayout.EndHorizontal();
                return;
            }
            selectedTypeIndex = DrawSelection(_componentTypes);

            if(GUILayout.Button("+"))
                AddComponent(Activator.CreateInstance(_componentTypes[selectedTypeIndex]), _targetData);
            EditorGUILayout.EndHorizontal();
            #endregion

            #region ComponentPool
            if(_targetData.components != null)
                if(_targetData.components.Length != 0)
                    for(int i = _targetData.components.Length - 1; i >= 0; i--)
                        _targetData.components[i] = DrawComponent(_targetData.components[i], i, _targetData);
                else
                    EditorGUILayout.HelpBox("no components found", MessageType.Info);
            else
                EditorGUILayout.HelpBox("components array is null", MessageType.Info);
            #endregion
        }
        private int DrawSelection(Type[] types)
        {
            var names = new string[types.Length];
            for (int i = 0; i < types.Length; i++)
                names[i] = types[i].Name;
            return EditorGUILayout.Popup(selectedTypeIndex, names);
        }
        private void AddComponent(object component, ComponentPreset moduleData)
        {
            var moduleDataComponents = moduleData.components != null ? new List<object>(moduleData.components) : new List<object>();
            moduleDataComponents.Add(ZeroData(component) ? new TypeContainer(component.GetType()) : component);
            moduleData.components = moduleDataComponents.ToArray();
        }
        private void RemoveComponentAt(int index, ComponentPreset moduleData)
        {
            var moduleDataComponentList = new List<object>(moduleData.components);
            moduleDataComponentList.RemoveAt(index);
            moduleData.components = moduleDataComponentList.ToArray();
        }
        private object DrawComponent(object component, int index, ComponentPreset assetData)
        {
            if (component == null)
            {
                DrawErrorStub("component is null", index, assetData);
                return component;
            }
            var isTypeContainer = component is TypeContainer;
            var componentType = isTypeContainer ? (component as TypeContainer).ExtractType() : component.GetType();

            GUILayout.BeginVertical(GUI.skin.box);
            #region Component Header
            GUILayout.BeginHorizontal();
            GUILayout.Label(componentType.Name, EditorStyles.boldLabel);

            //If type of added component has fields then draw button which on click switch data to type and vice versa
            if(!ZeroData(component) && DrawDataLessSwitchButton(!isTypeContainer, componentType.Name))
                return isTypeContainer ? Activator.CreateInstance(componentType) : new TypeContainer(componentType);

            if (DrawRemoveButton(componentType.Name))
            {
                RemoveComponentAt(index, assetData);
                GUIUtility.ExitGUI();
            }
            GUILayout.EndHorizontal();
            #endregion

            if(!isTypeContainer)
                component = DrawType(component);
            GUILayout.EndVertical();
            
            return component;
        }
        private object DrawType(object obj)
        {
            if(obj is Type)
                return obj;

            var componentType = obj.GetType();
            var componentFields = componentType.GetFields();
            
            EditorGUI.indentLevel++;
            foreach (var field in componentFields)
                if (!field.IsStatic)
                    field.SetValue(obj, DrawField(field, obj));
            EditorGUI.indentLevel--;
            return obj;
        }
        private object DrawField(FieldInfo field, object obj)
        {
            if (field.FieldType == typeof(int))
                return EditorGUILayout.IntField(field.Name, (int)field.GetValue(obj));
            else if (field.FieldType == typeof(float))
                return EditorGUILayout.FloatField(field.Name, (float)field.GetValue(obj));
            else if (field.FieldType == typeof(double))
                return EditorGUILayout.DoubleField(field.Name, (double)field.GetValue(obj));
            else if (field.FieldType == typeof(long))
                return EditorGUILayout.LongField(field.Name, (long)field.GetValue(obj));
            else if (field.FieldType == typeof(Vector2))
                return EditorGUILayout.Vector2Field(field.Name, (Vector2)field.GetValue(obj));
            else if (field.FieldType == typeof(float2))
                return (float2)EditorGUILayout.Vector2Field(field.Name, (float2)field.GetValue(obj));
            else if (field.FieldType == typeof(Vector2Int))
                return EditorGUILayout.Vector2IntField(field.Name, (Vector2Int)field.GetValue(obj));
            else if (field.FieldType == typeof(Vector3))
                return EditorGUILayout.Vector3Field(field.Name, (Vector3)field.GetValue(obj));
            else if (field.FieldType == typeof(float3))
                return (float3)EditorGUILayout.Vector3Field(field.Name, (float3)field.GetValue(obj));
            else if (field.FieldType == typeof(Vector3Int))
                return EditorGUILayout.Vector3IntField(field.Name, (Vector3Int)field.GetValue(obj));
            else if (field.FieldType == typeof(Vector4))
                return EditorGUILayout.Vector4Field(field.Name, (Vector4)field.GetValue(obj));
            else if (field.FieldType == typeof(float4))
                return (float4)EditorGUILayout.Vector4Field(field.Name, (float4)field.GetValue(obj));
            else if (field.FieldType == typeof(bool))
                return EditorGUILayout.Toggle(field.Name, (bool)field.GetValue(obj));
            else if (field.FieldType.IsEnum)
                return EditorGUILayout.EnumPopup(field.Name, (Enum)field.GetValue(obj));
            else
            {
                EditorGUILayout.LabelField(field.Name);
                return DrawType(field.GetValue(obj));
            }
        }
        private bool DrawRemoveButton(string deletingComponentName = "unknown")
            => GUILayout.Button("x", GetComponentHeaderButtonStyle()) && EditorUtility.DisplayDialog("Deleting confirmation", $"Do you realy want to delete {deletingComponentName}?", "delete", "cancel");
        private bool DrawDataLessSwitchButton(bool haveData, string changeDataLessComponentName = "unknown")
        {
            var buttonStyle = GetComponentHeaderButtonStyle();
            buttonStyle.fixedWidth = 40f;
            var header = haveData ? "Change to only type confirmation" : "Change to type with data confirmation";
            var label = haveData ? "data" : "type";
            return GUILayout.Button(label, buttonStyle) && EditorUtility.DisplayDialog(header, $"Do you realy want to change {changeDataLessComponentName}?", "accept", "cancel");
        }

        private GUIStyle GetComponentHeaderButtonStyle()
        {
            var componentHeaderButtonStyle = new GUIStyle(GUI.skin.button);
            componentHeaderButtonStyle.fixedWidth = 15f;
            componentHeaderButtonStyle.fixedHeight = 15f;
            componentHeaderButtonStyle.fontSize = 10;
            componentHeaderButtonStyle.alignment = TextAnchor.MiddleCenter;
            return componentHeaderButtonStyle;
        }
        private void Save()
        {
            EditorUtility.SetDirty(_targetData);
            AssetDatabase.SaveAssets();
        }
        private void DrawErrorStub(string message, int componentIndex, ComponentPreset data)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(message, MessageType.Error);
            if(DrawRemoveButton())
                RemoveComponentAt(componentIndex, data);
            GUILayout.EndHorizontal();
        }
        /// <summary>
        /// Returns true if type of given component has no fields.
        /// </summary>
        private bool ZeroData(object component)
        {
            //if compoentn is a type by himself then will check fields of this type
            var componentType = component is TypeContainer typeContainer ? typeContainer.ExtractType() : component.GetType();
            return componentType.GetFields().Length == 0;
        }
    }
}
#endif