"use client";

import { useState } from "react";
import { useGetApiAuditoria } from "@/lib/api-client/gerado";

const AREAS = ["", "Equipe", "LGPD", "Configurações"];

/**
 * Histórico das ações sensíveis que antes não deixavam rastro. OS, orçamento,
 * estoque e reatribuição têm as próprias trilhas — não são duplicadas aqui.
 */
export function SecaoAuditoria() {
  const [area, setArea] = useState("");
  const { data: resposta } = useGetApiAuditoria({ entidade: area || undefined });
  const registros = resposta?.status === 200 ? resposta.data : [];

  return (
    <section className="mt-8 rounded-2xl border border-borda bg-superficie p-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold text-tinta">Histórico de ações</h2>
          <p className="mt-1 text-sm text-tinta-suave">
            Quem mexeu em equipe, dados pessoais e configurações. Movimentações de
            OS, orçamento e estoque têm o histórico na própria tela.
          </p>
        </div>
        <select
          aria-label="Filtrar por área"
          value={area}
          onChange={(e) => setArea(e.target.value)}
          className="h-10 rounded-md border border-borda bg-superficie px-2 text-sm"
        >
          {AREAS.map((a) => (
            <option key={a || "todas"} value={a}>
              {a || "Todas as áreas"}
            </option>
          ))}
        </select>
      </div>

      {registros.length === 0 ? (
        <p className="mt-4 text-sm text-tinta-fraca">Nenhuma ação registrada ainda.</p>
      ) : (
        <ul className="mt-4 space-y-2">
          {registros.map((r) => (
            <li
              key={r.id}
              className="flex flex-wrap items-center justify-between gap-2 rounded-xl bg-sutil px-3 py-2 text-sm"
            >
              <div>
                <p className="text-tinta">
                  {r.acao}
                  {r.detalhe && <span className="text-tinta-suave"> — {r.detalhe}</span>}
                </p>
                <p className="text-xs text-tinta-fraca">
                  {r.usuarioNome} ·{" "}
                  {new Date(r.criadoEm ?? "").toLocaleString("pt-BR", {
                    day: "2-digit",
                    month: "2-digit",
                    hour: "2-digit",
                    minute: "2-digit",
                  })}
                </p>
              </div>
              <span className="rounded-full bg-sutil px-2 py-0.5 text-[10px] font-semibold text-tinta uppercase">
                {r.entidade}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
