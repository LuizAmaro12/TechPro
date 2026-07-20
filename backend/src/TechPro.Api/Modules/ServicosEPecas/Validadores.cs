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

public class ServicoRequestValidator : AbstractValidator<ServicoRequest>
{
    public ServicoRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Informe o nome do serviço.")
            .MaximumLength(200).WithMessage("O nome pode ter no máximo 200 caracteres.");
        RuleFor(x => x.Categoria)
            .MaximumLength(100).WithMessage("A categoria pode ter no máximo 100 caracteres.");
        RuleFor(x => x.PrecoBase)
            .GreaterThanOrEqualTo(0).WithMessage("O preço base não pode ser negativo.");
        RuleFor(x => x.DuracaoEstimadaMinutos)
            .GreaterThan(0).WithMessage("Informe a duração estimada em minutos.");
        RuleFor(x => x.PrazoMedioDias)
            .GreaterThan(0).When(x => x.PrazoMedioDias.HasValue)
            .WithMessage("O prazo médio deve ser de pelo menos 1 dia.");
        RuleFor(x => x.CapacidadeSimultanea)
            .GreaterThan(0).WithMessage("A capacidade simultânea deve ser de pelo menos 1.");
        RuleFor(x => x.SlaHoras)
            .InclusiveBetween(1, 2160).When(x => x.SlaHoras.HasValue)
            .WithMessage("O SLA deve ficar entre 1 hora e 90 dias.");
        RuleFor(x => x.Checklist)
            .NotNull().WithMessage("Envie o checklist (pode ser vazio).");
        RuleForEach(x => x.Checklist)
            .NotEmpty().WithMessage("Item do checklist não pode ser vazio.")
            .MaximumLength(300).WithMessage("Item do checklist pode ter no máximo 300 caracteres.");
        RuleFor(x => x.Pecas)
            .NotNull().WithMessage("Envie a lista de peças (pode ser vazia).")
            .Must(pecas => pecas is null || pecas.Select(p => p.PecaId).Distinct().Count() == pecas.Count)
            .WithMessage("Não repita a mesma peça no serviço.");
        RuleForEach(x => x.Pecas).ChildRules(peca => peca
            .RuleFor(p => p.QuantidadePadrao)
            .GreaterThan(0).WithMessage("A quantidade padrão da peça deve ser de pelo menos 1."));
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

public class MovimentacaoRequestValidator : AbstractValidator<MovimentacaoRequest>
{
    public MovimentacaoRequestValidator()
    {
        // Quantidade chega sempre positiva; o tipo define o sinal.
        RuleFor(m => m.Quantidade)
            .GreaterThanOrEqualTo(0).WithMessage("A quantidade não pode ser negativa.")
            .LessThanOrEqualTo(100000).WithMessage("Quantidade acima do limite aceito.");
        RuleFor(m => m.Quantidade)
            .GreaterThan(0).When(m => m.Tipo != TipoMovimentacaoEstoque.Ajuste)
            .WithMessage("Informe uma quantidade maior que zero.");
        // Ajuste sem motivo é exatamente o buraco que esta etapa fechou.
        RuleFor(m => m.Motivo)
            .NotEmpty().When(m => m.Tipo == TipoMovimentacaoEstoque.Ajuste)
            .WithMessage("Informe o motivo do ajuste de estoque.");
        RuleFor(m => m.Motivo)
            .MaximumLength(300).WithMessage("O motivo pode ter no máximo 300 caracteres.");
        RuleFor(m => m.CustoUnitario)
            .GreaterThanOrEqualTo(0).When(m => m.CustoUnitario.HasValue)
            .WithMessage("O custo não pode ser negativo.");
    }
}
