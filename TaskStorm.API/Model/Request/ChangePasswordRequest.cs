namespace TaskStorm.Model.Request;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
)
{}
