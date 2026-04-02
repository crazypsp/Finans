namespace Finans.Application.Abstractions.Banking
{
    /// <summary>
    /// Neden var?
    /// - Provider seçimi (code -> implementasyon) Infrastructure'da çözülür,
    ///   fakat sözleşme Application'da tanımlanır (katman bağımlılığı bozulmasın).
    /// </summary>
    public interface IBankProviderResolver
    {
        IBankProvider Resolve(string providerCode);
    }
}