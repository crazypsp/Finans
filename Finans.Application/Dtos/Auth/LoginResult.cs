using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Application.Dtos.Auth
{
    public sealed class LoginResult
    {
        public bool IsSuccess { get; set; }
        public string? Error { get; set; }

        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public int? CompanyId { get; set; }
        public string[] RoleCodes { get; set; } = Array.Empty<string>();
    }
}
