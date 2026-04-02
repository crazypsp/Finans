using Finans.Contracts.Reports;
using Finans.Infrastructure.Queries.Reports;
using Finans.WebMvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finans.WebMvc.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IReportQuery _reportQuery;

        public ReportController(IReportQuery reportQuery)
        {
            _reportQuery = reportQuery;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();

            var bankFilter = new BankImportReportFilterDto { CompanyId = companyId };
            var erpFilter = new ErpTransferReportFilterDto { CompanyId = companyId };
            var connectorFilter = new ConnectorReportFilterDto { CompanyId = companyId };

            ViewBag.Summary = await _reportQuery.GetDashboardSummaryAsync(companyId, ct);
            ViewBag.BankImportRows = await _reportQuery.GetBankImportReportAsync(bankFilter, ct);
            ViewBag.ErpTransferRows = await _reportQuery.GetErpTransferReportAsync(erpFilter, ct);
            ViewBag.ConnectorRows = await _reportQuery.GetConnectorReportAsync(connectorFilter, ct);

            ViewBag.BankFilter = bankFilter;
            ViewBag.ErpFilter = erpFilter;
            ViewBag.ConnectorFilter = connectorFilter;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(
            BankImportReportFilterDto bankFilter,
            ErpTransferReportFilterDto erpFilter,
            ConnectorReportFilterDto connectorFilter,
            CancellationToken ct)
        {
            var companyId = User.GetCompanyId();

            bankFilter.CompanyId = companyId;
            erpFilter.CompanyId = companyId;
            connectorFilter.CompanyId = companyId;

            ViewBag.Summary = await _reportQuery.GetDashboardSummaryAsync(companyId, ct);
            ViewBag.BankImportRows = await _reportQuery.GetBankImportReportAsync(bankFilter, ct);
            ViewBag.ErpTransferRows = await _reportQuery.GetErpTransferReportAsync(erpFilter, ct);
            ViewBag.ConnectorRows = await _reportQuery.GetConnectorReportAsync(connectorFilter, ct);

            ViewBag.BankFilter = bankFilter;
            ViewBag.ErpFilter = erpFilter;
            ViewBag.ConnectorFilter = connectorFilter;

            return View();
        }
    }
}
