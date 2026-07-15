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

export const esquemaEdicaoOrdemServico = esquemaOrdemServico
  .omit({ clienteId: true, servicoId: true })
  .extend({
    statusPagamento: z.string().min(1),
    statusAprovacao: z.string().min(1),
  });

export type ValoresEdicaoOrdemServico = z.infer<typeof esquemaEdicaoOrdemServico>;
