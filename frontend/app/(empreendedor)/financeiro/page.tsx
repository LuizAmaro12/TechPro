"use client";

import Link from "next/link";
import { useState } from "react";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useGetApiFinanceiro } from "@/lib/api-client/gerado";
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

  const presetAtivo = PRESETS.find((p) => {
    const periodo = p.periodo();
    return periodo.de === de && periodo.ate === ate;
  })?.rotulo;

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
        Financeiro
      </p>
      <h1 className="mt-2 text-3xl font-bold text-[#14162B]">Quanto entrou</h1>
      <p className="mt-1 text-sm text-[#6B7280]">
        O caixa da loja no período: o que foi recebido, o que está para receber e
        de onde o dinheiro veio.
      </p>

      {/* --- Período ------------------------------------------------------- */}
      <section className="mt-6 flex flex-wrap items-end gap-3">
        <div className="flex rounded-full border border-[#14162B]/10 p-0.5">
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
                  ? "bg-[#14162B] font-semibold text-white"
                  : "text-[#6B7280] hover:text-[#14162B]"
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
        <div className="rounded-2xl border border-[#14162B]/8 bg-white p-4">
          <p className="text-2xl font-bold text-[#14162B]">
            {isLoading ? "—" : formatarBRL(rel?.faturamento ?? 0)}
          </p>
          <p className="mt-1 text-xs text-[#6B7280]">Faturamento no período</p>
        </div>
        <div className="rounded-2xl border border-[#14162B]/8 bg-white p-4">
          <p className="text-2xl font-bold text-[#14162B]">
            {isLoading ? "—" : formatarBRL(rel?.ticketMedio ?? 0)}
          </p>
          <p className="mt-1 text-xs text-[#6B7280]">
            Ticket médio · {rel?.quantidadeOsPagas ?? 0} OS paga(s)
          </p>
        </div>
        <div className="rounded-2xl border border-[#14162B]/8 bg-white p-4">
          <p className="text-2xl font-bold text-[#14162B]">
            {isLoading ? "—" : (rel?.quantidadeTransacoes ?? 0)}
          </p>
          <p className="mt-1 text-xs text-[#6B7280]">Transações</p>
        </div>
        <div
          className={`rounded-2xl border p-4 ${
            (rel?.aReceber ?? 0) > 0
              ? "border-amber-200 bg-amber-50"
              : "border-[#14162B]/8 bg-white"
          }`}
        >
          <p
            className={`text-2xl font-bold ${
              (rel?.aReceber ?? 0) > 0 ? "text-amber-700" : "text-[#14162B]"
            }`}
          >
            {isLoading ? "—" : formatarBRL(rel?.aReceber ?? 0)}
          </p>
          <p className="mt-1 text-xs text-[#6B7280]">
            A receber · {rel?.quantidadePendentes ?? 0} OS
          </p>
        </div>
      </section>

      {/* --- Projeção de caixa --------------------------------------------- */}
      <section className="mt-6 rounded-2xl border border-[#14162B]/8 bg-white p-6">
        <h2 className="text-sm font-semibold text-[#14162B]">Quanto está para entrar</h2>
        <div className="mt-2 flex flex-wrap items-baseline gap-x-6 gap-y-1">
          <span className="text-3xl font-bold text-[#14162B]">
            {formatarBRL(rel?.projecao?.total ?? 0)}
          </span>
          <span className="text-sm text-[#6B7280]">
            {formatarBRL(rel?.projecao?.aprovadosAReceber ?? 0)} de orçamentos aprovados
            aguardando pagamento
          </span>
          <span className="text-sm text-[#6B7280]">
            + {formatarBRL(rel?.projecao?.agendamentosProximos7Dias ?? 0)} esperado dos
            agendamentos dos próximos 7 dias
          </span>
        </div>
        <p className="mt-2 text-xs text-[#8B8D98]">
          A parte dos agendamentos é estimativa pelo preço base do serviço — o
          orçamento final pode ser diferente.
        </p>
      </section>

      {/* --- Composição por forma ------------------------------------------ */}
      {(rel?.porForma?.length ?? 0) > 0 && (
        <section className="mt-6 rounded-2xl border border-[#14162B]/8 bg-white p-6">
          <h2 className="text-sm font-semibold text-[#14162B]">De onde veio o dinheiro</h2>
          <div className="mt-3 space-y-2">
            {rel?.porForma?.map((f) => {
              const pct = (rel?.faturamento ?? 0) > 0
                ? ((f.total ?? 0) / (rel?.faturamento ?? 1)) * 100
                : 0;
              return (
                <div key={f.forma} className="flex items-center gap-3 text-sm">
                  <span className="w-36 shrink-0 text-[#14162B]">
                    {ROTULOS_FORMA_PAGAMENTO[f.forma ?? "Outro"]}
                  </span>
                  <div className="h-2 flex-1 overflow-hidden rounded-full bg-[#F7F7F9]">
                    <div className="h-full rounded-full bg-[#14162B]" style={{ width: `${pct}%` }} />
                  </div>
                  <span className="w-28 shrink-0 text-right font-medium text-[#14162B]">
                    {formatarBRL(f.total ?? 0)}
                  </span>
                  <span className="w-12 shrink-0 text-right text-xs text-[#8B8D98]">
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
        <section className="mt-6 rounded-2xl border border-[#14162B]/8 bg-white p-6">
          <h2 className="text-sm font-semibold text-[#14162B]">
            A receber (orçamentos aprovados)
          </h2>
          <div className="mt-3 space-y-1">
            {rel?.pendentes?.map((p) => (
              <Link
                key={p.ordemServicoId}
                href="/ordens-servico"
                className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-[#14162B]/6 px-3 py-2 text-sm hover:border-[#14162B]/30"
              >
                <span className="text-[#14162B]">
                  <span className="font-medium">#{p.numero}</span> {p.clienteNome}
                  <span className="text-xs text-[#8B8D98]">
                    {" "}
                    · total {formatarBRL(p.total ?? 0)} · pago {formatarBRL(p.pago ?? 0)}
                  </span>
                </span>
                <span className="font-semibold text-amber-700">
                  {formatarBRL(p.saldo ?? 0)}
                </span>
              </Link>
            ))}
          </div>
        </section>
      )}

      {/* --- Transações ----------------------------------------------------- */}
      <section className="mt-6">
        <h2 className="text-sm font-semibold text-[#14162B]">Transações do período</h2>
        <div className="mt-3 overflow-x-auto rounded-2xl border border-[#14162B]/8">
          <table className="w-full text-left text-sm">
            <thead className="bg-[#F7F7F9] text-xs tracking-wide text-[#8B8D98] uppercase">
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
                <tr key={t.pagamentoId} className="border-t border-[#14162B]/6">
                  <td className="px-4 py-3 text-[#6B7280]">
                    {new Date(t.criadoEm ?? "").toLocaleString("pt-BR", {
                      day: "2-digit",
                      month: "2-digit",
                      hour: "2-digit",
                      minute: "2-digit",
                    })}
                  </td>
                  <td className="px-4 py-3 font-semibold text-[#14162B]">#{t.numero}</td>
                  <td className="px-4 py-3 text-[#14162B]">{t.clienteNome}</td>
                  <td className="px-4 py-3 text-[#6B7280]">
                    {ROTULOS_FORMA_PAGAMENTO[t.forma ?? "Outro"]}
                  </td>
                  <td className="px-4 py-3 text-right font-medium text-[#14162B]">
                    {formatarBRL(t.valor ?? 0)}
                  </td>
                </tr>
              ))}
              {rel && (rel.transacoes?.length ?? 0) === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-10 text-center text-[#6B7280]">
                    Nenhum pagamento entre {formatarDataCurta(de)} e {formatarDataCurta(ate)}.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
        {(rel?.quantidadeTransacoes ?? 0) > (rel?.transacoes?.length ?? 0) && (
          <p className="mt-2 text-xs text-[#8B8D98]">
            Mostrando as {rel?.transacoes?.length} mais recentes de{" "}
            {rel?.quantidadeTransacoes} no período.
          </p>
        )}
      </section>
    </div>
  );
}
