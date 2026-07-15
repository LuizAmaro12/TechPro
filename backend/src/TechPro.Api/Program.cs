using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.Financeiro;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Auth;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Enums viajam como string no JSON (ex.: status do agendamento) — legível no
// contrato OpenAPI e estável se a ordem dos membros mudar.
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TenantAmbiente>();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<TenantSessionInterceptor>();
builder.Services.AddDbContext<TechProDbContext>((sp, options) => options
    .UseNpgsql(builder.Configuration.GetConnectionString("TechPro"))
    .UseSnakeCaseNamingConvention()
    .AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>()));

// --- Identity + JWT -------------------------------------------------------

builder.Services
    .AddIdentityCore<Usuario>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<TechProDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FornecedorService>();
builder.Services.AddScoped<PecaService>();
builder.Services.AddScoped<ServicoService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<AparelhoService>();
builder.Services.AddScoped<AgendaService>();
builder.Services.AddScoped<DisponibilidadeService>();
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<OrdemServicoService>();
builder.Services.AddScoped<OrdemServicoPecaService>();
builder.Services.AddScoped<FinanceiroService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var chaveJwt = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key não configurada — defina em appsettings ou na variável de ambiente Jwt__Key.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Claims chegam com os nomes originais do JWT (sub, role, tenant_id),
        // sem o mapeamento legado para URIs da Microsoft.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = TokenService.ClaimRole,
        };
    });
builder.Services.AddAuthorization();

// --- CORS + rate limiting -------------------------------------------------

builder.Services.AddCors(options => options.AddPolicy("frontend", politica => politica
    .WithOrigins(builder.Configuration["Cors:FrontendOrigin"] ?? "http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// Endpoints de auth são a porta de entrada para força bruta: janela fixa por
// IP. Configurável para que os testes de integração não esbarrem no limite.
var limiteAuthPorMinuto = builder.Configuration.GetValue("RateLimiting:AuthPorMinuto", 10);
// A rota pública de agendamento não exige login (doc de stack, seção de
// segurança) — limite próprio, mais folgado que o de auth.
var limitePublicoPorMinuto = builder.Configuration.GetValue("RateLimiting:PublicoPorMinuto", 30);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limiteAuthPorMinuto,
            Window = TimeSpan.FromMinutes(1),
        }));
    options.AddPolicy("publico", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limitePublicoPorMinuto,
            Window = TimeSpan.FromMinutes(1),
        }));
});

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

app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposto para o WebApplicationFactory dos testes de integração.
public partial class Program;
