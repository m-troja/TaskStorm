using Microsoft.EntityFrameworkCore;
using TaskStorm.Data;
using TaskStorm.Exception;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;

namespace TaskStorm.Service.Impl;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    public static string UploadFolder ;
    private readonly PostgresqlDbContext _db;
    private readonly ILogger<FileService> _logger;
    public static string FullFilePath;

    public FileService(IWebHostEnvironment env,  PostgresqlDbContext _db, ILogger<FileService> _logger)
    {
        _env = env;
        this._db = _db;
        this._logger = _logger;
        UploadFolder = Environment.GetEnvironmentVariable("TS_FILE_UPLOAD_FOLDER") ?? "uploads";
        FullFilePath = Path.Combine(_env.ContentRootPath, UploadFolder);
        BuildResourceFolder();
    }

    public async Task<int> SaveImageAsync(IFormFile file, string commentId)
    {
        if (!ValidateFile(file))
            throw new InvalidOperationException("Invalid file type.");

        var GuidWithExtension = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var absolutePath = Path.Combine(FullFilePath, GuidWithExtension);
        var relativePath = Path.Combine(UploadFolder, GuidWithExtension).Replace("\\", "/");

        await CreateFile(absolutePath, file);

        return await SaveFileIntoDatabase(commentId.ToString(), GuidWithExtension, file.Name);

    }

    private async Task CreateFile(string filePath, IFormFile file)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

        }
        catch
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            throw;
        }
    }

    private async Task<int> SaveFileIntoDatabase(string commentId, string guidWithExtension, string realFileName)
    {
        _logger.LogDebug("Saving file metadata for {fileName} into database for comment {commentId}", guidWithExtension, commentId);

        int commentIdInt = int.Parse(commentId);
        var Attachment = new CommentAttachment();
        Attachment.CommentId = commentIdInt;
        Attachment.Guid = guidWithExtension;
        Attachment.Path = Path.Combine( UploadFolder, guidWithExtension).Replace("\\", "/");
        Attachment.FileName = realFileName;
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentIdInt);
        if (comment == null) throw new ContentNotFoundException("Comment not found");
        _logger.LogDebug("Found comment with ID {commentIdInt} for file attachment", commentIdInt);
        Attachment.Comment = comment;

        CommentAttachment result = null;
        try
        {
            await _db.Attachments.AddAsync(Attachment);
            await _db.SaveChangesAsync();
            result = await _db.Attachments.FirstOrDefaultAsync(a => a.Guid == guidWithExtension);
            if (result == null)
            {
                _logger.LogError("Saved entity was not found in the DB");
            }
            _logger.LogDebug("File metadata for {guidWithExtension} saved into database with ID {result.Id}", guidWithExtension, result.Id);
        }
        catch (Npgsql.PostgresException ex)
        {
            _logger.LogError("error saving file into DB: {}", ex);
        }

        return result.Id;
    }

    private bool ValidateFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var allowedContentTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        var ext = Path.GetExtension(file.FileName)
                      .ToLowerInvariant();

        bool isValid = allowedExtensions.Contains(ext)
                       && allowedContentTypes.Contains(file.ContentType);
        _logger.LogDebug("Validating file {fileName}: extension {ext}, content type {contentType} - valid: {isValid}", file.FileName, ext, file.ContentType, isValid);
      
        return isValid;
    }

    private void BuildResourceFolder()
    {
        var uploadsRoot = Path.Combine(_env.ContentRootPath, UploadFolder);

        if (!Directory.Exists(uploadsRoot))
            Directory.CreateDirectory(uploadsRoot);
        _logger.LogDebug("Ensured that upload directory exists at {uploadsRoot}", uploadsRoot);

    }
    public async Task DeleteFileAsync(int id)
    {
        var file = await _db.Attachments.Include(a => a.Comment).FirstOrDefaultAsync(a => a.Id == id);
        _logger.LogDebug("Attempting to delete file with ID {id}", id);
        try
        {
            await _db.Database.ExecuteSqlAsync($"DELETE FROM Attachments WHERE id = {id}");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error deleting file with ID {id} from database", id);
            throw;
        }
        _logger.LogDebug("Deleted file metadata with ID {id} from database", id);
    }


    public async Task<CommentAttachment>? GetFileById(int id)
    {
        _logger.LogDebug($"Retrieving attachment with ID {id} from database", id);
        var file = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == id);
        if (file == null)
        {
            _logger.LogWarning($"Attachment with ID {id} not found in database", id);
            return null;
        }
        _logger.LogDebug($"Found attachment: {file.FileName} with path {file.Path}" );
        return file;
    }


}
