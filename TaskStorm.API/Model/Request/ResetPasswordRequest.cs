namespace TaskStorm.Model.Request;

public record ResetPasswordRequest(
    int userId,
    string NewPassword
)
{}
