"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useParams } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiPublicoSlugDisponibilidade,
  useGetApiPublicoSlugInfo,
  usePostApiPublicoSlugAgendamentos,
  type AgendamentoPublicoResponse,
} from "@/lib/api-client/gerado";
import { formatarDataLonga, hojeIso, horaCurta } from "@/lib/agenda-datas";
import { formatarBRL } from "@/lib/formatadores";
import {
  esquemaAparelhoPortal,
  esquemaIdentificacao,
  type ValoresAparelhoPortal,
  type ValoresIdentificacao,
} from "@/lib/validators/agenda";

const ETAPAS = ["Seus dados", "Aparelho", "Serviço", "Data e horário", "Confirmação"];

/**
 * Fluxo progressivo de agendamento do portal do cliente (módulo 1, Fase 1):
 * identificação → aparelho/problema → serviço → data/horário → confirmação.
 * Sem login — a rota pública da API é resolvida pelo slug da loja.
 */
export default function PaginaAgendarPublico() {
  const { slug } = useParams<{ slug: string }>();
  const [etapa, setEtapa] = useState(0);
  const [erroEnvio, setErroEnvio] = useState<string | null>(null);
  const [confirmacao, setConfirmacao] = useState<AgendamentoPublicoResponse | null>(null);

  // Dados acumulados pelo wizard.
  const [identificacao, setIdentificacao] = useState<ValoresIdentificacao | null>(null);
  const [aparelho, setAparelho] = useState<ValoresAparelhoPortal | null>(null);
  const [servicoId, setServicoId] = useState<number | null>(null);
  const [data, setData] = useState(hojeIso());
  const [horaInicio, setHoraInicio] = useState("");

  const { data: respostaInfo, isLoading } = useGetApiPublicoSlugInfo(slug);
  const loja = respostaInfo?.status === 200 ? respostaInfo.data : null;

  const { data: respostaDisponibilidade } = useGetApiPublicoSlugDisponibilidade(
    slug,
    { servicoId: servicoId ?? undefined, data },
    { query: { enabled: etapa === 3 && servicoId !== null && data !== "" } },
  );
  const horariosLivres =
    respostaDisponibilidade?.status === 200
      ? (respostaDisponibilidade.data.horariosLivres ?? [])
      : [];

  const agendar = usePostApiPublicoSlugAgendamentos();

  const formIdentificacao = useForm<ValoresIdentificacao>({
    resolver: zodResolver(esquemaIdentificacao),
    defaultValues: { nomeContato: "", telefoneContato: "", emailContato: "" },
  });

  const formAparelho = useForm<ValoresAparelhoPortal>({
    resolver: zodResolver(esquemaAparelhoPortal),
    defaultValues: { aparelhoMarca: "", aparelhoModelo: "", descricaoProblema: "" },
  });

  const servicoEscolhido = loja?.servicos?.find((s) => s.id === servicoId) ?? null;

  async function aoConfirmar() {
    if (!identificacao || !aparelho || servicoId === null) return;
    setErroEnvio(null);
    try {
      const resposta = await agendar.mutateAsync({
        slug,
        data: {
          servicoId,
          data,
          horaInicio,
          nomeContato: identificacao.nomeContato,
          telefoneContato: identificacao.telefoneContato,
          emailContato: identificacao.emailContato || null,
          descricaoProblema: aparelho.descricaoProblema || null,
          aparelhoMarca: aparelho.aparelhoMarca,
          aparelhoModelo: aparelho.aparelhoModelo,
        },
      });
      if (resposta.status === 201) {
        setConfirmacao(resposta.data);
      }
    } catch (erro) {
      setErroEnvio(
        erro instanceof ApiError
          ? erro.message
          : "Não foi possível concluir o agendamento. Tente novamente.",
      );
    }
  }

  if (isLoading) {
    return (
      <div className="mx-auto w-full max-w-2xl px-6 py-16 text-center text-sm text-[#6B7280]">
        Carregando...
      </div>
    );
  }

  if (!loja) {
    return (
      <div className="mx-auto w-full max-w-2xl px-6 py-16 text-center">
        <h1 className="text-2xl font-bold text-[#14162B]">Loja não encontrada</h1>
        <p className="mt-2 text-sm text-[#6B7280]">
          Confira o link de agendamento com a assistência técnica.
        </p>
      </div>
    );
  }

  if (confirmacao) {
    return (
      <div className="mx-auto w-full max-w-2xl px-6 py-16 text-center">
        <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
          Agendamento confirmado
        </p>
        <h1 className="mt-2 text-3xl font-bold text-[#14162B]">Até breve!</h1>
        <div className="mx-auto mt-6 max-w-md rounded-2xl border border-[#14162B]/8 p-6 text-left text-sm">
          <p className="font-semibold text-[#14162B]">{confirmacao.nomeLoja}</p>
          <p className="mt-2 text-[#6B7280]">
            <span className="font-medium text-[#14162B]">{confirmacao.servicoNome}</span>
            <br />
            {formatarDataLonga(confirmacao.data ?? "")} às{" "}
            {horaCurta(confirmacao.horaInicio ?? "")}
          </p>
          <p className="mt-3 text-xs text-[#8B8D98]">
            Número do agendamento: #{confirmacao.id}. Guarde este número — a loja
            confirma seus dados no check-in.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto w-full max-w-2xl px-6 pb-16">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
        Agendamento online
      </p>
      <h1 className="mt-2 text-3xl font-bold text-[#14162B]">{loja.nome}</h1>
      <p className="mt-1 text-sm text-[#6B7280]">
        Agende seu atendimento em poucos passos — sem cadastro, sem senha.
      </p>

      {/* Indicador de progresso */}
      <ol className="mt-6 flex flex-wrap gap-2">
        {ETAPAS.map((rotulo, i) => (
          <li
            key={rotulo}
            className={`rounded-full px-3 py-1 text-xs ${
              i === etapa
                ? "bg-[#14162B] font-semibold text-white"
                : i < etapa
                  ? "bg-[#14162B]/10 text-[#14162B]"
                  : "bg-[#F7F7F9] text-[#8B8D98]"
            }`}
          >
            {i + 1}. {rotulo}
          </li>
        ))}
      </ol>

      <div className="mt-6 rounded-2xl border border-[#14162B]/8 p-6">
        {etapa === 0 && (
          <form
            onSubmit={formIdentificacao.handleSubmit((valores) => {
              setIdentificacao(valores);
              setEtapa(1);
            })}
          >
            <h2 className="text-lg font-semibold text-[#14162B]">Como falamos com você?</h2>
            <div className="mt-4 space-y-4">
              <div>
                <Label htmlFor="nomeContato">Nome</Label>
                <Input
                  id="nomeContato"
                  className="mt-1 h-11"
                  aria-invalid={!!formIdentificacao.formState.errors.nomeContato}
                  {...formIdentificacao.register("nomeContato")}
                />
                {formIdentificacao.formState.errors.nomeContato && (
                  <p className="mt-1 text-sm text-destructive">
                    {formIdentificacao.formState.errors.nomeContato.message}
                  </p>
                )}
              </div>
              <div>
                <Label htmlFor="telefoneContato">Telefone/WhatsApp</Label>
                <Input
                  id="telefoneContato"
                  placeholder="(11) 99999-0000"
                  className="mt-1 h-11"
                  aria-invalid={!!formIdentificacao.formState.errors.telefoneContato}
                  {...formIdentificacao.register("telefoneContato")}
                />
                {formIdentificacao.formState.errors.telefoneContato && (
                  <p className="mt-1 text-sm text-destructive">
                    {formIdentificacao.formState.errors.telefoneContato.message}
                  </p>
                )}
              </div>
              <div>
                <Label htmlFor="emailContato">E-mail (opcional)</Label>
                <Input
                  id="emailContato"
                  className="mt-1 h-11"
                  aria-invalid={!!formIdentificacao.formState.errors.emailContato}
                  {...formIdentificacao.register("emailContato")}
                />
                {formIdentificacao.formState.errors.emailContato && (
                  <p className="mt-1 text-sm text-destructive">
                    {formIdentificacao.formState.errors.emailContato.message}
                  </p>
                )}
              </div>
            </div>
            <Button
              type="submit"
              className="mt-6 h-11 rounded-full bg-[#14162B] px-8 text-white hover:bg-[#14162B]/90"
            >
              Continuar
            </Button>
          </form>
        )}

        {etapa === 1 && (
          <form
            onSubmit={formAparelho.handleSubmit((valores) => {
              setAparelho(valores);
              setEtapa(2);
            })}
          >
            <h2 className="text-lg font-semibold text-[#14162B]">Qual é o aparelho?</h2>
            <div className="mt-4 space-y-4">
              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <Label htmlFor="aparelhoMarca">Marca</Label>
                  <Input
                    id="aparelhoMarca"
                    placeholder="Samsung, Apple..."
                    className="mt-1 h-11"
                    aria-invalid={!!formAparelho.formState.errors.aparelhoMarca}
                    {...formAparelho.register("aparelhoMarca")}
                  />
                  {formAparelho.formState.errors.aparelhoMarca && (
                    <p className="mt-1 text-sm text-destructive">
                      {formAparelho.formState.errors.aparelhoMarca.message}
                    </p>
                  )}
                </div>
                <div>
                  <Label htmlFor="aparelhoModelo">Modelo</Label>
                  <Input
                    id="aparelhoModelo"
                    placeholder="Galaxy A54, iPhone 13..."
                    className="mt-1 h-11"
                    aria-invalid={!!formAparelho.formState.errors.aparelhoModelo}
                    {...formAparelho.register("aparelhoModelo")}
                  />
                  {formAparelho.formState.errors.aparelhoModelo && (
                    <p className="mt-1 text-sm text-destructive">
                      {formAparelho.formState.errors.aparelhoModelo.message}
                    </p>
                  )}
                </div>
              </div>
              <div>
                <Label htmlFor="descricaoProblema">O que está acontecendo? (opcional)</Label>
                <Input
                  id="descricaoProblema"
                  placeholder="Tela trincada, não liga, bateria..."
                  className="mt-1 h-11"
                  {...formAparelho.register("descricaoProblema")}
                />
              </div>
            </div>
            <div className="mt-6 flex gap-3">
              <Button
                type="submit"
                className="h-11 rounded-full bg-[#14162B] px-8 text-white hover:bg-[#14162B]/90"
              >
                Continuar
              </Button>
              <Button type="button" variant="ghost" onClick={() => setEtapa(0)}>
                Voltar
              </Button>
            </div>
          </form>
        )}

        {etapa === 2 && (
          <div>
            <h2 className="text-lg font-semibold text-[#14162B]">Qual serviço você precisa?</h2>
            {(loja.servicos?.length ?? 0) === 0 ? (
              <p className="mt-4 text-sm text-[#6B7280]">
                Esta loja ainda não tem serviços com agendamento online. Entre em
                contato direto com a assistência.
              </p>
            ) : (
              <div className="mt-4 grid gap-3 sm:grid-cols-2">
                {loja.servicos?.map((servico) => (
                  <button
                    key={servico.id}
                    type="button"
                    onClick={() => setServicoId(servico.id ?? null)}
                    className={`rounded-2xl border p-4 text-left transition-colors ${
                      servicoId === servico.id
                        ? "border-[#14162B] bg-[#14162B]/[0.03]"
                        : "border-[#14162B]/10 hover:border-[#14162B]/40"
                    }`}
                  >
                    <p className="font-semibold text-[#14162B]">{servico.nome}</p>
                    <p className="mt-1 text-sm text-[#6B7280]">
                      a partir de {formatarBRL(servico.precoBase ?? 0)} ·{" "}
                      {servico.duracaoEstimadaMinutos} min
                    </p>
                    {servico.exigeDiagnostico && (
                      <p className="mt-1 text-xs text-[#E8536B]">
                        valor final após diagnóstico
                      </p>
                    )}
                  </button>
                ))}
              </div>
            )}
            <div className="mt-6 flex gap-3">
              <Button
                type="button"
                disabled={servicoId === null}
                onClick={() => setEtapa(3)}
                className="h-11 rounded-full bg-[#14162B] px-8 text-white hover:bg-[#14162B]/90"
              >
                Continuar
              </Button>
              <Button type="button" variant="ghost" onClick={() => setEtapa(1)}>
                Voltar
              </Button>
            </div>
          </div>
        )}

        {etapa === 3 && (
          <div>
            <h2 className="text-lg font-semibold text-[#14162B]">Quando fica bom para você?</h2>
            <div className="mt-4">
              <Label htmlFor="dataAgendamento">Data</Label>
              <Input
                id="dataAgendamento"
                type="date"
                min={hojeIso()}
                value={data}
                onChange={(e) => {
                  setData(e.target.value);
                  setHoraInicio("");
                }}
                className="mt-1 h-11 max-w-56"
              />
            </div>
            <div className="mt-4">
              <Label>Horários disponíveis</Label>
              <div className="mt-2 flex flex-wrap gap-1.5">
                {horariosLivres.length === 0 ? (
                  <p className="text-sm text-[#8B8D98]">
                    Nenhum horário livre nesta data — tente outro dia.
                  </p>
                ) : (
                  horariosLivres.map((hora) => (
                    <button
                      key={hora}
                      type="button"
                      onClick={() => setHoraInicio(hora)}
                      className={`rounded-full border px-4 py-1.5 text-sm transition-colors ${
                        horaInicio === hora
                          ? "border-[#14162B] bg-[#14162B] text-white"
                          : "border-[#14162B]/15 text-[#14162B] hover:border-[#14162B]"
                      }`}
                    >
                      {horaCurta(hora)}
                    </button>
                  ))
                )}
              </div>
            </div>
            <div className="mt-6 flex gap-3">
              <Button
                type="button"
                disabled={horaInicio === ""}
                onClick={() => setEtapa(4)}
                className="h-11 rounded-full bg-[#14162B] px-8 text-white hover:bg-[#14162B]/90"
              >
                Continuar
              </Button>
              <Button type="button" variant="ghost" onClick={() => setEtapa(2)}>
                Voltar
              </Button>
            </div>
          </div>
        )}

        {etapa === 4 && identificacao && aparelho && servicoEscolhido && (
          <div>
            <h2 className="text-lg font-semibold text-[#14162B]">Confirme seu agendamento</h2>
            <dl className="mt-4 space-y-2 text-sm">
              <div className="flex justify-between gap-4">
                <dt className="text-[#6B7280]">Serviço</dt>
                <dd className="font-medium text-[#14162B]">{servicoEscolhido.nome}</dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-[#6B7280]">Quando</dt>
                <dd className="font-medium text-[#14162B]">
                  {formatarDataLonga(data)} às {horaCurta(horaInicio)}
                </dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-[#6B7280]">Aparelho</dt>
                <dd className="font-medium text-[#14162B]">
                  {aparelho.aparelhoMarca} {aparelho.aparelhoModelo}
                </dd>
              </div>
              <div className="flex justify-between gap-4">
                <dt className="text-[#6B7280]">Contato</dt>
                <dd className="font-medium text-[#14162B]">
                  {identificacao.nomeContato} · {identificacao.telefoneContato}
                </dd>
              </div>
              {aparelho.descricaoProblema && (
                <div className="flex justify-between gap-4">
                  <dt className="text-[#6B7280]">Problema</dt>
                  <dd className="text-right font-medium text-[#14162B]">
                    {aparelho.descricaoProblema}
                  </dd>
                </div>
              )}
            </dl>
            {erroEnvio && <p className="mt-4 text-sm text-destructive">{erroEnvio}</p>}
            <div className="mt-6 flex gap-3">
              <Button
                type="button"
                disabled={agendar.isPending}
                onClick={aoConfirmar}
                className="h-11 rounded-full bg-[#14162B] px-8 text-white hover:bg-[#14162B]/90"
              >
                {agendar.isPending ? "Enviando..." : "Confirmar agendamento"}
              </Button>
              <Button type="button" variant="ghost" onClick={() => setEtapa(3)}>
                Voltar
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
