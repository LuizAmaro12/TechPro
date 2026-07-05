namespace TechPro.Api.Shared.Tenancy;

/// <summary>
/// A raiz do tenant: cada assistência técnica cliente da TechPro é uma Empresa.
/// PK em UUID por decisão aprovada (não enumerável; viaja em JWT, URLs e RLS).
/// </summary>
public class Empresa
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}
