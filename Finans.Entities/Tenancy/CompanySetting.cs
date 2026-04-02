using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Tenancy
{
    public class CompanySetting : AuditableEntity
    {
        public int CompanyId { get; set; }

        public string? DefaultCurrencyCode { get; set; } // TRY, EUR
        public string? DateFormat { get; set; }          // dd.MM.yyyy
        public string? NumberFormat { get; set; }        // tr-TR vb.
        public string? UiTheme { get; set; }

        // Feature flags / parametrik ayarlar
        public string? SettingsJson { get; set; }
    }
}
