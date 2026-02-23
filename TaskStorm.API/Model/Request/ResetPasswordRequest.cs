namespace TaskStorm.Model.Request;

public record ResetPasswordRequest(
    int id,
    string NewPassword
)
{}
