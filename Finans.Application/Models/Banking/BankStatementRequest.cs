using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Application.Models.Banking
{
    /// <summary>
    /// Neden var?
    /// - Bankalara göre değişen credential/parametreleri normalize edip provider'a tek tip iletmek için.
    /// - Worker/Service katmanı bankaya özgü model bilmesin.
    /// </summary>
    public sealed class BankStatementRequest
    {
        public int CompanyId { get; set; }
        public int BankId { get; set; }

        public string ProviderCode { get; set; } = null!;

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = null!;

        // Credential
        public string? Secret { get; set; }
        public string? ExtrasJson { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string? Link { get; set; }
        public string? TLink { get; set; }

        public Dictionary<string, string> Extras { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string? GetExtra(string key) => Extras.TryGetValue(key, out var v) ? v : null;

        public string GetExtraRequired(string key)
            => GetExtra(key) is { Length: > 0 } v
                ? v
                : throw new ArgumentException($"Extras['{key}'] zorunlu.");
    }
}
