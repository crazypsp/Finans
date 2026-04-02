using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Common
{
    public abstract class AuditableEntity : BaseEntity
    {
        // Multi-tenant bağlamı (bazı tablolar sistem seviyesinde olabilir)
        public int CompanyId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }

        public bool IsDeleted { get; set; } = false;

        // EF Core Fluent API ile IsRowVersion() yapılacak.
        public byte[]? RowVersion { get; set; }
    }
}
