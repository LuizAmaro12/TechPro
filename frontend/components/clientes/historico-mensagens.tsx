"use client";

import { useGetApiClientesClienteIdMensagens } from "@/lib/api-client/gerado";
import {
  ROTULOS_EVENTO_COMUNICACAO,
  ROTULOS_STATUS_MENSAGEM,
} from "@/lib/ordens-servico-etapas";

/** Status que merecem destaque — são os que explicam um "não recebi". */
const COR_STATUS: Record<string, string> = {
  Enviada: "bg-emerald-50 text-emerald-700",
  Simulada: "bg-[#F7F7F9] text-[#6B7280]",
  Suprimida: "bg-amber-50 text-amber-700",
  Desativada: "bg-amber-50 text-amber-700",
  Falhou: "bg-[#E8536B]/10 text-[#E8536B]",
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
    <div className="mt-8 border-t border-[#14162B]/8 pt-6">
      <h3 className="text-sm font-semibold text-[#14162B]">Mensagens enviadas</h3>
      <p className="mt-1 text-xs text-[#8B8D98]">
        Histórico das notificações automáticas deste cliente, inclusive as que
        não foram enviadas e o motivo.
      </p>

      {mensagens.length === 0 ? (
        <p className="mt-3 text-sm text-[#8B8D98]">
          Nenhuma mensagem registrada para este cliente ainda.
        </p>
      ) : (
        <ul className="mt-3 space-y-2">
          {mensagens.map((m) => (
            <li
              key={m.id}
              className="flex flex-wrap items-center justify-between gap-2 rounded-xl bg-[#F7F7F9] px-3 py-2 text-sm"
            >
              <div>
                <p className="text-[#14162B]">
                  {ROTULOS_EVENTO_COMUNICACAO[m.tipoEvento ?? ""] ?? m.tipoEvento}
                  <span className="text-[#8B8D98]"> · {m.canal}</span>
                </p>
                <p className="text-xs text-[#8B8D98]">
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
                  COR_STATUS[m.status ?? ""] ?? "bg-[#F7F7F9] text-[#6B7280]"
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
