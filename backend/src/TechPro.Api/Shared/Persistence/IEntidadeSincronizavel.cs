namespace TechPro.Api.Shared.Persistence;

/// <summary>
/// Entidade do escopo offline do técnico (seções 4 e 5 do doc de stack):
/// PK UUID (gerável no cliente na Fase 2), <c>UpdatedAt</c> carimbado pelo
/// servidor em toda escrita (marca d'água de sincronização — o DbContext faz
/// isso automaticamente no SaveChanges) e <c>DeletedAt</c> como lápide de
/// soft-delete que o app offline consegue sincronizar.
/// </summary>
public interface IEntidadeSincronizavel
{
    Guid Id { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
