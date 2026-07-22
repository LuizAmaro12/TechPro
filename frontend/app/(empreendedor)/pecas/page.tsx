"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { ListaDeCompra } from "@/components/estoque/lista-de-compra";
import { MovimentacaoPeca } from "@/components/estoque/movimentacao-peca";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useDeleteApiPecasId,
  useGetApiFornecedores,
  useGetApiPecas,
  usePostApiFornecedores,
  usePostApiPecas,
  usePutApiPecasId,
  type PecaResponse,
} from "@/lib/api-client/gerado";
import { formatarBRL } from "@/lib/formatadores";
import { esquemaPeca, type ValoresPeca } from "@/lib/validators/catalogo";

const VALORES_INICIAIS: ValoresPeca = {
  nome: "",
  descricao: "",
  custoUnitario: 0,
  precoVenda: 0,
  quantidadeEmEstoque: 0,
  estoqueMinimo: 0,
  fornecedorId: "",
};

export default function PaginaPecas() {
  // Extrato/movimentação de uma peça por vez — abre abaixo da tabela.
  const [movimentandoId, setMovimentandoId] = useState<number | null>(null);
  const queryClient = useQueryClient();
  const [formAberto, setFormAberto] = useState(false);
  const [editandoId, setEditandoId] = useState<number | null>(null);
  const [mostrarInativas, setMostrarInativas] = useState(false);
  const [novoFornecedor, setNovoFornecedor] = useState("");

  const { data: respostaPecas } = useGetApiPecas({
    incluirInativas: mostrarInativas || undefined,
  });
  const pecas = respostaPecas?.status === 200 ? respostaPecas.data : undefined;

  const { data: respostaFornecedores } = useGetApiFornecedores();
  const fornecedores =
    respostaFornecedores?.status === 200 ? respostaFornecedores.data : [];

  const criarPeca = usePostApiPecas();
  const atualizarPeca = usePutApiPecasId();
  const desativarPeca = useDeleteApiPecasId();
  const criarFornecedor = usePostApiFornecedores();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ValoresPeca>({
    resolver: zodResolver(esquemaPeca),
    defaultValues: VALORES_INICIAIS,
  });

  const pecaMovimentando =
    movimentandoId === null
      ? null
      : (pecas?.itens ?? []).find((p) => p.id === movimentandoId) ?? null;

  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ["/api/pecas"] });
    queryClient.invalidateQueries({ queryKey: ["/api/fornecedores"] });
  }

  function invalidarEstoque() {
    invalidar();
    queryClient.invalidateQueries({ queryKey: ["/api/estoque/lista-compra"] });
    if (movimentandoId !== null) {
      queryClient.invalidateQueries({
        queryKey: [`/api/pecas/${movimentandoId}/movimentacoes`],
      });
    }
  }

  function abrirCriacao() {
    setEditandoId(null);
    reset(VALORES_INICIAIS);
    setFormAberto(true);
  }

  function abrirEdicao(peca: PecaResponse) {
    setEditandoId(peca.id ?? null);
    reset({
      nome: peca.nome ?? "",
      descricao: peca.descricao ?? "",
      custoUnitario: peca.custoUnitario ?? 0,
      precoVenda: peca.precoVenda ?? 0,
      quantidadeEmEstoque: peca.quantidadeEmEstoque ?? 0,
      estoqueMinimo: peca.estoqueMinimo ?? 0,
      fornecedorId: peca.fornecedor?.id ? String(peca.fornecedor.id) : "",
    });
    setFormAberto(true);
  }

  async function aoSalvar(valores: ValoresPeca) {
    const corpo = {
      nome: valores.nome,
      descricao: valores.descricao || null,
      custoUnitario: valores.custoUnitario,
      precoVenda: valores.precoVenda,
      quantidadeEmEstoque: valores.quantidadeEmEstoque,
      estoqueMinimo: valores.estoqueMinimo,
      fornecedorId: valores.fornecedorId ? Number(valores.fornecedorId) : null,
      ativo: true,
    };
    try {
      if (editandoId === null) {
        await criarPeca.mutateAsync({ data: corpo });
        toast.success("Peça cadastrada.");
      } else {
        await atualizarPeca.mutateAsync({ id: editandoId, data: corpo });
        toast.success("Peça atualizada.");
      }
      invalidar();
      setFormAberto(false);
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar a peça.");
    }
  }

  async function aoDesativar(id: number | undefined) {
    if (id === undefined) return;
    try {
      await desativarPeca.mutateAsync({ id });
      toast.success("Peça desativada.");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao desativar a peça.");
    }
  }

  async function aoCriarFornecedor() {
    const nome = novoFornecedor.trim();
    if (!nome) return;
    try {
      await criarFornecedor.mutateAsync({ data: { nome, contato: null } });
      toast.success("Fornecedor cadastrado.");
      setNovoFornecedor("");
      invalidar();
    } catch (erro) {
      toast.error(
        erro instanceof ApiError ? erro.message : "Erro ao cadastrar o fornecedor.",
      );
    }
  }

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <div className="flex items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-semibold tracking-[0.18em] text-marca uppercase">
            Catálogo
          </p>
          <h1 className="mt-2 text-3xl font-bold text-tinta">Peças</h1>
          <p className="mt-1 text-sm text-tinta-suave">
            Cadastre as peças que a sua assistência usa nos reparos — custo,
            preço de venda, estoque e fornecedor.
          </p>
        </div>
        <Button
          onClick={abrirCriacao}
          className="h-11 rounded-full bg-tinta px-6 text-sobre-tinta hover:bg-tinta/90"
        >
          Nova peça
        </Button>
      </div>

      {formAberto && (
        <form
          onSubmit={handleSubmit(aoSalvar)}
          className="mt-8 rounded-2xl border border-borda bg-superficie p-6"
        >
          <h2 className="text-lg font-semibold text-tinta">
            {editandoId === null ? "Nova peça" : "Editar peça"}
          </h2>

          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <Label htmlFor="nome">Nome</Label>
              <Input
                id="nome"
                className="mt-1 h-11"
                aria-invalid={!!errors.nome}
                {...register("nome")}
              />
              {errors.nome && (
                <p className="mt-1 text-sm text-destructive">{errors.nome.message}</p>
              )}
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="descricao">Descrição (opcional)</Label>
              <Input id="descricao" className="mt-1 h-11" {...register("descricao")} />
            </div>
            <div>
              <Label htmlFor="custoUnitario">Custo unitário (R$)</Label>
              <Input
                id="custoUnitario"
                type="number"
                step="0.01"
                min="0"
                className="mt-1 h-11"
                aria-invalid={!!errors.custoUnitario}
                {...register("custoUnitario", { valueAsNumber: true })}
              />
              {errors.custoUnitario && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.custoUnitario.message}
                </p>
              )}
            </div>
            <div>
              <Label htmlFor="precoVenda">Preço de venda (R$)</Label>
              <Input
                id="precoVenda"
                type="number"
                step="0.01"
                min="0"
                className="mt-1 h-11"
                aria-invalid={!!errors.precoVenda}
                {...register("precoVenda", { valueAsNumber: true })}
              />
              {errors.precoVenda && (
                <p className="mt-1 text-sm text-destructive">{errors.precoVenda.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="quantidadeEmEstoque">Quantidade em estoque</Label>
              <Input
                id="quantidadeEmEstoque"
                type="number"
                min="0"
                className="mt-1 h-11"
                aria-invalid={!!errors.quantidadeEmEstoque}
                {...register("quantidadeEmEstoque", { valueAsNumber: true })}
              />
              {errors.quantidadeEmEstoque && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.quantidadeEmEstoque.message}
                </p>
              )}
            </div>
            <div>
              <Label htmlFor="estoqueMinimo">Estoque mínimo</Label>
              <Input
                id="estoqueMinimo"
                type="number"
                min="0"
                className="mt-1 h-11"
                aria-invalid={!!errors.estoqueMinimo}
                {...register("estoqueMinimo", { valueAsNumber: true })}
              />
              {errors.estoqueMinimo && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.estoqueMinimo.message}
                </p>
              )}
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="fornecedorId">Fornecedor (opcional)</Label>
              <select
                id="fornecedorId"
                className="mt-1 h-11 w-full rounded-md border border-input bg-superficie px-3 text-sm"
                {...register("fornecedorId")}
              >
                <option value="">Sem fornecedor</option>
                {fornecedores.map((f) => (
                  <option key={f.id} value={f.id}>
                    {f.nome}
                  </option>
                ))}
              </select>
              <div className="mt-2 flex gap-2">
                <Input
                  placeholder="Cadastrar novo fornecedor..."
                  value={novoFornecedor}
                  onChange={(e) => setNovoFornecedor(e.target.value)}
                  className="h-9"
                />
                <Button
                  type="button"
                  variant="outline"
                  className="h-9"
                  onClick={aoCriarFornecedor}
                  disabled={!novoFornecedor.trim() || criarFornecedor.isPending}
                >
                  Adicionar
                </Button>
              </div>
            </div>
          </div>

          <div className="mt-6 flex gap-3">
            <Button
              type="submit"
              disabled={isSubmitting}
              className="h-11 rounded-full bg-tinta px-6 text-sobre-tinta hover:bg-tinta/90"
            >
              {isSubmitting ? "Salvando..." : "Salvar peça"}
            </Button>
            <Button type="button" variant="ghost" onClick={() => setFormAberto(false)}>
              Cancelar
            </Button>
          </div>
        </form>
      )}

      <ListaDeCompra />

      <div className="mt-8 flex items-center justify-between">
        <p className="text-sm text-tinta-suave">
          {pecas ? `${pecas.total} peça(s)` : "Carregando..."}
        </p>
        <label className="flex items-center gap-2 text-sm text-tinta-suave">
          <input
            type="checkbox"
            checked={mostrarInativas}
            onChange={(e) => setMostrarInativas(e.target.checked)}
          />
          Mostrar desativadas
        </label>
      </div>

      <div className="mt-3 overflow-x-auto rounded-2xl border border-borda">
        <table className="w-full text-left text-sm">
          <thead className="bg-sutil text-xs text-tinta-fraca uppercase tracking-wide">
            <tr>
              <th className="px-4 py-3">Peça</th>
              <th className="px-4 py-3">Custo</th>
              <th className="px-4 py-3">Venda</th>
              <th className="px-4 py-3">Estoque</th>
              <th className="px-4 py-3">Fornecedor</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {(pecas?.itens ?? []).map((peca) => (
              <tr key={peca.id} className="border-t border-borda">
                <td className="px-4 py-3 font-medium text-tinta">
                  {peca.nome}
                  {!peca.ativo && (
                    <span className="ml-2 rounded-full bg-sutil px-2 py-0.5 text-xs text-tinta-fraca">
                      desativada
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 text-tinta-suave">
                  {formatarBRL(peca.custoUnitario ?? 0)}
                </td>
                <td className="px-4 py-3 text-tinta-suave">
                  {formatarBRL(peca.precoVenda ?? 0)}
                </td>
                <td className="px-4 py-3">
                  <span
                    className={
                      peca.estoqueBaixo
                        ? "font-semibold text-marca"
                        : "text-tinta-suave"
                    }
                  >
                    {peca.quantidadeEmEstoque}
                    {peca.estoqueBaixo && " · baixo"}
                  </span>
                </td>
                <td className="px-4 py-3 text-tinta-suave">{peca.fornecedor?.nome ?? "—"}</td>
                <td className="px-4 py-3 text-right whitespace-nowrap">
                  <Button
                    variant="ghost"
                    className="h-8 px-3"
                    onClick={() =>
                      setMovimentandoId((atual) =>
                        atual === peca.id ? null : (peca.id ?? null),
                      )
                    }
                  >
                    Estoque
                  </Button>
                  <Button
                    variant="ghost"
                    className="h-8 px-3"
                    onClick={() => abrirEdicao(peca)}
                  >
                    Editar
                  </Button>
                  {peca.ativo && (
                    <Button
                      variant="ghost"
                      className="h-8 px-3 text-marca hover:text-marca"
                      onClick={() => aoDesativar(peca.id)}
                    >
                      Desativar
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {pecas && (pecas.itens?.length ?? 0) === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-tinta-suave">
                  Nenhuma peça cadastrada ainda. Clique em “Nova peça” para começar.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {pecaMovimentando && (
        <MovimentacaoPeca
          key={pecaMovimentando.id}
          pecaId={pecaMovimentando.id!}
          pecaNome={pecaMovimentando.nome ?? ""}
          saldoAtual={pecaMovimentando.quantidadeEmEstoque ?? 0}
          aoMudar={invalidarEstoque}
          aoFechar={() => setMovimentandoId(null)}
        />
      )}
    </div>
  );
}
