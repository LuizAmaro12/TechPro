using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Agendamentos;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Auth;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Shared.Persistence;

public class TechProDbContext(DbContextOptions options, ITenantProvider tenantProvider)
    : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(options)
{
    private readonly ITenantProvider _tenantProvider = tenantProvider;

    /// <summary>
    /// Avaliado a cada query (o EF Core parametriza membros do próprio contexto
    /// nos query filters), então o filtro sempre reflete o tenant da requisição.
    /// </summary>
    public Guid? TenantIdAtual => _tenantProvider.TenantId;

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Peca> Pecas => Set<Peca>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Aparelho> Aparelhos => Set<Aparelho>();
    public DbSet<HorarioFuncionamento> HorariosFuncionamento => Set<HorarioFuncionamento>();
    public DbSet<BloqueioAgenda> BloqueiosAgenda => Set<BloqueioAgenda>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Empresa>(e =>
        {
            e.ToTable("empresas");
            e.Property(x => x.Nome).HasMaxLength(200);
            e.Property(x => x.Slug).HasMaxLength(80);
            e.HasIndex(x => x.Slug).IsUnique();
            // A Empresa é a raiz do tenant: o filtro usa o próprio Id.
            // Fail-closed: sem tenant no contexto, nenhuma empresa é visível.
            e.HasQueryFilter(x => x.Id == TenantIdAtual);
        });

        builder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.Property(x => x.Nome).HasMaxLength(200);
            e.HasIndex(x => x.TenantId);
        });

        // O IdentityDbContext fixa nomes AspNet* explicitamente, o que escapa
        // da convenção snake_case — renomeamos para manter o schema coerente.
        builder.Entity<IdentityRole<Guid>>().ToTable("papeis");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("usuario_papeis");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("usuario_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("usuario_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("usuario_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("papel_claims");

        builder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.Property(x => x.TokenHash).HasMaxLength(128);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UsuarioId);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        // Papéis fixos do produto (módulo 13), seedados com IDs estáveis para
        // que a migration seja determinística.
        builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("a1b0c579-4f6e-4a56-9d10-2f6a1e01a001"),
                Name = Papeis.Gestor,
                NormalizedName = "GESTOR",
                ConcurrencyStamp = "seed-gestor",
            },
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("a1b0c579-4f6e-4a56-9d10-2f6a1e01a002"),
                Name = Papeis.Tecnico,
                NormalizedName = "TECNICO",
                ConcurrencyStamp = "seed-tecnico",
            },
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("a1b0c579-4f6e-4a56-9d10-2f6a1e01a003"),
                Name = Papeis.Atendente,
                NormalizedName = "ATENDENTE",
                ConcurrencyStamp = "seed-atendente",
            });

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

        // --- Clientes (módulo 5): CRM básico + aparelhos -------------------------

        builder.Entity<Cliente>(e =>
        {
            e.ToTable("clientes");
            e.Property(x => x.Nome).HasMaxLength(200);
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Cpf).HasMaxLength(14);
            e.Property(x => x.Endereco).HasMaxLength(300);
            e.Property(x => x.Observacoes).HasMaxLength(1000);
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.Nome });
            // Restrict: um principal com vinculados não pode ser removido por engano.
            e.HasOne(x => x.ClientePrincipal).WithMany().HasForeignKey(x => x.ClientePrincipalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Aparelho>(e =>
        {
            e.ToTable("aparelhos");
            e.Property(x => x.Marca).HasMaxLength(100);
            e.Property(x => x.Modelo).HasMaxLength(150);
            e.Property(x => x.Imei).HasMaxLength(50);
            e.Property(x => x.SenhaDesbloqueio).HasMaxLength(100);
            e.Property(x => x.Observacoes).HasMaxLength(500);
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.ClienteId);
            e.HasOne<Cliente>().WithMany(c => c.Aparelhos).HasForeignKey(x => x.ClienteId);
        });

        // --- Agenda (módulo 2): horários, bloqueios e agendamentos ---------------

        builder.Entity<HorarioFuncionamento>(e =>
        {
            e.ToTable("horarios_funcionamento");
            e.HasIndex(x => new { x.TenantId, x.DiaSemana }).IsUnique();
        });

        builder.Entity<BloqueioAgenda>(e =>
        {
            e.ToTable("bloqueios_agenda");
            e.Property(x => x.Motivo).HasMaxLength(200);
            e.HasIndex(x => new { x.TenantId, x.Data });
        });

        builder.Entity<Agendamento>(e =>
        {
            e.ToTable("agendamentos");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Origem).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.NomeContato).HasMaxLength(200);
            e.Property(x => x.TelefoneContato).HasMaxLength(20);
            e.Property(x => x.EmailContato).HasMaxLength(256);
            e.Property(x => x.DescricaoProblema).HasMaxLength(1000);
            e.Property(x => x.AparelhoMarca).HasMaxLength(100);
            e.Property(x => x.AparelhoModelo).HasMaxLength(150);
            e.Property(x => x.MotivoCancelamento).HasMaxLength(500);
            e.HasIndex(x => new { x.TenantId, x.Data });
            e.HasIndex(x => x.ClienteId);
            // Restrict: histórico de agenda não some junto com cliente/serviço
            // (ambos usam desativação, nunca exclusão física — defesa extra).
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Servico).WithMany().HasForeignKey(x => x.ServicoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        AplicarFiltroDeTenantPorConvencao(builder);
    }

    /// <summary>
    /// Convenção central de isolamento (seção 5 do doc de stack): toda entidade
    /// ITenantEntity recebe Global Query Filter por tenant_id automaticamente.
    /// Esquecer um .Where() deixa de ser um vetor de vazamento entre empresas.
    /// </summary>
    private void AplicarFiltroDeTenantPorConvencao(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parametro = Expression.Parameter(entityType.ClrType, "e");
            var corpo = Expression.Equal(
                Expression.Convert(
                    Expression.Property(parametro, nameof(ITenantEntity.TenantId)),
                    typeof(Guid?)),
                Expression.Property(Expression.Constant(this), nameof(TenantIdAtual)));

            builder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(corpo, parametro));
        }
    }
}
