using Finans.Contracts.Integration;

namespace Finans.Infrastructure.Queries.Integration
{
    public interface IDesktopConnectorQuery
    {
        Task<IReadOnlyList<DesktopConnectorClientDto>> ListAsync(int companyId, CancellationToken ct);
    }
}