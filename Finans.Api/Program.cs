using Finans.Application;
using Finans.Data;
using Finans.Data.Context;
using Finans.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<FinansDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("FinansDb"));
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();
await app.Services.InitializeFinansDbAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
