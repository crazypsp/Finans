using Finans.Application.Abstractions.ERP;
using Finans.Application.Models.ERP;
using Finans.Data.Context;
using Finans.Entities.Banking;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.ERP
{
    public sealed class BankTransactionRuleResolver : IBankTransactionRuleResolver
    {
        private readonly FinansDbContext _db;

        public BankTransactionRuleResolver(FinansDbContext db)
        {
            _db = db;
        }

        public async Task<BankTransactionRuleMatchResult> ResolveAsync(BankTransaction transaction, CancellationToken ct = default)
        {
            var rules = await _db.BankTransactionRules
                .Where(x =>
                    x.CompanyId == transaction.CompanyId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderBy(x => x.Priority)
                .ToListAsync(ct);

            foreach (var rule in rules)
            {
                if (rule.BankId.HasValue && rule.BankId.Value != transaction.BankId)
                    continue;

                if (!string.IsNullOrWhiteSpace(rule.AccountNumber) &&
                    !string.Equals(rule.AccountNumber, transaction.AccountNumber, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(rule.Currency) &&
                    !string.Equals(rule.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(rule.DebitCredit) &&
                    !string.Equals(rule.DebitCredit, transaction.DebitCredit, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (rule.MinAmount.HasValue && transaction.Amount < rule.MinAmount.Value)
                    continue;

                if (rule.MaxAmount.HasValue && transaction.Amount > rule.MaxAmount.Value)
                    continue;

                if (!string.IsNullOrWhiteSpace(rule.DescriptionContains))
                {
                    if (string.IsNullOrWhiteSpace(transaction.Description) ||
                        !transaction.Description.Contains(rule.DescriptionContains, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                return new BankTransactionRuleMatchResult
                {
                    IsMatched = true,
                    CurrentCode = rule.CurrentCode,
                    GlCode = rule.GlCode,
                    BankAccountCode = rule.BankAccountCode,
                    TransactionTag = rule.TransactionTag,
                    DescriptionOverride = rule.DescriptionOverride
                };
            }

            return new BankTransactionRuleMatchResult
            {
                IsMatched = false
            };
        }
    }
}
