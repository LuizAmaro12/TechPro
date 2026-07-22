import { AlternadorTema } from "@/components/ui/alternador-tema";

export default function AuthLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <main className="relative flex min-h-screen flex-1 items-center justify-center bg-superficie px-4 py-12">
      {/* Também aqui: quem chega no login com o tema "errado" consegue trocar
          sem precisar entrar antes. */}
      <div className="absolute top-6 right-6">
        <AlternadorTema compacto />
      </div>
      <div className="w-full max-w-md">{children}</div>
    </main>
  );
}
