using Finans.Application.Abstractions.Banking;

namespace Finans.Infrastructure.Banking
{
    internal static class BankProviderRegistry
    {
        public static IReadOnlyDictionary<string, IBankProvider> Build(IEnumerable<IBankProvider> providers)
        {
            var map = new Dictionary<string, IBankProvider>(StringComparer.OrdinalIgnoreCase);

            foreach (var provider in providers)
            {
                Register(map, provider.ProviderCode, provider);

                foreach (var alias in provider.ProviderAliases)
                    Register(map, alias, provider);
            }

            return map;
        }

        public static string FormatAvailableCodes(IReadOnlyDictionary<string, IBankProvider> providers)
            => providers.Count == 0
                ? "(provider kaydı yok)"
                : string.Join(", ", providers.Keys.OrderBy(x => x));

        private static void Register(
            IDictionary<string, IBankProvider> map,
            string? code,
            IBankProvider provider)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;

            var normalizedCode = code.Trim();
            if (map.TryGetValue(normalizedCode, out var existing) && !ReferenceEquals(existing, provider))
            {
                throw new InvalidOperationException(
                    $"Banka provider kodu çakışıyor: {normalizedCode}. " +
                    $"{existing.GetType().Name} ve {provider.GetType().Name} aynı kodu kullanıyor.");
            }

            map[normalizedCode] = provider;
        }
    }
}
