namespace FileFlows.Shared.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using FileFlows.Shared.Helpers.ArrayExtensions;

    /// <summary>
    /// Clones an object
    /// </summary>
    public class ObjectCloner
    {
#pragma warning disable CS8603
        private static readonly MethodInfo CloneMethod = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
#pragma warning restore CS8603

        private static bool IsPrimitive(Type type)
        {
            if (type == typeof(String)) return true;
            return (type.IsValueType & type.IsPrimitive);
        }

#pragma warning disable CS8604
        /// <summary>
        /// Clones an object
        /// </summary>
        /// <param name="original">the object to clone</param>
        /// <typeparam name="T">the object type to clone</typeparam>
        /// <returns>a cloned instance</returns>
        public static T Clone<T>(T original)
        {
            if (original == null)
                return original;
            return (T)Clone((object)original);
        }
#pragma warning restore CS8604

        /// <summary>
        /// Clones an object
        /// </summary>
        /// <param name="originalObject">the object to clone</param>
        /// <returns>A cloned instance</returns>
        public static object Clone(object? originalObject)
        {
            return InternalCopy(originalObject, new Dictionary<object, object>(new ReferenceEqualityComparer()));
        }
        private static object InternalCopy(object? originalObject, IDictionary<object, object> visited)
        {
            if (originalObject == null) return null;
            var typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect)) return originalObject;
            if (visited.ContainsKey(originalObject)) return visited[originalObject];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            var cloneObject = CloneMethod.Invoke(originalObject, null);
            if (typeToReflect.IsArray)
            {
                var arrayType = typeToReflect.GetElementType();
                if (IsPrimitive(arrayType) == false)
                {
                    Array? clonedArray = cloneObject as Array;
                    clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                }

            }
            visited.Add(originalObject, cloneObject);
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
            return cloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect)
        {
            if (typeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
                CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
            }
        }

        private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null)
        {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                var clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }
    }

    /// <summary>
    /// Checks if objects are teh same reference
    /// </summary>
    public class ReferenceEqualityComparer : EqualityComparer<Object>
    {
        /// <summary>
        /// Checks if two objects are equal
        /// </summary>
        /// <param name="x">first object</param>
        /// <param name="y">second object</param>
        /// <returns>If the objects are ruqla</returns>
        public override bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }
        /// <summary>
        /// Gets a hashcode of an object
        /// </summary>
        /// <param name="obj">the object</param>
        /// <returns>the objec hashcode</returns>
        public override int GetHashCode(object? obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }

    namespace ArrayExtensions
    {
        /// <summary>
        /// Array extensions
        /// </summary>
        public static class ArrayExtensions
        {
            /// <summary>
            /// Method to perform a foreach
            /// </summary>
            /// <param name="array">the array to foreach</param>
            /// <param name="action">the action to perform on each item</param>
            public static void ForEach(this Array? array, Action<Array, int[]> action)
            {
                if (array.LongLength == 0) return;
                ArrayTraverse walker = new ArrayTraverse(array);
                do action(array, walker.Position);
                while (walker.Step());
            }
        }

        /// <summary>
        /// Array transverse
        /// </summary>
        internal class ArrayTraverse
        {
            /// <summary>
            /// Array positional indexes
            /// </summary>
            public int[] Position;
            private int[] maxLengths;

            /// <summary>
            /// Transfers an array
            /// </summary>
            /// <param name="array">the array to transverse</param>
            public ArrayTraverse(Array? array)
            {
                maxLengths = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i)
                {
                    maxLengths[i] = array.GetLength(i) - 1;
                }
                Position = new int[array.Rank];
            }

            /// <summary>
            /// Step to the next item in the array
            /// </summary>
            /// <returns>true if successful</returns>
            public bool Step()
            {
                for (int i = 0; i < Position.Length; ++i)
                {
                    if (Position[i] < maxLengths[i])
                    {
                        Position[i]++;
                        for (int j = 0; j < i; j++)
                        {
                            Position[j] = 0;
                        }
                        return true;
                    }
                }
                return false;
            }
        }
    }
}