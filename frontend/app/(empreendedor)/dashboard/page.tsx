"use client";

import { useGetApiAuthMe } from "@/lib/api-client/gerado";
import { useAuth } from "@/lib/auth/AuthProvider";

const ROTULO_PAPEL: Record<string, string> = {
  gestor: "Gestor",
  tecnico: "Técnico",
  atendente: "Atendente",
};

export default function PaginaDashboard() {
  const { usuario } = useAuth();
  const { data: respostaMe } = useGetApiAuthMe();
  const me = respostaMe?.status === 200 ? respostaMe.data : undefined;

  const nomeEmpresa = me?.empresa?.nome ?? "—";
  const papel = ROTULO_PAPEL[me?.papel ?? usuario?.papel ?? ""] ?? "—";
  const tenantId = me?.tenantId ?? usuario?.tenantId ?? "—";

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <section>
        <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
          Visão geral
        </p>
        <h1 className="mt-2 text-3xl font-bold text-[#14162B]">
          Olá, {me?.nome ?? usuario?.nome ?? "..."}
        </h1>
        <p className="mt-1 text-sm text-[#6B7280]">
          Comece cadastrando seus serviços e peças no catálogo — as ordens de
          serviço e o financeiro chegam nas próximas etapas.
        </p>

        {/* Assinatura visual do guia de referência: card branco limpo sobre
            um glow gradiente — único ponto onde a cor "explode" na página. */}
        <div className="relative mt-10">
          <div
            aria-hidden
            className="absolute -inset-4 rounded-3xl bg-gradient-to-r from-orange-400 via-pink-500 to-indigo-500 opacity-15 blur-2xl"
          />
          <div className="relative grid gap-6 rounded-2xl border border-[#14162B]/8 bg-white p-8 sm:grid-cols-3">
            <div>
              <p className="text-xs font-medium text-[#8B8D98] uppercase tracking-wide">
                Empresa
              </p>
              <p className="mt-1 text-lg font-semibold text-[#14162B]">
                {nomeEmpresa}
              </p>
            </div>
            <div>
              <p className="text-xs font-medium text-[#8B8D98] uppercase tracking-wide">
                Seu papel
              </p>
              <p className="mt-1 text-lg font-semibold text-[#14162B]">{papel}</p>
            </div>
            <div className="min-w-0">
              <p className="text-xs font-medium text-[#8B8D98] uppercase tracking-wide">
                Tenant ID
              </p>
              <p
                className="mt-1 truncate font-mono text-sm text-[#6B7280]"
                title={tenantId}
              >
                {tenantId}
              </p>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
