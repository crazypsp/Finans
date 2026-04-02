namespace Finans.Application.Abstractions.Logging
{
    public interface INotificationService
    {
        Task CreateAsync(
            int companyId,
            string title,
            string message,
            string level,
            string? source,
            string? referenceId,
            CancellationToken ct = default);

        Task MarkAsReadAsync(int companyId, int id, CancellationToken ct = default);
    }
}
