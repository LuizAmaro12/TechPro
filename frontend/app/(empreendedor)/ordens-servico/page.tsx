"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiAgendaConfiguracoes,
  useGetApiClientes,
  useGetApiEquipe,
  useDeleteApiOrdensServicoIdPecasPecaUsadaId,
  useGetApiOrdensServico,
  useGetApiOrdensServicoId,
  useGetApiPecas,
  useGetApiServicos,
  usePostApiOrdensServico,
  usePostApiOrdensServicoIdPecas,
  usePostApiOrdensServicoIdPecasAplicarPadrao,
  usePutApiOrdensServicoId,
  type OrdemServicoResponse,
  type PecaUsadaResponse,
} from "@/lib/api-client/gerado";
import { formatarBRL } from "@/lib/formatadores";
import { formatarDataCurta } from "@/lib/agenda-datas";
import {
  ROTULOS_APROVACAO,
  ROTULOS_PAGAMENTO,
  ROTULOS_PRIORIDADE,
  rotuloDaEtapa,
} from "@/lib/ordens-servico-etapas";
import {
  esquemaEdicaoOrdemServico,
  esquemaOrdemServico,
  type ValoresEdicaoOrdemServico,
  type ValoresOrdemServico,
} from "@/lib/validators/ordens-servico";

function CampoErro({ mensagem }: { mensagem?: string }) {
  return mensagem ? <p className="mt-1 text-sm text-destructive">{mensagem}</p> : null;
}

const VALORES_INICIAIS: ValoresOrdemServico = {
  clienteId: "",
  servicoId: "",
  aparelhoMarca: "",
  aparelhoModelo: "",
  descricaoProblema: "",
  prioridade: "Normal",
  prazoEstimado: "",
  responsavelTecnicoId: "",
  observacoes: "",
};

