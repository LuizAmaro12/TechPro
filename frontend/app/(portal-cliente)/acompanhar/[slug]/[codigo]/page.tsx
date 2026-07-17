"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useParams } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { ContatoDaLoja } from "@/components/portal/contato-da-loja";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiPublicoSlugAcompanharCodigo,
  usePostApiPublicoSlugAcompanharCodigoOrcamentoAprovar,
  usePostApiPublicoSlugAcompanharCodigoOrcamentoRecusar,
} from "@/lib/api-client/gerado";
import { formatarDataLonga } from "@/lib/agenda-datas";
import { formatarBRL } from "@/lib/formatadores";
import { ETAPAS_OS, rotuloDaEtapa } from "@/lib/ordens-servico-etapas";

/**
 * Acompanhamento público da OS (módulo 1, Fase 1): o cliente abre o link com
 * código opaco que a loja enviou — sem login. Mostra a etapa atual sobre a
 * régua do fluxo e, quando há orçamento enviado, permite aprovar/recusar.
 * A linha do tempo completa chega na Fase 2.
 */
export default function PaginaAcompanharOs() {
  const { slug, codigo } = useParams<{ slug: string; codigo: string }>();
  const queryClient = useQueryClient();
  const [recusando, setRecusando] = useState(false);
  const [motivo, setMotivo] = useState("");

  const { data: resposta, isLoading } = useGetApiPublicoSlugAcompanharCodigo(slug, codigo);
  const os = resposta?.status === 200 ? resposta.data : null;

  const aprovar = usePostApiPublicoSlugAcompanharCodigoOrcamentoAprovar();
  const recusar = usePostApiPublicoSlugAcompanharCodigoOrcamentoRecusar();

  function invalidar() {
    queryClient.invalidateQueries({
      queryKey: [`/api/publico/${slug}/acompanhar/${codigo}`],
    });
  }

  async function aoAprovar() {
    try {
      await aprovar.mutateAsync({ slug, codigo, data: { motivo: null } });
      toast.success("Orçamento aprovado. A loja foi avisada!");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Não foi possível aprovar.");
    }
  }

  async function aoRecusar() {
    try {
      await recusar.mutateAsync({ slug, codigo, data: { motivo: motivo || null } });
      toast.success("Recusa registrada. A loja foi avisada.");
      setRecusando(false);
      setMotivo("");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Não foi possível recusar.");
    }
  }

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
  const orcamento = os.orcamento ?? null;
  const podeResponder = orcamento?.status === "Enviado" && !cancelada;

  return (
    <div className="mx-auto w-full max-w-2xl px-6 pb-16">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
        Acompanhamento
      </p>
      <h1 className="mt-2 text-3xl font-bold text-[#14162B]">{os.nomeLoja}</h1>
      <p className="mt-1 text-sm text-[#6B7280]">
        OS #{os.numero} · {os.servicoNome}
      </p>

      {/* Orçamento — só aparece depois de enviado pela loja */}
      {orcamento && (
        <div className="mt-8 rounded-2xl border border-[#14162B]/8 p-6">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 className="text-lg font-semibold text-[#14162B]">Seu orçamento</h2>
            {orcamento.status === "Aprovado" && (
              <span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-semibold text-emerald-700">
                aprovado
              </span>
            )}
            {orcamento.status === "Recusado" && (
              <span className="rounded-full bg-[#E8536B]/10 px-3 py-1 text-xs font-semibold text-[#E8536B]">
                recusado
              </span>
            )}
          </div>

          <dl className="mt-4 space-y-1.5 text-sm">
            <div className="flex justify-between">
              <dt className="text-[#6B7280]">Mão de obra</dt>
              <dd className="text-[#14162B]">{formatarBRL(orcamento.valorMaoDeObra ?? 0)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-[#6B7280]">Peças</dt>
              <dd className="text-[#14162B]">{formatarBRL(orcamento.valorPecas ?? 0)}</dd>
            </div>
            {(orcamento.desconto ?? 0) > 0 && (
              <div className="flex justify-between">
                <dt className="text-[#6B7280]">Desconto</dt>
                <dd className="text-[#14162B]">−{formatarBRL(orcamento.desconto ?? 0)}</dd>
              </div>
            )}
            <div className="flex justify-between border-t border-[#14162B]/6 pt-1.5 text-base">
              <dt className="font-semibold text-[#14162B]">Total</dt>
              <dd className="font-bold text-[#14162B]">{formatarBRL(orcamento.total ?? 0)}</dd>
            </div>
          </dl>

          {podeResponder && (
            <div className="mt-5">
              {!recusando ? (
                <div className="flex flex-wrap gap-3">
                  <Button
                    disabled={aprovar.isPending}
                    onClick={aoAprovar}
                    className="h-11 rounded-full bg-[#14162B] px-8 text-white hover:bg-[#14162B]/90"
                  >
                    Aprovar orçamento
                  </Button>
                  <Button
                    variant="ghost"
                    className="h-11 text-[#E8536B] hover:text-[#E8536B]"
                    onClick={() => setRecusando(true)}
                  >
                    Recusar
                  </Button>
                </div>
              ) : (
                <div className="flex flex-wrap items-end gap-2">
                  <div className="min-w-56 flex-1">
                    <label htmlFor="motivo" className="text-sm text-[#6B7280]">
                      Conte o motivo (opcional)
                    </label>
                    <Input
                      id="motivo"
                      value={motivo}
                      onChange={(e) => setMotivo(e.target.value)}
                      className="mt-1 h-11"
                    />
                  </div>
                  <Button
                    variant="outline"
                    className="h-11 rounded-full px-5 text-[#E8536B]"
                    disabled={recusar.isPending}
                    onClick={aoRecusar}
                  >
                    Confirmar recusa
                  </Button>
                  <Button variant="ghost" className="h-11" onClick={() => setRecusando(false)}>
                    Voltar
                  </Button>
                </div>
              )}
            </div>
          )}

          {orcamento.status === "Aprovado" && (
            <p className="mt-4 text-sm text-emerald-700">
              Obrigado! A loja já pode seguir com o reparo.
            </p>
          )}
          {orcamento.status === "Recusado" && (
            <p className="mt-4 text-sm text-[#6B7280]">
              Você recusou este orçamento. Fale com a loja se mudar de ideia.
            </p>
          )}
        </div>
      )}

      <div className="mt-6 rounded-2xl border border-[#14162B]/8 p-6">
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
            {ETAPAS_OS.filter((e) => e.valor !== "Cancelado").map((etapa, i) => {
              const alcancada = (os.linhaDoTempo ?? []).find((t) => t.etapa === etapa.valor);
              return (
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
                  className={`flex-1 ${
                    i === indiceAtual
                      ? "font-semibold text-[#14162B]"
                      : i < indiceAtual
                        ? "text-[#6B7280]"
                        : "text-[#8B8D98]"
                  }`}
                >
                  {etapa.rotulo}
                </span>
                {alcancada && (
                  <span className="shrink-0 text-xs text-[#8B8D98]">
                    {new Date(alcancada.alcancadaEm ?? "").toLocaleString("pt-BR", {
                      day: "2-digit",
                      month: "2-digit",
                      hour: "2-digit",
                      minute: "2-digit",
                    })}
                  </span>
                )}
              </li>
              );
            })}
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

      <ContatoDaLoja contato={os.contato} />
    </div>
  );
}
