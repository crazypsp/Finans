using Finans.Contracts.Reports;

namespace Finans.Infrastructure.Queries.Reports
{
    public interface IReportQuery
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(int companyId, CancellationToken ct);

        Task<IReadOnlyList<BankImportReportRowDto>> GetBankImportReportAsync(
            BankImportReportFilterDto filter,
            CancellationToken ct);

        Task<IReadOnlyList<ErpTransferReportRowDto>> GetErpTransferReportAsync(
            ErpTransferReportFilterDto filter,
            CancellationToken ct);

        Task<IReadOnlyList<ConnectorReportRowDto>> GetConnectorReportAsync(
            ConnectorReportFilterDto filter,
            CancellationToken ct);
    }
}
