using AgenticApiDemo.Application.DTOs;
using AgenticApiDemo.Services;
using AgenticApiDemo.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace AgenticApiDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        [Description("Registers a new user in the system.")]
        [ProducesResponseType(typeof(string), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationRequest request)
        {
            var user = await _userService.RegisterUserAsync(request);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, $"User '{user.Name}' was registered successfully with ID {user.Id}.");
        }
        
        [HttpGet("{id}")]
        [Description("Retrieves a specific user by their ID.")]
        [ProducesResponseType(typeof(string), 200)] // Corrected type for swagger if returning entity is desired or DTO
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("{id}")]
        [Description("Updates an existing user's information.")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateRequest request)
        {
            var updatedUser = await _userService.UpdateUserAsync(id, request);
            if (updatedUser == null) return NotFound();
            return Ok($"User with ID {id} was updated successfully.");
        }

        [HttpDelete("{id}")]
        [Description("Deletes a user from the system.")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var success = await _userService.DeleteUserAsync(id);
            if (!success) return NotFound();
            return Ok($"User with ID {id} was deleted successfully.");
        }

        [HttpDelete("all")]
        [Description("Deletes ALL users from the system.")]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<IActionResult> DeleteAllUsers()
        {
            var count = await _userService.DeleteAllUsersAsync();
            return Ok($"All {count} users have been deleted successfully.");
        }

        [HttpGet]
        [Description("Retrieves a list of all users, with optional filtering.")]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? jobTitleFilter,
            [FromQuery] int? minAge,
            [FromQuery] int? maxAge)
        {
            var users = await _userService.GetAllUsersAsync(jobTitleFilter, minAge, maxAge);
            return Ok(users);
        }
    }
}
