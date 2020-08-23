using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Entities;
using System;
using USerialization;
using System.Globalization;
using System.Collections.Generic;
using StringExtensionsForCodegen;

namespace ComponentPresets
{
    internal class ComponentPresetCodeGenerator : EditorWindow
    {
        private enum ComponentKind
        {
            IComponentData,
            ISharedComponentData,
            IBufferElementData
        }

        private const string InsidePackageTemplateFilePath = "/Editor/ScriptTemplates/ComponentPresetCodegenTemplate.txt";
        private const string PackageFolderPartitialName = "com.tonymax.dots-component-presets";
        private const string AddToActionsLineTemplate = "_addComponentActions[<#index#>] = <#item#>;";
        private const string DeclareActionTemplate = "(Entity entity, EntityManager entityManager) => {<#action logic#>}";
        private const string AddComponentDataTemplate = "entityManager.AddComponentData(entity, <#component#>);";
        private const string AddBufferTemplate = "var <#buffer#>Buffer = entityManager.AddBuffer<<#component#>>(entity);";
        private const string AddSharedComponentDataTemplate = "entityManager.AddSharedComponentData(entity, <#component#>);";
        private const string DeclareNewComponentTemplate = "new <#name#> { <#properties#> }";
        private const string DeclareNewEmptyComponentTemplate = "new <#name#>()";
        private const string AddElementToBufferTemplate = "<#buffer#>Buffer.Add(<#component#>);";

        public ComponentPreset[] componentPresets;
        private List<object> _bufferElementsToAdd;
        private string _currentGeneratedFilePath;

        private List<object> BufferElementsToAdd
        {
            get
            {
                if(_bufferElementsToAdd == null)
                    _bufferElementsToAdd = new List<object>();
                return _bufferElementsToAdd;
            }
        }

