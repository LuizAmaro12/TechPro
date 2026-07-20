"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { useGetApiEstoqueListaCompra } from "@/lib/api-client/gerado";
import { formatarBRL } from "@/lib/formatadores";

/**
 * O que precisa ser comprado, agrupado por fornecedor — a loja compra por
 * fornecedor, não peça a peça. Fica recolhida por padrão para não competir com
 * o catálogo, mas anuncia a contagem no cabeçalho.
 */
export function ListaDeCompra() {
  const [aberta, setAberta] = useState(false);
  const { data: resposta } = useGetApiEstoqueListaCompra();
  const lista = resposta?.status === 200 ? resposta.data : null;
  const total = lista?.totalDeItens ?? 0;

  if (!lista || total === 0) {
    return null;
  }

  return (
    <section className="mt-8 rounded-2xl border border-amber-300/60 bg-amber-50/60 p-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-[#14162B]">
            Lista de compra · {total} {total === 1 ? "peça" : "peças"} para repor
          </h2>
          <p className="mt-1 text-sm text-[#6B7280]">
            Peças no ou abaixo do estoque mínimo. Custo estimado:{" "}
            <strong className="text-[#14162B]">
              {formatarBRL(lista.custoEstimado ?? 0)}
            </strong>
          </p>
        </div>
        <Button
          variant="ghost"
          className="h-9 px-3"
          onClick={() => setAberta((v) => !v)}
        >
          {aberta ? "Recolher" : "Ver lista"}
        </Button>
      </div>

      {aberta && (
        <div className="mt-4 space-y-4">
          {lista.grupos?.map((grupo) => (
            <div
              key={grupo.fornecedorId ?? "sem-fornecedor"}
              className="rounded-xl border border-[#14162B]/8 bg-white p-4"
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <h3 className="text-sm font-semibold text-[#14162B]">
                  {grupo.fornecedorNome}
                </h3>
                <span className="text-sm text-[#6B7280]">
                  {formatarBRL(grupo.custoEstimado ?? 0)}
                </span>
              </div>
              <table className="mt-2 w-full text-left text-sm">
                <thead className="text-xs text-[#6B7280] uppercase">
                  <tr>
                    <th className="py-1">Peça</th>
                    <th className="py-1 text-right">Em estoque</th>
                    <th className="py-1 text-right">Mínimo</th>
                    <th className="py-1 text-right">Comprar</th>
                    <th className="py-1 text-right">Estimado</th>
                  </tr>
                </thead>
                <tbody>
                  {grupo.itens?.map((item) => (
                    <tr key={item.pecaId} className="border-t border-[#14162B]/6">
                      <td className="py-1.5 text-[#14162B]">{item.pecaNome}</td>
                      <td className="py-1.5 text-right text-[#E8536B]">
                        {item.quantidadeEmEstoque}
                      </td>
                      <td className="py-1.5 text-right text-[#6B7280]">
                        {item.estoqueMinimo}
                      </td>
                      <td className="py-1.5 text-right font-semibold text-[#14162B]">
                        {item.sugestaoCompra}
                      </td>
                      <td className="py-1.5 text-right text-[#6B7280]">
                        {formatarBRL(item.custoEstimado ?? 0)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
