"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useDeleteApiEquipeId,
  useGetApiEquipe,
  usePostApiEquipe,
  usePutApiEquipeId,
  type EquipeMembroResponse,
} from "@/lib/api-client/gerado";

export const ROTULO_PAPEL: Record<string, string> = {
  gestor: "Gestor",
  tecnico: "Técnico",
  atendente: "Atendente",
};

const DESCRICAO_PAPEL: Record<string, string> = {
  gestor: "Vê tudo, inclusive financeiro, configurações e dados pessoais.",
  tecnico: "Bancada: OS, estoque e peças. Não vê financeiro nem configurações.",
  atendente: "Balcão: clientes, agenda e pagamentos. Não vê custo nem margem.",
};

const NOVO = { nome: "", email: "", senha: "", papel: "tecnico" };

/** Membros da loja com função — só o gestor chega até aqui. */
export function SecaoEquipe() {
  const { data: resposta, refetch } = useGetApiEquipe({ incluirInativos: true });
  const membros = resposta?.status === 200 ? resposta.data : [];

  const criar = usePostApiEquipe();
  const atualizar = usePutApiEquipeId();
  const desativar = useDeleteApiEquipeId();

  const [formAberto, setFormAberto] = useState(false);
  const [novo, setNovo] = useState(NOVO);

  function reportar(erro: unknown, padrao: string) {
    toast.error(erro instanceof ApiError ? erro.message : padrao);
  }

  async function aoCriar(evento: React.FormEvent) {
    evento.preventDefault();
    try {
      await criar.mutateAsync({ data: novo });
      setNovo(NOVO);
      setFormAberto(false);
      await refetch();
      toast.success("Membro adicionado.");
    } catch (erro) {
      reportar(erro, "Não foi possível adicionar o membro.");
    }
  }

  async function aoTrocarPapel(membro: EquipeMembroResponse, papel: string) {
    try {
      await atualizar.mutateAsync({
        id: membro.id!,
        data: { nome: membro.nome ?? "", papel },
      });
      await refetch();
      toast.success("Função atualizada.");
    } catch (erro) {
      reportar(erro, "Não foi possível trocar a função.");
    }
  }

  async function aoDesativar(membro: EquipeMembroResponse) {
    if (!window.confirm(`Desativar ${membro.nome}? Ele perde o acesso ao sistema.`)) return;
    try {
      await desativar.mutateAsync({ id: membro.id! });
      await refetch();
      toast.success("Membro desativado.");
    } catch (erro) {
      reportar(erro, "Não foi possível desativar o membro.");
    }
  }

  return (
    <section className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold text-[#14162B]">Equipe</h2>
          <p className="mt-1 text-sm text-[#6B7280]">
            Quem trabalha na loja e o que cada função enxerga.
          </p>
        </div>
        <Button
          variant={formAberto ? "ghost" : "outline"}
          className="h-10"
          onClick={() => setFormAberto((v) => !v)}
        >
          {formAberto ? "Cancelar" : "Adicionar membro"}
        </Button>
      </div>

      {formAberto && (
        <form onSubmit={aoCriar} className="mt-4 grid gap-3 sm:grid-cols-2">
          <div>
            <Label htmlFor="membroNome">Nome</Label>
            <Input
              id="membroNome"
              required
              value={novo.nome}
              onChange={(e) => setNovo({ ...novo, nome: e.target.value })}
              className="mt-1 h-11"
            />
          </div>
          <div>
            <Label htmlFor="membroEmail">E-mail</Label>
            <Input
              id="membroEmail"
              type="email"
              required
              value={novo.email}
              onChange={(e) => setNovo({ ...novo, email: e.target.value })}
              className="mt-1 h-11"
            />
          </div>
          <div>
            <Label htmlFor="membroSenha">Senha inicial</Label>
            <Input
              id="membroSenha"
              type="password"
              required
              minLength={8}
              value={novo.senha}
              onChange={(e) => setNovo({ ...novo, senha: e.target.value })}
              className="mt-1 h-11"
            />
            <p className="mt-1 text-xs text-[#8B8D98]">
              Combine com a pessoa; ela pode trocar depois em Sua conta.
            </p>
          </div>
          <div>
            <Label htmlFor="membroPapel">Função</Label>
            <select
              id="membroPapel"
              value={novo.papel}
              onChange={(e) => setNovo({ ...novo, papel: e.target.value })}
              className="mt-1 h-11 w-full rounded-md border border-[#14162B]/10 bg-white px-2 text-sm"
            >
              {Object.entries(ROTULO_PAPEL).map(([valor, rotulo]) => (
                <option key={valor} value={valor}>
                  {rotulo}
                </option>
              ))}
            </select>
            <p className="mt-1 text-xs text-[#8B8D98]">{DESCRICAO_PAPEL[novo.papel]}</p>
          </div>
          <div className="sm:col-span-2">
            <Button
              type="submit"
              disabled={criar.isPending}
              className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
            >
              Adicionar
            </Button>
          </div>
        </form>
      )}

      <div className="mt-4 overflow-x-auto rounded-xl border border-[#14162B]/8">
        <table className="w-full text-left text-sm">
          <thead className="bg-[#F7F7F9] text-xs text-[#6B7280] uppercase">
            <tr>
              <th className="px-4 py-3">Nome</th>
              <th className="px-4 py-3">E-mail</th>
              <th className="px-4 py-3">Função</th>
              <th className="px-4 py-3 text-right">Ações</th>
            </tr>
          </thead>
          <tbody>
            {membros.map((m) => (
              <tr
                key={m.id}
                className={`border-t border-[#14162B]/6 ${m.ativo ? "" : "opacity-50"}`}
              >
                <td className="px-4 py-3 font-medium text-[#14162B]">
                  {m.nome}
                  {!m.ativo && (
                    <span className="ml-2 rounded-full bg-[#F7F7F9] px-2 py-0.5 text-[10px] font-semibold text-[#8B8D98] uppercase">
                      inativo
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 text-[#6B7280]">{m.email}</td>
                <td className="px-4 py-3">
                  {m.ativo ? (
                    <select
                      aria-label={`Função de ${m.nome}`}
                      value={m.papel ?? "tecnico"}
                      onChange={(e) => aoTrocarPapel(m, e.target.value)}
                      className="h-9 rounded-md border border-[#14162B]/10 bg-white px-2 text-sm"
                    >
                      {Object.entries(ROTULO_PAPEL).map(([valor, rotulo]) => (
                        <option key={valor} value={valor}>
                          {rotulo}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <span className="text-[#6B7280]">
                      {ROTULO_PAPEL[m.papel ?? ""] ?? m.papel}
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 text-right">
                  {m.ativo && (
                    <Button
                      variant="ghost"
                      className="h-8 px-3 text-xs text-[#E8536B] hover:text-[#E8536B]"
                      onClick={() => aoDesativar(m)}
                    >
                      Desativar
                    </Button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
