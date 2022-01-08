namespace FileFlows.Shared.Helpers
{
    using FileFlows.Plugin;

    public class ObjectHelper
    {
        public static bool ObjectsAreSame(object a, object b)
        {

            if (a == null && b == null)
                return true;

            if (a != null && b != null && a.Equals(b)) return true;

            if (a is ObjectReference objA && b is ObjectReference objB)
                return objA.Uid == objB.Uid;

            bool areEqual = System.Text.Json.JsonSerializer.Serialize(a) == System.Text.Json.JsonSerializer.Serialize(b);
            if (areEqual)
                return true;

            return false;
        }
    }
}
