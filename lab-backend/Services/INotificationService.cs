namespace LabManagementAPI.Services;

public interface INotificationService
{
    Task NotifyUserAsync(int userId, string type, string title, string message, string url, CancellationToken cancellationToken);
    Task NotifyUsersAsync(IEnumerable<int> userIds, string type, string title, string message, string url, CancellationToken cancellationToken);
    Task NotifyManagersAsync(string type, string title, string message, string url, CancellationToken cancellationToken);
}
