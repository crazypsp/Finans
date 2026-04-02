using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Localization
{
    public class Culture : AuditableEntity
    {
        public string CultureName { get; set; } = null!; // tr-TR
        public string DisplayName { get; set; } = null!; // Türkçe
        public bool IsActive { get; set; } = true;
    }
}
