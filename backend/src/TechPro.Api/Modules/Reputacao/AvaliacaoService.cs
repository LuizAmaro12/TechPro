using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.OrdensServico;
using TechPro.Api.Modules.Reputacao.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Reputacao;

/// <summary>
/// Avaliações e reputação (módulo 10): registra o veredito do cliente sobre um
/// reparo entregue, resume satisfação (estrelas + NPS, inclusive por técnico) e
/// gerencia o fechamento de loop das avaliações negativas.
/// </summary>
public class AvaliacaoService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição sem tenant resolvido.");

    public Task<bool> JaAvaliadaAsync(Guid ordemId) =>
        db.Avaliacoes.AnyAsync(a => a.OrdemServicoId == ordemId);

    // --- Envio público -------------------------------------------------------------

    /// <summary>
    /// Registra a avaliação do cliente. Só de OS entregue (gatilho e envio só
    /// após entrega, doc) e uma por OS. Snapshot de serviço/técnico.
    /// </summary>
    public async Task<CatalogoResultado<Avaliacao>> RegistrarAsync(OrdemServico ordem, AvaliacaoRequest request)
    {
        if (ordem.Etapa != EtapaOrdemServico.Entregue)
        {
            return CatalogoResultado<Avaliacao>.Falha("Só é possível avaliar após a entrega do aparelho.");
        }

        if (await db.Avaliacoes.AnyAsync(a => a.OrdemServicoId == ordem.Id))
        {
            return CatalogoResultado<Avaliacao>.Falha("Esta ordem de serviço já foi avaliada.");
        }

        var avaliacao = new Avaliacao
        {
            TenantId = TenantId,
            OrdemServicoId = ordem.Id,
            ClienteId = ordem.ClienteId,
            ServicoId = ordem.ServicoId,
            ResponsavelTecnicoId = ordem.ResponsavelTecnicoId,
            Nota = request.Nota,
            Recomendacao = request.Recomendacao,
            Comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim(),
            CriadoEm = DateTimeOffset.UtcNow,
        };
        db.Avaliacoes.Add(avaliacao);
        await db.SaveChangesAsync();
        return CatalogoResultado<Avaliacao>.Ok(avaliacao);
    }

    // --- Consulta interna ----------------------------------------------------------

    public async Task<List<AvaliacaoResponse>> ListarAsync(bool apenasPendentes)
    {
        var avaliacoes = await db.Avaliacoes
            .Include(a => a.Cliente)
            .Include(a => a.Servico)
            .Include(a => a.OrdemServico)
            .ToListAsync();

        // "Pendente" = negativa ainda não resolvida (o loop aberto).
        var filtradas = (apenasPendentes
                ? avaliacoes.Where(a => a.EhNegativa && !a.Resolvida)
                : avaliacoes)
            .OrderByDescending(a => a.CriadoEm)
            .ToList();

        var nomes = await NomesTecnicosAsync(filtradas.Select(a => a.ResponsavelTecnicoId));
        return filtradas.Select(a => ParaResponse(a, nomes)).ToList();
    }

    public async Task<ResumoAvaliacoesResponse> ResumoAsync()
    {
        // Agregação em memória: Sqlite (testes) não agrega decimais no servidor.
        var todas = await db.Avaliacoes.ToListAsync();
        if (todas.Count == 0)
        {
            return new ResumoAvaliacoesResponse(
                0, 0m, DistribuicaoVazia(), new NpsResponse(0, 0, 0, 0), 0, []);
        }

        var distribuicao = Enumerable.Range(1, 5)
            .Select(estrela => new DistribuicaoEstrela(estrela, todas.Count(a => a.Nota == estrela)))
            .Reverse()
            .ToList();

        var nomes = await NomesTecnicosAsync(todas.Select(a => a.ResponsavelTecnicoId));
        var porTecnico = todas
            .Where(a => a.ResponsavelTecnicoId is not null)
            .GroupBy(a => a.ResponsavelTecnicoId!.Value)
            .Select(g => new SatisfacaoTecnicoResponse(
                g.Key,
                nomes.TryGetValue(g.Key, out var nome) ? nome : "—",
                g.Count(),
                Media(g),
                CalcularNps(g.ToList()).Score))
            .OrderByDescending(t => t.Total)
            .ToList();

        return new ResumoAvaliacoesResponse(
            todas.Count,
            Media(todas),
            distribuicao,
            CalcularNps(todas),
            todas.Count(a => a.EhNegativa && !a.Resolvida),
            porTecnico);
    }

    // --- Fechamento de loop --------------------------------------------------------

    public async Task<CatalogoResultado<AvaliacaoResponse>?> ResolverAsync(
        int id, ResolverAvaliacaoRequest request, Guid? usuarioId)
    {
        var avaliacao = await db.Avaliacoes
            .Include(a => a.Cliente)
            .Include(a => a.Servico)
            .Include(a => a.OrdemServico)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (avaliacao is null)
        {
            return null;
        }

        if (!avaliacao.EhNegativa)
        {
            return CatalogoResultado<AvaliacaoResponse>.Falha(
                "Só avaliações negativas abrem loop de tratamento.");
        }

        if (avaliacao.Resolvida)
        {
            return CatalogoResultado<AvaliacaoResponse>.Falha("Esta avaliação já foi resolvida.");
        }

        avaliacao.Resolvida = true;
        avaliacao.ResolucaoNota = request.Nota.Trim();
        avaliacao.ResolvidaEm = DateTimeOffset.UtcNow;
        avaliacao.ResolvidaPorUsuarioId = usuarioId;
        await db.SaveChangesAsync();

        var nomes = await NomesTecnicosAsync([avaliacao.ResponsavelTecnicoId]);
        return CatalogoResultado<AvaliacaoResponse>.Ok(ParaResponse(avaliacao, nomes));
    }

    // --- Auxiliares ----------------------------------------------------------------

    private static List<DistribuicaoEstrela> DistribuicaoVazia() =>
        Enumerable.Range(1, 5).Select(e => new DistribuicaoEstrela(e, 0)).Reverse().ToList();

    private static decimal Media(IEnumerable<Avaliacao> avaliacoes)
    {
        var lista = avaliacoes.ToList();
        return lista.Count == 0 ? 0m : Math.Round((decimal)lista.Average(a => a.Nota), 1);
    }

    /// <summary>NPS clássico: promotores 9–10, neutros 7–8, detratores 0–6.</summary>
    private static NpsResponse CalcularNps(List<Avaliacao> avaliacoes)
    {
        var total = avaliacoes.Count;
        var promotores = avaliacoes.Count(a => a.Recomendacao >= 9);
        var neutros = avaliacoes.Count(a => a.Recomendacao is 7 or 8);
        var detratores = avaliacoes.Count(a => a.Recomendacao <= 6);
        var score = total == 0
            ? 0
            : (int)Math.Round((promotores - detratores) / (double)total * 100);
        return new NpsResponse(score, promotores, neutros, detratores);
    }

    /// <summary>usuarios é plano de controle (sem GQF) — filtro por tenant explícito.</summary>
    private async Task<Dictionary<Guid, string>> NomesTecnicosAsync(IEnumerable<Guid?> ids)
    {
        var alvos = ids.Where(i => i is not null).Select(i => i!.Value).Distinct().ToList();
        if (alvos.Count == 0)
        {
            return [];
        }

        return await db.Users
            .Where(u => alvos.Contains(u.Id) && u.TenantId == TenantId)
            .ToDictionaryAsync(u => u.Id, u => u.Nome);
    }

    private static AvaliacaoResponse ParaResponse(Avaliacao a, Dictionary<Guid, string> nomes) => new(
        a.Id,
        a.OrdemServico!.Numero,
        a.Cliente?.Nome,
        a.Servico!.Nome,
        a.ResponsavelTecnicoId is { } tid && nomes.TryGetValue(tid, out var nome) ? nome : null,
        a.Nota,
        a.Recomendacao,
        a.Comentario,
        a.EhNegativa,
        a.Resolvida,
        a.ResolucaoNota,
        a.ResolvidaEm,
        a.CriadoEm);
}
