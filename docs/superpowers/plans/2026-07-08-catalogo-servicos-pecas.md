# Catálogo (Serviços e Peças) — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Módulo 6 da Fase 1 (item 2 da ordem recomendada de `docs/fases_MVP.md`): cadastro de serviços, peças e fornecedores com relação serviço-peça, checklist padrão e capacidade — primeiro módulo de produto sobre a fundação multi-tenant (GQF + RLS), de ponta a ponta (API + testes + cliente orval + telas).

**Architecture:** Novo módulo `Modules/ServicosEPecas/` no monolito modular (seção 12 do doc de stack): entidades `ITenantEntity` com PK `int` identity (UUID/`updated_at`/`deleted_at` são exclusivos do escopo offline do técnico — seção 5), Global Query Filter automático por convenção + `RlsHelper` na migration. Controllers finos + Services com DbContext direto (mesmo padrão do módulo de Auth — sem camada Repository, desvio consciente já documentado). Exclusão de serviço/peça é desativação (`ativo=false`) para preservar histórico futuro de OS.

**Tech Stack:** .NET 10 Web API, EF Core 10 + Npgsql + EFCore.NamingConventions, FluentValidation, xUnit + WebApplicationFactory (Sqlite in-memory), Next.js 16 App Router, TanStack Query, orval, RHF + Zod 4, shadcn/ui.

## Global Constraints

- Documentos em `docs/` são vinculantes; não substituir tecnologia/padrão por conta própria.
- Toda mensagem de UI, validação e erro em pt-BR.
- Commits convencionais em pt-BR terminando com `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`. Nunca commitar `.env`.
- **Smart App Control bloqueia `dotnet test`/`dotnet ef` locais** (DLL não assinada). Todo build/teste/migration roda no container do SDK (comandos exatos em cada task). Testes: sempre a suíte inteira.
- Toda nova tabela de tenant: coluna `tenant_id` + entidade `ITenantEntity` (GQF automático) + `RlsHelper.AplicarIsolamentoTenant` na migration. PK `int` identity no catálogo.
- Snake_case é automático (`UseSnakeCaseNamingConvention`), mas nomes de tabela são explícitos via `ToTable` (plural pt-BR), como em `empresas`/`usuarios`.
- Access token só em memória no front (decisão aprovada nº 4) — nada muda aqui.
- Antes de escrever código de front, ler o guia relevante em `frontend/node_modules/next/dist/docs/` (ordem do `frontend/AGENTS.md`).
- Decisões aprovadas em 2026-07-07 para esta etapa: rotas `/servicos` e `/pecas` separadas; categoria = texto livre com sugestões no front; fornecedor = tabela mínima com FK na peça; `capacidade_simultanea` já nasce no serviço.

**Comandos de verificação (usados em todas as tasks de backend):**

Testes (cópia `:ro` — não polui o host):
```bash
docker run --rm -v "C:\Projetos\Pessoal\TechPro\backend:/repo:ro" -v techpro-nuget:/root/.nuget mcr.microsoft.com/dotnet/sdk:10.0 bash -c "cp -r /repo /work && cd /work && dotnet test TechPro.slnx"
```

Migration (mount rw — o arquivo gerado precisa voltar ao host):
```bash
docker run --rm -v "C:\Projetos\Pessoal\TechPro\backend:/src" -w /src -v techpro-nuget:/root/.nuget mcr.microsoft.com/dotnet/sdk:10.0 bash -c "dotnet tool restore && dotnet ef migrations add Catalogo --project src/TechPro.Api"
```

---

### Task 1: Fábrica de testes compartilhada

A `AuthApiFactory` está embutida em `AuthFluxoTests.cs`; o catálogo precisa da mesma fábrica. Extrair e renomear.

**Files:**
- Create: `backend/tests/TechPro.Api.Tests/TechProApiFactory.cs`
- Modify: `backend/tests/TechPro.Api.Tests/Auth/AuthFluxoTests.cs` (remover a classe embutida, trocar o fixture)

**Interfaces:**
- Produces: `TechProApiFactory : WebApplicationFactory<Program>` (namespace `TechPro.Api.Tests`), usada via `IClassFixture<TechProApiFactory>` pelas Tasks 3–5.

- [ ] **Step 1: Criar `TechProApiFactory.cs`** — mover a classe de `AuthFluxoTests.cs` (linhas 17–55) renomeando para `TechProApiFactory`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TechPro.Api.Shared.Persistence;

namespace TechPro.Api.Tests;

/// <summary>
/// Fábrica de API para testes de integração de qualquer módulo: Sqlite em
/// memória no lugar do Postgres (sem interceptor de sessão RLS, que é
/// específico de Postgres) e rate limit alto para os testes de auth não
/// esbarrarem no limite de produção.
/// </summary>
public sealed class TechProApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "chave-de-teste-suficientemente-longa-para-hs256-64-bytes!!!!");
        builder.UseSetting("Jwt:Issuer", "TechPro");
        builder.UseSetting("Jwt:Audience", "TechPro");
        builder.UseSetting("Jwt:AccessTokenMinutos", "15");
        builder.UseSetting("RateLimiting:AuthPorMinuto", "1000");

        builder.ConfigureServices(services =>
        {
            // EF Core 8+: AddDbContext registra a configuração das options como
            // serviço próprio — sem removê-la, Npgsql e Sqlite ficam empilhados.
            services.RemoveAll<IDbContextOptionsConfiguration<TechProDbContext>>();
            services.RemoveAll<DbContextOptions<TechProDbContext>>();
            _conexao.Open();
            services.AddDbContext<TechProDbContext>(o => o.UseSqlite(_conexao));

            using var provisorio = services.BuildServiceProvider();
            using var escopo = provisorio.CreateScope();
            escopo.ServiceProvider.GetRequiredService<TechProDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _conexao.Dispose();
    }
}
```

- [ ] **Step 2: Atualizar `AuthFluxoTests.cs`** — apagar a classe `AuthApiFactory` e seus usings exclusivos (`Microsoft.AspNetCore.Hosting`, `Microsoft.Data.Sqlite`, `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Infrastructure`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.DependencyInjection.Extensions`, `TechPro.Api.Shared.Persistence`, `Microsoft.AspNetCore.Mvc.Testing`); trocar a declaração da classe de teste para:

```csharp
public class AuthFluxoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
```

(`TechPro.Api.Tests.Auth` enxerga o namespace pai sem `using` extra.)

- [ ] **Step 3: Rodar a suíte no container** (comando dos Global Constraints). Expected: **15 passed**.

- [ ] **Step 4: Commit**

```bash
git add backend/tests
git commit -m "refactor(testes): fabrica de api compartilhada para modulos alem de auth

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Entidades do catálogo + migration com RLS

**Files:**
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/Fornecedor.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/Peca.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/Servico.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/ServicoPeca.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/ServicoChecklistItem.cs`
- Modify: `backend/src/TechPro.Api/Shared/Persistence/TechProDbContext.cs`
- Create: `backend/src/TechPro.Api/Migrations/<timestamp>_Catalogo.cs` (gerada)
- Test: `backend/tests/TechPro.Api.Tests/Catalogo/CatalogoIsolamentoTests.cs`

**Interfaces:**
- Consumes: `ITenantEntity`, `RlsHelper.AplicarIsolamentoTenant(MigrationBuilder, string)`, convenção de GQF do `TechProDbContext`.
- Produces: entidades `Fornecedor`, `Peca`, `Servico`, `ServicoPeca`, `ServicoChecklistItem` e DbSets `db.Fornecedores`, `db.Pecas`, `db.Servicos` usados pelas Tasks 3–5.

- [ ] **Step 1: Teste que falha** — `CatalogoIsolamentoTests.cs` (o arquivo não compila ainda: entidades não existem — este é o "vermelho"):

```csharp
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Tests.Tenancy;

namespace TechPro.Api.Tests.Catalogo;

public class CatalogoIsolamentoTests
{
    private static (TechProDbContext Contexto, TenantProviderFake Provider) CriarContexto()
    {
        var provider = new TenantProviderFake();
        var options = new DbContextOptionsBuilder<TechProDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return (new TechProDbContext(options, provider), provider);
    }

    [Fact]
    public void EntidadesDoCatalogoSaoFiltradasPorTenant()
    {
        var (contexto, provider) = CriarContexto();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        contexto.AddRange(
            new Servico { TenantId = tenantA, Nome = "Troca de tela" },
            new Servico { TenantId = tenantB, Nome = "Troca de bateria" },
            new Peca { TenantId = tenantA, Nome = "Tela iPhone 13" },
            new Peca { TenantId = tenantB, Nome = "Bateria S23" },
            new Fornecedor { TenantId = tenantA, Nome = "PeçaBoa" },
            new Fornecedor { TenantId = tenantB, Nome = "ImportaCel" });
        contexto.SaveChanges();

        provider.TenantId = tenantA;

        Assert.Equal("Troca de tela", Assert.Single(contexto.Servicos).Nome);
        Assert.Equal("Tela iPhone 13", Assert.Single(contexto.Pecas).Nome);
        Assert.Equal("PeçaBoa", Assert.Single(contexto.Fornecedores).Nome);
    }

    [Fact]
    public void SemTenantNoContextoCatalogoFicaVazio()
    {
        var (contexto, provider) = CriarContexto();
        contexto.Add(new Servico { TenantId = Guid.NewGuid(), Nome = "Qualquer" });
        contexto.SaveChanges();

        provider.TenantId = null;

        Assert.Empty(contexto.Servicos.ToList());
    }
}
```

Nota: `TenantProviderFake` é `internal` em `TechPro.Api.Tests.Tenancy` (`GlobalQueryFilterTests.cs`) — mesmo assembly, só importar o namespace.

- [ ] **Step 2: Rodar a suíte no container.** Expected: **FAIL de compilação** (`Servico` não existe).

- [ ] **Step 3: Criar as entidades** (uma por arquivo, namespace `TechPro.Api.Modules.ServicosEPecas`):

`Fornecedor.cs`:
```csharp
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

/// <summary>
/// Fornecedor de peças. Entidade própria (não campo texto) porque a Fase 2
/// exige histórico de preço de compra por fornecedor — normalizar strings
/// digitadas à mão, com dados reais, custaria muito mais depois.
/// </summary>
public class Fornecedor : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Nome { get; set; }
    public string? Contato { get; set; }
}
```

`Peca.cs`:
```csharp
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

public class Peca : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal PrecoVenda { get; set; }
    public int QuantidadeEmEstoque { get; set; }
    public int EstoqueMinimo { get; set; }
    public int? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    // Desativar em vez de apagar: a peça pode estar referenciada por OS futuras.
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; }
}
```

`Servico.cs`:
```csharp
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

public class Servico : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Nome { get; set; }
    public string? Categoria { get; set; }
    public decimal PrecoBase { get; set; }
    public int DuracaoEstimadaMinutos { get; set; }
    public int? PrazoMedioDias { get; set; }
    public bool ExigeDiagnostico { get; set; }
    public bool AgendavelOnline { get; set; }
    /// <summary>Atendimentos simultâneos que a agenda aceita para este serviço (módulo 2, "desde o início").</summary>
    public int CapacidadeSimultanea { get; set; } = 1;
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; }
    public List<ServicoPeca> Pecas { get; set; } = [];
    public List<ServicoChecklistItem> Checklist { get; set; } = [];
}
```

`ServicoPeca.cs`:
```csharp
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

/// <summary>"Peças normalmente utilizadas" pelo serviço (módulo 6), com quantidade padrão.</summary>
public class ServicoPeca : ITenantEntity
{
    public int ServicoId { get; set; }
    public int PecaId { get; set; }
    public Guid TenantId { get; set; }
    public int QuantidadePadrao { get; set; } = 1;
    public Peca? Peca { get; set; }
}
```

