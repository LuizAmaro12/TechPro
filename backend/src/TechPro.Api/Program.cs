using Microsoft.EntityFrameworkCore;
using Serilog;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<TenantSessionInterceptor>();
builder.Services.AddDbContext<TechProDbContext>((sp, options) => options
    .UseNpgsql(builder.Configuration.GetConnectionString("TechPro"))
    .UseSnakeCaseNamingConvention()
    .AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>()));

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Em desenvolvimento o schema é aplicado no startup; em produção a
    // migration roda como passo de deploy, nunca automaticamente.
    if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("TechPro")))
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TechProDbContext>().Database.Migrate();
    }
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposto para o WebApplicationFactory dos testes de integração.
public partial class Program;
