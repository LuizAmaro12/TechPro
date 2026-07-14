import { z } from "zod";

// Espelha as regras do back-end (Modules/Agendamentos/Validadores.cs).

const horaObrigatoria = z
  .string()
  .regex(/^\d{2}:\d{2}$/, "Informe a hora no formato HH:mm.");

export const esquemaAgendamento = z
  .object({
    // Strings no form: <select>/<input> devolvem string; "" = não escolhido.
    servicoId: z.string().min(1, "Escolha o serviço."),
    clienteId: z.string(),
    data: z.string().min(1, "Escolha a data."),
    horaInicio: z.string().min(1, "Escolha um horário disponível."),
    nomeContato: z.string().max(200, "O nome pode ter no máximo 200 caracteres."),
    telefoneContato: z.string().max(20, "O telefone pode ter no máximo 20 caracteres."),
    emailContato: z
      .union([z.email("Informe um e-mail válido."), z.literal("")])
      .transform((v) => v ?? ""),
    descricaoProblema: z
      .string()
      .max(1000, "A descrição pode ter no máximo 1000 caracteres."),
    aparelhoMarca: z.string().max(100, "A marca pode ter no máximo 100 caracteres."),
    aparelhoModelo: z.string().max(150, "O modelo pode ter no máximo 150 caracteres."),
  })
  .refine((v) => v.clienteId !== "" || v.nomeContato.trim() !== "", {
    path: ["nomeContato"],
    message: "Informe o nome do contato ou vincule um cliente.",
  })
  .refine((v) => v.clienteId !== "" || v.telefoneContato.trim() !== "", {
    path: ["telefoneContato"],
    message: "Informe o telefone do contato ou vincule um cliente.",
  });

export type ValoresAgendamento = z.infer<typeof esquemaAgendamento>;

export const esquemaBloqueio = z
  .object({
    data: z.string().min(1, "Escolha a data."),
    horaInicio: horaObrigatoria,
    horaFim: horaObrigatoria,
    motivo: z.string().max(200, "O motivo pode ter no máximo 200 caracteres."),
  })
  .refine((v) => v.horaInicio < v.horaFim, {
    path: ["horaFim"],
    message: "O fim deve ser depois do início.",
  });

export type ValoresBloqueio = z.infer<typeof esquemaBloqueio>;

export const esquemaSlug = z.object({
  slug: z
    .string()
    .min(3, "O endereço deve ter pelo menos 3 caracteres.")
    .max(80, "O endereço pode ter no máximo 80 caracteres.")
    .regex(
      /^[a-z0-9]+(-[a-z0-9]+)*$/,
      "Use apenas letras minúsculas, números e hífens (sem hífen no início/fim).",
    ),
});

export type ValoresSlug = z.infer<typeof esquemaSlug>;

// Portal público: identificação e aparelho do wizard.
export const esquemaIdentificacao = z.object({
  nomeContato: z
    .string()
    .min(1, "Informe seu nome.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
  telefoneContato: z
    .string()
    .min(1, "Informe seu telefone/WhatsApp.")
    .max(20, "O telefone pode ter no máximo 20 caracteres."),
  emailContato: z
    .union([z.email("Informe um e-mail válido."), z.literal("")])
    .transform((v) => v ?? ""),
});

export type ValoresIdentificacao = z.infer<typeof esquemaIdentificacao>;

export const esquemaAparelhoPortal = z.object({
  aparelhoMarca: z
    .string()
    .min(1, "Informe a marca do aparelho.")
    .max(100, "A marca pode ter no máximo 100 caracteres."),
  aparelhoModelo: z
    .string()
    .min(1, "Informe o modelo do aparelho.")
    .max(150, "O modelo pode ter no máximo 150 caracteres."),
  descricaoProblema: z
    .string()
    .max(1000, "A descrição pode ter no máximo 1000 caracteres."),
});

export type ValoresAparelhoPortal = z.infer<typeof esquemaAparelhoPortal>;