export default function PaginaOrdensServico() {
  const queryClient = useQueryClient();
  const [busca, setBusca] = useState("");
  const [incluirFinalizadas, setIncluirFinalizadas] = useState(false);
  const [formAberto, setFormAberto] = useState(false);
  const [editandoId, setEditandoId] = useState<string | null>(null);

  const { data: respostaOrdens } = useGetApiOrdensServico({
    busca: busca || undefined,
    incluirFinalizadas: incluirFinalizadas || undefined,
  });
  const ordens = respostaOrdens?.status === 200 ? respostaOrdens.data : undefined;

  const { data: respostaClientes } = useGetApiClientes({ tamanhoPagina: 100 });
  const clientes =
    respostaClientes?.status === 200 ? (respostaClientes.data.itens ?? []) : [];
  const { data: respostaServicos } = useGetApiServicos({ tamanhoPagina: 100 });
  const servicos =
    respostaServicos?.status === 200 ? (respostaServicos.data.itens ?? []) : [];
  const { data: respostaEquipe } = useGetApiEquipe();
  const equipe = respostaEquipe?.status === 200 ? respostaEquipe.data : [];
  const { data: respostaConfiguracao } = useGetApiAgendaConfiguracoes();
  const slug =
    respostaConfiguracao?.status === 200 ? respostaConfiguracao.data.slug : null;

  const criar = usePostApiOrdensServico();
  const atualizar = usePutApiOrdensServicoId();
  const adicionarPeca = usePostApiOrdensServicoIdPecas();
  const aplicarPecasPadrao = usePostApiOrdensServicoIdPecasAplicarPadrao();
  const removerPeca = useDeleteApiOrdensServicoIdPecasPecaUsadaId();
  const [novaPecaId, setNovaPecaId] = useState("");
  const [novaQuantidade, setNovaQuantidade] = useState("1");

  const { data: respostaPecas } = useGetApiPecas({ tamanhoPagina: 100 });
  const pecasCatalogo =
    respostaPecas?.status === 200 ? (respostaPecas.data.itens ?? []) : [];

  const formCriacao = useForm<ValoresOrdemServico>({
    resolver: zodResolver(esquemaOrdemServico),
    defaultValues: VALORES_INICIAIS,
  });

  const formEdicao = useForm<ValoresEdicaoOrdemServico>({
    resolver: zodResolver(esquemaEdicaoOrdemServico),
    defaultValues: {
      ...VALORES_INICIAIS,
      statusPagamento: "NaoPago",
      statusAprovacao: "Pendente",
    },
  });

  const { data: respostaDetalhe } = useGetApiOrdensServicoId(editandoId ?? "", {
    query: { enabled: editandoId !== null },
  });
  const detalhe = respostaDetalhe?.status === 200 ? respostaDetalhe.data : null;

  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ["/api/ordens-servico"] });
  }

  function invalidarPecas() {
    invalidar();
    queryClient.invalidateQueries({ queryKey: [`/api/ordens-servico/${editandoId}`] });
    queryClient.invalidateQueries({ queryKey: ["/api/pecas"] });
  }

  // Decisão 2026-07-15: estoque negativo é permitido — a UI avisa, não bloqueia.
  function avisarEstoque(linha: PecaUsadaResponse) {
    if (linha.estoqueNegativo) {
      toast.warning(
        `Estoque de ${linha.pecaNome} ficou negativo (${linha.estoqueRestante}) — ajuste a contagem no catálogo.`,
      );
    } else if (linha.estoqueAbaixoDoMinimo) {
      toast.warning(
        `${linha.pecaNome} ficou no estoque mínimo ou abaixo (${linha.estoqueRestante} restantes).`,
      );
    }
  }

  async function aoAdicionarPeca() {
    if (editandoId === null || !novaPecaId) return;
    try {
      const resposta = await adicionarPeca.mutateAsync({
        id: editandoId,
        data: { pecaId: Number(novaPecaId), quantidade: Number(novaQuantidade) || 1 },
      });
      if (resposta.status === 201) {
        toast.success("Peça registrada — baixa feita no estoque.");
        avisarEstoque(resposta.data);
      }
      setNovaPecaId("");
      setNovaQuantidade("1");
      invalidarPecas();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao registrar a peça.");
    }
  }

  async function aoAplicarPadrao() {
    if (editandoId === null) return;
    try {
      const resposta = await aplicarPecasPadrao.mutateAsync({ id: editandoId });
      if (resposta.status === 200) {
        if (resposta.data.length === 0) {
          toast.info("As peças padrão do serviço já estão na OS (ou não há nenhuma).");
        } else {
          toast.success(`${resposta.data.length} peça(s) do serviço aplicadas.`);
          resposta.data.forEach(avisarEstoque);
        }
      }
      invalidarPecas();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao aplicar as peças.");
    }
  }

  async function aoRemoverPeca(pecaUsadaId: string | undefined) {
    if (editandoId === null || !pecaUsadaId) return;
    try {
      await removerPeca.mutateAsync({ id: editandoId, pecaUsadaId });
      toast.success("Peça removida — estoque devolvido.");
      invalidarPecas();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao remover a peça.");
    }
  }

  function abrirCriacao() {
    setEditandoId(null);
    formCriacao.reset(VALORES_INICIAIS);
    setFormAberto(true);
  }

  function abrirEdicao(ordem: OrdemServicoResponse) {
    setFormAberto(false);
    setEditandoId(ordem.id ?? null);
    formEdicao.reset({
      aparelhoMarca: ordem.aparelhoMarca ?? "",
      aparelhoModelo: ordem.aparelhoModelo ?? "",
      descricaoProblema: ordem.descricaoProblema ?? "",
      prioridade: ordem.prioridade ?? "Normal",
      prazoEstimado: ordem.prazoEstimado ?? "",
      responsavelTecnicoId: ordem.responsavelTecnicoId ?? "",
      observacoes: ordem.observacoes ?? "",
      statusPagamento: ordem.statusPagamento ?? "NaoPago",
      statusAprovacao: ordem.statusAprovacao ?? "Pendente",
    });
  }

  async function aoCriar(valores: ValoresOrdemServico) {
    try {
      const resposta = await criar.mutateAsync({
        data: {
          clienteId: Number(valores.clienteId),
          servicoId: Number(valores.servicoId),
          aparelhoId: null,
          aparelhoMarca: valores.aparelhoMarca || null,
          aparelhoModelo: valores.aparelhoModelo || null,
          descricaoProblema: valores.descricaoProblema || null,
          prioridade: valores.prioridade as never,
          prazoEstimado: valores.prazoEstimado || null,
          responsavelTecnicoId: valores.responsavelTecnicoId || null,
          observacoes: valores.observacoes || null,
        },
      });
      if (resposta.status === 201) {
        toast.success(`OS #${resposta.data.numero} criada.`);
      }
      invalidar();
      setFormAberto(false);
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao criar a OS.");
    }
  }

  async function aoSalvarEdicao(valores: ValoresEdicaoOrdemServico) {
    if (editandoId === null) return;
    try {
      await atualizar.mutateAsync({
        id: editandoId,
        data: {
          aparelhoId: null,
          aparelhoMarca: valores.aparelhoMarca || null,
          aparelhoModelo: valores.aparelhoModelo || null,
          descricaoProblema: valores.descricaoProblema || null,
          prioridade: valores.prioridade as never,
          prazoEstimado: valores.prazoEstimado || null,
          responsavelTecnicoId: valores.responsavelTecnicoId || null,
          statusPagamento: valores.statusPagamento as never,
          statusAprovacao: valores.statusAprovacao as never,
          observacoes: valores.observacoes || null,
        },
      });
      toast.success("OS atualizada.");
      invalidar();
      queryClient.invalidateQueries({ queryKey: [`/api/ordens-servico/${editandoId}`] });
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar a OS.");
    }
  }

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <div className="flex items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
            Ordens de serviço
          </p>
          <h1 className="mt-2 text-3xl font-bold text-[#14162B]">Ordens de serviço</h1>
          <p className="mt-1 text-sm text-[#6B7280]">
            O registro único de cada aparelho, do check-in à entrega. Para mover
            etapas visualmente, use o{" "}
            <Link href="/kanban" className="underline underline-offset-4">
              Kanban
            </Link>
            .
          </p>
        </div>
        <Button
          onClick={abrirCriacao}
          className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
        >
          Nova OS
        </Button>
      </div>

      {formAberto && (
        <form
          onSubmit={formCriacao.handleSubmit(aoCriar)}
          className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6"
        >
          <h2 className="text-lg font-semibold text-[#14162B]">Nova ordem de serviço</h2>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <div>
              <Label htmlFor="clienteId">Cliente</Label>
              <select
                id="clienteId"
                className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                aria-invalid={!!formCriacao.formState.errors.clienteId}
                {...formCriacao.register("clienteId")}
              >
                <option value="">Escolha o cliente...</option>
                {clientes.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.nome} — {c.telefone}
                  </option>
                ))}
              </select>
              <CampoErro mensagem={formCriacao.formState.errors.clienteId?.message} />
            </div>
            <div>
              <Label htmlFor="servicoId">Serviço</Label>
              <select
                id="servicoId"
                className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                aria-invalid={!!formCriacao.formState.errors.servicoId}
                {...formCriacao.register("servicoId")}
              >
                <option value="">Escolha o serviço...</option>
                {servicos
                  .filter((s) => s.ativo)
                  .map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.nome}
                    </option>
                  ))}
              </select>
              <CampoErro mensagem={formCriacao.formState.errors.servicoId?.message} />
            </div>
            <div>
              <Label htmlFor="aparelhoMarca">Marca do aparelho</Label>
              <Input
                id="aparelhoMarca"
                className="mt-1 h-11"
                {...formCriacao.register("aparelhoMarca")}
              />
            </div>
            <div>
              <Label htmlFor="aparelhoModelo">Modelo</Label>
              <Input
                id="aparelhoModelo"
                className="mt-1 h-11"
                {...formCriacao.register("aparelhoModelo")}
              />
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="descricaoProblema">Problema relatado</Label>
              <Input
                id="descricaoProblema"
                className="mt-1 h-11"
                {...formCriacao.register("descricaoProblema")}
              />
            </div>
            <div>
              <Label htmlFor="prioridade">Prioridade</Label>
              <select
                id="prioridade"
                className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                {...formCriacao.register("prioridade")}
              >
                {Object.entries(ROTULOS_PRIORIDADE).map(([valor, rotulo]) => (
                  <option key={valor} value={valor}>
                    {rotulo}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <Label htmlFor="prazoEstimado">Prazo estimado</Label>
              <Input
                id="prazoEstimado"
                type="date"
                className="mt-1 h-11"
                {...formCriacao.register("prazoEstimado")}
              />
            </div>
            <div>
              <Label htmlFor="responsavelTecnicoId">Responsável técnico</Label>
              <select
                id="responsavelTecnicoId"
                className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                {...formCriacao.register("responsavelTecnicoId")}
              >
                <option value="">Sem responsável</option>
                {equipe.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.nome}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <Label htmlFor="observacoes">Observações</Label>
              <Input
                id="observacoes"
                className="mt-1 h-11"
                {...formCriacao.register("observacoes")}
              />
            </div>
          </div>
          <div className="mt-6 flex gap-3">
            <Button
              type="submit"
              disabled={formCriacao.formState.isSubmitting}
              className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
            >
              {formCriacao.formState.isSubmitting ? "Criando..." : "Criar OS"}
            </Button>
            <Button type="button" variant="ghost" onClick={() => setFormAberto(false)}>
              Fechar
            </Button>
          </div>
        </form>
      )}

      {editandoId !== null && detalhe && (
        <div className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 className="text-lg font-semibold text-[#14162B]">
              OS #{detalhe.ordem?.numero} — {detalhe.ordem?.clienteNome}
            </h2>
            <span className="rounded-full bg-[#14162B]/5 px-3 py-1 text-xs font-semibold text-[#14162B]">
              {rotuloDaEtapa(detalhe.ordem?.etapa)}
            </span>
          </div>
          <p className="mt-1 text-sm text-[#6B7280]">
            {detalhe.ordem?.servicoNome}
            {detalhe.ordem?.aparelhoMarca &&
              ` · ${detalhe.ordem.aparelhoMarca} ${detalhe.ordem.aparelhoModelo ?? ""}`}
          </p>

          {slug && detalhe.ordem?.codigoAcompanhamento && (
            <div className="mt-3 flex flex-wrap items-center gap-2 text-sm">
              <span className="text-[#6B7280]">Link de acompanhamento:</span>
              <code className="rounded-lg bg-[#F7F7F9] px-3 py-1.5 text-xs text-[#14162B]">
                {`${window.location.origin}/acompanhar/${slug}/${detalhe.ordem.codigoAcompanhamento}`}
              </code>
              <Button
                type="button"
                variant="outline"
                className="h-8 rounded-full px-3 text-xs"
                onClick={() => {
                  navigator.clipboard.writeText(
                    `${window.location.origin}/acompanhar/${slug}/${detalhe.ordem!.codigoAcompanhamento}`,
                  );
                  toast.success("Link copiado.");
                }}
              >
                Copiar
              </Button>
            </div>
          )}

          <form onSubmit={formEdicao.handleSubmit(aoSalvarEdicao)} className="mt-4">
            <div className="grid gap-4 sm:grid-cols-3">
              <div>
                <Label htmlFor="edicaoPrioridade">Prioridade</Label>
                <select
                  id="edicaoPrioridade"
                  className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                  {...formEdicao.register("prioridade")}
                >
                  {Object.entries(ROTULOS_PRIORIDADE).map(([valor, rotulo]) => (
                    <option key={valor} value={valor}>
                      {rotulo}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <Label htmlFor="edicaoPrazo">Prazo estimado</Label>
                <Input
                  id="edicaoPrazo"
                  type="date"
                  className="mt-1 h-11"
                  {...formEdicao.register("prazoEstimado")}
                />
              </div>
              <div>
                <Label htmlFor="edicaoResponsavel">Responsável técnico</Label>
                <select
                  id="edicaoResponsavel"
                  className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                  {...formEdicao.register("responsavelTecnicoId")}
                >
                  <option value="">Sem responsável</option>
                  {equipe.map((m) => (
                    <option key={m.id} value={m.id}>
                      {m.nome}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <Label htmlFor="edicaoPagamento">Pagamento</Label>
                <select
                  id="edicaoPagamento"
                  className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                  {...formEdicao.register("statusPagamento")}
                >
                  {Object.entries(ROTULOS_PAGAMENTO).map(([valor, rotulo]) => (
                    <option key={valor} value={valor}>
                      {rotulo}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <Label htmlFor="edicaoAprovacao">Aprovação do orçamento</Label>
                <select
                  id="edicaoAprovacao"
                  className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                  {...formEdicao.register("statusAprovacao")}
                >
                  {Object.entries(ROTULOS_APROVACAO).map(([valor, rotulo]) => (
                    <option key={valor} value={valor}>
                      {rotulo}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <Label htmlFor="edicaoObservacoes">Observações</Label>
                <Input
                  id="edicaoObservacoes"
                  className="mt-1 h-11"
                  {...formEdicao.register("observacoes")}
                />
              </div>
            </div>

            <div className="mt-6 flex gap-3">
              <Button
                type="submit"
                disabled={formEdicao.formState.isSubmitting}
                className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
              >
                {formEdicao.formState.isSubmitting ? "Salvando..." : "Salvar OS"}
              </Button>
              <Button type="button" variant="ghost" onClick={() => setEditandoId(null)}>
                Fechar
              </Button>
            </div>
          </form>

          <div className="mt-6 border-t border-[#14162B]/6 pt-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h3 className="text-sm font-semibold text-[#14162B]">Peças utilizadas</h3>
              {detalhe.ordem?.etapa !== "Entregue" && detalhe.ordem?.etapa !== "Cancelado" && (
                <Button
                  type="button"
                  variant="outline"
                  className="h-8 rounded-full px-3 text-xs"
                  disabled={aplicarPecasPadrao.isPending}
                  onClick={aoAplicarPadrao}
                >
                  Aplicar peças padrão do serviço
                </Button>
              )}
            </div>

            {detalhe.ordem?.etapa !== "Entregue" && detalhe.ordem?.etapa !== "Cancelado" && (
              <div className="mt-3 flex flex-wrap items-end gap-2">
                <select
                  aria-label="Peça"
                  value={novaPecaId}
                  onChange={(e) => setNovaPecaId(e.target.value)}
                  className="h-10 min-w-64 rounded-md border border-input bg-white px-3 text-sm"
                >
                  <option value="">Escolha a peça...</option>
                  {pecasCatalogo
                    .filter((p) => p.ativo)
                    .map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.nome} — {p.quantidadeEmEstoque} em estoque
                      </option>
                    ))}
                </select>
                <Input
                  aria-label="Quantidade"
                  type="number"
                  min={1}
                  max={999}
                  value={novaQuantidade}
                  onChange={(e) => setNovaQuantidade(e.target.value)}
                  className="h-10 w-20"
                />
                <Button
                  type="button"
                  variant="outline"
                  className="h-10 rounded-full px-4"
                  disabled={!novaPecaId || adicionarPeca.isPending}
                  onClick={aoAdicionarPeca}
                >
                  Adicionar peça
                </Button>
              </div>
            )}

            {(detalhe.pecas?.length ?? 0) === 0 ? (
              <p className="mt-3 text-sm text-[#8B8D98]">
                Nenhuma peça registrada nesta OS.
              </p>
            ) : (
              <div className="mt-3 space-y-1">
                {detalhe.pecas?.map((linha) => (
                  <div
                    key={linha.id}
                    className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-[#14162B]/6 px-3 py-2 text-sm"
                  >
                    <span className="text-[#14162B]">
                      <span className="font-medium">{linha.quantidade}×</span>{" "}
                      {linha.pecaNome}
                      <span className="ml-2 text-xs text-[#8B8D98]">
                        custo {formatarBRL(linha.custoUnitarioNoUso ?? 0)} · venda{" "}
                        {formatarBRL(linha.precoVendaNoUso ?? 0)}
                      </span>
                    </span>
                    {detalhe.ordem?.etapa !== "Entregue" &&
                      detalhe.ordem?.etapa !== "Cancelado" && (
                        <Button
                          variant="ghost"
                          className="h-7 px-3 text-xs text-[#E8536B] hover:text-[#E8536B]"
                          onClick={() => aoRemoverPeca(linha.id)}
                        >
                          Remover
                        </Button>
                      )}
                  </div>
                ))}
                <p className="pt-1 text-right text-sm text-[#6B7280]">
                  Total em peças (venda):{" "}
                  <span className="font-semibold text-[#14162B]">
                    {formatarBRL(
                      detalhe.pecas?.reduce(
                        (soma, l) =>
                          soma + (l.precoVendaNoUso ?? 0) * (l.quantidade ?? 0),
                        0,
                      ) ?? 0,
                    )}
                  </span>
                </p>
              </div>
            )}
          </div>

          {(detalhe.historico?.length ?? 0) > 0 && (
            <div className="mt-6 border-t border-[#14162B]/6 pt-4">
              <h3 className="text-sm font-semibold text-[#14162B]">Trilha de etapas</h3>
              <ul className="mt-2 space-y-1 text-sm text-[#6B7280]">
                {detalhe.historico?.map((h, i) => (
                  <li key={i}>
                    {new Date(h.criadoEm ?? "").toLocaleString("pt-BR", {
                      day: "2-digit",
                      month: "2-digit",
                      hour: "2-digit",
                      minute: "2-digit",
                    })}{" "}
                    — {h.deEtapa ? `${rotuloDaEtapa(h.deEtapa)} → ` : ""}
                    <span className="font-medium text-[#14162B]">
                      {rotuloDaEtapa(h.paraEtapa)}
                    </span>
                    {h.usuarioNome && ` · ${h.usuarioNome}`}
                    {h.motivo && ` · "${h.motivo}"`}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}

      <div className="mt-8 flex flex-wrap items-center justify-between gap-3">
        <Input
          placeholder="Buscar por nº, nome ou telefone..."
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          className="h-10 max-w-72"
        />
        <label className="flex items-center gap-2 text-sm text-[#6B7280]">
          <input
            type="checkbox"
            checked={incluirFinalizadas}
            onChange={(e) => setIncluirFinalizadas(e.target.checked)}
          />
          Mostrar finalizadas
        </label>
      </div>

      <div className="mt-3 overflow-x-auto rounded-2xl border border-[#14162B]/8">
        <table className="w-full text-left text-sm">
          <thead className="bg-[#F7F7F9] text-xs text-[#8B8D98] uppercase tracking-wide">
            <tr>
              <th className="px-4 py-3">OS</th>
              <th className="px-4 py-3">Cliente</th>
              <th className="px-4 py-3">Serviço</th>
              <th className="px-4 py-3">Etapa</th>
              <th className="px-4 py-3">Prioridade</th>
              <th className="px-4 py-3">Prazo</th>
              <th className="px-4 py-3">Pagamento</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {(ordens ?? []).map((ordem) => (
              <tr key={ordem.id} className="border-t border-[#14162B]/6">
                <td className="px-4 py-3 font-semibold text-[#14162B]">#{ordem.numero}</td>
                <td className="px-4 py-3 text-[#14162B]">{ordem.clienteNome}</td>
                <td className="px-4 py-3 text-[#6B7280]">{ordem.servicoNome}</td>
                <td className="px-4 py-3">
                  <span className="rounded-full bg-[#14162B]/5 px-2 py-0.5 text-xs font-medium text-[#14162B]">
                    {rotuloDaEtapa(ordem.etapa)}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <span
                    className={
                      ordem.prioridade === "Alta"
                        ? "font-semibold text-[#E8536B]"
                        : "text-[#6B7280]"
                    }
                  >
                    {ROTULOS_PRIORIDADE[ordem.prioridade ?? "Normal"]}
                  </span>
                </td>
                <td className="px-4 py-3 text-[#6B7280]">
                  {ordem.prazoEstimado ? formatarDataCurta(ordem.prazoEstimado) : "—"}
                </td>
                <td className="px-4 py-3 text-[#6B7280]">
                  {ROTULOS_PAGAMENTO[ordem.statusPagamento ?? "NaoPago"]}
                </td>
                <td className="px-4 py-3 text-right">
                  <Button
                    variant="ghost"
                    className="h-8 px-3"
                    onClick={() => abrirEdicao(ordem)}
                  >
                    Abrir
                  </Button>
                </td>
              </tr>
            ))}
            {ordens && ordens.length === 0 && (
              <tr>
                <td colSpan={8} className="px-4 py-10 text-center text-[#6B7280]">
                  Nenhuma OS por aqui. Crie uma manualmente ou faça o check-in de
                  um agendamento.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
