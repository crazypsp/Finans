using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Logging
{
    public class UserActivityLog : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int UserId { get; set; }

        public string Activity { get; set; } = null!; // LOGIN, FILTER, EXPORT, TRANSFER_CREATE
        public string? Target { get; set; }           // entity/id
        public string? DetailsJson { get; set; }

        public string? IpAddress { get; set; }
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }
}
