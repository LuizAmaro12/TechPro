import { z } from "zod";

// Espelha as regras do back-end (Modules/Configuracoes/Validadores.cs).

export const esquemaLoja = z.object({
  nome: z
    .string()
    .min(1, "Informe o nome da loja.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
  telefone: z.string().max(20, "O telefone pode ter no máximo 20 caracteres."),
  email: z
    .union([z.email("Informe um e-mail válido."), z.literal("")])
    .transform((v) => v ?? ""),
  endereco: z.string().max(300, "O endereço pode ter no máximo 300 caracteres."),
  politicas: z.string().max(2000, "As políticas podem ter no máximo 2000 caracteres."),
});

export type ValoresLoja = z.infer<typeof esquemaLoja>;

export const esquemaConta = z.object({
  nome: z
    .string()
    .min(1, "Informe seu nome.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
});

export type ValoresConta = z.infer<typeof esquemaConta>;

export const esquemaTrocarSenha = z
  .object({
    senhaAtual: z.string().min(1, "Informe a senha atual."),
    novaSenha: z
      .string()
      .min(8, "A nova senha precisa de ao menos 8 caracteres.")
      .regex(/\d/, "A nova senha precisa de ao menos um número."),
    confirmacao: z.string().min(1, "Repita a nova senha."),
  })
  .refine((v) => v.novaSenha === v.confirmacao, {
    path: ["confirmacao"],
    message: "As senhas não conferem.",
  });

export type ValoresTrocarSenha = z.infer<typeof esquemaTrocarSenha>;
