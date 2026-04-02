using Finans.Application.Models.Banking;

namespace Finans.Infrastructure.Banking.Base
{
    public static class ProviderRequestGuards
    {
        public static void EnsureBasic(BankStatementRequest request, string providerName)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException($"{providerName} için Username zorunlu.");
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException($"{providerName} için Password zorunlu.");
            if (string.IsNullOrWhiteSpace(request.AccountNumber))
                throw new ArgumentException($"{providerName} için AccountNumber zorunlu.");
            if (request.EndDate < request.StartDate)
                throw new ArgumentException($"{providerName} için tarih aralığı geçersiz.");
        }
    }
}
