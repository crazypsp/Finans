using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Banking
{
    public class Bank : AuditableEntity
    {
        // Dış servis bankId vs.
        public int ExternalBankId { get; set; }

        public string BankName { get; set; } = null!;

        // Provider seçiminde kullanılacak anahtar: "AKB", "ISB"...
        public string ProviderCode { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        // UI/Provider ihtiyaç alanları (BankProviders uyumluluğu)
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool RequiresLink { get; set; }
        public bool RequiresTLink { get; set; }
        public bool RequiresAccountNumber { get; set; }
        public string? DefaultLink { get; set; }
        public string? DefaultTLink { get; set; }

        public DateTime? ModifiedAtUtc { get; set; }
    }
}
