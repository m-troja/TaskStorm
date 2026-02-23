using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using TaskStorm.Data;
using TaskStorm.Exception.UserException;
using TaskStorm.Model.Entity;
using TaskStorm.Log;
using TaskStorm.Model.DTO;
using TaskStorm.Model.DTO.Cnv;
using System.ComponentModel;
using TaskStorm.Exception.Tokens;
using TaskStorm.Security;
using TaskStorm.Model.Request;
using TaskStorm.Exception;

namespace TaskStorm.Service.Impl;

public class UserService : IUserService
{
    private string SlackBotSlackUserId = "USLACKBOT";
    private readonly PostgresqlDbContext _db;
    private readonly ILogger<UserService> l;
    private readonly UserCnv _userCnv;
    private readonly IChatGptService _chatGptService;
    private readonly IPasswordService _passwordService;

    public UserService(PostgresqlDbContext db, ILogger<UserService> logger, UserCnv userCnv, IChatGptService _chatGptService,
        IPasswordService _passwordService
        )
    {
        _db = db;
        l = logger;
        _userCnv = userCnv;
        this._chatGptService = _chatGptService;
        this._passwordService = _passwordService;
    }
    public async Task<User> GetByIdAsync(int id)
    {
        l.LogDebug($"Fetching user by id {id}");
        User? user = await _db.Users
            .Include(u => u.Roles)
            .Include(u => u.Teams)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new UserNotFoundException("User by id '" + id + "' was not found");
        l.LogDebug("User fetched: " + user);
        return user;
    }
    
