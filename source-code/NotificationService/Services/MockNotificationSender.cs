using NotificationService.Models;

namespace NotificationService.Services;

public class MockNotificationSender
{
    private readonly ILogger<MockNotificationSender> _logger;

    public MockNotificationSender(ILogger<MockNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendNotificationAsync(Notification notification)
    {
        try
        {
            _logger.LogInformation(">>> MOCK NOTIFICATION SENDER <<<");
            _logger.LogInformation("  [SENDING] Sending mock notification ID: {Id} to User: {UserId}", notification.Id, notification.UserId);
            _logger.LogInformation("  [TYPE] {Type}", notification.Type);
            _logger.LogInformation("  [MESSAGE] \"{Message}\"", notification.Message);
            _logger.LogInformation("  [ORDER ID] {OrderId}", notification.OrderId.HasValue ? notification.OrderId.Value.ToString() : "N/A");
            _logger.LogInformation(">>> NOTIFICATION SENT SUCCESS <<<");

            notification.Status = "Sent";
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send mock notification ID: {Id}", notification.Id);
            notification.Status = "Failed";
            return Task.FromResult(false);
        }
    }
}
