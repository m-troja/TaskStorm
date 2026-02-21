using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskStorm.Data;
using TaskStorm.Model.Entity;
using TaskStorm.Service.Impl;
using Xunit;

namespace TaskStorm.Tests.Service;
public class ChatGptServiceTest
{
    private PostgresqlDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PostgresqlDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new PostgresqlDbContext(options);

        db.Activities.RemoveRange(db.Activities);
        db.SaveChanges();

        return db;
    }

    private ILogger<ChatGptService> GetLoggerStub() =>
        new LoggerFactory().CreateLogger<ChatGptService>();

    [Fact]
    public async Task GetAllChatGptUsersAsync_ShouldReturnAllChatGptUsers()
    {
        // given
        var db = GetInMemoryDb();
        var logger = GetLoggerStub();
        var users = new List<ChatGptUser>
        {
            new ChatGptUser(1, "slackName1", "U12345678"),
            new ChatGptUser(2, "slackName2", "U22345678")
        };

        var mockHttp = new MockHttpMessageHandler();

        var json = System.Text.Json.JsonSerializer.Serialize(users);

        mockHttp.When(HttpMethod.Get, "http://localhost:6969/api/v1/users/all")
                .Respond("application/json", json);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:6969");

        var service = new ChatGptService(logger, httpClient, db);
        SetPrivateField(service, "_ChatServerAddress", "localhost");
        SetPrivateField(service, "_ChatServerPort", "6969");
        SetPrivateField(service, "GetAllUsersEndpoint", "/api/v1/users/all");
        // when
        var usersFromService = await service.GetAllChatGptUsersAsync();

        // then
        Assert.NotNull(usersFromService);
        Assert.Equal(2, usersFromService.Count);
    }

    [Fact]
    public async Task<List<User>> RegisterSLackUsers_ShouldRegisterUsers()
    {
        // given
        var db = GetInMemoryDb();
        var logger = GetLoggerStub();
        var service = new ChatGptService(logger, new HttpClient(), db);
        var chatGptUsers = new List<ChatGptUser>
        {
            new ChatGptUser(1, "slackName1", "U12345678"),
            new ChatGptUser(2, "slackName2", "U22345678")
        };

        // when
        var registeredUsers = await service.RegisterSlackUsers(chatGptUsers);
        // then
        Assert.NotNull(registeredUsers);
        Assert.Equal(2, registeredUsers.Count);
        Assert.Contains(registeredUsers, u => u.SlackUserId == "U12345678" && u.FirstName == "slackName1");
        Assert.Contains(registeredUsers, u => u.SlackUserId == "U22345678" && u.FirstName == "slackName2");
        return registeredUsers;
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(obj, value);
    }

}
