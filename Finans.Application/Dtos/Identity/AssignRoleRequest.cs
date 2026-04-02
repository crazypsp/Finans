using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Application.Dtos.Identity
{
    public sealed class AssignRoleRequest
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public int? ScopeCompanyId { get; set; } // şimdilik null
    }
}