`ServicoChecklistItem.cs`:
```csharp
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

/// <summary>
/// Item do checklist padrão do serviço. Tabela própria (não jsonb): a Fase 2
/// marca item a item na OS ("checklist de qualidade por tipo de serviço").
/// </summary>
public class ServicoChecklistItem : ITenantEntity
{
    public int Id { get; set; }
    public int ServicoId { get; set; }
    public Guid TenantId { get; set; }
    public int Ordem { get; set; }
    public required string Descricao { get; set; }
}
```

- [ ] **Step 4: Registrar no `TechProDbContext`** — adicionar `using TechPro.Api.Modules.ServicosEPecas;`, os DbSets logo abaixo de `RefreshTokens`:

```csharp
public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
public DbSet<Peca> Pecas => Set<Peca>();
public DbSet<Servico> Servicos => Set<Servico>();
```

e o bloco de configuração no fim de `OnModelCreating`, antes de `AplicarFiltroDeTenantPorConvencao(builder);`:

```csharp
// --- Catálogo (módulo 6): serviços, peças e fornecedores ----------------

builder.Entity<Fornecedor>(e =>
{
    e.ToTable("fornecedores");
    e.Property(x => x.Nome).HasMaxLength(200);
    e.Property(x => x.Contato).HasMaxLength(200);
    e.HasIndex(x => x.TenantId);
});

builder.Entity<Peca>(e =>
{
    e.ToTable("pecas");
    e.Property(x => x.Nome).HasMaxLength(200);
    e.Property(x => x.Descricao).HasMaxLength(500);
    e.Property(x => x.CustoUnitario).HasPrecision(10, 2);
    e.Property(x => x.PrecoVenda).HasPrecision(10, 2);
    e.HasIndex(x => x.TenantId);
    // Restrict: fornecedor com peça vinculada não pode sumir (service devolve 409).
    e.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId)
        .OnDelete(DeleteBehavior.Restrict);
});

builder.Entity<Servico>(e =>
{
    e.ToTable("servicos");
    e.Property(x => x.Nome).HasMaxLength(200);
    e.Property(x => x.Categoria).HasMaxLength(100);
    e.Property(x => x.PrecoBase).HasPrecision(10, 2);
    e.HasIndex(x => x.TenantId);
});

builder.Entity<ServicoPeca>(e =>
{
    e.ToTable("servico_pecas");
    e.HasKey(x => new { x.ServicoId, x.PecaId });
    e.HasIndex(x => x.TenantId);
    e.HasOne<Servico>().WithMany(s => s.Pecas).HasForeignKey(x => x.ServicoId);
    // Peça referenciada por serviço não pode ser apagada fisicamente.
    e.HasOne(x => x.Peca).WithMany().HasForeignKey(x => x.PecaId)
        .OnDelete(DeleteBehavior.Restrict);
});

builder.Entity<ServicoChecklistItem>(e =>
{
    e.ToTable("servico_checklist_itens");
    e.Property(x => x.Descricao).HasMaxLength(300);
    e.HasIndex(x => x.TenantId);
    e.HasOne<Servico>().WithMany(s => s.Checklist).HasForeignKey(x => x.ServicoId);
});
```

- [ ] **Step 5: Rodar a suíte no container.** Expected: **17 passed** (15 + 2 novos).

- [ ] **Step 6: Gerar a migration no container** (comando dos Global Constraints). Expected: `Migrations/<timestamp>_Catalogo.cs` criado no host. Conferir no arquivo: 5 `CreateTable` (fornecedores, pecas, servicos, servico_pecas, servico_checklist_itens), todos com coluna `tenant_id`.

- [ ] **Step 7: Aplicar RLS na migration** — no fim do método `Up()` do arquivo gerado, adicionar (com `using TechPro.Api.Shared.Tenancy;` no topo):

```csharp
RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "fornecedores");
RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "pecas");
RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "servicos");
RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "servico_pecas");
RlsHelper.AplicarIsolamentoTenant(migrationBuilder, "servico_checklist_itens");
```

(`Down()` não precisa de nada: as policies caem junto com as tabelas.)

- [ ] **Step 8: Verificar RLS de verdade no Postgres** — subir a stack e conferir `rowsecurity`:

```bash
docker compose up -d --build api
docker compose exec postgres psql -U postgres -d techpro -c "select tablename, rowsecurity, forcerowsecurity from pg_tables where schemaname='public' and tablename in ('fornecedores','pecas','servicos','servico_pecas','servico_checklist_itens');"
```

Expected: 5 linhas, todas `rowsecurity = t` e `forcerowsecurity = t`.

- [ ] **Step 9: Rodar a suíte no container de novo** (a migration não afeta Sqlite, mas garante compilação limpa). Expected: **17 passed**.

- [ ] **Step 10: Commit**

```bash
git add backend/src backend/tests
git commit -m "feat(catalogo): entidades de servicos, pecas e fornecedores com GQF e RLS

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: API de Fornecedores (+ fundações compartilhadas do módulo)

**Files:**
- Create: `backend/src/TechPro.Api/Shared/Api/PaginaResponse.cs`
- Create: `backend/src/TechPro.Api/Shared/Api/ValidacaoExtensions.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/CatalogoResultado.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/Dtos/FornecedorDtos.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/Validadores.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/FornecedorService.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/FornecedoresController.cs`
- Modify: `backend/src/TechPro.Api/Program.cs` (registrar service)
- Modify: `backend/src/TechPro.Api/Shared/Auth/AuthController.cs` (usar a extensão de validação — DRY)
- Test: `backend/tests/TechPro.Api.Tests/Catalogo/CatalogoFluxoTests.cs`

**Interfaces:**
- Consumes: `TechProApiFactory` (Task 1), `db.Fornecedores`/`db.Pecas` (Task 2), `AuthResponse` (login dos testes).
- Produces: `PaginaResponse<T>(IReadOnlyList<T> Itens, int Total, int Pagina, int TamanhoPagina)`; `CatalogoResultado<T>` com `Ok(T)`/`Falha(string)`; extensão `this ControllerBase.ProblemaDeValidacao(ValidationResult)`; `FornecedorRequest(string Nome, string? Contato)`, `FornecedorResponse(int Id, string Nome, string? Contato)`; endpoints `GET/POST /api/fornecedores`, `PUT/DELETE /api/fornecedores/{id}`; helpers de teste `RegistrarEmpresaAsync`/`EnviarAsync` reusados nas Tasks 4–5.

- [ ] **Step 1: Testes que falham** — criar `CatalogoFluxoTests.cs` com os helpers e os 2 testes de fornecedor (não compila: DTOs não existem):

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Auth;

namespace TechPro.Api.Tests.Catalogo;

public class CatalogoFluxoTests(TechProApiFactory fabrica) : IClassFixture<TechProApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    /// <summary>Registra uma empresa nova e devolve o access token do gestor.</summary>
    private async Task<string> RegistrarEmpresaAsync(string email)
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar", new
        {
            nomeEmpresa = $"Loja de {email}",
            nome = "Dono",
            email,
            senha = "senha123",
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var auth = await resposta.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    private async Task<HttpResponseMessage> EnviarAsync(
        HttpMethod metodo, string url, string token, object? corpo = null)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        return await _cliente.SendAsync(requisicao);
    }

    [Fact]
    public async Task FornecedorCrudCompleto()
    {
        var token = await RegistrarEmpresaAsync("fornecedor.crud@exemplo.com");

        var criado = await EnviarAsync(HttpMethod.Post, "/api/fornecedores", token,
            new { nome = "PeçaBoa Distribuidora", contato = "vendas@pecaboa.com" });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var fornecedor = await criado.Content.ReadFromJsonAsync<FornecedorResponse>();
        Assert.NotNull(fornecedor);

        var lista = await EnviarAsync(HttpMethod.Get, "/api/fornecedores", token);
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        var fornecedores = await lista.Content.ReadFromJsonAsync<List<FornecedorResponse>>();
        Assert.Contains(fornecedores!, f => f.Nome == "PeçaBoa Distribuidora");

        var atualizado = await EnviarAsync(HttpMethod.Put, $"/api/fornecedores/{fornecedor.Id}", token,
            new { nome = "PeçaBoa Ltda", contato = "(11) 99999-0000" });
        Assert.Equal(HttpStatusCode.OK, atualizado.StatusCode);

        var removido = await EnviarAsync(HttpMethod.Delete, $"/api/fornecedores/{fornecedor.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, removido.StatusCode);
    }

    [Fact]
    public async Task FornecedorDeOutraEmpresaEInvisivel()
    {
        var tokenA = await RegistrarEmpresaAsync("fornecedor.iso.a@exemplo.com");
        var tokenB = await RegistrarEmpresaAsync("fornecedor.iso.b@exemplo.com");

        var criado = await EnviarAsync(HttpMethod.Post, "/api/fornecedores", tokenA,
            new { nome = "Só da Empresa A", contato = (string?)null });
        var fornecedor = await criado.Content.ReadFromJsonAsync<FornecedorResponse>();

        var listaB = await EnviarAsync(HttpMethod.Get, "/api/fornecedores", tokenB);
        var fornecedoresB = await listaB.Content.ReadFromJsonAsync<List<FornecedorResponse>>();
        Assert.DoesNotContain(fornecedoresB!, f => f.Nome == "Só da Empresa A");

        var puxarB = await EnviarAsync(HttpMethod.Put, $"/api/fornecedores/{fornecedor!.Id}", tokenB,
            new { nome = "Tentativa de roubo", contato = (string?)null });
        Assert.Equal(HttpStatusCode.NotFound, puxarB.StatusCode);
    }
}
```

- [ ] **Step 2: Rodar a suíte no container.** Expected: **FAIL de compilação** (`FornecedorResponse` não existe).

- [ ] **Step 3: Implementar as fundações e o módulo de fornecedores.**

`Shared/Api/PaginaResponse.cs`:
```csharp
namespace TechPro.Api.Shared.Api;

/// <summary>Envelope padrão de listagens paginadas da API.</summary>
public record PaginaResponse<T>(IReadOnlyList<T> Itens, int Total, int Pagina, int TamanhoPagina);
```

`Shared/Api/ValidacaoExtensions.cs`:
```csharp
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace TechPro.Api.Shared.Api;

public static class ValidacaoExtensions
{
    /// <summary>Converte falhas do FluentValidation no ValidationProblemDetails padrão da API.</summary>
    public static IActionResult ProblemaDeValidacao(this ControllerBase controller, ValidationResult validacao)
    {
        foreach (var erro in validacao.Errors)
        {
            controller.ModelState.AddModelError(erro.PropertyName, erro.ErrorMessage);
        }

        return controller.ValidationProblem(controller.ModelState);
    }
}
```

`Modules/ServicosEPecas/CatalogoResultado.cs`:
```csharp
namespace TechPro.Api.Modules.ServicosEPecas;

/// <summary>
/// Resultado de escrita do catálogo: um valor ou uma mensagem de erro de
/// negócio (o controller traduz em 400 ProblemDetails).
/// </summary>
public sealed record CatalogoResultado<T>(T? Valor, string? Erro) where T : class
{
    public static CatalogoResultado<T> Ok(T valor) => new(valor, null);
    public static CatalogoResultado<T> Falha(string erro) => new(null, erro);
}
```

`Modules/ServicosEPecas/Dtos/FornecedorDtos.cs`:
```csharp
namespace TechPro.Api.Modules.ServicosEPecas.Dtos;

public record FornecedorRequest(string Nome, string? Contato);

public record FornecedorResponse(int Id, string Nome, string? Contato);
```

`Modules/ServicosEPecas/Validadores.cs` (os validadores de peça/serviço entram nas Tasks 4–5 neste mesmo arquivo):
```csharp
using FluentValidation;
using TechPro.Api.Modules.ServicosEPecas.Dtos;

namespace TechPro.Api.Modules.ServicosEPecas;

public class FornecedorRequestValidator : AbstractValidator<FornecedorRequest>
{
    public FornecedorRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Informe o nome do fornecedor.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(x => x.Contato)
            .MaximumLength(200).WithMessage("O contato pode ter no máximo 200 caracteres.");
    }
}
```

