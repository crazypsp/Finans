using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Application.Models.Banking
{
    /// <summary>
    /// Neden var?
    /// - Provider'dan dönen sonucu tek tipe indirger (success/error + rows).
    /// - Ham response'u loglamak için taşır (denetim/audit).
    /// </summary>
    public sealed class BankStatementResult
    {
        public bool IsSuccess { get; set; }
        public string? Error { get; set; }
        public string? RawResponse { get; set; }

        public List<BankStatementRow> Rows { get; set; } = new();
    }

    public sealed class BankStatementRow
    {
        public string ExternalUniqueKey { get; set; } = null!;
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string DebitCredit { get; set; } = null!; // D/C
        public string? Currency { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal? BalanceAfter { get; set; }
        public string? AccountNumber { get; set; }
        public string? BranchNo { get; set; }
        public string? Iban { get; set; }
        public string? CustomerNo { get; set; }
    }
}
