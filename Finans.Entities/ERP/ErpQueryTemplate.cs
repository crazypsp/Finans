using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.ERP
{
    public class ErpQueryTemplate : AuditableEntity
    {
        public int ErpSystemId { get; set; }

        // BANK_TX_TO_VOUCHER, GET_GL_ACCOUNTS vb.
        public string QueryKey { get; set; } = null!;
        public string SqlText { get; set; } = null!;
        public string? ParameterSchemaJson { get; set; }

        public int TemplateVersion { get; set; } = 1;
        public bool IsDefault { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}
