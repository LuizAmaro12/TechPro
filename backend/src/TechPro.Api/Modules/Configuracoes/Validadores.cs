using FluentValidation;
using TechPro.Api.Modules.Configuracoes.Dtos;

namespace TechPro.Api.Modules.Configuracoes;

public class LojaRequestValidator : AbstractValidator<LojaRequest>
{
    public LojaRequestValidator()
    {
        RuleFor(l => l.Nome)
            .NotEmpty().WithMessage("Informe o nome da loja.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(l => l.Telefone)
            .MaximumLength(20).WithMessage("O telefone pode ter no máximo 20 caracteres.");
        RuleFor(l => l.Email)
            .MaximumLength(256)
            .EmailAddress().When(l => !string.IsNullOrWhiteSpace(l.Email))
            .WithMessage("Informe um e-mail válido.");
        RuleFor(l => l.Endereco)
            .MaximumLength(300).WithMessage("O endereço pode ter no máximo 300 caracteres.");
        RuleFor(l => l.Politicas)
            .MaximumLength(2000).WithMessage("As políticas podem ter no máximo 2000 caracteres.");
    }
}

public class PreferenciasNotificacaoRequestValidator
    : AbstractValidator<PreferenciasNotificacaoRequest>
{
    public PreferenciasNotificacaoRequestValidator()
    {
        RuleFor(p => p.Itens)
            .NotNull().WithMessage("Informe as preferências.")
            .Must(itens => itens is not null
                && itens.Select(i => (i.TipoEvento, i.Canal)).Distinct().Count() == itens.Count)
            .WithMessage("Há preferências repetidas para o mesmo evento e canal.");
    }
}

public class ContaRequestValidator : AbstractValidator<ContaRequest>
{
    public ContaRequestValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Informe seu nome.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
    }
}

public class TrocarSenhaRequestValidator : AbstractValidator<TrocarSenhaRequest>
{
    public TrocarSenhaRequestValidator()
    {
        RuleFor(t => t.SenhaAtual)
            .NotEmpty().WithMessage("Informe a senha atual.");
        // Espelha a política do Identity configurada no Program.cs.
        RuleFor(t => t.NovaSenha)
            .NotEmpty().WithMessage("Informe a nova senha.")
            .MinimumLength(8).WithMessage("A nova senha precisa de ao menos 8 caracteres.")
            .Matches(@"\d").WithMessage("A nova senha precisa de ao menos um número.");
    }
}
