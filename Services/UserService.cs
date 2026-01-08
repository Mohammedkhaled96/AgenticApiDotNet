using AgenticApiDemo.Application.DTOs;
using AgenticApiDemo.Domain.Entities;
using AgenticApiDemo.Infrastructure.Data;
using AgenticApiDemo.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgenticApiDemo.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User> RegisterUserAsync(UserRegistrationRequest request)
        {
            var user = new User
            {
                Name = request.Name,
                Age = request.Age,
                JobTitle = request.JobTitle,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Service: Successfully registered new user: {UserName}", user.Name);
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> UpdateUserAsync(int id, UserUpdateRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            if (request.Name != null) user.Name = request.Name;
            if (request.Age.HasValue) user.Age = request.Age.Value;
            if (request.JobTitle != null) user.JobTitle = request.JobTitle;
            
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteAllUsersAsync()
        {
            var allUsers = await _context.Users.ToListAsync();
            int count = allUsers.Count;

            _context.Users.RemoveRange(allUsers);
            await _context.SaveChangesAsync();
            return count;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(string? jobTitleFilter, int? minAge, int? maxAge)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(jobTitleFilter))
            {
                query = query.Where(u => u.JobTitle.Contains(jobTitleFilter));
            }

            if (minAge.HasValue)
            {
                query = query.Where(u => u.Age >= minAge.Value);
            }

            if (maxAge.HasValue)
            {
                query = query.Where(u => u.Age <= maxAge.Value);
            }

            return await query.ToListAsync();
        }
    }
}