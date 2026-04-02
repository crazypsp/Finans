using Finans.Contracts.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Infrastructure.Queries.Dashboard
{
    public interface IDashboardQuery
    {
        Task<DashboardSummaryDto> GetSummaryAsync(int companyId, DateTime? start, DateTime? end, CancellationToken ct);
    }
}
