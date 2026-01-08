using System.ComponentModel;
using AgenticApiDemo.Application.DTOs;
using AgenticApiDemo.Services;
using AgenticApiDemo.Interfaces;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace AgenticApiDemo.Infrastructure.Plugins
{
    public class UserApiPlugin
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserApiPlugin> _logger;

        public UserApiPlugin(IUserService userService, ILogger<UserApiPlugin> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [KernelFunction, Description("Registers a new user in the system.")]
        public async Task<string> RegisterUser(
            [Description("The full name of the user.")] string name,
            [Description("The age of the user in years.")] int age,
            [Description("The user's job title.")] string jobTitle)
        {
            try
            {
                var request = new UserRegistrationRequest { Name = name, Age = age, JobTitle = jobTitle };
                var user = await _userService.RegisterUserAsync(request);
                return JsonSerializer.Serialize(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                return $"Error: {ex.Message}";
            }
        }

        [KernelFunction, Description("Updates an existing user's information.")]
        public async Task<string> UpdateUser(
            [Description("The ID of the user to update.")] int id,
            [Description("The user's new name.")] string? name,
            [Description("The user's new age.")] int? age,
            [Description("The user's new job title.")] string? jobTitle)
        {
            try
            {
                var request = new UserUpdateRequest { Name = name, Age = age, JobTitle = jobTitle };
                var updatedUser = await _userService.UpdateUserAsync(id, request);
                return updatedUser != null ? JsonSerializer.Serialize(updatedUser) : $"User with ID {id} not found.";
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error updating user");
                return $"Error: {ex.Message}";
            }
        }

        [KernelFunction, Description("Deletes a user from the system.")]
        public async Task<string> DeleteUser([Description("The ID of the user to delete.")] int id)
        {
            try
            {
                var success = await _userService.DeleteUserAsync(id);
                return success ? "User deleted successfully." : $"User with ID {id} not found.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return $"Error: {ex.Message}";
            }
        }

        [KernelFunction, Description("Deletes ALL users from the system. Use with extreme caution.")]
        public async Task<string> DeleteAllUsers()
        {
            try
            {
                var count = await _userService.DeleteAllUsersAsync();
                return $"All {count} users deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all users");
                return $"Error: {ex.Message}";
            }
        }

        [KernelFunction, Description("Retrieves a list of all users, with optional filtering.")]
        public async Task<string> GetAllUsers(
            [Description("Filter users by job title.")] string? jobTitleFilter,
            [Description("The minimum age to filter by.")] int? minAge,
            [Description("The maximum age to filter by.")] int? maxAge)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync(jobTitleFilter, minAge, maxAge);
                return JsonSerializer.Serialize(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return $"Error: {ex.Message}";
            }
        }

        [KernelFunction, Description("Retrieves a specific user by their ID.")]
        public async Task<string> GetUserById([Description("The ID of the user to retrieve.")] int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return user != null ? JsonSerializer.Serialize(user) : $"User with ID {id} not found.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID");
                return $"Error: {ex.Message}";
            }
        }
    }
}