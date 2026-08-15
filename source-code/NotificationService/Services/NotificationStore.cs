using NotificationService.Models;
using System.Collections.Concurrent;

namespace NotificationService.Services;

public class NotificationStore
{
    public ConcurrentDictionary<int, Notification> Notifications { get; } = new();
    private int _nextId = 0;

    public Notification Add(Notification notification)
    {
        if (notification.Id <= 0)
        {
            notification.Id = Interlocked.Increment(ref _nextId);
        }
        Notifications[notification.Id] = notification;
        return notification;
    }
}