`Modules/ServicosEPecas/FornecedorService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

public class FornecedorService(TechProDbContext db, ITenantProvider tenantProvider)
{
    public enum Remocao { Removido, NaoEncontrado, EmUso }

    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    public async Task<IReadOnlyList<FornecedorResponse>> ListarAsync() =>
        await db.Fornecedores
            .OrderBy(f => f.Nome)
            .Select(f => new FornecedorResponse(f.Id, f.Nome, f.Contato))
            .ToListAsync();

    public async Task<FornecedorResponse> CriarAsync(FornecedorRequest request)
    {
        var fornecedor = new Fornecedor
        {
            TenantId = TenantId,
            Nome = request.Nome.Trim(),
            Contato = Normalizar(request.Contato),
        };
        db.Fornecedores.Add(fornecedor);
        await db.SaveChangesAsync();
        return new FornecedorResponse(fornecedor.Id, fornecedor.Nome, fornecedor.Contato);
    }

    public async Task<FornecedorResponse?> AtualizarAsync(int id, FornecedorRequest request)
    {
        // O GQF já limita ao tenant atual: id de outra empresa "não existe".
        var fornecedor = await db.Fornecedores.SingleOrDefaultAsync(f => f.Id == id);
        if (fornecedor is null)
        {
            return null;
        }

        fornecedor.Nome = request.Nome.Trim();
        fornecedor.Contato = Normalizar(request.Contato);
        await db.SaveChangesAsync();
        return new FornecedorResponse(fornecedor.Id, fornecedor.Nome, fornecedor.Contato);
    }

    public async Task<Remocao> RemoverAsync(int id)
    {
        var fornecedor = await db.Fornecedores.SingleOrDefaultAsync(f => f.Id == id);
        if (fornecedor is null)
        {
            return Remocao.NaoEncontrado;
        }

        if (await db.Pecas.AnyAsync(p => p.FornecedorId == id))
        {
            return Remocao.EmUso;
        }

        db.Fornecedores.Remove(fornecedor);
        await db.SaveChangesAsync();
        return Remocao.Removido;
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
```

`Modules/ServicosEPecas/FornecedoresController.cs`:
```csharp
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.ServicosEPecas;

[ApiController]
[Route("api/fornecedores")]
[Authorize]
[Produces("application/json")]
public class FornecedoresController(
    FornecedorService service,
    IValidator<FornecedorRequest> validador) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<FornecedorResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar() => Ok(await service.ListarAsync());

    [HttpPost]
    [ProducesResponseType<FornecedorResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(FornecedorRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var fornecedor = await service.CriarAsync(request);
        return Created($"/api/fornecedores/{fornecedor.Id}", fornecedor);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<FornecedorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, FornecedorRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var fornecedor = await service.AtualizarAsync(id, request);
        return fornecedor is null ? NotFound() : Ok(fornecedor);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(int id) => await service.RemoverAsync(id) switch
    {
        FornecedorService.Remocao.Removido => NoContent(),
        FornecedorService.Remocao.EmUso => Problem(
            title: "Este fornecedor tem peças vinculadas e não pode ser removido.",
            statusCode: StatusCodes.Status409Conflict),
        _ => NotFound(),
    };
}
```

`Program.cs` — depois de `builder.Services.AddScoped<AuthService>();`:
```csharp
builder.Services.AddScoped<FornecedorService>();
```
(com `using TechPro.Api.Modules.ServicosEPecas;` no topo.)

`AuthController.cs` — trocar o método privado `ProblemaDeValidacao` pela extensão: adicionar `using TechPro.Api.Shared.Api;`, apagar o método privado `ProblemaDeValidacao` (linhas 113–121) e trocar as duas chamadas `return ProblemaDeValidacao(validacao.Errors.Select(...));` e `return ProblemaDeValidacao(resultado.Erros.Select(...));` por:

```csharp
return this.ProblemaDeValidacao(validacao);           // em Registrar e Login
```
e, para os erros do Identity em Registrar (não vêm de um ValidationResult):
```csharp
foreach (var erro in resultado.Erros)
{
    ModelState.AddModelError(nameof(requisicao.Senha), erro);
}

return ValidationProblem(ModelState);
```

- [ ] **Step 4: Rodar a suíte no container.** Expected: **19 passed** (17 + 2).

- [ ] **Step 5: Commit**

```bash
git add backend/src backend/tests
git commit -m "feat(catalogo): crud de fornecedores com validacao e isolamento por tenant

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: API de Peças

**Files:**
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/Dtos/PecaDtos.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/PecaService.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/PecasController.cs`
- Modify: `backend/src/TechPro.Api/Modules/ServicosEPecas/Validadores.cs` (adicionar `PecaRequestValidator`)
- Modify: `backend/src/TechPro.Api/Program.cs` (registrar `PecaService`)
- Test: `backend/tests/TechPro.Api.Tests/Catalogo/CatalogoFluxoTests.cs` (adicionar testes)

**Interfaces:**
- Consumes: `CatalogoResultado<T>`, `PaginaResponse<T>`, `ValidacaoExtensions`, `FornecedorResponse`, helpers de teste da Task 3.
- Produces: `PecaRequest(string Nome, string? Descricao, decimal CustoUnitario, decimal PrecoVenda, int QuantidadeEmEstoque, int EstoqueMinimo, int? FornecedorId, bool Ativo)`; `PecaResponse(int Id, string Nome, string? Descricao, decimal CustoUnitario, decimal PrecoVenda, int QuantidadeEmEstoque, int EstoqueMinimo, FornecedorResponse? Fornecedor, bool EstoqueBaixo, bool Ativo)`; endpoints `GET /api/pecas?busca=&incluirInativas=&pagina=&tamanhoPagina=`, `GET/PUT/DELETE /api/pecas/{id}`, `POST /api/pecas`. A Task 5 consome `db.Pecas` e `PecaResponse`.

- [ ] **Step 1: Testes que falham** — adicionar a `CatalogoFluxoTests.cs`:

```csharp
private static object CorpoPeca(
    string nome = "Tela iPhone 13", int? fornecedorId = null, int quantidade = 5, int minimo = 2) => new
{
    nome,
    descricao = "OLED compatível",
    custoUnitario = 450.00m,
    precoVenda = 900.00m,
    quantidadeEmEstoque = quantidade,
    estoqueMinimo = minimo,
    fornecedorId,
    ativo = true,
};

[Fact]
public async Task PecaCrudCompletoComFornecedorEEstoqueBaixo()
{
    var token = await RegistrarEmpresaAsync("peca.crud@exemplo.com");

    var fornecedorResposta = await EnviarAsync(HttpMethod.Post, "/api/fornecedores", token,
        new { nome = "ImportaCel", contato = (string?)null });
    var fornecedor = await fornecedorResposta.Content.ReadFromJsonAsync<FornecedorResponse>();

    var criada = await EnviarAsync(HttpMethod.Post, "/api/pecas", token,
        CorpoPeca(fornecedorId: fornecedor!.Id));
    Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
    var peca = await criada.Content.ReadFromJsonAsync<PecaResponse>();
    Assert.NotNull(peca);
    Assert.Equal("ImportaCel", peca.Fornecedor?.Nome);
    Assert.False(peca.EstoqueBaixo);

    // Quantidade cai para o mínimo: alerta de estoque baixo (módulo 7).
    var atualizada = await EnviarAsync(HttpMethod.Put, $"/api/pecas/{peca.Id}", token,
        CorpoPeca(fornecedorId: fornecedor.Id, quantidade: 2, minimo: 2));
    Assert.Equal(HttpStatusCode.OK, atualizada.StatusCode);
    var pecaAtualizada = await atualizada.Content.ReadFromJsonAsync<PecaResponse>();
    Assert.True(pecaAtualizada!.EstoqueBaixo);

    // Desativar preserva o registro, mas some da listagem padrão.
    var desativada = await EnviarAsync(HttpMethod.Delete, $"/api/pecas/{peca.Id}", token);
    Assert.Equal(HttpStatusCode.NoContent, desativada.StatusCode);

    var listaPadrao = await EnviarAsync(HttpMethod.Get, "/api/pecas", token);
    var paginaPadrao = await listaPadrao.Content.ReadFromJsonAsync<PaginaResponse<PecaResponse>>();
    Assert.DoesNotContain(paginaPadrao!.Itens, p => p.Id == peca.Id);

    var listaCompleta = await EnviarAsync(HttpMethod.Get, "/api/pecas?incluirInativas=true", token);
    var paginaCompleta = await listaCompleta.Content.ReadFromJsonAsync<PaginaResponse<PecaResponse>>();
    Assert.Contains(paginaCompleta!.Itens, p => p.Id == peca.Id && !p.Ativo);
}

[Fact]
public async Task PecaDeOutraEmpresaEInvisivel()
{
    var tokenA = await RegistrarEmpresaAsync("peca.iso.a@exemplo.com");
    var tokenB = await RegistrarEmpresaAsync("peca.iso.b@exemplo.com");

    var criada = await EnviarAsync(HttpMethod.Post, "/api/pecas", tokenA, CorpoPeca(nome: "Peça secreta de A"));
    var peca = await criada.Content.ReadFromJsonAsync<PecaResponse>();

    var listaB = await EnviarAsync(HttpMethod.Get, "/api/pecas?incluirInativas=true", tokenB);
    var paginaB = await listaB.Content.ReadFromJsonAsync<PaginaResponse<PecaResponse>>();
    Assert.DoesNotContain(paginaB!.Itens, p => p.Nome == "Peça secreta de A");

    Assert.Equal(HttpStatusCode.NotFound,
        (await EnviarAsync(HttpMethod.Get, $"/api/pecas/{peca!.Id}", tokenB)).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound,
        (await EnviarAsync(HttpMethod.Delete, $"/api/pecas/{peca.Id}", tokenB)).StatusCode);
}

[Fact]
public async Task PecaComPrecoNegativoRetorna400()
{
    var token = await RegistrarEmpresaAsync("peca.validacao@exemplo.com");

    var resposta = await EnviarAsync(HttpMethod.Post, "/api/pecas", token, new
    {
        nome = "Peça inválida",
        custoUnitario = -1.00m,
        precoVenda = 10.00m,
        quantidadeEmEstoque = 0,
        estoqueMinimo = 0,
        ativo = true,
    });

    Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
}

[Fact]
public async Task CatalogoExigeAutenticacao()
{
    Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente.GetAsync("/api/pecas")).StatusCode);
    Assert.Equal(HttpStatusCode.Unauthorized, (await _cliente.GetAsync("/api/fornecedores")).StatusCode);
}
```

Adicionar aos usings do arquivo: `using TechPro.Api.Shared.Api;`.

- [ ] **Step 2: Rodar a suíte no container.** Expected: **FAIL de compilação** (`PecaResponse` não existe).

- [ ] **Step 3: Implementar.**

`Dtos/PecaDtos.cs`:
```csharp
namespace TechPro.Api.Modules.ServicosEPecas.Dtos;

public record PecaRequest(
    string Nome,
    string? Descricao,
    decimal CustoUnitario,
    decimal PrecoVenda,
    int QuantidadeEmEstoque,
    int EstoqueMinimo,
    int? FornecedorId,
    bool Ativo = true);

public record PecaResponse(
    int Id,
    string Nome,
    string? Descricao,
    decimal CustoUnitario,
    decimal PrecoVenda,
    int QuantidadeEmEstoque,
    int EstoqueMinimo,
    FornecedorResponse? Fornecedor,
    bool EstoqueBaixo,
    bool Ativo);
```

