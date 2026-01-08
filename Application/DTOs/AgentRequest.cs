using System.ComponentModel.DataAnnotations;

namespace AgenticApiDemo.Application.DTOs
{
    public class AgentRequest
    {
        [Required(ErrorMessage = "Prompt is required.")]
        [StringLength(1000, ErrorMessage = "Prompt must be less than 1000 characters.")]
        public string Prompt { get; set; } = string.Empty;
    }
}
