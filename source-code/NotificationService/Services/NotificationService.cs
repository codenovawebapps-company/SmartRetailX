namespace NotificationService.Services;

public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public void ProcessNotification(
        string userId,
        string orderId,
        string message)
    {
        _logger.LogInformation(
            "Notification for User {UserId}, Order {OrderId}: {Message}",
            userId,
            orderId,
            message);
    }
}
