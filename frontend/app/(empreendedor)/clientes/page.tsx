"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api-client/fetcher";
import {
  useDeleteApiClientesClienteIdAparelhosId,
  useDeleteApiClientesId,
  useGetApiClientes,
  useGetApiClientesId,
  usePostApiClientes,
  usePostApiClientesClienteIdAparelhos,
  usePutApiClientesClienteIdAparelhosId,
  usePutApiClientesId,
  type AparelhoResponse,
  type ClienteResponse,
} from "@/lib/api-client/gerado";
import {
  esquemaAparelho,
  esquemaCliente,
  type ValoresAparelho,
  type ValoresCliente,
} from "@/lib/validators/clientes";

const CLIENTE_INICIAL: ValoresCliente = {
  nome: "",
  telefone: "",
  email: "",
  cpf: "",
  endereco: "",
  observacoes: "",
  vip: false,
  consentiuComunicacoes: true,
  clientePrincipalId: "",
};

const APARELHO_INICIAL: ValoresAparelho = {
  marca: "",
  modelo: "",
  imei: "",
  senhaDesbloqueio: "",
  observacoes: "",
};

export default function PaginaClientes() {
  const queryClient = useQueryClient();
  const [formAberto, setFormAberto] = useState(false);
  const [editandoId, setEditandoId] = useState<number | null>(null);
  const [mostrarInativos, setMostrarInativos] = useState(false);
  const [somenteVip, setSomenteVip] = useState(false);
  const [busca, setBusca] = useState("");
  const [editandoAparelhoId, setEditandoAparelhoId] = useState<number | null>(null);
  const [formAparelhoAberto, setFormAparelhoAberto] = useState(false);

  const { data: respostaClientes } = useGetApiClientes({
    busca: busca || undefined,
    somenteVip: somenteVip || undefined,
    incluirInativos: mostrarInativos || undefined,
  });
  const clientes =
    respostaClientes?.status === 200 ? respostaClientes.data : undefined;

  // Detalhe só é buscado quando um cliente está em edição (traz os aparelhos).
  const { data: respostaDetalhe } = useGetApiClientesId(editandoId ?? 0, {
    query: { enabled: editandoId !== null },
  });
  const detalhe = respostaDetalhe?.status === 200 ? respostaDetalhe.data : undefined;
  const aparelhos = (detalhe?.aparelhos ?? []).filter((a) => a.ativo);

  const criarCliente = usePostApiClientes();
  const atualizarCliente = usePutApiClientesId();
  const desativarCliente = useDeleteApiClientesId();
  const criarAparelho = usePostApiClientesClienteIdAparelhos();
  const atualizarAparelho = usePutApiClientesClienteIdAparelhosId();
  const desativarAparelho = useDeleteApiClientesClienteIdAparelhosId();

  const formCliente = useForm<ValoresCliente>({
    resolver: zodResolver(esquemaCliente),
    defaultValues: CLIENTE_INICIAL,
  });
  const formAparelho = useForm<ValoresAparelho>({
    resolver: zodResolver(esquemaAparelho),
    defaultValues: APARELHO_INICIAL,
  });

  // Opções plausíveis de vínculo: ativos, sem principal próprio e != editado.
  const opcoesVinculo = (clientes?.itens ?? []).filter(
    (c) => c.ativo && !c.clientePrincipal && c.id !== editandoId,
  );

  function invalidar() {
    queryClient.invalidateQueries();
  }

  function abrirCriacao() {
    setEditandoId(null);
    setFormAparelhoAberto(false);
    formCliente.reset(CLIENTE_INICIAL);
    setFormAberto(true);
  }

  function abrirEdicao(cliente: ClienteResponse) {
    setEditandoId(cliente.id ?? null);
    setFormAparelhoAberto(false);
    setEditandoAparelhoId(null);
    formCliente.reset({
      nome: cliente.nome ?? "",
      telefone: cliente.telefone ?? "",
      email: cliente.email ?? "",
      cpf: cliente.cpf ?? "",
      endereco: cliente.endereco ?? "",
      observacoes: cliente.observacoes ?? "",
      vip: cliente.vip ?? false,
      consentiuComunicacoes: cliente.consentiuComunicacoes ?? false,
      clientePrincipalId: cliente.clientePrincipal?.id
        ? String(cliente.clientePrincipal.id)
        : "",
    });
    setFormAberto(true);
  }

  async function aoSalvarCliente(valores: ValoresCliente) {
    const corpo = {
      nome: valores.nome,
      telefone: valores.telefone,
      email: valores.email || null,
      cpf: valores.cpf || null,
      endereco: valores.endereco || null,
      observacoes: valores.observacoes || null,
      vip: valores.vip,
      consentiuComunicacoes: valores.consentiuComunicacoes,
      clientePrincipalId: valores.clientePrincipalId
        ? Number(valores.clientePrincipalId)
        : null,
      ativo: true,
    };
    try {
      if (editandoId === null) {
        const resposta = await criarCliente.mutateAsync({ data: corpo });
        toast.success("Cliente cadastrado.");
        if (resposta.status === 201 && resposta.data.id) {
          // Segue direto para a edição, onde os aparelhos podem ser adicionados.
          setEditandoId(resposta.data.id);
        }
      } else {
        await atualizarCliente.mutateAsync({ id: editandoId, data: corpo });
        toast.success("Cliente atualizado.");
        setFormAberto(false);
      }
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar o cliente.");
    }
  }

  async function aoDesativarCliente(id: number | undefined) {
    if (id === undefined) return;
    try {
      await desativarCliente.mutateAsync({ id });
      toast.success("Cliente desativado.");
      invalidar();
    } catch (erro) {
      toast.error(
        erro instanceof ApiError ? erro.message : "Erro ao desativar o cliente.",
      );
    }
  }

  function abrirNovoAparelho() {
    setEditandoAparelhoId(null);
    formAparelho.reset(APARELHO_INICIAL);
    setFormAparelhoAberto(true);
  }

  function abrirEdicaoAparelho(aparelho: AparelhoResponse) {
    setEditandoAparelhoId(aparelho.id ?? null);
    formAparelho.reset({
      marca: aparelho.marca ?? "",
      modelo: aparelho.modelo ?? "",
      imei: aparelho.imei ?? "",
      senhaDesbloqueio: aparelho.senhaDesbloqueio ?? "",
      observacoes: aparelho.observacoes ?? "",
    });
    setFormAparelhoAberto(true);
  }

  async function aoSalvarAparelho(valores: ValoresAparelho) {
    if (editandoId === null) return;
    const corpo = {
      marca: valores.marca,
      modelo: valores.modelo,
      imei: valores.imei || null,
      senhaDesbloqueio: valores.senhaDesbloqueio || null,
      observacoes: valores.observacoes || null,
      ativo: true,
    };
    try {
      if (editandoAparelhoId === null) {
        await criarAparelho.mutateAsync({ clienteId: editandoId, data: corpo });
        toast.success("Aparelho adicionado.");
      } else {
        await atualizarAparelho.mutateAsync({
          clienteId: editandoId,
          id: editandoAparelhoId,
          data: corpo,
        });
        toast.success("Aparelho atualizado.");
      }
      setFormAparelhoAberto(false);
      invalidar();
    } catch (erro) {
      toast.error(erro instanceof ApiError ? erro.message : "Erro ao salvar o aparelho.");
    }
  }

  async function aoDesativarAparelho(id: number | undefined) {
    if (editandoId === null || id === undefined) return;
    try {
      await desativarAparelho.mutateAsync({ clienteId: editandoId, id });
      toast.success("Aparelho desativado.");
      invalidar();
    } catch (erro) {
      toast.error(
        erro instanceof ApiError ? erro.message : "Erro ao desativar o aparelho.",
      );
    }
  }

  const errosCliente = formCliente.formState.errors;
  const errosAparelho = formAparelho.formState.errors;

  return (
    <div className="mx-auto w-full max-w-5xl px-6 py-10">
      <div className="flex items-end justify-between gap-4">
        <div>
          <p className="text-[11px] font-semibold tracking-[0.18em] text-[#E8536B] uppercase">
            CRM
          </p>
          <h1 className="mt-2 text-3xl font-bold text-[#14162B]">Clientes</h1>
          <p className="mt-1 text-sm text-[#6B7280]">
            Quem confia os aparelhos à sua assistência — contatos, aparelhos e
            consentimento de comunicação.
          </p>
        </div>
        <Button
          onClick={abrirCriacao}
          className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
        >
          Novo cliente
        </Button>
      </div>

      {formAberto && (
        <div className="mt-8 rounded-2xl border border-[#14162B]/8 bg-white p-6">
          <form onSubmit={formCliente.handleSubmit(aoSalvarCliente)}>
            <h2 className="text-lg font-semibold text-[#14162B]">
              {editandoId === null ? "Novo cliente" : "Editar cliente"}
            </h2>

            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <div>
                <Label htmlFor="nome">Nome</Label>
                <Input
                  id="nome"
                  className="mt-1 h-11"
                  aria-invalid={!!errosCliente.nome}
                  {...formCliente.register("nome")}
                />
                {errosCliente.nome && (
                  <p className="mt-1 text-sm text-destructive">
                    {errosCliente.nome.message}
                  </p>
                )}
              </div>
              <div>
                <Label htmlFor="telefone">Telefone / WhatsApp</Label>
                <Input
                  id="telefone"
                  className="mt-1 h-11"
                  aria-invalid={!!errosCliente.telefone}
                  {...formCliente.register("telefone")}
                />
                {errosCliente.telefone && (
                  <p className="mt-1 text-sm text-destructive">
                    {errosCliente.telefone.message}
                  </p>
                )}
              </div>
              <div>
                <Label htmlFor="email">E-mail (opcional)</Label>
                <Input
                  id="email"
                  className="mt-1 h-11"
                  aria-invalid={!!errosCliente.email}
                  {...formCliente.register("email")}
                />
                {errosCliente.email && (
                  <p className="mt-1 text-sm text-destructive">
                    {errosCliente.email.message}
                  </p>
                )}
              </div>
              <div>
                <Label htmlFor="cpf">CPF (opcional)</Label>
                <Input
                  id="cpf"
                  className="mt-1 h-11"
                  aria-invalid={!!errosCliente.cpf}
                  {...formCliente.register("cpf")}
                />
                {errosCliente.cpf && (
                  <p className="mt-1 text-sm text-destructive">
                    {errosCliente.cpf.message}
                  </p>
                )}
              </div>
              <div className="sm:col-span-2">
                <Label htmlFor="endereco">Endereço (opcional)</Label>
                <Input id="endereco" className="mt-1 h-11" {...formCliente.register("endereco")} />
              </div>
              <div className="sm:col-span-2">
                <Label htmlFor="observacoes">Observações (opcional)</Label>
                <Input
                  id="observacoes"
                  className="mt-1 h-11"
                  {...formCliente.register("observacoes")}
                />
              </div>
              <div>
                <Label htmlFor="clientePrincipalId">Vinculado a (família/empresa)</Label>
                <select
                  id="clientePrincipalId"
                  className="mt-1 h-11 w-full rounded-md border border-input bg-white px-3 text-sm"
                  {...formCliente.register("clientePrincipalId")}
                >
                  <option value="">Sem vínculo</option>
                  {opcoesVinculo.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.nome}
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex flex-col justify-end gap-2">
                <label className="flex items-center gap-2 text-sm text-[#14162B]">
                  <input type="checkbox" {...formCliente.register("vip")} />
                  Cliente VIP
                </label>
                <label className="flex items-center gap-2 text-sm text-[#14162B]">
                  <input type="checkbox" {...formCliente.register("consentiuComunicacoes")} />
                  Autorizou receber comunicações sobre os serviços (LGPD)
                </label>
              </div>
            </div>

            <div className="mt-6 flex gap-3">
              <Button
                type="submit"
                disabled={formCliente.formState.isSubmitting}
                className="h-11 rounded-full bg-[#14162B] px-6 text-white hover:bg-[#14162B]/90"
              >
                {formCliente.formState.isSubmitting ? "Salvando..." : "Salvar cliente"}
              </Button>
              <Button type="button" variant="ghost" onClick={() => setFormAberto(false)}>
                Fechar
              </Button>
            </div>
          </form>

          {editandoId !== null && (
            <div className="mt-8 border-t border-[#14162B]/8 pt-6">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-[#14162B]">
                  Aparelhos deste cliente
                </h3>
                <Button type="button" variant="outline" className="h-9" onClick={abrirNovoAparelho}>
                  Adicionar aparelho
                </Button>
              </div>

              {aparelhos.length === 0 && !formAparelhoAberto && (
                <p className="mt-3 text-sm text-[#6B7280]">
                  Nenhum aparelho cadastrado ainda.
                </p>
              )}

              <ul className="mt-3 space-y-2">
                {aparelhos.map((aparelho) => (
                  <li
                    key={aparelho.id}
                    className="flex items-center justify-between rounded-xl border border-[#14162B]/8 px-4 py-3"
                  >
                    <div>
                      <p className="text-sm font-medium text-[#14162B]">
                        {aparelho.marca} {aparelho.modelo}
                      </p>
                      <p className="text-xs text-[#6B7280]">
                        {aparelho.imei ? `IMEI/série: ${aparelho.imei}` : "Sem IMEI/série"}
                        {aparelho.observacoes ? ` · ${aparelho.observacoes}` : ""}
                      </p>
                    </div>
                    <div className="whitespace-nowrap">
                      <Button
                        type="button"
                        variant="ghost"
                        className="h-8 px-3"
                        onClick={() => abrirEdicaoAparelho(aparelho)}
                      >
                        Editar
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        className="h-8 px-3 text-[#E8536B] hover:text-[#E8536B]"
                        onClick={() => aoDesativarAparelho(aparelho.id)}
                      >
                        Desativar
                      </Button>
                    </div>
                  </li>
                ))}
              </ul>

              {formAparelhoAberto && (
                <form
                  onSubmit={formAparelho.handleSubmit(aoSalvarAparelho)}
                  className="mt-4 rounded-xl bg-[#F7F7F9] p-4"
                >
                  <div className="grid gap-3 sm:grid-cols-2">
                    <div>
                      <Label htmlFor="marca">Marca</Label>
                      <Input
                        id="marca"
                        className="mt-1 h-10 bg-white"
                        aria-invalid={!!errosAparelho.marca}
                        {...formAparelho.register("marca")}
                      />
                      {errosAparelho.marca && (
                        <p className="mt-1 text-sm text-destructive">
                          {errosAparelho.marca.message}
                        </p>
                      )}
                    </div>
                    <div>
                      <Label htmlFor="modelo">Modelo</Label>
                      <Input
                        id="modelo"
                        className="mt-1 h-10 bg-white"
                        aria-invalid={!!errosAparelho.modelo}
                        {...formAparelho.register("modelo")}
                      />
                      {errosAparelho.modelo && (
                        <p className="mt-1 text-sm text-destructive">
                          {errosAparelho.modelo.message}
                        </p>
                      )}
                    </div>
                    <div>
                      <Label htmlFor="imei">IMEI / nº de série (opcional)</Label>
                      <Input id="imei" className="mt-1 h-10 bg-white" {...formAparelho.register("imei")} />
                    </div>
                    <div>
                      <Label htmlFor="senhaDesbloqueio">Senha de desbloqueio (opcional)</Label>
                      <Input
                        id="senhaDesbloqueio"
                        className="mt-1 h-10 bg-white"
                        {...formAparelho.register("senhaDesbloqueio")}
                      />
                    </div>
                    <div className="sm:col-span-2">
                      <Label htmlFor="obsAparelho">Observações (opcional)</Label>
                      <Input
                        id="obsAparelho"
                        className="mt-1 h-10 bg-white"
                        {...formAparelho.register("observacoes")}
                      />
                    </div>
                  </div>
                  <div className="mt-4 flex gap-2">
                    <Button
                      type="submit"
                      disabled={formAparelho.formState.isSubmitting}
                      className="h-9 rounded-full bg-[#14162B] px-5 text-white hover:bg-[#14162B]/90"
                    >
                      {editandoAparelhoId === null ? "Adicionar" : "Salvar aparelho"}
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      className="h-9"
                      onClick={() => setFormAparelhoAberto(false)}
                    >
                      Cancelar
                    </Button>
                  </div>
                </form>
              )}
            </div>
          )}
        </div>
      )}

      <div className="mt-8 flex flex-wrap items-center justify-between gap-3">
        <Input
          placeholder="Buscar por nome, telefone ou CPF..."
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          className="h-10 max-w-xs"
        />
        <div className="flex items-center gap-4">
          <label className="flex items-center gap-2 text-sm text-[#6B7280]">
            <input
              type="checkbox"
              checked={somenteVip}
              onChange={(e) => setSomenteVip(e.target.checked)}
            />
            Somente VIP
          </label>
          <label className="flex items-center gap-2 text-sm text-[#6B7280]">
            <input
              type="checkbox"
              checked={mostrarInativos}
              onChange={(e) => setMostrarInativos(e.target.checked)}
            />
            Mostrar inativos
          </label>
        </div>
      </div>

      <div className="mt-3 overflow-x-auto rounded-2xl border border-[#14162B]/8">
        <table className="w-full text-left text-sm">
          <thead className="bg-[#F7F7F9] text-xs text-[#8B8D98] uppercase tracking-wide">
            <tr>
              <th className="px-4 py-3">Cliente</th>
              <th className="px-4 py-3">Telefone</th>
              <th className="px-4 py-3">E-mail</th>
              <th className="px-4 py-3">Aparelhos</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody>
            {(clientes?.itens ?? []).map((cliente) => (
              <tr key={cliente.id} className="border-t border-[#14162B]/6">
                <td className="px-4 py-3 font-medium text-[#14162B]">
                  {cliente.nome}
                  {cliente.vip && (
                    <span className="ml-2 rounded-full bg-[#E8536B]/10 px-2 py-0.5 text-xs font-semibold text-[#E8536B]">
                      VIP
                    </span>
                  )}
                  {cliente.clientePrincipal && (
                    <span
                      className="ml-2 rounded-full bg-[#F7F7F9] px-2 py-0.5 text-xs text-[#6B7280]"
                      title={`Vinculado a ${cliente.clientePrincipal.nome}`}
                    >
                      vínculo: {cliente.clientePrincipal.nome}
                    </span>
                  )}
                  {!cliente.ativo && (
                    <span className="ml-2 rounded-full bg-[#F7F7F9] px-2 py-0.5 text-xs text-[#8B8D98]">
                      inativo
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 text-[#6B7280]">{cliente.telefone}</td>
                <td className="px-4 py-3 text-[#6B7280]">{cliente.email ?? "—"}</td>
                <td className="px-4 py-3 text-[#6B7280]">{cliente.quantidadeAparelhos}</td>
                <td className="px-4 py-3 text-right whitespace-nowrap">
                  <Button
                    variant="ghost"
                    className="h-8 px-3"
                    onClick={() => abrirEdicao(cliente)}
                  >
                    Editar
                  </Button>
                  {cliente.ativo && (
                    <Button
                      variant="ghost"
                      className="h-8 px-3 text-[#E8536B] hover:text-[#E8536B]"
                      onClick={() => aoDesativarCliente(cliente.id)}
                    >
                      Desativar
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {clientes && (clientes.itens?.length ?? 0) === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-[#6B7280]">
                  Nenhum cliente encontrado. Clique em “Novo cliente” para começar.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
