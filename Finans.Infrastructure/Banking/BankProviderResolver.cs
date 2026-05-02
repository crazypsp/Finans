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
        private readonly IReadOnlyDictionary<string, IBankProvider> _providers;

        public BankProviderResolver(IEnumerable<IBankProvider> providers)
        {
            _providers = BankProviderRegistry.Build(providers);
        }

        public IBankProvider Resolve(string providerCode)
        {
            if (string.IsNullOrWhiteSpace(providerCode))
                throw new ArgumentException("ProviderCode zorunlu.", nameof(providerCode));

            if (_providers.TryGetValue(providerCode, out var provider))
                return provider;

            var availableCodes = BankProviderRegistry.FormatAvailableCodes(_providers);
            throw new InvalidOperationException(
                $"Bank provider not found: {providerCode}. Available codes: {availableCodes}");
        }
    }
}
