import { z } from "zod";

// Espelha as regras do back-end (Modules/ServicosEPecas/Validadores.cs):
// mesma régua nos dois lados, mensagens pt-BR.
export const esquemaFornecedor = z.object({
  nome: z
    .string()
    .min(1, "Informe o nome do fornecedor.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
  contato: z.string().max(200, "O contato pode ter no máximo 200 caracteres."),
});

export type ValoresFornecedor = z.infer<typeof esquemaFornecedor>;

export const esquemaPeca = z.object({
  nome: z
    .string()
    .min(1, "Informe o nome da peça.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
  descricao: z.string().max(500, "A descrição pode ter no máximo 500 caracteres."),
  custoUnitario: z
    .number({ message: "Informe o custo unitário." })
    .min(0, "O custo unitário não pode ser negativo."),
  precoVenda: z
    .number({ message: "Informe o preço de venda." })
    .min(0, "O preço de venda não pode ser negativo."),
  quantidadeEmEstoque: z
    .number({ message: "Informe a quantidade." })
    .int("Use um número inteiro.")
    .min(0, "A quantidade não pode ser negativa."),
  estoqueMinimo: z
    .number({ message: "Informe o estoque mínimo." })
    .int("Use um número inteiro.")
    .min(0, "O estoque mínimo não pode ser negativo."),
  // String no form: <select> devolve string e "" significa "sem fornecedor";
  // a conversão para número acontece no submit.
  fornecedorId: z.string(),
});

export type ValoresPeca = z.infer<typeof esquemaPeca>;

export const esquemaServico = z.object({
  nome: z
    .string()
    .min(1, "Informe o nome do serviço.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
  categoria: z.string().max(100, "A categoria pode ter no máximo 100 caracteres."),
  precoBase: z
    .number({ message: "Informe o preço base." })
    .min(0, "O preço base não pode ser negativo."),
  duracaoEstimadaMinutos: z
    .number({ message: "Informe a duração estimada." })
    .int("Use um número inteiro.")
    .min(1, "A duração deve ser de pelo menos 1 minuto."),
  prazoMedioDias: z
    .number()
    .int("Use um número inteiro.")
    .min(1, "O prazo médio deve ser de pelo menos 1 dia.")
    .optional(),
  exigeDiagnostico: z.boolean(),
  agendavelOnline: z.boolean(),
  capacidadeSimultanea: z
    .number({ message: "Informe a capacidade." })
    .int("Use um número inteiro.")
    .min(1, "A capacidade deve ser de pelo menos 1."),
  checklist: z.array(
    z.object({
      descricao: z
        .string()
        .min(1, "Item do checklist não pode ser vazio.")
        .max(300, "Item do checklist pode ter no máximo 300 caracteres."),
    }),
  ),
  pecas: z.array(
    z.object({
      pecaId: z.string().min(1, "Escolha a peça."),
      quantidadePadrao: z
        .number({ message: "Informe a quantidade." })
        .int("Use um número inteiro.")
        .min(1, "A quantidade deve ser de pelo menos 1."),
    }),
  ),
});

export type ValoresServico = z.infer<typeof esquemaServico>;
