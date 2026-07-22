"use client";

import { motion } from "motion/react";
import { useTheme } from "next-themes";
import { useSyncExternalStore } from "react";

/**
 * "Já hidratou?" sem `setState` em efeito: o snapshot do servidor é `false` e
 * o do cliente é `true`, então o React re-renderiza sozinho após a hidratação.
 */
const semAssinatura = () => () => {};
function useJaHidratou() {
  return useSyncExternalStore(
    semAssinatura,
    () => true,
    () => false,
  );
}

const OPCOES = [
  { valor: "light", rotulo: "Claro", icone: SolIcone },
  { valor: "system", rotulo: "Sistema", icone: SistemaIcone },
  { valor: "dark", rotulo: "Escuro", icone: LuaIcone },
] as const;

/**
 * Alternador de tema em três estados (claro / sistema / escuro).
 *
 * Decisões:
 * - **Segmentado, não botão que cicla**: com três estados, um botão único
 *   obriga o usuário a adivinhar a ordem e a clicar duas vezes para voltar.
 *   Aqui os três estados ficam visíveis e a escolha é um clique.
 * - **"Sistema" existe e é o padrão**: quem configurou o SO no escuro espera
 *   que o app respeite isso sem precisar mexer em nada.
 * - **Só renderiza depois de montar**: o tema resolvido só existe no cliente;
 *   pintar antes causaria divergência de hidratação.
 */
export function AlternadorTema({ compacto = false }: { compacto?: boolean }) {
  const { theme, setTheme } = useTheme();
  const montado = useJaHidratou();

  // Placeholder do mesmo tamanho: evita o layout "pular" quando monta.
  if (!montado) {
    return <div aria-hidden className={compacto ? "h-9 w-28" : "h-9 w-36"} />;
  }

  const atual = theme ?? "system";

  return (
    <div
      role="radiogroup"
      aria-label="Tema da interface"
      className="inline-flex items-center gap-0.5 rounded-full border border-borda bg-sutil p-0.5"
    >
      {OPCOES.map(({ valor, rotulo, icone: Icone }) => {
        const ativo = atual === valor;
        return (
          <button
            key={valor}
            type="button"
            role="radio"
            aria-checked={ativo}
            aria-label={rotulo}
            title={rotulo}
            onClick={() => setTheme(valor)}
            className={`relative flex h-8 items-center gap-1.5 rounded-full px-2.5 text-xs transition-colors ${
              ativo ? "text-sobre-tinta" : "text-tinta-suave hover:text-tinta"
            }`}
          >
            {ativo && (
              <motion.span
                layoutId="indicador-tema"
                className="absolute inset-0 rounded-full bg-tinta"
                transition={{ type: "spring", stiffness: 420, damping: 34 }}
              />
            )}
            <Icone className="relative z-10 h-4 w-4" />
            {!compacto && <span className="relative z-10">{rotulo}</span>}
          </button>
        );
      })}
    </div>
  );
}

// Ícones de linha fina, monocromáticos — o guia estético pede stroke, não
// preenchido (docs/UI_UX-referencia.md, seção 3).

function SolIcone(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      {...props}
    >
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
    </svg>
  );
}

function LuaIcone(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
      {...props}
    >
      <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8Z" />
    </svg>
  );
}

function SistemaIcone(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
      {...props}
    >
      <rect x="2.5" y="4" width="19" height="12.5" rx="2" />
      <path d="M8.5 20.5h7" />
    </svg>
  );
}
