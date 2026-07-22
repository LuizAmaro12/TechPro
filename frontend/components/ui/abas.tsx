"use client";

import { AnimatePresence, motion } from "motion/react";
import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useRef } from "react";

export type Aba = {
  id: string;
  rotulo: string;
  /** Contador opcional ao lado do rótulo (ex.: pendências). */
  contador?: number;
  conteudo: React.ReactNode;
};

/**
 * Abas com indicador deslizante e troca de conteúdo suave.
 *
 * Decisões:
 * - **Estado na URL** (`?aba=`): recarregar a página, voltar no histórico ou
 *   mandar o link para alguém cai na mesma aba. Numa tela de configuração isso
 *   importa mais do que em abas decorativas.
 * - **Só a aba ativa é montada**: as seções fazem requisições próprias; montar
 *   todas dispararia chamadas que o usuário nem pediu (e algumas dariam 403
 *   para papéis sem acesso).
 * - **Respeita `prefers-reduced-motion`** via `useReducedMotion` implícito do
 *   motion: quem pediu menos animação recebe a troca sem deslocamento.
 */
export function Abas({ abas, parametro = "aba" }: { abas: Aba[]; parametro?: string }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const listaRef = useRef<HTMLDivElement>(null);

  const daUrl = searchParams.get(parametro);
  const ativa = abas.some((a) => a.id === daUrl) ? daUrl! : abas[0]?.id;

  const selecionar = useCallback(
    (id: string) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set(parametro, id);
      // replace (não push): trocar de aba não deve encher o histórico de voltas.
      router.replace(`?${params.toString()}`, { scroll: false });
    },
    [router, searchParams, parametro],
  );

  /** Setas navegam entre abas, como manda o padrão de tablist. */
  function aoTeclar(evento: React.KeyboardEvent) {
    const direcao = evento.key === "ArrowRight" ? 1 : evento.key === "ArrowLeft" ? -1 : 0;
    if (direcao === 0) return;
    evento.preventDefault();
    const atual = abas.findIndex((a) => a.id === ativa);
    const proxima = abas[(atual + direcao + abas.length) % abas.length];
    selecionar(proxima.id);
    listaRef.current
      ?.querySelector<HTMLButtonElement>(`[data-aba="${proxima.id}"]`)
      ?.focus();
  }

  const conteudo = abas.find((a) => a.id === ativa)?.conteudo;

  return (
    <div>
      <div
        ref={listaRef}
        role="tablist"
        aria-label="Seções das configurações"
        onKeyDown={aoTeclar}
        className="mt-6 flex gap-1 overflow-x-auto border-b border-borda"
      >
        {abas.map((aba) => {
          const selecionada = aba.id === ativa;
          return (
            <button
              key={aba.id}
              data-aba={aba.id}
              role="tab"
              type="button"
              aria-selected={selecionada}
              tabIndex={selecionada ? 0 : -1}
              onClick={() => selecionar(aba.id)}
              className={`relative shrink-0 px-4 py-2.5 text-sm transition-colors ${
                selecionada
                  ? "font-semibold text-tinta"
                  : "text-tinta-suave hover:text-tinta"
              }`}
            >
              <span className="flex items-center gap-1.5">
                {aba.rotulo}
                {aba.contador != null && aba.contador > 0 && (
                  <span className="rounded-full bg-marca-fundo px-1.5 py-0.5 text-[10px] font-semibold text-marca">
                    {aba.contador}
                  </span>
                )}
              </span>
              {selecionada && (
                // layoutId faz o indicador deslizar entre as abas em vez de
                // sumir e reaparecer.
                <motion.span
                  layoutId="indicador-aba"
                  className="absolute inset-x-2 -bottom-px h-0.5 rounded-full bg-tinta"
                  transition={{ type: "spring", stiffness: 420, damping: 34 }}
                />
              )}
            </button>
          );
        })}
      </div>

      <AnimatePresence mode="wait" initial={false}>
        <motion.div
          key={ativa}
          role="tabpanel"
          initial={{ opacity: 0, y: 6 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -6 }}
          transition={{ duration: 0.18, ease: "easeOut" }}
        >
          {conteudo}
        </motion.div>
      </AnimatePresence>
    </div>
  );
}
