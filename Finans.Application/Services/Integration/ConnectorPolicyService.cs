using Finans.Application.Abstractions.Integration;
using Finans.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Finans.Application.Services.Integration
{
    public sealed class ConnectorPolicyService : IConnectorPolicyService
    {
        private readonly FinansDbContext _db;

        public ConnectorPolicyService(FinansDbContext db)
        {
            _db = db;
        }

        public async Task<(bool IsAllowed, string? Message)> CanTransferAsync(
            int companyId,
            string machineName,
            string version,
            CancellationToken ct = default)
        {
            var client = await _db.DesktopConnectorClients
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.MachineName == machineName &&
                    !x.IsDeleted,
                    ct);

            if (client == null)
                return (false, "Connector kaydı bulunamadı.");

            if (!client.IsActive)
                return (false, "Connector pasif durumda.");

            if (!client.IsLicensed)
                return (false, "Connector lisansı pasif.");

            // İlk fazda basit versiyon kontrolü
            if (!string.Equals(client.Version, version, StringComparison.OrdinalIgnoreCase))
                return (false, $"Connector sürümü uyumsuz. DB={client.Version}, Local={version}");

            return (true, null);
        }
    }
}