using FluentValidation;
using TechPro.Api.Modules.Agendamentos.Dtos;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Agendamentos;

public class HorarioFuncionamentoDiaValidator : AbstractValidator<HorarioFuncionamentoDia>
{
    public HorarioFuncionamentoDiaValidator()
    {
        RuleFor(d => d.DiaSemana)
            .InclusiveBetween(0, 6).WithMessage("O dia da semana deve estar entre 0 (domingo) e 6 (sábado).");

        When(d => d.Ativo, () =>
        {
            RuleFor(d => d.Abertura)
                .NotNull().WithMessage("Informe o horário de abertura do dia ativo.");
            RuleFor(d => d.Fechamento)
                .NotNull().WithMessage("Informe o horário de fechamento do dia ativo.");
            RuleFor(d => d)
                .Must(d => d.Abertura is null || d.Fechamento is null || d.Abertura < d.Fechamento)
                .WithMessage("A abertura deve ser antes do fechamento.")
                .Must(d => (d.IntervaloInicio is null) == (d.IntervaloFim is null))
                .WithMessage("Preencha início e fim do intervalo, ou nenhum dos dois.")
                .Must(d => d.IntervaloInicio is null || d.IntervaloFim is null
                    || (d.IntervaloInicio < d.IntervaloFim
                        && d.Abertura <= d.IntervaloInicio
                        && d.IntervaloFim <= d.Fechamento))
                .WithMessage("O intervalo deve estar dentro do horário de funcionamento, com início antes do fim.");
        });
    }
}

public class HorariosFuncionamentoRequestValidator : AbstractValidator<HorariosFuncionamentoRequest>
{
    public HorariosFuncionamentoRequestValidator()
    {
        RuleFor(r => r.Dias)
            .NotNull().WithMessage("Informe os dias da semana.")
            .Must(dias => dias is { Count: 7 } && dias.Select(d => d.DiaSemana).Distinct().Count() == 7)
            .WithMessage("Envie os 7 dias da semana, sem repetição.");
        RuleForEach(r => r.Dias).SetValidator(new HorarioFuncionamentoDiaValidator());
    }
}

public class BloqueioRequestValidator : AbstractValidator<BloqueioRequest>
{
    public BloqueioRequestValidator()
    {
        RuleFor(b => b)
            .Must(b => b.HoraInicio < b.HoraFim)
            .WithMessage("O início do bloqueio deve ser antes do fim.");
        RuleFor(b => b.Motivo)
            .MaximumLength(200).WithMessage("O motivo pode ter no máximo 200 caracteres.");
    }
}

public class ConfiguracaoAgendaRequestValidator : AbstractValidator<ConfiguracaoAgendaRequest>
{
    public ConfiguracaoAgendaRequestValidator()
    {
        RuleFor(c => c.Slug)
            .NotEmpty().WithMessage("Informe o endereço público da loja.")
            .Length(3, GeradorDeSlug.TamanhoMaximo)
            .WithMessage($"O endereço deve ter entre 3 e {GeradorDeSlug.TamanhoMaximo} caracteres.")
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Use apenas letras minúsculas, números e hífens (sem hífen no início/fim).");
    }
}

public class AgendamentoRequestValidator : AbstractValidator<AgendamentoRequest>
{
    public AgendamentoRequestValidator()
    {
        RuleFor(a => a.ServicoId)
            .GreaterThan(0).WithMessage("Informe o serviço.");

        // Sem cliente vinculado, o snapshot de contato é obrigatório.
        When(a => a.ClienteId is null, () =>
        {
            RuleFor(a => a.NomeContato)
                .NotEmpty().WithMessage("Informe o nome do contato (ou vincule um cliente).");
            RuleFor(a => a.TelefoneContato)
                .NotEmpty().WithMessage("Informe o telefone do contato (ou vincule um cliente).");
        });

        RuleFor(a => a.NomeContato).MaximumLength(200);
        RuleFor(a => a.TelefoneContato).MaximumLength(20);
        RuleFor(a => a.EmailContato)
            .MaximumLength(256)
            .EmailAddress().When(a => !string.IsNullOrEmpty(a.EmailContato))
            .WithMessage("Informe um e-mail válido.");
        RuleFor(a => a.DescricaoProblema).MaximumLength(1000);
        RuleFor(a => a.AparelhoMarca).MaximumLength(100);
        RuleFor(a => a.AparelhoModelo).MaximumLength(150);
    }
}

