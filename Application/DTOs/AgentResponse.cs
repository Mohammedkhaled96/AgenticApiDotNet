namespace AgenticApiDemo.Application.DTOs
{
    public class AgentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long ExecutionTimeMs { get; set; }
    }
}
