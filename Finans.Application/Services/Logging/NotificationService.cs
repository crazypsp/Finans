using Finans.Application.Abstractions.Logging;
using Finans.Data.Context;
using Finans.Entities.Logging;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.Logging
{
    public sealed class NotificationService : INotificationService
    {
        private readonly FinansDbContext _db;

        public NotificationService(FinansDbContext db)
        {
            _db = db;
        }

        public async Task CreateAsync(
            int companyId,
            string title,
            string message,
            string level,
            string? source,
            string? referenceId,
            CancellationToken ct = default)
        {
            var entity = new SystemNotification
            {
                CompanyId = companyId,
                Title = title,
                Message = message,
                Level = level,
                IsRead = false,
                Source = source,
                ReferenceId = referenceId,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.SystemNotifications.Add(entity);
            await _db.SaveChangesAsync(ct);
        }

        public async Task MarkAsReadAsync(int companyId, int id, CancellationToken ct = default)
        {
            var entity = await _db.SystemNotifications
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id && !x.IsDeleted, ct)
                ?? throw new InvalidOperationException("Bildirim bulunamadı.");

            entity.IsRead = true;
            entity.ReadAtUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }
    }
}
