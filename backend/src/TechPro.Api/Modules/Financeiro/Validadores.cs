using FluentValidation;
using TechPro.Api.Modules.Financeiro.Dtos;

namespace TechPro.Api.Modules.Financeiro;

public class OrcamentoRequestValidator : AbstractValidator<OrcamentoRequest>
{
    public OrcamentoRequestValidator()
    {
        RuleFor(o => o.ValorMaoDeObra)
            .InclusiveBetween(0, 1_000_000)
            .WithMessage("O valor de mão de obra deve estar entre 0 e 1.000.000.");
        RuleFor(o => o.Desconto)
            .InclusiveBetween(0, 1_000_000)
            .WithMessage("O desconto deve estar entre 0 e 1.000.000.");
    }
}

public class RespostaOrcamentoRequestValidator : AbstractValidator<RespostaOrcamentoRequest>
{
    public RespostaOrcamentoRequestValidator()
    {
        RuleFor(r => r.Motivo)
            .MaximumLength(500).WithMessage("O motivo pode ter no máximo 500 caracteres.");
    }
}

public class PagamentoRequestValidator : AbstractValidator<PagamentoRequest>
{
    public PagamentoRequestValidator()
    {
        RuleFor(p => p.Valor)
            .GreaterThan(0).WithMessage("O valor do pagamento deve ser maior que zero.")
            .LessThanOrEqualTo(1_000_000).WithMessage("O valor máximo é 1.000.000.");
        RuleFor(p => p.Observacao)
            .MaximumLength(200).WithMessage("A observação pode ter no máximo 200 caracteres.");
    }
}
