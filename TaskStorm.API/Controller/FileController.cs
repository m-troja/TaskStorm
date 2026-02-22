using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using TaskStorm.Exception;
using TaskStorm.Exception.Tokens;
using TaskStorm.Exception.UserException;
using TaskStorm.Log;
using TaskStorm.Model.DTO.Cnv;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Model.Response;
using TaskStorm.Security;
using TaskStorm.Service;
using TaskStorm.Service.Impl;

namespace TaskStorm.Controller;

[ApiController]
[Route("api/v1/file")]
public class FileController : ControllerBase
{
    private readonly ILogger<FileController> l;
    private readonly IFileService _fileService;


    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AttachmentCreatedResponse>> Upload(
        [FromForm] FileUploadRequest request)
    {
        l.LogDebug("POST api/v1/file: commentId={commentId}, fileName={fileName}, Name={name}, fileSize={fileSize}",
            request.CommentId, request.File?.FileName, request.File?.Name, request.File?.Length);

        if (request.File == null || request.File.Length == 0)
        {
            l.LogError("Invalid file upload attempt: No file provided or file is empty, file {}", request.File?.FileName);
            return BadRequest("Invalid file");
        }

        var id = await _fileService.SaveImageAsync(request.File, request.CommentId);

        return Ok(new AttachmentCreatedResponse(
            ResponseType.FILE_UPLOADED_OK,
            id,
            int.Parse(request.CommentId)
        ));
    }
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        l.LogDebug("FileController DELETE file with id: {id}", id);

        try
        {
            await _fileService.DeleteFileAsync(id);
            return NoContent();
        }
        catch (ContentNotFoundException ex)
        {
            l.LogWarning("Delete failed: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (System.Exception ex)
        {
            l.LogError(ex, "Unexpected error while deleting file with id: {id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetFile(int id)
    {
        l.LogDebug("FileController GET file with id: {id}", id);

        var attachment = await _fileService.GetFileById(id);
        if (attachment == null)
        {
            l.LogError($"File ID not found: {id}");
            return NotFound("File not found");
        }
        
        var absolutePath = Path.Combine(FileService.FullFilePath, attachment.Guid);

        if (!System.IO.File.Exists(absolutePath))
        {
            l.LogError($"File not found on disk: {absolutePath}" );
            return NotFound("File not found on disk");
        }

        var contentType = attachment.FileName.EndsWith(".png") ? "image/png" :
                          attachment.FileName.EndsWith(".jpg") || attachment.FileName.EndsWith(".jpeg") ? "image/jpeg" :
                          attachment.FileName.EndsWith(".webp") ? "image/webp" :
                          "application/octet-stream";

        var fileStream = System.IO.File.OpenRead(absolutePath);
        
        l.LogDebug("Serving file {fileName} with content type {contentType}", attachment.FileName, contentType);

        return File(fileStream, contentType, attachment.Guid);
    }

    public FileController(ILogger<FileController> l, IFileService fileService)
    {
        _fileService = fileService;
        this.l = l;
    }
}


