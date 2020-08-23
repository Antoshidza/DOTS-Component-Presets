#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace USerialization
{
    public static class USerializer
    {
        private const string serializationLineSeparator = "<(n)>";
        private const string serializationFieldInfoSeparator = "<(v)>";

        public static bool CanBeFullySerialized(Type type)
        {
            if(type.IsPrimitive)
                return true;

            var result = true;
            foreach(var field in GetNotStaticFields(type))
            {
                if(!field.FieldType.IsSerializable)
                    return false;
                var nestedFields = field.FieldType.GetFields();
                if(nestedFields.Length != 0)
                    result &= CanBeFullySerialized(field.FieldType);
            }
            return result;
        }
        public static FieldInfo[] GetNotStaticFields(Type type)
        {
            var fields = type.GetFields();
            var notStaticFields = new List<FieldInfo>();
            foreach(var field in fields)
                if(!field.IsStatic)
                    notStaticFields.Add(field);
            return notStaticFields.ToArray();
        }
        public static string Serialize(object obj)
        {
            if(obj == null)
                throw new Exception("Object you trying serialize is null");

            var type = obj.GetType();
            var resultString = type.FullName + ", " + type.Assembly.FullName;
            var notStaticFields = GetNotStaticFields(type);

            for(int i = 0; i < notStaticFields.Length; i++)
            {
                var field = notStaticFields[i];
                resultString += serializationLineSeparator + field.Name + serializationFieldInfoSeparator;
                if(field.FieldType.IsPrimitive)
                    resultString += field.GetValue(obj).ToString();
                else
                {
                    var innerLinesCount = GetTotalSerializationLinesCount(field.FieldType);
                    if(innerLinesCount > 1)
                        resultString += innerLinesCount + serializationLineSeparator + Serialize(field.GetValue(obj));
                }
            }
            return resultString;
        }
        public static object Deserialize(string objectString)
        {
            var lines = objectString.Split(new string[] { serializationLineSeparator }, StringSplitOptions.RemoveEmptyEntries);
            var objectType = Type.GetType(lines[0]);
            var instance = Activator.CreateInstance(objectType);

            if(objectType == null)
                throw new Exception("type is null");

            for(int i = 1; i < lines.Length; i++)
            {
                var elements = lines[i].Split(new string[] { serializationFieldInfoSeparator }, StringSplitOptions.RemoveEmptyEntries);
                var field = objectType.GetField(elements[0]);
                var fieldValueString = elements[1];

                if(field.FieldType.IsPrimitive)
                    field.SetValue(instance, TypeDescriptor.GetConverter(field.FieldType).ConvertFrom(fieldValueString));
                else
                {
                    var from = i + 1;
                    i += int.Parse(fieldValueString);
                    var nestedObjectString = lines[from];
                    for(int j = from + 1; j < i + 1; j++)
                        nestedObjectString += serializationLineSeparator + lines[j];
                    field.SetValue(instance, Deserialize(nestedObjectString));
                }
            }

            return instance;
        }
        public static int GetTotalSerializationLinesCount(Type type)
        {
            var count = 1;
            var notStaticFields = GetNotStaticFields(type);
            foreach(var field in notStaticFields)
            {
                count++;
                if(!field.FieldType.IsPrimitive)
                    count += GetTotalSerializationLinesCount(field.FieldType);
            }
            return count;
        }
        public static string DebugSerialization(string objectString)
        {
            var result = string.Empty;
            foreach(var line in objectString.Split(serializationLineSeparator.ToCharArray()))
                result += "|> " + line + "\n";
            return result;
        }
        public static string DebugInstance(object obj) => Serialize(obj);
    }
}
#endif