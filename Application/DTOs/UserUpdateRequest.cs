using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AgenticApiDemo.Application.DTOs
{
    public class UserUpdateRequest
    {
        [Description("The new full name of the user.")]
        public string? Name { get; set; }

        [Range(1, 150)]
        [Description("The new age of the user in years.")]
        public int? Age { get; set; }

        [Description("The new job title of the user.")]
        public string? JobTitle { get; set; }
    }
}
