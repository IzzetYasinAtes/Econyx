namespace Econyx.Application.Ports;

public interface INotificationService
{
    Task SendTradeNotificationAsync(string message, CancellationToken ct = default);
    Task SendAlertAsync(string title, string message, CancellationToken ct = default);
}
