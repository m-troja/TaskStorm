using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TaskStorm.Data;
using TaskStorm.Exception.Registration;
using TaskStorm.Exception.UserException;
using TaskStorm.Log;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Security;
using TaskStorm.Service;

namespace TaskStorm.Service.Impl;

public class RegisterService : IRegisterService
{
    private readonly PostgresqlDbContext _db;
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<RegisterService> _logger;
    private readonly IChatGptService _chatGptService;

    public RegisterService(
        PostgresqlDbContext db,
        IUserService userService,
        IRoleService roleService,
        IPasswordService passwordService,
        ILogger<RegisterService> logger,
        IChatGptService chatGptService)
    {
        _db = db;
        _userService = userService;
        _roleService = roleService;
        _passwordService = passwordService;
        _logger = logger;
        _chatGptService = chatGptService;
    }

    public async Task<User> Register(RegistrationRequest request)
    {
        if (request == null)
        {
            _logger.LogError("Registration request is null");
            throw new RegisterEmailException("Registration request cannot be null");
        }

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            _logger.LogError("Registration failed: Missing required fields");
            throw new RegisterEmailException("Missing required fields");
        }

        string email = request.Email.Trim().ToLower();
        _logger.LogInformation($"Received registration request for email: {email}");

        if (!new EmailAddressAttribute().IsValid(email))
        {
            _logger.LogError($"Invalid email address: {email}");
            throw new RegisterEmailException("Invalid email address");
        }

        if (!await IsEmailAvailableAsync(email))
        {
            _logger.LogWarning($"Email already registered: {email}");
            throw new UserAlreadyExistsException("Email already registered");
        }

        
        // Generate salt and hash password
        byte[] salt = _passwordService.GenerateSalt();
        string hashedPassword = _passwordService.HashPassword(request.Password, salt);

        // Get role
        Role role = await _roleService.GetRoleByName(Role.ROLE_USER);

        var userBySlackUserId = await GetUserBySlackUserId(request.SlackUserId);
        if (userBySlackUserId != null)
        {
            userBySlackUserId.FirstName = request.FirstName;
            userBySlackUserId.LastName = request.LastName;
            userBySlackUserId.Email = email;
            userBySlackUserId.Password = hashedPassword;
            userBySlackUserId.Salt = salt;
            userBySlackUserId.Disabled = false;
            userBySlackUserId.Roles.Add(role);
            _db.Users.Update(userBySlackUserId);
            await _db.SaveChangesAsync();
            _logger.LogInformation($"User registered successfully with SlackUserId: {request.SlackUserId}");
            return userBySlackUserId;
        }
        var user = new User(request.FirstName, request.LastName, email, hashedPassword, salt, role) { SlackUserId = request.SlackUserId };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation($"User registered successfully: {email}");
        return user;
    }

    private async Task<User?> GetUserBySlackUserId(string slackUserId)
    {
        var user = await _db.Users.Where(u => u.SlackUserId == slackUserId).FirstOrDefaultAsync();
        if (user == null)
        {
            _logger.LogDebug("No user found with SlackUserId: {SlackUserId}", slackUserId);
        }
        else
        {
            _logger.LogDebug($"Fetched user by SlackUserId: {user}", user);
        }
        return user;
    }
    private async Task<bool> IsEmailAvailableAsync(string email)
    {
        var existingUser = await _userService.TryGetByEmailAsync(email);
        return existingUser == null;
    }
}
