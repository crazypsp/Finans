using Finans.Entities.Common;

namespace Finans.Entities.Logging
{
    public class ErpTransferLog : AuditableEntity
    {
        // CompanyId, CreatedAtUtc, IsDeleted AuditableEntity'den gelir

        public int UserId { get; set; }

        public int? ErpSystemId { get; set; }

        public int? ErpTransferBatchId { get; set; }
        public int? ErpTransferItemId { get; set; }
        public int? BankTransactionId { get; set; }

        public string Operation { get; set; } = null!;
        public TransferStatus Status { get; set; }

        public string? RequestDataJson { get; set; }
        public string? ResponseDataJson { get; set; }
        public string? ErrorMessage { get; set; }

        public string? VoucherNo { get; set; }
        public int? DurationMs { get; set; }

        public string? CorrelationId { get; set; }
    }
}
