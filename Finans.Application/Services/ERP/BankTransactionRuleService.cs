using Finans.Application.Abstractions.ERP;
using Finans.Contracts.ERP;
using Finans.Data.Context;
using Finans.Entities.ERP;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.ERP
{
    public sealed class BankTransactionRuleService : IBankTransactionRuleService
    {
        private readonly FinansDbContext _db;

        public BankTransactionRuleService(FinansDbContext db)
        {
            _db = db;
        }

        public async Task CreateOrUpdateAsync(BankTransactionRuleDto dto, CancellationToken ct = default)
        {
            BankTransactionRule entity;

            if (dto.Id > 0)
            {
                entity = await _db.BankTransactionRules
                    .FirstOrDefaultAsync(x => x.CompanyId == dto.CompanyId && x.Id == dto.Id && !x.IsDeleted, ct)
                    ?? throw new InvalidOperationException("Rule kaydı bulunamadı.");

                entity.BankId = dto.BankId;
                entity.AccountNumber = dto.AccountNumber;
                entity.Currency = dto.Currency;
                entity.DebitCredit = dto.DebitCredit;
                entity.DescriptionContains = dto.DescriptionContains;
                entity.MinAmount = dto.MinAmount;
                entity.MaxAmount = dto.MaxAmount;
                entity.CurrentCode = dto.CurrentCode;
                entity.GlCode = dto.GlCode;
                entity.BankAccountCode = dto.BankAccountCode;
                entity.TransactionTag = dto.TransactionTag;
                entity.DescriptionOverride = dto.DescriptionOverride;
                entity.Priority = dto.Priority;
                entity.IsActive = dto.IsActive;
                entity.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                entity = new BankTransactionRule
                {
                    CompanyId = dto.CompanyId,
                    BankId = dto.BankId,
                    AccountNumber = dto.AccountNumber,
                    Currency = dto.Currency,
                    DebitCredit = dto.DebitCredit,
                    DescriptionContains = dto.DescriptionContains,
                    MinAmount = dto.MinAmount,
                    MaxAmount = dto.MaxAmount,
                    CurrentCode = dto.CurrentCode,
                    GlCode = dto.GlCode,
                    BankAccountCode = dto.BankAccountCode,
                    TransactionTag = dto.TransactionTag,
                    DescriptionOverride = dto.DescriptionOverride,
                    Priority = dto.Priority,
                    IsActive = dto.IsActive,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                };

                _db.BankTransactionRules.Add(entity);
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int companyId, int id, CancellationToken ct = default)
        {
            var entity = await _db.BankTransactionRules
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id && !x.IsDeleted, ct)
                ?? throw new InvalidOperationException("Rule kaydı bulunamadı.");

            entity.IsDeleted = true;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }
    }
}
