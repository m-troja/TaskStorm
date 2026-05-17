using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using TaskStorm.Data;
using TaskStorm.Event;
using TaskStorm.Service;
using TaskStorm.Tools;

namespace TaskStorm.Controller;

[Authorize]
[ApiController]
[Route("api/v1/notifications")]
public class NotificationController : ControllerBase
{
    private readonly ILogger<NotificationController> l;
    private readonly INotificationService _NotificationService;

    [HttpGet("me")]
    public async Task<ActionResult<PagedResult<Notification>>> GetUserNotifications([FromQuery] int qty)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var notifications = await _NotificationService.GetNotificationsForUserAsync(userId, qty);
        if (notifications == null || !notifications.Items.Any()) return new EmptyResult();

        return Ok(notifications);
    }

    [HttpPut("read/{id:int}")]
    public async Task<ActionResult<PagedResult<Notification>>> Read( int id)
    {
        Notification notification = null!;
        try
        {
            notification = await _NotificationService.MarkAsReadAsync(id);
        }
        catch (BadHttpRequestException ex)
        {
            l.LogWarning(ex, $"Notification with ID: {id} was not found.");
            return NotFound(ex.Message);
        }
        catch(InvalidOperationException ex) {
            l.LogWarning(ex, $"Notification with ID: {id} is already marked as read.");
            return BadRequest(ex.Message);
        }
        return Ok(notification);
    }

    public NotificationController(ILogger<NotificationController> logger, INotificationService notificationService)
    {
        l = logger;
        _NotificationService = notificationService;
    }
}
