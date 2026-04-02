namespace Finans.Application.Abstractions.Security
{
    public interface ICompanyAuthorizationService
    {
        Task<bool> CanAccessTransferItemAsync(int companyId, int transferItemId, CancellationToken ct = default);
        Task<bool> CanAccessBatchAsync(int companyId, int batchId, CancellationToken ct = default);
    }
}
