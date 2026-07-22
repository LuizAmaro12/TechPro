"use client";

import { useGetApiClientesClienteIdMensagens } from "@/lib/api-client/gerado";
import {
  ROTULOS_EVENTO_COMUNICACAO,
  ROTULOS_STATUS_MENSAGEM,
} from "@/lib/ordens-servico-etapas";

/** Status que merecem destaque — são os que explicam um "não recebi". */
const COR_STATUS: Record<string, string> = {
  Enviada: "bg-ok-fundo text-ok",
  Simulada: "bg-sutil text-tinta-suave",
  Suprimida: "bg-alerta-fundo text-alerta",
  Desativada: "bg-alerta-fundo text-alerta",
  Falhou: "bg-marca-fundo text-marca",
};

/**
 * Central de mensagens do cliente: tudo que já foi comunicado, incluindo o que
 * **não** saiu (sem consentimento ou desligado nas configurações) — é o que
 * responde "por que meu cliente não recebeu?" sem abrir o WhatsApp da loja.
 */
export function HistoricoMensagens({ clienteId }: { clienteId: number }) {
  const { data: resposta } = useGetApiClientesClienteIdMensagens(clienteId);
  const mensagens = resposta?.status === 200 ? resposta.data : [];

  return (
    <div className="mt-8 border-t border-borda pt-6">
      <h3 className="text-sm font-semibold text-tinta">Mensagens enviadas</h3>
      <p className="mt-1 text-xs text-tinta-fraca">
        Histórico das notificações automáticas deste cliente, inclusive as que
        não foram enviadas e o motivo.
      </p>

      {mensagens.length === 0 ? (
        <p className="mt-3 text-sm text-tinta-fraca">
          Nenhuma mensagem registrada para este cliente ainda.
        </p>
      ) : (
        <ul className="mt-3 space-y-2">
          {mensagens.map((m) => (
            <li
              key={m.id}
              className="flex flex-wrap items-center justify-between gap-2 rounded-xl bg-sutil px-3 py-2 text-sm"
            >
              <div>
                <p className="text-tinta">
                  {ROTULOS_EVENTO_COMUNICACAO[m.tipoEvento ?? ""] ?? m.tipoEvento}
                  <span className="text-tinta-fraca"> · {m.canal}</span>
                </p>
                <p className="text-xs text-tinta-fraca">
                  {m.destino} ·{" "}
                  {new Date(m.criadoEm ?? "").toLocaleString("pt-BR", {
                    day: "2-digit",
                    month: "2-digit",
                    hour: "2-digit",
                    minute: "2-digit",
                  })}
                  {m.erro && ` · ${m.erro}`}
                </p>
              </div>
              <span
                className={`rounded-full px-2 py-0.5 text-[10px] font-semibold ${
                  COR_STATUS[m.status ?? ""] ?? "bg-sutil text-tinta-suave"
                }`}
              >
                {ROTULOS_STATUS_MENSAGEM[m.status ?? ""] ?? m.status}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
