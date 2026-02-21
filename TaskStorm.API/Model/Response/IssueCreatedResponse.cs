using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.Response;

public record IssueCreatedResponse(
    ResponseType responseType, 
    string key,
    int id
){}
