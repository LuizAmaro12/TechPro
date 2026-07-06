export default function AuthLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <main className="flex min-h-screen flex-1 items-center justify-center bg-white px-4 py-12">
      <div className="w-full max-w-md">{children}</div>
    </main>
  );
}
