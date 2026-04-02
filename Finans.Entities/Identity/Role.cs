using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Identity
{
    public class Role : AuditableEntity
    {
        public string Name { get; set; } = null!;

        // ADMIN, DEALER, ...
        public string Code { get; set; } = null!;

        public string? Description { get; set; }
        public bool IsSystemRole { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
