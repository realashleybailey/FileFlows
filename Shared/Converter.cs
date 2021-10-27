namespace ViWatcher.Shared
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Converter {
        
        public static object ConvertObject(Type type, object value)
        {
            if(value == null)
                return Activator.CreateInstance(type);
            Type valueType = value.GetType();
            if(valueType == type)
                return value;
            if(type.IsArray && typeof(IEnumerable).IsAssignableFrom(valueType))
                return ChangeListToArray(type.GetElementType(), (IEnumerable)value, valueType);

            if(valueType == typeof(Int64) && type == typeof(Int32))
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
            List<object> list = new List<object>();
            foreach(var o in value)
                list.Add(o);
            var array = Array.CreateInstance(arrayType, list.Count);
            for (int i = 0; i < list.Count;i++)
                array.SetValue(list[i], i);
            return array;
        }
    }
}