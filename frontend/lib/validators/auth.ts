import { z } from "zod";

// Espelha as regras do back-end (Validadores.cs) para o erro aparecer
// no campo antes mesmo de a requisição sair.
export const esquemaLogin = z.object({
  email: z.email("E-mail inválido.").min(1, "Informe o e-mail."),
  senha: z.string().min(1, "Informe a senha."),
});

export const esquemaCadastro = z.object({
  nomeEmpresa: z
    .string()
    .min(1, "Informe o nome da assistência técnica.")
    .max(200, "O nome da empresa pode ter no máximo 200 caracteres."),
  nome: z
    .string()
    .min(1, "Informe seu nome.")
    .max(200, "O nome pode ter no máximo 200 caracteres."),
  email: z.email("E-mail inválido."),
  senha: z
    .string()
    .min(8, "A senha precisa ter pelo menos 8 caracteres.")
    .regex(/[a-zA-Z]/, "A senha precisa ter pelo menos uma letra.")
    .regex(/[0-9]/, "A senha precisa ter pelo menos um número."),
});

export type DadosLogin = z.infer<typeof esquemaLogin>;
export type DadosCadastro = z.infer<typeof esquemaCadastro>;