        [MenuItem("Window/Component preset code generator")]
        public static void ShowWindow() 
            => GetWindow<ComponentPresetCodeGenerator>("Component preset code generator");
        private void OnGUI()
        {
            GUILayout.Space(20f);
            GUILayout.BeginHorizontal();

            _currentGeneratedFilePath = EditorGUILayout.TextField("Path", _currentGeneratedFilePath);

            if(GUILayout.Button("pick folder"))
                _currentGeneratedFilePath = EditorUtility.OpenFolderPanel("Choose path for generated file", "Assets", string.Empty);

            //Draw "Generate" button
            if(GUILayout.Button("Generate") &&
                EditorUtility.DisplayDialog("Generate confirmation", "This will generate ComponentAggregator class", "apply", "cancel"))
                    Generate();

            GUILayout.EndVertical();

            //Draw component collection asset list
            var serializedObject = new SerializedObject(this as ScriptableObject);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("componentPresets"), true); // True means show children
            serializedObject.ApplyModifiedProperties();
        }
        private void Generate()
        {
            var componentAggregatorTemplateText = File.ReadAllText(GetTemplatePath());
            var resultString = string.Empty;
            for(int i = 0; i < componentPresets.Length; i++)
                resultString += "\n" + GetComponentPresetAddString(componentPresets[i], i, offset: 3);
            var result = componentAggregatorTemplateText.
                Replace("using".GetMark(), string.Empty).
                Replace("enum".GetMark(), GetEnumLines(offset: 2)).
                Replace("preset types count".GetMark(), componentPresets.Length.ToString()).
                Replace("add component logic".GetMark(), resultString);

            BufferElementsToAdd.Clear();

            File.WriteAllText(_currentGeneratedFilePath + "/ComponentCollection.cs", result);
            AssetDatabase.Refresh();
        }
        private string GetTemplatePath()
        {
            var packagesDirectoryInfo = new DirectoryInfo("Packages");
            var filesAndDirs = packagesDirectoryInfo.GetFileSystemInfos("*" + PackageFolderPartitialName + "*");

            if(filesAndDirs.Length == 0)
                throw new Exception("Can't find folder of DOTS Component Presets package");
            if(filesAndDirs.Length > 1)
                throw new Exception($"There must be only one package with name like {PackageFolderPartitialName}");

            return filesAndDirs[0].FullName + InsidePackageTemplateFilePath;
        }
        private string GetComponentPresetAddString(ComponentPreset preset, int index, int offset = 0)
        {
            var resultAddLogicLines = string.Empty;
            foreach(var item in preset.components)
            {
                if(GetComponentKind(item) == ComponentKind.IBufferElementData)
                    BufferElementsToAdd.Add(item);
                else
                    resultAddLogicLines += "\n" + GetAddComponentLine(item).WithOffset(offset + 1);
            }
            resultAddLogicLines += GetAddElementToBufferLines(offset + 1);
            var actionString = DeclareActionTemplate.Replace("action logic".GetMark(), resultAddLogicLines);
            return AddToActionsLineTemplate
                .WithOffset(offset)
                .Replace("index".GetMark(), index.ToString())
                .Replace("item".GetMark(), actionString);
        }
        private string GetAddComponentLine(object component)
        {
            var componentKind = GetComponentKind(component);
            var addTemplate = GetTemplateForComponent(componentKind);
            return GetAddLine(component, addTemplate);
        }
        private ComponentKind GetComponentKind(object component)
        {
            if(component is TypeContainer typeContainer)
            {
                var componentType = typeContainer.ExtractType();
                if(IsImplementInterface(componentType, typeof(IComponentData)))
                    return ComponentKind.IComponentData;
                else if(IsImplementInterface(componentType, typeof(ISharedComponentData)))
                    return ComponentKind.ISharedComponentData;
                else if(IsImplementInterface(componentType, typeof(IBufferElementData)))
                    return ComponentKind.IBufferElementData;

                throw new Exception($"Component ({componentType.Name}) object must be IComponentData/ISharedComponentData/IBufferElementData");
            }

            if(component is IComponentData)
                return ComponentKind.IComponentData;
            else if(component is ISharedComponentData)
                return ComponentKind.ISharedComponentData;
            else if(component is IBufferElementData)
                return ComponentKind.IBufferElementData;

            throw new Exception($"Component ({component.GetType().Name}) object must be IComponentData/ISharedComponentData/IBufferElementData");
        }
        private string GetTemplateForComponent(ComponentKind componentKind)
        {
            switch(componentKind)
            {
                case ComponentKind.IComponentData:
                    return AddComponentDataTemplate;
                case ComponentKind.ISharedComponentData:
                    return AddSharedComponentDataTemplate;
                case ComponentKind.IBufferElementData:
                    return AddBufferTemplate;
            }
            throw new Exception($"There is no option for {componentKind}");
        }
        private string GetAddLine(object component, string addTemplate)
        {
            var declareComponentLine = component is TypeContainer typeContainer ? 
                GetNewDataLessComponentDeclarationString(typeContainer.ExtractType()) :
                GetNewComponentDataDeclarationString(component);

            return addTemplate.Replace("component".GetMark(), declareComponentLine);
        }
        private string GetNewComponentDataDeclarationString(object component)
        {
            var type = component.GetType();
            //_usedNamespaces.Add(type.Namespace);
            var notStaticFields = USerializer.GetNotStaticFields(type);
            var propertiesString = string.Empty;

            for(int i = 0; i < notStaticFields.Length; i++)
            {
                var field = notStaticFields[i];
                propertiesString += field.Name + " = ";
                if(field.FieldType.IsPrimitive)
                {
                    var addition = field.GetValue(component).ToString();
                    if(field.FieldType == typeof(bool))
                        addition = addition.ToLower();
                    else if(field.FieldType == typeof(float))
                    {
                        var value = float.Parse(addition);
                        var numberFormatInfo = new NumberFormatInfo();
                        numberFormatInfo.NumberDecimalSeparator = ".";
                        addition = value.ToString(numberFormatInfo) + "f";
                    }
                    propertiesString += addition;
                }
                else if(field.FieldType.IsEnum)
                    propertiesString += field.FieldType.FullName + "." + field.GetValue(component).ToString();
                else
                    propertiesString += GetNewComponentDataDeclarationString(field.GetValue(component));

                if(i < notStaticFields.Length - 1)
                    propertiesString += ", ";
            }

            return DeclareNewComponentTemplate.
                Replace("name".GetMark(), component.GetType().FullName).
                Replace("properties".GetMark(), propertiesString);
        }
        private string GetNewDataLessComponentDeclarationString(object component) 
            => DeclareNewEmptyComponentTemplate.Replace("name".GetMark(), (component as Type).FullName);
        private string GetEnumLines(int offset = 0)
        {
            var result = string.Empty;
            for(int i = 0; i < componentPresets.Length; i++)
            {
                var enumLine = componentPresets[i].name + (i < componentPresets.Length - 1 ? ",\n" : string.Empty);
                result += enumLine.WithOffset(offset);
            }
            return result;
        }
        private bool IsImplementInterface(Type type, Type interfaceType) => new List<Type>(type.GetInterfaces()).Contains(interfaceType);
        private string GetAddElementToBufferLines(int offset = 0)
        {
            var resultString = string.Empty;
            var accountedBufferTypes = new List<Type>();

            foreach(var component in BufferElementsToAdd)
            {
                var componentType = component is TypeContainer typeContainer ? typeContainer.ExtractType() : component.GetType();
                if(!accountedBufferTypes.Contains(componentType))
                {
                    var addBufferLine = AddBufferTemplate
                        .Replace("component".GetMark(), componentType.FullName)
                        .Replace("buffer".GetMark(), componentType.FullName.Replace(".", string.Empty).ToLower())
                        .WithOffset(offset);
                    resultString += "\n" + addBufferLine;
                    accountedBufferTypes.Add(componentType);
                }

                if(!(component is TypeContainer))
                {
                    var declareComponentString = GetNewComponentDataDeclarationString(component);
                    var elementAddLine = AddElementToBufferTemplate
                        .Replace("buffer".GetMark(), componentType.FullName.Replace(".", string.Empty).ToLower())
                        .Replace("component".GetMark(), declareComponentString)
                        .WithOffset(offset);
                    resultString += "\n" + elementAddLine;
                }
            }

            BufferElementsToAdd.Clear();

            return resultString;
        }
    }
}