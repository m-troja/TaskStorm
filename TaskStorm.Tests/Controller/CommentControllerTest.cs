using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TaskStorm.Controller;
using TaskStorm.Model.DTO;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Request;
using TaskStorm.Service;
using Xunit;

namespace TaskStorm.Tests.Controller;
public class CommentControllerTest
{
    private readonly Mock<ICommentService> mc;
    private readonly CommentController _controller;

    public ILogger<CommentController> GetLogger() =>
        new LoggerFactory().CreateLogger<CommentController>();

    public CommentControllerTest()
    {
        mc = new Mock<ICommentService>();
        _controller =  new CommentController(mc.Object, GetLogger());
    }

    [Fact]
    public async Task GetCommentsByIssueId_ShouldReturnComments_WhenCommentsExist()
    {
        var issueId = 1;

        var expectedComments = new List<CommentDto>
        {
            GetCommentDto(1), 
            GetCommentDto(2)
        };

        mc.Setup(s => s.GetCommentsByIssueIdAsync(issueId))
          .ReturnsAsync(expectedComments);

        // when
        var result = await _controller.GetCommentsByIssueId(issueId);

        // then
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returnComments = Assert.IsType<List<CommentDto>>(ok.Value);

        Assert.Equal(expectedComments.Count, returnComments.Count);
        Assert.Equal(expectedComments, returnComments);
    }

    [Fact]
    public async Task DeleteCommentById_ShouldDeleteComment()
    {
        // given
        var commentId = 1;
        mc.Setup(s => s.DeleteCommentById(commentId))
          .Returns(Task.CompletedTask)
          .Verifiable();
        
        // when
        var result = await _controller.DeleteCommentById(commentId);
        
        // then
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var message = Assert.IsType<string>(ok.Value);
        Assert.Equal($"Deleted comment by id={commentId}", message);
        mc.Verify(s => s.DeleteCommentById(commentId), Times.Once);
    }

    [Fact]
    public async Task DeleteAllCommentsByIssueId_ShouldDeleteComments()
    {
        var mc = new Mock<ICommentService>();
        var controller = new CommentController(mc.Object, GetLogger());
        var issueId = 1;
        mc.Setup(s => s.DeleteAllCommentsByIssueId(issueId))
          .Returns(Task.CompletedTask)
          .Verifiable();
        // when
        var result = await controller.DeleteAllCommentsByIssueId(issueId);
        // then
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var message = Assert.IsType<string>(ok.Value);
        Assert.Equal($"Deleted all comments for issue id={issueId}", message);
        mc.Verify(s => s.DeleteAllCommentsByIssueId(issueId), Times.Once);
    }

    [Fact]
    public async Task CreateComment_ShouldCreateComment()
    {
        SetUser(1, Role.ROLE_ADMIN);

        var createRequest = new CreateCommentRequest("Content", 1, 1);
        var expectedDto = GetCommentDto(1);

        mc.Setup(s => s.CreateCommentAsync(createRequest, 1))
          .ReturnsAsync(expectedDto)
          .Verifiable();
        
        // when
        var result = await _controller.CreateComment(createRequest);
        
        // then
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        mc.Verify(s => s.CreateCommentAsync(createRequest, 1), Times.Once);
    }

    //private Comment GetComment(int id)
    //{
    //    return new Comment(id, "content" + " " + id.ToString, 1, 1, DateTime.Parse("20260-02-01"), DateTime.Parse("20260-02-01"),
    //        );
    //}

    private CommentDto GetCommentDto(int id)
    {
        var attachmentIds = new List<int> { 1, 2, 3 };
        return new CommentDto(id, 1, "content" + " " + id.ToString(), 1, DateTime.Parse("2026-02-01"), DateTime.Parse("2026-02-01"), "FirstName LastName",
            attachmentIds, "U123");
    }

    private void SetUser(int id, string role)
    {
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, id.ToString()),
        new Claim(ClaimTypes.Role, role)
    };

        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}
