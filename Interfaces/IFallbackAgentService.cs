using Microsoft.SemanticKernel;

namespace AgenticApiDemo.Interfaces
{
    public interface IFallbackAgentService
    {
        Task<string> ExecuteFallbackLogic(string prompt, Kernel kernel);
    }
}
