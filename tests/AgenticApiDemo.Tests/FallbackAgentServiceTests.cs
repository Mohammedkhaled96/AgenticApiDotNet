using System.Text.Json;
using AgenticApiDemo.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;

namespace AgenticApiDemo.Tests;

public class FallbackAgentServiceTests
{
    private readonly FakeUserApi _api = new();
    private readonly Kernel _kernel;
    private readonly FallbackAgentService _agent = new(NullLogger<FallbackAgentService>.Instance);

    public FallbackAgentServiceTests()
    {
        _kernel = new Kernel();
        _kernel.Plugins.AddFromObject(_api, "UserApi");
    }

    [Fact]
    public async Task Register_English_ExtractsNameAgeAndJob()
    {
        var reply = await _agent.ExecuteFallbackLogic("Register a user named Sarah age 28 job Doctor", _kernel);

        Assert.Equal("RegisterUser", _api.LastFunction);
        Assert.Equal("Sarah", _api.Args["name"]);
        Assert.Equal(28, _api.Args["age"]);
        Assert.Equal("Doctor", _api.Args["jobTitle"]);
        Assert.Contains("User [ID: 1] Name: Sarah, Age: 28, Job: Doctor", reply);
    }

    [Fact]
    public async Task Register_Arabic_ExtractsNameAgeAndJob()
    {
        await _agent.ExecuteFallbackLogic("سجل مستخدم اسمه أحمد عمره 30 وظيفته مهندس", _kernel);

        Assert.Equal("RegisterUser", _api.LastFunction);
        Assert.Equal("أحمد", _api.Args["name"]);
        Assert.Equal(30, _api.Args["age"]);
        Assert.Equal("مهندس", _api.Args["jobTitle"]);
    }

    [Fact]
    public async Task Register_WithoutDetails_UsesDefaults()
    {
        await _agent.ExecuteFallbackLogic("create someone", _kernel);

        Assert.Equal("Fallback User", _api.Args["name"]);
        Assert.Equal(25, _api.Args["age"]);
        Assert.Equal("Unknown", _api.Args["jobTitle"]);
    }

    [Fact]
    public async Task Update_WithId_PassesOnlyProvidedFields()
    {
        await _agent.ExecuteFallbackLogic("update user id 5 age 40", _kernel);

        Assert.Equal("UpdateUser", _api.LastFunction);
        Assert.Equal(5, _api.Args["id"]);
        Assert.Equal(40, _api.Args["age"]);
        Assert.Null(_api.Args["name"]);
        Assert.Null(_api.Args["jobTitle"]);
    }

    [Fact]
    public async Task Update_WithoutNumericId_AsksForId()
    {
        var reply = await _agent.ExecuteFallbackLogic("update the user with id", _kernel);

        Assert.Null(_api.LastFunction);
        Assert.Contains("couldn't find the ID", reply);
    }

    [Fact]
    public async Task Delete_WithId_DeletesThatUser()
    {
        var reply = await _agent.ExecuteFallbackLogic("delete user id 7", _kernel);

        Assert.Equal("DeleteUser", _api.LastFunction);
        Assert.Equal(7, _api.Args["id"]);
        Assert.Contains("User deleted successfully.", reply);
    }

    [Fact]
    public async Task GetById_ReturnsFormattedUser()
    {
        var reply = await _agent.ExecuteFallbackLogic("get user id 3", _kernel);

        Assert.Equal("GetUserById", _api.LastFunction);
        Assert.Equal(3, _api.Args["id"]);
        Assert.Contains("User [ID: 3]", reply);
    }

    [Fact]
    public async Task ListUsers_WhenEmpty_SaysNoUsersFound()
    {
        var reply = await _agent.ExecuteFallbackLogic("list all users", _kernel);

        Assert.Equal("GetAllUsers", _api.LastFunction);
        Assert.Contains("No users found.", reply);
    }

    [Fact]
    public async Task DeleteAll_DeletesEveryUser()
    {
        var reply = await _agent.ExecuteFallbackLogic("delete all users", _kernel);

        Assert.Equal("DeleteAllUsers", _api.LastFunction);
        Assert.Contains("All 0 users deleted successfully.", reply);
    }

    [Fact]
    public async Task UnknownCommand_ReturnsHelpMessage()
    {
        var reply = await _agent.ExecuteFallbackLogic("what is the capital of France", _kernel);

        Assert.Null(_api.LastFunction);
        Assert.StartsWith("[Fallback Agent] I understood you want to do something", reply);
    }

    /// <summary>Stand-in for UserApiPlugin that records the call instead of touching a database.</summary>
    private sealed class FakeUserApi
    {
        public string? LastFunction { get; private set; }
        public Dictionary<string, object?> Args { get; } = new();

        private void Record(string function, params (string Key, object? Value)[] args)
        {
            LastFunction = function;
            Args.Clear();
            foreach (var (key, value) in args) Args[key] = value;
        }

        private static string UserJson(int id, string? name, int? age, string? jobTitle) =>
            JsonSerializer.Serialize(new { Id = id, Name = name, Age = age, JobTitle = jobTitle });

        [KernelFunction]
        public string RegisterUser(string name, int age, string jobTitle)
        {
            Record(nameof(RegisterUser), ("name", name), ("age", age), ("jobTitle", jobTitle));
            return UserJson(1, name, age, jobTitle);
        }

        [KernelFunction]
        public string UpdateUser(int id, string? name, int? age, string? jobTitle)
        {
            Record(nameof(UpdateUser), ("id", id), ("name", name), ("age", age), ("jobTitle", jobTitle));
            return UserJson(id, name ?? "Existing", age ?? 30, jobTitle ?? "Existing");
        }

        [KernelFunction]
        public string DeleteUser(int id)
        {
            Record(nameof(DeleteUser), ("id", id));
            return "User deleted successfully.";
        }

        [KernelFunction]
        public string DeleteAllUsers()
        {
            Record(nameof(DeleteAllUsers));
            return "All 0 users deleted successfully.";
        }

        [KernelFunction]
        public string GetAllUsers(string? jobTitleFilter, int? minAge, int? maxAge)
        {
            Record(nameof(GetAllUsers), ("jobTitleFilter", jobTitleFilter), ("minAge", minAge), ("maxAge", maxAge));
            return "[]";
        }

        [KernelFunction]
        public string GetUserById(int id)
        {
            Record(nameof(GetUserById), ("id", id));
            return UserJson(id, "Test", 20, "Tester");
        }
    }
}
