using Finans.Application;
using Finans.Application.Abstractions.ERP;
using Finans.Application.Abstractions.Transfer;
using Finans.Data;
using Finans.Data.Context;
using Finans.DesktopConnector.Services;
using Finans.DesktopConnector.Workers;
using Finans.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/connector-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "Finans.DesktopConnector";
    });

    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    builder.Services.AddDbContext<FinansDbContext>(opt =>
    {
        opt.UseSqlServer(builder.Configuration.GetConnectionString("FinansDb"));
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure();

    // Logo Tiger servisleri — DesktopConnector gerçek implementasyonları kullanır
    builder.Services.AddScoped<ILogoTigerTransferService, LogoTigerTransferService>();
    builder.Services.AddScoped<IErpTransferClient, LogoTigerErpTransferClient>();
    builder.Services.AddScoped<IErpAccountSyncService, LogoTigerAccountSyncService>();

    // Background workers
    builder.Services.AddHostedService<ConnectorHeartbeatWorker>();
    builder.Services.AddHostedService<ErpTransferWorker>();

    var host = builder.Build();
    await host.Services.InitializeFinansDbAsync();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "DesktopConnector başlatılamadı.");
}
finally
{
    Log.CloseAndFlush();
}
