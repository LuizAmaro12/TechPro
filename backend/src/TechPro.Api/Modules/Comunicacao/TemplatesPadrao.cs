namespace TechPro.Api.Modules.Comunicacao;

/// <summary>
/// Textos padrão de cada evento, já em forma de template (com
/// <c>{variaveis}</c>). São a **única fonte**: o despacho renderiza estes
/// mesmos textos quando a loja não personalizou, e a tela de configuração
/// mostra exatamente eles como ponto de partida. Sem duplicação — o que a loja
/// vê é o que o cliente recebe.
/// </summary>
public static class TemplatesPadrao
{
    public static (string Assunto, string Corpo) Para(TipoEventoComunicacao evento) => evento switch
    {
        TipoEventoComunicacao.AgendamentoConfirmado => (
            "Agendamento confirmado — {loja}",
            "Olá, {cliente}! Seu agendamento de {servico} na {loja} está confirmado "
            + "para {data}. Até lá!"),

        TipoEventoComunicacao.AgendamentoLembrete => (
            "Lembrete do seu agendamento — {loja}",
            "Oi, {cliente}! Passando para lembrar do seu agendamento de {servico} na "
            + "{loja} em {data}. Se precisar remarcar, é só avisar."),

        TipoEventoComunicacao.OrdemServicoCriada => (
            "Recebemos seu aparelho — OS #{numero} ({loja})",
            "Olá, {cliente}! Abrimos a ordem de serviço #{numero} para o seu {aparelho} "
            + "({servico}). Acompanhe por aqui: {link}"),

        TipoEventoComunicacao.OrcamentoDisponivel => (
            "Orçamento da OS #{numero} — {loja}",
            "Olá, {cliente}! O orçamento do reparo do seu {aparelho} ficou em {valor}. "
            + "Você pode aprovar ou recusar por aqui: {link}"),

        TipoEventoComunicacao.OrcamentoAprovado => (
            "Orçamento aprovado — OS #{numero}",
            "Recebemos a aprovação do orçamento da OS #{numero}. Já vamos seguir com o "
            + "reparo do seu {aparelho} e avisamos quando estiver pronto!"),

        TipoEventoComunicacao.OrcamentoRecusado => (
            "Orçamento recusado — OS #{numero}",
            "Registramos a recusa do orçamento da OS #{numero}. Se quiser conversar "
            + "sobre outras opções, é só falar com a {loja}."),

        TipoEventoComunicacao.ProntoParaRetirada => (
            "Seu aparelho está pronto! — OS #{numero} ({loja})",
            "Boa notícia, {cliente}! O reparo do seu {aparelho} foi concluído e está "
            + "pronto para retirada na {loja}. Te esperamos!"),

        TipoEventoComunicacao.PedidoAvaliacao => (
            "Como foi seu atendimento? — OS #{numero} ({loja})",
            "Olá, {cliente}! Esperamos que o seu {aparelho} esteja funcionando bem. Sua "
            + "opinião ajuda muito a {loja}: avalie o atendimento por aqui em 1 "
            + "minuto. {link}"),

        _ => ("{loja}", "Mensagem da {loja}."),
    };
}
