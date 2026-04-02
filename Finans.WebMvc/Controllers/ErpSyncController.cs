using Finans.Application.Abstractions.ERP;
using Finans.WebMvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finans.WebMvc.Controllers
{
    [Authorize]
    public class ErpSyncController : Controller
    {
        private readonly IErpAccountSyncService _syncService;

        public ErpSyncController(IErpAccountSyncService syncService)
        {
            _syncService = syncService;
        }

        [HttpPost]
        public async Task<IActionResult> SyncAll(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();
            try
            {
                await _syncService.SyncGlAccountsAsync(companyId, ct);
                await _syncService.SyncCurrentAccountsAsync(companyId, ct);
                await _syncService.SyncBankAccountsAsync(companyId, ct);
                TempData["Msg"] = "ERP hesap planı senkronizasyonu tamamlandı.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = $"Senkronizasyon hatası: {ex.Message}";
            }
            return RedirectToAction("Index", "Dashboard");
        }
    }
}