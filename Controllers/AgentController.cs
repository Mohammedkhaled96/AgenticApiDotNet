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
        private readonly IFallbackAgentService _fallbackService;
        private readonly ILogger<AgentController> _logger;
        private readonly ActivitySource _activitySource;

        public AgentController(Kernel kernel, IFallbackAgentService fallbackService, ILogger<AgentController> logger)
        {
            _kernel = kernel;
            _fallbackService = fallbackService;
            _logger = logger;
            _activitySource = new ActivitySource("AgenticApi.Agent");
        }

        [HttpPost("converse")]
        [ProducesResponseType(typeof(AgentResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> Converse([FromBody] AgentRequest request)
        {
            using var activity = _activitySource.StartActivity("AgentConversation");
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
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
                    "3. SEARCH BEFORE ACTION: If you need to update or delete a user and you don't have their numeric ID, you MUST call 'UserApi-GetAllUsers' first to find it. Never use placeholders like 'ID_HERE'.\n" +
                    "4. TALK NORMALLY: Never tell the user about the technical tool names (e.g., don't say 'I am calling UserApi-GetAllUsers'). Just perform the action and tell them the result.\n" +
                    "5. If a request is missing critical data (like name for registration), ask for it.\n\n" +
                    "AVAILABLE TOOLS:\n" +
                    "- UserApi-RegisterUser: Registers a new user. REQUIRED: Name, Age, Job Title.\n" +
                    "- UserApi-UpdateUser: Updates existing user. REQUIRED: User ID.\n" +
                    "- UserApi-DeleteUser: Deletes a user. REQUIRED: User ID.\n" +
                    "- UserApi-DeleteAllUsers: Deletes ALL users. CAUTION: Only use if explicitly requested.\n" +
                    "- UserApi-GetAllUsers: Lists users. OPTIONAL: Filter by Job, Min/Max Age.\n" +
                    "- UserApi-GetUserById: Gets a user. REQUIRED: User ID.\n\n" +
                    "RESPONSE GUIDELINES:\n" +
                    "- After a tool executes, confirm the action in the SAME language the user used.\n" +
                    "- If the tool fails, explain why clearly without being overly technical."
                );
                history.AddUserMessage(request.Prompt);

                var result = await chatCompletionService.GetChatMessageContentAsync(history, settings, _kernel);

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
                _logger.LogWarning(ex, "Primary AI failed or is unavailable. Attempting fallback logic for prompt: {Prompt}", request.Prompt);

                try
                {
                    var fallbackResult = await _fallbackService.ExecuteFallbackLogic(request.Prompt, _kernel);
                    
                    stopwatch.Stop();
                    return Ok(new AgentResponse
                    {
                        Success = true,
                        Message = fallbackResult,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    });
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback logic also failed.");
                    return Problem(
                        detail: "The AI service is currently unavailable, and the fallback mechanism failed.",
                        instance: HttpContext.Request.Path,
                        statusCode: 500
                    );
                }
            }
        }
    }
}