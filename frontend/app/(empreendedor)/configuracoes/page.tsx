"use client";

import { EditorTemplates } from "@/components/configuracoes/editor-templates";
import { SecaoAuditoria } from "@/components/configuracoes/secao-auditoria";
import { SecaoEquipe } from "@/components/configuracoes/secao-equipe";
import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiAuthMe,
  useGetApiConfiguracoesLoja,
  useGetApiConfiguracoesNotificacoes,
  usePostApiContaSenha,
  usePutApiConfiguracoesLoja,
  usePutApiConfiguracoesNotificacoes,
  usePutApiConta,
  type LojaResponse,
  type PreferenciaItem,
} from "@/lib/api-client/gerado";
import { ROTULOS_EVENTO_COMUNICACAO } from "@/lib/ordens-servico-etapas";
import {
  esquemaConta,
  esquemaLoja,
  esquemaTrocarSenha,
  type ValoresConta,
  type ValoresLoja,
  type ValoresTrocarSenha,
} from "@/lib/validators/configuracoes";

// Ordem canônica dos eventos na matriz de notificações.
const EVENTOS = [
  "AgendamentoConfirmado",
  "AgendamentoLembrete",
  "OrdemServicoCriada",
  "OrcamentoDisponivel",
  "OrcamentoAprovado",
  "OrcamentoRecusado",
  "ProntoParaRetirada",
  "PedidoAvaliacao",
];

export default function PaginaConfiguracoes() {
  const { data: respostaLoja } = useGetApiConfiguracoesLoja();
  const loja = respostaLoja?.status === 200 ? respostaLoja.data : undefined;
  const { data: respostaPrefs } = useGetApiConfiguracoesNotificacoes();
  const prefs = respostaPrefs?.status === 200 ? respostaPrefs.data.itens : undefined;
  const { data: respostaMe } = useGetApiAuthMe();
  const me = respostaMe?.status === 200 ? respostaMe.data : undefined;

  return (
    <div className="mx-auto w-full max-w-3xl px-6 py-10">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
        Configurações
      </p>
      <h1 className="mt-2 text-3xl font-bold text-[#14162B]">Sua loja e sua conta</h1>
      <p className="mt-1 text-sm text-[#6B7280]">
        Os horários de funcionamento e o link público de agendamento ficam em{" "}
        <Link
          href="/agenda/configuracoes"
          className="underline underline-offset-4 hover:text-[#14162B]"
        >
          configurações da agenda
        </Link>
        .
      </p>

      {loja ? <SecaoLoja key={loja.slug} loja={loja} /> : <Carregando titulo="Dados da loja" />}
      {prefs ? (
        <SecaoNotificacoes key={prefs.length} iniciais={prefs} />
      ) : (
        <Carregando titulo="Notificações" />
      )}
      <EditorTemplates />

      <SecaoEquipe />

      <SecaoAuditoria />

      {me ? <SecaoConta key={me.id} nomeAtual={me.nome ?? ""} email={me.email ?? ""} /> : null}
    </div>
  );
}

function Carregando({ titulo }: { titulo: string }) {
  return (
    <section className="mt-6 rounded-2xl border border-[#14162B]/8 bg-white p-6">
      <h2 className="text-lg font-semibold text-[#14162B]">{titulo}</h2>
      <p className="mt-2 text-sm text-[#6B7280]">Carregando...</p>
    </section>
  );
}

