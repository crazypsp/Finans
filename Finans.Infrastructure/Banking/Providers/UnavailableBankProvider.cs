using Finans.Application.Abstractions.Banking;
using Finans.Application.Models.Banking;

namespace Finans.Infrastructure.Banking.Providers
{
    public sealed class UnavailableBankProvider : IBankProvider
    {
        private readonly string _message;

        public UnavailableBankProvider(string providerCode, string bankName, string message)
        {
            ProviderCode = providerCode;
            _message = $"{bankName}: {message}";
        }

        public string ProviderCode { get; }

        public Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct = default)
            => Task.FromResult(new BankStatementResult
            {
                IsSuccess = false,
                Error = _message,
                RawResponse = null,
                Rows = new List<BankStatementRow>()
            });
    }
}
