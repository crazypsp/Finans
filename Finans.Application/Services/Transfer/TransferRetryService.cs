using Finans.Application.Abstractions.Logging;
using Finans.Application.Abstractions.Transfer;
using Finans.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.Transfer
{
    public sealed class TransferRetryService : ITransferRetryService
    {
        private readonly FinansDbContext _db;
        private readonly IAuditLogService _auditLogService;

        public TransferRetryService(FinansDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        public async Task RetryItemAsync(int companyId, int itemId, CancellationToken ct = default)
        {
            var item = await _db.ErpTransferItems
                .FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.Id == itemId &&
                    !x.IsDeleted,
                    ct);

            if (item == null)
                throw new InvalidOperationException("Transfer item bulunamadi.");

            // Audit log - null check SONRASI
            await _auditLogService.WriteAsync(
                companyId: companyId,
                userId: null,
                entityName: "ErpTransferItem",
                actionType: "Retry",
                recordId: item.Id.ToString(),
                description: "Basarisiz transfer tekrar kuyruga alindi.",
                oldValues: null,
                newValues: null,
                ipAddress: null,
                machineName: Environment.MachineName,
                ct: ct);

            item.Status = "Pending";
            item.ResultMessage = null;
            item.TransferredAtUtc = null;
            item.UpdatedAtUtc = DateTime.UtcNow;

            var tx = await _db.BankTransactions
                .FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.Id == item.BankTransactionId &&
                    !x.IsDeleted,
                    ct);

            if (tx != null)
            {
                tx.IsTransferred = false;
                tx.ErpVoucherNo = null;
                tx.ErpResultMessage = null;
                tx.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task RetryItemsAsync(int companyId, IReadOnlyList<int> itemIds, CancellationToken ct = default)
        {
            if (itemIds == null || itemIds.Count == 0)
                throw new InvalidOperationException("Retry icin en az bir kayit secilmelidir.");

            var items = await _db.ErpTransferItems
                .Where(x =>
                    x.CompanyId == companyId &&
                    itemIds.Contains(x.Id) &&
                    !x.IsDeleted)
                .ToListAsync(ct);

            if (items.Count == 0)
                throw new InvalidOperationException("Retry edilecek kayit bulunamadi.");

            var bankTransactionIds = items.Select(x => x.BankTransactionId).Distinct().ToList();

            var transactions = await _db.BankTransactions
                .Where(x =>
                    x.CompanyId == companyId &&
                    bankTransactionIds.Contains(x.Id) &&
                    !x.IsDeleted)
                .ToListAsync(ct);

            foreach (var item in items)
            {
                item.Status = "Pending";
                item.ResultMessage = null;
                item.TransferredAtUtc = null;
                item.UpdatedAtUtc = DateTime.UtcNow;
            }

            foreach (var tx in transactions)
            {
                tx.IsTransferred = false;
                tx.ErpVoucherNo = null;
                tx.ErpResultMessage = null;
                tx.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
