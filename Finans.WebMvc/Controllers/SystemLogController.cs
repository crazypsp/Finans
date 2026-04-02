using Finans.Application.Abstractions.Logging;
using Finans.Infrastructure.Queries.Logging;
using Finans.WebMvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finans.WebMvc.Controllers
{
    [Authorize]
    public class SystemLogController : Controller
    {
        private readonly ILoggingQuery _query;
        private readonly INotificationService _notificationService;

        public SystemLogController(
            ILoggingQuery query,
            INotificationService notificationService)
        {
            _query = query;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();

            ViewBag.AuditLogs = await _query.GetAuditLogsAsync(companyId, ct);
            ViewBag.Notifications = await _query.GetNotificationsAsync(companyId, ct);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(int id, CancellationToken ct)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(User.GetCompanyId(), id, ct);
                TempData["Msg"] = "Bildirim okundu olarak işaretlendi.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
