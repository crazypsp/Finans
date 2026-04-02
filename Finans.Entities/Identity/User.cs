using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Identity
{
    public class User : AuditableEntity
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string PasswordHash { get; set; } = null!;
        public string? PasswordSalt { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsEmailVerified { get; set; } = false;

        public DateTime? LastLoginAtUtc { get; set; }

        // Sistem admin (tenant dışı işler)
        public bool IsSystemAdmin { get; set; } = false;
    }
}
