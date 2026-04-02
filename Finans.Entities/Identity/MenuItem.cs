using Finans.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Identity
{
    public class MenuItem : AuditableEntity
    {
        public string Title { get; set; } = null!;
        public string Code { get; set; } = null!;   // DASHBOARD, TRANSFER, REPORTS...
        public string? Route { get; set; }          // /transfer
        public string? Icon { get; set; }

        public int? ParentMenuItemId { get; set; }
        public int SortOrder { get; set; } = 0;

        public bool IsVisible { get; set; } = true;
    }
}
