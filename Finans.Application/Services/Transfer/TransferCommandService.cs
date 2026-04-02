using Finans.Application.Abstractions.Transfer;
using Finans.Contracts.Transfer;
using Finans.Data.Context;
using Finans.Entities.ERP;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.Transfer
{
    public sealed class TransferCommandService : ITransferCommandService
    {
        private readonly FinansDbContext _db;

        public TransferCommandService(FinansDbContext db)
        {
            _db = db;
        }

        public async Task<int> CreateTransferRequestAsync(CreateTransferRequestDto request, CancellationToken ct)
        {
            var alreadyExists = await _db.ErpTransferItems
    .AnyAsync(x => x.BankTransactionId == request.BankTransactionId && x.CompanyId == request.CompanyId, ct);

            if (alreadyExists)
                throw new InvalidOperationException("Bu işlem için zaten bir aktarım isteği mevcut.");

            // Neden kontrol?
            // - Aktarılmış bir transaction için tekrar transfer emri oluşturmayı engelleriz.
            var tx = await _db.BankTransactions
                .FirstOrDefaultAsync(x => x.Id == request.BankTransactionId && x.CompanyId == request.CompanyId, ct);

            if (tx == null)
                throw new InvalidOperationException("BankTransaction bulunamadı.");

            if (tx.IsTransferred)
                throw new InvalidOperationException("Bu kayıt zaten aktarılmış.");

            // Neden batch?
            // - Tek satır aktarım bile batch altında tutulursa audit ve raporlama kolaylaşır.
            var batchNo = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.UserId}";

            var batch = new ErpTransferBatch
            {
                CompanyId = request.CompanyId,
                UserId = request.UserId,
                ErpSystemId = 1, // Logo Tiger seed'i Gün 3'te netleşecek. Şimdilik 1.
                BatchNo = batchNo,
                TransferType = "SINGLE",
                FilterJson = null,
                TotalCount = 1,
                SuccessCount = 0,
                FailedCount = 0,
                StartedAtUtc = DateTime.UtcNow,
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.ErpTransferBatches.Add(batch);
            await _db.SaveChangesAsync(ct); // batch Id almak için

            var item = new ErpTransferItem
            {
                CompanyId = request.CompanyId,
                ErpTransferBatchId = batch.Id,
                BankTransactionId = request.BankTransactionId,

                CurrentCode = request.CurrentCode,
                GlCode = request.GlCode,
                BankAccCode = request.BankAccCode,

                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.ErpTransferItems.Add(item);
            await _db.SaveChangesAsync(ct);

            // BankTransaction üzerinde iz bırakmak istersek:
            tx.TransferBatchNo = batchNo;
            tx.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return batch.Id;
        }
    }
}