namespace Willow.Infrastructure
{
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string[] CallStack { get; set; }
    }
}