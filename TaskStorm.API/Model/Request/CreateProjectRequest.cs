namespace TaskStorm.Model.Request;

public record CreateProjectRequest(string shortName, string? description)
{
}