/** Dados da loja — o que o cliente final vê nas páginas públicas. */
function SecaoLoja({ loja }: { loja: LojaResponse }) {
  const queryClient = useQueryClient();
  const salvar = usePutApiConfiguracoesLoja();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ValoresLoja>({
    resolver: zodResolver(esquemaLoja),
    defaultValues: {
      nome: loja.nome ?? "",
      telefone: loja.telefone ?? "",
      email: loja.email ?? "",
      endereco: loja.endereco ?? "",
      politicas: loja.politicas ?? "",
    },
  });

  async function aoSalvar(valores: ValoresLoja) {
    try {
      await salvar.mutateAsync({
        data: {
          nome: valores.nome,
          telefone: valores.telefone || null,
          email: valores.email || null,
          endereco: valores.endereco || null,
          politicas: valores.politicas || null,
        },
      });
      toast.success("Dados da loja salvos.");
      queryClient.invalidateQueries({ queryKey: ["/api/configuracoes/loja"] });
      queryClient.invalidateQueries({ queryKey: ["/api/auth/me"] });
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar os dados.");
    }
  }

  return (
    <section className="mt-6 rounded-2xl border border-[#14162B]/8 bg-white p-6">
      <h2 className="text-lg font-semibold text-[#14162B]">Dados da loja</h2>
      <p className="mt-1 text-sm text-[#6B7280]">
        Contato e políticas aparecem para o cliente nas páginas de agendamento e
        de acompanhamento.
      </p>
      <form onSubmit={handleSubmit(aoSalvar)} className="mt-4 grid gap-4 sm:grid-cols-2">
        <div className="sm:col-span-2">
          <Label htmlFor="nome">Nome da loja</Label>
          <Input
            id="nome"
            className="mt-1 h-11"
            aria-invalid={!!errors.nome}
            {...register("nome")}
          />
          {errors.nome && (
            <p className="mt-1 text-sm text-destructive">{errors.nome.message}</p>
          )}
        </div>
        <div>
          <Label htmlFor="telefone">Telefone / WhatsApp</Label>
          <Input
            id="telefone"
            placeholder="(11) 3333-4444"
            className="mt-1 h-11"
            {...register("telefone")}
          />
        </div>
        <div>
          <Label htmlFor="email">E-mail de contato</Label>
          <Input
            id="email"
            className="mt-1 h-11"
            aria-invalid={!!errors.email}
            {...register("email")}
          />
          {errors.email && (
            <p className="mt-1 text-sm text-destructive">{errors.email.message}</p>
          )}
        </div>
        <div className="sm:col-span-2">
          <Label htmlFor="endereco">Endereço</Label>
          <Input
            id="endereco"
            placeholder="Rua, número, bairro, cidade"
            className="mt-1 h-11"
            {...register("endereco")}
          />
        </div>
        <div className="sm:col-span-2">
          <Label htmlFor="politicas">Políticas da loja</Label>
          <textarea
            id="politicas"
            rows={4}
            placeholder="Garantia de 90 dias. Aparelhos não retirados em 30 dias..."
            className="mt-1 w-full rounded-md border border-input bg-white px-3 py-2 text-sm"
            {...register("politicas")}
          />
          {errors.politicas && (
            <p className="mt-1 text-sm text-destructive">{errors.politicas.message}</p>
          )}
        </div>
        <div className="sm:col-span-2">
          <Button
            type="submit"
            disabled={isSubmitting}
            className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
          >
            {isSubmitting ? "Salvando..." : "Salvar dados da loja"}
          </Button>
        </div>
      </form>
    </section>
  );
}

