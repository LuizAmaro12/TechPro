"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { AlternadorTema } from "@/components/ui/alternador-tema";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth/AuthProvider";

// `papeis` ausente = visível para todos. Esconder não é segurança — o backend
// é a fonte da verdade —, é não levar o usuário a um 403.
const LINKS = [
  { href: "/dashboard", rotulo: "Visão geral" },
  { href: "/kanban", rotulo: "Kanban" },
  { href: "/ordens-servico", rotulo: "Ordens" },
  { href: "/agenda", rotulo: "Agenda" },
  { href: "/clientes", rotulo: "Clientes" },
  { href: "/avaliacoes", rotulo: "Avaliações" },
  { href: "/financeiro", rotulo: "Financeiro", papeis: ["gestor"] },
  { href: "/servicos", rotulo: "Serviços" },
  { href: "/pecas", rotulo: "Peças", papeis: ["gestor", "tecnico"] },
];

// Configurações sai da navegação principal de propósito: não é uma seção de
// conteúdo como Kanban ou Ordens, é ajuste do app — e com 10 itens o menu já
// não cabia na largura do cabeçalho. Vira ícone junto das ações da direita,
// sem restrição de papel (qualquer pessoa precisa trocar a própria senha; as
// abas de gestão da loja é que ficam escondidas dentro da página).
const CONFIGURACOES = "/configuracoes";

/**
 * Guarda de rota do lado do cliente: tudo em (empreendedor) exige sessão.
 * A segurança real está na API (JWT + GQF + RLS) — aqui é só UX.
 */
export default function EmpreendedorLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  const { usuario, carregando, sair } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (!carregando && !usuario) router.replace("/login");
  }, [carregando, usuario, router]);

  if (carregando || !usuario) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-superficie">
        <p className="text-sm text-tinta-suave">Carregando sua sessão...</p>
      </main>
    );
  }

  async function aoSair() {
    await sair();
    router.replace("/login");
  }

  return (
    <main className="min-h-screen bg-superficie">
      <header className="mx-auto flex w-full max-w-6xl items-center justify-between gap-4 px-6 py-6">
        <div className="flex min-w-0 items-center gap-6">
          <span className="text-lg font-bold text-tinta">TechPro</span>
          <nav className="flex min-w-0 items-center gap-1 overflow-x-auto [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
            {LINKS.filter(
              (link) => !link.papeis || link.papeis.includes(usuario.papel ?? ""),
            ).map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={`shrink-0 rounded-full px-3 py-1.5 text-sm whitespace-nowrap transition-colors ${
                  pathname.startsWith(link.href)
                    ? "bg-tinta font-semibold text-sobre-tinta"
                    : "text-tinta-suave hover:text-tinta"
                }`}
              >
                {link.rotulo}
              </Link>
            ))}
          </nav>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <AlternadorTema compacto />
          <Link
            href={CONFIGURACOES}
            aria-label="Configurações"
            title="Configurações"
            aria-current={pathname.startsWith(CONFIGURACOES) ? "page" : undefined}
            className={`flex h-9 w-9 items-center justify-center rounded-full transition-colors ${
              pathname.startsWith(CONFIGURACOES)
                ? "bg-tinta text-sobre-tinta"
                : "text-tinta-suave hover:bg-sutil hover:text-tinta"
            }`}
          >
            <EngrenagemIcone className="h-4.5 w-4.5" />
          </Link>
          <Button
            variant="ghost"
            onClick={aoSair}
            className="text-tinta-suave hover:text-tinta"
          >
            Sair
          </Button>
        </div>
      </header>
      {children}
    </main>
  );
}

/** Ícone de linha fina, como o resto do sistema (guia estético, seção 3). */
function EngrenagemIcone(props: React.SVGProps<SVGSVGElement>) {
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
      <circle cx="12" cy="12" r="3.2" />
      <path d="M19.4 15a1.7 1.7 0 0 0 .34 1.87l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.7 1.7 0 0 0-1.87-.34 1.7 1.7 0 0 0-1.03 1.56V21a2 2 0 1 1-4 0v-.09A1.7 1.7 0 0 0 8.9 19.3a1.7 1.7 0 0 0-1.87.34l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.7 1.7 0 0 0 .34-1.87 1.7 1.7 0 0 0-1.56-1.03H3a2 2 0 1 1 0-4h.09A1.7 1.7 0 0 0 4.7 8.9a1.7 1.7 0 0 0-.34-1.87l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.7 1.7 0 0 0 1.87.34H9.1a1.7 1.7 0 0 0 1.03-1.56V3a2 2 0 1 1 4 0v.09a1.7 1.7 0 0 0 1.03 1.56 1.7 1.7 0 0 0 1.87-.34l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.7 1.7 0 0 0-.34 1.87v.01a1.7 1.7 0 0 0 1.56 1.03H21a2 2 0 1 1 0 4h-.09a1.7 1.7 0 0 0-1.56 1.03Z" />
    </svg>
  );
}
