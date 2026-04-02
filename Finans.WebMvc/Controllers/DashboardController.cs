using Finans.Infrastructure.Queries.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Finans.WebMvc.Security;
using Finans.Infrastructure.Queries.Reports;

namespace Finans.WebMvc.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardQuery _query;
        private readonly IHealthQuery _healthQuery;

        public DashboardController(IDashboardQuery query,IHealthQuery healthQuery)
        {
            _query = query;
            _healthQuery = healthQuery;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();
            ViewBag.Health = await _healthQuery.GetSummaryAsync(companyId, ct);
            // Şimdilik CompanyId yoksa 1 kabul ediyoruz.
            // Gün 2'nin ikinci yarısında multi-tenant çözümünü cookie/claim'e koyacağız.

            var dto = await _query.GetSummaryAsync(companyId, null, null, ct);
            return View(dto);
        }
    }
}