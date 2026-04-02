using Finans.Application.Abstractions.Integration;
using Finans.Data.Context;
using Finans.Entities.Integration;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.Integration
{
    public sealed class ConnectorHeartbeatService : IConnectorHeartbeatService
    {
        private readonly FinansDbContext _db;

        public ConnectorHeartbeatService(FinansDbContext db)
        {
            _db = db;
        }

        public async Task RegisterOrUpdateAsync(
            int companyId,
            string machineName,
            string connectorKey,
            string version,
            CancellationToken ct = default)
        {
            var entity = await _db.DesktopConnectorClients
                .FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.MachineName == machineName &&
                    !x.IsDeleted,
                    ct);

            if (entity == null)
            {
                entity = new DesktopConnectorClient
                {
                    CompanyId = companyId,
                    MachineName = machineName,
                    ConnectorKey = connectorKey,
                    Version = version,
                    IsActive = true,
                    IsLicensed = true,
                    LastHeartbeatAtUtc = DateTime.UtcNow,
                    LastStatus = "Registered",
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                };

                _db.DesktopConnectorClients.Add(entity);
            }
            else
            {
                entity.ConnectorKey = connectorKey;
                entity.Version = version;
                entity.IsActive = true;
                entity.LastHeartbeatAtUtc = DateTime.UtcNow;
                entity.LastStatus = "Updated";
                entity.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task WriteHeartbeatAsync(
            int companyId,
            string machineName,
            string version,
            string status,
            string? message,
            CancellationToken ct = default)
        {
            var client = await _db.DesktopConnectorClients
                .FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.MachineName == machineName &&
                    !x.IsDeleted,
                    ct);

            if (client != null)
            {
                client.LastHeartbeatAtUtc = DateTime.UtcNow;
                client.LastStatus = status;
                client.LastError = status == "Error" ? message : null;
                client.UpdatedAtUtc = DateTime.UtcNow;
            }

            var log = new DesktopConnectorHeartbeatLog
            {
                CompanyId = companyId,
                DesktopConnectorClientId = client?.Id ?? 0,
                MachineName = machineName,
                Version = version,
                HeartbeatAtUtc = DateTime.UtcNow,
                Status = status,
                Message = message,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.DesktopConnectorHeartbeatLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> IsConnectorActiveAsync(
            int companyId,
            TimeSpan maxAge,
            CancellationToken ct = default)
        {
            var threshold = DateTime.UtcNow.Subtract(maxAge);

            return await _db.DesktopConnectorClients
                .AnyAsync(x =>
                    x.CompanyId == companyId &&
                    x.IsActive &&
                    x.IsLicensed &&
                    !x.IsDeleted &&
                    x.LastHeartbeatAtUtc != null &&
                    x.LastHeartbeatAtUtc >= threshold,
                    ct);
        }
    }
}