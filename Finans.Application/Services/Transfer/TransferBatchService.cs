using Finans.Application.Abstractions.Transfer;
using Finans.Contracts.Transfer;
using Finans.Data.Context;
using Finans.Entities.ERP;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.Transfer
{
    public sealed class TransferBatchService : ITransferBatchService
    {
        private readonly FinansDbContext _db;

        public TransferBatchService(FinansDbContext db)
        {
            _db = db;
        }

        public async Task<CreateTransferBatchResultDto> CreateBatchAsync(CreateTransferBatchRequestDto request, CancellationToken ct = default)
        {
            if (request.BankTransactionIds == null || request.BankTransactionIds.Count == 0)
                throw new InvalidOperationException("Aktarım için en az bir kayıt seçilmelidir.");

            var transactions = await _db.BankTransactions
                .Where(x =>
                    x.CompanyId == request.CompanyId &&
                    request.BankTransactionIds.Contains(x.Id) &&
                    !x.IsDeleted)
                .ToListAsync(ct);

            if (transactions.Count == 0)
                throw new InvalidOperationException("Seçilen kayıtlar bulunamadı.");

            var alreadyQueued = transactions
                .Where(x => !x.IsTransferred && !string.IsNullOrWhiteSpace(x.TransferBatchNo))
                .Select(x => x.Id)
                .ToList();

            if (alreadyQueued.Count > 0)
                throw new InvalidOperationException($"Bazı kayıtlar zaten aktarım kuyruğunda: {string.Join(",", alreadyQueued)}");

            var alreadyTransferred = transactions
                .Where(x => x.IsTransferred)
                .Select(x => x.Id)
                .ToList();

            if (alreadyTransferred.Count > 0)
                throw new InvalidOperationException($"Bazı kayıtlar zaten aktarılmış: {string.Join(",", alreadyTransferred)}");

            var batchNo = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.UserId}";

            var batch = new ErpTransferBatch
            {
                CompanyId = request.CompanyId,
                UserId = request.UserId,
                ErpSystemId = 1, // Logo Tiger varsayılan
                BatchNo = batchNo,
                TransferType = request.TransferType,
                TotalCount = transactions.Count,
                SuccessCount = 0,
                FailedCount = 0,
                StartedAtUtc = DateTime.UtcNow,
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.ErpTransferBatches.Add(batch);
            await _db.SaveChangesAsync(ct);

            foreach (var tx in transactions)
            {
                var item = new ErpTransferItem
                {
                    CompanyId = request.CompanyId,
                    ErpTransferBatchId = batch.Id,
                    BankTransactionId = tx.Id,

                    CurrentCode = request.CurrentCode,
                    GlCode = request.GlCode,
                    BankAccCode = request.BankAccountCode,

                    Status = "Pending",
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                };

                _db.ErpTransferItems.Add(item);

                // Kuyruk izi
                tx.TransferBatchNo = batchNo;
                tx.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);

            return new CreateTransferBatchResultDto
            {
                BatchId = batch.Id,
                BatchNo = batch.BatchNo,
                ItemCount = transactions.Count
            };
        }
    }
}