using AgenticApiDemo.Application.DTOs;
using AgenticApiDemo.Infrastructure.Plugins;
using AgenticApiDemo.Interfaces;
using AgenticApiDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.SemanticKernel.Connectors.OpenAI; 
using System.Text.RegularExpressions;
using System.Text.Json;

namespace AgenticApiDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly ILogger<AgentController> _logger;
        private readonly ActivitySource _activitySource;
        private readonly IFallbackAgentService _fallbackAgent;

        public AgentController(Kernel kernel, ILogger<AgentController> logger, IFallbackAgentService fallbackAgent)
        {
            _kernel = kernel;
            _logger = logger;
            _fallbackAgent = fallbackAgent;
            _activitySource = new ActivitySource("AgenticApi.Agent");
        }

        [HttpPost("converse")]
        [ProducesResponseType(typeof(AgentResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> Converse([FromBody] AgentRequest request)
        {
            using var activity = _activitySource.StartActivity("AgentConversation");
            
            try
            {
                var stopwatch = Stopwatch.StartNew();
                _logger.LogInformation("Processing agent request: {Prompt}", request.Prompt);

                var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
                
                var settings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions 
                };
                
                var history = new ChatHistory();
                history.AddSystemMessage(
                    "You are 'Agentic', an intelligent and precise assistant for the User Management System.\n" +
                    "CORE RESPONSIBILITIES:\n" +
                    "1. Support multiple languages, including Arabic and English.\n" +
                    "2. Use the provided tools to manage users (Create, Read, Update, Delete).\n" +
                    "3. If a user request is ambiguous, ASK for clarification instead of guessing.\n\n" +
                    "AVAILABLE TOOLS:\n" +
                    "- UserApi-RegisterUser: Registers a new user. REQUIRED: Name, Age, Job Title.\n" +
                    "- UserApi-UpdateUser: Updates existing user. REQUIRED: User ID.\n" +
                    "- UserApi-DeleteUser: Deletes a user. REQUIRED: User ID.\n" +
                    "- UserApi-DeleteAllUsers: Deletes ALL users. CAUTION: Only use if explicitly requested.\n" +
                    "- UserApi-GetAllUsers: Lists users. OPTIONAL: Filter by Job, Min/Max Age.\n" +
                    "- UserApi-GetUserById: Gets a user. REQUIRED: User ID.\n\n" +
                    "RESPONSE GUIDELINES:\n" +
                    "- After a tool executes, confirm the action in the SAME language the user used.\n" +
                    "- Include key details (ID, Name) in your confirmation.\n" +
                    "- If the tool fails, explain why based on the error message."
                );
                history.AddUserMessage(request.Prompt);

                ChatMessageContent result;
                try 
                {
                    result = await chatCompletionService.GetChatMessageContentAsync(history, settings, _kernel);
                }
                catch (Exception ex) when (ex.InnerException is System.Net.Http.HttpRequestException || ex.Message.Contains("refused"))
                {
                    _logger.LogWarning("Ollama/AI is offline. Switching to Fallback Rule-Based Agent.");
                    var fallbackResponse = await _fallbackAgent.ExecuteFallbackLogic(request.Prompt, _kernel);
                    result = new ChatMessageContent(AuthorRole.Assistant, fallbackResponse);
                }

                stopwatch.Stop();
                
                var response = new AgentResponse
                {
                    Success = true,
                    Message = result.Content ?? "Task completed.",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in the Converse endpoint.");
                return Problem(
                    detail: "An unexpected error occurred. Please try again later.",
                    instance: HttpContext.Request.Path,
                    statusCode: 500
                );
            }
        }
    }
}