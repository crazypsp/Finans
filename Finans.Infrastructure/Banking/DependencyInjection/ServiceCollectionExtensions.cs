using Finans.Application.Abstractions.Banking;
using Finans.Infrastructure.Banking.Managers;
using Finans.Infrastructure.Banking.Managers.BankProviders;
using Finans.Infrastructure.Banking.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Infrastructure.Banking.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBankingInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IBankStatementManager, BankStatementManager>();
            services.AddScoped<IBankProviderResolver, BankProviderResolver>();

            // Gerçek banka provider'ları
            services.AddScoped<IBankProvider, IsBankStatementProvider>();
            services.AddScoped<IBankProvider, AkbankStatementProvider>();
            services.AddScoped<IBankProvider, KuveytTurkStatementProvider>();
            services.AddScoped<IBankProvider, QnbMaestroStatementProvider>();
            services.AddScoped<IBankProvider, VakifBankStatementProvider>();
            services.AddScoped<IBankProvider, ZiraatKatilimStatementProvider>();
            services.AddScoped<IBankProvider, TurkiyeFinansStatementProvider>();
            services.AddScoped<IBankProvider, VakifKatilimStatementProvider>();
            services.AddScoped<IBankProvider, SekerbankStatementProvider>();
            services.AddScoped<IBankProvider>(_ => new UnavailableBankProvider(
                "ALB",
                "Albaraka Turk",
                "Provider proxy/dokumani projede tamamlanmadigi icin otomatik import aktif degil."));
            services.AddScoped<IBankProvider>(_ => new UnavailableBankProvider(
                "ANB",
                "Anadolubank",
                "Dokumanda endpoint/istek semasi olmadigi icin otomatik import aktif degil."));
            services.AddScoped<IBankProvider>(_ => new UnavailableBankProvider(
                "DEN",
                "Denizbank",
                "InterAPI sertifika/abonelik ve production endpoint dogrulamasi gerektirdigi icin otomatik import aktif degil."));
            services.AddScoped<IBankProvider>(_ => new UnavailableBankProvider(
                "EML",
                "Emlak Katilim",
                "Provider proxy/dokumani projede tamamlanmadigi icin otomatik import aktif degil."));

            services.AddMemoryCache();
            services.AddHttpClient();

            return services;
        }
    }
}
