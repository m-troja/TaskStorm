using TaskStorm.Model.IssueFolder;

namespace TaskStorm.Model.Response;

public record AttachmentCreatedResponse(
    ResponseType responseType, 
    int fileId,
    int commentId
)
{}
