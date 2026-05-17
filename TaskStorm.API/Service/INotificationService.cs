using TaskStorm.Event;
using TaskStorm.Tools;

namespace TaskStorm.Service;

public interface INotificationService
{

    Task<PagedResult<Notification>> GetNotificationsForUserAsync(int userId,int qty, bool read);
    Task<Notification> MarkAsReadAsync(int notificationId);
}
