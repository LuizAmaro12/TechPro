"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  useDeleteApiOnboardingDadosExemplo,
  useGetApiDashboard,
  useGetApiOnboarding,
} from "@/lib/api-client/gerado";
import { useAuth } from "@/lib/auth/AuthProvider";
import { formatarBRL } from "@/lib/formatadores";

type Kpi = { rotulo: string; valor: number; href: string; destaque?: boolean };

const PASSOS_ATIVACAO: { chave: string; rotulo: string; href: string }[] = [
  { chave: "lojaConfigurada", rotulo: "Dados da loja", href: "/agenda/configuracoes" },
  { chave: "horariosConfigurados", rotulo: "Horários de funcionamento", href: "/agenda/configuracoes" },
  { chave: "temServico", rotulo: "Primeiro serviço", href: "/servicos" },
  { chave: "temPeca", rotulo: "Primeira peça", href: "/pecas" },
  { chave: "temCliente", rotulo: "Primeiro cliente", href: "/clientes" },
];

export default function PaginaDashboard() {
  const { usuario } = useAuth();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { data: resposta, isLoading } = useGetApiDashboard();
  const dash = resposta?.status === 200 ? resposta.data : undefined;

  // Primeiro acesso: leva ao wizard de ativação. Depois de concluir/pular, o
  // card de ativação abaixo cobre os passos que ainda faltam.
  const { data: respostaOnboarding } = useGetApiOnboarding();
  const onboarding = respostaOnboarding?.status === 200 ? respostaOnboarding.data : undefined;
  useEffect(() => {
    if (onboarding && !onboarding.onboardingConcluido) {
      router.replace("/bem-vindo");
    }
  }, [onboarding, router]);

  const removerExemplo = useDeleteApiOnboardingDadosExemplo();
  async function aoRemoverExemplo() {
    try {
      await removerExemplo.mutateAsync();
      toast.success("Dados de exemplo removidos.");
      queryClient.invalidateQueries({ queryKey: ["/api/onboarding"] });
      queryClient.invalidateQueries({ queryKey: ["/api/dashboard"] });
    } catch {
      toast.error("Não foi possível remover os dados de exemplo.");
    }
  }

  const passos = onboarding?.passos as Record<string, boolean> | undefined;
  const ativacaoIncompleta =
    onboarding?.onboardingConcluido &&
    (onboarding?.passosConcluidos ?? 0) < (onboarding?.totalPassos ?? 5);

  const kpis: Kpi[] = [
    { rotulo: "OS abertas", valor: dash?.osAbertas ?? 0, href: "/kanban" },
    { rotulo: "Agendamentos hoje", valor: dash?.agendamentosHoje ?? 0, href: "/agenda" },
    {
      rotulo: "Serviços em atraso",
      valor: dash?.servicosEmAtraso ?? 0,
      href: "/ordens-servico",
      destaque: (dash?.servicosEmAtraso ?? 0) > 0,
    },
    { rotulo: "Aparelhos em reparo", valor: dash?.aparelhosEmReparo ?? 0, href: "/kanban" },
    {
      rotulo: "Prontos para retirada",
      valor: dash?.prontosParaRetirada ?? 0,
      href: "/kanban",
    },
  ];

  const variacao = dash?.variacaoFaturamentoPct ?? null;
  const subiu = (variacao ?? 0) >= 0;
  const osAtrasadas = dash?.radar?.osAtrasadas ?? [];
  const orcamentosPendentes = dash?.radar?.orcamentosPendentes ?? [];
  const temRadar =
    (dash?.radar?.totalOsAtrasadas ?? 0) > 0 ||
    (dash?.radar?.totalOrcamentosPendentes ?? 0) > 0;

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-marca uppercase">
        Visão geral
      </p>
      <h1 className="mt-2 text-3xl font-bold text-tinta">
        Olá, {usuario?.nome ?? "..."}
      </h1>
      <p className="mt-1 text-sm text-tinta-suave">
        O resumo da operação de hoje — o que precisa de atenção vem primeiro.
      </p>

      {/* --- Ativação: passos que ainda faltam (some quando completo) ------- */}
      {(ativacaoIncompleta || onboarding?.temDadosExemplo) && (
        <section className="mt-8 rounded-2xl border border-borda bg-sutil p-6">
          {ativacaoIncompleta && (
            <>
              <div className="flex items-center justify-between gap-2">
                <h2 className="text-sm font-semibold text-tinta">
                  Ativação da loja
                </h2>
                <span className="text-xs font-semibold text-tinta-suave">
                  {onboarding?.passosConcluidos} de {onboarding?.totalPassos}
                </span>
              </div>
              <div className="mt-3 flex flex-wrap gap-2">
                {PASSOS_ATIVACAO.map((p) => {
                  const feito = passos?.[p.chave] ?? false;
                  return (
                    <Link
                      key={p.chave}
                      href={p.href}
                      className={`flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-sm ${
                        feito
                          ? "border-ok/40 bg-ok-fundo text-ok"
                          : "border-borda-forte bg-superficie text-tinta hover:border-tinta"
                      }`}
                    >
                      <span>{feito ? "✓" : "○"}</span>
                      {p.rotulo}
                    </Link>
                  );
                })}
              </div>
            </>
          )}
          {onboarding?.temDadosExemplo && (
            <div className={ativacaoIncompleta ? "mt-4" : ""}>
              <p className="text-sm text-tinta-suave">
                Você tem um cliente e uma OS de exemplo carregados.
              </p>
              <Button
                variant="outline"
                className="mt-2 h-8 rounded-full px-3 text-xs"
                disabled={removerExemplo.isPending}
                onClick={aoRemoverExemplo}
              >
                Remover dados de exemplo
              </Button>
            </div>
          )}
        </section>
      )}

      {/* --- Radar do dia: o que precisa de ação agora ---------------------- */}
      {temRadar && (
        <section className="relative mt-8">
          <div
            aria-hidden
            className="absolute -inset-3 rounded-3xl bg-gradient-to-r from-orange-400 via-pink-500 to-indigo-500 opacity-10 blur-2xl"
          />
          <div className="relative rounded-2xl border border-marca/20 bg-superficie p-6">
            <h2 className="text-sm font-semibold tracking-wide text-marca uppercase">
              Radar do dia
            </h2>
            <div className="mt-4 grid gap-6 md:grid-cols-2">
              {osAtrasadas.length > 0 && (
                <div>
                  <p className="text-sm font-semibold text-tinta">
                    OS em atraso ({dash?.radar?.totalOsAtrasadas})
                  </p>
                  <ul className="mt-2 space-y-1">
                    {osAtrasadas.map((os) => (
                      <li key={os.id}>
                        <Link
                          href="/ordens-servico"
                          className="flex items-center justify-between gap-2 rounded-lg px-2 py-1.5 text-sm hover:bg-sutil"
                        >
                          <span className="text-tinta">
                            <span className="font-medium">#{os.numero}</span> {os.clienteNome}
                            <span className="text-xs text-tinta-fraca"> · {os.servicoNome}</span>
                          </span>
                          <span className="shrink-0 rounded-full bg-marca-fundo px-2 py-0.5 text-xs font-semibold text-marca">
                            {os.diasAtraso}d
                          </span>
                        </Link>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {orcamentosPendentes.length > 0 && (
                <div>
                  <p className="text-sm font-semibold text-tinta">
                    Orçamentos aguardando resposta ({dash?.radar?.totalOrcamentosPendentes})
                  </p>
                  <ul className="mt-2 space-y-1">
                    {orcamentosPendentes.map((o) => (
                      <li key={o.id}>
                        <Link
                          href="/ordens-servico"
                          className="flex items-center justify-between gap-2 rounded-lg px-2 py-1.5 text-sm hover:bg-sutil"
                        >
                          <span className="text-tinta">
                            <span className="font-medium">#{o.numero}</span> {o.clienteNome}
                            <span className="text-xs text-tinta-fraca">
                              {" "}
                              · {formatarBRL(o.total ?? 0)}
                            </span>
                          </span>
                          <span className="shrink-0 rounded-full bg-alerta-fundo px-2 py-0.5 text-xs font-semibold text-alerta">
                            há {o.diasAguardando}d
                          </span>
                        </Link>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          </div>
        </section>
      )}

      {/* --- KPIs ----------------------------------------------------------- */}
      <section className="mt-8 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        {kpis.map((kpi) => (
          <Link
            key={kpi.rotulo}
            href={kpi.href}
            className={`rounded-2xl border p-4 transition-colors hover:border-borda-forte ${
              kpi.destaque ? "border-marca/40 bg-marca/[0.03]" : "border-borda bg-superficie"
            }`}
          >
            <p
              className={`text-3xl font-bold ${
                kpi.destaque ? "text-marca" : "text-tinta"
              }`}
            >
              {isLoading ? "—" : kpi.valor}
            </p>
            <p className="mt-1 text-xs text-tinta-suave">{kpi.rotulo}</p>
          </Link>
        ))}
      </section>

      {/* --- Faturamento do mês + comparativo ------------------------------
          Só para o gestor: faturamento é dado financeiro (matriz de permissões)
          e o link levaria técnico/atendente a um 403. --- */}
      {usuario?.papel === "gestor" && (
      <Link
        href="/financeiro"
        className="mt-6 block rounded-2xl border border-borda bg-superficie p-6 transition-colors hover:border-borda-forte"
      >
        <p className="text-xs font-medium tracking-wide text-tinta-fraca uppercase">
          Faturamento do mês
        </p>
        <div className="mt-1 flex flex-wrap items-baseline gap-3">
          <span className="text-3xl font-bold text-tinta">
            {formatarBRL(dash?.faturamentoMes ?? 0)}
          </span>
          {variacao !== null && (
            <span
              className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                subiu ? "bg-ok-fundo text-ok" : "bg-marca-fundo text-marca"
              }`}
            >
              {subiu ? "▲" : "▼"} {Math.abs(variacao)}% vs. mês anterior
            </span>
          )}
        </div>
        <p className="mt-1 text-sm text-tinta-suave">
          Mês anterior: {formatarBRL(dash?.faturamentoMesAnterior ?? 0)} · pagamentos
          recebidos (caixa) — ver detalhes no Financeiro
        </p>
      </Link>
      )}
    </div>
  );
}
