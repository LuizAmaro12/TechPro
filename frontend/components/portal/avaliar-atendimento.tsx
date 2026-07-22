"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { ApiError } from "@/lib/api-client/fetcher";
import { usePostApiPublicoSlugAcompanharCodigoAvaliacao } from "@/lib/api-client/gerado";

/**
 * Avaliação do cliente no acompanhamento público — só aparece depois da
 * entrega (o servidor decide via `podeAvaliar`). Duas escalas: estrelas para a
 * experiência do reparo e 0–10 de recomendação para o NPS.
 */
export function AvaliarAtendimento({
  slug,
  codigo,
  aoAvaliar,
}: {
  slug: string;
  codigo: string;
  aoAvaliar: () => void;
}) {
  const [nota, setNota] = useState(0);
  const [recomendacao, setRecomendacao] = useState<number | null>(null);
  const [comentario, setComentario] = useState("");
  const avaliar = usePostApiPublicoSlugAcompanharCodigoAvaliacao();

  async function aoEnviar(evento: React.FormEvent) {
    evento.preventDefault();
    if (nota === 0 || recomendacao === null) return;
    try {
      await avaliar.mutateAsync({
        slug,
        codigo,
        data: { nota, recomendacao, comentario: comentario.trim() || null },
      });
      toast.success("Obrigado pela sua avaliação!");
      aoAvaliar();
    } catch (erro) {
      toast.error(
        erro instanceof ApiError ? erro.message : "Não foi possível enviar a avaliação.",
      );
    }
  }

  return (
    <section className="mt-6 rounded-2xl border border-borda bg-superficie p-6">
      <h2 className="text-lg font-semibold text-tinta">Como foi seu atendimento?</h2>
      <p className="mt-1 text-sm text-tinta-suave">
        Leva menos de um minuto e ajuda muito a loja a melhorar.
      </p>

      <form onSubmit={aoEnviar}>
        <fieldset className="mt-5">
          <legend className="text-sm font-medium text-tinta">
            Sua nota para o reparo
          </legend>
          <div className="mt-2 flex gap-1">
            {[1, 2, 3, 4, 5].map((estrela) => (
              <button
                key={estrela}
                type="button"
                aria-label={`${estrela} ${estrela === 1 ? "estrela" : "estrelas"}`}
                aria-pressed={nota === estrela}
                onClick={() => setNota(estrela)}
                className={`h-11 w-11 rounded-full text-2xl leading-none transition-colors ${
                  estrela <= nota ? "text-marca" : "text-tinta-fraca hover:text-tinta-suave"
                }`}
              >
                {estrela <= nota ? "★" : "☆"}
              </button>
            ))}
          </div>
        </fieldset>

        <fieldset className="mt-5">
          <legend className="text-sm font-medium text-tinta">
            De 0 a 10, quanto recomendaria esta assistência?
          </legend>
          <div className="mt-2 flex flex-wrap gap-1">
            {Array.from({ length: 11 }, (_, n) => (
              <button
                key={n}
                type="button"
                aria-label={`Nota ${n} de 10`}
                aria-pressed={recomendacao === n}
                onClick={() => setRecomendacao(n)}
                className={`h-10 w-10 rounded-lg border text-sm font-semibold transition-colors ${
                  recomendacao === n
                    ? "border-tinta bg-tinta text-sobre-tinta"
                    : "border-borda text-tinta hover:border-borda-forte"
                }`}
              >
                {n}
              </button>
            ))}
          </div>
        </fieldset>

        <label className="mt-5 block">
          <span className="text-sm font-medium text-tinta">
            Quer contar algo? (opcional)
          </span>
          <textarea
            value={comentario}
            onChange={(e) => setComentario(e.target.value)}
            rows={3}
            maxLength={1000}
            className="mt-2 w-full rounded-xl border border-borda p-3 text-sm text-tinta"
            placeholder="O que funcionou bem ou o que poderia melhorar?"
          />
        </label>

        <Button
          type="submit"
          disabled={avaliar.isPending || nota === 0 || recomendacao === null}
          className="mt-4 h-11 rounded-full bg-tinta px-6 text-sobre-tinta hover:bg-tinta/90"
        >
          Enviar avaliação
        </Button>
      </form>
    </section>
  );
}
