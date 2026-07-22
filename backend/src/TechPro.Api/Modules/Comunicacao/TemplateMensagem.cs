using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Comunicacao;

/// <summary>
/// Texto personalizado da loja para um evento. **Ausência = padrão**: sem
/// registro, vale o texto embutido no <see cref="ComunicacaoService"/> — mesmo
/// raciocínio da matriz de preferências (zero seed, loja nova já nasce com
/// mensagens boas).
///
/// Um por evento (não por evento × canal): o despacho já usa um assunto —
/// aplicado só no e-mail — e um corpo compartilhado.
/// </summary>
public class TemplateMensagem : ITenantEntity
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }

    public TipoEventoComunicacao TipoEvento { get; set; }

    /// <summary>Usado só no canal e-mail (WhatsApp não tem assunto).</summary>
    public string? Assunto { get; set; }

    public required string Corpo { get; set; }

    public DateTimeOffset AtualizadoEm { get; set; }
}

/// <summary>
/// Variáveis que cada evento disponibiliza no template. É a fonte da verdade
/// tanto para a validação na gravação quanto para a dica na tela — assim a
/// loja nunca digita uma variável que não existe naquele contexto.
/// </summary>
public static class VariaveisDeTemplate
{
    private const string Cliente = "cliente";
    private const string Loja = "loja";
    private const string Servico = "servico";
    private const string Aparelho = "aparelho";
    private const string Numero = "numero";
    private const string Data = "data";
    private const string Valor = "valor";
    private const string Link = "link";

    public static IReadOnlyList<string> Para(TipoEventoComunicacao evento) => evento switch
    {
        TipoEventoComunicacao.AgendamentoConfirmado or TipoEventoComunicacao.AgendamentoLembrete =>
            [Cliente, Loja, Servico, Data],
        TipoEventoComunicacao.OrdemServicoCriada =>
            [Cliente, Loja, Servico, Aparelho, Numero, Link],
        TipoEventoComunicacao.OrcamentoDisponivel =>
            [Cliente, Loja, Aparelho, Numero, Valor, Link],
        TipoEventoComunicacao.OrcamentoAprovado or TipoEventoComunicacao.OrcamentoRecusado =>
            [Cliente, Loja, Aparelho, Numero],
        TipoEventoComunicacao.ProntoParaRetirada =>
            [Cliente, Loja, Aparelho, Numero, Link],
        TipoEventoComunicacao.PedidoAvaliacao =>
            [Cliente, Loja, Aparelho, Numero, Link],
        _ => [Cliente, Loja],
    };
}
