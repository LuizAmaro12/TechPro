using FluentValidation;

namespace TechPro.Api.Shared.Auth;

/// <summary>
/// As regras de senha espelham (e apertam um pouco) as opções do Identity em
/// Program.cs: mínimo 8, ao menos uma letra e um número. Assim o usuário
/// recebe mensagens em pt-BR do validador em vez dos erros genéricos do Identity.
/// </summary>
public class RegistrarRequestValidator : AbstractValidator<RegistrarRequest>
{
    public RegistrarRequestValidator()
    {
        RuleFor(r => r.NomeEmpresa)
            .NotEmpty().WithMessage("Informe o nome da assistência técnica.")
            .MaximumLength(200).WithMessage("O nome da empresa pode ter no máximo 200 caracteres.");

        RuleFor(r => r.Nome)
            .NotEmpty().WithMessage("Informe seu nome.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Informe o e-mail.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(256).WithMessage("O e-mail pode ter no máximo 256 caracteres.");

        RuleFor(r => r.Senha)
            .NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(8).WithMessage("A senha precisa ter pelo menos 8 caracteres.")
            .Matches("[a-zA-Z]").WithMessage("A senha precisa ter pelo menos uma letra.")
            .Matches("[0-9]").WithMessage("A senha precisa ter pelo menos um número.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().WithMessage("Informe o e-mail.");
        RuleFor(r => r.Senha).NotEmpty().WithMessage("Informe a senha.");
    }
}
