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
      <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
        Visão geral
      </p>
      <h1 className="mt-2 text-3xl font-bold text-[#14162B]">
        Olá, {usuario?.nome ?? "..."}
      </h1>
      <p className="mt-1 text-sm text-[#6B7280]">
        O resumo da operação de hoje — o que precisa de atenção vem primeiro.
      </p>

      {/* --- Ativação: passos que ainda faltam (some quando completo) ------- */}
      {(ativacaoIncompleta || onboarding?.temDadosExemplo) && (
        <section className="mt-8 rounded-2xl border border-[#14162B]/8 bg-[#F7F7F9] p-6">
          {ativacaoIncompleta && (
            <>
              <div className="flex items-center justify-between gap-2">
                <h2 className="text-sm font-semibold text-[#14162B]">
                  Ativação da loja
                </h2>
                <span className="text-xs font-semibold text-[#6B7280]">
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
                          ? "border-emerald-200 bg-emerald-50 text-emerald-700"
                          : "border-[#14162B]/15 bg-white text-[#14162B] hover:border-[#14162B]"
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
              <p className="text-sm text-[#6B7280]">
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
          <div className="relative rounded-2xl border border-[#E8536B]/20 bg-white p-6">
            <h2 className="text-sm font-semibold tracking-wide text-[#E8536B] uppercase">
              Radar do dia
            </h2>
            <div className="mt-4 grid gap-6 md:grid-cols-2">
              {osAtrasadas.length > 0 && (
                <div>
                  <p className="text-sm font-semibold text-[#14162B]">
                    OS em atraso ({dash?.radar?.totalOsAtrasadas})
                  </p>
                  <ul className="mt-2 space-y-1">
                    {osAtrasadas.map((os) => (
                      <li key={os.id}>
                        <Link
                          href="/ordens-servico"
                          className="flex items-center justify-between gap-2 rounded-lg px-2 py-1.5 text-sm hover:bg-[#F7F7F9]"
                        >
                          <span className="text-[#14162B]">
                            <span className="font-medium">#{os.numero}</span> {os.clienteNome}
                            <span className="text-xs text-[#8B8D98]"> · {os.servicoNome}</span>
                          </span>
                          <span className="shrink-0 rounded-full bg-[#E8536B]/10 px-2 py-0.5 text-xs font-semibold text-[#E8536B]">
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
                  <p className="text-sm font-semibold text-[#14162B]">
                    Orçamentos aguardando resposta ({dash?.radar?.totalOrcamentosPendentes})
                  </p>
                  <ul className="mt-2 space-y-1">
                    {orcamentosPendentes.map((o) => (
                      <li key={o.id}>
                        <Link
                          href="/ordens-servico"
                          className="flex items-center justify-between gap-2 rounded-lg px-2 py-1.5 text-sm hover:bg-[#F7F7F9]"
                        >
                          <span className="text-[#14162B]">
                            <span className="font-medium">#{o.numero}</span> {o.clienteNome}
                            <span className="text-xs text-[#8B8D98]">
                              {" "}
                              · {formatarBRL(o.total ?? 0)}
                            </span>
                          </span>
                          <span className="shrink-0 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-700">
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
            className={`rounded-2xl border p-4 transition-colors hover:border-[#14162B]/30 ${
              kpi.destaque ? "border-[#E8536B]/30 bg-[#E8536B]/[0.03]" : "border-[#14162B]/8 bg-white"
            }`}
          >
            <p
              className={`text-3xl font-bold ${
                kpi.destaque ? "text-[#E8536B]" : "text-[#14162B]"
              }`}
            >
              {isLoading ? "—" : kpi.valor}
            </p>
            <p className="mt-1 text-xs text-[#6B7280]">{kpi.rotulo}</p>
          </Link>
        ))}
      </section>

      {/* --- Faturamento do mês + comparativo ------------------------------ */}
      <Link
        href="/financeiro"
        className="mt-6 block rounded-2xl border border-[#14162B]/8 bg-white p-6 transition-colors hover:border-[#14162B]/30"
      >
        <p className="text-xs font-medium tracking-wide text-[#8B8D98] uppercase">
          Faturamento do mês
        </p>
        <div className="mt-1 flex flex-wrap items-baseline gap-3">
          <span className="text-3xl font-bold text-[#14162B]">
            {formatarBRL(dash?.faturamentoMes ?? 0)}
          </span>
          {variacao !== null && (
            <span
              className={`rounded-full px-2 py-0.5 text-xs font-semibold ${
                subiu ? "bg-emerald-100 text-emerald-700" : "bg-[#E8536B]/10 text-[#E8536B]"
              }`}
            >
              {subiu ? "▲" : "▼"} {Math.abs(variacao)}% vs. mês anterior
            </span>
          )}
        </div>
        <p className="mt-1 text-sm text-[#6B7280]">
          Mês anterior: {formatarBRL(dash?.faturamentoMesAnterior ?? 0)} · pagamentos
          recebidos (caixa) — ver detalhes no Financeiro
        </p>
      </Link>
    </div>
  );
}
