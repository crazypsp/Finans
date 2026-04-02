using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Banking
{
    public class BankAccount : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int BankId { get; set; }

        public string AccountNumber { get; set; } = null!;
        public string? Iban { get; set; }
        public string? BranchNo { get; set; }
        public string? CustomerNo { get; set; }

        public string? Currency { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
