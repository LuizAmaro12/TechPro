/**
 * Layout do portal do cliente final: público, sem guarda de sessão — o
 * agendamento não exige login (módulo 1 do doc de produto).
 */
export default function PortalClienteLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <main className="min-h-screen bg-white">
      <header className="mx-auto flex w-full max-w-2xl items-center px-6 py-6">
        <span className="text-lg font-bold text-[#14162B]">TechPro</span>
      </header>
      {children}
    </main>
  );
}
