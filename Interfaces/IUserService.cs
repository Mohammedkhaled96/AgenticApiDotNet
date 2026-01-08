using AgenticApiDemo.Application.DTOs;
using AgenticApiDemo.Domain.Entities;

namespace AgenticApiDemo.Interfaces
{
    public interface IUserService
    {
        Task<User> RegisterUserAsync(UserRegistrationRequest request);
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> UpdateUserAsync(int id, UserUpdateRequest request);
        Task<bool> DeleteUserAsync(int id);
        Task<int> DeleteAllUsersAsync();
        Task<IEnumerable<User>> GetAllUsersAsync(string? jobTitleFilter, int? minAge, int? maxAge);
    }
}
