using Finans.Application.Abstractions.Transfer;
using Finans.Infrastructure.Queries.Transfer;
using Finans.WebMvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finans.WebMvc.Controllers
{
    [Authorize]
    public class ErpTransferFailedController : Controller
    {
        private readonly IFailedTransferQuery _query;
        private readonly ITransferRetryService _retryService;

        public ErpTransferFailedController(
            IFailedTransferQuery query,
            ITransferRetryService retryService)
        {
            _query = query;
            _retryService = retryService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();
            var rows = await _query.ListAsync(companyId, ct);
            return View(rows);
        }

        [HttpPost]
        public async Task<IActionResult> RetryItem(int id, CancellationToken ct)
        {
            try
            {
                await _retryService.RetryItemAsync(User.GetCompanyId(), id, ct);
                TempData["Msg"] = "Kayıt tekrar kuyruğa alındı.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RetrySelected([FromForm] List<int> selectedIds, CancellationToken ct)
        {
            try
            {
                await _retryService.RetryItemsAsync(User.GetCompanyId(), selectedIds, ct);
                TempData["Msg"] = "Seçilen kayıtlar tekrar kuyruğa alındı.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
