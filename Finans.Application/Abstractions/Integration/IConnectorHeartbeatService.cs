namespace Finans.Application.Abstractions.Integration
{
    public interface IConnectorHeartbeatService
    {
        Task RegisterOrUpdateAsync(
            int companyId,
            string machineName,
            string connectorKey,
            string version,
            CancellationToken ct = default);

        Task WriteHeartbeatAsync(
            int companyId,
            string machineName,
            string version,
            string status,
            string? message,
            CancellationToken ct = default);

        Task<bool> IsConnectorActiveAsync(
            int companyId,
            TimeSpan maxAge,
            CancellationToken ct = default);
    }
}