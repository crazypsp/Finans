using System.Threading;
using System.Threading.Tasks;
using Finans.Application.Dtos.Auth;

namespace Finans.Application.Abstractions.Security
{
    /// <summary>
    /// Neden var?
    /// - WebMvc'nin DbContext'i direkt kullanmasını engellemek için.
    /// - Login iş kuralı tek yerde kalsın diye.
    /// </summary>
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
    }
}