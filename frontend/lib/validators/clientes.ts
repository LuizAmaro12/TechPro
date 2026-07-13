import { z } from "zod";

// Espelha as regras do back-end (Modules/Clientes/Validadores.cs).
export const esquemaCliente = z.object({
  nome: z
    .string()
    .min(1, "Informe o nome do cliente.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
  telefone: z
    .string()
    .min(1, "Informe o telefone/WhatsApp do cliente.")
    .max(20, "O telefone pode ter no máximo 20 caracteres."),
  email: z
    .union([z.email("Informe um e-mail válido."), z.literal("")])
    .transform((v) => v ?? ""),
  cpf: z
    .string()
    .refine(
      (v) => v === "" || v.replace(/\D/g, "").length === 11,
      "O CPF deve ter 11 dígitos.",
    ),
  endereco: z.string().max(300, "O endereço pode ter no máximo 300 caracteres."),
  observacoes: z
    .string()
    .max(1000, "As observações podem ter no máximo 1000 caracteres."),
  vip: z.boolean(),
  consentiuComunicacoes: z.boolean(),
  // String no form: <select> devolve string e "" significa "sem vínculo".
  clientePrincipalId: z.string(),
});

export type ValoresCliente = z.infer<typeof esquemaCliente>;

export const esquemaAparelho = z.object({
  marca: z
    .string()
    .min(1, "Informe a marca do aparelho.")
    .max(100, "A marca pode ter no máximo 100 caracteres."),
  modelo: z
    .string()
    .min(1, "Informe o modelo do aparelho.")
    .max(150, "O modelo pode ter no máximo 150 caracteres."),
  imei: z.string().max(50, "O IMEI/nº de série pode ter no máximo 50 caracteres."),
  senhaDesbloqueio: z.string().max(100, "A senha pode ter no máximo 100 caracteres."),
  observacoes: z
    .string()
    .max(500, "As observações podem ter no máximo 500 caracteres."),
});

export type ValoresAparelho = z.infer<typeof esquemaAparelho>;
