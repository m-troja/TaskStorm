using TaskStorm.Event;
using TaskStorm.Tools;

namespace TaskStorm.Service;

public interface INotificationService
{

    Task<PagedResult<Notification>> GetNotificationsForUserAsync(int userId,int qty);
    Task<Notification> MarkAsReadAsync(int notificationId);
}
