using FluentValidation;
using TechPro.Api.Modules.ServicosEPecas.Dtos;

namespace TechPro.Api.Modules.ServicosEPecas;

public class FornecedorRequestValidator : AbstractValidator<FornecedorRequest>
{
    public FornecedorRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Informe o nome do fornecedor.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(x => x.Contato)
            .MaximumLength(200).WithMessage("O contato pode ter no máximo 200 caracteres.");
    }
}

public class PecaRequestValidator : AbstractValidator<PecaRequest>
{
    public PecaRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Informe o nome da peça.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(x => x.Descricao)
            .MaximumLength(500).WithMessage("A descrição pode ter no máximo 500 caracteres.");
        RuleFor(x => x.CustoUnitario)
            .GreaterThanOrEqualTo(0).WithMessage("O custo unitário não pode ser negativo.");
        RuleFor(x => x.PrecoVenda)
            .GreaterThanOrEqualTo(0).WithMessage("O preço de venda não pode ser negativo.");
        RuleFor(x => x.QuantidadeEmEstoque)
            .GreaterThanOrEqualTo(0).WithMessage("A quantidade em estoque não pode ser negativa.");
        RuleFor(x => x.EstoqueMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("O estoque mínimo não pode ser negativo.");
    }
}
