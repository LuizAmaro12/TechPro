"use client";

import { useRef, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  usePostApiClientesImportar,
  type ImportacaoClientesResponse,
} from "@/lib/api-client/gerado";

/**
 * Importação da carteira por CSV. Lê o arquivo no cliente e manda o texto; a
 * loja vê um relatório do que entrou, do que já existia e do que precisa
 * corrigir. Só adiciona — reimportar depois de arrumar é seguro.
 */
export function ImportarCsv({ aoImportar }: { aoImportar: () => void }) {
  const [aberto, setAberto] = useState(false);
  const [conteudo, setConteudo] = useState("");
  const [relatorio, setRelatorio] = useState<ImportacaoClientesResponse | null>(null);
  const inputArquivo = useRef<HTMLInputElement>(null);
  const importar = usePostApiClientesImportar();

  async function aoEscolherArquivo(evento: React.ChangeEvent<HTMLInputElement>) {
    const arquivo = evento.target.files?.[0];
    if (arquivo) {
      setConteudo(await arquivo.text());
    }
  }

  async function aoEnviar() {
    setRelatorio(null);
    try {
      const resposta = await importar.mutateAsync({ data: { conteudoCsv: conteudo } });
      if (resposta.status === 200) {
        setRelatorio(resposta.data);
        if ((resposta.data.importados ?? 0) > 0) {
          aoImportar();
        }
      }
    } catch (erro) {
      toast.error(
        erro instanceof ApiError ? erro.message : "Não foi possível importar o arquivo.",
      );
    }
  }

  function fechar() {
    setAberto(false);
    setConteudo("");
    setRelatorio(null);
    if (inputArquivo.current) inputArquivo.current.value = "";
  }

  if (!aberto) {
    return (
      <Button variant="outline" className="h-11" onClick={() => setAberto(true)}>
        Importar CSV
      </Button>
    );
  }

  return (
    <div className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold text-[#14162B]">Importar clientes por CSV</h2>
          <p className="mt-1 text-sm text-[#6B7280]">
            Traga a sua carteira de uma planilha. Precisa das colunas{" "}
            <strong>nome</strong> e <strong>telefone</strong> no cabeçalho; e-mail,
            CPF, endereço e observações são opcionais. Contatos com telefone repetido
            são ignorados.
          </p>
        </div>
        <Button variant="ghost" className="h-9 px-3" onClick={fechar}>
          Fechar
        </Button>
      </div>

      <div className="mt-4">
        <input
          ref={inputArquivo}
          type="file"
          accept=".csv,text/csv,text/plain"
          onChange={aoEscolherArquivo}
          className="block text-sm text-[#6B7280] file:mr-3 file:rounded-full file:border-0 file:bg-[#14162B]/5 file:px-4 file:py-2 file:text-sm file:text-[#14162B]"
        />
      </div>

      <textarea
        value={conteudo}
        onChange={(e) => setConteudo(e.target.value)}
        placeholder={"nome,telefone,email\nMaria Souza,(11) 99999-0001,maria@ex.com"}
        rows={6}
        className="mt-3 w-full rounded-xl border border-[#14162B]/10 p-3 font-mono text-xs text-[#14162B]"
      />

      <div className="mt-3 flex gap-2">
        <Button
          disabled={importar.isPending || conteudo.trim() === ""}
          onClick={aoEnviar}
          className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
        >
          Importar
        </Button>
      </div>

      {relatorio && (
        <div className="mt-4 rounded-xl border border-[#14162B]/8 bg-[#F7F7F9] p-4 text-sm">
          <div className="flex flex-wrap gap-2">
            <span className="rounded-lg bg-emerald-50 px-3 py-1.5 text-emerald-700">
              Importados <strong>{relatorio.importados}</strong>
            </span>
            <span className="rounded-lg bg-amber-50 px-3 py-1.5 text-amber-700">
              Já existiam <strong>{relatorio.duplicados}</strong>
            </span>
            <span className="rounded-lg bg-[#E8536B]/10 px-3 py-1.5 text-[#E8536B]">
              Com erro <strong>{relatorio.erros?.length ?? 0}</strong>
            </span>
          </div>
          {(relatorio.erros?.length ?? 0) > 0 && (
            <ul className="mt-3 space-y-1 text-[#6B7280]">
              {relatorio.erros!.map((e) => (
                <li key={e.linha}>
                  Linha {e.linha}: {e.motivo}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
