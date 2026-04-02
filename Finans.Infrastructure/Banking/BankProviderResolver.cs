using Finans.Application.Abstractions.Banking;

namespace Finans.Infrastructure.Banking
{
    /// <summary>
    /// Neden var?
    /// - ProviderCode'a göre doğru entegrasyon sınıfını seçmek için.
    /// - Provider eklemek sadece yeni class yazıp DI'ye eklemekle mümkün olsun.
    /// </summary>
    public sealed class BankProviderResolver : IBankProviderResolver
    {
        private readonly IEnumerable<IBankProvider> _providers;

        public BankProviderResolver(IEnumerable<IBankProvider> providers)
        {
            _providers = providers;
        }

        public IBankProvider Resolve(string providerCode)
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderCode == providerCode);
            if (provider == null)
                throw new InvalidOperationException($"Bank provider not found: {providerCode}");

            return provider;
        }
    }
}