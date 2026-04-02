using Finans.Application.Abstractions.Security;
using Microsoft.AspNetCore.Mvc;

namespace Finans.WebMvc.Controllers
{
    /// <summary>
    /// Neden var?
    /// - İlk kurulumda seed ile gelen admin kullanıcısının şifresi placeholder olduğu için,
    ///   güvenli bir hash/salt üretip DB'ye yazabilmek için.
    /// - Sadece Development ortamında kullanılacak (Gün 2'de kapatacağız).
    /// </summary>
    public class DevToolsController : Controller
    {
        private readonly IPasswordHasher _hasher;

        public DevToolsController(IPasswordHasher hasher)
        {
            _hasher = hasher;
        }

        [HttpGet("/dev/hash")]
        public IActionResult Hash([FromQuery] string password)
        {
            var (hash, salt) = _hasher.HashPassword(password);
            return Ok(new { password, hash, salt });
        }
    }
}