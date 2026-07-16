using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Clientes;
using TechPro.Api.Modules.Onboarding.Dtos;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Onboarding;

/// <summary>
/// Encapsula o wizard de ativação (módulo 0). O checklist é derivado dos dados
/// reais (sempre exato, sem estado novo); só a conclusão do wizard e os dados
/// de exemplo têm efeito de escrita. Os passos em si (horários, serviços,
/// peças) usam os endpoints já existentes — aqui é só o entorno.
/// </summary>
public class OnboardingService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição sem tenant resolvido.");

    public async Task<OnboardingStatusResponse> ObterStatusAsync()
    {
        var empresa = await db.Empresas.SingleAsync();
        var horariosConfigurados = await db.HorariosFuncionamento.AnyAsync(h => h.Ativo);
        var temServico = await db.Servicos.AnyAsync(s => s.Ativo && !s.Exemplo);
        var temPeca = await db.Pecas.AnyAsync(p => p.Ativo);
        var temCliente = await db.Clientes.AnyAsync(c => c.Ativo && !c.Exemplo);
        var temDadosExemplo = await db.Clientes.AnyAsync(c => c.Exemplo)
            || await db.OrdensServico.AnyAsync(o => o.Exemplo && o.DeletedAt == null);

        var passos = new PassosOnboarding(
            LojaConfigurada: !string.IsNullOrWhiteSpace(empresa.Nome),
            HorariosConfigurados: horariosConfigurados,
            TemServico: temServico,
            TemPeca: temPeca,
            TemCliente: temCliente);

        var concluidos = new[]
        {
            passos.LojaConfigurada, passos.HorariosConfigurados,
            passos.TemServico, passos.TemPeca, passos.TemCliente,
        }.Count(p => p);

        return new OnboardingStatusResponse(
            empresa.OnboardingConcluidoEm is not null,
            passos,
            concluidos,
            TotalPassos: 5,
            temDadosExemplo);
    }

    public async Task ConcluirAsync()
    {
        var empresa = await db.Empresas.SingleAsync();
        empresa.OnboardingConcluidoEm ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Cria um cliente, um serviço e uma OS fictícios (marcados Exemplo) para o
    /// dono "sentir" o produto. Idempotente — não duplica se já existirem.
    /// Criados direto (sem os gatilhos de notificação de OS).
    /// </summary>
    public async Task CarregarDadosExemploAsync()
    {
        if (await db.Clientes.AnyAsync(c => c.Exemplo))
        {
            return;
        }

        var agora = DateTimeOffset.UtcNow;

        var cliente = new Cliente
        {
            TenantId = TenantId,
            Nome = "Cliente Exemplo (Maria)",
            Telefone = "(11) 90000-0000",
            Observacoes = "Registro de exemplo — pode remover no card de ativação.",
            Exemplo = true,
            CriadoEm = agora,
        };

        var servico = new Servico
        {
            TenantId = TenantId,
            Nome = "Troca de tela (exemplo)",
            Categoria = "Reparo",
            PrecoBase = 350m,
            DuracaoEstimadaMinutos = 60,
            Exemplo = true,
            CriadoEm = agora,
        };
        db.Clientes.Add(cliente);
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();

        var numero = (await db.OrdensServico.MaxAsync(o => (int?)o.Numero) ?? 0) + 1;
        var ordem = new OrdemServico
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Numero = numero,
            ClienteId = cliente.Id,
            ServicoId = servico.Id,
            AparelhoMarca = "Samsung",
            AparelhoModelo = "Galaxy A54",
            DescricaoProblema = "Tela trincada (exemplo).",
            Etapa = EtapaOrdemServico.EmReparo,
            CodigoAcompanhamento = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8)),
            Exemplo = true,
            CriadoEm = agora,
        };
        db.OrdensServico.Add(ordem);
        db.HistoricosEtapaOrdemServico.Add(new OrdemServicoHistoricoEtapa
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OrdemServicoId = ordem.Id,
            DeEtapa = null,
            ParaEtapa = EtapaOrdemServico.EmReparo,
            Motivo = "Dados de exemplo",
            CriadoEm = agora,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Remove os registros de exemplo respeitando as FKs (OS antes das raízes).</summary>
    public async Task RemoverDadosExemploAsync()
    {
        var ordens = await db.OrdensServico.Where(o => o.Exemplo).ToListAsync();
        var ordemIds = ordens.Select(o => o.Id).ToList();

        var historico = await db.HistoricosEtapaOrdemServico
            .Where(h => ordemIds.Contains(h.OrdemServicoId))
            .ToListAsync();
        db.HistoricosEtapaOrdemServico.RemoveRange(historico);
        db.OrdensServico.RemoveRange(ordens);

        // Notificações fictícias, se houver (não têm FK, mas limpamos por higiene).
        var mensagens = await db.MensagensEnviadas
            .Where(m => m.OrdemServicoId != null && ordemIds.Contains(m.OrdemServicoId.Value))
            .ToListAsync();
        db.MensagensEnviadas.RemoveRange(mensagens);
        await db.SaveChangesAsync();

        db.Servicos.RemoveRange(await db.Servicos.Where(s => s.Exemplo).ToListAsync());
        db.Clientes.RemoveRange(await db.Clientes.Where(c => c.Exemplo).ToListAsync());
        await db.SaveChangesAsync();
    }
}
