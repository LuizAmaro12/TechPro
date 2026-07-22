import { AlternadorTema } from "@/components/ui/alternador-tema";

/**
 * Layout do portal do cliente final: público, sem guarda de sessão — o
 * agendamento não exige login (módulo 1 do doc de produto).
 */
export default function PortalClienteLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <main className="min-h-screen bg-superficie">
      <header className="mx-auto flex w-full max-w-2xl items-center justify-between px-6 py-6">
        <span className="text-lg font-bold text-tinta">TechPro</span>
        <AlternadorTema compacto />
      </header>
      {children}
    </main>
  );
}
