namespace FileFlows.Server.Helpers;

using NPoco;

/// <summary>
/// Custom DB Mapper
/// </summary>
class CustomDbMapper : DefaultMapper
{
    /// <summary>
    /// Function to convert a database object type
    /// </summary>
    /// <param name="destType">the type of object to create</param>
    /// <param name="sourceType">the type of object to create a new object from</param>
    /// <returns>a function to do a conversion</returns>
    public override Func<object, object> GetFromDbConverter(Type destType, Type sourceType)
    {
        if (destType == typeof(Guid) && sourceType == typeof(string))
            return (value) => Guid.Parse((string)value);
        return base.GetFromDbConverter(destType, sourceType);
    }
}