namespace Finans.Contracts.Logging
{
    public sealed class SystemNotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Level { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ReadAtUtc { get; set; }
        public string? Source { get; set; }
        public string? ReferenceId { get; set; }
    }
}
