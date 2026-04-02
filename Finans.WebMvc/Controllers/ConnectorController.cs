using Finans.Infrastructure.Queries.Integration;
using Finans.WebMvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finans.WebMvc.Controllers
{
    [Authorize]
    public class ConnectorController : Controller
    {
        private readonly IDesktopConnectorQuery _query;

        public ConnectorController(IDesktopConnectorQuery query)
        {
            _query = query;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();
            var rows = await _query.ListAsync(companyId, ct);
            return View(rows);
        }
    }
}