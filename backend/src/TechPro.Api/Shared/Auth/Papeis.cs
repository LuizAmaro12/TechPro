namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Papéis do módulo 13 do documento de produto. O primeiro usuário de uma
/// empresa nasce gestor (decisão aprovada em 2026-07-05); permissões
/// granulares por papel entram na Fase 2.
/// </summary>
public static class Papeis
{
    public const string Gestor = "gestor";
    public const string Tecnico = "tecnico";
    public const string Atendente = "atendente";
}
