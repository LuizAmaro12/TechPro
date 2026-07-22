using Microsoft.Extensions.DependencyInjection;

namespace TechPro.Api.Shared.Auth;

/// <summary>
/// Políticas nomeadas de autorização — a matriz de permissões aprovada vive
/// aqui, num lugar só. Usar <c>[Authorize(Policy = Politicas.Financeiro)]</c> em
/// vez de repetir listas de papéis pelos controllers deixa a intenção explícita
/// e torna uma mudança de regra uma edição única.
///
/// Fail-closed por padrão: endpoint sem política continua exigindo autenticação;
/// as políticas só **restringem** — nada fica mais aberto do que já era.
/// </summary>
public static class Politicas
{
    /// <summary>Só o gestor: financeiro, configurações, equipe, LGPD, auditoria.</summary>
    public const string Gestao = "gestao";

    /// <summary>Balcão: gestor e atendente (clientes, agenda, pagamentos).</summary>
    public const string Atendimento = "atendimento";

    /// <summary>Bancada: gestor e técnico (estoque, custo/preço de peça).</summary>
    public const string Bancada = "bancada";

    public static IServiceCollection AddPoliticasTechPro(this IServiceCollection services) =>
        services.AddAuthorizationBuilder()
            .AddPolicy(Gestao, p => p.RequireRole(Papeis.Gestor))
            .AddPolicy(Atendimento, p => p.RequireRole(Papeis.Gestor, Papeis.Atendente))
            .AddPolicy(Bancada, p => p.RequireRole(Papeis.Gestor, Papeis.Tecnico))
            .Services;
}