public class CancelamentoRequestValidator : AbstractValidator<CancelamentoRequest>
{
    public CancelamentoRequestValidator()
    {
        RuleFor(c => c.Motivo)
            .MaximumLength(500).WithMessage("O motivo pode ter no máximo 500 caracteres.");
    }
}

public class AgendamentoPublicoRequestValidator : AbstractValidator<AgendamentoPublicoRequest>
{
    public AgendamentoPublicoRequestValidator()
    {
        RuleFor(a => a.ServicoId)
            .GreaterThan(0).WithMessage("Informe o serviço.");
        RuleFor(a => a.NomeContato)
            .NotEmpty().WithMessage("Informe seu nome.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(a => a.TelefoneContato)
            .NotEmpty().WithMessage("Informe seu telefone/WhatsApp.")
            .MaximumLength(20).WithMessage("O telefone pode ter no máximo 20 caracteres.");
        RuleFor(a => a.EmailContato)
            .MaximumLength(256)
            .EmailAddress().When(a => !string.IsNullOrEmpty(a.EmailContato))
            .WithMessage("Informe um e-mail válido.");
        RuleFor(a => a.DescricaoProblema)
            .MaximumLength(1000).WithMessage("A descrição pode ter no máximo 1000 caracteres.");
        RuleFor(a => a.AparelhoMarca)
            .NotEmpty().WithMessage("Informe a marca do aparelho.")
            .MaximumLength(100).WithMessage("A marca pode ter no máximo 100 caracteres.");
        RuleFor(a => a.AparelhoModelo)
            .NotEmpty().WithMessage("Informe o modelo do aparelho.")
            .MaximumLength(150).WithMessage("O modelo pode ter no máximo 150 caracteres.");
    }
}

public class FilaEsperaPublicaRequestValidator : AbstractValidator<FilaEsperaPublicaRequest>
{
    public FilaEsperaPublicaRequestValidator()
    {
        RuleFor(a => a.ServicoId).GreaterThan(0).WithMessage("Informe o serviço.");
        RuleFor(a => a.NomeContato)
            .NotEmpty().WithMessage("Informe seu nome.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(a => a.TelefoneContato)
            .NotEmpty().WithMessage("Informe seu telefone/WhatsApp.")
            .MaximumLength(20).WithMessage("O telefone pode ter no máximo 20 caracteres.");
        RuleFor(a => a.EmailContato)
            .MaximumLength(256)
            .EmailAddress().When(a => !string.IsNullOrEmpty(a.EmailContato))
            .WithMessage("Informe um e-mail válido.");
        RuleFor(a => a.DescricaoProblema).MaximumLength(1000);
        RuleFor(a => a.AparelhoMarca).MaximumLength(100);
        RuleFor(a => a.AparelhoModelo).MaximumLength(150);
    }
}

public class FilaEsperaRequestValidator : AbstractValidator<FilaEsperaRequest>
{
    public FilaEsperaRequestValidator()
    {
        RuleFor(a => a.ServicoId).GreaterThan(0).WithMessage("Informe o serviço.");
        RuleFor(a => a.NomeContato).MaximumLength(200);
        RuleFor(a => a.TelefoneContato).MaximumLength(20);
        RuleFor(a => a.EmailContato)
            .MaximumLength(256)
            .EmailAddress().When(a => !string.IsNullOrEmpty(a.EmailContato))
            .WithMessage("Informe um e-mail válido.");
        RuleFor(a => a.DescricaoProblema).MaximumLength(1000);
        RuleFor(a => a.AparelhoMarca).MaximumLength(100);
        RuleFor(a => a.AparelhoModelo).MaximumLength(150);
    }
}
