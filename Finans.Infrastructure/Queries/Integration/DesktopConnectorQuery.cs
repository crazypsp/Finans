using Dapper;
using Finans.Contracts.Integration;
using Finans.Infrastructure.Data;

namespace Finans.Infrastructure.Queries.Integration
{
    public sealed class DesktopConnectorQuery : IDesktopConnectorQuery
    {
        private readonly ISqlConnectionFactory _factory;

        public DesktopConnectorQuery(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<DesktopConnectorClientDto>> ListAsync(int companyId, CancellationToken ct)
        {
            const string sql = @"
SELECT
    Id,
    CompanyId,
    MachineName,
    ConnectorKey,
    Version,
    IsActive,
    IsLicensed,
    LastHeartbeatAtUtc,
    LastTransferAtUtc,
    LastStatus,
    LastError
FROM DesktopConnectorClients
WHERE CompanyId = @CompanyId
  AND IsDeleted = 0
ORDER BY Id DESC;
";

            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<DesktopConnectorClientDto>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }
    }
}