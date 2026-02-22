using System.ComponentModel.DataAnnotations;

namespace TaskStorm.Model.Request;

public class FileUploadRequest
{
    [Required]
    public IFormFile File { get; set; }

    [Required]
    public string CommentId { get; set; }
}