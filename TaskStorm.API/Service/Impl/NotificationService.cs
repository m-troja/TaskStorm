using Microsoft.EntityFrameworkCore;
using TaskStorm.Data;
using TaskStorm.Event;
using TaskStorm.Tools;

namespace TaskStorm.Service.Impl;

public class NotificationService : INotificationService
{
        private readonly ILogger<NotificationService> l;
        private readonly PostgresqlDbContext _db;
    
        public NotificationService(ILogger<NotificationService> logger,  PostgresqlDbContext db)
        {
            l = logger;
            _db = db;
        }
    
        public async Task<PagedResult<Notification>> GetNotificationsForUserAsync(int userId, int qty, bool unread)
        {
            l.LogDebug($"Fetching notifications for user with ID: {userId}, unread: {unread}, quantity: {qty}");

        List<Notification> notifications = null!;
        if (qty == 0)  qty = 5;

        switch (unread)
        {
            case true: 
              notifications = await _db.Notifications.Where(n => n.UserId == userId && n.IsRead == unread)
                .OrderByDescending(n => n.CreatedAt)
                .Take(qty)
                .ToListAsync();

            break;

            case false:
                notifications = await _db.Notifications.Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(qty)
                .ToListAsync();

                break;
        }

            l.LogDebug($"Retrieved {notifications.Count()} notifications for user with ID: {userId}");
            
             return new PagedResult<Notification>
            {
                Items = notifications,
                TotalCount = notifications.Count(),
                PageSize = qty,
                PageNumber = 1
            };
        }

    public async Task<Notification> MarkAsReadAsync(int notificationId)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId) ?? null;
        if (notification == null)
        {
            l.LogWarning($"Notification with ID: {notificationId} not found.");
            throw new BadHttpRequestException($"Notification with ID: {notificationId} not found.");
        }
        else if (notification.IsRead == true)
        {
            throw new InvalidOperationException($"Notification with ID: {notificationId} is already marked as read.");
        }
        notification.IsRead = true;
        await _db.SaveChangesAsync();
        l.LogInformation($"Notification with ID: {notificationId} marked as read.");
        return notification;
    }

}
