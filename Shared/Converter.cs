namespace FileFlows.Shared
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.Json;

    public class Converter
    {

        public static object ConvertObject(Type type, object value)
        {
            if (value == null)
                return Activator.CreateInstance(type);
            Type valueType = value.GetType();
            if (value is JsonElement je)
            {
                string json = je.GetRawText();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize(json, type, options);
            }
            if (valueType == type)
                return value;


            if (type.IsArray && typeof(IEnumerable).IsAssignableFrom(valueType))
                return ChangeListToArray(type.GetElementType(), (IEnumerable)value, valueType);


            // not used yet, so not tested
            // if (valueType.IsArray && typeof(IEnumerable).IsAssignableFrom(type))
            //     return ChangeArrayToList(type.GetElementType(), (Array)value);

            if (valueType == typeof(Int64) && type == typeof(Int32))
                return Convert.ToInt32(value);
            return Convert.ChangeType(value, type);
        }

        public static object ChangeListToArray<T>(IEnumerable value, Type valueType)
        {
            var arrayType = typeof(T).GetElementType();
            return ChangeListToArray(arrayType, value, valueType);
        }
        public static object ChangeListToArray(Type arrayType, IEnumerable value, Type valueType)
        {
            Logger.Instance.DLog("Change list to array");
            List<object> list = new List<object>();
            foreach (var o in value)
                list.Add(o);
            var array = Array.CreateInstance(arrayType, list.Count);
            for (int i = 0; i < list.Count; i++)
                array.SetValue(list[i], i);
            return array;
        }
        // public static object ChangeArrayToList(Type listType, Array array)
        // {
        //     var genericListType = typeof(List<>).MakeGenericType(listType);
        //     var genericList = Activator.CreateInstance(genericListType);
        //     var addMethod = genericList.GetType().GetMethod("Add", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        //     foreach (var o in array)
        //         addMethod.Invoke(genericList, new object[] { o });
        //     return genericList;
        // }
    }
}