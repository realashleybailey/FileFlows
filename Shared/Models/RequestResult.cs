namespace FileFlows.Shared.Models
{
    public class RequestResult<T>
    {
        public bool Success { get; set; }
        public string Body { get; set; }
        public T Data { get; set; }
    }
}