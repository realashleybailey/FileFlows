using System.Reflection;
using NPoco;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// UID Converter that converts GUIDs to string when saving to the database
/// this is used by the sqlite db to avoid writing GUIDs as blobs
/// </summary>
public class UidConverter:DefaultMapper
{
    /// <summary>
    /// Get the convert for converting an object to the object actually stored in the database
    /// </summary>
    /// <param name="destType">the type we are trying to convert to</param>
    /// <param name="sourceMemberInfo">the information about the type being converted</param>
    /// <returns>the converter function to use</returns>
    public override Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
    {
        if (destType == typeof(Guid))
            return x => x.ToString();
        return base.GetToDbConverter(destType, sourceMemberInfo);
    }
}
