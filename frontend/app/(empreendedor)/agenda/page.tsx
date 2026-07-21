"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { FilaDeEspera } from "@/components/agenda/fila-de-espera";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiAgendaDisponibilidade,
  useGetApiAgendamentos,
  useGetApiClientes,
  useGetApiServicos,
  usePostApiAgendamentos,
  usePostApiAgendamentosIdCancelar,
  usePostApiAgendamentosIdCheckin,
  usePostApiAgendamentosIdNaoCompareceu,
  usePutApiAgendamentosId,
  type AgendamentoResponse,
} from "@/lib/api-client/gerado";
import {
  DIAS_SEMANA_CURTOS,
  deIso,
  fimDoMes,
  formatarDataCurta,
  formatarDataLonga,
  hojeIso,
  horaCurta,
  inicioDaSemana,
  inicioDoMes,
  paraIso,
  somarDias,
} from "@/lib/agenda-datas";
import { esquemaAgendamento, type ValoresAgendamento } from "@/lib/validators/agenda";

type Visao = "dia" | "semana" | "mes";

const VALORES_INICIAIS: ValoresAgendamento = {
  servicoId: "",
  clienteId: "",
  data: hojeIso(),
  horaInicio: "",
  nomeContato: "",
  telefoneContato: "",
  emailContato: "",
  descricaoProblema: "",
  aparelhoMarca: "",
  aparelhoModelo: "",
};

