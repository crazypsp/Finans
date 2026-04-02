namespace Finans.Application.Abstractions.Integration
{
    public interface IConnectorPolicyService
    {
        Task<(bool IsAllowed, string? Message)> CanTransferAsync(
            int companyId,
            string machineName,
            string version,
            CancellationToken ct = default);
    }
}