"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/lib/auth/AuthProvider";
import { esquemaCadastro, type DadosCadastro } from "@/lib/validators/auth";

export default function PaginaCadastro() {
  const { usuario, carregando, cadastrar } = useAuth();
  const router = useRouter();

  const formulario = useForm<DadosCadastro>({
    resolver: zodResolver(esquemaCadastro),
    defaultValues: { nomeEmpresa: "", nome: "", email: "", senha: "" },
  });

  useEffect(() => {
    if (!carregando && usuario) router.replace("/dashboard");
  }, [carregando, usuario, router]);

  async function aoEnviar(dados: DadosCadastro) {
    try {
      await cadastrar(dados);
      router.replace("/dashboard");
    } catch (erro) {
      toast.error(
        erro instanceof Error ? erro.message : "Não foi possível criar a conta.",
      );
    }
  }

  const { errors, isSubmitting } = formulario.formState;

  return (
    <div className="rounded-2xl border border-[#14162B]/8 bg-white p-8 shadow-[0_1px_2px_rgba(20,22,43,0.04)]">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
        Comece agora
      </p>
      <h1 className="mt-2 text-2xl font-bold text-[#14162B]">
        Crie a conta da sua assistência
      </h1>
      <p className="mt-1 text-sm text-[#6B7280]">
        Você será o gestor da empresa e poderá convidar sua equipe depois.
      </p>

      <form
        className="mt-8 space-y-5"
        onSubmit={formulario.handleSubmit(aoEnviar)}
        noValidate
      >
        <div className="space-y-2">
          <Label htmlFor="nomeEmpresa">Nome da assistência técnica</Label>
          <Input
            id="nomeEmpresa"
            placeholder="Ex.: TecCell Reparos"
            aria-invalid={!!errors.nomeEmpresa}
            className="h-11"
            {...formulario.register("nomeEmpresa")}
          />
          {errors.nomeEmpresa && (
            <p className="text-sm text-destructive">{errors.nomeEmpresa.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="nome">Seu nome</Label>
          <Input
            id="nome"
            autoComplete="name"
            aria-invalid={!!errors.nome}
            className="h-11"
            {...formulario.register("nome")}
          />
          {errors.nome && (
            <p className="text-sm text-destructive">{errors.nome.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="email">E-mail</Label>
          <Input
            id="email"
            type="email"
            autoComplete="email"
            placeholder="voce@suaempresa.com.br"
            aria-invalid={!!errors.email}
            className="h-11"
            {...formulario.register("email")}
          />
          {errors.email && (
            <p className="text-sm text-destructive">{errors.email.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="senha">Senha</Label>
          <Input
            id="senha"
            type="password"
            autoComplete="new-password"
            aria-invalid={!!errors.senha}
            className="h-11"
            {...formulario.register("senha")}
          />
          {errors.senha && (
            <p className="text-sm text-destructive">{errors.senha.message}</p>
          )}
          <p className="text-xs text-[#8B8D98]">
            Mínimo de 8 caracteres, com pelo menos uma letra e um número.
          </p>
        </div>

        <Button
          type="submit"
          disabled={isSubmitting}
          className="h-11 w-full rounded-full bg-[#14162B] text-white hover:bg-[#14162B]/90"
        >
          {isSubmitting ? "Criando conta..." : "Criar conta"}
        </Button>
      </form>

      <p className="mt-6 text-center text-sm text-[#6B7280]">
        Já tem conta?{" "}
        <Link
          href="/login"
          className="font-medium text-[#14162B] underline-offset-4 hover:underline"
        >
          Entrar
        </Link>
      </p>
    </div>
  );
}
