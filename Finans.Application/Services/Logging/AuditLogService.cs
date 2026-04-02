using Finans.Application.Abstractions.Logging;
using Finans.Data.Context;
using Finans.Entities.Logging;

namespace Finans.Application.Services.Logging
{
    public sealed class AuditLogService : IAuditLogService
    {
        private readonly FinansDbContext _db;

        public AuditLogService(FinansDbContext db)
        {
            _db = db;
        }

        public async Task WriteAsync(
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
            CancellationToken ct = default)
        {
            var entity = new AuditLog
            {
                CompanyId = companyId,
                UserId = userId,
                EntityName = entityName,
                ActionType = actionType,
                RecordId = recordId,
                Description = description,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress,
                MachineName = machineName,
                OccurredAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.AuditLogs.Add(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
