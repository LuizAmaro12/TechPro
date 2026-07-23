using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.OrdensServico;

/// <summary>
/// Item do checklist técnico de uma OS — o que o técnico confere na bancada
/// (módulo 4). Copiado do template do serviço na abertura da OS, com a
/// descrição em **snapshot**: o serviço pode editar o checklist depois, mas a
/// OS mantém o que tinha quando foi aberta.
///
/// No escopo offline (UUID + colunas de sync) porque é trabalho de campo — o
/// doc de stack manda tratar entidades do fluxo do técnico assim desde o
/// primeiro migration, não como retrofit.
/// </summary>
public class ItemChecklistOrdemServico : ITenantEntity, IEntidadeSincronizavel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid OrdemServicoId { get; set; }

    /// <summary>Ordem de exibição, herdada do template.</summary>
    public int Ordem { get; set; }

    public required string Descricao { get; set; }

    public bool Concluido { get; set; }
    public DateTimeOffset? ConcluidoEm { get; set; }
    public Guid? ConcluidoPorUsuarioId { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
