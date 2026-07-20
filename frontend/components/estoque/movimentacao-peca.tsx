"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiPecasIdMovimentacoes,
  usePostApiPecasIdMovimentacoes,
  type TipoMovimentacaoEstoque,
} from "@/lib/api-client/gerado";
import { formatarBRL } from "@/lib/formatadores";

const ROTULOS: Record<string, string> = {
  Entrada: "Entrada",
  Saida: "Saída",
  Ajuste: "Ajuste",
  ConsumoOs: "Usada na OS",
  EstornoOs: "Devolvida da OS",
};

/** Só estes três podem ser lançados à mão — os de OS vêm do fluxo da ordem. */
const TIPOS_MANUAIS: TipoMovimentacaoEstoque[] = ["Entrada", "Saida", "Ajuste"];

/**
 * Extrato da peça e lançamento manual. Existe porque, antes desta etapa, o
 * saldo mudava sem deixar rastro e não havia como registrar uma compra
 * recebida — a loja editava o número na mão.
 */
export function MovimentacaoPeca({
  pecaId,
  pecaNome,
  saldoAtual,
  aoMudar,
  aoFechar,
}: {
  pecaId: number;
  pecaNome: string;
  saldoAtual: number;
  aoMudar: () => void;
  aoFechar: () => void;
}) {
  const [tipo, setTipo] = useState<TipoMovimentacaoEstoque>("Entrada");
  const [quantidade, setQuantidade] = useState("1");
  const [custo, setCusto] = useState("");
  const [motivo, setMotivo] = useState("");

  const { data: resposta } = useGetApiPecasIdMovimentacoes(pecaId);
  const extrato = resposta?.status === 200 ? resposta.data : [];
  const movimentar = usePostApiPecasIdMovimentacoes();

  const ajuste = tipo === "Ajuste";

  async function aoLancar(evento: React.FormEvent) {
    evento.preventDefault();
    try {
      await movimentar.mutateAsync({
        id: pecaId,
        data: {
          tipo,
          quantidade: Number(quantidade),
          custoUnitario: custo === "" ? null : Number(custo),
          motivo: motivo.trim() || null,
        },
      });
      setQuantidade("1");
      setCusto("");
      setMotivo("");
      toast.success("Movimentação registrada.");
      aoMudar();
    } catch (erro) {
      toast.error(
        erro instanceof ApiError ? erro.message : "Não foi possível registrar.",
      );
    }
  }

  return (
    <section className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold text-[#14162B]">
            Estoque de {pecaNome}
          </h2>
          <p className="mt-1 text-sm text-[#6B7280]">
            Saldo atual: <strong className="text-[#14162B]">{saldoAtual}</strong>{" "}
            — todo movimento fica registrado, inclusive os gerados pelas OS.
          </p>
        </div>
        <Button variant="ghost" className="h-9 px-3" onClick={aoFechar}>
          Fechar
        </Button>
      </div>

      <form onSubmit={aoLancar} className="mt-4 grid gap-3 sm:grid-cols-4">
        <div>
          <Label htmlFor="tipoMovimento">Tipo</Label>
          <select
            id="tipoMovimento"
            value={tipo}
            onChange={(e) => setTipo(e.target.value as TipoMovimentacaoEstoque)}
            className="mt-1 h-11 w-full rounded-md border border-[#14162B]/10 bg-white px-2 text-sm"
          >
            {TIPOS_MANUAIS.map((t) => (
              <option key={t} value={t}>
                {ROTULOS[t]}
              </option>
            ))}
          </select>
        </div>
        <div>
          <Label htmlFor="quantidadeMovimento">
            {ajuste ? "Saldo contado" : "Quantidade"}
          </Label>
          <Input
            id="quantidadeMovimento"
            type="number"
            min="0"
            className="mt-1 h-11"
            value={quantidade}
            onChange={(e) => setQuantidade(e.target.value)}
          />
          {ajuste && (
            <p className="mt-1 text-xs text-[#8B8D98]">
              Informe quanto existe de verdade na prateleira.
            </p>
          )}
        </div>
        <div>
          <Label htmlFor="custoMovimento">Custo unitário</Label>
          <Input
            id="custoMovimento"
            type="number"
            step="0.01"
            min="0"
            placeholder="opcional"
            className="mt-1 h-11"
            value={custo}
            onChange={(e) => setCusto(e.target.value)}
            disabled={tipo !== "Entrada"}
          />
          {tipo === "Entrada" && (
            <p className="mt-1 text-xs text-[#8B8D98]">
              Se informado, passa a ser o custo da peça.
            </p>
          )}
        </div>
        <div>
          <Label htmlFor="motivoMovimento">
            Motivo {ajuste && <span className="text-[#E8536B]">*</span>}
          </Label>
          <Input
            id="motivoMovimento"
            className="mt-1 h-11"
            placeholder={ajuste ? "Ex.: contagem" : "opcional"}
            maxLength={300}
            value={motivo}
            onChange={(e) => setMotivo(e.target.value)}
          />
        </div>
        <div className="sm:col-span-4">
          <Button
            type="submit"
            disabled={movimentar.isPending || (ajuste && !motivo.trim())}
            className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
          >
            Registrar movimentação
          </Button>
        </div>
      </form>

      <h3 className="mt-6 text-sm font-semibold text-[#14162B]">
        Histórico de movimentações
      </h3>
      {extrato.length === 0 ? (
        <p className="mt-2 text-sm text-[#8B8D98]">
          Nenhuma movimentação registrada para esta peça.
        </p>
      ) : (
        <div className="mt-2 overflow-x-auto rounded-xl border border-[#14162B]/8">
          <table className="w-full text-left text-sm">
            <thead className="bg-[#F7F7F9] text-xs text-[#6B7280] uppercase">
              <tr>
                <th className="px-4 py-2">Quando</th>
                <th className="px-4 py-2">Tipo</th>
                <th className="px-4 py-2 text-right">Qtd.</th>
                <th className="px-4 py-2 text-right">Saldo após</th>
                <th className="px-4 py-2">Detalhe</th>
              </tr>
            </thead>
            <tbody>
              {extrato.map((m) => (
                <tr key={m.id} className="border-t border-[#14162B]/6">
                  <td className="px-4 py-2 text-[#6B7280] whitespace-nowrap">
                    {new Date(m.criadoEm ?? "").toLocaleString("pt-BR", {
                      day: "2-digit",
                      month: "2-digit",
                      hour: "2-digit",
                      minute: "2-digit",
                    })}
                  </td>
                  <td className="px-4 py-2 text-[#14162B]">
                    {ROTULOS[m.tipo ?? ""] ?? m.tipo}
                  </td>
                  <td
                    className={`px-4 py-2 text-right font-semibold ${
                      (m.quantidade ?? 0) < 0 ? "text-[#E8536B]" : "text-emerald-700"
                    }`}
                  >
                    {(m.quantidade ?? 0) > 0 ? `+${m.quantidade}` : m.quantidade}
                  </td>
                  <td className="px-4 py-2 text-right text-[#14162B]">{m.saldoApos}</td>
                  <td className="px-4 py-2 text-[#6B7280]">
                    {m.ordemServicoNumero ? `OS #${m.ordemServicoNumero}` : null}
                    {m.motivo}
                    {m.custoUnitario != null && ` · ${formatarBRL(m.custoUnitario)}`}
                    {m.usuarioNome && ` · ${m.usuarioNome}`}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
