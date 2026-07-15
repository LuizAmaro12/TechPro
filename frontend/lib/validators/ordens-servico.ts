import { z } from "zod";

// Espelha as regras do back-end (Modules/OrdensServico/Validadores.cs).
// Selects devolvem string; "" = não escolhido.

export const esquemaOrdemServico = z.object({
  clienteId: z.string().min(1, "Escolha o cliente."),
  servicoId: z.string().min(1, "Escolha o serviço."),
  aparelhoMarca: z.string().max(100, "A marca pode ter no máximo 100 caracteres."),
  aparelhoModelo: z.string().max(150, "O modelo pode ter no máximo 150 caracteres."),
  descricaoProblema: z
    .string()
    .max(1000, "A descrição pode ter no máximo 1000 caracteres."),
  prioridade: z.string().min(1),
  prazoEstimado: z.string(),
  responsavelTecnicoId: z.string(),
  observacoes: z
    .string()
    .max(1000, "As observações podem ter no máximo 1000 caracteres."),
});

export type ValoresOrdemServico = z.infer<typeof esquemaOrdemServico>;

// Status de pagamento/aprovação saíram da edição manual: desde a etapa de
// orçamento (2026-07-15) são derivados dos fluxos reais.
export const esquemaEdicaoOrdemServico = esquemaOrdemServico.omit({
  clienteId: true,
  servicoId: true,
});

export type ValoresEdicaoOrdemServico = z.infer<typeof esquemaEdicaoOrdemServico>;

export const esquemaOrcamento = z.object({
  valorMaoDeObra: z
    .number("Informe o valor de mão de obra.")
    .min(0, "O valor não pode ser negativo.")
    .max(1_000_000, "O valor máximo é 1.000.000."),
  desconto: z
    .number("Informe o desconto (0 se não houver).")
    .min(0, "O desconto não pode ser negativo.")
    .max(1_000_000, "O desconto máximo é 1.000.000."),
});

export type ValoresOrcamento = z.infer<typeof esquemaOrcamento>;

export const esquemaPagamento = z.object({
  valor: z
    .number("Informe o valor do pagamento.")
    .positive("O valor deve ser maior que zero.")
    .max(1_000_000, "O valor máximo é 1.000.000."),
  forma: z.string().min(1, "Escolha a forma de pagamento."),
  observacao: z.string().max(200, "A observação pode ter no máximo 200 caracteres."),
});

export type ValoresPagamento = z.infer<typeof esquemaPagamento>;
