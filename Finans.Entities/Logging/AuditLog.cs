using Finans.Entities.Common;

namespace Finans.Entities.Logging
{
    public sealed class AuditLog : AuditableEntity
    {
        public int CompanyId { get; set; }
        public int? UserId { get; set; }

        public string EntityName { get; set; } = null!;
        public string ActionType { get; set; } = null!;   // Create, Update, Delete, Retry, Login, etc.
        public string? RecordId { get; set; }

        public string? Description { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public string? IpAddress { get; set; }
        public string? MachineName { get; set; }

        public DateTime OccurredAtUtc { get; set; }
    }
}
