using Finans.Contracts.Reports;

namespace Finans.Infrastructure.Queries.Reports
{
    public interface IHealthQuery
    {
        Task<SystemHealthSummaryDto> GetSummaryAsync(int companyId, CancellationToken ct);
    }
}