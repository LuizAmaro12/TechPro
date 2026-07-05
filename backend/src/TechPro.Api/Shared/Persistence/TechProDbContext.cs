using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Empresa>(e =>
        {
            e.ToTable("empresas");
            e.Property(x => x.Nome).HasMaxLength(200);
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