Adicionar a `Validadores.cs`:
```csharp
public class PecaRequestValidator : AbstractValidator<PecaRequest>
{
    public PecaRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Informe o nome da peça.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(x => x.Descricao)
            .MaximumLength(500).WithMessage("A descrição pode ter no máximo 500 caracteres.");
        RuleFor(x => x.CustoUnitario)
            .GreaterThanOrEqualTo(0).WithMessage("O custo unitário não pode ser negativo.");
        RuleFor(x => x.PrecoVenda)
            .GreaterThanOrEqualTo(0).WithMessage("O preço de venda não pode ser negativo.");
        RuleFor(x => x.QuantidadeEmEstoque)
            .GreaterThanOrEqualTo(0).WithMessage("A quantidade em estoque não pode ser negativa.");
        RuleFor(x => x.EstoqueMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("O estoque mínimo não pode ser negativo.");
    }
}
```

`PecaService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

public class PecaService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    public async Task<PaginaResponse<PecaResponse>> ListarAsync(
        string? busca, bool incluirInativas, int pagina, int tamanhoPagina)
    {
        var query = db.Pecas.Include(p => p.Fornecedor).AsQueryable();
        if (!incluirInativas)
        {
            query = query.Where(p => p.Ativo);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(p => p.Nome.ToLower().Contains(termo));
        }

        var total = await query.CountAsync();
        var itens = await query
            .OrderBy(p => p.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        return new PaginaResponse<PecaResponse>(
            itens.Select(ParaResponse).ToList(), total, pagina, tamanhoPagina);
    }

    public async Task<PecaResponse?> ObterAsync(int id)
    {
        var peca = await db.Pecas.Include(p => p.Fornecedor).SingleOrDefaultAsync(p => p.Id == id);
        return peca is null ? null : ParaResponse(peca);
    }

    public async Task<CatalogoResultado<PecaResponse>> CriarAsync(PecaRequest request)
    {
        if (!await FornecedorExisteAsync(request.FornecedorId))
        {
            return CatalogoResultado<PecaResponse>.Falha("Fornecedor não encontrado.");
        }

        var peca = new Peca
        {
            TenantId = TenantId,
            CriadoEm = DateTimeOffset.UtcNow,
            Nome = request.Nome.Trim(),
        };
        Aplicar(peca, request);
        db.Pecas.Add(peca);
        await db.SaveChangesAsync();
        return CatalogoResultado<PecaResponse>.Ok((await ObterAsync(peca.Id))!);
    }

    public async Task<CatalogoResultado<PecaResponse>?> AtualizarAsync(int id, PecaRequest request)
    {
        var peca = await db.Pecas.SingleOrDefaultAsync(p => p.Id == id);
        if (peca is null)
        {
            return null;
        }

        if (!await FornecedorExisteAsync(request.FornecedorId))
        {
            return CatalogoResultado<PecaResponse>.Falha("Fornecedor não encontrado.");
        }

        Aplicar(peca, request);
        await db.SaveChangesAsync();
        return CatalogoResultado<PecaResponse>.Ok((await ObterAsync(id))!);
    }

    public async Task<bool> DesativarAsync(int id)
    {
        var peca = await db.Pecas.SingleOrDefaultAsync(p => p.Id == id);
        if (peca is null)
        {
            return false;
        }

        peca.Ativo = false;
        await db.SaveChangesAsync();
        return true;
    }

    private static void Aplicar(Peca peca, PecaRequest request)
    {
        peca.Nome = request.Nome.Trim();
        peca.Descricao = string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao.Trim();
        peca.CustoUnitario = request.CustoUnitario;
        peca.PrecoVenda = request.PrecoVenda;
        peca.QuantidadeEmEstoque = request.QuantidadeEmEstoque;
        peca.EstoqueMinimo = request.EstoqueMinimo;
        peca.FornecedorId = request.FornecedorId;
        peca.Ativo = request.Ativo;
    }

    // O GQF faz fornecedor de outra empresa "não existir" aqui: referência
    // cruzada de tenant vira 400 de negócio, nunca um vínculo silencioso.
    private async Task<bool> FornecedorExisteAsync(int? fornecedorId) =>
        fornecedorId is null || await db.Fornecedores.AnyAsync(f => f.Id == fornecedorId);

    private static PecaResponse ParaResponse(Peca p) => new(
        p.Id, p.Nome, p.Descricao, p.CustoUnitario, p.PrecoVenda,
        p.QuantidadeEmEstoque, p.EstoqueMinimo,
        p.Fornecedor is null ? null : new FornecedorResponse(p.Fornecedor.Id, p.Fornecedor.Nome, p.Fornecedor.Contato),
        EstoqueBaixo: p.QuantidadeEmEstoque <= p.EstoqueMinimo,
        p.Ativo);
}
```

`PecasController.cs`:
```csharp
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.ServicosEPecas;

[ApiController]
[Route("api/pecas")]
[Authorize]
[Produces("application/json")]
public class PecasController(PecaService service, IValidator<PecaRequest> validador) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaResponse<PecaResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca,
        [FromQuery] bool incluirInativas = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20)
    {
        pagina = Math.Max(pagina, 1);
        tamanhoPagina = Math.Clamp(tamanhoPagina, 1, 100);
        return Ok(await service.ListarAsync(busca, incluirInativas, pagina, tamanhoPagina));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<PecaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(int id) =>
        await service.ObterAsync(id) is { } peca ? Ok(peca) : NotFound();

    [HttpPost]
    [ProducesResponseType<PecaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(PecaRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.CriarAsync(request);
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/pecas/{resultado.Valor!.Id}", resultado.Valor);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<PecaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, PecaRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.AtualizarAsync(id, request);
        if (resultado is null)
        {
            return NotFound();
        }

        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(resultado.Valor);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desativar(int id) =>
        await service.DesativarAsync(id) ? NoContent() : NotFound();
}
```

`Program.cs`: `builder.Services.AddScoped<PecaService>();` (junto ao registro do `FornecedorService`).

- [ ] **Step 4: Rodar a suíte no container.** Expected: **23 passed** (19 + 4).

- [ ] **Step 5: Commit**

```bash
git add backend/src backend/tests
git commit -m "feat(catalogo): crud de pecas com fornecedor e alerta de estoque baixo

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: API de Serviços (checklist + peças vinculadas)

**Files:**
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/Dtos/ServicoDtos.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/ServicoService.cs`
- Create: `backend/src/TechPro.Api/Modules/ServicosEPecas/ServicosController.cs`
- Modify: `backend/src/TechPro.Api/Modules/ServicosEPecas/Validadores.cs` (adicionar `ServicoRequestValidator`)
- Modify: `backend/src/TechPro.Api/Program.cs` (registrar `ServicoService`)
- Test: `backend/tests/TechPro.Api.Tests/Catalogo/CatalogoFluxoTests.cs` (adicionar testes)

**Interfaces:**
- Consumes: tudo das Tasks 2–4.
- Produces: `ServicoPecaRequest(int PecaId, int QuantidadePadrao)`; `ServicoRequest(string Nome, string? Categoria, decimal PrecoBase, int DuracaoEstimadaMinutos, int? PrazoMedioDias, bool ExigeDiagnostico, bool AgendavelOnline, int CapacidadeSimultanea, bool Ativo, IReadOnlyList<string> Checklist, IReadOnlyList<ServicoPecaRequest> Pecas)`; `ServicoPecaResponse(int PecaId, string Nome, int QuantidadePadrao)`; `ServicoResponse(int Id, string Nome, string? Categoria, decimal PrecoBase, int DuracaoEstimadaMinutos, int? PrazoMedioDias, bool ExigeDiagnostico, bool AgendavelOnline, int CapacidadeSimultanea, bool Ativo, IReadOnlyList<string> Checklist, IReadOnlyList<ServicoPecaResponse> Pecas)`; endpoints `GET /api/servicos?busca=&categoria=&incluirInativos=&pagina=&tamanhoPagina=`, `GET/PUT/DELETE /api/servicos/{id}`, `POST /api/servicos`.

- [ ] **Step 1: Testes que falham** — adicionar a `CatalogoFluxoTests.cs`:

```csharp
private static object CorpoServico(string nome = "Troca de tela", object[]? pecas = null) => new
{
    nome,
    categoria = "Tela",
    precoBase = 350.00m,
    duracaoEstimadaMinutos = 60,
    prazoMedioDias = 1,
    exigeDiagnostico = false,
    agendavelOnline = true,
    capacidadeSimultanea = 2,
    ativo = true,
    checklist = new[] { "Testar touch em toda a tela", "Conferir Face ID" },
    pecas = pecas ?? [],
};

[Fact]
public async Task ServicoCompletoComChecklistEPecas()
{
    var token = await RegistrarEmpresaAsync("servico.crud@exemplo.com");

    var pecaResposta = await EnviarAsync(HttpMethod.Post, "/api/pecas", token, CorpoPeca(nome: "Tela A54"));
    var peca = await pecaResposta.Content.ReadFromJsonAsync<PecaResponse>();

    var criado = await EnviarAsync(HttpMethod.Post, "/api/servicos", token,
        CorpoServico(pecas: [new { pecaId = peca!.Id, quantidadePadrao = 1 }]));
    Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
    var servico = await criado.Content.ReadFromJsonAsync<ServicoResponse>();
    Assert.NotNull(servico);
    Assert.Equal(["Testar touch em toda a tela", "Conferir Face ID"], servico.Checklist);
    Assert.Equal("Tela A54", Assert.Single(servico.Pecas).Nome);
    Assert.Equal(2, servico.CapacidadeSimultanea);

    // PUT substitui checklist e peças por inteiro.
    var atualizado = await EnviarAsync(HttpMethod.Put, $"/api/servicos/{servico.Id}", token, new
    {
        nome = "Troca de tela premium",
        categoria = "Tela",
        precoBase = 420.00m,
        duracaoEstimadaMinutos = 90,
        prazoMedioDias = 2,
        exigeDiagnostico = true,
        agendavelOnline = true,
        capacidadeSimultanea = 1,
        ativo = true,
        checklist = new[] { "Teste completo pós-reparo" },
        pecas = Array.Empty<object>(),
    });
    Assert.Equal(HttpStatusCode.OK, atualizado.StatusCode);
    var servicoAtualizado = await atualizado.Content.ReadFromJsonAsync<ServicoResponse>();
    Assert.Equal(["Teste completo pós-reparo"], servicoAtualizado!.Checklist);
    Assert.Empty(servicoAtualizado.Pecas);

    // Desativar preserva o registro fora da listagem padrão.
    Assert.Equal(HttpStatusCode.NoContent,
        (await EnviarAsync(HttpMethod.Delete, $"/api/servicos/{servico.Id}", token)).StatusCode);
    var lista = await EnviarAsync(HttpMethod.Get, "/api/servicos", token);
    var pagina = await lista.Content.ReadFromJsonAsync<PaginaResponse<ServicoResponse>>();
    Assert.DoesNotContain(pagina!.Itens, s => s.Id == servico.Id);
}

[Fact]
public async Task ServicoDeOutraEmpresaEInvisivel()
{
    var tokenA = await RegistrarEmpresaAsync("servico.iso.a@exemplo.com");
    var tokenB = await RegistrarEmpresaAsync("servico.iso.b@exemplo.com");

    var criado = await EnviarAsync(HttpMethod.Post, "/api/servicos", tokenA, CorpoServico(nome: "Só de A"));
    var servico = await criado.Content.ReadFromJsonAsync<ServicoResponse>();

    var listaB = await EnviarAsync(HttpMethod.Get, "/api/servicos?incluirInativos=true", tokenB);
    var paginaB = await listaB.Content.ReadFromJsonAsync<PaginaResponse<ServicoResponse>>();
    Assert.DoesNotContain(paginaB!.Itens, s => s.Nome == "Só de A");

    Assert.Equal(HttpStatusCode.NotFound,
        (await EnviarAsync(HttpMethod.Get, $"/api/servicos/{servico!.Id}", tokenB)).StatusCode);
}

[Fact]
public async Task ServicoNaoAceitaPecaDeOutraEmpresa()
{
    var tokenA = await RegistrarEmpresaAsync("servico.idor.a@exemplo.com");
    var tokenB = await RegistrarEmpresaAsync("servico.idor.b@exemplo.com");

    var pecaDeA = await (await EnviarAsync(HttpMethod.Post, "/api/pecas", tokenA, CorpoPeca()))
        .Content.ReadFromJsonAsync<PecaResponse>();

    // B tenta referenciar a peça de A pelo id: o GQF a torna inexistente → 400.
    var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", tokenB,
        CorpoServico(pecas: [new { pecaId = pecaDeA!.Id, quantidadePadrao = 1 }]));

    Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
}

[Fact]
public async Task ServicoSemNomeRetorna400()
{
    var token = await RegistrarEmpresaAsync("servico.validacao@exemplo.com");
    var resposta = await EnviarAsync(HttpMethod.Post, "/api/servicos", token, CorpoServico(nome: ""));
    Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
}
```

