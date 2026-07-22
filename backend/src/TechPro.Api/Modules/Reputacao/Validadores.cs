using FluentValidation;
using TechPro.Api.Modules.Reputacao.Dtos;

namespace TechPro.Api.Modules.Reputacao;

public class AvaliacaoRequestValidator : AbstractValidator<AvaliacaoRequest>
{
    public AvaliacaoRequestValidator()
    {
        RuleFor(a => a.Nota)
            .InclusiveBetween(1, 5).WithMessage("A nota deve ser de 1 a 5 estrelas.");
        RuleFor(a => a.Recomendacao)
            .InclusiveBetween(0, 10).WithMessage("A recomendação deve ser de 0 a 10.");
        RuleFor(a => a.Comentario)
            .MaximumLength(1000).WithMessage("O comentário pode ter no máximo 1000 caracteres.");
    }
}

public class ResolverAvaliacaoRequestValidator : AbstractValidator<ResolverAvaliacaoRequest>
{
    public ResolverAvaliacaoRequestValidator()
    {
        // O valor do recurso é o registro de como foi tratado — sem nota, não há
        // "reputação gerenciada".
        RuleFor(r => r.Nota)
            .NotEmpty().WithMessage("Descreva como a avaliação foi tratada.")
            .MaximumLength(1000).WithMessage("A nota pode ter no máximo 1000 caracteres.");
    }
}
