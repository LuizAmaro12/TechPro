"use client";

import { useParams } from "next/navigation";
import { useGetApiPublicoSlugAcompanharCodigo } from "@/lib/api-client/gerado";
import { formatarDataLonga } from "@/lib/agenda-datas";
import { ETAPAS_OS, rotuloDaEtapa } from "@/lib/ordens-servico-etapas";

/**
 * Acompanhamento público da OS (módulo 1, Fase 1): o cliente abre o link com
 * código opaco que a loja enviou — sem login. Mostra a etapa atual sobre a
 * régua do fluxo; a linha do tempo completa chega na Fase 2.
 */
export default function PaginaAcompanharOs() {
  const { slug, codigo } = useParams<{ slug: string; codigo: string }>();
  const { data: resposta, isLoading } = useGetApiPublicoSlugAcompanharCodigo(slug, codigo);
  const os = resposta?.status === 200 ? resposta.data : null;

  if (isLoading) {
    return (
      <div className="mx-auto w-full max-w-2xl px-6 py-16 text-center text-sm text-[#6B7280]">
        Carregando...
      </div>
    );
  }

  if (!os) {
    return (
      <div className="mx-auto w-full max-w-2xl px-6 py-16 text-center">
        <h1 className="text-2xl font-bold text-[#14162B]">Ordem não encontrada</h1>
        <p className="mt-2 text-sm text-[#6B7280]">
          Confira o link de acompanhamento com a assistência técnica.
        </p>
      </div>
    );
  }

  const cancelada = os.etapa === "Cancelado";
  const indiceAtual = ETAPAS_OS.findIndex((e) => e.valor === os.etapa);

  return (
    <div className="mx-auto w-full max-w-2xl px-6 pb-16">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
        Acompanhamento
      </p>
      <h1 className="mt-2 text-3xl font-bold text-[#14162B]">{os.nomeLoja}</h1>
      <p className="mt-1 text-sm text-[#6B7280]">
        OS #{os.numero} · {os.servicoNome}
      </p>

      <div className="mt-8 rounded-2xl border border-[#14162B]/8 p-6">
        <p className="text-sm text-[#6B7280]">Situação atual</p>
        <p
          className={`mt-1 text-2xl font-bold ${
            cancelada ? "text-[#8B8D98]" : "text-[#14162B]"
          }`}
        >
          {rotuloDaEtapa(os.etapa)}
        </p>
        {os.prazoEstimado && !cancelada && (
          <p className="mt-1 text-sm text-[#6B7280]">
            Previsão de conclusão: {formatarDataLonga(os.prazoEstimado)}
          </p>
        )}

        {!cancelada && (
          <ol className="mt-6 space-y-2">
            {ETAPAS_OS.filter((e) => e.valor !== "Cancelado").map((etapa, i) => (
              <li key={etapa.valor} className="flex items-center gap-3 text-sm">
                <span
                  className={`inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[10px] font-bold ${
                    i < indiceAtual
                      ? "bg-[#14162B] text-white"
                      : i === indiceAtual
                        ? "bg-[#E8536B] text-white"
                        : "bg-[#F7F7F9] text-[#8B8D98]"
                  }`}
                >
                  {i < indiceAtual ? "✓" : i + 1}
                </span>
                <span
                  className={
                    i === indiceAtual
                      ? "font-semibold text-[#14162B]"
                      : i < indiceAtual
                        ? "text-[#6B7280]"
                        : "text-[#8B8D98]"
                  }
                >
                  {etapa.rotulo}
                </span>
              </li>
            ))}
          </ol>
        )}

        <p className="mt-6 text-xs text-[#8B8D98]">
          Atualizado em{" "}
          {new Date(os.atualizadoEm ?? "").toLocaleString("pt-BR", {
            day: "2-digit",
            month: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
          })}
          . Dúvidas? Fale direto com a loja.
        </p>
      </div>
    </div>
  );
}
