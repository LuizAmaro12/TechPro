"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useGetApiConfiguracoesTemplates,
  usePutApiConfiguracoesTemplates,
  type TemplateItem,
} from "@/lib/api-client/gerado";
import { ROTULOS_EVENTO_COMUNICACAO } from "@/lib/ordens-servico-etapas";

/**
 * Edição do texto de cada notificação. O que aparece aqui é literalmente o que
 * o cliente recebe: sem personalização, mostramos o texto padrão do sistema.
 */
export function EditorTemplates() {
  const { data: resposta, refetch } = useGetApiConfiguracoesTemplates();
  const templates = resposta?.status === 200 ? resposta.data.itens ?? [] : [];
  const salvar = usePutApiConfiguracoesTemplates();

  const [editando, setEditando] = useState<string | null>(null);
  const [assunto, setAssunto] = useState("");
  const [corpo, setCorpo] = useState("");

  function abrir(item: TemplateItem) {
    setEditando(item.tipoEvento ?? null);
    setAssunto(item.assunto ?? "");
    setCorpo(item.corpo ?? "");
  }

  async function enviar(tipoEvento: string, novoCorpo: string, novoAssunto: string | null) {
    try {
      await salvar.mutateAsync({
        data: { itens: [{ tipoEvento: tipoEvento as never, assunto: novoAssunto, corpo: novoCorpo }] },
      });
      setEditando(null);
      await refetch();
      toast.success("Mensagem atualizada.");
    } catch (erro) {
      // A API devolve exatamente qual variável não existe e quais valem.
      toast.error(
        erro instanceof ApiError ? erro.message : "Não foi possível salvar a mensagem.",
      );
    }
  }

  return (
    <section className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6">
      <h2 className="text-lg font-semibold text-[#14162B]">Textos das mensagens</h2>
      <p className="mt-1 text-sm text-[#6B7280]">
        Ajuste o texto que o cliente recebe em cada evento. As variáveis entre
        chaves são trocadas pelos dados reais no envio.
      </p>

      <div className="mt-4 space-y-3">
        {templates.map((item) => {
          const aberto = editando === item.tipoEvento;
          return (
            <article
              key={item.tipoEvento}
              className="rounded-xl border border-[#14162B]/8 p-4"
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <h3 className="text-sm font-semibold text-[#14162B]">
                  {ROTULOS_EVENTO_COMUNICACAO[item.tipoEvento ?? ""] ?? item.tipoEvento}
                  {item.personalizado && (
                    <span className="ml-2 rounded-full bg-[#E8536B]/10 px-2 py-0.5 text-[10px] font-semibold text-[#E8536B] uppercase">
                      personalizado
                    </span>
                  )}
                </h3>
                <div className="flex gap-1">
                  {item.personalizado && (
                    <Button
                      variant="ghost"
                      className="h-8 px-3 text-xs"
                      onClick={() => enviar(item.tipoEvento!, "", null)}
                    >
                      Voltar ao padrão
                    </Button>
                  )}
                  <Button
                    variant="outline"
                    className="h-8 px-3 text-xs"
                    onClick={() => (aberto ? setEditando(null) : abrir(item))}
                  >
                    {aberto ? "Fechar" : "Editar"}
                  </Button>
                </div>
              </div>

              {!aberto && (
                <p className="mt-2 text-sm whitespace-pre-wrap text-[#6B7280]">{item.corpo}</p>
              )}

              {aberto && (
                <div className="mt-3 space-y-3">
                  <p className="text-xs text-[#8B8D98]">
                    Disponíveis:{" "}
                    {(item.variaveisDisponiveis ?? []).map((v) => (
                      <button
                        key={v}
                        type="button"
                        onClick={() => setCorpo((c) => `${c}{${v}}`)}
                        className="mr-1 rounded bg-[#14162B]/5 px-1.5 py-0.5 font-mono text-[#14162B] hover:bg-[#14162B]/10"
                      >
                        {`{${v}}`}
                      </button>
                    ))}
                  </p>
                  <div>
                    <label
                      className="text-xs text-[#6B7280]"
                      htmlFor={`assunto-${item.tipoEvento}`}
                    >
                      Assunto (só no e-mail)
                    </label>
                    <Input
                      id={`assunto-${item.tipoEvento}`}
                      value={assunto}
                      maxLength={200}
                      onChange={(e) => setAssunto(e.target.value)}
                      className="mt-1 h-10"
                    />
                  </div>
                  <div>
                    <label
                      className="text-xs text-[#6B7280]"
                      htmlFor={`corpo-${item.tipoEvento}`}
                    >
                      Mensagem
                    </label>
                    <textarea
                      id={`corpo-${item.tipoEvento}`}
                      value={corpo}
                      rows={4}
                      maxLength={2000}
                      onChange={(e) => setCorpo(e.target.value)}
                      className="mt-1 w-full rounded-xl border border-[#14162B]/10 p-3 text-sm text-[#14162B]"
                    />
                  </div>
                  <Button
                    disabled={salvar.isPending || !corpo.trim()}
                    onClick={() => enviar(item.tipoEvento!, corpo, assunto || null)}
                    className="h-10 rounded-full bg-[#14162B] px-5 text-white hover:bg-[#14162B]/90"
                  >
                    Salvar mensagem
                  </Button>
                </div>
              )}
            </article>
          );
        })}
      </div>
    </section>
  );
}
