using Finans.Contracts.ERP;

namespace Finans.Infrastructure.Queries.ERP
{
    public interface IErpCodeMappingQuery
    {
        Task<IReadOnlyList<ErpCodeMappingDto>> ListAsync(int companyId, CancellationToken ct);
    }
}