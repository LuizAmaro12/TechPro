"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useDeleteApiAgendaBloqueiosId,
  useGetApiAgendaBloqueios,
  useGetApiAgendaConfiguracoes,
  useGetApiAgendaHorarios,
  usePostApiAgendaBloqueios,
  usePutApiAgendaConfiguracoes,
  usePutApiAgendaHorarios,
} from "@/lib/api-client/gerado";
import { DIAS_SEMANA_LONGOS, formatarDataCurta, hojeIso, horaCurta } from "@/lib/agenda-datas";
import {
  esquemaBloqueio,
  esquemaSlug,
  type ValoresBloqueio,
  type ValoresSlug,
} from "@/lib/validators/agenda";

type DiaForm = {
  ativo: boolean;
  abertura: string;
  fechamento: string;
  intervaloInicio: string;
  intervaloFim: string;
};

const DIA_PADRAO: DiaForm = {
  ativo: false,
  abertura: "09:00",
  fechamento: "18:00",
  intervaloInicio: "",
  intervaloFim: "",
};

export default function PaginaConfiguracoesAgenda() {
  const queryClient = useQueryClient();

  const { data: respostaHorarios } = useGetApiAgendaHorarios();
  const { data: respostaConfiguracao } = useGetApiAgendaConfiguracoes();
  const { data: respostaBloqueios } = useGetApiAgendaBloqueios({ deData: hojeIso() });
  const bloqueios = respostaBloqueios?.status === 200 ? respostaBloqueios.data : [];

  const salvarSlug = usePutApiAgendaConfiguracoes();
  const criarBloqueio = usePostApiAgendaBloqueios();
  const removerBloqueio = useDeleteApiAgendaBloqueiosId();

  // O form de horários nasce já com os dados da API (componente filho é
  // montado só depois da resposta) — sem sincronizar estado em efeito.
  const diasIniciais =
    respostaHorarios?.status === 200
      ? Array.from({ length: 7 }, (_, dia) => {
          const salvo = respostaHorarios.data.find((h) => h.diaSemana === dia);
          return salvo?.ativo
            ? {
                ativo: true,
                abertura: horaCurta(salvo.abertura ?? "09:00"),
                fechamento: horaCurta(salvo.fechamento ?? "18:00"),
                intervaloInicio: salvo.intervaloInicio ? horaCurta(salvo.intervaloInicio) : "",
                intervaloFim: salvo.intervaloFim ? horaCurta(salvo.intervaloFim) : "",
              }
            : DIA_PADRAO;
        })
      : null;

  const formSlug = useForm<ValoresSlug>({
    resolver: zodResolver(esquemaSlug),
    defaultValues: { slug: "" },
  });

  useEffect(() => {
    if (respostaConfiguracao?.status === 200 && !formSlug.formState.isDirty) {
      formSlug.reset({ slug: respostaConfiguracao.data.slug ?? "" });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- só sincroniza quando a API responde
  }, [respostaConfiguracao]);

  const formBloqueio = useForm<ValoresBloqueio>({
    resolver: zodResolver(esquemaBloqueio),
    defaultValues: { data: hojeIso(), horaInicio: "", horaFim: "", motivo: "" },
  });

  async function aoSalvarSlug(valores: ValoresSlug) {
    try {
      await salvarSlug.mutateAsync({ data: { slug: valores.slug } });
      toast.success("Endereço público atualizado.");
      queryClient.invalidateQueries({ queryKey: ["/api/agenda/configuracoes"] });
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar o endereço.");
    }
  }

  async function aoCriarBloqueio(valores: ValoresBloqueio) {
    try {
      await criarBloqueio.mutateAsync({
        data: {
          data: valores.data,
          horaInicio: `${valores.horaInicio}:00`,
          horaFim: `${valores.horaFim}:00`,
          motivo: valores.motivo || null,
        },
      });
      toast.success("Bloqueio criado.");
      formBloqueio.reset({ data: hojeIso(), horaInicio: "", horaFim: "", motivo: "" });
      queryClient.invalidateQueries({ queryKey: ["/api/agenda/bloqueios"] });
      queryClient.invalidateQueries({ queryKey: ["/api/agenda/disponibilidade"] });
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao criar o bloqueio.");
    }
  }

  async function aoRemoverBloqueio(id: number | undefined) {
    if (id === undefined) return;
    try {
      await removerBloqueio.mutateAsync({ id });
      toast.success("Bloqueio removido.");
      queryClient.invalidateQueries({ queryKey: ["/api/agenda/bloqueios"] });
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao remover o bloqueio.");
    }
  }

  const slugAtual =
    respostaConfiguracao?.status === 200 ? respostaConfiguracao.data.slug : undefined;
  const urlPublica =
    typeof window !== "undefined" && slugAtual
      ? `${window.location.origin}/agendar/${slugAtual}`
      : null;

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-marca uppercase">
        Agenda
      </p>
      <h1 className="mt-2 text-3xl font-bold text-tinta">Configurações da agenda</h1>
      <p className="mt-1 text-sm text-tinta-suave">
        Horários de funcionamento, bloqueios pontuais e o link público de agendamento.{" "}
        <Link href="/agenda" className="underline underline-offset-4 hover:text-tinta">
          Voltar para a agenda
        </Link>
      </p>

      {/* --- Link público ------------------------------------------------- */}
      <section className="mt-8 rounded-2xl border border-borda p-6">
        <h2 className="text-lg font-semibold text-tinta">Link público de agendamento</h2>
        <p className="mt-1 text-sm text-tinta-suave">
          Divulgue este endereço para os seus clientes agendarem sozinhos.
        </p>
        <form
          onSubmit={formSlug.handleSubmit(aoSalvarSlug)}
          className="mt-4 flex flex-wrap items-end gap-3"
        >
          <div className="min-w-64 flex-1">
            <Label htmlFor="slug">Endereço da loja</Label>
            <Input
              id="slug"
              className="mt-1 h-11"
              aria-invalid={!!formSlug.formState.errors.slug}
              {...formSlug.register("slug")}
            />
            {formSlug.formState.errors.slug && (
              <p className="mt-1 text-sm text-destructive">
                {formSlug.formState.errors.slug.message}
              </p>
            )}
          </div>
          <Button
            type="submit"
            disabled={formSlug.formState.isSubmitting}
            className="h-11 rounded-full bg-tinta px-6 text-sobre-tinta hover:bg-tinta/90"
          >
            Salvar endereço
          </Button>
        </form>
        {urlPublica && (
          <div className="mt-3 flex flex-wrap items-center gap-2 text-sm">
            <code className="rounded-lg bg-sutil px-3 py-1.5 text-tinta">
              {urlPublica}
            </code>
            <Button
              type="button"
              variant="outline"
              className="h-8 rounded-full px-3 text-xs"
              onClick={() => {
                navigator.clipboard.writeText(urlPublica);
                toast.success("Link copiado.");
              }}
            >
              Copiar
            </Button>
          </div>
        )}
      </section>

      {/* --- Horários de funcionamento ------------------------------------ */}
      {diasIniciais ? (
        <SecaoHorarios inicial={diasIniciais} />
      ) : (
        <section className="mt-6 rounded-2xl border border-borda p-6">
          <p className="text-sm text-tinta-suave">Carregando horários...</p>
        </section>
      )}

      {/* --- Bloqueios ------------------------------------------------------ */}
      <section className="mt-6 rounded-2xl border border-borda p-6">
        <h2 className="text-lg font-semibold text-tinta">Bloqueios de agenda</h2>
        <p className="mt-1 text-sm text-tinta-suave">
          Feriados, ausências ou manutenção: o período bloqueado some da agenda pública.
        </p>
        <form
          onSubmit={formBloqueio.handleSubmit(aoCriarBloqueio)}
          className="mt-4 flex flex-wrap items-end gap-3"
        >
          <div>
            <Label htmlFor="bloqueioData">Data</Label>
            <Input
              id="bloqueioData"
              type="date"
              className="mt-1 h-11"
              aria-invalid={!!formBloqueio.formState.errors.data}
              {...formBloqueio.register("data")}
            />
          </div>
          <div>
            <Label htmlFor="bloqueioInicio">Das</Label>
            <Input
              id="bloqueioInicio"
              type="time"
              className="mt-1 h-11 w-28"
              aria-invalid={!!formBloqueio.formState.errors.horaInicio}
              {...formBloqueio.register("horaInicio")}
            />
          </div>
          <div>
            <Label htmlFor="bloqueioFim">Até</Label>
            <Input
              id="bloqueioFim"
              type="time"
              className="mt-1 h-11 w-28"
              aria-invalid={!!formBloqueio.formState.errors.horaFim}
              {...formBloqueio.register("horaFim")}
            />
          </div>
          <div className="min-w-48 flex-1">
            <Label htmlFor="bloqueioMotivo">Motivo (opcional)</Label>
            <Input
              id="bloqueioMotivo"
              className="mt-1 h-11"
              {...formBloqueio.register("motivo")}
            />
          </div>
          <Button
            type="submit"
            disabled={formBloqueio.formState.isSubmitting}
            className="h-11 rounded-full bg-tinta px-6 text-sobre-tinta hover:bg-tinta/90"
          >
            Bloquear
          </Button>
        </form>
        {(formBloqueio.formState.errors.horaFim ||
          formBloqueio.formState.errors.horaInicio ||
          formBloqueio.formState.errors.data) && (
          <p className="mt-2 text-sm text-destructive">
            {formBloqueio.formState.errors.data?.message ??
              formBloqueio.formState.errors.horaInicio?.message ??
              formBloqueio.formState.errors.horaFim?.message}
          </p>
        )}

        <div className="mt-4 space-y-2">
          {bloqueios.length === 0 ? (
            <p className="text-sm text-tinta-fraca">Nenhum bloqueio futuro.</p>
          ) : (
            bloqueios.map((b) => (
              <div
                key={b.id}
                className="flex items-center justify-between rounded-xl border border-borda px-3 py-2 text-sm"
              >
                <span className="text-tinta">
                  <span className="font-medium">{formatarDataCurta(b.data ?? "")}</span> ·{" "}
                  {horaCurta(b.horaInicio ?? "")}–{horaCurta(b.horaFim ?? "")}
                  {b.motivo && <span className="text-tinta-suave"> — {b.motivo}</span>}
                </span>
                <Button
                  variant="ghost"
                  className="h-8 px-3 text-xs text-marca hover:text-marca"
                  onClick={() => aoRemoverBloqueio(b.id)}
                >
                  Remover
                </Button>
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  );
}

/**
 * Seção de horários com estado próprio: montada só depois da resposta da API,
 * então o useState já nasce com os dados — sem sincronizar estado em efeito.
 */
function SecaoHorarios({ inicial }: { inicial: DiaForm[] }) {
  const queryClient = useQueryClient();
  const [dias, setDias] = useState<DiaForm[]>(inicial);
  const [salvando, setSalvando] = useState(false);
  const salvarHorarios = usePutApiAgendaHorarios();

  function alterarDia(indice: number, mudanca: Partial<DiaForm>) {
    setDias((atual) => atual.map((d, i) => (i === indice ? { ...d, ...mudanca } : d)));
  }

  async function aoSalvar() {
    // Validação leve no cliente; a regra completa mora no back-end.
    for (const [i, dia] of dias.entries()) {
      if (dia.ativo && dia.abertura >= dia.fechamento) {
        toast.error(`${DIAS_SEMANA_LONGOS[i]}: a abertura deve ser antes do fechamento.`);
        return;
      }
      if (dia.ativo && (dia.intervaloInicio === "") !== (dia.intervaloFim === "")) {
        toast.error(`${DIAS_SEMANA_LONGOS[i]}: preencha início e fim do intervalo.`);
        return;
      }
    }

    setSalvando(true);
    try {
      await salvarHorarios.mutateAsync({
        data: {
          dias: dias.map((dia, diaSemana) => ({
            diaSemana,
            ativo: dia.ativo,
            abertura: dia.ativo ? `${dia.abertura}:00` : null,
            fechamento: dia.ativo ? `${dia.fechamento}:00` : null,
            intervaloInicio:
              dia.ativo && dia.intervaloInicio ? `${dia.intervaloInicio}:00` : null,
            intervaloFim: dia.ativo && dia.intervaloFim ? `${dia.intervaloFim}:00` : null,
          })),
        },
      });
      toast.success("Horários salvos.");
      queryClient.invalidateQueries({ queryKey: ["/api/agenda/horarios"] });
      queryClient.invalidateQueries({ queryKey: ["/api/agenda/disponibilidade"] });
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar os horários.");
    } finally {
      setSalvando(false);
    }
  }

  return (
    <section className="mt-6 rounded-2xl border border-borda p-6">
      <h2 className="text-lg font-semibold text-tinta">Horários de funcionamento</h2>
      <p className="mt-1 text-sm text-tinta-suave">
        Dias desmarcados ficam fechados — nenhum horário é oferecido neles.
      </p>
      <div className="mt-4 space-y-2">
        {dias.map((dia, i) => (
          <div
            key={DIAS_SEMANA_LONGOS[i]}
            className="flex flex-wrap items-center gap-3 rounded-xl border border-borda px-3 py-2"
          >
            <label className="flex w-40 items-center gap-2 text-sm font-medium text-tinta">
              <input
                type="checkbox"
                checked={dia.ativo}
                onChange={(e) => alterarDia(i, { ativo: e.target.checked })}
              />
              {DIAS_SEMANA_LONGOS[i]}
            </label>
            {dia.ativo ? (
              <div className="flex flex-wrap items-center gap-2 text-sm text-tinta-suave">
                <Input
                  type="time"
                  aria-label={`Abertura ${DIAS_SEMANA_LONGOS[i]}`}
                  value={dia.abertura}
                  onChange={(e) => alterarDia(i, { abertura: e.target.value })}
                  className="h-9 w-28"
                />
                às
                <Input
                  type="time"
                  aria-label={`Fechamento ${DIAS_SEMANA_LONGOS[i]}`}
                  value={dia.fechamento}
                  onChange={(e) => alterarDia(i, { fechamento: e.target.value })}
                  className="h-9 w-28"
                />
                <span className="ml-2">intervalo (opcional)</span>
                <Input
                  type="time"
                  aria-label={`Início do intervalo ${DIAS_SEMANA_LONGOS[i]}`}
                  value={dia.intervaloInicio}
                  onChange={(e) => alterarDia(i, { intervaloInicio: e.target.value })}
                  className="h-9 w-28"
                />
                <Input
                  type="time"
                  aria-label={`Fim do intervalo ${DIAS_SEMANA_LONGOS[i]}`}
                  value={dia.intervaloFim}
                  onChange={(e) => alterarDia(i, { intervaloFim: e.target.value })}
                  className="h-9 w-28"
                />
              </div>
            ) : (
              <span className="text-sm text-tinta-fraca">Fechado</span>
            )}
          </div>
        ))}
      </div>
      <Button
        onClick={aoSalvar}
        disabled={salvando}
        className="mt-4 h-11 rounded-full bg-tinta px-6 text-sobre-tinta hover:bg-tinta/90"
      >
        {salvando ? "Salvando..." : "Salvar horários"}
      </Button>
    </section>
  );
}
