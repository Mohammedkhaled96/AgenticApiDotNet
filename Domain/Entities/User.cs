using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AgenticApiDemo.Domain.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 150)]
        public int Age { get; set; }

        [MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