    public async Task<User> CreateUserAsync(User user)
    {
        l.LogDebug("Creating user: " + user);
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        l.LogDebug("User created successfully: " + user);

        return user;
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        User? user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            l.LogError("User by email '" + email + "' was not found");
            throw new UserNotFoundException("User by email '" + email + "' was not found");
        }
        return user;
    }
    public async Task<User?> TryGetByEmailAsync(string email)
    {
        User? user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == email);
        
        return user;
    }

    public async Task UpdateUserAsync(User user) 
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        l.LogDebug("Fetching all uses from db");
        List<User> users = await _db.Users.Include(u => u.Roles).ToListAsync();
        l.LogDebug($"Fetched {users.Count} users");
        return users;
    }

    public async Task<UserDto> GetUserBySlackUserIdAsync(string slackUserId)
    {
        l.LogDebug($"Fetching user by Slack user ID: {slackUserId}");
        User? user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.SlackUserId == slackUserId);
        if (user == null)
        {
            l.LogDebug($"User with Slack user ID '{slackUserId}' not found");
            throw new UserNotFoundException("User by Slack user ID '" + slackUserId + "' was not found");
        }
        l.LogDebug("User fetched: " + user);
        UserDto userDto = _userCnv.ConvertUserToDto(user);
        return userDto;
    }

    public async Task<int> GetIdBySlackUserId(string slackUserId)
    {
        if (string.IsNullOrEmpty(slackUserId)) { throw new ArgumentException("Slack user ID cannot be null or empty", slackUserId); }
        l.LogDebug($"Getting user ID by Slack user ID: {slackUserId}");

        int id = await _db.Users.Where(u => u.SlackUserId == slackUserId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        l.LogDebug($"First fetch of user ID: {id} for Slack user ID: {slackUserId}");


        if (id == 0)
        {
            l.LogDebug($"User with Slack user ID '{slackUserId}' not found - calling ChatGPT API");
            var users = await _chatGptService.GetAllChatGptUsersAsync();
            id = users.Find(u => u.SlackUserId == slackUserId)?.Id ?? 0;
            l.LogDebug($"Second fetch of user ID: {id} for Slack user ID: {slackUserId}"); 
            if (id != 0)
            {
                l.LogDebug($"User with Slack user ID '{slackUserId}' found after ChatGPT sync: ID {id}");
                return id;
            }
        }

        if (id == 0)
        {
            l.LogDebug($"User with Slack user ID '{slackUserId}' not found - assigning bot");
            var BotUser = await _db.Users.Where(u => u.SlackUserId == SlackBotSlackUserId).FirstAsync();
            l.LogDebug($"Bot fetched: {BotUser}");
            return BotUser.Id;
        }
        return id;
    }
    public async Task DeleteAllUsers()
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM Users");
        l.LogInformation("Deleted all Users from database");
    }

    public async Task DeleteUserById(int id)
    {
        await _db.Database.ExecuteSqlAsync($"DELETE FROM Users WHERE id = {id}");
        l.LogInformation($"Deleted User by Id={id}");
    }

    public async Task<bool> SaveRefreshTokenAsync(RefreshToken refreshToken)
    {
        l.LogDebug($"Saving RefreshToken = {refreshToken}");
        var existingToken = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);

        if (existingToken != null)
        {
            if (existingToken.Expires <= DateTime.UtcNow)
            {
                l.LogDebug($"Existing RefreshToken for UserId = {refreshToken.UserId} is expired - deleting it and adding new token.");
                _db.RefreshTokens.Remove(existingToken);
                return false;
            }
            else
            {
                l.LogDebug($"A valid refreshToken for UserId = {refreshToken.UserId} exists - skipping saving.");
                return false;
            }

        }
        l.LogDebug($"No existing RefreshToken for UserId = {refreshToken.UserId}. Adding new token.");

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<RefreshToken?> GetRefreshTokenAsync(int userId)
    {
        l.LogDebug($"Fetching RefreshToken for UserId = {userId}");
        RefreshToken? refreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId);
        if (refreshToken != null)
        {
            if (refreshToken.IsRevoked || refreshToken.Expires <= DateTime.UtcNow)
            {
                l.LogDebug($"RefreshToken for UserId = {userId} is revoked or expired, deleting it");
                _db.RefreshTokens.Remove(refreshToken);
                return null;
            }
            else
            {
                l.LogDebug($"RefreshToken for UserId = {userId} is valid.");
                return refreshToken;
            }
        }
        else
        {
            l.LogDebug($"No RefreshToken found for UserId = {userId}.");
            return null;
        }
    }
    public async Task<User> GetUserByRefreshTokenAsync(string token)
    {
       l.LogDebug($"Fetching User by RefreshToken = {token}");
        var refreshToken = await _db.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.Roles)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null)
        {
            l.LogDebug("No RefreshToken found.");
            throw new InvalidRefreshTokenException("Invalid refresh token");
        }
        if (refreshToken.IsRevoked || refreshToken.Expires <= DateTime.UtcNow)
        {
            l.LogDebug("Expired is revoked or expired.");
            throw new InvalidRefreshTokenException("Expired refresh token");
        }
        l.LogDebug($"User fetched: {refreshToken.User}");
        return refreshToken.User!;
    }

    public async Task<User> ResetPassword(ResetPasswordRequest req)
    {
        l.LogDebug($"Resetting password for user with id: {req.id}");
        User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.id);
        if (user == null)
        {
            l.LogDebug($"User with id '{req.id}' not found");
            throw new UserNotFoundException("User by email '" + req.id + "' was not found");
        }
        byte[] salt = _passwordService.GenerateSalt();
        string hashedPassword = _passwordService.HashPassword(req.NewPassword, salt);
        user.Password = hashedPassword;
        user.Salt = salt;
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        l.LogDebug($"Password reset successfully for user with email: {req.id}");
        return user;
    }
    public async Task<User> UpdateRole(UpdateRoleRequest req)
    {
        l.LogDebug($"Updating role for user with id: {req.userId}, addRoleId: {req.addRoleId}, removeRoleId: {req.removeRoleId}");
        User? user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == req.userId);
        if (user == null)
        {
            l.LogDebug($"User with id '{req.userId}' not found");
            throw new UserNotFoundException("User by email '" + req.userId + "' was not found");
        }
        var addRole = await _db.Roles.FirstOrDefaultAsync(r => r.Id == req.addRoleId);
        var removeRole = await _db.Roles.FirstOrDefaultAsync(r => r.Id == req.removeRoleId);
        if (addRole != null && !user.Roles.Any(r => r.Id == addRole.Id))
        {
            user.Roles.Add(addRole);
            l.LogDebug($"Added role '{addRole.Name}' to user with id: {req.userId}");
        }
        if (removeRole != null && user.Roles.Any(r => r.Id == removeRole.Id))
        {
            if (removeRole.Name == Role.ROLE_ADMIN)
            {
                int adminCount = await _db.Users.CountAsync(u => u.Roles.Any(r => r.Name == Role.ROLE_ADMIN) && u.Id != user.Id);
                if (adminCount == 0)
                {
                    l.LogDebug($"Cannot remove role '{removeRole.Name}' from user with id: {req.userId} because they are the last admin");
                    throw new BadRequestException("Cannot remove the last admin role from the system");
                }
            }
            user.Roles.Remove(removeRole);
            l.LogDebug($"Removed role '{removeRole.Name}' from user with id: {req.userId}");
        }
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        l.LogDebug($"Roles updated successfully for user with id: {req.userId}");
        return user;
    }

}