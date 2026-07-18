"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useDeleteApiServicosId,
  useGetApiPecas,
  useGetApiServicos,
  usePostApiServicos,
  usePutApiServicosId,
  type ServicoResponse,
} from "@/lib/api-client/gerado";
import { formatarBRL } from "@/lib/formatadores";
import { esquemaServico, type ValoresServico } from "@/lib/validators/catalogo";

const SUGESTOES_CATEGORIA = [
  "Tela",
  "Bateria",
  "Conector de carga",
  "Placa",
  "Câmera",
  "Limpeza",
  "Software",
  "Película",
];

const VALORES_INICIAIS: ValoresServico = {
  nome: "",
  categoria: "",
  precoBase: 0,
  duracaoEstimadaMinutos: 60,
  prazoMedioDias: undefined,
  exigeDiagnostico: false,
  agendavelOnline: true,
  capacidadeSimultanea: 1,
  slaHoras: undefined,
  checklist: [],
  pecas: [],
};

export default function PaginaServicos() {
  const queryClient = useQueryClient();
  const [formAberto, setFormAberto] = useState(false);
  const [editandoId, setEditandoId] = useState<number | null>(null);
  const [mostrarInativos, setMostrarInativos] = useState(false);

  const { data: respostaServicos } = useGetApiServicos({
    incluirInativos: mostrarInativos || undefined,
  });
  const servicos =
    respostaServicos?.status === 200 ? respostaServicos.data : undefined;

  // Peças ativas para o vínculo "peças normalmente utilizadas".
  const { data: respostaPecas } = useGetApiPecas({ tamanhoPagina: 100 });
  const pecasDisponiveis =
    respostaPecas?.status === 200 ? (respostaPecas.data.itens ?? []) : [];

  const criarServico = usePostApiServicos();
  const atualizarServico = usePutApiServicosId();
  const desativarServico = useDeleteApiServicosId();

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ValoresServico>({
    resolver: zodResolver(esquemaServico),
    defaultValues: VALORES_INICIAIS,
  });
  const checklist = useFieldArray({ control, name: "checklist" });
  const pecasForm = useFieldArray({ control, name: "pecas" });

  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ["/api/servicos"] });
  }

  function abrirCriacao() {
    setEditandoId(null);
    reset(VALORES_INICIAIS);
    setFormAberto(true);
  }

  function abrirEdicao(servico: ServicoResponse) {
    setEditandoId(servico.id ?? null);
    reset({
      nome: servico.nome ?? "",
      categoria: servico.categoria ?? "",
      precoBase: servico.precoBase ?? 0,
      duracaoEstimadaMinutos: servico.duracaoEstimadaMinutos ?? 60,
      prazoMedioDias: servico.prazoMedioDias ?? undefined,
      exigeDiagnostico: servico.exigeDiagnostico ?? false,
      agendavelOnline: servico.agendavelOnline ?? true,
      capacidadeSimultanea: servico.capacidadeSimultanea ?? 1,
      slaHoras: servico.slaHoras ?? undefined,
      checklist: (servico.checklist ?? []).map((descricao) => ({ descricao })),
      pecas: (servico.pecas ?? []).map((p) => ({
        pecaId: String(p.pecaId),
        quantidadePadrao: p.quantidadePadrao ?? 1,
      })),
    });
    setFormAberto(true);
  }

  async function aoSalvar(valores: ValoresServico) {
    const corpo = {
      nome: valores.nome,
      categoria: valores.categoria || null,
      precoBase: valores.precoBase,
      duracaoEstimadaMinutos: valores.duracaoEstimadaMinutos,
      prazoMedioDias: valores.prazoMedioDias ?? null,
      exigeDiagnostico: valores.exigeDiagnostico,
      agendavelOnline: valores.agendavelOnline,
      capacidadeSimultanea: valores.capacidadeSimultanea,
      slaHoras: valores.slaHoras ?? null,
      ativo: true,
      checklist: valores.checklist.map((item) => item.descricao),
      pecas: valores.pecas.map((p) => ({
        pecaId: Number(p.pecaId),
        quantidadePadrao: p.quantidadePadrao,
      })),
    };
    try {
      if (editandoId === null) {
        await criarServico.mutateAsync({ data: corpo });
        toast.success("Serviço cadastrado.");
      } else {
        await atualizarServico.mutateAsync({ id: editandoId, data: corpo });
        toast.success("Serviço atualizado.");
      }
      invalidar();
      setFormAberto(false);
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar o serviço.");
    }
  }

  async function aoDesativar(id: number | undefined) {
    if (id === undefined) return;
    try {
      await desativarServico.mutateAsync({ id });
      toast.success("Serviço desativado.");
      invalidar();
    } catch (erro) {
      toast.error(
        erro instanceof ApiError ? erro.message : "Erro ao desativar o serviço.",
      );
    }
  }

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <div className="flex items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
            Catálogo
          </p>
          <h1 className="mt-2 text-3xl font-bold text-[#14162B]">Serviços</h1>
          <p className="mt-1 text-sm text-[#6B7280]">
            Os serviços que a sua assistência oferece — preço, duração,
            checklist padrão e peças normalmente utilizadas.
          </p>
        </div>
        <Button
          onClick={abrirCriacao}
          className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
        >
          Novo serviço
        </Button>
      </div>

      {formAberto && (
        <form
          onSubmit={handleSubmit(aoSalvar)}
          className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6"
        >
          <h2 className="text-lg font-semibold text-[#14162B]">
            {editandoId === null ? "Novo serviço" : "Editar serviço"}
          </h2>

          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <div>
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
            <div>
              <Label htmlFor="categoria">Categoria (opcional)</Label>
              <Input
                id="categoria"
                list="sugestoes-categoria"
                className="mt-1 h-11"
                {...register("categoria")}
              />
              <datalist id="sugestoes-categoria">
                {SUGESTOES_CATEGORIA.map((c) => (
                  <option key={c} value={c} />
                ))}
              </datalist>
            </div>
            <div>
              <Label htmlFor="precoBase">Preço base (R$)</Label>
              <Input
                id="precoBase"
                type="number"
                step="0.01"
                min="0"
                className="mt-1 h-11"
                aria-invalid={!!errors.precoBase}
                {...register("precoBase", { valueAsNumber: true })}
              />
              {errors.precoBase && (
                <p className="mt-1 text-sm text-destructive">{errors.precoBase.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="duracaoEstimadaMinutos">Duração estimada (min)</Label>
              <Input
                id="duracaoEstimadaMinutos"
                type="number"
                min="1"
                className="mt-1 h-11"
                aria-invalid={!!errors.duracaoEstimadaMinutos}
                {...register("duracaoEstimadaMinutos", { valueAsNumber: true })}
              />
              {errors.duracaoEstimadaMinutos && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.duracaoEstimadaMinutos.message}
                </p>
              )}
            </div>
            <div>
              <Label htmlFor="prazoMedioDias">Prazo médio (dias, opcional)</Label>
              <Input
                id="prazoMedioDias"
                type="number"
                min="1"
                className="mt-1 h-11"
                {...register("prazoMedioDias", {
                  setValueAs: (v) =>
                    v === "" || v === null || Number.isNaN(Number(v))
                      ? undefined
                      : Number(v),
                })}
              />
              {errors.prazoMedioDias && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.prazoMedioDias.message}
                </p>
              )}
            </div>
            <div>
              <Label htmlFor="capacidadeSimultanea">Capacidade simultânea</Label>
              <Input
                id="capacidadeSimultanea"
                type="number"
                min="1"
                className="mt-1 h-11"
                aria-invalid={!!errors.capacidadeSimultanea}
                {...register("capacidadeSimultanea", { valueAsNumber: true })}
              />
              {errors.capacidadeSimultanea && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.capacidadeSimultanea.message}
                </p>
              )}
              <p className="mt-1 text-xs text-[#8B8D98]">
                Quantos atendimentos deste serviço a agenda aceita ao mesmo tempo.
              </p>
            </div>
            <div>
              <Label htmlFor="slaHoras">SLA por etapa (horas)</Label>
              <Input
                id="slaHoras"
                type="number"
                min="1"
                placeholder="sem SLA"
                className="mt-1 h-11"
                aria-invalid={!!errors.slaHoras}
                {...register("slaHoras", {
                  setValueAs: (v) => (v === "" ? undefined : Number(v)),
                })}
              />
              {errors.slaHoras && (
                <p className="mt-1 text-sm text-destructive">{errors.slaHoras.message}</p>
              )}
              <p className="mt-1 text-xs text-[#8B8D98]">
                Horas que a OS pode ficar parada em uma etapa antes do card do
                Kanban alertar. Deixe vazio para não acompanhar prazo.
              </p>
            </div>
            <label className="flex items-center gap-2 text-sm text-[#14162B]">
              <input type="checkbox" {...register("exigeDiagnostico")} />
              Exige diagnóstico antes do orçamento
            </label>
            <label className="flex items-center gap-2 text-sm text-[#14162B]">
              <input type="checkbox" {...register("agendavelOnline")} />
              Disponível para agendamento online
            </label>
          </div>

          <fieldset className="mt-6">
            <legend className="text-sm font-semibold text-[#14162B]">
              Checklist padrão
            </legend>
            {checklist.fields.map((campo, indice) => (
              <div key={campo.id} className="mt-2 flex gap-2">
                <Input
                  className="h-10"
                  placeholder={`Item ${indice + 1}`}
                  aria-invalid={!!errors.checklist?.[indice]?.descricao}
                  {...register(`checklist.${indice}.descricao`)}
                />
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => checklist.remove(indice)}
                >
                  Remover
                </Button>
              </div>
            ))}
            {errors.checklist && (
              <p className="mt-1 text-sm text-destructive">
                Revise os itens do checklist.
              </p>
            )}
            <Button
              type="button"
              variant="outline"
              className="mt-2 h-9"
              onClick={() => checklist.append({ descricao: "" })}
            >
              Adicionar item
            </Button>
          </fieldset>

          <fieldset className="mt-6">
            <legend className="text-sm font-semibold text-[#14162B]">
              Peças normalmente utilizadas
            </legend>
            {pecasForm.fields.map((campo, indice) => (
              <div key={campo.id} className="mt-2 flex gap-2">
                <select
                  className="h-10 w-full rounded-md border border-input bg-white px-3 text-sm"
                  aria-invalid={!!errors.pecas?.[indice]?.pecaId}
                  {...register(`pecas.${indice}.pecaId`)}
                >
                  <option value="">Escolha a peça...</option>
                  {pecasDisponiveis.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nome}
                    </option>
                  ))}
                </select>
                <Input
                  type="number"
                  min="1"
                  className="h-10 w-24"
                  title="Quantidade"
                  {...register(`pecas.${indice}.quantidadePadrao`, {
                    valueAsNumber: true,
                  })}
                />
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => pecasForm.remove(indice)}
                >
                  Remover
                </Button>
              </div>
            ))}
            {errors.pecas && (
              <p className="mt-1 text-sm text-destructive">
                Revise as peças vinculadas.
              </p>
            )}
            <Button
              type="button"
              variant="outline"
              className="mt-2 h-9"
              onClick={() => pecasForm.append({ pecaId: "", quantidadePadrao: 1 })}
            >
              Vincular peça
            </Button>
          </fieldset>

          <div className="mt-6 flex gap-3">
            <Button
              type="submit"
              disabled={isSubmitting}
              className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
            >
              {isSubmitting ? "Salvando..." : "Salvar serviço"}
            </Button>
            <Button type="button" variant="ghost" onClick={() => setFormAberto(false)}>
              Cancelar
            </Button>
          </div>
        </form>
      )}

      <div className="mt-8 flex items-center justify-between">
        <p className="text-sm text-[#6B7280]">
          {servicos ? `${servicos.total} serviço(s)` : "Carregando..."}
        </p>
        <label className="flex items-center gap-2 text-sm text-[#6B7280]">
          <input
            type="checkbox"
            checked={mostrarInativos}
            onChange={(e) => setMostrarInativos(e.target.checked)}
          />
          Mostrar desativados
        </label>
      </div>

      <div className="mt-3 overflow-x-auto rounded-2xl border border-[#14162B]/8">
        <table className="w-full text-left text-sm">
          <thead className="bg-[#F7F7F9] text-xs text-[#8B8D98] uppercase tracking-wide">
            <tr>
              <th className="px-4 py-3">Serviço</th>
              <th className="px-4 py-3">Categoria</th>
              <th className="px-4 py-3">Preço base</th>
              <th className="px-4 py-3">Duração</th>
              <th className="px-4 py-3">Online</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {(servicos?.itens ?? []).map((servico) => (
              <tr key={servico.id} className="border-t border-[#14162B]/6">
                <td className="px-4 py-3 font-medium text-[#14162B]">
                  {servico.nome}
                  {!servico.ativo && (
                    <span className="ml-2 rounded-full bg-[#F7F7F9] px-2 py-0.5 text-xs text-[#8B8D98]">
                      desativado
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 text-[#6B7280]">{servico.categoria ?? "—"}</td>
                <td className="px-4 py-3 text-[#6B7280]">
                  {formatarBRL(servico.precoBase ?? 0)}
                </td>
                <td className="px-4 py-3 text-[#6B7280]">
                  {servico.duracaoEstimadaMinutos} min
                </td>
                <td className="px-4 py-3 text-[#6B7280]">
                  {servico.agendavelOnline ? "Sim" : "Não"}
                </td>
                <td className="px-4 py-3 text-right whitespace-nowrap">
                  <Button
                    variant="ghost"
                    className="h-8 px-3"
                    onClick={() => abrirEdicao(servico)}
                  >
                    Editar
                  </Button>
                  {servico.ativo && (
                    <Button
                      variant="ghost"
                      className="h-8 px-3 text-[#E8536B] hover:text-[#E8536B]"
                      onClick={() => aoDesativar(servico.id)}
                    >
                      Desativar
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {servicos && (servicos.itens?.length ?? 0) === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-[#6B7280]">
                  Nenhum serviço cadastrado ainda. Clique em “Novo serviço” para
                  começar.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
