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
import { esquemaLogin, type DadosLogin } from "@/lib/validators/auth";

export default function PaginaLogin() {
  const { usuario, carregando, entrar } = useAuth();
  const router = useRouter();

  const formulario = useForm<DadosLogin>({
    resolver: zodResolver(esquemaLogin),
    defaultValues: { email: "", senha: "" },
  });

  useEffect(() => {
    if (!carregando && usuario) router.replace("/dashboard");
  }, [carregando, usuario, router]);

  async function aoEnviar(dados: DadosLogin) {
    try {
      await entrar(dados);
      router.replace("/dashboard");
    } catch (erro) {
      toast.error(erro instanceof Error ? erro.message : "Não foi possível entrar.");
    }
  }

  const { errors, isSubmitting } = formulario.formState;

  return (
    <div className="rounded-2xl border border-[#14162B]/8 bg-white p-8 shadow-[0_1px_2px_rgba(20,22,43,0.04)]">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
        Acesse sua conta
      </p>
      <h1 className="mt-2 text-2xl font-bold text-[#14162B]">
        Entrar na TechPro
      </h1>
      <p className="mt-1 text-sm text-[#6B7280]">
        Gestão inteligente para sua assistência técnica.
      </p>

      <form
        className="mt-8 space-y-5"
        onSubmit={formulario.handleSubmit(aoEnviar)}
        noValidate
      >
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
            autoComplete="current-password"
            aria-invalid={!!errors.senha}
            className="h-11"
            {...formulario.register("senha")}
          />
          {errors.senha && (
            <p className="text-sm text-destructive">{errors.senha.message}</p>
          )}
        </div>

        <Button
          type="submit"
          disabled={isSubmitting}
          className="h-11 w-full rounded-full bg-[#14162B] text-white hover:bg-[#14162B]/90"
        >
          {isSubmitting ? "Entrando..." : "Entrar"}
        </Button>
      </form>

      <p className="mt-6 text-center text-sm text-[#6B7280]">
        Ainda não tem conta?{" "}
        <Link
          href="/cadastro"
          className="font-medium text-[#14162B] underline-offset-4 hover:underline"
        >
          Criar conta grátis
        </Link>
      </p>
    </div>
  );
}
