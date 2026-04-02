using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Logging
{
    public class BankIntegrationLog : AuditableEntity
    {
        public int? CompanyId { get; set; }
        public int BankId { get; set; }
        public int? BankCredentialId { get; set; }

        public LogLevel Level { get; set; } = LogLevel.Info;

        public string Operation { get; set; } = null!; // GetStatement
        public string Status { get; set; } = null!;    // Success/Failed

        public string? RequestText { get; set; }
        public string? ResponseText { get; set; }
        public string? ErrorMessage { get; set; }

        public string? CorrelationId { get; set; }
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }
}
