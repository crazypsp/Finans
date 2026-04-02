using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Integrations
{
    public class IntegrationCredential : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int IntegrationEndpointId { get; set; }

        public AuthMethod AuthMethod { get; set; }
        public string? Username { get; set; }
        public string? SecretEncrypted { get; set; }

        public string? ExtrasJson { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
