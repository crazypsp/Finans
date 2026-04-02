using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Integrations
{
    public class IntegrationEndpoint : AuditableEntity
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;  // LOGO_UNITY, ERP_API vb.

        public IntegrationTechnology Technology { get; set; }

        public string? BaseUrl { get; set; }
        public string? HealthCheckUrl { get; set; }

        public string? DllName { get; set; }
        public string? TypeName { get; set; }
        public string? Version { get; set; }

        public bool IsActive { get; set; } = true;
        public string? SettingsJson { get; set; }
    }
}
