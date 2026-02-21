using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;

namespace TaskStorm.Service
{
    public interface IFileService
{
        Task<int> SaveImageAsync(FileUploadRequest req);
        Task DeleteFileAsync(int id);
        Task<CommentAttachment>? GetFileById(int id);
    }
}
