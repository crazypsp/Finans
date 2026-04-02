using Finans.Contracts.ERP;

namespace Finans.Application.Abstractions.ERP
{
    public interface IErpCodeMappingService
    {
        Task CreateOrUpdateAsync(ErpCodeMappingDto dto, CancellationToken ct = default);
        Task DeleteAsync(int companyId, int id, CancellationToken ct = default);
    }
}