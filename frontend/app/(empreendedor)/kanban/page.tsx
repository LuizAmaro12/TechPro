"use client";

import {
  DndContext,
  PointerSensor,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  BORDA_SLA,
  ROTULO_SLA,
  formatarTempoNaEtapa,
  situacaoSla,
} from "@/lib/sla";
import {
  useGetApiAgendamentos,
  useGetApiOrdensServico,
  usePostApiAgendamentosIdCheckin,
  usePostApiOrdensServicoIdEtapa,
  type AgendamentoResponse,
  type EtapaOrdemServico,
  type OrdemServicoResponse,
} from "@/lib/api-client/gerado";
import { formatarDataCurta, hojeIso, horaCurta } from "@/lib/agenda-datas";
import { ETAPAS_OS, rotuloDaEtapa } from "@/lib/ordens-servico-etapas";

const COLUNA_AGENDADO = "Agendado";
const ETAPAS_FINALIZADAS: EtapaOrdemServico[] = ["Entregue", "Cancelado"];

type MovimentoPendente = { ordemId: string; paraEtapa: EtapaOrdemServico };

export default function PaginaKanban() {
  const queryClient = useQueryClient();
  const [mostrarFinalizadas, setMostrarFinalizadas] = useState(false);
  const [pendente, setPendente] = useState<MovimentoPendente | null>(null);
  const [motivo, setMotivo] = useState("");

  const { data: respostaOrdens } = useGetApiOrdensServico({
    incluirFinalizadas: mostrarFinalizadas || undefined,
  });
  const ordens = respostaOrdens?.status === 200 ? respostaOrdens.data : [];

  // A coluna "Agendado" mostra agendamentos de hoje em diante que ainda não
  // viraram OS; arrastar para "Check-in realizado" faz o check-in.
  const { data: respostaAgendamentos } = useGetApiAgendamentos({
    inicio: hojeIso(),
    status: "Agendado",
  });
  const agendamentos =
    respostaAgendamentos?.status === 200 ? respostaAgendamentos.data : [];

  const moverEtapa = usePostApiOrdensServicoIdEtapa();
  const fazerCheckin = usePostApiAgendamentosIdCheckin();

  const sensores = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
  );

  const colunas: { id: string; rotulo: string }[] = [
    { id: COLUNA_AGENDADO, rotulo: "Agendado" },
    ...ETAPAS_OS.filter(
      (e) => mostrarFinalizadas || !ETAPAS_FINALIZADAS.includes(e.valor),
    ).map((e) => ({ id: e.valor as string, rotulo: e.rotulo })),
  ];

  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ["/api/ordens-servico"] });
    queryClient.invalidateQueries({ queryKey: ["/api/agendamentos"] });
  }

  async function executarCheckin(agendamentoId: number) {
    try {
      await fazerCheckin.mutateAsync({ id: agendamentoId });
      toast.success("Check-in feito — OS criada.");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro no check-in.");
    }
  }

  async function executarMovimento(
    ordemId: string, paraEtapa: EtapaOrdemServico, motivoMovimento?: string,
  ) {
    if (paraEtapa === "Cancelado" && !motivoMovimento) {
      setPendente({ ordemId, paraEtapa });
      setMotivo("");
      return;
    }

    try {
      await moverEtapa.mutateAsync({
        id: ordemId,
        data: { paraEtapa, motivo: motivoMovimento || null },
      });
      toast.success(`Movida para ${rotuloDaEtapa(paraEtapa)}.`);
      setPendente(null);
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao mover a OS.");
    }
  }

  function aoSoltar(evento: DragEndEvent) {
    const destino = evento.over?.id as string | undefined;
    const origem = evento.active.id as string;
    if (!destino) return;

    if (origem.startsWith("ag:")) {
      // Agendamento só materializa OS entrando em "Check-in realizado".
      if (destino === "CheckInRealizado") {
        executarCheckin(Number(origem.slice(3)));
      } else if (destino !== COLUNA_AGENDADO) {
        toast.error("Agendamento vira OS pelo check-in — solte em \"Check-in realizado\".");
      }
      return;
    }

    const ordemId = origem.slice(3);
    const ordem = ordens.find((o) => o.id === ordemId);
    if (destino === COLUNA_AGENDADO || !ordem || ordem.etapa === destino) return;
    executarMovimento(ordemId, destino as EtapaOrdemServico);
  }

  function CartaoOs({ ordem }: { ordem: OrdemServicoResponse }) {
    const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
      id: `os:${ordem.id}`,
    });
    const sla = situacaoSla(ordem.horasNaEtapa, ordem.slaHoras);
    const tempo = formatarTempoNaEtapa(ordem.horasNaEtapa);
    return (
      <div
        ref={setNodeRef}
        {...attributes}
        {...listeners}
        style={
          transform
            ? { transform: `translate(${transform.x}px, ${transform.y}px)` }
            : undefined
        }
        title={
          sla === "sem-sla"
            ? `Nesta etapa há ${tempo}`
            : `Nesta etapa há ${tempo} (limite de ${ordem.slaHoras}h)`
        }
        className={`cursor-grab rounded-xl border border-l-4 border-[#14162B]/8 bg-white p-3 text-sm shadow-sm ${
          BORDA_SLA[sla]
        } ${isDragging ? "z-50 opacity-80 shadow-lg" : ""}`}
      >
        <div className="flex items-center justify-between gap-2">
          <span className="font-semibold text-[#14162B]">#{ordem.numero}</span>
          {ordem.prioridade === "Alta" && (
            <span className="rounded-full bg-[#E8536B]/10 px-2 py-0.5 text-[10px] font-semibold text-[#E8536B] uppercase">
              alta
            </span>
          )}
        </div>
        <p className="mt-1 font-medium text-[#14162B]">{ordem.clienteNome}</p>
        <p className="text-xs text-[#6B7280]">{ordem.servicoNome}</p>
        {(ordem.aparelhoMarca || ordem.aparelhoModelo) && (
          <p className="text-xs text-[#8B8D98]">
            {[ordem.aparelhoMarca, ordem.aparelhoModelo].filter(Boolean).join(" ")}
          </p>
        )}
        <div className="mt-1 flex flex-wrap items-center gap-1 text-[10px] text-[#8B8D98]">
          {/* Só avisa quando importa: card no prazo não vira ruído. */}
          {(sla === "atencao" || sla === "estourado") && (
            <span
              className={`rounded-full px-1.5 py-0.5 font-semibold ${
                sla === "estourado"
                  ? "bg-[#E8536B]/10 text-[#E8536B]"
                  : "bg-amber-100 text-amber-700"
              }`}
            >
              {ROTULO_SLA[sla]} · {tempo}
            </span>
          )}
          {ordem.prazoEstimado && <span>prazo {formatarDataCurta(ordem.prazoEstimado)}</span>}
          {ordem.responsavelTecnicoNome && <span>· {ordem.responsavelTecnicoNome}</span>}
          {ordem.statusAprovacao === "Aprovado" && (
            <span className="rounded-full bg-emerald-100 px-1.5 py-0.5 font-semibold text-emerald-700">
              orçamento ok
            </span>
          )}
          {ordem.statusAprovacao === "Recusado" && (
            <span className="rounded-full bg-[#E8536B]/10 px-1.5 py-0.5 font-semibold text-[#E8536B]">
              recusado
            </span>
          )}
          {ordem.statusPagamento === "Pago" && (
            <span className="rounded-full bg-emerald-100 px-1.5 py-0.5 font-semibold text-emerald-700">
              pago
            </span>
          )}
          {ordem.statusPagamento === "Parcial" && (
            <span className="rounded-full bg-amber-100 px-1.5 py-0.5 font-semibold text-amber-700">
              parcial
            </span>
          )}
        </div>
        {/* Fallback sem drag (touch): mover pelo select. */}
        <select
          aria-label={`Mover OS ${ordem.numero}`}
          value={ordem.etapa}
          onPointerDown={(e) => e.stopPropagation()}
          onChange={(e) =>
            executarMovimento(ordem.id!, e.target.value as EtapaOrdemServico)
          }
          className="mt-2 h-7 w-full rounded-md border border-[#14162B]/10 bg-white px-1 text-xs text-[#6B7280]"
        >
          {ETAPAS_OS.map((e) => (
            <option key={e.valor} value={e.valor}>
              {e.rotulo}
            </option>
          ))}
        </select>
      </div>
    );
  }

  function CartaoAgendamento({ agendamento }: { agendamento: AgendamentoResponse }) {
    const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
      id: `ag:${agendamento.id}`,
    });
    return (
      <div
        ref={setNodeRef}
        {...attributes}
        {...listeners}
        style={
          transform
            ? { transform: `translate(${transform.x}px, ${transform.y}px)` }
            : undefined
        }
        className={`cursor-grab rounded-xl border border-dashed border-[#14162B]/15 bg-[#F7F7F9] p-3 text-sm ${
          isDragging ? "z-50 opacity-80 shadow-lg" : ""
        }`}
      >
        <p className="font-medium text-[#14162B]">{agendamento.nomeContato}</p>
        <p className="text-xs text-[#6B7280]">
          {formatarDataCurta(agendamento.data ?? "")} às{" "}
          {horaCurta(agendamento.horaInicio ?? "")} · {agendamento.servicoNome}
        </p>
        <Button
          variant="outline"
          className="mt-2 h-7 w-full rounded-full text-xs"
          onPointerDown={(e) => e.stopPropagation()}
          onClick={() => executarCheckin(agendamento.id!)}
        >
          Check-in
        </Button>
      </div>
    );
  }

  function Coluna({ id, rotulo }: { id: string; rotulo: string }) {
    const { setNodeRef, isOver } = useDroppable({ id });
    const cartoes =
      id === COLUNA_AGENDADO
        ? agendamentos.map((a) => <CartaoAgendamento key={a.id} agendamento={a} />)
        : ordens
            .filter((o) => o.etapa === id)
            .map((o) => <CartaoOs key={o.id} ordem={o} />);
    return (
      <div
        ref={setNodeRef}
        className={`flex w-60 shrink-0 flex-col rounded-2xl border p-2 transition-colors ${
          isOver ? "border-[#14162B] bg-[#14162B]/[0.03]" : "border-[#14162B]/8"
        }`}
      >
        <p className="px-1 pb-2 text-xs font-semibold tracking-wide text-[#6B7280] uppercase">
          {rotulo}
          <span className="ml-1 text-[#8B8D98]">({cartoes.length})</span>
        </p>
        <div className="flex min-h-24 flex-col gap-2">{cartoes}</div>
      </div>
    );
  }

  return (
    <div className="mx-auto w-full max-w-[96rem] px-6 py-10">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
            Kanban
          </p>
          <h1 className="mt-2 text-3xl font-bold text-[#14162B]">Fluxo da oficina</h1>
          <p className="mt-1 text-sm text-[#6B7280]">
            Arraste os cards entre as etapas. Detalhes e edição ficam em{" "}
            <Link href="/ordens-servico" className="underline underline-offset-4">
              Ordens de serviço
            </Link>
            .
          </p>
        </div>
        <label className="flex items-center gap-2 text-sm text-[#6B7280]">
          <input
            type="checkbox"
            checked={mostrarFinalizadas}
            onChange={(e) => setMostrarFinalizadas(e.target.checked)}
          />
          Mostrar finalizadas
        </label>
      </div>

      {pendente && (
        <div className="mt-6 flex flex-wrap items-center gap-2 rounded-2xl border border-[#E8536B]/30 bg-[#E8536B]/5 p-4">
          <p className="text-sm text-[#14162B]">
            Cancelar a OS #{ordens.find((o) => o.id === pendente.ordemId)?.numero}: qual o
            motivo?
          </p>
          <Input
            placeholder="Motivo do cancelamento"
            value={motivo}
            onChange={(e) => setMotivo(e.target.value)}
            className="h-9 max-w-72"
          />
          <Button
            className="h-9 rounded-full bg-[#14162B] px-4 text-white hover:bg-[#14162B]/90"
            disabled={!motivo.trim()}
            onClick={() => executarMovimento(pendente.ordemId, pendente.paraEtapa, motivo)}
          >
            Confirmar cancelamento
          </Button>
          <Button variant="ghost" className="h-9" onClick={() => setPendente(null)}>
            Desistir
          </Button>
        </div>
      )}

      <DndContext sensors={sensores} onDragEnd={aoSoltar}>
        <div className="mt-6 flex gap-3 overflow-x-auto pb-4">
          {colunas.map((coluna) => (
            <Coluna key={coluna.id} id={coluna.id} rotulo={coluna.rotulo} />
          ))}
        </div>
      </DndContext>
    </div>
  );
}
