using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Logging
{
    public class SystemLog : AuditableEntity
    {
        public LogLevel Level { get; set; } = LogLevel.Info;

        public string Message { get; set; } = null!;
        public string? Source { get; set; }      // service/module
        public string? ActionName { get; set; }  // controller/job adı

        public string? Exception { get; set; }
        public string? PayloadJson { get; set; }

        public int? UserId { get; set; }
        public string? IpAddress { get; set; }

        public string? CorrelationId { get; set; }
        public DateTime LoggedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
