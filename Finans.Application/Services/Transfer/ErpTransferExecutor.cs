using Finans.Application.Abstractions.ERP;
using Finans.Application.Abstractions.Integration;
using Finans.Application.Abstractions.Logging;
using Finans.Application.Abstractions.Transfer;
using Finans.Data.Context;
using Finans.Entities.Common;
using Finans.Entities.Logging;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.Transfer
{
    // DÜZELTME: Artık Abstractions.Transfer.IErpTransferExecutor implement ediliyor.
    // Önceki Abstractions.ERP.IErpTransferExecutor stub'ı silindi (bkz. README).
    public sealed class ErpTransferExecutor : Abstractions.Transfer.IErpTransferExecutor
    {
        private readonly FinansDbContext _db;
        private readonly IErpTransferClient _erpClient;
        private readonly IConnectorHeartbeatService _connectorHeartbeatService;
        private readonly IConnectorPolicyService _connectorPolicyService;
        private readonly IErpCodeResolver _erpCodeResolver;
        private readonly IBankTransactionRuleResolver _ruleResolver;
        private readonly INotificationService _notificationService;

        public ErpTransferExecutor(FinansDbContext db, IErpTransferClient erpClient, IConnectorHeartbeatService connectorHeartbeatService, IConnectorPolicyService connectorPolicyService, IErpCodeResolver erpCodeResolver, IBankTransactionRuleResolver ruleResolver, INotificationService notificationService)
        {
            _db = db;
            _erpClient = erpClient;
            _connectorHeartbeatService = connectorHeartbeatService;
            _connectorPolicyService = connectorPolicyService;
            _erpCodeResolver = erpCodeResolver;
            _ruleResolver = ruleResolver;
            _notificationService = notificationService;
        }

        public async Task ExecutePendingAsync(CancellationToken ct = default)
        {
            var pendingItems = await _db.ErpTransferItems
                .Where(x => !x.IsDeleted && x.Status == "Pending")
                .OrderBy(x => x.Id)
                .ToListAsync(ct);

            foreach (var item in pendingItems)
            {
                var connectorActive = await _connectorHeartbeatService.IsConnectorActiveAsync(
                    item.CompanyId,
                    TimeSpan.FromMinutes(5),
                    ct);

                if (!connectorActive)
                {
                    item.Status = "Failed";
                    item.ResultMessage = "Aktif Desktop Connector bulunamadı.";
                    item.UpdatedAtUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    continue;
                }

                var machineName = Environment.MachineName;
                var version = "1.0.0";

                var policy = await _connectorPolicyService.CanTransferAsync(
                    item.CompanyId,
                    machineName,
                    version,
                    ct);

                if (!policy.IsAllowed)
                {
                    item.Status = "Failed";
                    item.ResultMessage = policy.Message;
                    item.UpdatedAtUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    continue;
                }

                ct.ThrowIfCancellationRequested();

                var batch = await _db.ErpTransferBatches
                    .FirstOrDefaultAsync(x => x.Id == item.ErpTransferBatchId && !x.IsDeleted, ct);

                if (batch == null)
                    continue;

                var transaction = await _db.BankTransactions
                    .FirstOrDefaultAsync(x => x.Id == item.BankTransactionId && !x.IsDeleted, ct);

                if (transaction == null)
                {
                    item.Status = "Failed";
                    item.ResultMessage = "BankTransaction bulunamadı.";
                    item.UpdatedAtUtc = DateTime.UtcNow;

                    batch.FailedCount += 1;
                    batch.Status = CalculateBatchStatus(batch.TotalCount, batch.SuccessCount, batch.FailedCount);
                    batch.UpdatedAtUtc = DateTime.UtcNow;

                    await WriteErpTransferLogAsync(
                        companyId: item.CompanyId,
                        bankTransactionId: item.BankTransactionId,
                        batchId: batch.Id,
                        status: TransferStatus.Failed,
                        message: "BankTransaction bulunamadı.",
                        ct: ct);

                    await _db.SaveChangesAsync(ct);
                    continue;
                }

                try
                {
                    var resolvedCodes = await _erpCodeResolver.ResolveAsync(transaction, ct);

                    var ruleMatch = await _ruleResolver.ResolveAsync(transaction, ct);
                    var mappingMatch = await _erpCodeResolver.ResolveAsync(transaction, ct);

                    var currentCode =
                        !string.IsNullOrWhiteSpace(item.CurrentCode) ? item.CurrentCode :
                        !string.IsNullOrWhiteSpace(ruleMatch.CurrentCode) ? ruleMatch.CurrentCode :
                        mappingMatch.CurrentCode;

                    var glCode =
                        !string.IsNullOrWhiteSpace(item.GlCode) ? item.GlCode :
                        !string.IsNullOrWhiteSpace(ruleMatch.GlCode) ? ruleMatch.GlCode :
                        mappingMatch.GlCode;

                    var bankAccountCode =
                        !string.IsNullOrWhiteSpace(item.BankAccCode) ? item.BankAccCode :
                        !string.IsNullOrWhiteSpace(ruleMatch.BankAccountCode) ? ruleMatch.BankAccountCode :
                        mappingMatch.BankAccountCode;

                    if (ruleMatch.IsMatched && !string.IsNullOrWhiteSpace(ruleMatch.DescriptionOverride))
                    {
                        transaction.Description = ruleMatch.DescriptionOverride;
                    }

                    var result = await _erpClient.TransferAsync(
                        companyId: item.CompanyId,
                        bankTransactionId: item.BankTransactionId,
                        currentCode: currentCode,
                        glCode: glCode,
                        bankAccountCode: bankAccountCode,
                        ct: ct);

                    if (result.IsSuccess)
                    {
                        item.Status = "Success";
                        item.ResultMessage = result.Message;
                        item.TransferredAtUtc = DateTime.UtcNow;
                        item.UpdatedAtUtc = DateTime.UtcNow;

                        transaction.IsTransferred = true;
                        transaction.ErpVoucherNo = result.VoucherNo;
                        transaction.ErpResultMessage = result.Message;
                        transaction.UpdatedAtUtc = DateTime.UtcNow;

                        batch.SuccessCount += 1;
                        batch.Status = CalculateBatchStatus(batch.TotalCount, batch.SuccessCount, batch.FailedCount);
                        batch.UpdatedAtUtc = DateTime.UtcNow;

                        await WriteErpTransferLogAsync(
                            companyId: item.CompanyId,
                            bankTransactionId: item.BankTransactionId,
                            batchId: batch.Id,
                            status: TransferStatus.Success,
                            message: result.Message ?? "ERP aktarımı başarılı.",
                            ct: ct);
                    }
                    else
                    {
                        item.Status = "Failed";
                        item.ResultMessage = result.Message;
                        item.UpdatedAtUtc = DateTime.UtcNow;

                        batch.FailedCount += 1;
                        batch.Status = CalculateBatchStatus(batch.TotalCount, batch.SuccessCount, batch.FailedCount);
                        batch.UpdatedAtUtc = DateTime.UtcNow;

                        await WriteErpTransferLogAsync(
                            companyId: item.CompanyId,
                            bankTransactionId: item.BankTransactionId,
                            batchId: batch.Id,
                            status: TransferStatus.Failed,
                            message: result.Message ?? "ERP aktarımı başarısız.",
                            ct: ct);
                    }
                }
                catch (Exception ex)
                {
                    item.Status = "Failed";
                    item.ResultMessage = ex.Message;
                    item.UpdatedAtUtc = DateTime.UtcNow;

                    batch.FailedCount += 1;
                    batch.Status = CalculateBatchStatus(batch.TotalCount, batch.SuccessCount, batch.FailedCount);
                    batch.UpdatedAtUtc = DateTime.UtcNow;

                    await WriteErpTransferLogAsync(
                        companyId: item.CompanyId,
                        bankTransactionId: item.BankTransactionId,
                        batchId: batch.Id,
                        status: TransferStatus.Failed,
                        message: ex.Message,
                        ct: ct);

                    await _notificationService.CreateAsync(
                        companyId: item.CompanyId,
                        title: "ERP aktarım hatası",
                        message: ex.Message,
                        level: "Error",
                        source: "ERP",
                        referenceId: item.Id.ToString(),
                        ct: ct);
                }

                await _db.SaveChangesAsync(ct);
            }
        }

        private static string CalculateBatchStatus(int totalCount, int successCount, int failedCount)
        {
            var processedCount = successCount + failedCount;

            if (processedCount <= 0)
                return "Pending";

            if (processedCount < totalCount)
                return "Pending";

            if (failedCount == 0)
                return "Success";

            return "Failed";
        }

        private async Task WriteErpTransferLogAsync(
            int companyId,
            int bankTransactionId,
            int batchId,
            TransferStatus status,
            string message,
            CancellationToken ct)
        {
            var entity = new ErpTransferLog
            {
                CompanyId = companyId,
                UserId = 0,
                ErpTransferBatchId = batchId,
                BankTransactionId = bankTransactionId,
                Operation = "ErpTransfer",
                Status = status,
                ErrorMessage = message,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.ErpTransferLogs.Add(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}