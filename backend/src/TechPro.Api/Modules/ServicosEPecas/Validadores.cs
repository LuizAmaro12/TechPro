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
