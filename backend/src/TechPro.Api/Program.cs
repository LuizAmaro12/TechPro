using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.Comunicacao;
using TechPro.Api.Modules.Comunicacao.Canais;
using TechPro.Api.Modules.Configuracoes;
using TechPro.Api.Modules.Dashboard;
using TechPro.Api.Modules.Financeiro;
using TechPro.Api.Modules.Onboarding;
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
builder.Services.AddScoped<ImportacaoClientesService>();
builder.Services.AddScoped<AparelhoService>();
builder.Services.AddScoped<LgpdService>();
builder.Services.AddScoped<AgendaService>();
builder.Services.AddScoped<DisponibilidadeService>();
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<FilaEsperaService>();
builder.Services.AddScoped<OrdemServicoService>();
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<OrdemServicoPecaService>();
builder.Services.AddScoped<OrdemServicoInteracaoService>();
builder.Services.AddScoped<TechPro.Api.Modules.Reputacao.AvaliacaoService>();
builder.Services.AddScoped<FinanceiroService>();
builder.Services.AddScoped<FinanceiroRelatorioService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<OnboardingService>();
builder.Services.AddScoped<ConfiguracoesService>();

// --- Comunicação (módulo 9): provedor abstraído, adaptador log por padrão ---
builder.Services.AddScoped<ComunicacaoService>();
builder.Services.AddScoped<LembreteJob>();

// WhatsApp: Evolution só quando explicitamente selecionado; senão, log.
if (string.Equals(builder.Configuration["Comunicacao:Whatsapp:Provedor"], "evolution",
        StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton(new EvolutionOpcoes
    {
        BaseUrl = builder.Configuration["Comunicacao:Whatsapp:Evolution:BaseUrl"],
        ApiKey = builder.Configuration["Comunicacao:Whatsapp:Evolution:ApiKey"],
        Instancia = builder.Configuration["Comunicacao:Whatsapp:Evolution:Instancia"],
    });
    builder.Services.AddHttpClient<EvolutionWhatsAppCanal>();
    builder.Services.AddScoped<ICanalNotificacao>(sp =>
        sp.GetRequiredService<EvolutionWhatsAppCanal>());
}
else
{
    builder.Services.AddScoped<ICanalNotificacao, LogWhatsAppCanal>();
}

// E-mail: Resend só quando explicitamente selecionado; senão, log.
if (string.Equals(builder.Configuration["Comunicacao:Email:Provedor"], "resend",
        StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton(new ResendOpcoes
    {
        ApiKey = builder.Configuration["Comunicacao:Email:Resend:ApiKey"],
        Remetente = builder.Configuration["Comunicacao:Email:Resend:Remetente"]
            ?? "TechPro <onboarding@resend.dev>",
    });
    builder.Services.AddHttpClient<ResendEmailCanal>();
    builder.Services.AddScoped<ICanalNotificacao>(sp =>
        sp.GetRequiredService<ResendEmailCanal>());
}
else
{
    builder.Services.AddScoped<ICanalNotificacao, LogEmailCanal>();
}

// Hangfire só quando habilitado (docker). Sem ele, o agendador é no-op — os
// testes e o `dotnet run` puro não dependem de Postgres/Hangfire.
var hangfireHabilitado = builder.Configuration.GetValue("Comunicacao:Hangfire:Habilitado", false);
if (hangfireHabilitado)
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(
            builder.Configuration.GetConnectionString("TechPro"))));
    builder.Services.AddHangfireServer();
    builder.Services.AddScoped<IAgendadorDeLembretes, HangfireAgendadorDeLembretes>();
}
else
{
    builder.Services.AddScoped<IAgendadorDeLembretes, AgendadorDeLembretesNulo>();
}
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

// Headers de segurança em toda resposta (defesa contra clickjacking, MIME
// sniffing e vazamento de referer). O CSP restringe as origens; a API só serve
// JSON/Swagger, então uma política enxuta basta — o front (Next/Vercel) tem o
// seu próprio CSP. Em Development afrouxamos o CSP para o Swagger UI funcionar.
app.Use(async (contexto, next) =>
{
    var headers = contexto.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Cross-Origin-Opener-Policy"] = "same-origin";
    headers["Content-Security-Policy"] = app.Environment.IsDevelopment()
        // Swagger UI usa estilos/scripts inline.
        ? "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'"
        : "default-src 'none'; frame-ancestors 'none'";
    await next();
});

app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Dashboard do Hangfire: duas guardas independentes (defesa em profundidade) —
// só é montado em Development E o próprio filtro nega fora de Development. Se
// alguém remover a condição do mount, o dashboard continua fechado em produção.
// Expor de verdade exige antes um filtro de autorização real (papel gestor).
if (hangfireHabilitado && app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new DashboardSomenteEmDesenvolvimento(app.Environment)],
    });
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Filtro do dashboard Hangfire: fail-closed fora de Development. É a segunda
/// guarda — a primeira é o mount condicional. Produção só deve expor o
/// dashboard com um filtro que exija autenticação e papel gestor.
/// </summary>
internal sealed class DashboardSomenteEmDesenvolvimento(IWebHostEnvironment ambiente)
    : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) =>
        ambiente.IsDevelopment();
}

// Exposto para o WebApplicationFactory dos testes de integração.
public partial class Program;
