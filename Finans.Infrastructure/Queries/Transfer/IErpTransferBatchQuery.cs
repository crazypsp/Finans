using Finans.Contracts.Transfer;

namespace Finans.Infrastructure.Queries.Transfer
{
    public interface IErpTransferBatchQuery
    {
        Task<IReadOnlyList<ErpTransferBatchListDto>> ListAsync(int companyId, CancellationToken ct);
    }
}