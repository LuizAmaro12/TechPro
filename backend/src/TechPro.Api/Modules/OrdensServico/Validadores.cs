using FluentValidation;
using TechPro.Api.Modules.OrdensServico.Dtos;

namespace TechPro.Api.Modules.OrdensServico;

public class OrdemServicoRequestValidator : AbstractValidator<OrdemServicoRequest>
{
    public OrdemServicoRequestValidator()
    {
        RuleFor(o => o.ClienteId).GreaterThan(0).WithMessage("Informe o cliente.");
        RuleFor(o => o.ServicoId).GreaterThan(0).WithMessage("Informe o serviço.");
        RuleFor(o => o.AparelhoMarca)
            .MaximumLength(100).WithMessage("A marca pode ter no máximo 100 caracteres.");
        RuleFor(o => o.AparelhoModelo)
            .MaximumLength(150).WithMessage("O modelo pode ter no máximo 150 caracteres.");
        RuleFor(o => o.DescricaoProblema)
            .MaximumLength(1000).WithMessage("A descrição pode ter no máximo 1000 caracteres.");
        RuleFor(o => o.Observacoes)
            .MaximumLength(1000).WithMessage("As observações podem ter no máximo 1000 caracteres.");
    }
}

public class OrdemServicoAtualizacaoRequestValidator
    : AbstractValidator<OrdemServicoAtualizacaoRequest>
{
    public OrdemServicoAtualizacaoRequestValidator()
    {
        RuleFor(o => o.AparelhoMarca)
            .MaximumLength(100).WithMessage("A marca pode ter no máximo 100 caracteres.");
        RuleFor(o => o.AparelhoModelo)
            .MaximumLength(150).WithMessage("O modelo pode ter no máximo 150 caracteres.");
        RuleFor(o => o.DescricaoProblema)
            .MaximumLength(1000).WithMessage("A descrição pode ter no máximo 1000 caracteres.");
        RuleFor(o => o.Observacoes)
            .MaximumLength(1000).WithMessage("As observações podem ter no máximo 1000 caracteres.");
    }
}

public class MudancaEtapaRequestValidator : AbstractValidator<MudancaEtapaRequest>
{
    public MudancaEtapaRequestValidator()
    {
        RuleFor(m => m.Motivo)
            .MaximumLength(500).WithMessage("O motivo pode ter no máximo 500 caracteres.");
        RuleFor(m => m.Motivo)
            .NotEmpty()
            .When(m => m.ParaEtapa == EtapaOrdemServico.Cancelado)
            .WithMessage("Informe o motivo do cancelamento.");
    }
}
