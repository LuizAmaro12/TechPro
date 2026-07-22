"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useDeleteApiOrdensServicoIdComentariosComentarioId,
  usePostApiOrdensServicoIdComentarios,
  usePostApiOrdensServicoIdResponsavel,
  type ComentarioResponse,
  type EquipeMembroResponse,
  type ReatribuicaoResponse,
} from "@/lib/api-client/gerado";

const DATA_CURTA: Intl.DateTimeFormatOptions = {
  day: "2-digit",
  month: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
};

function quando(valor: string | undefined) {
  return new Date(valor ?? "").toLocaleString("pt-BR", DATA_CURTA);
}

/**
 * Comentários internos e troca de responsável — as duas coisas que fazem a OS
 * responder depois "por que foi feito assim e por quem". O componente recebe o
 * detalhe já carregado e avisa o pai para revalidar.
 */
export function InteracoesOs({
  ordemId,
  responsavelAtualId,
  comentarios,
  reatribuicoes,
  equipe,
  aoMudar,
}: {
  ordemId: string;
  responsavelAtualId: string | null;
  comentarios: ComentarioResponse[];
  reatribuicoes: ReatribuicaoResponse[];
  equipe: EquipeMembroResponse[];
  aoMudar: () => void;
}) {
  const [texto, setTexto] = useState("");
  const [novoResponsavel, setNovoResponsavel] = useState(responsavelAtualId ?? "");
  const [motivo, setMotivo] = useState("");

  const comentar = usePostApiOrdensServicoIdComentarios();
  const removerComentario = useDeleteApiOrdensServicoIdComentariosComentarioId();
  const reatribuir = usePostApiOrdensServicoIdResponsavel();

  function reportar(erro: unknown, padrao: string) {
    toast.error(erro instanceof ApiError ? erro.message : padrao);
  }

  async function aoComentar(evento: React.FormEvent) {
    evento.preventDefault();
    if (!texto.trim()) return;
    try {
      await comentar.mutateAsync({ id: ordemId, data: { texto: texto.trim() } });
      setTexto("");
      aoMudar();
    } catch (erro) {
      reportar(erro, "Não foi possível salvar o comentário.");
    }
  }

  async function aoRemover(comentarioId: string) {
    try {
      await removerComentario.mutateAsync({ id: ordemId, comentarioId });
      aoMudar();
    } catch (erro) {
      reportar(erro, "Não foi possível remover o comentário.");
    }
  }

  async function aoReatribuir(evento: React.FormEvent) {
    evento.preventDefault();
    try {
      await reatribuir.mutateAsync({
        id: ordemId,
        data: { responsavelTecnicoId: novoResponsavel || null, motivo: motivo.trim() },
      });
      setMotivo("");
      toast.success("Responsável atualizado.");
      aoMudar();
    } catch (erro) {
      reportar(erro, "Não foi possível trocar o responsável.");
    }
  }

  return (
    <>
      <div className="mt-6 border-t border-borda pt-4">
        <h3 className="text-sm font-semibold text-tinta">Comentários internos</h3>
        <p className="mt-1 text-xs text-tinta-fraca">
          Só a equipe vê — o cliente nunca enxerga isto no link de acompanhamento.
        </p>

        <form onSubmit={aoComentar} className="mt-3 flex flex-wrap gap-2">
          <Input
            aria-label="Novo comentário interno"
            placeholder="Ex.: cliente autorizou trocar o conector por telefone"
            value={texto}
            maxLength={2000}
            onChange={(e) => setTexto(e.target.value)}
            className="h-10 min-w-64 flex-1"
          />
          <Button
            type="submit"
            disabled={comentar.isPending || !texto.trim()}
            className="h-10 rounded-full bg-tinta px-5 text-sobre-tinta hover:bg-tinta/90"
          >
            Comentar
          </Button>
        </form>

        {comentarios.length === 0 ? (
          <p className="mt-3 text-sm text-tinta-fraca">Nenhum comentário ainda.</p>
        ) : (
          <ul className="mt-3 space-y-2">
            {comentarios.map((c) => (
              <li
                key={c.id}
                className="flex items-start justify-between gap-3 rounded-xl bg-sutil px-3 py-2 text-sm"
              >
                <div>
                  <p className="text-tinta">{c.texto}</p>
                  <p className="mt-0.5 text-xs text-tinta-fraca">
                    {c.autorNome ?? "sistema"} · {quando(c.criadoEm)}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => aoRemover(c.id!)}
                  className="shrink-0 text-xs text-tinta-fraca underline hover:text-marca"
                >
                  remover
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="mt-6 border-t border-borda pt-4">
        <h3 className="text-sm font-semibold text-tinta">Responsável técnico</h3>
        <p className="mt-1 text-xs text-tinta-fraca">
          Toda troca fica registrada com o motivo — é o que responde a quem mexeu
          no aparelho se o cliente questionar.
        </p>

        <form onSubmit={aoReatribuir} className="mt-3 flex flex-wrap items-end gap-2">
          <div className="min-w-48">
            <Label htmlFor="novoResponsavel">Passar para</Label>
            <select
              id="novoResponsavel"
              value={novoResponsavel}
              onChange={(e) => setNovoResponsavel(e.target.value)}
              className="mt-1 h-10 w-full rounded-md border border-borda bg-superficie px-2 text-sm"
            >
              <option value="">Sem responsável</option>
              {equipe.map((m) => (
                <option key={m.id} value={m.id}>
                  {m.nome}
                </option>
              ))}
            </select>
          </div>
          <div className="min-w-64 flex-1">
            <Label htmlFor="motivoReatribuicao">Motivo</Label>
            <Input
              id="motivoReatribuicao"
              placeholder="Ex.: técnico de férias"
              value={motivo}
              maxLength={500}
              onChange={(e) => setMotivo(e.target.value)}
              className="mt-1 h-10"
            />
          </div>
          <Button
            type="submit"
            disabled={reatribuir.isPending || !motivo.trim()}
            className="h-10 rounded-full bg-tinta px-5 text-sobre-tinta hover:bg-tinta/90"
          >
            Trocar
          </Button>
        </form>

        {reatribuicoes.length > 0 && (
          <ul className="mt-3 space-y-1 text-sm text-tinta-suave">
            {reatribuicoes.map((r) => (
              <li key={r.id}>
                {quando(r.criadoEm)} — {r.deNome ?? "sem responsável"} →{" "}
                <span className="font-medium text-tinta">
                  {r.paraNome ?? "sem responsável"}
                </span>
                {` · "${r.motivo}"`}
                {r.porNome && ` · por ${r.porNome}`}
              </li>
            ))}
          </ul>
        )}
      </div>
    </>
  );
}
