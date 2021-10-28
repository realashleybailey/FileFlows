namespace FileFlow.Server.Models
{
    using NPoco;

    [PrimaryKey(nameof(Uid), AutoIncrement = false)]
    internal class DbObject
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }

        public string Data { get; set; }
    }
}