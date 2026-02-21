using TaskStorm.Model.DTO;
using TaskStorm.Model.Entity;

namespace TaskStorm.Model.Response;

public record TokenResponseDto(
    AccessToken AccessToken,
    RefreshTokenDto RefreshToken)
{}