using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Application.Abstractions.Security
{
    /// <summary>
    /// Neden var?
    /// - Şifreleme algoritmasını UI/Data katmanına bulaştırmamak için.
    /// - Yarın ASP.NET Identity'ye geçsek bile sadece bu implementasyonu değiştiririz.
    /// </summary>
    public interface IPasswordHasher
    {
        (string Hash, string Salt) HashPassword(string password);
        bool Verify(string password, string hash, string salt);
    }
}
