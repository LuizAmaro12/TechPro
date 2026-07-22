"use client";

import Link from "next/link";
import { useState } from "react";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  useGetApiFinanceiro,
  useGetApiFinanceiroRentabilidade,
} from "@/lib/api-client/gerado";
import { formatarDataCurta, hojeIso, somarDias } from "@/lib/agenda-datas";
import { formatarBRL } from "@/lib/formatadores";
import { ROTULOS_FORMA_PAGAMENTO } from "@/lib/ordens-servico-etapas";

function inicioDoMesIso() {
  return `${hojeIso().slice(0, 7)}-01`;
}

function mesPassado(): { de: string; ate: string } {
  const primeiroDesteMes = new Date(`${inicioDoMesIso()}T00:00:00`);
  const ultimoDoPassado = new Date(primeiroDesteMes);
  ultimoDoPassado.setDate(0);
  const primeiroDoPassado = new Date(ultimoDoPassado);
  primeiroDoPassado.setDate(1);
  const iso = (d: Date) =>
    `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
  return { de: iso(primeiroDoPassado), ate: iso(ultimoDoPassado) };
}

const PRESETS: { rotulo: string; periodo: () => { de: string; ate: string } }[] = [
  { rotulo: "Hoje", periodo: () => ({ de: hojeIso(), ate: hojeIso() }) },
  { rotulo: "7 dias", periodo: () => ({ de: somarDias(hojeIso(), -6), ate: hojeIso() }) },
  { rotulo: "Este mês", periodo: () => ({ de: inicioDoMesIso(), ate: hojeIso() }) },
  { rotulo: "Mês passado", periodo: mesPassado },
];

export default function PaginaFinanceiro() {
  const [de, setDe] = useState(inicioDoMesIso());
  const [ate, setAte] = useState(hojeIso());

  const { data: resposta, isLoading } = useGetApiFinanceiro({ de, ate });
  const rel = resposta?.status === 200 ? resposta.data : undefined;

  const { data: respostaRent } = useGetApiFinanceiroRentabilidade({ de, ate });
  const rent = respostaRent?.status === 200 ? respostaRent.data : undefined;

  const presetAtivo = PRESETS.find((p) => {
    const periodo = p.periodo();
    return periodo.de === de && periodo.ate === ate;
  })?.rotulo;

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-marca uppercase">
        Financeiro
      </p>
      <h1 className="mt-2 text-3xl font-bold text-tinta">Quanto entrou</h1>
      <p className="mt-1 text-sm text-tinta-suave">
        O caixa da loja no período: o que foi recebido, o que está para receber e
        de onde o dinheiro veio.
      </p>

      {/* --- Período ------------------------------------------------------- */}
      <section className="mt-6 flex flex-wrap items-end gap-3">
        <div className="flex rounded-full border border-borda p-0.5">
          {PRESETS.map((preset) => (
            <button
              key={preset.rotulo}
              onClick={() => {
                const periodo = preset.periodo();
                setDe(periodo.de);
                setAte(periodo.ate);
              }}
              className={`rounded-full px-4 py-1.5 text-sm transition-colors ${
                presetAtivo === preset.rotulo
                  ? "bg-tinta font-semibold text-sobre-tinta"
                  : "text-tinta-suave hover:text-tinta"
              }`}
            >
              {preset.rotulo}
            </button>
          ))}
        </div>
        <div>
          <Label htmlFor="de">De</Label>
          <Input
            id="de"
            type="date"
            value={de}
            onChange={(e) => setDe(e.target.value)}
            className="mt-1 h-10"
          />
        </div>
        <div>
          <Label htmlFor="ate">Até</Label>
          <Input
            id="ate"
            type="date"
            value={ate}
            min={de}
            onChange={(e) => setAte(e.target.value)}
            className="mt-1 h-10"
          />
        </div>
      </section>

      {/* --- KPIs ---------------------------------------------------------- */}
      <section className="mt-6 grid grid-cols-2 gap-3 lg:grid-cols-4">
        <div className="rounded-2xl border border-borda bg-superficie p-4">
          <p className="text-2xl font-bold text-tinta">
            {isLoading ? "—" : formatarBRL(rel?.faturamento ?? 0)}
          </p>
          <p className="mt-1 text-xs text-tinta-suave">Faturamento no período</p>
        </div>
        <div className="rounded-2xl border border-borda bg-superficie p-4">
          <p className="text-2xl font-bold text-tinta">
            {isLoading ? "—" : formatarBRL(rel?.ticketMedio ?? 0)}
          </p>
          <p className="mt-1 text-xs text-tinta-suave">
            Ticket médio · {rel?.quantidadeOsPagas ?? 0} OS paga(s)
          </p>
        </div>
        <div className="rounded-2xl border border-borda bg-superficie p-4">
          <p className="text-2xl font-bold text-tinta">
            {isLoading ? "—" : (rel?.quantidadeTransacoes ?? 0)}
          </p>
          <p className="mt-1 text-xs text-tinta-suave">Transações</p>
        </div>
        <div
          className={`rounded-2xl border p-4 ${
            (rel?.aReceber ?? 0) > 0
              ? "border-amber-200 bg-alerta-fundo"
              : "border-borda bg-superficie"
          }`}
        >
          <p
            className={`text-2xl font-bold ${
              (rel?.aReceber ?? 0) > 0 ? "text-alerta" : "text-tinta"
            }`}
          >
            {isLoading ? "—" : formatarBRL(rel?.aReceber ?? 0)}
          </p>
          <p className="mt-1 text-xs text-tinta-suave">
            A receber · {rel?.quantidadePendentes ?? 0} OS
          </p>
        </div>
      </section>

      {/* --- Projeção de caixa --------------------------------------------- */}
      <section className="mt-6 rounded-2xl border border-borda bg-superficie p-6">
        <h2 className="text-sm font-semibold text-tinta">Quanto está para entrar</h2>
        <div className="mt-2 flex flex-wrap items-baseline gap-x-6 gap-y-1">
          <span className="text-3xl font-bold text-tinta">
            {formatarBRL(rel?.projecao?.total ?? 0)}
          </span>
          <span className="text-sm text-tinta-suave">
            {formatarBRL(rel?.projecao?.aprovadosAReceber ?? 0)} de orçamentos aprovados
            aguardando pagamento
          </span>
          <span className="text-sm text-tinta-suave">
            + {formatarBRL(rel?.projecao?.agendamentosProximos7Dias ?? 0)} esperado dos
            agendamentos dos próximos 7 dias
          </span>
        </div>
        <p className="mt-2 text-xs text-tinta-fraca">
          A parte dos agendamentos é estimativa pelo preço base do serviço — o
          orçamento final pode ser diferente.
        </p>
      </section>

      {/* --- Composição por forma ------------------------------------------ */}
      {(rel?.porForma?.length ?? 0) > 0 && (
        <section className="mt-6 rounded-2xl border border-borda bg-superficie p-6">
          <h2 className="text-sm font-semibold text-tinta">De onde veio o dinheiro</h2>
          <div className="mt-3 space-y-2">
            {rel?.porForma?.map((f) => {
              const pct = (rel?.faturamento ?? 0) > 0
                ? ((f.total ?? 0) / (rel?.faturamento ?? 1)) * 100
                : 0;
              return (
                <div key={f.forma} className="flex items-center gap-3 text-sm">
                  <span className="w-36 shrink-0 text-tinta">
                    {ROTULOS_FORMA_PAGAMENTO[f.forma ?? "Outro"]}
                  </span>
                  <div className="h-2 flex-1 overflow-hidden rounded-full bg-sutil">
                    <div className="h-full rounded-full bg-tinta" style={{ width: `${pct}%` }} />
                  </div>
                  <span className="w-28 shrink-0 text-right font-medium text-tinta">
                    {formatarBRL(f.total ?? 0)}
                  </span>
                  <span className="w-12 shrink-0 text-right text-xs text-tinta-fraca">
                    {Math.round(pct)}%
                  </span>
                </div>
              );
            })}
          </div>
        </section>
      )}

      {/* --- A receber ------------------------------------------------------ */}
      {(rel?.pendentes?.length ?? 0) > 0 && (
        <section className="mt-6 rounded-2xl border border-borda bg-superficie p-6">
          <h2 className="text-sm font-semibold text-tinta">
            A receber (orçamentos aprovados)
          </h2>
          <div className="mt-3 space-y-1">
            {rel?.pendentes?.map((p) => (
              <Link
                key={p.ordemServicoId}
                href="/ordens-servico"
                className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-borda px-3 py-2 text-sm hover:border-borda-forte"
              >
                <span className="text-tinta">
                  <span className="font-medium">#{p.numero}</span> {p.clienteNome}
                  <span className="text-xs text-tinta-fraca">
                    {" "}
                    · total {formatarBRL(p.total ?? 0)} · pago {formatarBRL(p.pago ?? 0)}
                  </span>
                </span>
                <span className="font-semibold text-alerta">
                  {formatarBRL(p.saldo ?? 0)}
                </span>
              </Link>
            ))}
          </div>
        </section>
      )}

      {/* --- Rentabilidade (margem realizada) ------------------------------- */}
      <section className="mt-6 rounded-2xl border border-borda bg-superficie p-6">
        <div className="flex flex-wrap items-baseline justify-between gap-2">
          <h2 className="text-sm font-semibold text-tinta">Quanto sobrou (margem)</h2>
          <span className="text-xs text-tinta-fraca">
            {rent?.quantidadeOs ?? 0} OS entregue(s) no período
          </span>
        </div>
        <p className="mt-1 text-xs text-tinta-fraca">
          Base: OS <strong>entregues</strong> no período, com o custo da peça
          congelado no momento do uso — diferente do faturamento acima, que é o
          caixa recebido.
        </p>

        <div className="mt-4 grid grid-cols-2 gap-3 lg:grid-cols-4">
          <div>
            <p className="text-2xl font-bold text-tinta">
              {formatarBRL(rent?.lucroBruto ?? 0)}
            </p>
            <p className="mt-1 text-xs text-tinta-suave">Lucro bruto</p>
          </div>
          <div>
            <p className="text-2xl font-bold text-tinta">
              {rent?.margemPercentual ?? 0}%
            </p>
            <p className="mt-1 text-xs text-tinta-suave">Margem média</p>
          </div>
          <div>
            <p className="text-2xl font-bold text-tinta">
              {formatarBRL(rent?.receitaTotal ?? 0)}
            </p>
            <p className="mt-1 text-xs text-tinta-suave">Receita das entregas</p>
          </div>
          <div>
            <p className="text-2xl font-bold text-tinta">
              {formatarBRL(rent?.custoPecas ?? 0)}
            </p>
            <p className="mt-1 text-xs text-tinta-suave">Custo de peças</p>
          </div>
        </div>

        {(rent?.osSemOrcamento ?? 0) > 0 && (
          <p className="mt-3 rounded-xl bg-alerta-fundo px-3 py-2 text-xs text-alerta">
            {rent?.osSemOrcamento} OS entregue(s) sem orçamento registrado — o
            custo da peça entra, mas a receita não, o que puxa a margem para
            baixo.
          </p>
        )}

        {(rent?.porServico?.length ?? 0) > 0 && (
          <div className="mt-4 overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="text-xs tracking-wide text-tinta-fraca uppercase">
                <tr>
                  <th className="py-2">Serviço</th>
                  <th className="py-2 text-right">OS</th>
                  <th className="py-2 text-right">Receita</th>
                  <th className="py-2 text-right">Custo peças</th>
                  <th className="py-2 text-right">Lucro</th>
                  <th className="py-2 text-right">Margem</th>
                </tr>
              </thead>
              <tbody>
                {rent?.porServico?.map((s) => (
                  <tr key={s.servicoId} className="border-t border-borda">
                    <td className="py-2.5 text-tinta">{s.servicoNome}</td>
                    <td className="py-2.5 text-right text-tinta-suave">{s.quantidadeOs}</td>
                    <td className="py-2.5 text-right text-tinta-suave">
                      {formatarBRL(s.receita ?? 0)}
                    </td>
                    <td className="py-2.5 text-right text-tinta-suave">
                      {formatarBRL(s.custoPecas ?? 0)}
                    </td>
                    <td className="py-2.5 text-right font-medium text-tinta">
                      {formatarBRL(s.lucroBruto ?? 0)}
                    </td>
                    <td className="py-2.5 text-right text-tinta-suave">
                      {s.margemPercentual}%
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {(rent?.quantidadeOs ?? 0) === 0 && (
          <p className="mt-3 text-sm text-tinta-fraca">
            Nenhuma OS entregue no período — a margem aparece aqui quando os
            reparos forem concluídos.
          </p>
        )}
      </section>

      {/* --- Transações ----------------------------------------------------- */}
      <section className="mt-6">
        <h2 className="text-sm font-semibold text-tinta">Transações do período</h2>
        <span className="sr-only">Lista dos pagamentos recebidos no período.</span>
        <div className="mt-3 overflow-x-auto rounded-2xl border border-borda">
          <table className="w-full text-left text-sm">
            <thead className="bg-sutil text-xs tracking-wide text-tinta-fraca uppercase">
              <tr>
                <th className="px-4 py-3">Data</th>
                <th className="px-4 py-3">OS</th>
                <th className="px-4 py-3">Cliente</th>
                <th className="px-4 py-3">Forma</th>
                <th className="px-4 py-3 text-right">Valor</th>
              </tr>
            </thead>
            <tbody>
              {rel?.transacoes?.map((t) => (
                <tr key={t.pagamentoId} className="border-t border-borda">
                  <td className="px-4 py-3 text-tinta-suave">
                    {new Date(t.criadoEm ?? "").toLocaleString("pt-BR", {
                      day: "2-digit",
                      month: "2-digit",
                      hour: "2-digit",
                      minute: "2-digit",
                    })}
                  </td>
                  <td className="px-4 py-3 font-semibold text-tinta">#{t.numero}</td>
                  <td className="px-4 py-3 text-tinta">{t.clienteNome}</td>
                  <td className="px-4 py-3 text-tinta-suave">
                    {ROTULOS_FORMA_PAGAMENTO[t.forma ?? "Outro"]}
                  </td>
                  <td className="px-4 py-3 text-right font-medium text-tinta">
                    {formatarBRL(t.valor ?? 0)}
                  </td>
                </tr>
              ))}
              {rel && (rel.transacoes?.length ?? 0) === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-10 text-center text-tinta-suave">
                    Nenhum pagamento entre {formatarDataCurta(de)} e {formatarDataCurta(ate)}.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
        {(rel?.quantidadeTransacoes ?? 0) > (rel?.transacoes?.length ?? 0) && (
          <p className="mt-2 text-xs text-tinta-fraca">
            Mostrando as {rel?.transacoes?.length} mais recentes de{" "}
            {rel?.quantidadeTransacoes} no período.
          </p>
        )}
      </section>
    </div>
  );
}
