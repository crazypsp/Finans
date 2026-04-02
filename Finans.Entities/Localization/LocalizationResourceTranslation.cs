using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Localization
{
    public class LocalizationResourceTranslation : AuditableEntity
    {
        public int LocalizationResourceId { get; set; }
        public string CultureName { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
