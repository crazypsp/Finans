namespace Finans.Contracts.Logging
{
    public sealed class AuditLogDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int? UserId { get; set; }
        public string EntityName { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string? RecordId { get; set; }
        public string? Description { get; set; }
        public string? IpAddress { get; set; }
        public string? MachineName { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }
}