/** Matriz evento × canal (decisão 2026-07-16). */
function SecaoNotificacoes({ iniciais }: { iniciais: PreferenciaItem[] }) {
  const queryClient = useQueryClient();
  const salvar = usePutApiConfiguracoesNotificacoes();
  const [itens, setItens] = useState<PreferenciaItem[]>(iniciais);
  const [salvando, setSalvando] = useState(false);

  function ativo(evento: string, canal: string) {
    return itens.find((i) => i.tipoEvento === evento && i.canal === canal)?.ativo ?? true;
  }

  function alternar(evento: string, canal: string) {
    setItens((atual) =>
      atual.map((i) =>
        i.tipoEvento === evento && i.canal === canal ? { ...i, ativo: !i.ativo } : i,
      ),
    );
  }

  async function aoSalvar() {
    setSalvando(true);
    try {
      await salvar.mutateAsync({ data: { itens } });
      toast.success("Preferências de notificação salvas.");
      queryClient.invalidateQueries({ queryKey: ["/api/configuracoes/notificacoes"] });
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar as preferências.");
    } finally {
      setSalvando(false);
    }
  }

  return (
    <section className="mt-6 rounded-2xl border border-[#14162B]/8 bg-white p-6">
      <h2 className="text-lg font-semibold text-[#14162B]">Notificações</h2>
      <p className="mt-1 text-sm text-[#6B7280]">
        Escolha o que o cliente recebe em cada canal. Desligar aqui não apaga o
        registro — a OS mostra a notificação como “desativada nas configurações”.
      </p>
      <div className="mt-4 overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead className="text-xs tracking-wide text-[#8B8D98] uppercase">
            <tr>
              <th className="py-2">Evento</th>
              <th className="w-28 py-2 text-center">WhatsApp</th>
              <th className="w-28 py-2 text-center">E-mail</th>
            </tr>
          </thead>
          <tbody>
            {EVENTOS.map((evento) => (
              <tr key={evento} className="border-t border-[#14162B]/6">
                <td className="py-2.5 text-[#14162B]">
                  {ROTULOS_EVENTO_COMUNICACAO[evento] ?? evento}
                </td>
                {["WhatsApp", "Email"].map((canal) => (
                  <td key={canal} className="py-2.5 text-center">
                    <input
                      type="checkbox"
                      aria-label={`${ROTULOS_EVENTO_COMUNICACAO[evento] ?? evento} por ${canal}`}
                      checked={ativo(evento, canal)}
                      onChange={() => alternar(evento, canal)}
                    />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Button
        onClick={aoSalvar}
        disabled={salvando}
        className="mt-4 h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
      >
        {salvando ? "Salvando..." : "Salvar notificações"}
      </Button>
    </section>
  );
}

/** Conta do usuário: nome + troca de senha (e-mail é o login, fora da Fase 1). */
function SecaoConta({ nomeAtual, email }: { nomeAtual: string; email: string }) {
  const queryClient = useQueryClient();
  const salvarConta = usePutApiConta();
  const trocarSenha = usePostApiContaSenha();

  const formConta = useForm<ValoresConta>({
    resolver: zodResolver(esquemaConta),
    defaultValues: { nome: nomeAtual },
  });

  const formSenha = useForm<ValoresTrocarSenha>({
    resolver: zodResolver(esquemaTrocarSenha),
    defaultValues: { senhaAtual: "", novaSenha: "", confirmacao: "" },
  });

  async function aoSalvarConta(valores: ValoresConta) {
    try {
      await salvarConta.mutateAsync({ data: { nome: valores.nome } });
      toast.success("Nome atualizado.");
      queryClient.invalidateQueries({ queryKey: ["/api/auth/me"] });
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar o nome.");
    }
  }

  async function aoTrocarSenha(valores: ValoresTrocarSenha) {
    try {
      await trocarSenha.mutateAsync({
        data: { senhaAtual: valores.senhaAtual, novaSenha: valores.novaSenha },
      });
      toast.success("Senha alterada.");
      formSenha.reset({ senhaAtual: "", novaSenha: "", confirmacao: "" });
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao trocar a senha.");
    }
  }

  return (
    <section className="mt-6 rounded-2xl border border-[#14162B]/8 bg-white p-6">
      <h2 className="text-lg font-semibold text-[#14162B]">Sua conta</h2>
      <p className="mt-1 text-sm text-[#6B7280]">
        Login: <span className="font-medium text-[#14162B]">{email}</span> — para trocar
        o e-mail, fale com o suporte (exige confirmação).
      </p>

      <form
        onSubmit={formConta.handleSubmit(aoSalvarConta)}
        className="mt-4 flex flex-wrap items-end gap-3"
      >
        <div className="min-w-56 flex-1">
          <Label htmlFor="contaNome">Seu nome</Label>
          <Input
            id="contaNome"
            className="mt-1 h-11"
            aria-invalid={!!formConta.formState.errors.nome}
            {...formConta.register("nome")}
          />
          {formConta.formState.errors.nome && (
            <p className="mt-1 text-sm text-destructive">
              {formConta.formState.errors.nome.message}
            </p>
          )}
        </div>
        <Button
          type="submit"
          variant="outline"
          disabled={formConta.formState.isSubmitting}
          className="h-11 rounded-full px-5"
        >
          Salvar nome
        </Button>
      </form>

      <form
        onSubmit={formSenha.handleSubmit(aoTrocarSenha)}
        className="mt-6 border-t border-[#14162B]/6 pt-4"
      >
        <h3 className="text-sm font-semibold text-[#14162B]">Trocar senha</h3>
        <div className="mt-3 grid gap-4 sm:grid-cols-3">
          <div>
            <Label htmlFor="senhaAtual">Senha atual</Label>
            <Input
              id="senhaAtual"
              type="password"
              className="mt-1 h-11"
              aria-invalid={!!formSenha.formState.errors.senhaAtual}
              {...formSenha.register("senhaAtual")}
            />
          </div>
          <div>
            <Label htmlFor="novaSenha">Nova senha</Label>
            <Input
              id="novaSenha"
              type="password"
              className="mt-1 h-11"
              aria-invalid={!!formSenha.formState.errors.novaSenha}
              {...formSenha.register("novaSenha")}
            />
          </div>
          <div>
            <Label htmlFor="confirmacao">Repita a nova senha</Label>
            <Input
              id="confirmacao"
              type="password"
              className="mt-1 h-11"
              aria-invalid={!!formSenha.formState.errors.confirmacao}
              {...formSenha.register("confirmacao")}
            />
          </div>
        </div>
        {(formSenha.formState.errors.senhaAtual ||
          formSenha.formState.errors.novaSenha ||
          formSenha.formState.errors.confirmacao) && (
          <p className="mt-2 text-sm text-destructive">
            {formSenha.formState.errors.senhaAtual?.message ??
              formSenha.formState.errors.novaSenha?.message ??
              formSenha.formState.errors.confirmacao?.message}
          </p>
        )}
        <Button
          type="submit"
          variant="outline"
          disabled={formSenha.formState.isSubmitting}
          className="mt-4 h-11 rounded-full px-5"
        >
          Trocar senha
        </Button>
      </form>
    </section>
  );
}