- [ ] **Step 2: Rodar a suíte no container.** Expected: **FAIL de compilação** (`ServicoResponse` não existe).

- [ ] **Step 3: Implementar.**

`Dtos/ServicoDtos.cs`:
```csharp
namespace TechPro.Api.Modules.ServicosEPecas.Dtos;

public record ServicoPecaRequest(int PecaId, int QuantidadePadrao);

public record ServicoRequest(
    string Nome,
    string? Categoria,
    decimal PrecoBase,
    int DuracaoEstimadaMinutos,
    int? PrazoMedioDias,
    bool ExigeDiagnostico,
    bool AgendavelOnline,
    int CapacidadeSimultanea,
    bool Ativo,
    IReadOnlyList<string> Checklist,
    IReadOnlyList<ServicoPecaRequest> Pecas);

public record ServicoPecaResponse(int PecaId, string Nome, int QuantidadePadrao);

public record ServicoResponse(
    int Id,
    string Nome,
    string? Categoria,
    decimal PrecoBase,
    int DuracaoEstimadaMinutos,
    int? PrazoMedioDias,
    bool ExigeDiagnostico,
    bool AgendavelOnline,
    int CapacidadeSimultanea,
    bool Ativo,
    IReadOnlyList<string> Checklist,
    IReadOnlyList<ServicoPecaResponse> Pecas);
```

Adicionar a `Validadores.cs`:
```csharp
public class ServicoRequestValidator : AbstractValidator<ServicoRequest>
{
    public ServicoRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Informe o nome do serviço.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(x => x.Categoria)
            .MaximumLength(100).WithMessage("A categoria pode ter no máximo 100 caracteres.");
        RuleFor(x => x.PrecoBase)
            .GreaterThanOrEqualTo(0).WithMessage("O preço base não pode ser negativo.");
        RuleFor(x => x.DuracaoEstimadaMinutos)
            .GreaterThan(0).WithMessage("Informe a duração estimada em minutos.");
        RuleFor(x => x.PrazoMedioDias)
            .GreaterThan(0).When(x => x.PrazoMedioDias.HasValue)
            .WithMessage("O prazo médio deve ser de pelo menos 1 dia.");
        RuleFor(x => x.CapacidadeSimultanea)
            .GreaterThan(0).WithMessage("A capacidade simultânea deve ser de pelo menos 1.");
        RuleFor(x => x.Checklist)
            .NotNull().WithMessage("Envie o checklist (pode ser vazio).");
        RuleForEach(x => x.Checklist)
            .NotEmpty().WithMessage("Item do checklist não pode ser vazio.")
            .MaximumLength(300).WithMessage("Item do checklist pode ter no máximo 300 caracteres.");
        RuleFor(x => x.Pecas)
            .NotNull().WithMessage("Envie a lista de peças (pode ser vazia).")
            .Must(pecas => pecas is null || pecas.Select(p => p.PecaId).Distinct().Count() == pecas.Count)
            .WithMessage("Não repita a mesma peça no serviço.");
        RuleForEach(x => x.Pecas).ChildRules(peca => peca
            .RuleFor(p => p.QuantidadePadrao)
            .GreaterThan(0).WithMessage("A quantidade padrão da peça deve ser de pelo menos 1."));
    }
}
```

`ServicoService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.ServicosEPecas;

public class ServicoService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    public async Task<PaginaResponse<ServicoResponse>> ListarAsync(
        string? busca, string? categoria, bool incluirInativos, int pagina, int tamanhoPagina)
    {
        var query = db.Servicos
            .Include(s => s.Checklist)
            .Include(s => s.Pecas).ThenInclude(sp => sp.Peca)
            .AsQueryable();

        if (!incluirInativos)
        {
            query = query.Where(s => s.Ativo);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(s => s.Nome.ToLower().Contains(termo));
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var termoCategoria = categoria.Trim().ToLower();
            query = query.Where(s => s.Categoria != null && s.Categoria.ToLower() == termoCategoria);
        }

        var total = await query.CountAsync();
        var itens = await query
            .OrderBy(s => s.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        return new PaginaResponse<ServicoResponse>(
            itens.Select(ParaResponse).ToList(), total, pagina, tamanhoPagina);
    }

    public async Task<ServicoResponse?> ObterAsync(int id)
    {
        var servico = await db.Servicos
            .Include(s => s.Checklist)
            .Include(s => s.Pecas).ThenInclude(sp => sp.Peca)
            .SingleOrDefaultAsync(s => s.Id == id);
        return servico is null ? null : ParaResponse(servico);
    }

    public async Task<CatalogoResultado<ServicoResponse>> CriarAsync(ServicoRequest request)
    {
        if (!await PecasExistemAsync(request.Pecas))
        {
            return CatalogoResultado<ServicoResponse>.Falha("Uma ou mais peças informadas não existem.");
        }

        var servico = new Servico
        {
            TenantId = TenantId,
            CriadoEm = DateTimeOffset.UtcNow,
            Nome = request.Nome.Trim(),
        };
        Aplicar(servico, request);
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();
        return CatalogoResultado<ServicoResponse>.Ok((await ObterAsync(servico.Id))!);
    }

    public async Task<CatalogoResultado<ServicoResponse>?> AtualizarAsync(int id, ServicoRequest request)
    {
        var servico = await db.Servicos
            .Include(s => s.Checklist)
            .Include(s => s.Pecas)
            .SingleOrDefaultAsync(s => s.Id == id);
        if (servico is null)
        {
            return null;
        }

        if (!await PecasExistemAsync(request.Pecas))
        {
            return CatalogoResultado<ServicoResponse>.Falha("Uma ou mais peças informadas não existem.");
        }

        Aplicar(servico, request);
        await db.SaveChangesAsync();
        return CatalogoResultado<ServicoResponse>.Ok((await ObterAsync(id))!);
    }

    public async Task<bool> DesativarAsync(int id)
    {
        var servico = await db.Servicos.SingleOrDefaultAsync(s => s.Id == id);
        if (servico is null)
        {
            return false;
        }

        servico.Ativo = false;
        await db.SaveChangesAsync();
        return true;
    }

    // Substituição integral de checklist e peças: semântica de PUT, coleções
    // pequenas — mais simples e à prova de estados parciais do que um diff.
    private void Aplicar(Servico servico, ServicoRequest request)
    {
        servico.Nome = request.Nome.Trim();
        servico.Categoria = string.IsNullOrWhiteSpace(request.Categoria) ? null : request.Categoria.Trim();
        servico.PrecoBase = request.PrecoBase;
        servico.DuracaoEstimadaMinutos = request.DuracaoEstimadaMinutos;
        servico.PrazoMedioDias = request.PrazoMedioDias;
        servico.ExigeDiagnostico = request.ExigeDiagnostico;
        servico.AgendavelOnline = request.AgendavelOnline;
        servico.CapacidadeSimultanea = request.CapacidadeSimultanea;
        servico.Ativo = request.Ativo;
        servico.Checklist.Clear();
        servico.Checklist.AddRange(request.Checklist.Select((descricao, indice) => new ServicoChecklistItem
        {
            TenantId = TenantId,
            Ordem = indice + 1,
            Descricao = descricao.Trim(),
        }));
        servico.Pecas.Clear();
        servico.Pecas.AddRange(request.Pecas.Select(p => new ServicoPeca
        {
            TenantId = TenantId,
            PecaId = p.PecaId,
            QuantidadePadrao = p.QuantidadePadrao,
        }));
    }

    // O GQF faz peça de outra empresa "não existir" aqui: referência cruzada
    // de tenant vira 400 de negócio, nunca um vínculo silencioso (IDOR).
    private async Task<bool> PecasExistemAsync(IReadOnlyList<ServicoPecaRequest> pecas)
    {
        if (pecas.Count == 0)
        {
            return true;
        }

        var ids = pecas.Select(p => p.PecaId).Distinct().ToList();
        return await db.Pecas.CountAsync(p => ids.Contains(p.Id)) == ids.Count;
    }

    private static ServicoResponse ParaResponse(Servico s) => new(
        s.Id, s.Nome, s.Categoria, s.PrecoBase, s.DuracaoEstimadaMinutos, s.PrazoMedioDias,
        s.ExigeDiagnostico, s.AgendavelOnline, s.CapacidadeSimultanea, s.Ativo,
        s.Checklist.OrderBy(i => i.Ordem).Select(i => i.Descricao).ToList(),
        s.Pecas.Select(p => new ServicoPecaResponse(p.PecaId, p.Peca?.Nome ?? "", p.QuantidadePadrao)).ToList());
}
```

`ServicosController.cs` — mesmo formato do `PecasController`, rota `api/servicos`:
```csharp
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPro.Api.Modules.ServicosEPecas.Dtos;
using TechPro.Api.Shared.Api;

namespace TechPro.Api.Modules.ServicosEPecas;

[ApiController]
[Route("api/servicos")]
[Authorize]
[Produces("application/json")]
public class ServicosController(ServicoService service, IValidator<ServicoRequest> validador) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaResponse<ServicoResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca,
        [FromQuery] string? categoria,
        [FromQuery] bool incluirInativos = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20)
    {
        pagina = Math.Max(pagina, 1);
        tamanhoPagina = Math.Clamp(tamanhoPagina, 1, 100);
        return Ok(await service.ListarAsync(busca, categoria, incluirInativos, pagina, tamanhoPagina));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(int id) =>
        await service.ObterAsync(id) is { } servico ? Ok(servico) : NotFound();

    [HttpPost]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(ServicoRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.CriarAsync(request);
        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/servicos/{resultado.Valor!.Id}", resultado.Valor);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, ServicoRequest request)
    {
        var validacao = await validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            return this.ProblemaDeValidacao(validacao);
        }

        var resultado = await service.AtualizarAsync(id, request);
        if (resultado is null)
        {
            return NotFound();
        }

        if (resultado.Erro is not null)
        {
            return Problem(title: resultado.Erro, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(resultado.Valor);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desativar(int id) =>
        await service.DesativarAsync(id) ? NoContent() : NotFound();
}
```

`Program.cs`: `builder.Services.AddScoped<ServicoService>();`.

- [ ] **Step 4: Rodar a suíte no container.** Expected: **27 passed** (23 + 4).

- [ ] **Step 5: Commit**

