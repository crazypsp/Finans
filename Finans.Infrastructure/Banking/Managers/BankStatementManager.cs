using System.Security.Cryptography;
using System.Text;
using Finans.Application.Abstractions.Banking;
using Finans.Application.Models.Banking;
using Finans.Infrastructure.Banking;

namespace Finans.Infrastructure.Banking.Managers
{
    public sealed class BankStatementManager : IBankStatementManager
    {
        private readonly IReadOnlyDictionary<string, IBankProvider> _providers;

        public BankStatementManager(IEnumerable<IBankProvider> providers)
        {
            _providers = BankProviderRegistry.Build(providers);
        }

        public async Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default)
        {
            Validate(request);

            if (!_providers.TryGetValue(request.ProviderCode, out var provider))
            {
                var availableCodes = BankProviderRegistry.FormatAvailableCodes(_providers);
                throw new InvalidOperationException(
                    $"ProviderCode={request.ProviderCode} için provider yok. Kayıtlı kodlar: {availableCodes}");
            }

            BankStatementResult raw;
            try
            {
                raw = await provider.GetStatementAsync(request, ct);
            }
            catch (Exception ex)
            {
                throw new Exception($"Banka provider hata. Provider={request.ProviderCode}. {ex.Message}", ex);
            }

            if (!raw.IsSuccess)
                return raw;

            Normalize(raw.Rows, request.AccountNumber);
            raw.Rows = Deduplicate(raw.Rows)
                .OrderByDescending(x => x.TransactionDate)
                .ThenByDescending(x => x.ReferenceNumber)
                .ToList();

            return raw;
        }

        private static void Validate(BankStatementRequest r)
        {
            if (r.BankId <= 0) throw new ArgumentException("BankId zorunlu");
            if (string.IsNullOrWhiteSpace(r.ProviderCode)) throw new ArgumentException("ProviderCode zorunlu");
            if (string.IsNullOrWhiteSpace(r.Username)) throw new ArgumentException("Username zorunlu");
            if (string.IsNullOrWhiteSpace(r.Password)) throw new ArgumentException("Password zorunlu");
            if (string.IsNullOrWhiteSpace(r.AccountNumber)) throw new ArgumentException("AccountNumber zorunlu");
            if (r.EndDate < r.StartDate) throw new ArgumentException("EndDate < StartDate olamaz");
        }

        private static void Normalize(List<BankStatementRow> rows, string accountNo)
        {
            foreach (var r in rows)
            {
                r.AccountNumber ??= accountNo;
                r.Description ??= "";
                r.Currency = string.IsNullOrWhiteSpace(r.Currency) ? "TRY" : r.Currency;
                r.DebitCredit = r.DebitCredit is "C" or "D"
                    ? r.DebitCredit
                    : (r.Amount >= 0 ? "C" : "D");

                if (string.IsNullOrWhiteSpace(r.ExternalUniqueKey))
                    r.ExternalUniqueKey = HashRow(r);
            }
        }

        private static List<BankStatementRow> Deduplicate(List<BankStatementRow> rows)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<BankStatementRow>(rows.Count);

            foreach (var r in rows)
            {
                var key = string.IsNullOrWhiteSpace(r.ExternalUniqueKey) ? HashRow(r) : r.ExternalUniqueKey;
                if (seen.Add(key))
                    result.Add(r);
            }

            return result;
        }

        private static string HashRow(BankStatementRow r)
        {
            var raw = string.Join("|", new[]
            {
                r.AccountNumber ?? "",
                r.TransactionDate.ToString("O"),
                r.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                r.Currency ?? "",
                r.DebitCredit ?? "",
                r.Description ?? "",
                r.ReferenceNumber ?? "",
                r.BalanceAfter?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""
            });

            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }
}
