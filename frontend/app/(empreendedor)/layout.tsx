"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth/AuthProvider";

const LINKS = [
  { href: "/dashboard", rotulo: "Visão geral" },
  { href: "/kanban", rotulo: "Kanban" },
  { href: "/ordens-servico", rotulo: "Ordens" },
  { href: "/agenda", rotulo: "Agenda" },
  { href: "/clientes", rotulo: "Clientes" },
  { href: "/financeiro", rotulo: "Financeiro" },
  { href: "/servicos", rotulo: "Serviços" },
  { href: "/pecas", rotulo: "Peças" },
  { href: "/configuracoes", rotulo: "Configurações" },
];

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
      <main className="flex min-h-screen items-center justify-center bg-white">
        <p className="text-sm text-[#6B7280]">Carregando sua sessão...</p>
      </main>
    );
  }

  async function aoSair() {
    await sair();
    router.replace("/login");
  }

  return (
    <main className="min-h-screen bg-white">
      <header className="mx-auto flex w-full max-w-5xl items-center justify-between px-6 py-6">
        <div className="flex items-center gap-8">
          <span className="text-lg font-bold text-[#14162B]">TechPro</span>
          <nav className="flex items-center gap-1">
            {LINKS.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={`rounded-full px-4 py-1.5 text-sm transition-colors ${
                  pathname.startsWith(link.href)
                    ? "bg-[#14162B] font-semibold text-white"
                    : "text-[#6B7280] hover:text-[#14162B]"
                }`}
              >
                {link.rotulo}
              </Link>
            ))}
          </nav>
        </div>
        <Button
          variant="ghost"
          onClick={aoSair}
          className="text-[#6B7280] hover:text-[#14162B]"
        >
          Sair
        </Button>
      </header>
      {children}
    </main>
  );
}
