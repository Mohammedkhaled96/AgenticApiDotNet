using System.Text.Json;
using System.Text.RegularExpressions;
using AgenticApiDemo.Interfaces;
using Microsoft.SemanticKernel;

namespace AgenticApiDemo.Services
{
    public class FallbackAgentService : IFallbackAgentService
    {
        private readonly ILogger<FallbackAgentService> _logger;

        public FallbackAgentService(ILogger<FallbackAgentService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ExecuteFallbackLogic(string prompt, Kernel kernel)
        {
            var lowerPrompt = prompt.ToLowerInvariant();
            var userPlugin = kernel.Plugins["UserApi"];
            _logger.LogInformation("Fallback Agent processing prompt: {Prompt}", prompt);

            // English OR Arabic keywords for "Register"
            // "سجل", "انشئ", "ضيف" = Register/Create/Add
            if (lowerPrompt.Contains("register") || lowerPrompt.Contains("create") || lowerPrompt.Contains("add") ||
                lowerPrompt.Contains("سجل") || lowerPrompt.Contains("انشئ") || lowerPrompt.Contains("ضيف"))
            {
                _logger.LogInformation("Fallback Agent detected 'Register' command.");

                // 1. Extract Name
                // English: "name is X" or "named X"
                var nameMatch = Regex.Match(prompt, @"(?:name\s+is|named)\s+(.+?)(?:\s+(?:age|job|works|and)|$|[.,])", RegexOptions.IgnoreCase);
                
                // Arabic: "اسمه X" or "اسم X"
                if (!nameMatch.Success)
                {
                    // Improved Regex: Stop at "age", "job", or keywords like "عمره", "وظيفته"
                    nameMatch = Regex.Match(prompt, @"(?:اسمه|اسم)\s+(.+?)(?:\s+(?:عمره|سن|سنه|وظيفته|يعمل|شغال)|$)", RegexOptions.Singleline); 
                }

                var name = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : "Fallback User";
                name = name.Replace("\"", "").Replace("'", "").Trim();

                // 2. Extract Age
                // English: "age is 30" or "30 years"
                var ageMatch = Regex.Match(prompt, @"(?:age\s+is\s+|age\s+)(\d+)", RegexOptions.IgnoreCase);
                if (!ageMatch.Success) 
                {
                    ageMatch = Regex.Match(prompt, @"(\d+)\s*(?:years|yrs)"); 
                }
                
                // Arabic: "عمره 30" or "سن 30"
                if (!ageMatch.Success)
                {
                    ageMatch = Regex.Match(prompt, @"(?:عمره|سن|سنه)\s+(\d+)");
                }

                int age = ageMatch.Success && int.TryParse(ageMatch.Groups[1].Value, out int a) ? a : 25;

                // 3. Extract Job
                // English: "job is X", "works as X", "job title is X"
                var jobMatch = Regex.Match(prompt, @"(?:job(?:\s+title)?\s+(?:is|works\s+as))\s+(.+?)(?:$|[.,])", RegexOptions.IgnoreCase);
                
                // Arabic: "وظيفته X", "يعمل X" (handling potential 'and' prefix 'و')
                if (!jobMatch.Success)
                {
                    // Improved Regex: Allow optional 'و' at the start of the keyword
                    jobMatch = Regex.Match(prompt, @"(?:(?:^|\s)و?)(?:وظيفته|يعمل|شغال)\s+(.+?)(?:$|[.,])", RegexOptions.Singleline);
                }

                var job = jobMatch.Success ? jobMatch.Groups[1].Value.Trim() : "Unknown";
                job = job.Replace("\"", "").Replace("'", "").Trim();

                _logger.LogInformation("Fallback extracted: Name='{Name}', Age={Age}, Job='{Job}'", name, age, job);

                var func = userPlugin["RegisterUser"];
                var result = await func.InvokeAsync(kernel, new KernelArguments { ["name"] = name, ["age"] = age, ["jobTitle"] = job });
                
                string formattedResult = FormatAgentResponse(result.ToString() ?? "");

                return "[Fallback Agent - AR/EN] I've registered the user successfully. (AI was offline, used logic). Details:\n" + formattedResult;
            }
            // English OR Arabic keywords for "Update User"
            // "update", "change", "modify", "تعديل", "تحديث", "غير"
            else if (lowerPrompt.Contains("id") && 
                    (lowerPrompt.Contains("update") || lowerPrompt.Contains("change") || lowerPrompt.Contains("modify") || 
                     lowerPrompt.Contains("تعديل") || lowerPrompt.Contains("تحديث") || lowerPrompt.Contains("غير")))
            {
                // Extract ID
                var idMatch = Regex.Match(prompt, @"(?:id|رقم)\s*(\d+)", RegexOptions.IgnoreCase);
                if (!idMatch.Success || !int.TryParse(idMatch.Groups[1].Value, out int id))
                {
                     return "[Fallback Agent] I understood you want to update a user, but I couldn't find the ID. Please specify 'id X'.";
                }

                // Extract Name (Optional)
                var nameMatch = Regex.Match(prompt, @"(?:name|اسم)\s+(?:is|to|becomes)?\s*(.+?)(?:\s+(?:age|job|works|and)|$|[.,])", RegexOptions.IgnoreCase);
                string? name = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : null;
                if (name != null) name = name.Replace("\"", "").Replace("'", "").Trim();

                // Extract Age (Optional)
                var ageMatch = Regex.Match(prompt, @"(?:age|عمر|سن)\s+(?:is|to|becomes)?\s*(\d+)", RegexOptions.IgnoreCase);
                int? age = ageMatch.Success && int.TryParse(ageMatch.Groups[1].Value, out int a) ? a : null;

                // Extract Job (Optional)
                var jobMatch = Regex.Match(prompt, @"(?:job|works|وظيف|عمل)\s+(?:is|as|to|becomes)?\s*(.+?)(?:\s+(?:name|age|and)|$|[.,])", RegexOptions.IgnoreCase);
                string? job = jobMatch.Success ? jobMatch.Groups[1].Value.Trim() : null;
                if (job != null) job = job.Replace("\"", "").Replace("'", "").Trim();

                var func = userPlugin["UpdateUser"];
                var result = await func.InvokeAsync(kernel, new KernelArguments { ["id"] = id, ["name"] = name, ["age"] = age, ["jobTitle"] = job });
                return "[Fallback Agent - AR/EN] " + FormatAgentResponse(result.ToString() ?? "");
            }
            // English OR Arabic keywords for "Delete User" (Single)
            // "delete", "remove", "حذف", "مسح" AND "id"
            else if (lowerPrompt.Contains("id") && 
                    (lowerPrompt.Contains("delete") || lowerPrompt.Contains("remove") || lowerPrompt.Contains("حذف") || lowerPrompt.Contains("مسح")))
            {
                 // Extract ID
                var idMatch = Regex.Match(prompt, @"(?:id|رقم)\s*(\d+)", RegexOptions.IgnoreCase);
                if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out int id))
                {
                    var func = userPlugin["DeleteUser"];
                    var result = await func.InvokeAsync(kernel, new KernelArguments { ["id"] = id });
                    return "[Fallback Agent - AR/EN] " + FormatAgentResponse(result.ToString() ?? "");
                }
            }
            // English OR Arabic keywords for "Get User By ID"
            // "get user id X", "user id X", "هات مستخدم رقم X"
            else if (lowerPrompt.Contains("id") && (lowerPrompt.Contains("get") || lowerPrompt.Contains("user") || lowerPrompt.Contains("هات") || lowerPrompt.Contains("مستخدم")))
            {
                // Extract ID
                var idMatch = Regex.Match(prompt, @"(?:id|رقم)\s*(\d+)", RegexOptions.IgnoreCase);
                if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out int id))
                {
                    var func = userPlugin["GetUserById"];
                    var result = await func.InvokeAsync(kernel, new KernelArguments { ["id"] = id });
                    return "[Fallback Agent - AR/EN] " + FormatAgentResponse(result.ToString() ?? "");
                }
            }
            // English OR Arabic keywords for "Get Users"
            // "هات", "اعرض", "قائمة" = Get/Show/List
            else if (lowerPrompt.Contains("get") || lowerPrompt.Contains("list") || lowerPrompt.Contains("show") ||
                     lowerPrompt.Contains("هات") || lowerPrompt.Contains("اعرض") || lowerPrompt.Contains("قائمة"))
            {
                var func = userPlugin["GetAllUsers"];
                var arguments = new KernelArguments
                {
                    ["jobTitleFilter"] = null,
                    ["minAge"] = null,
                    ["maxAge"] = null
                };
                var result = await func.InvokeAsync(kernel, arguments);
                return "[Fallback Agent - AR/EN] Here is the list of users:\n" + FormatAgentResponse(result.ToString() ?? "");
            }
            // English OR Arabic keywords for "Delete All"
            // "حذف الكل", "مسح الجميع", "امسح كل", "احذف الجميع"
            else if ((lowerPrompt.Contains("delete") && lowerPrompt.Contains("all")) || 
                     ((lowerPrompt.Contains("حذف") || lowerPrompt.Contains("مسح") || lowerPrompt.Contains("امسح") || lowerPrompt.Contains("احذف")) && 
                      (lowerPrompt.Contains("الكل") || lowerPrompt.Contains("كل") || lowerPrompt.Contains("الجميع") || lowerPrompt.Contains("جميع") || lowerPrompt.Contains("المستخدمين"))))
            {
                var func = userPlugin["DeleteAllUsers"];
                var result = await func.InvokeAsync(kernel);
                return "[Fallback Agent - AR/EN] " + FormatAgentResponse(result.ToString() ?? "");
            }

            return "[Fallback Agent] I understood you want to do something, but since the AI brain (Ollama) is offline, I can only handle 'Register', 'Get Users', and 'Delete All' commands (English or Arabic) right now.";
        }

        private string FormatAgentResponse(string content)
        {
            try
            {
                // Try to parse as JSON Element
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    var formattedList = new List<string>();
                    foreach (var element in root.EnumerateArray())
                    {
                        formattedList.Add(FormatSingleUser(element));
                    }
                    
                    if (formattedList.Count == 0) return "No users found.";
                    return string.Join("\n", formattedList);
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    // Check if it's a User object (has id, name, etc.) - Check PascalCase or camelCase
                    if (root.TryGetProperty("id", out _) || root.TryGetProperty("Id", out _))
                    {
                        return FormatSingleUser(root);
                    }
                    // Handle other JSON objects or error messages returned as JSON
                    if (root.TryGetProperty("message", out var msg) || root.TryGetProperty("Message", out msg))
                    {
                        return msg.GetString() ?? content;
                    }
                }

                return content;
            }
            catch (JsonException)
            {
                // Not JSON, return as is
                return content;
            }
        }

        private string FormatSingleUser(JsonElement element)
        {
            // Helper to get property value regardless of casing
            string GetProp(string key) 
            {
                if (element.TryGetProperty(key, out var prop)) return prop.ToString();
                // Try PascalCase
                string pascalKey = char.ToUpper(key[0]) + key.Substring(1);
                if (element.TryGetProperty(pascalKey, out prop)) return prop.ToString();
                return "N/A";
            }

            string id = GetProp("id");
            string name = GetProp("name");
            string age = GetProp("age");
            string job = GetProp("jobTitle");

            // Clean up name string if it was JSON string
            if (name.StartsWith("\"") && name.EndsWith("\"")) name = name.Trim('"');
            if (job.StartsWith("\"") && job.EndsWith("\"")) job = job.Trim('"');

            return $"User [ID: {id}] Name: {name}, Age: {age}, Job: {job}";
        }
    }
}