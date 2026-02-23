using TaskStorm.Model.DTO;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;

namespace TaskStorm.Service
{
    public interface IUserService
    {
        Task<int> GetIdBySlackUserId(string slackUserId);
        Task<User> GetByIdAsync(int id);
        Task<User?> TryGetByEmailAsync(string email);
        Task<User> GetByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<List<User>> GetAllUsersAsync();
        Task<UserDto> GetUserBySlackUserIdAsync(string slackUserId);
        Task DeleteAllUsers();
        Task DeleteUserById(int id);
        Task<bool> SaveRefreshTokenAsync(RefreshToken refreshToken);
        Task<User> GetUserByRefreshTokenAsync(string token);
        Task<User> ResetPassword(ResetPasswordRequest req);
        Task<User> UpdateRole(UpdateRoleRequest req);
    }
}
