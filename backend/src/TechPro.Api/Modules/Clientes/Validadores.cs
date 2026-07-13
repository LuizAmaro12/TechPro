using FluentValidation;
using TechPro.Api.Modules.Clientes.Dtos;

namespace TechPro.Api.Modules.Clientes;

public class ClienteRequestValidator : AbstractValidator<ClienteRequest>
{
    public ClienteRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Informe o nome do cliente.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(x => x.Telefone)
            .NotEmpty().WithMessage("Informe o telefone/WhatsApp do cliente.")
            .MaximumLength(20).WithMessage("O telefone pode ter no máximo 20 caracteres.");
        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Informe um e-mail válido.")
            .MaximumLength(256).WithMessage("O e-mail pode ter no máximo 256 caracteres.");
        RuleFor(x => x.Cpf)
            .Must(cpf => CpfTemOnzeDigitos(cpf!)).When(x => !string.IsNullOrWhiteSpace(x.Cpf))
            .WithMessage("O CPF deve ter 11 dígitos.");
        RuleFor(x => x.Endereco)
            .MaximumLength(300).WithMessage("O endereço pode ter no máximo 300 caracteres.");
        RuleFor(x => x.Observacoes)
            .MaximumLength(1000).WithMessage("As observações podem ter no máximo 1000 caracteres.");
    }

    private static bool CpfTemOnzeDigitos(string cpf) =>
        cpf.Count(char.IsDigit) == 11 && cpf.All(c => char.IsDigit(c) || c is '.' or '-' or ' ');
}

public class AparelhoRequestValidator : AbstractValidator<AparelhoRequest>
{
    public AparelhoRequestValidator()
    {
        RuleFor(x => x.Marca)
            .NotEmpty().WithMessage("Informe a marca do aparelho.")
            .MaximumLength(100).WithMessage("A marca pode ter no máximo 100 caracteres.");
        RuleFor(x => x.Modelo)
            .NotEmpty().WithMessage("Informe o modelo do aparelho.")
            .MaximumLength(150).WithMessage("O modelo pode ter no máximo 150 caracteres.");
        RuleFor(x => x.Imei)
            .MaximumLength(50).WithMessage("O IMEI/nº de série pode ter no máximo 50 caracteres.");
        RuleFor(x => x.SenhaDesbloqueio)
            .MaximumLength(100).WithMessage("A senha pode ter no máximo 100 caracteres.");
        RuleFor(x => x.Observacoes)
            .MaximumLength(500).WithMessage("As observações podem ter no máximo 500 caracteres.");
    }
}
