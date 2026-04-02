using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Identity
{
    public class UserRole : AuditableEntity
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }

        // Firma bazlı rol: null => global rol
        public int? ScopeCompanyId { get; set; }

        public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
        public int? AssignedByUserId { get; set; }
    }
}
