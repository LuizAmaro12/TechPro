"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiAgendaConfiguracoes,
  useGetApiAuthMe,
  usePostApiOnboardingConcluir,
  usePostApiOnboardingDadosExemplo,
  usePostApiPecas,
  usePostApiServicos,
  usePutApiAgendaConfiguracoes,
  usePutApiAgendaHorarios,
} from "@/lib/api-client/gerado";
import { DIAS_SEMANA_LONGOS } from "@/lib/agenda-datas";

// Sugestões pré-preenchidas de serviços comuns (editáveis antes de cadastrar).
const SERVICOS_SUGERIDOS = [
  { nome: "Troca de tela", precoBase: 350, duracao: 60 },
  { nome: "Troca de bateria", precoBase: 180, duracao: 40 },
  { nome: "Troca de conector de carga", precoBase: 150, duracao: 60 },
  { nome: "Limpeza interna", precoBase: 80, duracao: 30 },
  { nome: "Troca de película", precoBase: 40, duracao: 15 },
];

const PECAS_SUGERIDAS = [
  { nome: "Tela compatível", custo: 120, venda: 300 },
  { nome: "Bateria", custo: 60, venda: 150 },
  { nome: "Conector de carga", custo: 15, venda: 60 },
];

const PASSOS = ["Sua loja", "Horários", "Serviços", "Peças", "Tudo pronto"];

