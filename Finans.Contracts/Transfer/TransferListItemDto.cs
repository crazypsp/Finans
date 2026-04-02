using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Contracts.Transfer
{
    /// <summary>
    /// Neden var?
    /// - Grid listesi için minimal satır modeli.
    /// - Entity'deki onlarca alanı UI'ya taşımayız.
    /// </summary>
    public sealed class TransferListItemDto
    {
        public int BankTransactionId { get; set; }
        public int BankId { get; set; }

        public string BankName { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string? Iban { get; set; }
        public string? BranchNo { get; set; }
        public string? CustomerNo { get; set; }

        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";
        public string DebitCredit { get; set; } = "C";
        public string? ReferenceNumber { get; set; }

        public bool IsTransferred { get; set; }
        public string? TransferBatchNo { get; set; }
        public string TransferStatus { get; set; } = "New";

        // Eslestirme bilgisi
        public bool IsMatched { get; set; }
        public string? MatchedCurrentCode { get; set; }
        public string? MatchedCurrentName { get; set; }
    }
}
