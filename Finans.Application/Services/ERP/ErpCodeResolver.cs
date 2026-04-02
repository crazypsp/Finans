using Finans.Application.Abstractions.ERP;
using Finans.Data.Context;
using Finans.Entities.Banking;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.ERP
{
    public sealed class ErpCodeResolver : IErpCodeResolver
    {
        private readonly FinansDbContext _db;

        public ErpCodeResolver(FinansDbContext db)
        {
            _db = db;
        }

        public async Task<(string? CurrentCode, string? GlCode, string? BankAccountCode)> ResolveAsync(
            BankTransaction transaction,
            CancellationToken ct = default)
        {
            var mappings = await _db.ErpCodeMappings
                .Where(x =>
                    x.CompanyId == transaction.CompanyId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderBy(x => x.Priority)
                .ToListAsync(ct);

            foreach (var map in mappings)
            {
                if (map.BankId.HasValue && map.BankId.Value != transaction.BankId)
                    continue;

                if (!string.IsNullOrWhiteSpace(map.Currency) &&
                    !string.Equals(map.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(map.DebitCredit) &&
                    !string.Equals(map.DebitCredit, transaction.DebitCredit, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(map.DescriptionKeyword))
                {
                    if (string.IsNullOrWhiteSpace(transaction.Description) ||
                        !transaction.Description.Contains(map.DescriptionKeyword, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                return (map.CurrentCode, map.GlCode, map.BankAccountCode);
            }

            return (null, null, null);
        }
    }
}