```bash
git add backend/src backend/tests
git commit -m "feat(catalogo): crud de servicos com checklist ordenado e pecas vinculadas

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Contrato — snapshot do swagger + cliente orval

**Files:**
- Modify: `frontend/openapi/swagger.json` (snapshot regenerado)
- Modify: `frontend/lib/api-client/gerado/index.ts` (regenerado pelo orval — nunca editar à mão)

**Interfaces:**
- Produces: hooks/fns tipados `useGetApiServicos`, `useGetApiPecas`, `useGetApiFornecedores`, `usePostApiServicos`, `usePostApiPecas`, `usePostApiFornecedores`, `usePutApiServicosId`, `usePutApiPecasId`, `usePutApiFornecedoresId`, `useDeleteApiServicosId`, `useDeleteApiPecasId`, `useDeleteApiFornecedoresId` e modelos `ServicoResponse`, `PecaResponse`, `FornecedorResponse` (+ tipos de params) usados nas Tasks 8–9.

- [ ] **Step 1: Rebuildar a API e capturar o swagger**

```bash
docker compose up -d --build api
curl --retry 5 --retry-connrefused http://localhost:5080/swagger/v1/swagger.json -o frontend/openapi/swagger.json
```

Expected: JSON contém paths `/api/servicos`, `/api/pecas`, `/api/fornecedores`.

- [ ] **Step 2: Regenerar o cliente e checar tipos**

```bash
cd frontend && npm run gerar-api && npx tsc --noEmit && npm run lint
```

Expected: geração sem erros; `tsc` e lint limpos. Conferir em `gerado/index.ts` que os hooks acima existem com esses nomes exatos (se o orval nomear diferente — ex.: sufixo do path param — anotar o nome real e usar o real nas Tasks 8–9).

- [ ] **Step 3: Commit**

```bash
git add frontend/openapi/swagger.json frontend/lib/api-client/gerado
git commit -m "feat(frontend): cliente tipado regenerado com endpoints do catalogo

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: Front — navegação do painel do empreendedor

**Files:**
- Modify: `frontend/app/(empreendedor)/layout.tsx` (header + nav compartilhados)
- Modify: `frontend/app/(empreendedor)/dashboard/page.tsx` (remover o header local; atualizar o texto de próximos módulos)

**Interfaces:**
- Consumes: `useAuth()` (`usuario`, `carregando`, `sair`).
- Produces: nav com links `/dashboard`, `/servicos`, `/pecas` visível em todas as páginas do grupo — as Tasks 8–9 criam as páginas sem se preocupar com header.

- [ ] **Step 1: Reescrever `frontend/app/(empreendedor)/layout.tsx`:**

```tsx
"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth/AuthProvider";

const LINKS = [
  { href: "/dashboard", rotulo: "Visão geral" },
  { href: "/servicos", rotulo: "Serviços" },
  { href: "/pecas", rotulo: "Peças" },
];

/**
 * Guarda de rota do lado do cliente: tudo em (empreendedor) exige sessão.
 * A segurança real está na API (JWT + GQF + RLS) — aqui é só UX.
 */
export default function EmpreendedorLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  const { usuario, carregando, sair } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (!carregando && !usuario) router.replace("/login");
  }, [carregando, usuario, router]);

  if (carregando || !usuario) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-white">
        <p className="text-sm text-[#6B7280]">Carregando sua sessão...</p>
      </main>
    );
  }

  async function aoSair() {
    await sair();
    router.replace("/login");
  }

  return (
    <main className="min-h-screen bg-white">
      <header className="mx-auto flex w-full max-w-5xl items-center justify-between px-6 py-6">
        <div className="flex items-center gap-8">
          <span className="text-lg font-bold text-[#14162B]">TechPro</span>
          <nav className="flex items-center gap-1">
            {LINKS.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={`rounded-full px-4 py-1.5 text-sm transition-colors ${
                  pathname.startsWith(link.href)
                    ? "bg-[#14162B] font-semibold text-white"
                    : "text-[#6B7280] hover:text-[#14162B]"
                }`}
              >
                {link.rotulo}
              </Link>
            ))}
          </nav>
        </div>
        <Button
          variant="ghost"
          onClick={aoSair}
          className="text-[#6B7280] hover:text-[#14162B]"
        >
          Sair
        </Button>
      </header>
      {children}
    </main>
  );
}
```

- [ ] **Step 2: Enxugar `dashboard/page.tsx`** — apagar o bloco `<header>...</header>` (o layout agora o fornece), remover os imports `Button` e `useRouter` que ficarem sem uso, remover a função `aoSair` e trocar o parágrafo de introdução para:

```tsx
<p className="mt-1 text-sm text-[#6B7280]">
  Comece cadastrando seus serviços e peças no catálogo — as ordens de
  serviço e o financeiro chegam nas próximas etapas.
</p>
```

(`sair` sai do destructuring de `useAuth()`; manter `usuario`.)

- [ ] **Step 3: Verificar**

```bash
cd frontend && npx tsc --noEmit && npm run lint && npm run build
```

Expected: tudo limpo. Depois `docker compose restart frontend` (bind mount do Windows não dispara hot-reload) e conferir http://localhost:3000/dashboard no navegador: nav com os três links, "Sair" funciona.

- [ ] **Step 4: Commit**

```bash
git add frontend/app
git commit -m "feat(frontend): navegacao compartilhada do painel do empreendedor

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 8: Front — página /pecas (peças + fornecedores)

**Files:**
- Create: `frontend/lib/formatadores.ts`
- Modify: `frontend/lib/validators/catalogo.ts` (criar; esquemas de peça e fornecedor — o de serviço entra na Task 9 no mesmo arquivo)
- Create: `frontend/app/(empreendedor)/pecas/page.tsx`

**Interfaces:**
- Consumes: hooks do orval (Task 6), `ApiError` de `@/lib/api-client/fetcher`, componentes `Button`/`Input`/`Label`, `toast` do sonner.
- Produces: `formatarBRL(valor: number): string`; `esquemaPeca`/`ValoresPeca`, `esquemaFornecedor` — reusados na Task 9.

- [ ] **Step 1: Criar `frontend/lib/formatadores.ts`:**

```ts
const brl = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

export function formatarBRL(valor: number): string {
  return brl.format(valor);
}
```

- [ ] **Step 2: Criar `frontend/lib/validators/catalogo.ts`:**

```ts
import { z } from "zod";

