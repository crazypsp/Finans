using Finans.Application.Abstractions.Transfer;
using Finans.Contracts.Transfer;
using Finans.Data.Context;
using Finans.Infrastructure.Queries.Transfer;
using Finans.WebMvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finans.WebMvc.Controllers
{
    [Authorize]
    public class TransferController : Controller
    {
        private readonly ITransferQuery _query;
        private readonly ITransferBatchService _batchService;
        private readonly FinansDbContext _db;

        public TransferController(ITransferQuery query, ITransferBatchService batchService, FinansDbContext db)
        {
            _query = query;
            _batchService = batchService;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();

            // Banka listesi dropdown için
            var banks = await _db.Banks
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.BankName)
                .Select(x => new { x.Id, x.BankName })
                .ToListAsync(ct);

            ViewBag.Banks = banks;

            return View(new TransferFilterDto
            {
                CompanyId = companyId,
                OnlyNotTransferred = true,
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today
            });
        }

        [HttpPost]
        public async Task<IActionResult> Index(TransferFilterDto filter, CancellationToken ct)
        {
            filter.CompanyId = User.GetCompanyId();

            var companyId = filter.CompanyId;

            var banks = await _db.Banks
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.BankName)
                .Select(x => new { x.Id, x.BankName })
                .ToListAsync(ct);

            ViewBag.Banks = banks;

            var list = await _query.ListAsync(filter, ct);
            ViewBag.List = list;

            return View(filter);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBatch([FromForm] List<int> selectedIds, CancellationToken ct)
        {
            if (selectedIds == null || selectedIds.Count == 0)
            {
                TempData["Err"] = "Aktarım için kayıt seçmelisin.";
                return RedirectToAction(nameof(Index));
            }

            var req = new CreateTransferBatchRequestDto
            {
                CompanyId = User.GetCompanyId(),
                UserId = User.GetUserId(),
                BankTransactionIds = selectedIds,
                TransferType = "MANUAL"
            };

            try
            {
                var result = await _batchService.CreateBatchAsync(req, ct);
                TempData["Msg"] = $"Batch oluşturuldu. BatchNo={result.BatchNo}, Kayıt={result.ItemCount}";
            }
            catch (Exception ex)
            {
                TempData["Err"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Modal içinden tek kayıt için GL ve Cari hesap listesi döner.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetErpAccounts(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();

            var glAccounts = await _db.ErpGlAccounts
                .Where(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.GlCode)
                .Select(x => new { x.GlCode, x.GlName })
                .ToListAsync(ct);

            var currentAccounts = await _db.ErpCurrentAccounts
                .Where(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.CurrentCode)
                .Select(x => new { x.CurrentCode, x.CurrentName })
                .ToListAsync(ct);

            var bankAccounts = await _db.ErpBankAccounts
                .Where(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.BankAccCode)
                .Select(x => new { x.BankAccCode, x.BankAccName })
                .ToListAsync(ct);

            return Json(new { glAccounts, currentAccounts, bankAccounts });
        }

        /// <summary>
        /// Tek satır için aktarım batch'i oluşturur (modal üzerinden).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TransferSingle(
            [FromForm] int bankTransactionId,
            [FromForm] string? currentCode,
            [FromForm] string glCode,
            [FromForm] string bankAccountCode,
            CancellationToken ct)
        {
            var companyId = User.GetCompanyId();

            var req = new CreateTransferBatchRequestDto
            {
                CompanyId = companyId,
                UserId = User.GetUserId(),
                BankTransactionIds = new List<int> { bankTransactionId },
                TransferType = "MANUAL",
                CurrentCode = currentCode,
                GlCode = glCode,
                BankAccountCode = bankAccountCode
            };

            try
            {
                var result = await _batchService.CreateBatchAsync(req, ct);
                TempData["Msg"] = $"Aktarım kuyruğa alındı. BatchNo={result.BatchNo}";
            }
            catch (Exception ex)
            {
                TempData["Err"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}