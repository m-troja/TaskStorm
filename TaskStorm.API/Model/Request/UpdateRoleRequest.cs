namespace TaskStorm.Model.Request
{
    public record UpdateRoleRequest
        (
            int userId,
            int? addRoleId,
            int? removeRoleId
        )
    {
    }
}
