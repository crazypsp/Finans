using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Banking
{
    public class BankApiPayload : AuditableEntity
    {
        public int? CompanyId { get; set; }
        public int BankId { get; set; }

        public int? UserId { get; set; }
        public int? RoleId { get; set; }

        public string Operation { get; set; } = null!;  // GetStatement, Login...
        public string? RequestText { get; set; }        // JSON/XML/FORM
        public string? ResponseText { get; set; }       // JSON/XML/HTML

        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public string? CorrelationId { get; set; }
        public string? Hash { get; set; }               // duplicate engelleme
        public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
