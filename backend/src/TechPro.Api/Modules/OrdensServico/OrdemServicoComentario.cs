using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// Comentário interno da OS — visível só para a loja, nunca para o cliente
/// final. No escopo offline (UUID + colunas de sync) porque o técnico comenta
/// em campo: o doc de stack manda tratar entidades do fluxo de campo assim
/// desde o primeiro migration, não como retrofit.
/// </summary>
public class OrdemServicoComentario : ITenantEntity, IEntidadeSincronizavel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid OrdemServicoId { get; set; }

    public required string Texto { get; set; }

    /// <summary>Quem escreveu (claim sub); nulo em comentário do sistema.</summary>
    public Guid? AutorUsuarioId { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
