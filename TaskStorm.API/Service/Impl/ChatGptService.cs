using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using TaskStorm.Data;
using TaskStorm.Exception.UserException;
using TaskStorm.Log;
using TaskStorm.Model.Entity;

namespace TaskStorm.Service.Impl;

public class ChatGptService : IChatGptService
{
    private readonly string GetAllUsersEndpoint = "/api/v1/users/all";
    private readonly String _ChatServerAddress = Environment.GetEnvironmentVariable("CHAT_SERVER_ADDRESS") ?? "localhost";
    private readonly String _ChatServerPort = Environment.GetEnvironmentVariable("CHAT_SERVER_PORT") ?? "6969";
    private readonly String _baseUrl;

    private readonly ILogger<ChatGptService> l;
    private readonly HttpClient _http;
    private readonly PostgresqlDbContext _db;

   public ChatGptService(ILogger<ChatGptService> l, HttpClient http, PostgresqlDbContext db)
    {
        _baseUrl = $"http://{_ChatServerAddress}:{_ChatServerPort}";
        this.l = l;
        _http = http;
        _db = db;
    }
    public async Task<List<User>> GetAllChatGptUsersAsync()
    {
        string uri = _baseUrl + GetAllUsersEndpoint;
        l.LogDebug("Fetching ChatGPT users from " + _baseUrl);
        var response = await _http.GetAsync(uri);
        l.LogDebug("Response is {} , responseToString: {}" , response, response.ToString());
        response.EnsureSuccessStatusCode();
        List<ChatGptUser> chatGptUsers = await response.Content.ReadFromJsonAsync<List<ChatGptUser>>() ?? new List<ChatGptUser>();
        List<User> users = await RegisterSlackUsers(chatGptUsers);
        l.LogDebug($"Fetched {chatGptUsers.Count} ChatGPT users");
        return users;
    }

    public async Task<List<User>> RegisterSlackUsers(List<ChatGptUser> chatGptUsers)
    {
        l.LogDebug($"Triggered RegisterSlackUsers with {chatGptUsers.Count} users: {string.Join(", ", chatGptUsers.Select(u => u.slackUserId))}");
        l.LogDebug($"Users are: {string.Join(", ", chatGptUsers.Select(u => u.slackUserId + " - " + u.slackName))}");
        List<User> users = new List<User>();
        foreach (var chatGptUser in chatGptUsers)
        {
            l.LogDebug($"Registering {chatGptUser}");
            if (chatGptUser == null) { throw new ArgumentException("Slack registration request is null"); }
            if (chatGptUser.slackUserId == null || chatGptUser.slackUserId.Trim() == "") { throw new ArgumentException("Slack name is null or empty"); }
            if (chatGptUser.slackName == null || chatGptUser.slackName.Trim() == "") 
            { 
                l.LogError($"Error registering {chatGptUser} - real slackName missing");
                continue; 
            }
            User user;
            if (!_db.Users.Any(u => u.SlackUserId == chatGptUser.slackUserId))
            {
                l.LogDebug($"Registering user with SlackUserId {chatGptUser.slackUserId}");
                user = new User(chatGptUser.slackName, chatGptUser.slackUserId);
                users.Add(user);
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
        }
        return users;
    }
}
