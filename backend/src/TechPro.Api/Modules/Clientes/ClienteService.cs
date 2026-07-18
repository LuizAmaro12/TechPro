using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Api;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Clientes;

public class ClienteService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    public async Task<PaginaResponse<ClienteResponse>> ListarAsync(
        string? busca, bool somenteVip, bool incluirInativos, int pagina, int tamanhoPagina)
    {
        var query = db.Clientes.Include(c => c.ClientePrincipal).AsQueryable();
        if (!incluirInativos)
        {
            query = query.Where(c => c.Ativo);
        }

        if (somenteVip)
        {
            query = query.Where(c => c.Vip);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(c =>
                c.Nome.ToLower().Contains(termo) ||
                c.Telefone.Contains(termo) ||
                (c.Cpf != null && c.Cpf.Contains(termo)));
        }

        var total = await query.CountAsync();
        var itens = await query
            .OrderBy(c => c.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(c => new
            {
                Cliente = c,
                QuantidadeAparelhos = c.Aparelhos.Count(a => a.Ativo),
            })
            .ToListAsync();

        return new PaginaResponse<ClienteResponse>(
            itens.Select(x => ParaResponse(x.Cliente, x.QuantidadeAparelhos)).ToList(),
            total, pagina, tamanhoPagina);
    }

    public async Task<ClienteDetalheResponse?> ObterAsync(int id)
    {
        var cliente = await db.Clientes
            .Include(c => c.ClientePrincipal)
            .Include(c => c.Aparelhos)
            .SingleOrDefaultAsync(c => c.Id == id);
        if (cliente is null)
        {
            return null;
        }

        var resumo = ParaResponse(cliente, cliente.Aparelhos.Count(a => a.Ativo));
        return new ClienteDetalheResponse(
            resumo.Id, resumo.Nome, resumo.Telefone, resumo.Email, resumo.Cpf, resumo.Endereco,
            resumo.Observacoes, resumo.Vip, resumo.Ativo, resumo.ClientePrincipal,
            resumo.ConsentiuComunicacoes, resumo.ConsentimentoEm, resumo.AnonimizadoEm,
            cliente.Aparelhos
                .OrderBy(a => a.Id)
                .Select(ParaAparelhoResponse)
                .ToList());
    }

    public async Task<CatalogoResultado<ClienteResponse>> CriarAsync(ClienteRequest request)
    {
        var erroVinculo = await ValidarVinculoAsync(request.ClientePrincipalId, clienteId: null);
        if (erroVinculo is not null)
        {
            return CatalogoResultado<ClienteResponse>.Falha(erroVinculo);
        }

        var cliente = new Cliente
        {
            TenantId = TenantId,
            CriadoEm = DateTimeOffset.UtcNow,
            Nome = request.Nome.Trim(),
            Telefone = request.Telefone.Trim(),
        };
        Aplicar(cliente, request);
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var criado = await db.Clientes.Include(c => c.ClientePrincipal).SingleAsync(c => c.Id == cliente.Id);
        return CatalogoResultado<ClienteResponse>.Ok(ParaResponse(criado, quantidadeAparelhos: 0));
    }

    public async Task<CatalogoResultado<ClienteResponse>?> AtualizarAsync(int id, ClienteRequest request)
    {
        var cliente = await db.Clientes.SingleOrDefaultAsync(c => c.Id == id);
        if (cliente is null)
        {
            return null;
        }

        var erroVinculo = await ValidarVinculoAsync(request.ClientePrincipalId, clienteId: id);
        if (erroVinculo is not null)
        {
            return CatalogoResultado<ClienteResponse>.Falha(erroVinculo);
        }

        Aplicar(cliente, request);
        await db.SaveChangesAsync();

        var atualizado = await db.Clientes.Include(c => c.ClientePrincipal).SingleAsync(c => c.Id == id);
        var quantidade = await db.Aparelhos.CountAsync(a => a.ClienteId == id && a.Ativo);
        return CatalogoResultado<ClienteResponse>.Ok(ParaResponse(atualizado, quantidade));
    }

    public async Task<bool> DesativarAsync(int id)
    {
        var cliente = await db.Clientes.SingleOrDefaultAsync(c => c.Id == id);
        if (cliente is null)
        {
            return false;
        }

        cliente.Ativo = false;
        await db.SaveChangesAsync();
        return true;
    }

    private void Aplicar(Cliente cliente, ClienteRequest request)
    {
        cliente.Nome = request.Nome.Trim();
        cliente.Telefone = request.Telefone.Trim();
        cliente.Email = Normalizar(request.Email)?.ToLowerInvariant();
        cliente.Cpf = Normalizar(request.Cpf);
        cliente.Endereco = Normalizar(request.Endereco);
        cliente.Observacoes = Normalizar(request.Observacoes);
        cliente.Vip = request.Vip;
        cliente.Ativo = request.Ativo;
        cliente.ClientePrincipalId = request.ClientePrincipalId;

        // O consentimento ganha carimbo de data na primeira vez que é dado e
        // perde o carimbo se for revogado (módulo 14 — base mínima, Fase 1).
        if (request.ConsentiuComunicacoes && !cliente.ConsentiuComunicacoes)
        {
            cliente.ConsentimentoEm = DateTimeOffset.UtcNow;
        }
        else if (!request.ConsentiuComunicacoes)
        {
            cliente.ConsentimentoEm = null;
        }

        cliente.ConsentiuComunicacoes = request.ConsentiuComunicacoes;
    }

    /// <summary>
    /// Vínculo família/empresa com 1 nível só. O GQF faz cliente de outra
    /// empresa "não existir" aqui — referência cruzada vira 400 (anti-IDOR).
    /// </summary>
    private async Task<string?> ValidarVinculoAsync(int? clientePrincipalId, int? clienteId)
    {
        if (clientePrincipalId is null)
        {
            return null;
        }

        if (clientePrincipalId == clienteId)
        {
            return "Um cliente não pode ser vinculado a si mesmo.";
        }

        var principal = await db.Clientes.SingleOrDefaultAsync(c => c.Id == clientePrincipalId);
        if (principal is null)
        {
            return "Cliente principal não encontrado.";
        }

        if (principal.ClientePrincipalId is not null)
        {
            return "Este cliente já é vinculado a outro — o vínculo tem um nível só.";
        }

        if (clienteId is not null &&
            await db.Clientes.AnyAsync(c => c.ClientePrincipalId == clienteId))
        {
            return "Este cliente possui vinculados e não pode virar vinculado de outro.";
        }

        return null;
    }

    /// <summary>
    /// Vínculo silencioso por telefone (decisão 2026-07-13): compara só os
    /// dígitos com clientes ativos do tenant; telefone inédito cria cliente
    /// novo. Usado pelo portal público de agendamento e pela conversão de
    /// agendamento em OS.
    /// </summary>
    public async Task<Cliente> VincularOuCriarPorTelefoneAsync(
        string nome, string telefone, string? email)
    {
        telefone = telefone.Trim();
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Ativo
            && c.Telefone
                .Replace("(", "").Replace(")", "").Replace("-", "")
                .Replace(" ", "").Replace(".", "").Replace("+", "") == digitos);
        if (cliente is not null)
        {
            return cliente;
        }

        cliente = new Cliente
        {
            TenantId = TenantId,
            Nome = nome.Trim(),
            Telefone = telefone,
            Email = Normalizar(email),
            CriadoEm = DateTimeOffset.UtcNow,
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        return cliente;
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ClienteResponse ParaResponse(Cliente c, int quantidadeAparelhos) => new(
        c.Id, c.Nome, c.Telefone, c.Email, c.Cpf, c.Endereco, c.Observacoes, c.Vip, c.Ativo,
        c.ClientePrincipal is null ? null : new VinculoResponse(c.ClientePrincipal.Id, c.ClientePrincipal.Nome),
        c.ConsentiuComunicacoes, c.ConsentimentoEm, quantidadeAparelhos, c.AnonimizadoEm);

    internal static AparelhoResponse ParaAparelhoResponse(Aparelho a) => new(
        a.Id, a.Marca, a.Modelo, a.Imei, a.SenhaDesbloqueio, a.Observacoes, a.Ativo);
}
