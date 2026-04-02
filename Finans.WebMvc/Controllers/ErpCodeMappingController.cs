using Finans.Application.Abstractions.ERP;
using Finans.Contracts.ERP;
using Finans.Infrastructure.Queries.ERP;
using Finans.WebMvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finans.WebMvc.Controllers
{
    [Authorize]
    public class ErpCodeMappingController : Controller
    {
        private readonly IErpCodeMappingQuery _query;
        private readonly IErpCodeMappingService _service;

        public ErpCodeMappingController(
            IErpCodeMappingQuery query,
            IErpCodeMappingService service)
        {
            _query = query;
            _service = service;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var rows = await _query.ListAsync(User.GetCompanyId(), ct);
            return View(rows);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ErpCodeMappingDto
            {
                CompanyId = User.GetCompanyId(),
                IsActive = true
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(ErpCodeMappingDto dto, CancellationToken ct)
        {
            dto.CompanyId = User.GetCompanyId();

            try
            {
                await _service.CreateOrUpdateAsync(dto, ct);
                TempData["Msg"] = "Mapping kaydedildi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Err"] = ex.Message;
                return View(dto);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await _service.DeleteAsync(User.GetCompanyId(), id, ct);
                TempData["Msg"] = "Mapping silindi.";
            }
            catch (Exception ex)
            {
                TempData["Err"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
