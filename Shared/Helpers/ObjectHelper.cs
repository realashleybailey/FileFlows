namespace FileFlows.Shared.Helpers;

using FileFlows.Plugin;

/// <summary>
/// Generic helper methods for objecst
/// </summary>
public class ObjectHelper
{
    
    /// <summary>
    /// Tests if two objects are logically the same
    /// </summary>
    /// <param name="a">The first object to test</param>
    /// <param name="b">The second object to test</param>
    /// <returns>true if the objects are logically the same</returns>
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