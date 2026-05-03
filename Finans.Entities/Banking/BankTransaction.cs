using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Banking
{
    public class BankTransaction : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int BankId { get; set; }

        public string AccountNumber { get; set; } = null!;
        public string? Iban { get; set; }
        public string? BranchNo { get; set; }
        public string? CustomerNo { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime? ValueDate { get; set; }

        public string? Description { get; set; }

        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string DebitCredit { get; set; } = null!; // D/C

        public decimal? BalanceAfterTransaction { get; set; }
        public string? ReferenceNumber { get; set; }

        // Bankanın döndürdüğü hareket ID'si. Tekrarlı import engeli bu ID ile garanti edilir.
        public string? ExternalTransactionId { get; set; }

        // Normalize edilmiş kaynak anahtar. Company/Bank/Account/ID bilgisini içerir.
        public string ExternalUniqueKey { get; set; } = null!;

        // Eşleştirme (Logo cari)
        public bool IsMatched { get; set; } = false;
        public DateTime? MatchedAtUtc { get; set; }
        public string? MatchedCurrentCode { get; set; }
        public string? MatchedCurrentName { get; set; }

        // Aktarım durumu
        public bool IsTransferred { get; set; } = false;
        public DateTime? TransferredAtUtc { get; set; }
        public string? TransferBatchNo { get; set; }
        public string? ErpVoucherNo { get; set; }
        public string? ErpResultMessage { get; set; }
        public int? LastTransferLogId { get; set; }

        // Kaynağı takip
        public int? BankApiPayloadId { get; set; }
    }
}
