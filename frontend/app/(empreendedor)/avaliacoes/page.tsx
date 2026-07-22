"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiAvaliacoes,
  useGetApiAvaliacoesResumo,
  usePostApiAvaliacoesIdResolver,
  type AvaliacaoResponse,
} from "@/lib/api-client/gerado";

function Estrelas({ nota }: { nota: number }) {
  return (
    <span className="text-marca" aria-label={`${nota} de 5 estrelas`}>
      {"★".repeat(nota)}
      <span className="text-tinta-fraca">{"★".repeat(5 - nota)}</span>
    </span>
  );
}

/** NPS vai de −100 a +100: a cor comunica a faixa sem precisar explicar. */
function corDoNps(score: number) {
  if (score >= 50) return "text-ok";
  if (score >= 0) return "text-alerta";
  return "text-marca";
}

export default function PaginaAvaliacoes() {
  const queryClient = useQueryClient();
  const [apenasPendentes, setApenasPendentes] = useState(false);
  const [resolvendoId, setResolvendoId] = useState<number | null>(null);
  const [notaResolucao, setNotaResolucao] = useState("");

  const { data: respostaResumo } = useGetApiAvaliacoesResumo();
  const resumo = respostaResumo?.status === 200 ? respostaResumo.data : null;

  const { data: respostaLista } = useGetApiAvaliacoes({ apenasPendentes });
  const avaliacoes = respostaLista?.status === 200 ? respostaLista.data : [];

  const resolver = usePostApiAvaliacoesIdResolver();

  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ["/api/avaliacoes"] });
    queryClient.invalidateQueries({ queryKey: ["/api/avaliacoes/resumo"] });
  }

  async function aoResolver(id: number) {
    try {
      await resolver.mutateAsync({ id, data: { nota: notaResolucao.trim() } });
      toast.success("Loop fechado — tratamento registrado.");
      setResolvendoId(null);
      setNotaResolucao("");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Não foi possível registrar.");
    }
  }

  const maiorFaixa = Math.max(
    1,
    ...(resumo?.distribuicao ?? []).map((d) => d.quantidade ?? 0),
  );

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <div>
        <p className="text-[11px] font-semibold tracking-[0.18em] text-marca uppercase">
          Reputação
        </p>
        <h1 className="mt-2 text-3xl font-bold text-tinta">Avaliações</h1>
        <p className="mt-1 text-sm text-tinta-suave">
          O que os clientes acharam do reparo — e o que ainda precisa de resposta.
          O pedido de avaliação sai automaticamente quando a OS é entregue.
        </p>
      </div>

      {resumo && (resumo.total ?? 0) === 0 ? (
        <p className="mt-8 rounded-2xl border border-borda bg-superficie p-6 text-sm text-tinta-suave">
          Ainda não há avaliações. Assim que você entregar uma OS, o cliente
          recebe o convite para avaliar pelo link de acompanhamento.
        </p>
      ) : (
        resumo && (
          <>
            {(resumo.pendenciasLoop ?? 0) > 0 && (
              <div className="mt-8 flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-marca/40 bg-marca-fundo p-4">
                <p className="text-sm text-tinta">
                  <strong>{resumo.pendenciasLoop}</strong> avaliação(ões) negativa(s)
                  aguardando tratamento.
                </p>
                <Button
                  variant="outline"
                  className="h-9 rounded-full px-4"
                  onClick={() => setApenasPendentes(true)}
                >
                  Ver pendências
                </Button>
              </div>
            )}

            <div className="mt-6 grid gap-4 sm:grid-cols-3">
              <div className="rounded-2xl border border-borda bg-superficie p-5">
                <p className="text-xs text-tinta-suave uppercase">Média</p>
                <p className="mt-1 text-3xl font-bold text-tinta">
                  {(resumo.mediaEstrelas ?? 0).toLocaleString("pt-BR", {
                    minimumFractionDigits: 1,
                  })}
                </p>
                <p className="mt-1 text-sm text-tinta-suave">
                  {resumo.total} avaliação(ões)
                </p>
              </div>

              <div className="rounded-2xl border border-borda bg-superficie p-5">
                <p className="text-xs text-tinta-suave uppercase">NPS</p>
                <p className={`mt-1 text-3xl font-bold ${corDoNps(resumo.nps?.score ?? 0)}`}>
                  {(resumo.nps?.score ?? 0) > 0 ? "+" : ""}
                  {resumo.nps?.score ?? 0}
                </p>
                <p className="mt-1 text-sm text-tinta-suave">
                  {resumo.nps?.promotores} promotores · {resumo.nps?.detratores} detratores
                </p>
              </div>

              <div className="rounded-2xl border border-borda bg-superficie p-5">
                <p className="text-xs text-tinta-suave uppercase">Distribuição</p>
                <div className="mt-2 space-y-1">
                  {resumo.distribuicao?.map((d) => (
                    <div key={d.estrelas} className="flex items-center gap-2 text-xs">
                      <span className="w-3 text-tinta-suave">{d.estrelas}</span>
                      <span className="h-2 flex-1 rounded-full bg-sutil">
                        <span
                          className="block h-2 rounded-full bg-marca"
                          style={{
                            width: `${((d.quantidade ?? 0) / maiorFaixa) * 100}%`,
                          }}
                        />
                      </span>
                      <span className="w-4 text-right text-tinta-suave">{d.quantidade}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            {(resumo.porTecnico?.length ?? 0) > 0 && (
              <div className="mt-6 overflow-x-auto rounded-2xl border border-borda">
                <table className="w-full text-left text-sm">
                  <thead className="bg-sutil text-xs text-tinta-suave uppercase">
                    <tr>
                      <th className="px-4 py-3">Técnico</th>
                      <th className="px-4 py-3 text-right">Avaliações</th>
                      <th className="px-4 py-3 text-right">Média</th>
                      <th className="px-4 py-3 text-right">NPS</th>
                    </tr>
                  </thead>
                  <tbody>
                    {resumo.porTecnico?.map((t) => (
                      <tr key={t.tecnicoId} className="border-t border-borda">
                        <td className="px-4 py-3 font-medium text-tinta">
                          {t.tecnicoNome}
                        </td>
                        <td className="px-4 py-3 text-right text-tinta-suave">{t.total}</td>
                        <td className="px-4 py-3 text-right text-tinta">
                          {(t.mediaEstrelas ?? 0).toLocaleString("pt-BR", {
                            minimumFractionDigits: 1,
                          })}
                        </td>
                        <td className={`px-4 py-3 text-right font-semibold ${corDoNps(t.nps ?? 0)}`}>
                          {(t.nps ?? 0) > 0 ? "+" : ""}
                          {t.nps}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )
      )}

      <div className="mt-8 flex items-center justify-between">
        <h2 className="text-lg font-semibold text-tinta">
          {apenasPendentes ? "Pendências de tratamento" : "Todas as avaliações"}
        </h2>
        <label className="flex items-center gap-2 text-sm text-tinta-suave">
          <input
            type="checkbox"
            checked={apenasPendentes}
            onChange={(e) => setApenasPendentes(e.target.checked)}
          />
          Só negativas em aberto
        </label>
      </div>

      <div className="mt-3 space-y-3">
        {avaliacoes.length === 0 && (
          <p className="rounded-2xl border border-borda bg-superficie p-6 text-sm text-tinta-suave">
            {apenasPendentes
              ? "Nenhuma avaliação negativa aguardando tratamento. 🎉"
              : "Nenhuma avaliação ainda."}
          </p>
        )}

        {avaliacoes.map((a: AvaliacaoResponse) => (
          <article
            key={a.id}
            className={`rounded-2xl border bg-superficie p-5 ${
              a.negativa && !a.resolvida
                ? "border-marca/40"
                : "border-borda"
            }`}
          >
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="flex items-center gap-3">
                <Estrelas nota={a.nota ?? 0} />
                <span className="text-sm text-tinta-suave">
                  recomenda {a.recomendacao}/10
                </span>
                {a.negativa && !a.resolvida && (
                  <span className="rounded-full bg-marca-fundo px-2 py-0.5 text-[10px] font-semibold text-marca uppercase">
                    tratar
                  </span>
                )}
                {a.resolvida && (
                  <span className="rounded-full bg-ok-fundo px-2 py-0.5 text-[10px] font-semibold text-ok uppercase">
                    resolvida
                  </span>
                )}
              </div>
              <span className="text-xs text-tinta-fraca">
                OS #{a.ordemServicoNumero} ·{" "}
                {new Date(a.criadoEm ?? "").toLocaleDateString("pt-BR")}
              </span>
            </div>

            <p className="mt-1 text-xs text-tinta-fraca">
              {a.clienteNome ?? "Cliente"} · {a.servicoNome}
              {a.tecnicoNome && ` · técnico ${a.tecnicoNome}`}
            </p>

            {a.comentario && (
              <p className="mt-3 rounded-xl bg-sutil p-3 text-sm text-tinta">
                “{a.comentario}”
              </p>
            )}

            {a.resolvida && a.resolucaoNota && (
              <p className="mt-3 text-sm text-tinta-suave">
                <strong className="text-tinta">Como foi tratado:</strong>{" "}
                {a.resolucaoNota}
              </p>
            )}

            {a.negativa && !a.resolvida && (
              <div className="mt-3">
                {resolvendoId === a.id ? (
                  <div className="flex flex-wrap gap-2">
                    <Input
                      autoFocus
                      placeholder="Como a loja tratou o problema?"
                      value={notaResolucao}
                      maxLength={1000}
                      onChange={(e) => setNotaResolucao(e.target.value)}
                      className="h-10 min-w-64 flex-1"
                    />
                    <Button
                      disabled={resolver.isPending || !notaResolucao.trim()}
                      onClick={() => aoResolver(a.id!)}
                      className="h-10 rounded-full bg-tinta px-5 text-sobre-tinta hover:bg-tinta/90"
                    >
                      Registrar
                    </Button>
                    <Button
                      variant="ghost"
                      className="h-10 px-3"
                      onClick={() => setResolvendoId(null)}
                    >
                      Cancelar
                    </Button>
                  </div>
                ) : (
                  <Button
                    variant="outline"
                    className="h-9 rounded-full px-4"
                    onClick={() => {
                      setResolvendoId(a.id ?? null);
                      setNotaResolucao("");
                    }}
                  >
                    Marcar como tratada
                  </Button>
                )}
              </div>
            )}
          </article>
        ))}
      </div>
    </div>
  );
}
