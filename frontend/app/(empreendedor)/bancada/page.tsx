"use client";

import { useState } from "react";
import { useAuth } from "@/lib/auth/AuthProvider";
import {
  useGetApiOrdensServico,
  type OrdemServicoResponse,
} from "@/lib/api-client/gerado";
import { ROTULOS_PRIORIDADE, rotuloDaEtapa } from "@/lib/ordens-servico-etapas";
import { BORDA_SLA, ROTULO_SLA, formatarTempoNaEtapa, situacaoSla } from "@/lib/sla";
import { OrdemDaBancada } from "@/components/bancada/ordem-da-bancada";

/**
 * Portal do técnico (módulo 4) — a lista **do que é dele** para fazer agora, em
 * uma coluna, com alvos de toque grandes. O Kanban é a visão gerencial; aqui é
 * a bancada. Filtra por `responsavelId`; sem financeiro, sem OS de outros.
 */
export default function PaginaBancada() {
  const { usuario } = useAuth();
  const [abertaId, setAbertaId] = useState<string | null>(null);

  const { data: resposta, isLoading } = useGetApiOrdensServico(
    { responsavelId: usuario?.id },
    { query: { enabled: !!usuario?.id } },
  );
  const ordens = resposta?.status === 200 ? resposta.data : [];

  // Prioridade primeiro (Alta > Normal > Baixa), depois prazo mais próximo.
  const peso = { Alta: 0, Normal: 1, Baixa: 2 } as const;
  const emAndamento = [...ordens].sort((a, b) => {
    const pa = peso[a.prioridade ?? "Normal"] - peso[b.prioridade ?? "Normal"];
    if (pa !== 0) return pa;
    return (a.prazoEstimado ?? "9999").localeCompare(b.prazoEstimado ?? "9999");
  });

  return (
    <div className="mx-auto w-full max-w-2xl px-4 py-8 sm:px-6">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-marca uppercase">
        Bancada
      </p>
      <h1 className="mt-2 text-2xl font-bold text-tinta sm:text-3xl">Minhas ordens</h1>
      <p className="mt-1 text-sm text-tinta-suave">
        As OS sob sua responsabilidade, por prioridade e prazo. Toque para
        trabalhar.
      </p>

      {isLoading ? (
        <p className="mt-8 text-sm text-tinta-suave">Carregando...</p>
      ) : emAndamento.length === 0 ? (
        <p className="mt-8 rounded-2xl border border-borda bg-superficie p-6 text-sm text-tinta-suave">
          Nenhuma OS atribuída a você no momento. Quando o gestor te definir como
          responsável de uma ordem, ela aparece aqui.
        </p>
      ) : (
        <ul className="mt-6 space-y-3">
          {emAndamento.map((ordem) => (
            <li key={ordem.id}>
              {abertaId === ordem.id ? (
                <OrdemDaBancada
                  ordemId={ordem.id!}
                  resumo={ordem}
                  aoFechar={() => setAbertaId(null)}
                />
              ) : (
                <CartaoResumo ordem={ordem} aoAbrir={() => setAbertaId(ordem.id ?? null)} />
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function CartaoResumo({
  ordem,
  aoAbrir,
}: {
  ordem: OrdemServicoResponse;
  aoAbrir: () => void;
}) {
  const sla = situacaoSla(ordem.horasNaEtapa, ordem.slaHoras);
  const tempo = formatarTempoNaEtapa(ordem.horasNaEtapa);
  const aparelho = [ordem.aparelhoMarca, ordem.aparelhoModelo].filter(Boolean).join(" ");

  return (
    <button
      onClick={aoAbrir}
      className={`flex w-full items-center gap-3 rounded-2xl border border-l-4 border-borda bg-superficie p-4 text-left transition-colors hover:border-borda-forte ${BORDA_SLA[sla]}`}
    >
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="font-semibold text-tinta">#{ordem.numero}</span>
          {ordem.prioridade === "Alta" && (
            <span className="rounded-full bg-marca-fundo px-2 py-0.5 text-[10px] font-semibold text-marca uppercase">
              alta
            </span>
          )}
          {(sla === "atencao" || sla === "estourado") && (
            <span
              className={`rounded-full px-2 py-0.5 text-[10px] font-semibold ${
                sla === "estourado"
                  ? "bg-marca-fundo text-marca"
                  : "bg-alerta-fundo text-alerta"
              }`}
            >
              {ROTULO_SLA[sla]} · {tempo}
            </span>
          )}
        </div>
        <p className="mt-1 truncate font-medium text-tinta">{ordem.clienteNome}</p>
        <p className="truncate text-sm text-tinta-suave">
          {ordem.servicoNome}
          {aparelho && ` · ${aparelho}`}
        </p>
        <p className="mt-1 text-xs text-tinta-fraca">
          {rotuloDaEtapa(ordem.etapa)} · {ROTULOS_PRIORIDADE[ordem.prioridade ?? "Normal"]}
        </p>
      </div>
      <span aria-hidden className="text-tinta-fraca">
        ›
      </span>
    </button>
  );
}
