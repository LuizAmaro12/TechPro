"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiOrdensServicoId,
  useGetApiPecas,
  usePostApiOrdensServicoIdComentarios,
  usePostApiOrdensServicoIdEtapa,
  usePostApiOrdensServicoIdPecas,
  usePutApiOrdensServicoIdChecklistItemId,
  useDeleteApiOrdensServicoIdPecasPecaUsadaId,
  type OrdemServicoResponse,
} from "@/lib/api-client/gerado";
import { ETAPAS_OS } from "@/lib/ordens-servico-etapas";

/**
 * Detalhe enxuto para a bancada: mover etapa, marcar o checklist técnico,
 * registrar peça usada (baixa automática) e comentar. **Sem** orçamento,
 * pagamento ou margem — o técnico não precisa disso para executar o serviço.
 */
export function OrdemDaBancada({
  ordemId,
  resumo,
  aoFechar,
}: {
  ordemId: string;
  resumo: OrdemServicoResponse;
  aoFechar: () => void;
}) {
  const queryClient = useQueryClient();
  const { data: resposta, refetch } = useGetApiOrdensServicoId(ordemId);
  const detalhe = resposta?.status === 200 ? resposta.data : null;

  const { data: respPecas } = useGetApiPecas({ tamanhoPagina: 100 });
  const pecas = respPecas?.status === 200 ? (respPecas.data.itens ?? []) : [];

  const moverEtapa = usePostApiOrdensServicoIdEtapa();
  const marcarItem = usePutApiOrdensServicoIdChecklistItemId();
  const adicionarPeca = usePostApiOrdensServicoIdPecas();
  const removerPeca = useDeleteApiOrdensServicoIdPecasPecaUsadaId();
  const comentar = usePostApiOrdensServicoIdComentarios();

  const [pecaId, setPecaId] = useState("");
  const [quantidade, setQuantidade] = useState("1");
  const [texto, setTexto] = useState("");

  const aparelho = [resumo.aparelhoMarca, resumo.aparelhoModelo].filter(Boolean).join(" ");

  function reportar(erro: unknown, padrao: string) {
    toast.error(erro instanceof ApiError ? erro.message : padrao);
  }

  async function recarregar() {
    await refetch();
    queryClient.invalidateQueries({ queryKey: ["/api/ordens-servico"] });
  }

  async function aoMover(etapa: string) {
    try {
      await moverEtapa.mutateAsync({ id: ordemId, data: { paraEtapa: etapa as never, motivo: null } });
      await recarregar();
      toast.success("Etapa atualizada.");
    } catch (erro) {
      reportar(erro, "Não foi possível mover a etapa.");
    }
  }

  async function aoMarcar(itemId: string, concluido: boolean) {
    try {
      await marcarItem.mutateAsync({ id: ordemId, itemId, data: { concluido } });
      await refetch();
    } catch (erro) {
      reportar(erro, "Não foi possível atualizar o item.");
    }
  }

  async function aoAdicionarPeca(evento: React.FormEvent) {
    evento.preventDefault();
    if (!pecaId) return;
    try {
      await adicionarPeca.mutateAsync({
        id: ordemId,
        data: { pecaId: Number(pecaId), quantidade: Number(quantidade) },
      });
      setPecaId("");
      setQuantidade("1");
      await recarregar();
      toast.success("Peça registrada (baixa no estoque).");
    } catch (erro) {
      reportar(erro, "Não foi possível registrar a peça.");
    }
  }

  async function aoRemoverPeca(pecaUsadaId: string) {
    try {
      await removerPeca.mutateAsync({ id: ordemId, pecaUsadaId });
      await recarregar();
    } catch (erro) {
      reportar(erro, "Não foi possível remover a peça.");
    }
  }

  async function aoComentar(evento: React.FormEvent) {
    evento.preventDefault();
    if (!texto.trim()) return;
    try {
      await comentar.mutateAsync({ id: ordemId, data: { texto: texto.trim() } });
      setTexto("");
      await refetch();
    } catch (erro) {
      reportar(erro, "Não foi possível salvar o comentário.");
    }
  }

  const finalizada = resumo.etapa === "Entregue" || resumo.etapa === "Cancelado";

  return (
    <section className="rounded-2xl border border-borda-forte bg-superficie p-4">
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-semibold text-tinta">
            #{resumo.numero} · {resumo.clienteNome}
          </p>
          <p className="text-sm text-tinta-suave">
            {resumo.servicoNome}
            {aparelho && ` · ${aparelho}`}
          </p>
        </div>
        <Button variant="ghost" className="h-9 px-3" onClick={aoFechar}>
          Fechar
        </Button>
      </div>

      {resumo.descricaoProblema && (
        <p className="mt-2 rounded-xl bg-sutil p-3 text-sm text-tinta">
          {resumo.descricaoProblema}
        </p>
      )}

      {/* Etapa — controle grande, alvo de toque */}
      <div className="mt-4">
        <label htmlFor="etapaBancada" className="text-xs font-semibold text-tinta-suave uppercase">
          Etapa
        </label>
        <select
          id="etapaBancada"
          value={resumo.etapa}
          onChange={(e) => aoMover(e.target.value)}
          disabled={moverEtapa.isPending}
          className="mt-1 h-12 w-full rounded-xl border border-borda bg-superficie px-3 text-base text-tinta"
        >
          {ETAPAS_OS.map((e) => (
            <option key={e.valor} value={e.valor}>
              {e.rotulo}
            </option>
          ))}
        </select>
      </div>

      {/* Checklist técnico */}
      {(detalhe?.checklist?.length ?? 0) > 0 && (
        <div className="mt-4">
          <p className="text-xs font-semibold text-tinta-suave uppercase">Checklist</p>
          <ul className="mt-2 space-y-1">
            {detalhe!.checklist!.map((item) => (
              <li key={item.id}>
                <label className="flex items-center gap-3 rounded-xl px-2 py-2.5 text-sm hover:bg-sutil">
                  <input
                    type="checkbox"
                    checked={item.concluido ?? false}
                    onChange={(e) => aoMarcar(item.id!, e.target.checked)}
                    className="h-5 w-5 shrink-0 accent-[#14162B]"
                  />
                  <span className={item.concluido ? "text-tinta-fraca line-through" : "text-tinta"}>
                    {item.descricao}
                  </span>
                </label>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Peças usadas */}
      <div className="mt-4">
        <p className="text-xs font-semibold text-tinta-suave uppercase">Peças usadas</p>
        {(detalhe?.pecas?.length ?? 0) > 0 && (
          <ul className="mt-2 space-y-1">
            {detalhe!.pecas!.map((linha) => (
              <li
                key={linha.id}
                className="flex items-center justify-between gap-2 rounded-xl bg-sutil px-3 py-2 text-sm"
              >
                <span className="text-tinta">
                  {linha.quantidade}× {linha.pecaNome}
                  {linha.estoqueNegativo && (
                    <span className="ml-2 text-marca">estoque negativo</span>
                  )}
                </span>
                {!finalizada && (
                  <button
                    type="button"
                    onClick={() => aoRemoverPeca(linha.id!)}
                    className="text-xs text-tinta-fraca underline hover:text-marca"
                  >
                    remover
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}
        {!finalizada && (
          <form onSubmit={aoAdicionarPeca} className="mt-2 flex flex-wrap gap-2">
            <select
              aria-label="Peça"
              value={pecaId}
              onChange={(e) => setPecaId(e.target.value)}
              className="h-11 min-w-40 flex-1 rounded-xl border border-borda bg-superficie px-2 text-sm text-tinta"
            >
              <option value="">Selecionar peça...</option>
              {pecas.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.nome} ({p.quantidadeEmEstoque} em estoque)
                </option>
              ))}
            </select>
            <Input
              aria-label="Quantidade"
              type="number"
              min="1"
              value={quantidade}
              onChange={(e) => setQuantidade(e.target.value)}
              className="h-11 w-20"
            />
            <Button
              type="submit"
              disabled={adicionarPeca.isPending || !pecaId}
              className="h-11 rounded-full bg-tinta px-5 text-sobre-tinta hover:bg-tinta/90"
            >
              Adicionar
            </Button>
          </form>
        )}
      </div>

      {/* Comentário interno */}
      <div className="mt-4">
        <p className="text-xs font-semibold text-tinta-suave uppercase">Comentários internos</p>
        {(detalhe?.comentarios?.length ?? 0) > 0 && (
          <ul className="mt-2 space-y-1">
            {detalhe!.comentarios!.map((c) => (
              <li key={c.id} className="rounded-xl bg-sutil px-3 py-2 text-sm">
                <p className="text-tinta">{c.texto}</p>
                <p className="text-xs text-tinta-fraca">{c.autorNome ?? "equipe"}</p>
              </li>
            ))}
          </ul>
        )}
        <form onSubmit={aoComentar} className="mt-2 flex gap-2">
          <Input
            aria-label="Novo comentário"
            placeholder="Anotar algo sobre o reparo..."
            value={texto}
            maxLength={2000}
            onChange={(e) => setTexto(e.target.value)}
            className="h-11 flex-1"
          />
          <Button
            type="submit"
            disabled={comentar.isPending || !texto.trim()}
            className="h-11 rounded-full bg-tinta px-5 text-sobre-tinta hover:bg-tinta/90"
          >
            Anotar
          </Button>
        </form>
      </div>
    </section>
  );
}
