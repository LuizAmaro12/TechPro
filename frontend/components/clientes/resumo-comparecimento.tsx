"use client";

import { useGetApiClientesClienteIdComparecimento } from "@/lib/api-client/gerado";

const ROTULO_STATUS: Record<string, string> = {
  Agendado: "agendado",
  CheckInRealizado: "compareceu",
  Cancelado: "cancelou",
  NaoCompareceu: "faltou",
};

/**
 * Histórico de comparecimento do cliente — deriva dos agendamentos, então só
 * aparece quando o cliente já teve algum. O selo de risco existe para a loja
 * decidir se cobra sinal / confirma presença antes.
 */
export function ResumoComparecimento({ clienteId }: { clienteId: number }) {
  const { data: resposta } = useGetApiClientesClienteIdComparecimento(clienteId);
  const dados = resposta?.status === 200 ? resposta.data : null;

  if (!dados) return null;

  const compareceu = dados.compareceu ?? 0;
  const faltou = dados.faltou ?? 0;
  const cancelou = dados.cancelou ?? 0;
  const recentes = dados.recentes ?? [];

  const total = compareceu + faltou + cancelou;
  if (total === 0) {
    return (
      <div className="mt-8 border-t border-borda pt-6">
        <h3 className="text-sm font-semibold text-tinta">Comparecimento</h3>
        <p className="mt-2 text-sm text-tinta-fraca">
          Sem histórico de agendamentos ainda.
        </p>
      </div>
    );
  }

  const risco = faltou > 0;

  return (
    <div className="mt-8 border-t border-borda pt-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-semibold text-tinta">Comparecimento</h3>
        {risco && (
          <span className="rounded-full bg-alerta-fundo px-3 py-1 text-xs font-semibold text-alerta">
            ⚠ Já faltou {faltou}× — confirme presença
          </span>
        )}
      </div>

      <div className="mt-3 flex flex-wrap gap-2 text-sm">
        <span className="rounded-lg bg-ok-fundo px-3 py-1.5 text-ok">
          Compareceu <strong>{compareceu}</strong>
        </span>
        <span className="rounded-lg bg-alerta-fundo px-3 py-1.5 text-alerta">
          Faltou <strong>{faltou}</strong>
        </span>
        <span className="rounded-lg bg-sutil px-3 py-1.5 text-tinta-suave">
          Cancelou <strong>{cancelou}</strong>
        </span>
      </div>

      {recentes.length > 0 && (
        <ul className="mt-3 space-y-1 text-sm text-tinta-suave">
          {recentes.map((r) => (
            <li key={r.agendamentoId}>
              {new Date(`${r.data}T${r.horaInicio}`).toLocaleDateString("pt-BR", {
                day: "2-digit",
                month: "2-digit",
                year: "2-digit",
              })}{" "}
              — {r.servicoNome} ·{" "}
              <span className="text-tinta">
                {ROTULO_STATUS[r.status ?? ""] ?? r.status}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
