using TaskStorm.Model.Entity;

namespace TaskStorm.Service;

public interface IChatGptService
{
    Task<List<User>> RegisterSlackUsers(List<ChatGptUser> chatGptUser);
    Task<List<User>> GetAllChatGptUsersAsync();
}
