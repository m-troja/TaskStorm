namespace TaskStorm.Model.Request;

public record FileUploadRequest // form
    (
    
    IFormFile File,
        string CommentId
     )
{
}
