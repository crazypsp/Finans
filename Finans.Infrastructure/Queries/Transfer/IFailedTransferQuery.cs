using Finans.Contracts.Transfer;

namespace Finans.Infrastructure.Queries.Transfer
{
    public interface IFailedTransferQuery
    {
        Task<IReadOnlyList<FailedTransferItemDto>> ListAsync(int companyId, CancellationToken ct);
    }
}
