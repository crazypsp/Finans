using Finans.Data.Context;
using Finans.Entities.Banking;
using Finans.WebMvc.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finans.WebMvc.Controllers
{
    [Authorize]
    public class BankManagementController : Controller
    {
        private readonly FinansDbContext _db;

        public BankManagementController(FinansDbContext db) => _db = db;

        // ----- BANK -----
        public async Task<IActionResult> Banks(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();
            var list = await _db.Banks
                .Where(x => x.CompanyId == companyId && !x.IsDeleted)
                .ToListAsync(ct);
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBank(Bank model, CancellationToken ct)
        {
            model.CompanyId = User.GetCompanyId();
            model.CreatedAtUtc = DateTime.UtcNow;
            model.IsDeleted = false;
            _db.Banks.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Msg"] = "Banka eklendi.";
            return RedirectToAction(nameof(Banks));
        }

        // ----- BANK ACCOUNT -----
        public async Task<IActionResult> Accounts(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();
            var list = await _db.BankAccounts
                .Where(x => x.CompanyId == companyId && !x.IsDeleted)
                .ToListAsync(ct);
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(BankAccount model, CancellationToken ct)
        {
            model.CompanyId = User.GetCompanyId();
            model.CreatedAtUtc = DateTime.UtcNow;
            model.IsDeleted = false;
            _db.BankAccounts.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Msg"] = "Hesap eklendi.";
            return RedirectToAction(nameof(Accounts));
        }

        // ----- CREDENTIAL -----
        public async Task<IActionResult> Credentials(CancellationToken ct)
        {
            var companyId = User.GetCompanyId();
            var list = await _db.BankCredentials
                .Where(x => x.CompanyId == companyId && !x.IsDeleted)
                .ToListAsync(ct);
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCredential(BankCredential model, CancellationToken ct)
        {
            model.CompanyId = User.GetCompanyId();
            model.CreatedAtUtc = DateTime.UtcNow;
            model.IsDeleted = false;
            // Gerçek uygulamada password şifrelenmeli
            model.SecretEncrypted = model.Password ?? "";
            _db.BankCredentials.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["Msg"] = "Credential eklendi.";
            return RedirectToAction(nameof(Credentials));
        }
    }
}
