using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Logging
{
    public class MatchingLog : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        public int BankId { get; set; }

        public string? AccountNumber { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int TotalTransactions { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }

        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }

        public string? MatchingCriteriaJson { get; set; }
    }
}
