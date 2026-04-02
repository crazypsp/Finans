using Finans.Application.Abstractions.Security;
using Finans.Application.Abstractions.Transfer;
using Finans.Infrastructure.Queries.Transfer;
using Finans.WebMvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finans.WebMvc.Controllers
{
    [Authorize]
    public class ErpTransferController : Controller
    {
        private readonly IErpTransferBatchQuery _batchQuery;
        private readonly ITransferRetryService _retryService;
        private readonly ICompanyAuthorizationService _authorizationService;

        public ErpTransferController(IErpTransferBatchQuery batchQuery, ITransferRetryService retryService, ICompanyAuthorizationService authorizationService)
        {
            _batchQuery = batchQuery;
            _retryService = retryService;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();
            var rows = await _batchQuery.ListAsync(companyId, ct);
            return View(rows);
        }

        [HttpPost]
        public async Task<IActionResult> RetryItem(int id, CancellationToken ct)
        {
            var companyId = User.GetCompanyId();

            if (!await _authorizationService.CanAccessTransferItemAsync(companyId, id, ct))
            {
                TempData["Err"] = "Bu kayıt için yetkin yok.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _retryService.RetryItemAsync(companyId, id, ct);
                TempData["Msg"] = "Kayıt tekrar kuyruğa alındı.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

    }
}