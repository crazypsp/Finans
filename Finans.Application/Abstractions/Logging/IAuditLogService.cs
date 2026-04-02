namespace Finans.Application.Abstractions.Logging
{
    public interface IAuditLogService
    {
        Task WriteAsync(
            int companyId,
            int? userId,
            string entityName,
            string actionType,
            string? recordId,
            string? description,
            string? oldValues,
            string? newValues,
            string? ipAddress,
            string? machineName,
            CancellationToken ct = default);
    }
}
