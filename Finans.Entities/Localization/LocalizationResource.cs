using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Localization
{
    public class LocalizationResource : AuditableEntity
    {
        public string ResourceKey { get; set; } = null!; // MENU.TRANSFER.TITLE
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