export default function PaginaBemVindo() {
  const router = useRouter();
  const [passo, setPasso] = useState(0);

  const { data: respostaMe } = useGetApiAuthMe();
  const nomeEmpresa = respostaMe?.status === 200 ? respostaMe.data.empresa?.nome : undefined;
  const { data: respostaConfig } = useGetApiAgendaConfiguracoes();

  // Passo 1 — slug. Deriva do valor da API até o dono editar (sem efeito).
  const slugAtual = respostaConfig?.status === 200 ? (respostaConfig.data.slug ?? "") : "";
  const [slugEditado, setSlugEditado] = useState<string | null>(null);
  const slug = slugEditado ?? slugAtual;
  const setSlug = setSlugEditado;

  // Passo 2 — horários (setup rápido: dias abertos + um horário para todos)
  const [diasAbertos, setDiasAbertos] = useState<boolean[]>([
    false, true, true, true, true, true, false,
  ]);
  const [abertura, setAbertura] = useState("09:00");
  const [fechamento, setFechamento] = useState("18:00");

  // Passo 3 — serviços selecionados (com preço/duração editáveis)
  const [servicos, setServicos] = useState(
    SERVICOS_SUGERIDOS.map((s) => ({ ...s, marcado: true })),
  );

  // Passo 4 — peças selecionadas
  const [pecas, setPecas] = useState(PECAS_SUGERIDAS.map((p) => ({ ...p, marcado: false })));

  // Passo 5 — dados de exemplo
  const [comExemplo, setComExemplo] = useState(false);

  const salvarSlug = usePutApiAgendaConfiguracoes();
  const salvarHorarios = usePutApiAgendaHorarios();
  const criarServico = usePostApiServicos();
  const criarPeca = usePostApiPecas();
  const carregarExemplo = usePostApiOnboardingDadosExemplo();
  const concluir = usePostApiOnboardingConcluir();
  const [processando, setProcessando] = useState(false);

  async function pular() {
    try {
      await concluir.mutateAsync();
    } finally {
      router.replace("/dashboard");
    }
  }

  async function avancarSlug() {
    setProcessando(true);
    try {
      await salvarSlug.mutateAsync({ data: { slug } });
      setPasso(1);
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Não foi possível salvar o endereço.");
    } finally {
      setProcessando(false);
    }
  }

  async function avancarHorarios() {
    setProcessando(true);
    try {
      await salvarHorarios.mutateAsync({
        data: {
          dias: diasAbertos.map((aberto, diaSemana) => ({
            diaSemana,
            ativo: aberto,
            abertura: aberto ? `${abertura}:00` : null,
            fechamento: aberto ? `${fechamento}:00` : null,
            intervaloInicio: null,
            intervaloFim: null,
          })),
        },
      });
      setPasso(2);
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Não foi possível salvar os horários.");
    } finally {
      setProcessando(false);
    }
  }

  async function avancarServicos() {
    const escolhidos = servicos.filter((s) => s.marcado);
    setProcessando(true);
    try {
      for (const s of escolhidos) {
        await criarServico.mutateAsync({
          data: {
            nome: s.nome,
            categoria: "Reparo",
            precoBase: s.precoBase,
            duracaoEstimadaMinutos: s.duracao,
            prazoMedioDias: null,
            exigeDiagnostico: false,
            agendavelOnline: true,
            capacidadeSimultanea: 1,
            ativo: true,
            checklist: [],
            pecas: [],
          },
        });
      }
      if (escolhidos.length > 0) toast.success(`${escolhidos.length} serviço(s) cadastrado(s).`);
      setPasso(3);
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao cadastrar os serviços.");
    } finally {
      setProcessando(false);
    }
  }

  async function avancarPecas() {
    const escolhidas = pecas.filter((p) => p.marcado);
    setProcessando(true);
    try {
      for (const p of escolhidas) {
        await criarPeca.mutateAsync({
          data: {
            nome: p.nome,
            descricao: null,
            custoUnitario: p.custo,
            precoVenda: p.venda,
            quantidadeEmEstoque: 0,
            estoqueMinimo: 1,
            fornecedorId: null,
            ativo: true,
          },
        });
      }
      if (escolhidas.length > 0) toast.success(`${escolhidas.length} peça(s) cadastrada(s).`);
      setPasso(4);
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao cadastrar as peças.");
    } finally {
      setProcessando(false);
    }
  }

  async function finalizar() {
    setProcessando(true);
    try {
      if (comExemplo) {
        await carregarExemplo.mutateAsync();
      }
      await concluir.mutateAsync();
      toast.success("Tudo pronto! Bem-vindo à TechPro.");
      router.replace("/dashboard");
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao finalizar.");
      setProcessando(false);
    }
  }

  return (
    <div className="mx-auto w-full max-w-2xl px-6 py-10">
      <div className="flex items-center justify-between">
        <p className="text-[11px] font-semibold tracking-[0.18em] text-marca uppercase">
          Vamos configurar sua loja
        </p>
        <button
          onClick={pular}
          className="text-sm text-tinta-fraca underline-offset-4 hover:text-tinta hover:underline"
        >
          Pular por agora
        </button>
      </div>

      {/* Progresso */}
      <ol className="mt-4 flex flex-wrap gap-2">
        {PASSOS.map((rotulo, i) => (
          <li
            key={rotulo}
            className={`rounded-full px-3 py-1 text-xs ${
              i === passo
                ? "bg-tinta font-semibold text-sobre-tinta"
                : i < passo
                  ? "bg-sutil text-tinta"
                  : "bg-sutil text-tinta-fraca"
            }`}
          >
            {i + 1}. {rotulo}
          </li>
        ))}
      </ol>

      <div className="mt-6 rounded-2xl border border-borda bg-superficie p-6">
        {passo === 0 && (
          <div>
            <h1 className="text-2xl font-bold text-tinta">
              Olá! Vamos preparar a {nomeEmpresa ?? "sua loja"} 👋
            </h1>
            <p className="mt-2 text-sm text-tinta-suave">
              Em poucos passos sua assistência fica pronta para receber
              agendamentos e abrir ordens de serviço. Comece confirmando o
              endereço público que seus clientes vão usar para agendar.
            </p>
            <div className="mt-4">
              <Label htmlFor="slug">Endereço público de agendamento</Label>
              <div className="mt-1 flex items-center gap-1 text-sm text-tinta-suave">
                <span className="whitespace-nowrap">/agendar/</span>
                <Input id="slug" value={slug} onChange={(e) => setSlug(e.target.value)} className="h-11" />
              </div>
              <p className="mt-1 text-xs text-tinta-fraca">
                Letras minúsculas, números e hífens. Dá para mudar depois nas
                configurações da agenda.
              </p>
            </div>
            <div className="mt-6">
              <Button
                onClick={avancarSlug}
                disabled={processando || slug.trim().length < 3}
                className="h-11 rounded-full bg-tinta px-8 text-sobre-tinta hover:bg-tinta/90"
              >
                Continuar
              </Button>
            </div>
          </div>
        )}

        {passo === 1 && (
          <div>
            <h1 className="text-2xl font-bold text-tinta">Horário de funcionamento</h1>
            <p className="mt-2 text-sm text-tinta-suave">
              Marque os dias abertos e o horário. Depois dá para ajustar cada dia
              (e intervalos) nas configurações da agenda.
            </p>
            <div className="mt-4 flex flex-wrap gap-2">
              {DIAS_SEMANA_LONGOS.map((dia, i) => (
                <button
                  key={dia}
                  type="button"
                  onClick={() =>
                    setDiasAbertos((atual) => atual.map((v, j) => (j === i ? !v : v)))
                  }
                  className={`rounded-full border px-3 py-1.5 text-sm transition-colors ${
                    diasAbertos[i]
                      ? "border-tinta bg-tinta text-sobre-tinta"
                      : "border-borda-forte text-tinta-suave hover:border-tinta"
                  }`}
                >
                  {dia.slice(0, 3)}
                </button>
              ))}
            </div>
            <div className="mt-4 flex flex-wrap items-end gap-3">
              <div>
                <Label htmlFor="abertura">Abre às</Label>
                <Input
                  id="abertura"
                  type="time"
                  value={abertura}
                  onChange={(e) => setAbertura(e.target.value)}
                  className="mt-1 h-11 w-32"
                />
              </div>
              <div>
                <Label htmlFor="fechamento">Fecha às</Label>
                <Input
                  id="fechamento"
                  type="time"
                  value={fechamento}
                  onChange={(e) => setFechamento(e.target.value)}
                  className="mt-1 h-11 w-32"
                />
              </div>
            </div>
            <PassoRodape
              processando={processando}
              onVoltar={() => setPasso(0)}
              onAvancar={avancarHorarios}
              avancarDesabilitado={!diasAbertos.some(Boolean) || abertura >= fechamento}
            />
          </div>
        )}

        {passo === 2 && (
          <div>
            <h1 className="text-2xl font-bold text-tinta">Seus primeiros serviços</h1>
            <p className="mt-2 text-sm text-tinta-suave">
              Escolha os serviços que sua loja faz — ajuste preço e duração à
              vontade. Você pode adicionar mais depois no catálogo.
            </p>
            <div className="mt-4 space-y-2">
              {servicos.map((s, i) => (
                <div
                  key={s.nome}
                  className="flex flex-wrap items-center gap-3 rounded-xl border border-borda px-3 py-2"
                >
                  <label className="flex flex-1 items-center gap-2 text-sm font-medium text-tinta">
                    <input
                      type="checkbox"
                      checked={s.marcado}
                      onChange={() =>
                        setServicos((atual) =>
                          atual.map((x, j) => (j === i ? { ...x, marcado: !x.marcado } : x)),
                        )
                      }
                    />
                    {s.nome}
                  </label>
                  <div className="flex items-center gap-1 text-sm text-tinta-suave">
                    R$
                    <Input
                      type="number"
                      min="0"
                      step="0.01"
                      value={s.precoBase}
                      onChange={(e) =>
                        setServicos((atual) =>
                          atual.map((x, j) =>
                            j === i ? { ...x, precoBase: Number(e.target.value) } : x,
                          ),
                        )
                      }
                      className="h-9 w-24"
                    />
                    <Input
                      type="number"
                      min="0"
                      value={s.duracao}
                      onChange={(e) =>
                        setServicos((atual) =>
                          atual.map((x, j) =>
                            j === i ? { ...x, duracao: Number(e.target.value) } : x,
                          ),
                        )
                      }
                      className="h-9 w-20"
                    />
                    min
                  </div>
                </div>
              ))}
            </div>
            <PassoRodape
              processando={processando}
              onVoltar={() => setPasso(1)}
              onAvancar={avancarServicos}
            />
          </div>
        )}

        {passo === 3 && (
          <div>
            <h1 className="text-2xl font-bold text-tinta">Peças (opcional)</h1>
            <p className="mt-2 text-sm text-tinta-suave">
              Se você controla estoque de peças, marque as que usa. Pode pular e
              cadastrar depois — a quantidade começa em zero.
            </p>
            <div className="mt-4 space-y-2">
              {pecas.map((p, i) => (
                <label
                  key={p.nome}
                  className="flex items-center gap-3 rounded-xl border border-borda px-3 py-2 text-sm text-tinta"
                >
                  <input
                    type="checkbox"
                    checked={p.marcado}
                    onChange={() =>
                      setPecas((atual) =>
                        atual.map((x, j) => (j === i ? { ...x, marcado: !x.marcado } : x)),
                      )
                    }
                  />
                  <span className="flex-1 font-medium">{p.nome}</span>
                  <span className="text-xs text-tinta-fraca">
                    custo R$ {p.custo} · venda R$ {p.venda}
                  </span>
                </label>
              ))}
            </div>
            <PassoRodape
              processando={processando}
              onVoltar={() => setPasso(2)}
              onAvancar={avancarPecas}
              rotuloAvancar={pecas.some((p) => p.marcado) ? "Continuar" : "Pular peças"}
            />
          </div>
        )}

        {passo === 4 && (
          <div>
            <h1 className="text-2xl font-bold text-tinta">Tudo pronto! 🎉</h1>
            <p className="mt-2 text-sm text-tinta-suave">
              Sua loja já está configurada. Quer carregar um cliente e uma ordem
              de serviço de exemplo para explorar o Kanban e as telas sem
              precisar cadastrar nada? Dá para remover a qualquer momento.
            </p>
            <label className="mt-4 flex items-center gap-2 text-sm text-tinta">
              <input
                type="checkbox"
                checked={comExemplo}
                onChange={(e) => setComExemplo(e.target.checked)}
              />
              Carregar dados de exemplo (removíveis)
            </label>
            <div className="mt-6 flex gap-3">
              <Button
                onClick={finalizar}
                disabled={processando}
                className="h-11 rounded-full bg-tinta px-8 text-sobre-tinta hover:bg-tinta/90"
              >
                {processando ? "Finalizando..." : "Ir para o painel"}
              </Button>
              <Button type="button" variant="ghost" onClick={() => setPasso(3)}>
                Voltar
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function PassoRodape({
  processando,
  onVoltar,
  onAvancar,
  avancarDesabilitado = false,
  rotuloAvancar = "Continuar",
}: {
  processando: boolean;
  onVoltar: () => void;
  onAvancar: () => void;
  avancarDesabilitado?: boolean;
  rotuloAvancar?: string;
}) {
  return (
    <div className="mt-6 flex gap-3">
      <Button
        onClick={onAvancar}
        disabled={processando || avancarDesabilitado}
        className="h-11 rounded-full bg-tinta px-8 text-sobre-tinta hover:bg-tinta/90"
      >
        {processando ? "Salvando..." : rotuloAvancar}
      </Button>
      <Button type="button" variant="ghost" onClick={onVoltar} disabled={processando}>
        Voltar
      </Button>
    </div>
  );
}
