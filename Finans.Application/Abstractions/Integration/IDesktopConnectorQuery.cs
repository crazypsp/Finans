namespace Finans.Application.Abstractions.Integration;

public interface IDesktopConnectorQuery
{
    Task<bool> IsAliveAsync();
}