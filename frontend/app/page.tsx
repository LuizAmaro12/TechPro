import { redirect } from "next/navigation";

// A raiz ainda não tem landing page (fase posterior): manda para o dashboard,
// e o guard de (empreendedor) devolve para /login quem não tem sessão.
export default function PaginaInicial() {
  redirect("/dashboard");
}
