"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiFilaEspera,
  usePostApiFilaEsperaIdConverter,
  usePostApiFilaEsperaIdDescartar,
} from "@/lib/api-client/gerado";
import { formatarDataCurta, hojeIso } from "@/lib/agenda-datas";

/**
 * Fila de espera: a demanda que se perdia quando não havia vaga. Fica recolhida
 * por padrão para não competir com a agenda, mas anuncia a contagem. Converter
 * abre a escolha de data/hora (cria um agendamento normal); descartar tira da
 * lista sem apagar o registro.
 */
export function FilaDeEspera({ aoConverter }: { aoConverter: () => void }) {
  const [aberta, setAberta] = useState(false);
  const [convertendoId, setConvertendoId] = useState<number | null>(null);
  const [data, setData] = useState(hojeIso());
  const [hora, setHora] = useState("09:00");

  const { data: resposta, refetch } = useGetApiFilaEspera();
  const fila = resposta?.status === 200 ? resposta.data : [];
  const converter = usePostApiFilaEsperaIdConverter();
  const descartar = usePostApiFilaEsperaIdDescartar();

  if (fila.length === 0) return null;

  function reportar(erro: unknown, padrao: string) {
    toast.error(erro instanceof ApiError ? erro.message : padrao);
  }

  async function aoConfirmarConversao(id: number) {
    try {
      await converter.mutateAsync({
        id,
        data: { data, horaInicio: `${hora}:00` },
      });
      setConvertendoId(null);
      toast.success("Agendamento criado a partir da fila.");
      await refetch();
      aoConverter();
    } catch (erro) {
      reportar(erro, "Não foi possível converter. Verifique o horário.");
    }
  }

  async function aoDescartar(id: number) {
    try {
      await descartar.mutateAsync({ id, data: { motivo: null } });
      await refetch();
    } catch (erro) {
      reportar(erro, "Não foi possível descartar.");
    }
  }

  return (
    <section className="mt-6 rounded-2xl border border-marca/40 bg-marca-fundo p-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-tinta">
            Fila de espera · {fila.length}{" "}
            {fila.length === 1 ? "pessoa aguardando" : "pessoas aguardando"}
          </h2>
          <p className="mt-1 text-sm text-tinta-suave">
            Quem pediu horário e não tinha vaga. Converta quando abrir um horário.
          </p>
        </div>
        <Button variant="ghost" className="h-9 px-3" onClick={() => setAberta((v) => !v)}>
          {aberta ? "Recolher" : "Ver fila"}
        </Button>
      </div>

      {aberta && (
        <ul className="mt-4 space-y-2">
          {fila.map((item) => (
            <li key={item.id} className="rounded-xl border border-borda bg-superficie p-3 text-sm">
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div>
                  <p className="font-medium text-tinta">
                    {item.nomeContato} · {item.telefoneContato}
                  </p>
                  <p className="text-tinta-suave">
                    {item.servicoNome}
                    {item.origem === "Portal" && " · veio pelo portal"}
                    {item.dataPreferida &&
                      ` · queria ${formatarDataCurta(item.dataPreferida)}`}
                  </p>
                  {(item.aparelhoMarca || item.aparelhoModelo) && (
                    <p className="text-xs text-tinta-fraca">
                      {[item.aparelhoMarca, item.aparelhoModelo].filter(Boolean).join(" ")}
                    </p>
                  )}
                </div>
                <div className="flex gap-1">
                  <Button
                    variant="outline"
                    className="h-8 rounded-full px-3 text-xs"
                    onClick={() =>
                      setConvertendoId((atual) => (atual === item.id ? null : item.id!))
                    }
                  >
                    Converter
                  </Button>
                  <Button
                    variant="ghost"
                    className="h-8 px-3 text-xs text-tinta-fraca"
                    onClick={() => aoDescartar(item.id!)}
                  >
                    Descartar
                  </Button>
                </div>
              </div>

              {convertendoId === item.id && (
                <div className="mt-3 flex flex-wrap items-end gap-2 border-t border-borda pt-3">
                  <div>
                    <label className="text-xs text-tinta-suave">Data</label>
                    <Input
                      type="date"
                      min={hojeIso()}
                      value={data}
                      onChange={(e) => setData(e.target.value)}
                      className="mt-1 h-9 max-w-44 text-sm"
                    />
                  </div>
                  <div>
                    <label className="text-xs text-tinta-suave">Hora</label>
                    <Input
                      type="time"
                      value={hora}
                      onChange={(e) => setHora(e.target.value)}
                      className="mt-1 h-9 max-w-32 text-sm"
                    />
                  </div>
                  <Button
                    className="h-9 rounded-full bg-tinta px-5 text-xs text-sobre-tinta hover:bg-tinta/90"
                    disabled={converter.isPending}
                    onClick={() => aoConfirmarConversao(item.id!)}
                  >
                    Criar agendamento
                  </Button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
