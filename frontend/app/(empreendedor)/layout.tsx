"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAuth } from "@/lib/auth/AuthProvider";

/**
 * Guarda de rota do lado do cliente: tudo em (empreendedor) exige sessão.
 * A segurança real está na API (JWT + GQF + RLS) — aqui é só UX.
 */
export default function EmpreendedorLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  const { usuario, carregando } = useAuth();
  const router = useRouter();

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

  return <main className="min-h-screen bg-white">{children}</main>;
}
