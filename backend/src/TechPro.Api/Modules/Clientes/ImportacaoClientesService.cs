using Microsoft.EntityFrameworkCore;
using TechPro.Api.Modules.Clientes.Dtos;
using TechPro.Api.Modules.ServicosEPecas;
using TechPro.Api.Shared.Persistence;
using TechPro.Api.Shared.Tenancy;

namespace TechPro.Api.Modules.Clientes;

/// <summary>
/// Importa a carteira existente da loja por CSV — a porta de entrada do
/// produto. Só adiciona: importa as linhas válidas e **reporta** as duplicadas
/// e inválidas, para a loja corrigir e reimportar (a deduplicação por telefone
/// torna o reenvio seguro).
/// </summary>
public class ImportacaoClientesService(TechProDbContext db, ITenantProvider tenantProvider)
{
    private const int TetoLinhas = 5000;

    private Guid TenantId => tenantProvider.TenantId
        ?? throw new InvalidOperationException("Requisição autenticada sem tenant_id.");

    public async Task<CatalogoResultado<ImportacaoClientesResponse>> ImportarAsync(string conteudo)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
        {
            return CatalogoResultado<ImportacaoClientesResponse>.Falha("Envie o conteúdo do CSV.");
        }

        var linhas = QuebrarLinhas(conteudo);
        if (linhas.Count == 0)
        {
            return CatalogoResultado<ImportacaoClientesResponse>.Falha("O arquivo está vazio.");
        }

        var delimitador = DetectarDelimitador(linhas[0]);
        var cabecalho = ParsearLinha(linhas[0], delimitador)
            .Select(NormalizarCabecalho).ToList();

        var idxNome = IndiceDe(cabecalho, "nome", "name", "cliente");
        var idxTelefone = IndiceDe(cabecalho, "telefone", "celular", "whatsapp", "fone", "phone");
        if (idxNome < 0 || idxTelefone < 0)
        {
            return CatalogoResultado<ImportacaoClientesResponse>.Falha(
                "O CSV precisa de colunas de nome e telefone no cabeçalho.");
        }

        var idxEmail = IndiceDe(cabecalho, "email", "e-mail");
        var idxCpf = IndiceDe(cabecalho, "cpf");
        var idxEndereco = IndiceDe(cabecalho, "endereco", "endereço");
        var idxObs = IndiceDe(cabecalho, "observacoes", "observações", "obs", "observacao");

        var dados = linhas.Skip(1).ToList();
        if (dados.Count > TetoLinhas)
        {
            return CatalogoResultado<ImportacaoClientesResponse>.Falha(
                $"O arquivo tem {dados.Count} linhas; o limite por importação é {TetoLinhas}.");
        }

        // Telefones já existentes (ativos) — deduplicação contra o banco.
        var existentes = (await db.Clientes
                .Where(c => c.Ativo)
                .Select(c => c.Telefone)
                .ToListAsync())
            .Select(SoDigitos)
            .Where(d => d.Length > 0)
            .ToHashSet();

        var erros = new List<LinhaImportacaoResponse>();
        var noArquivo = new HashSet<string>();
        var novos = new List<Cliente>();
        var duplicados = 0;

        for (var i = 0; i < dados.Count; i++)
        {
            var numeroLinha = i + 2; // +1 pelo cabeçalho, +1 para base-1 (como o usuário vê)
            if (string.IsNullOrWhiteSpace(dados[i]))
            {
                continue; // linha em branco no fim do arquivo: ignora em silêncio
            }

            var campos = ParsearLinha(dados[i], delimitador);
            var nome = Campo(campos, idxNome);
            var telefone = Campo(campos, idxTelefone);

            if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(telefone))
            {
                erros.Add(new LinhaImportacaoResponse(numeroLinha, "Nome e telefone são obrigatórios."));
                continue;
            }

            var digitos = SoDigitos(telefone);
            if (digitos.Length < 8)
            {
                erros.Add(new LinhaImportacaoResponse(numeroLinha, "Telefone inválido."));
                continue;
            }

            if (existentes.Contains(digitos) || !noArquivo.Add(digitos))
            {
                duplicados++;
                continue;
            }

            novos.Add(new Cliente
            {
                TenantId = TenantId,
                CriadoEm = DateTimeOffset.UtcNow,
                Nome = nome!.Trim(),
                Telefone = telefone!.Trim(),
                Email = Limpar(Campo(campos, idxEmail)),
                Cpf = Limpar(Campo(campos, idxCpf)),
                Endereco = Limpar(Campo(campos, idxEndereco)),
                Observacoes = Limpar(Campo(campos, idxObs)),
                // Importar uma lista não é consentimento LGPD.
                ConsentiuComunicacoes = false,
            });
        }

        if (novos.Count > 0)
        {
            db.Clientes.AddRange(novos);
            await db.SaveChangesAsync();
        }

        return CatalogoResultado<ImportacaoClientesResponse>.Ok(new ImportacaoClientesResponse(
            dados.Count(l => !string.IsNullOrWhiteSpace(l)),
            novos.Count,
            duplicados,
            erros));
    }

    // --- Parser CSV mínimo (aspas + delimitador , ou ;) -----------------------------

    private static List<string> QuebrarLinhas(string conteudo) =>
        conteudo.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n')
            .ToList();

    private static char DetectarDelimitador(string cabecalho)
    {
        // Fora de aspas, quem aparece mais no cabeçalho vence; empate → vírgula.
        var (virgulas, pontosVirgula) = (0, 0);
        var dentroAspas = false;
        foreach (var c in cabecalho)
        {
            if (c == '"')
            {
                dentroAspas = !dentroAspas;
            }
            else if (!dentroAspas && c == ',')
            {
                virgulas++;
            }
            else if (!dentroAspas && c == ';')
            {
                pontosVirgula++;
            }
        }

        return pontosVirgula > virgulas ? ';' : ',';
    }

    private static List<string> ParsearLinha(string linha, char delimitador)
    {
        var campos = new List<string>();
        var atual = new System.Text.StringBuilder();
        var dentroAspas = false;

        for (var i = 0; i < linha.Length; i++)
        {
            var c = linha[i];
            if (dentroAspas)
            {
                if (c == '"')
                {
                    // Aspas duplicadas dentro do campo = uma aspa literal.
                    if (i + 1 < linha.Length && linha[i + 1] == '"')
                    {
                        atual.Append('"');
                        i++;
                    }
                    else
                    {
                        dentroAspas = false;
                    }
                }
                else
                {
                    atual.Append(c);
                }
            }
            else if (c == '"')
            {
                dentroAspas = true;
            }
            else if (c == delimitador)
            {
                campos.Add(atual.ToString());
                atual.Clear();
            }
            else
            {
                atual.Append(c);
            }
        }

        campos.Add(atual.ToString());
        return campos;
    }

    private static string NormalizarCabecalho(string valor) =>
        RemoverAcentos(valor.Trim().ToLowerInvariant());

    private static int IndiceDe(List<string> cabecalho, params string[] nomes)
    {
        for (var i = 0; i < cabecalho.Count; i++)
        {
            if (nomes.Contains(cabecalho[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? Campo(List<string> campos, int indice) =>
        indice >= 0 && indice < campos.Count ? campos[indice] : null;

    private static string? Limpar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string SoDigitos(string valor) =>
        new(valor.Where(char.IsDigit).ToArray());

    private static string RemoverAcentos(string texto)
    {
        var normalizado = texto.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalizado)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
