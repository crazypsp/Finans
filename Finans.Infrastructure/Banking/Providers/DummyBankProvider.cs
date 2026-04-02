using Finans.Application.Abstractions.Banking;
using Finans.Application.Models.Banking;

namespace Finans.Infrastructure.Banking.Providers
{
    /// <summary>
    /// Neden var?
    /// - Gerçek bankalara bağlanmadan import pipeline'ı doğrulamak için.
    /// - Sonradan gerçek provider'lar bunun yerine eklenir.
    /// </summary>
    public sealed class DummyBankProvider : IBankProvider
    {
        public string ProviderCode => "DUMMY";

        public Task<BankStatementResult> GetStatementAsync(BankStatementRequest request, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            return Task.FromResult(new BankStatementResult
            {
                IsSuccess = true,
                RawResponse = "{ \"dummy\": true }",
                Rows = new List<BankStatementRow>
                {
                    new BankStatementRow
                    {
                        ExternalUniqueKey = $"DUMMY-{request.AccountNumber}-{now:yyyyMMddHHmmss}",
                        TransactionDate = now.Date,
                        Description = "Dummy bank txn",
                        Amount = 123.45m,
                        DebitCredit = "C",
                        Currency = "TRY",
                        ReferenceNumber = "REF-001"
                    }
                }
            });
        }
    }
}