export default function PaginaAgenda() {
  const queryClient = useQueryClient();
  const [visao, setVisao] = useState<Visao>("semana");
  const [dataRef, setDataRef] = useState(hojeIso());
  const [formAberto, setFormAberto] = useState(false);
  const [editandoId, setEditandoId] = useState<number | null>(null);
  const [cancelandoId, setCancelandoId] = useState<number | null>(null);
  const [motivoCancelamento, setMotivoCancelamento] = useState("");

  const inicio =
    visao === "dia" ? dataRef : visao === "semana" ? inicioDaSemana(dataRef) : inicioDoMes(dataRef);
  const fim =
    visao === "dia"
      ? dataRef
      : visao === "semana"
        ? somarDias(inicioDaSemana(dataRef), 6)
        : fimDoMes(dataRef);

  const { data: respostaAgendamentos } = useGetApiAgendamentos({ inicio, fim });
  const agendamentos =
    respostaAgendamentos?.status === 200 ? respostaAgendamentos.data : [];

  const { data: respostaServicos } = useGetApiServicos({ tamanhoPagina: 100 });
  const servicos =
    respostaServicos?.status === 200 ? (respostaServicos.data.itens ?? []) : [];

  const { data: respostaClientes } = useGetApiClientes({ tamanhoPagina: 100 });
  const clientes =
    respostaClientes?.status === 200 ? (respostaClientes.data.itens ?? []) : [];

  const criar = usePostApiAgendamentos();
  const atualizar = usePutApiAgendamentosId();
  const fazerCheckin = usePostApiAgendamentosIdCheckin();
  const cancelar = usePostApiAgendamentosIdCancelar();
  const marcarFalta = usePostApiAgendamentosIdNaoCompareceu();

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<ValoresAgendamento>({
    resolver: zodResolver(esquemaAgendamento),
    defaultValues: VALORES_INICIAIS,
  });

  const servicoEscolhido = watch("servicoId");
  const dataEscolhida = watch("data");
  const horaEscolhida = watch("horaInicio");

  const { data: respostaDisponibilidade } = useGetApiAgendaDisponibilidade(
    { servicoId: Number(servicoEscolhido), data: dataEscolhida },
    { query: { enabled: formAberto && servicoEscolhido !== "" && dataEscolhida !== "" } },
  );
  const horariosLivres =
    respostaDisponibilidade?.status === 200
      ? (respostaDisponibilidade.data.horariosLivres ?? [])
      : [];
  // Na edição o próprio horário atual não aparece como livre — reapresenta ele.
  const opcoesDeHorario =
    editandoId !== null && horaEscolhida && !horariosLivres.includes(horaEscolhida)
      ? [horaEscolhida, ...horariosLivres].sort()
      : horariosLivres;

  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ["/api/agendamentos"] });
    queryClient.invalidateQueries({ queryKey: ["/api/agenda/disponibilidade"] });
    queryClient.invalidateQueries({ queryKey: ["/api/clientes"] });
  }

  function navegar(direcao: -1 | 1) {
    if (visao === "dia") {
      setDataRef(somarDias(dataRef, direcao));
    } else if (visao === "semana") {
      setDataRef(somarDias(dataRef, direcao * 7));
    } else {
      const data = deIso(inicioDoMes(dataRef));
      data.setMonth(data.getMonth() + direcao);
      setDataRef(paraIso(data));
    }
  }

  function abrirCriacao(dataInicial?: string) {
    setEditandoId(null);
    reset({ ...VALORES_INICIAIS, data: dataInicial ?? dataRef });
    setFormAberto(true);
  }

  function abrirEdicao(agendamento: AgendamentoResponse) {
    setEditandoId(agendamento.id ?? null);
    reset({
      servicoId: String(agendamento.servicoId ?? ""),
      clienteId: agendamento.clienteId ? String(agendamento.clienteId) : "",
      data: agendamento.data ?? hojeIso(),
      horaInicio: agendamento.horaInicio ?? "",
      nomeContato: agendamento.nomeContato ?? "",
      telefoneContato: agendamento.telefoneContato ?? "",
      emailContato: agendamento.emailContato ?? "",
      descricaoProblema: agendamento.descricaoProblema ?? "",
      aparelhoMarca: agendamento.aparelhoMarca ?? "",
      aparelhoModelo: agendamento.aparelhoModelo ?? "",
    });
    setFormAberto(true);
  }

  async function aoSalvar(valores: ValoresAgendamento) {
    const corpo = {
      servicoId: Number(valores.servicoId),
      data: valores.data,
      horaInicio: valores.horaInicio,
      clienteId: valores.clienteId ? Number(valores.clienteId) : null,
      nomeContato: valores.nomeContato || null,
      telefoneContato: valores.telefoneContato || null,
      emailContato: valores.emailContato || null,
      descricaoProblema: valores.descricaoProblema || null,
      aparelhoMarca: valores.aparelhoMarca || null,
      aparelhoModelo: valores.aparelhoModelo || null,
    };
    try {
      if (editandoId === null) {
        await criar.mutateAsync({ data: corpo });
        toast.success("Agendamento criado.");
      } else {
        await atualizar.mutateAsync({ id: editandoId, data: corpo });
        toast.success("Agendamento atualizado.");
      }
      invalidar();
      setFormAberto(false);
    } catch (erro) {
      toast.error(
        erro instanceof ApiError ? erro.message : "Erro ao salvar o agendamento.",
      );
    }
  }

  async function aoCheckin(id: number | undefined) {
    if (id === undefined) return;
    try {
      await fazerCheckin.mutateAsync({ id });
      toast.success("Check-in realizado.");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao fazer check-in.");
    }
  }

  async function aoNaoCompareceu(id: number | undefined) {
    if (id === undefined) return;
    try {
      await marcarFalta.mutateAsync({ id });
      toast.success("Falta registrada.");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao registrar falta.");
    }
  }

  async function aoConfirmarCancelamento(id: number) {
    try {
      await cancelar.mutateAsync({ id, data: { motivo: motivoCancelamento || null } });
      toast.success("Agendamento cancelado.");
      setCancelandoId(null);
      setMotivoCancelamento("");
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao cancelar.");
    }
  }

  function porDia(dia: string): AgendamentoResponse[] {
    return agendamentos
      .filter((a) => a.data === dia)
      .sort((a, b) => (a.horaInicio ?? "").localeCompare(b.horaInicio ?? ""));
  }

  function CartaoAgendamento({ agendamento }: { agendamento: AgendamentoResponse }) {
    const cancelado = agendamento.status === "Cancelado";
    const faltou = agendamento.status === "NaoCompareceu";
    const comCheckin = agendamento.status === "CheckInRealizado";
    // Terminal (cancelado ou faltou) fica esmaecido; só o agendado age.
    const encerrado = cancelado || faltou;
    return (
      <div
        className={`rounded-xl border p-3 text-sm ${
          encerrado
            ? "border-[#14162B]/6 bg-[#F7F7F9] opacity-60"
            : "border-[#14162B]/8 bg-white"
        }`}
      >
        <div className="flex items-center justify-between gap-2">
          <span className="font-semibold text-[#14162B]">
            {horaCurta(agendamento.horaInicio ?? "")}–{horaCurta(agendamento.horaFim ?? "")}
          </span>
          <span className="flex items-center gap-1">
            {agendamento.origem === "Portal" && (
              <span className="rounded-full bg-[#E8536B]/10 px-2 py-0.5 text-[10px] font-semibold text-[#E8536B] uppercase">
                portal
              </span>
            )}
            {comCheckin && (
              <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold text-emerald-700 uppercase">
                check-in
              </span>
            )}
            {cancelado && (
              <span className="rounded-full bg-[#F7F7F9] px-2 py-0.5 text-[10px] font-semibold text-[#8B8D98] uppercase">
                cancelado
              </span>
            )}
            {faltou && (
              <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold text-amber-700 uppercase">
                faltou
              </span>
            )}
          </span>
        </div>
        <p className="mt-1 font-medium text-[#14162B]">{agendamento.servicoNome}</p>
        <p className="text-[#6B7280]">
          {agendamento.nomeContato} · {agendamento.telefoneContato}
        </p>
        {/* Risco onde a decisão é tomada: só quando há histórico e o
            agendamento ainda está em aberto. */}
        {!encerrado && (agendamento.clienteFaltas ?? 0) > 0 && (
          <p className="mt-1 text-xs font-semibold text-amber-700">
            ⚠ Cliente já faltou {agendamento.clienteFaltas}×
          </p>
        )}
        {/* Peça em falta: avisar antes de o cliente chegar, para pedir/remarcar. */}
        {!encerrado && (agendamento.pecasEmFalta?.length ?? 0) > 0 && (
          <p
            className="mt-1 text-xs font-semibold text-[#E8536B]"
            title={agendamento
              .pecasEmFalta!.map(
                (p) => `${p.pecaNome}: precisa ${p.necessario}, tem ${p.emEstoque}`,
              )
              .join(" · ")}
          >
            ⚠ Peça em falta:{" "}
            {agendamento.pecasEmFalta!.map((p) => p.pecaNome).join(", ")}
          </p>
        )}
        {(agendamento.aparelhoMarca || agendamento.aparelhoModelo) && (
          <p className="text-xs text-[#8B8D98]">
            {[agendamento.aparelhoMarca, agendamento.aparelhoModelo]
              .filter(Boolean)
              .join(" ")}
          </p>
        )}
        {!encerrado && (
          <div className="mt-2 flex flex-wrap gap-1">
            {agendamento.status === "Agendado" && (
              <>
                <Button
                  variant="outline"
                  className="h-7 rounded-full px-3 text-xs"
                  onClick={() => aoCheckin(agendamento.id)}
                >
                  Check-in
                </Button>
                <Button
                  variant="ghost"
                  className="h-7 px-3 text-xs"
                  onClick={() => abrirEdicao(agendamento)}
                >
                  Editar
                </Button>
                <Button
                  variant="ghost"
                  className="h-7 px-3 text-xs text-amber-700 hover:text-amber-700"
                  onClick={() => aoNaoCompareceu(agendamento.id)}
                >
                  Não compareceu
                </Button>
              </>
            )}
            <Button
              variant="ghost"
              className="h-7 px-3 text-xs text-[#E8536B] hover:text-[#E8536B]"
              onClick={() => {
                setCancelandoId(agendamento.id ?? null);
                setMotivoCancelamento("");
              }}
            >
              Cancelar
            </Button>
          </div>
        )}
        {cancelandoId === agendamento.id && (
          <div className="mt-2 flex gap-2">
            <Input
              placeholder="Motivo (opcional)"
              value={motivoCancelamento}
              onChange={(e) => setMotivoCancelamento(e.target.value)}
              className="h-8 text-xs"
            />
            <Button
              variant="outline"
              className="h-8 px-3 text-xs text-[#E8536B]"
              onClick={() => aoConfirmarCancelamento(agendamento.id!)}
            >
              Confirmar
            </Button>
          </div>
        )}
      </div>
    );
  }

  const diasDaSemana = Array.from({ length: 7 }, (_, i) =>
    somarDias(inicioDaSemana(dataRef), i),
  );

  // Grade fixa de 6 semanas a partir do domingo da semana do dia 1º.
  const diasDoMes = Array.from({ length: 42 }, (_, i) =>
    somarDias(inicioDaSemana(inicioDoMes(dataRef)), i),
  );

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <div className="flex items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
            Agenda
          </p>
          <h1 className="mt-2 text-3xl font-bold text-[#14162B]">Agendamentos</h1>
          <p className="mt-1 text-sm text-[#6B7280]">
            Acompanhe os horários da loja, faça check-in e organize a semana.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Link
            href="/agenda/configuracoes"
            className="text-sm text-[#6B7280] underline-offset-4 hover:text-[#14162B] hover:underline"
          >
            Configurações
          </Link>
          <Button
            onClick={() => abrirCriacao()}
            className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
          >
            Novo agendamento
          </Button>
        </div>
      </div>

      <FilaDeEspera aoConverter={invalidar} />

      {formAberto && (
        <form
          onSubmit={handleSubmit(aoSalvar)}
          className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6"
        >
          <h2 className="text-lg font-semibold text-[#14162B]">
            {editandoId === null ? "Novo agendamento" : "Editar agendamento"}
          </h2>

          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <div>
              <Label htmlFor="servicoId">Serviço</Label>
              <select
                id="servicoId"
                className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                aria-invalid={!!errors.servicoId}
                {...register("servicoId")}
              >
                <option value="">Escolha o serviço...</option>
                {servicos
                  .filter((s) => s.ativo)
                  .map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.nome} ({s.duracaoEstimadaMinutos} min)
                    </option>
                  ))}
              </select>
              {errors.servicoId && (
                <p className="mt-1 text-sm text-destructive">{errors.servicoId.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="clienteId">Cliente (opcional)</Label>
              <select
                id="clienteId"
                className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                {...register("clienteId")}
              >
                <option value="">Sem cliente vinculado</option>
                {clientes.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.nome} — {c.telefone}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <Label htmlFor="data">Data</Label>
              <Input
                id="data"
                type="date"
                className="mt-1 h-11"
                aria-invalid={!!errors.data}
                {...register("data")}
              />
              {errors.data && (
                <p className="mt-1 text-sm text-destructive">{errors.data.message}</p>
              )}
            </div>
            <div>
              <Label>Horário disponível</Label>
              <div className="mt-1 flex min-h-11 flex-wrap items-center gap-1.5">
                {servicoEscolhido === "" || dataEscolhida === "" ? (
                  <p className="text-sm text-[#8B8D98]">
                    Escolha serviço e data para ver os horários.
                  </p>
                ) : opcoesDeHorario.length === 0 ? (
                  <p className="text-sm text-[#8B8D98]">
                    Sem horários livres nesse dia — veja os horários de funcionamento.
                  </p>
                ) : (
                  opcoesDeHorario.map((hora) => (
                    <button
                      key={hora}
                      type="button"
                      onClick={() =>
                        setValue("horaInicio", hora, { shouldValidate: true })
                      }
                      className={`rounded-full border px-3 py-1 text-sm transition-colors ${
                        horaEscolhida === hora
                          ? "border-[#14162B] bg-[#14162B] text-white"
                          : "border-[#14162B]/15 text-[#14162B] hover:border-[#14162B]"
                      }`}
                    >
                      {horaCurta(hora)}
                    </button>
                  ))
                )}
              </div>
              {errors.horaInicio && (
                <p className="mt-1 text-sm text-destructive">{errors.horaInicio.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="nomeContato">Nome do contato</Label>
              <Input
                id="nomeContato"
                placeholder="Preenchido pelo cliente vinculado, se vazio"
                className="mt-1 h-11"
                aria-invalid={!!errors.nomeContato}
                {...register("nomeContato")}
              />
              {errors.nomeContato && (
                <p className="mt-1 text-sm text-destructive">{errors.nomeContato.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="telefoneContato">Telefone/WhatsApp</Label>
              <Input
                id="telefoneContato"
                className="mt-1 h-11"
                aria-invalid={!!errors.telefoneContato}
                {...register("telefoneContato")}
              />
              {errors.telefoneContato && (
                <p className="mt-1 text-sm text-destructive">
                  {errors.telefoneContato.message}
                </p>
              )}
            </div>
            <div>
              <Label htmlFor="aparelhoMarca">Marca do aparelho (opcional)</Label>
              <Input id="aparelhoMarca" className="mt-1 h-11" {...register("aparelhoMarca")} />
            </div>
            <div>
              <Label htmlFor="aparelhoModelo">Modelo (opcional)</Label>
              <Input
                id="aparelhoModelo"
                className="mt-1 h-11"
                {...register("aparelhoModelo")}
              />
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="descricaoProblema">Problema relatado (opcional)</Label>
              <Input
                id="descricaoProblema"
                className="mt-1 h-11"
                {...register("descricaoProblema")}
              />
            </div>
          </div>

          <div className="mt-6 flex gap-3">
            <Button
              type="submit"
              disabled={isSubmitting}
              className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
            >
              {isSubmitting ? "Salvando..." : "Salvar agendamento"}
            </Button>
            <Button type="button" variant="ghost" onClick={() => setFormAberto(false)}>
              Fechar
            </Button>
          </div>
        </form>
      )}

      <div className="mt-8 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-1">
          <Button variant="outline" className="h-9 rounded-full px-3" onClick={() => navegar(-1)}>
            ‹
          </Button>
          <Button
            variant="outline"
            className="h-9 rounded-full px-4"
            onClick={() => setDataRef(hojeIso())}
          >
            Hoje
          </Button>
          <Button variant="outline" className="h-9 rounded-full px-3" onClick={() => navegar(1)}>
            ›
          </Button>
          <span className="ml-3 text-sm font-medium text-[#14162B] capitalize">
            {visao === "mes"
              ? deIso(dataRef).toLocaleDateString("pt-BR", { month: "long", year: "numeric" })
              : visao === "semana"
                ? `${formatarDataCurta(inicioDaSemana(dataRef))} – ${formatarDataCurta(somarDias(inicioDaSemana(dataRef), 6))}`
                : formatarDataLonga(dataRef)}
          </span>
        </div>
        <div className="flex rounded-full border border-[#14162B]/10 p-0.5">
          {(["dia", "semana", "mes"] as const).map((opcao) => (
            <button
              key={opcao}
              onClick={() => setVisao(opcao)}
              className={`rounded-full px-4 py-1.5 text-sm capitalize transition-colors ${
                visao === opcao
                  ? "bg-[#14162B] font-semibold text-white"
                  : "text-[#6B7280] hover:text-[#14162B]"
              }`}
            >
              {opcao === "mes" ? "mês" : opcao}
            </button>
          ))}
        </div>
      </div>

      {visao === "dia" && (
        <div className="mt-4 space-y-2">
          {porDia(dataRef).length === 0 ? (
            <div className="rounded-2xl border border-[#14162B]/8 px-4 py-10 text-center text-sm text-[#6B7280]">
              Nenhum agendamento neste dia.
            </div>
          ) : (
            porDia(dataRef).map((a) => <CartaoAgendamento key={a.id} agendamento={a} />)
          )}
        </div>
      )}

      {visao === "semana" && (
        <div className="mt-4 grid grid-cols-2 gap-2 md:grid-cols-7">
          {diasDaSemana.map((dia) => (
            <div key={dia} className="min-h-32 rounded-2xl border border-[#14162B]/8 p-2">
              <button
                onClick={() => {
                  setDataRef(dia);
                  setVisao("dia");
                }}
                className={`w-full rounded-lg px-1 py-1 text-center text-xs font-semibold ${
                  dia === hojeIso() ? "bg-[#14162B] text-white" : "text-[#6B7280]"
                }`}
              >
                {DIAS_SEMANA_CURTOS[deIso(dia).getDay()]} {formatarDataCurta(dia)}
              </button>
              <div className="mt-1 space-y-1">
                {porDia(dia).map((a) => (
                  <button
                    key={a.id}
                    onClick={() => {
                      setDataRef(dia);
                      setVisao("dia");
                    }}
                    className={`block w-full rounded-lg px-1.5 py-1 text-left text-[11px] leading-tight ${
                      a.status === "Cancelado" || a.status === "NaoCompareceu"
                        ? "bg-[#F7F7F9] text-[#8B8D98] line-through"
                        : "bg-[#14162B]/5 text-[#14162B]"
                    }`}
                  >
                    <span className="font-semibold">{horaCurta(a.horaInicio ?? "")}</span>{" "}
                    {a.nomeContato}
                  </button>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {visao === "mes" && (
        <div className="mt-4 overflow-hidden rounded-2xl border border-[#14162B]/8">
          <div className="grid grid-cols-7 bg-[#F7F7F9] text-center text-xs text-[#8B8D98] uppercase">
            {DIAS_SEMANA_CURTOS.map((dia) => (
              <div key={dia} className="py-2">
                {dia}
              </div>
            ))}
          </div>
          <div className="grid grid-cols-7">
            {diasDoMes.map((dia) => {
              const doMes = dia.slice(0, 7) === dataRef.slice(0, 7);
              const quantidade = porDia(dia).filter(
                (a) => a.status !== "Cancelado" && a.status !== "NaoCompareceu",
              ).length;
              return (
                <button
                  key={dia}
                  onClick={() => {
                    setDataRef(dia);
                    setVisao("dia");
                  }}
                  className={`h-20 border-t border-r border-[#14162B]/6 p-1.5 text-left align-top transition-colors hover:bg-[#F7F7F9] ${
                    doMes ? "" : "opacity-40"
                  }`}
                >
                  <span
                    className={`inline-flex h-6 w-6 items-center justify-center rounded-full text-xs ${
                      dia === hojeIso()
                        ? "bg-[#14162B] font-semibold text-white"
                        : "text-[#14162B]"
                    }`}
                  >
                    {deIso(dia).getDate()}
                  </span>
                  {quantidade > 0 && (
                    <span className="mt-1 block rounded-full bg-[#E8536B]/10 px-1.5 py-0.5 text-[10px] font-semibold text-[#E8536B]">
                      {quantidade} agend.
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