// Espelha as regras do back-end (Modules/ServicosEPecas/Validadores.cs):
// mesma régua nos dois lados, mensagens pt-BR.
export const esquemaFornecedor = z.object({
  nome: z
    .string()
    .min(1, "Informe o nome do fornecedor.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
  contato: z.string().max(200, "O contato pode ter no máximo 200 caracteres."),
});

export type ValoresFornecedor = z.infer<typeof esquemaFornecedor>;

export const esquemaPeca = z.object({
  nome: z
    .string()
    .min(1, "Informe o nome da peça.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
  descricao: z.string().max(500, "A descrição pode ter no máximo 500 caracteres."),
  custoUnitario: z
    .number({ message: "Informe o custo unitário." })
    .min(0, "O custo unitário não pode ser negativo."),
  precoVenda: z
    .number({ message: "Informe o preço de venda." })
    .min(0, "O preço de venda não pode ser negativo."),
  quantidadeEmEstoque: z
    .number({ message: "Informe a quantidade." })
    .int("Use um número inteiro.")
    .min(0, "A quantidade não pode ser negativa."),
  estoqueMinimo: z
    .number({ message: "Informe o estoque mínimo." })
    .int("Use um número inteiro.")
    .min(0, "O estoque mínimo não pode ser negativo."),
  fornecedorId: z.string(),
});

export type ValoresPeca = z.infer<typeof esquemaPeca>;
```

(`fornecedorId` fica string no form — `<select>` devolve string; `""` = sem fornecedor — e é convertido no submit.)

- [ ] **Step 3: Criar `frontend/app/(empreendedor)/pecas/page.tsx`:**

```tsx
"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useDeleteApiPecasId,
  useGetApiFornecedores,
  useGetApiPecas,
  usePostApiFornecedores,
  usePostApiPecas,
  usePutApiPecasId,
  type PecaResponse,
} from "@/lib/api-client/gerado";
import { formatarBRL } from "@/lib/formatadores";
import { esquemaPeca, type ValoresPeca } from "@/lib/validators/catalogo";

const VALORES_INICIAIS: ValoresPeca = {
  nome: "",
  descricao: "",
  custoUnitario: 0,
  precoVenda: 0,
  quantidadeEmEstoque: 0,
  estoqueMinimo: 0,
  fornecedorId: "",
};

export default function PaginaPecas() {
  const queryClient = useQueryClient();
  const [formAberto, setFormAberto] = useState(false);
  const [editandoId, setEditandoId] = useState<number | null>(null);
  const [mostrarInativas, setMostrarInativas] = useState(false);
  const [novoFornecedor, setNovoFornecedor] = useState("");

  const { data: respostaPecas } = useGetApiPecas({
    incluirInativas: mostrarInativas || undefined,
  });
  const pecas = respostaPecas?.status === 200 ? respostaPecas.data : undefined;

  const { data: respostaFornecedores } = useGetApiFornecedores();
  const fornecedores =
    respostaFornecedores?.status === 200 ? respostaFornecedores.data : [];

  const criarPeca = usePostApiPecas();
  const atualizarPeca = usePutApiPecasId();
  const desativarPeca = useDeleteApiPecasId();
  const criarFornecedor = usePostApiFornecedores();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ValoresPeca>({
    resolver: zodResolver(esquemaPeca),
    defaultValues: VALORES_INICIAIS,
  });

  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ["/api/pecas"] });
    queryClient.invalidateQueries({ queryKey: ["/api/fornecedores"] });
  }

  function abrirCriacao() {
    setEditandoId(null);
    reset(VALORES_INICIAIS);
    setFormAberto(true);
  }

  function abrirEdicao(peca: PecaResponse) {
    setEditandoId(peca.id ?? null);
    reset({
      nome: peca.nome ?? "",
      descricao: peca.descricao ?? "",
      custoUnitario: peca.custoUnitario ?? 0,
      precoVenda: peca.precoVenda ?? 0,
      quantidadeEmEstoque: peca.quantidadeEmEstoque ?? 0,
      estoqueMinimo: peca.estoqueMinimo ?? 0,
      fornecedorId: peca.fornecedor?.id ? String(peca.fornecedor.id) : "",
    });
    setFormAberto(true);
  }

  async function aoSalvar(valores: ValoresPeca) {
    const corpo = {
      nome: valores.nome,
      descricao: valores.descricao || null,
      custoUnitario: valores.custoUnitario,
      precoVenda: valores.precoVenda,
      quantidadeEmEstoque: valores.quantidadeEmEstoque,
      estoqueMinimo: valores.estoqueMinimo,
      fornecedorId: valores.fornecedorId ? Number(valores.fornecedorId) : null,
      ativo: true,
    };
    try {
      if (editandoId === null) {
        await criarPeca.mutateAsync({ data: corpo });
        toast.success("Peça cadastrada.");
      } else {
        await atualizarPeca.mutateAsync({ id: editandoId, data: corpo });
        toast.success("Peça atualizada.");
      }
      invalidar();
      setFormAberto(false);
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar a peça.");
    }
  }

  async function aoDesativar(id: number | undefined) {
    if (id === undefined) return;
    try {
      await desativarPeca.mutateAsync({ id });
      toast.success("Peça desativada.");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao desativar a peça.");
    }
  }

  async function aoCriarFornecedor() {
    const nome = novoFornecedor.trim();
    if (!nome) return;
    try {
      await criarFornecedor.mutateAsync({ data: { nome, contato: null } });
      toast.success("Fornecedor cadastrado.");
      setNovoFornecedor("");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao cadastrar o fornecedor.");
    }
  }

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <div className="flex items-end justify-between">
        <div>
          <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
            Catálogo
          </p>
          <h1 className="mt-2 text-3xl font-bold text-[#14162B]">Peças</h1>
          <p className="mt-1 text-sm text-[#6B7280]">
            Cadastre as peças que a sua assistência usa nos reparos — custo,
            preço de venda, estoque e fornecedor.
          </p>
        </div>
        <Button
          onClick={abrirCriacao}
          className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
        >
          Nova peça
        </Button>
      </div>

      {formAberto && (
        <form
          onSubmit={handleSubmit(aoSalvar)}
          className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6"
        >
          <h2 className="text-lg font-semibold text-[#14162B]">
            {editandoId === null ? "Nova peça" : "Editar peça"}
          </h2>

          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <Label htmlFor="nome">Nome</Label>
              <Input id="nome" className="mt-1 h-11" aria-invalid={!!errors.nome} {...register("nome")} />
              {errors.nome && <p className="mt-1 text-sm text-destructive">{errors.nome.message}</p>}
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="descricao">Descrição (opcional)</Label>
              <Input id="descricao" className="mt-1 h-11" {...register("descricao")} />
            </div>
            <div>
              <Label htmlFor="custoUnitario">Custo unitário (R$)</Label>
              <Input
                id="custoUnitario" type="number" step="0.01" min="0" className="mt-1 h-11"
                aria-invalid={!!errors.custoUnitario}
                {...register("custoUnitario", { valueAsNumber: true })}
              />
              {errors.custoUnitario && (
                <p className="mt-1 text-sm text-destructive">{errors.custoUnitario.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="precoVenda">Preço de venda (R$)</Label>
              <Input
                id="precoVenda" type="number" step="0.01" min="0" className="mt-1 h-11"
                aria-invalid={!!errors.precoVenda}
                {...register("precoVenda", { valueAsNumber: true })}
              />
              {errors.precoVenda && (
                <p className="mt-1 text-sm text-destructive">{errors.precoVenda.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="quantidadeEmEstoque">Quantidade em estoque</Label>
              <Input
                id="quantidadeEmEstoque" type="number" min="0" className="mt-1 h-11"
                aria-invalid={!!errors.quantidadeEmEstoque}
                {...register("quantidadeEmEstoque", { valueAsNumber: true })}
              />
              {errors.quantidadeEmEstoque && (
                <p className="mt-1 text-sm text-destructive">{errors.quantidadeEmEstoque.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="estoqueMinimo">Estoque mínimo</Label>
              <Input
                id="estoqueMinimo" type="number" min="0" className="mt-1 h-11"
                aria-invalid={!!errors.estoqueMinimo}
                {...register("estoqueMinimo", { valueAsNumber: true })}
              />
              {errors.estoqueMinimo && (
                <p className="mt-1 text-sm text-destructive">{errors.estoqueMinimo.message}</p>
              )}
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="fornecedorId">Fornecedor (opcional)</Label>
              <select
                id="fornecedorId"
                className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                {...register("fornecedorId")}
              >
                <option value="">Sem fornecedor</option>
                {fornecedores.map((f) => (
                  <option key={f.id} value={f.id}>
                    {f.nome}
                  </option>
                ))}
              </select>
              <div className="mt-2 flex gap-2">
                <Input
                  placeholder="Cadastrar novo fornecedor..."
                  value={novoFornecedor}
                  onChange={(e) => setNovoFornecedor(e.target.value)}
                  className="h-9"
                />
                <Button
                  type="button"
                  variant="outline"
                  className="h-9"
                  onClick={aoCriarFornecedor}
                  disabled={!novoFornecedor.trim() || criarFornecedor.isPending}
                >
                  Adicionar
                </Button>
              </div>
            </div>
          </div>

          <div className="mt-6 flex gap-3">
            <Button
              type="submit"
              disabled={isSubmitting}
              className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
            >
              {isSubmitting ? "Salvando..." : "Salvar peça"}
            </Button>
            <Button type="button" variant="ghost" onClick={() => setFormAberto(false)}>
              Cancelar
            </Button>
          </div>
        </form>
      )}

      <div className="mt-8 flex items-center justify-between">
        <p className="text-sm text-[#6B7280]">
          {pecas ? `${pecas.total} peça(s)` : "Carregando..."}
        </p>
        <label className="flex items-center gap-2 text-sm text-[#6B7280]">
          <input
            type="checkbox"
            checked={mostrarInativas}
            onChange={(e) => setMostrarInativas(e.target.checked)}
          />
          Mostrar desativadas
        </label>
      </div>

      <div className="mt-3 overflow-x-auto rounded-2xl border border-[#14162B]/8">
        <table className="w-full text-left text-sm">
          <thead className="bg-[#F7F7F9] text-xs text-[#8B8D98] uppercase tracking-wide">
            <tr>
              <th className="px-4 py-3">Peça</th>
              <th className="px-4 py-3">Custo</th>
              <th className="px-4 py-3">Venda</th>
              <th className="px-4 py-3">Estoque</th>
              <th className="px-4 py-3">Fornecedor</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {(pecas?.itens ?? []).map((peca) => (
              <tr key={peca.id} className="border-t border-[#14162B]/6">
                <td className="px-4 py-3 font-medium text-[#14162B]">
                  {peca.nome}
                  {!peca.ativo && (
                    <span className="ml-2 rounded-full bg-[#F7F7F9] px-2 py-0.5 text-xs text-[#8B8D98]">
                      desativada
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 text-[#6B7280]">{formatarBRL(peca.custoUnitario ?? 0)}</td>
                <td className="px-4 py-3 text-[#6B7280]">{formatarBRL(peca.precoVenda ?? 0)}</td>
                <td className="px-4 py-3">
                  <span className={peca.estoqueBaixo ? "font-semibold text-[#E8536B]" : "text-[#6B7280]"}>
                    {peca.quantidadeEmEstoque}
                    {peca.estoqueBaixo && " · baixo"}
                  </span>
                </td>
                <td className="px-4 py-3 text-[#6B7280]">{peca.fornecedor?.nome ?? "—"}</td>
                <td className="px-4 py-3 text-right whitespace-nowrap">
                  <Button variant="ghost" className="h-8 px-3" onClick={() => abrirEdicao(peca)}>
                    Editar
                  </Button>
                  {peca.ativo && (
                    <Button
                      variant="ghost"
                      className="h-8 px-3 text-[#E8536B] hover:text-[#E8536B]"
                      onClick={() => aoDesativar(peca.id)}
                    >
                      Desativar
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {pecas && pecas.itens?.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-[#6B7280]">
                  Nenhuma peça cadastrada ainda. Clique em “Nova peça” para começar.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Verificar**

```bash
cd frontend && npx tsc --noEmit && npm run lint && npm run build
```

Expected: limpo (nomes gerados pelo orval podem divergir — ajustar imports conforme o `gerado/index.ts` real). Depois `docker compose restart frontend` e testar manualmente em http://localhost:3000/pecas: criar fornecedor, criar peça, editar, desativar, "mostrar desativadas".

- [ ] **Step 5: Commit**

```bash
git add frontend
git commit -m "feat(frontend): tela de pecas com fornecedores e alerta de estoque baixo

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 9: Front — página /servicos

**Files:**
- Modify: `frontend/lib/validators/catalogo.ts` (adicionar `esquemaServico`)
- Create: `frontend/app/(empreendedor)/servicos/page.tsx`

**Interfaces:**
- Consumes: hooks do orval (Task 6), `esquemaPeca` NÃO — usa `esquemaServico`; `formatarBRL` (Task 8).

- [ ] **Step 1: Adicionar a `frontend/lib/validators/catalogo.ts`:**

```ts
export const esquemaServico = z.object({
  nome: z
    .string()
    .min(1, "Informe o nome do serviço.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
  categoria: z.string().max(100, "A categoria pode ter no máximo 100 caracteres."),
  precoBase: z
    .number({ message: "Informe o preço base." })
    .min(0, "O preço base não pode ser negativo."),
  duracaoEstimadaMinutos: z
    .number({ message: "Informe a duração estimada." })
    .int("Use um número inteiro.")
    .min(1, "A duração deve ser de pelo menos 1 minuto."),
  prazoMedioDias: z
    .number()
    .int("Use um número inteiro.")
    .min(1, "O prazo médio deve ser de pelo menos 1 dia.")
    .optional(),
  exigeDiagnostico: z.boolean(),
  agendavelOnline: z.boolean(),
  capacidadeSimultanea: z
    .number({ message: "Informe a capacidade." })
    .int("Use um número inteiro.")
    .min(1, "A capacidade deve ser de pelo menos 1."),
  checklist: z.array(
    z.object({
      descricao: z
        .string()
        .min(1, "Item do checklist não pode ser vazio.")
        .max(300, "Item do checklist pode ter no máximo 300 caracteres."),
    }),
  ),
  pecas: z.array(
    z.object({
      pecaId: z.string().min(1, "Escolha a peça."),
      quantidadePadrao: z
        .number({ message: "Informe a quantidade." })
        .int("Use um número inteiro.")
        .min(1, "A quantidade deve ser de pelo menos 1."),
    }),
  ),
});

export type ValoresServico = z.infer<typeof esquemaServico>;
```

- [ ] **Step 2: Criar `frontend/app/(empreendedor)/servicos/page.tsx`** — mesmo esqueleto da página de peças (header da seção com tag rosa "Catálogo", botão pílula "Novo serviço", form em card, tabela, checkbox "mostrar desativados"), com as diferenças:

```tsx
"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useDeleteApiServicosId,
  useGetApiPecas,
  useGetApiServicos,
  usePostApiServicos,
  usePutApiServicosId,
  type ServicoResponse,
} from "@/lib/api-client/gerado";
import { formatarBRL } from "@/lib/formatadores";
import { esquemaServico, type ValoresServico } from "@/lib/validators/catalogo";

const SUGESTOES_CATEGORIA = [
  "Tela",
  "Bateria",
  "Conector de carga",
  "Placa",
  "Câmera",
  "Limpeza",
  "Software",
  "Película",
];

const VALORES_INICIAIS: ValoresServico = {
  nome: "",
  categoria: "",
  precoBase: 0,
  duracaoEstimadaMinutos: 60,
  prazoMedioDias: undefined,
  exigeDiagnostico: false,
  agendavelOnline: true,
  capacidadeSimultanea: 1,
  checklist: [],
  pecas: [],
};

export default function PaginaServicos() {
  const queryClient = useQueryClient();
  const [formAberto, setFormAberto] = useState(false);
  const [editandoId, setEditandoId] = useState<number | null>(null);
  const [mostrarInativos, setMostrarInativos] = useState(false);

  const { data: respostaServicos } = useGetApiServicos({
    incluirInativos: mostrarInativos || undefined,
  });
  const servicos = respostaServicos?.status === 200 ? respostaServicos.data : undefined;

  // Peças ativas para o vínculo "peças normalmente utilizadas".
  const { data: respostaPecas } = useGetApiPecas({ tamanhoPagina: 100 });
  const pecasDisponiveis = respostaPecas?.status === 200 ? (respostaPecas.data.itens ?? []) : [];

  const criarServico = usePostApiServicos();
  const atualizarServico = usePutApiServicosId();
  const desativarServico = useDeleteApiServicosId();

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ValoresServico>({
    resolver: zodResolver(esquemaServico),
    defaultValues: VALORES_INICIAIS,
  });
  const checklist = useFieldArray({ control, name: "checklist" });
  const pecasForm = useFieldArray({ control, name: "pecas" });

  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ["/api/servicos"] });
  }

  function abrirCriacao() {
    setEditandoId(null);
    reset(VALORES_INICIAIS);
    setFormAberto(true);
  }

  function abrirEdicao(servico: ServicoResponse) {
    setEditandoId(servico.id ?? null);
    reset({
      nome: servico.nome ?? "",
      categoria: servico.categoria ?? "",
      precoBase: servico.precoBase ?? 0,
      duracaoEstimadaMinutos: servico.duracaoEstimadaMinutos ?? 60,
      prazoMedioDias: servico.prazoMedioDias ?? undefined,
      exigeDiagnostico: servico.exigeDiagnostico ?? false,
      agendavelOnline: servico.agendavelOnline ?? true,
      capacidadeSimultanea: servico.capacidadeSimultanea ?? 1,
      checklist: (servico.checklist ?? []).map((descricao) => ({ descricao })),
      pecas: (servico.pecas ?? []).map((p) => ({
        pecaId: String(p.pecaId),
        quantidadePadrao: p.quantidadePadrao ?? 1,
      })),
    });
    setFormAberto(true);
  }

  async function aoSalvar(valores: ValoresServico) {
    const corpo = {
      nome: valores.nome,
      categoria: valores.categoria || null,
      precoBase: valores.precoBase,
      duracaoEstimadaMinutos: valores.duracaoEstimadaMinutos,
      prazoMedioDias: valores.prazoMedioDias ?? null,
      exigeDiagnostico: valores.exigeDiagnostico,
      agendavelOnline: valores.agendavelOnline,
      capacidadeSimultanea: valores.capacidadeSimultanea,
      ativo: true,
      checklist: valores.checklist.map((item) => item.descricao),
      pecas: valores.pecas.map((p) => ({
        pecaId: Number(p.pecaId),
        quantidadePadrao: p.quantidadePadrao,
      })),
    };
    try {
      if (editandoId === null) {
        await criarServico.mutateAsync({ data: corpo });
        toast.success("Serviço cadastrado.");
      } else {
        await atualizarServico.mutateAsync({ id: editandoId, data: corpo });
        toast.success("Serviço atualizado.");
      }
      invalidar();
      setFormAberto(false);
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar o serviço.");
    }
  }

  async function aoDesativar(id: number | undefined) {
    if (id === undefined) return;
    try {
      await desativarServico.mutateAsync({ id });
      toast.success("Serviço desativado.");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao desativar o serviço.");
    }
  }

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <div className="flex items-end justify-between">
        <div>
          <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
            Catálogo
          </p>
          <h1 className="mt-2 text-3xl font-bold text-[#14162B]">Serviços</h1>
          <p className="mt-1 text-sm text-[#6B7280]">
            Os serviços que a sua assistência oferece — preço, duração,
            checklist padrão e peças normalmente utilizadas.
          </p>
        </div>
        <Button
          onClick={abrirCriacao}
          className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
        >
          Novo serviço
        </Button>
      </div>

      {formAberto && (
        <form
          onSubmit={handleSubmit(aoSalvar)}
          className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6"
        >
          <h2 className="text-lg font-semibold text-[#14162B]">
            {editandoId === null ? "Novo serviço" : "Editar serviço"}
          </h2>

          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <div>
              <Label htmlFor="nome">Nome</Label>
              <Input id="nome" className="mt-1 h-11" aria-invalid={!!errors.nome} {...register("nome")} />
              {errors.nome && <p className="mt-1 text-sm text-destructive">{errors.nome.message}</p>}
            </div>
            <div>
              <Label htmlFor="categoria">Categoria (opcional)</Label>
              <Input
                id="categoria" list="sugestoes-categoria" className="mt-1 h-11"
                {...register("categoria")}
              />
              <datalist id="sugestoes-categoria">
                {SUGESTOES_CATEGORIA.map((c) => (
                  <option key={c} value={c} />
                ))}
              </datalist>
            </div>
            <div>
              <Label htmlFor="precoBase">Preço base (R$)</Label>
              <Input
                id="precoBase" type="number" step="0.01" min="0" className="mt-1 h-11"
                aria-invalid={!!errors.precoBase}
                {...register("precoBase", { valueAsNumber: true })}
              />
              {errors.precoBase && (
                <p className="mt-1 text-sm text-destructive">{errors.precoBase.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="duracaoEstimadaMinutos">Duração estimada (min)</Label>
              <Input
                id="duracaoEstimadaMinutos" type="number" min="1" className="mt-1 h-11"
                aria-invalid={!!errors.duracaoEstimadaMinutos}
                {...register("duracaoEstimadaMinutos", { valueAsNumber: true })}
              />
              {errors.duracaoEstimadaMinutos && (
                <p className="mt-1 text-sm text-destructive">{errors.duracaoEstimadaMinutos.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="prazoMedioDias">Prazo médio (dias, opcional)</Label>
              <Input
                id="prazoMedioDias" type="number" min="1" className="mt-1 h-11"
                {...register("prazoMedioDias", {
                  setValueAs: (v) => (v === "" || v === null ? undefined : Number(v)),
                })}
              />
              {errors.prazoMedioDias && (
                <p className="mt-1 text-sm text-destructive">{errors.prazoMedioDias.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="capacidadeSimultanea">Capacidade simultânea</Label>
              <Input
                id="capacidadeSimultanea" type="number" min="1" className="mt-1 h-11"
                aria-invalid={!!errors.capacidadeSimultanea}
                {...register("capacidadeSimultanea", { valueAsNumber: true })}
              />
              {errors.capacidadeSimultanea && (
                <p className="mt-1 text-sm text-destructive">{errors.capacidadeSimultanea.message}</p>
              )}
              <p className="mt-1 text-xs text-[#8B8D98]">
                Quantos atendimentos deste serviço a agenda aceita ao mesmo tempo.
              </p>
            </div>
            <label className="flex items-center gap-2 text-sm text-[#14162B]">
              <input type="checkbox" {...register("exigeDiagnostico")} />
              Exige diagnóstico antes do orçamento
            </label>
            <label className="flex items-center gap-2 text-sm text-[#14162B]">
              <input type="checkbox" {...register("agendavelOnline")} />
              Disponível para agendamento online
            </label>
          </div>

          <fieldset className="mt-6">
            <legend className="text-sm font-semibold text-[#14162B]">Checklist padrão</legend>
            {checklist.fields.map((campo, indice) => (
              <div key={campo.id} className="mt-2 flex gap-2">
                <Input
                  className="h-10"
                  placeholder={`Item ${indice + 1}`}
                  aria-invalid={!!errors.checklist?.[indice]?.descricao}
                  {...register(`checklist.${indice}.descricao`)}
                />
                <Button type="button" variant="ghost" onClick={() => checklist.remove(indice)}>
                  Remover
                </Button>
              </div>
            ))}
            {errors.checklist && (
              <p className="mt-1 text-sm text-destructive">Revise os itens do checklist.</p>
            )}
            <Button
              type="button" variant="outline" className="mt-2 h-9"
              onClick={() => checklist.append({ descricao: "" })}
            >
              Adicionar item
            </Button>
          </fieldset>

          <fieldset className="mt-6">
            <legend className="text-sm font-semibold text-[#14162B]">
              Peças normalmente utilizadas
            </legend>
            {pecasForm.fields.map((campo, indice) => (
              <div key={campo.id} className="mt-2 flex gap-2">
                <select
                  className="h-10 w-full rounded-md border border-input bg-white px-3 text-sm"
                  aria-invalid={!!errors.pecas?.[indice]?.pecaId}
                  {...register(`pecas.${indice}.pecaId`)}
                >
                  <option value="">Escolha a peça...</option>
                  {pecasDisponiveis.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nome}
                    </option>
                  ))}
                </select>
                <Input
                  type="number" min="1" className="h-10 w-24" title="Quantidade"
                  {...register(`pecas.${indice}.quantidadePadrao`, { valueAsNumber: true })}
                />
                <Button type="button" variant="ghost" onClick={() => pecasForm.remove(indice)}>
                  Remover
                </Button>
              </div>
            ))}
            {errors.pecas && (
              <p className="mt-1 text-sm text-destructive">Revise as peças vinculadas.</p>
            )}
            <Button
              type="button" variant="outline" className="mt-2 h-9"
              onClick={() => pecasForm.append({ pecaId: "", quantidadePadrao: 1 })}
            >
              Vincular peça
            </Button>
          </fieldset>

          <div className="mt-6 flex gap-3">
            <Button
              type="submit"
              disabled={isSubmitting}
              className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
            >
              {isSubmitting ? "Salvando..." : "Salvar serviço"}
            </Button>
            <Button type="button" variant="ghost" onClick={() => setFormAberto(false)}>
              Cancelar
            </Button>
          </div>
        </form>
      )}

      <div className="mt-8 flex items-center justify-between">
        <p className="text-sm text-[#6B7280]">
          {servicos ? `${servicos.total} serviço(s)` : "Carregando..."}
        </p>
        <label className="flex items-center gap-2 text-sm text-[#6B7280]">
          <input
            type="checkbox"
            checked={mostrarInativos}
            onChange={(e) => setMostrarInativos(e.target.checked)}
          />
          Mostrar desativados
        </label>
      </div>

      <div className="mt-3 overflow-x-auto rounded-2xl border border-[#14162B]/8">
        <table className="w-full text-left text-sm">
          <thead className="bg-[#F7F7F9] text-xs text-[#8B8D98] uppercase tracking-wide">
            <tr>
              <th className="px-4 py-3">Serviço</th>
              <th className="px-4 py-3">Categoria</th>
              <th className="px-4 py-3">Preço base</th>
              <th className="px-4 py-3">Duração</th>
              <th className="px-4 py-3">Online</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {(servicos?.itens ?? []).map((servico) => (
              <tr key={servico.id} className="border-t border-[#14162B]/6">
                <td className="px-4 py-3 font-medium text-[#14162B]">
                  {servico.nome}
                  {!servico.ativo && (
                    <span className="ml-2 rounded-full bg-[#F7F7F9] px-2 py-0.5 text-xs text-[#8B8D98]">
                      desativado
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 text-[#6B7280]">{servico.categoria ?? "—"}</td>
                <td className="px-4 py-3 text-[#6B7280]">{formatarBRL(servico.precoBase ?? 0)}</td>
                <td className="px-4 py-3 text-[#6B7280]">{servico.duracaoEstimadaMinutos} min</td>
                <td className="px-4 py-3 text-[#6B7280]">{servico.agendavelOnline ? "Sim" : "Não"}</td>
                <td className="px-4 py-3 text-right whitespace-nowrap">
                  <Button variant="ghost" className="h-8 px-3" onClick={() => abrirEdicao(servico)}>
                    Editar
                  </Button>
                  {servico.ativo && (
                    <Button
                      variant="ghost"
                      className="h-8 px-3 text-[#E8536B] hover:text-[#E8536B]"
                      onClick={() => aoDesativar(servico.id)}
                    >
                      Desativar
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {servicos && servicos.itens?.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-[#6B7280]">
                  Nenhum serviço cadastrado ainda. Clique em “Novo serviço” para começar.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
```

- [ ] **Step 3: Verificar**

```bash
cd frontend && npx tsc --noEmit && npm run lint && npm run build
```

Expected: limpo. `docker compose restart frontend` e testar http://localhost:3000/servicos manualmente: criar serviço com checklist de 2 itens e 1 peça vinculada, editar, desativar.

- [ ] **Step 4: Commit**

```bash
git add frontend
git commit -m "feat(frontend): tela de servicos com checklist e pecas vinculadas

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 10: Verificação e2e + documentação

**Files:**
- Create (scratchpad, fora do repo): `<scratchpad>/e2e/fluxo-catalogo.mjs`
- Modify: `docs/progresso.md`

- [ ] **Step 1: Script e2e no scratchpad** (playwright com `channel: "msedge"`, mesmo padrão de `fluxo-auth.mjs`): registrar conta nova → `/pecas`: cadastrar fornecedor "ImportaCel" e peça "Tela A54" → `/servicos`: cadastrar "Troca de tela" com 2 itens de checklist e a peça vinculada → asserts: linhas nas tabelas, badge de estoque baixo ausente, toasts de sucesso; screenshots de cada tela; imprimir JSON de evidência.

- [ ] **Step 2: Rodar o e2e.** Expected: JSON com todos os passos `true` + screenshots.

- [ ] **Step 3: Atualizar `docs/progresso.md`:**
  - Status geral: nova subseção "Etapa Catálogo concluída em 2026-07-08" com o JSON de evidência e a nova contagem de testes (27).
  - "O que está construído": nova subseção **Catálogo (módulo 6)** — serviços com checklist/capacidade/peças vinculadas, peças com fornecedor/estoque mínimo/alerta, fornecedores; RLS + GQF verificados nas 5 tabelas novas; desativação em vez de exclusão.
  - "Desvios e notas conscientes": registrar "sem camada Repository (DbContext é o repositório; introduzir apenas quando houver query complexa reutilizada)" e "kits de serviço e peça compatível ficam para a Fase 2 (fases_MVP.md)".
  - "Próximos passos": próximo da ordem recomendada = **Clientes e aparelhos**.

- [ ] **Step 4: Commit**

```bash
git add docs/progresso.md
git commit -m "docs: progresso da etapa catalogo com evidencias e2e

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```
