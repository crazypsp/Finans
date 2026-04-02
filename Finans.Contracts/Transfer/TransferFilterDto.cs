using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Contracts.Transfer
{
    /// <summary>
    /// Neden var?
    /// - Aktarım ekranındaki filtreler standart ve tekrar kullanılabilir olsun diye.
    /// - Dapper query bu filtreye göre dinamik çalışır.
    /// </summary>
    public sealed class TransferFilterDto
    {
        public int CompanyId { get; set; }

        public int? BankId { get; set; }
        public string? AccountNumber { get; set; }
        public string? Currency { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        public string? DescriptionContains { get; set; }

        public bool OnlyNotTransferred { get; set; } = true;
        public bool OnlyPendingTransfer { get; set; } = false;
    }
}
