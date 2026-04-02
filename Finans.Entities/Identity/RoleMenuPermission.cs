using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Identity
{
    public class RoleMenuPermission : AuditableEntity
    {
        public int RoleId { get; set; }
        public int MenuItemId { get; set; }

        public bool CanView { get; set; } = true;
        public bool CanCreate { get; set; } = false;
        public bool CanUpdate { get; set; } = false;
        public bool CanDelete { get; set; } = false;

        public bool CanApprove { get; set; } = false;
        public bool CanExport { get; set; } = false;
    }
}
