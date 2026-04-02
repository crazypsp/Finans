using Finans.Entities.Common;

namespace Finans.Entities.Logging
{
    public sealed class SystemNotification : AuditableEntity
    {
        public int CompanyId { get; set; }

        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Level { get; set; } = "Info"; // Info, Warning, Error

        public bool IsRead { get; set; }
        public DateTime? ReadAtUtc { get; set; }

        public string? Source { get; set; } // Connector, BankImport, ERP, Auth, etc.
        public string? ReferenceId { get; set; }
    }
}
