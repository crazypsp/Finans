using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Logging
{
    public class ExportLog : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int UserId { get; set; }

        public string ExportType { get; set; } = null!; // EXCEL, PDF
        public string? FileName { get; set; }

        public string? FilterCriteriaJson { get; set; }
        public int RecordCount { get; set; }

        public string? FilePath { get; set; }
        public long? FileSize { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
