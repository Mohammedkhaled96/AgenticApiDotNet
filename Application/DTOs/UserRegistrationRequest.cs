using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AgenticApiDemo.Application.DTOs
{
    public class UserRegistrationRequest
    {
        [Required]
        [Description("The full name of the user.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 150)]
        [Description("The age of the user in years.")]
        public int Age { get; set; }

        [Required]
        [Description("The user's job title.")]
        public string JobTitle { get; set; } = string.Empty;
    }
}
