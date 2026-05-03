using Finans.Application;
using Finans.Data;
using Finans.Data.Context;
using Finans.Infrastructure;
using Finans.WorkerService.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Finans.WorkerService";
});

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

builder.Services.AddDbContext<FinansDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("FinansDb"));
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddHostedService<BankImportWorker>();

if (builder.Configuration.GetValue<bool>("Worker:EnableErpTransferWorker", false))
{
    builder.Services.AddHostedService<ErpTransferWorker>();
}

var host = builder.Build();
await host.Services.InitializeFinansDbAsync();
await host.RunAsync();
