using AgenticApiDemo.Application.DTOs;
using AgenticApiDemo.Infrastructure.Data;
using AgenticApiDemo.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgenticApiDemo.Tests;

public sealed class UserServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _service = new UserService(_context, NullLogger<UserService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private Task<Domain.Entities.User> Register(string name, int age, string job) =>
        _service.RegisterUserAsync(new UserRegistrationRequest { Name = name, Age = age, JobTitle = job });

    [Fact]
    public async Task RegisterUserAsync_PersistsUser()
    {
        var before = DateTime.UtcNow;

        var user = await Register("Sarah", 28, "Doctor");

        Assert.True(user.Id > 0);
        var stored = await _service.GetUserByIdAsync(user.Id);
        Assert.NotNull(stored);
        Assert.Equal("Sarah", stored.Name);
        Assert.Equal(28, stored.Age);
        Assert.Equal("Doctor", stored.JobTitle);
        Assert.True(stored.CreatedAt >= before);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        Assert.Null(await _service.GetUserByIdAsync(999));
    }

    [Fact]
    public async Task UpdateUserAsync_ChangesOnlyProvidedFields()
    {
        var user = await Register("Mohamed", 30, "Engineer");

        var updated = await _service.UpdateUserAsync(user.Id, new UserUpdateRequest { Age = 31 });

        Assert.NotNull(updated);
        Assert.Equal("Mohamed", updated.Name);
        Assert.Equal(31, updated.Age);
        Assert.Equal("Engineer", updated.JobTitle);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        Assert.Null(await _service.UpdateUserAsync(999, new UserUpdateRequest { Name = "Ghost" }));
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesExistingUser()
    {
        var user = await Register("Ali", 40, "Driver");

        Assert.True(await _service.DeleteUserAsync(user.Id));
        Assert.Null(await _service.GetUserByIdAsync(user.Id));
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenUserDoesNotExist()
    {
        Assert.False(await _service.DeleteUserAsync(999));
    }

    [Fact]
    public async Task DeleteAllUsersAsync_ReturnsNumberOfDeletedUsers()
    {
        await Register("A", 20, "Dev");
        await Register("B", 25, "QA");

        Assert.Equal(2, await _service.DeleteAllUsersAsync());
        Assert.Empty(await _service.GetAllUsersAsync(null, null, null));
    }

    [Theory]
    [InlineData(null, null, null, 3)]
    [InlineData("Engineer", null, null, 2)]
    [InlineData(null, 30, null, 2)]
    [InlineData(null, null, 29, 1)]
    [InlineData("Engineer", 30, 40, 1)]
    public async Task GetAllUsersAsync_AppliesFilters(string? job, int? minAge, int? maxAge, int expected)
    {
        await Register("Young", 22, "Engineer");
        await Register("Senior", 35, "Engineer");
        await Register("Doc", 50, "Doctor");

        var users = await _service.GetAllUsersAsync(job, minAge, maxAge);

        Assert.Equal(expected, users.Count());
    }
}
