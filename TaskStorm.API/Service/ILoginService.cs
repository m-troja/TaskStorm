using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;

namespace TaskStorm.Service;

public interface ILoginService
{
    Task<TokenResponseDto> LoginAsync(LoginRequest lr);
}
