namespace FileFlow.Server.Helpers
{
    using NPoco;

    class CustomDbMapper : DefaultMapper
    {
        public override Func<object, object> GetFromDbConverter(Type destType, Type sourceType)
        {
            if (destType == typeof(Guid) && sourceType == typeof(string))
                return (value) => Guid.Parse((string)value);
            return base.GetFromDbConverter(destType, sourceType);
        }
    }
}